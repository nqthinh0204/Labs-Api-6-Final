using AspLab06Final.Mvc.Models;
using AspLab06Final.Mvc.Repositories;
using AspLab06Final.Mvc.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AspLab06Final.Mvc.Services;

// Lab06 - Luồng nghiệp vụ nhiều bước có transaction: Tạo đơn Bán Sách + trừ tồn kho.
// "Nhiều bước": (1) đọc + kiểm tra tồn kho từng dòng sách, (2) trừ tồn kho, (3) tạo Sale + SaleItems.
// Toàn bộ phải THÀNH CÔNG CÙNG NHAU trong 1 transaction, hoặc rollback hết nếu bất kỳ bước nào lỗi.
public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IAuditLogService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SaleService> _logger;

    public SaleService(
        ISaleRepository saleRepository,
        IBookRepository bookRepository,
        IAuditLogService audit,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SaleService> logger)
    {
        _saleRepository = saleRepository;
        _bookRepository = bookRepository;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<SaleCreateViewModel> GetCreateModelAsync()
    {
        var books = await _bookRepository.GetActiveListAsync();
        return new SaleCreateViewModel
        {
            Lines = books
                .Where(b => b.Quantity > 0)
                .OrderBy(b => b.Title)
                .Select(b => new SaleCreateLineViewModel
                {
                    BookId = b.Id,
                    BookCode = b.BookCode,
                    Title = b.Title,
                    Price = b.Price,
                    AvailableQuantity = b.Quantity,
                    QuantityToSell = 0
                }).ToList()
        };
    }

    public async Task<OperationResult> CreateSaleAsync(SaleCreateViewModel model)
    {
        var requestedLines = model.Lines.Where(l => l.QuantityToSell > 0).ToList();
        if (requestedLines.Count == 0)
        {
            return OperationResult.Fail("", "Vui lòng nhập số lượng bán cho ít nhất 1 cuốn sách.");
        }

        await using var transaction = await _saleRepository.BeginTransactionAsync();
        try
        {
            var saleItems = new List<SaleItem>();
            decimal total = 0;

            foreach (var line in requestedLines)
            {
                // Luôn đọc lại giá + tồn kho MỚI NHẤT từ DB ngay tại thời điểm xử lý -
                // KHÔNG tin Price/AvailableQuantity mà client gửi kèm trong form (có thể cũ hoặc bị sửa).
                var book = await _bookRepository.GetByIdAsync(line.BookId);
                if (book == null)
                {
                    await transaction.RollbackAsync();
                    return OperationResult.Fail("", $"Không tìm thấy sách (Id={line.BookId}) hoặc sách đã bị xoá.");
                }

                if (line.QuantityToSell < 0)
                {
                    await transaction.RollbackAsync();
                    return OperationResult.Fail("", "Số lượng bán không hợp lệ.");
                }

                if (book.Quantity < line.QuantityToSell)
                {
                    await transaction.RollbackAsync();
                    return OperationResult.Fail("",
                        $"Sách '{book.Title}' chỉ còn {book.Quantity} cuốn trong kho, không đủ để bán {line.QuantityToSell}.");
                }

                book.Quantity -= line.QuantityToSell;
                book.UpdatedAt = DateTime.Now;

                saleItems.Add(new SaleItem
                {
                    BookId = book.Id,
                    BookTitleSnapshot = book.Title,
                    UnitPrice = book.Price,
                    Quantity = line.QuantityToSell
                });
                total += book.Price * line.QuantityToSell;
            }

            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            var sale = new Sale
            {
                SaleCode = $"SALE-{DateTime.Now:yyyyMMdd-HHmmss}-{Random.Shared.Next(1000, 9999)}",
                CreatedAt = DateTime.Now,
                CreatedByUserName = userName,
                TotalAmount = total,
                Items = saleItems
            };

            await _saleRepository.AddSaleAsync(sale);
            // 1 lần SaveChangesAsync duy nhất ghi nhận CẢ thay đổi Book.Quantity (nhiều dòng) LẪN Sale/SaleItems mới -
            // nếu bất kỳ phần nào lỗi (VD RowVersion của 1 cuốn sách bị đổi bởi request khác), toàn bộ đều rollback.
            await _saleRepository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Sale created. SaleCode={SaleCode}, Total={Total}", sale.SaleCode, total);
            await _audit.LogAsync("Information", "CreateSale", "Sale", sale.SaleCode,
                $"Tạo đơn bán {sale.SaleCode} gồm {saleItems.Count} dòng sách, tổng tiền {total:N0}đ.");

            return OperationResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning("Concurrency conflict while creating sale.");
            await _audit.LogAsync("Warning", "ConcurrencyConflict", "Sale", null,
                "Xung đột concurrency khi tạo đơn bán (tồn kho vừa bị người khác thay đổi).", "Failed");
            return OperationResult.Fail("", "Tồn kho vừa bị người khác thay đổi trong lúc bạn thao tác. Vui lòng tải lại trang và thử lại.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Unexpected error while creating sale.");
            await _audit.LogAsync("Error", "CreateSale", "Sale", null, "Lỗi hệ thống khi tạo đơn bán.", "Failed");
            return OperationResult.Fail("", "Có lỗi hệ thống xảy ra, đơn bán chưa được tạo. Vui lòng thử lại.");
        }
    }

    public async Task<List<SaleListItemViewModel>> GetHistoryAsync()
    {
        var sales = await _saleRepository.GetHistoryAsync();
        return sales.Select(s => new SaleListItemViewModel
        {
            Id = s.Id,
            SaleCode = s.SaleCode,
            CreatedAt = s.CreatedAt,
            CreatedByUserName = s.CreatedByUserName,
            TotalAmount = s.TotalAmount,
            ItemCount = s.Items.Count
        }).ToList();
    }

    public async Task<SaleDetailViewModel?> GetDetailAsync(int id)
    {
        var s = await _saleRepository.GetDetailAsync(id);
        if (s == null) return null;

        return new SaleDetailViewModel
        {
            Id = s.Id,
            SaleCode = s.SaleCode,
            CreatedAt = s.CreatedAt,
            CreatedByUserName = s.CreatedByUserName,
            TotalAmount = s.TotalAmount,
            Items = s.Items.Select(i => new SaleDetailItemViewModel
            {
                BookTitleSnapshot = i.BookTitleSnapshot,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}
