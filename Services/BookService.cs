using AspLab06Final.Mvc.Models;
using AspLab06Final.Mvc.Options;
using AspLab06Final.Mvc.Repositories;
using AspLab06Final.Mvc.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AspLab06Final.Mvc.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IAuditLogService _audit;
    private readonly IFileUploadService _fileUpload;
    private readonly ILogger<BookService> _logger;
    private readonly AppSettings _settings;

    public BookService(
        IBookRepository bookRepository,
        IAuditLogService audit,
        IFileUploadService fileUpload,
        ILogger<BookService> logger,
        IOptions<AppSettings> options)
    {
        _bookRepository = bookRepository;
        _audit = audit;
        _fileUpload = fileUpload;
        _logger = logger;
        _settings = options.Value;
    }

    private bool IsLow(Book b) =>
        b.Quantity > 0 && b.Quantity <= Math.Max(b.MinStock, _settings.LowStockThreshold);

    private BookListItemViewModel ToListItem(Book b) => new()
    {
        Id = b.Id,
        BookCode = b.BookCode,
        Title = b.Title,
        Author = b.Author,
        Price = b.Price,
        Quantity = b.Quantity,
        MinStock = b.MinStock,
        GenreName = b.Genre?.Name ?? "N/A",
        IsLowStock = IsLow(b),
        CreatedAt = b.CreatedAt,
        CoverImageUrl = b.CoverImageUrl
    };

    public async Task<List<BookListItemViewModel>> GetActiveListAsync()
    {
        var books = await _bookRepository.GetActiveListAsync();
        return books.Select(ToListItem).ToList();
    }

    public async Task<BookSearchViewModel> SearchAsync(string? keyword, string? stockStatus)
    {
        var threshold = _settings.LowStockThreshold;
        var books = await _bookRepository.SearchActiveAsync(keyword, stockStatus, threshold);
        return new BookSearchViewModel
        {
            Keyword = keyword,
            StockStatus = stockStatus,
            HasSearched = true,
            Results = books.Select(ToListItem).ToList()
        };
    }

    public async Task<BookDetailViewModel?> GetDetailAsync(int id)
    {
        var b = await _bookRepository.GetByIdReadOnlyAsync(id);
        if (b == null)
        {
            _logger.LogWarning("Book not found. BookId={BookId}", id);
            await _audit.LogAsync("Warning", "NotFound", "Book", id.ToString(), $"Không tìm thấy sách Id={id}.", "Failed");
            return null;
        }
        return new BookDetailViewModel
        {
            Id = b.Id,
            BookCode = b.BookCode,
            Title = b.Title,
            Author = b.Author,
            Publisher = b.Publisher,
            Price = b.Price,
            Quantity = b.Quantity,
            MinStock = b.MinStock,
            GenreName = b.Genre?.Name ?? "N/A",
            IsLowStock = IsLow(b),
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt,
            CoverImageUrl = b.CoverImageUrl
        };
    }

    public async Task<OperationResult> CreateAsync(BookCreateViewModel model)
    {
        // Custom validation nghiệp vụ: mã sách không được trùng (kể cả bản ghi đã soft delete)
        if (await _bookRepository.BookCodeExistsAsync(model.BookCode))
            return OperationResult.Fail(nameof(model.BookCode), "Mã sách này đã tồn tại.");

        var book = new Book
        {
            BookCode = model.BookCode,
            Title = model.Title,
            Author = model.Author,
            Publisher = model.Publisher,
            Price = model.Price,
            Quantity = model.Quantity,
            MinStock = model.MinStock,
            GenreId = model.GenreId,
            CreatedAt = DateTime.Now
        };

        await _bookRepository.AddAsync(book);
        await _bookRepository.SaveChangesAsync();

        _logger.LogInformation("Book created. BookId={BookId}, BookCode={BookCode}", book.Id, book.BookCode);
        await _audit.LogAsync("Information", "Create", "Book", book.BookCode,
            $"Tạo sách '{book.Title}' (BookCode={book.BookCode}).");
        return OperationResult.Ok();
    }

    public async Task<BookEditViewModel?> GetEditModelAsync(int id)
    {
        var b = await _bookRepository.GetByIdReadOnlyAsync(id);
        if (b == null) return null;
        return new BookEditViewModel
        {
            Id = b.Id,
            BookCode = b.BookCode,
            Title = b.Title,
            Author = b.Author,
            Publisher = b.Publisher,
            Price = b.Price,
            Quantity = b.Quantity,
            MinStock = b.MinStock,
            GenreId = b.GenreId,
            RowVersion = Convert.ToBase64String(b.RowVersion),
            CurrentCoverImageUrl = b.CoverImageUrl
        };
    }

    public async Task<OperationResult> UpdateAsync(BookEditViewModel model)
    {
        var book = await _bookRepository.GetByIdAsync(model.Id);
        if (book == null)
            return OperationResult.Fail("", "Không tìm thấy sách cần cập nhật.");

        if (await _bookRepository.BookCodeExistsAsync(model.BookCode, model.Id))
            return OperationResult.Fail(nameof(model.BookCode), "Mã sách này đã tồn tại.");

        book.Title = model.Title;
        book.BookCode = model.BookCode;
        book.Author = model.Author;
        book.Publisher = model.Publisher;
        book.Price = model.Price;
        book.Quantity = model.Quantity;
        book.MinStock = model.MinStock;
        book.GenreId = model.GenreId;
        book.UpdatedAt = DateTime.Now;

        if (!TrySetOriginalRowVersion(book, model.RowVersion))
            return OperationResult.Fail("", "RowVersion không hợp lệ. Vui lòng tải lại trang.");

        try
        {
            await _bookRepository.SaveChangesAsync();
            _logger.LogInformation("Book updated. BookId={BookId}", book.Id);
            await _audit.LogAsync("Information", "Edit", "Book", book.BookCode,
                $"Cập nhật sách '{book.Title}' (Id={book.Id}).");
            return OperationResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            _bookRepository.Detach(book); // bỏ thay đổi lỗi để ghi audit không bị lưu lại
            _logger.LogWarning("Concurrency conflict on update. BookId={BookId}", book.Id);
            await _audit.LogAsync("Warning", "ConcurrencyConflict", "Book", book.BookCode,
                $"Xung đột concurrency khi cập nhật sách Id={book.Id}.", "Failed");
            return OperationResult.Fail("",
                "Dữ liệu đã được người khác cập nhật. Vui lòng tải lại trang và thử lại.");
        }
    }

    public Task<BookDetailViewModel?> GetDeleteModelAsync(int id) => GetDetailAsync(id);

    public async Task<OperationResult> SoftDeleteAsync(int id)
    {
        var book = await _bookRepository.GetByIdAsync(id);
        if (book == null)
            return OperationResult.Fail("", "Không tìm thấy sách cần xóa.");

        book.IsDeleted = true;
        book.DeletedAt = DateTime.Now;
        book.UpdatedAt = DateTime.Now;
        await _bookRepository.SaveChangesAsync();

        _logger.LogWarning("Book soft deleted. BookId={BookId}, BookCode={BookCode}", book.Id, book.BookCode);
        await _audit.LogAsync("Warning", "SoftDelete", "Book", book.BookCode,
            $"Xóa mềm sách '{book.Title}' (Id={book.Id}).");
        return OperationResult.Ok();
    }

    public async Task<List<BookTrashItemViewModel>> GetTrashAsync()
    {
        var books = await _bookRepository.GetTrashAsync();
        return books.Select(b => new BookTrashItemViewModel
        {
            Id = b.Id,
            BookCode = b.BookCode,
            Title = b.Title,
            Author = b.Author,
            DeletedAt = b.DeletedAt
        }).ToList();
    }

    public async Task<OperationResult> RestoreAsync(int id)
    {
        var book = await _bookRepository.GetDeletedByIdAsync(id);
        if (book == null)
            return OperationResult.Fail("", "Không tìm thấy sách trong thùng rác.");

        book.IsDeleted = false;
        book.DeletedAt = null;
        book.UpdatedAt = DateTime.Now;
        await _bookRepository.SaveChangesAsync();

        _logger.LogInformation("Book restored. BookId={BookId}, BookCode={BookCode}", book.Id, book.BookCode);
        await _audit.LogAsync("Information", "Restore", "Book", book.BookCode,
            $"Khôi phục sách '{book.Title}' (Id={book.Id}).");
        return OperationResult.Ok();
    }

    public async Task<BookAdjustStockViewModel?> GetAdjustStockModelAsync(int id)
    {
        var b = await _bookRepository.GetByIdReadOnlyAsync(id);
        if (b == null) return null;
        return new BookAdjustStockViewModel
        {
            Id = b.Id,
            BookCode = b.BookCode,
            Title = b.Title,
            CurrentQuantity = b.Quantity,
            RowVersion = Convert.ToBase64String(b.RowVersion)
        };
    }

    public async Task<OperationResult> AdjustStockAsync(BookAdjustStockViewModel model)
    {
        var book = await _bookRepository.GetByIdAsync(model.Id);
        if (book == null)
            return OperationResult.Fail("", "Không tìm thấy sách cần điều chỉnh.");

        var newQty = book.Quantity + model.Delta;
        if (newQty < 0)
            return OperationResult.Fail(nameof(model.Delta),
                $"Tồn kho sau điều chỉnh không được nhỏ hơn 0 (hiện tại {book.Quantity}).");

        book.Quantity = newQty;
        book.UpdatedAt = DateTime.Now;

        if (!TrySetOriginalRowVersion(book, model.RowVersion))
            return OperationResult.Fail("", "RowVersion không hợp lệ. Vui lòng tải lại trang.");

        try
        {
            await _bookRepository.SaveChangesAsync();
            _logger.LogInformation("Stock adjusted. BookId={BookId}, Delta={Delta}, NewQuantity={NewQuantity}",
                book.Id, model.Delta, newQty);
            await _audit.LogAsync("Information", "AdjustStock", "Book", book.BookCode,
                $"Điều chỉnh tồn kho sách Id={book.Id}: {model.Delta:+#;-#;0} -> {newQty}.");
            return OperationResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            _bookRepository.Detach(book); // bỏ thay đổi lỗi để ghi audit không bị lưu lại
            _logger.LogWarning("Concurrency conflict on adjust stock. BookId={BookId}", book.Id);
            await _audit.LogAsync("Warning", "ConcurrencyConflict", "Book", book.BookCode,
                $"Xung đột concurrency khi điều chỉnh tồn kho Id={book.Id}.", "Failed");
            return OperationResult.Fail("",
                "Dữ liệu đã được người khác cập nhật. Vui lòng tải lại trang và thử lại.");
        }
    }

    // Lab06 Feature 2 - Upload / Thay ảnh bìa sách an toàn
    public async Task<BookImageUploadViewModel?> GetImageUploadModelAsync(int id)
    {
        var b = await _bookRepository.GetByIdReadOnlyAsync(id);
        if (b == null) return null;
        return new BookImageUploadViewModel
        {
            Id = b.Id,
            BookCode = b.BookCode,
            Title = b.Title,
            CurrentCoverImageUrl = b.CoverImageUrl
        };
    }

    public async Task<OperationResult> UploadCoverImageAsync(BookImageUploadViewModel model)
    {
        var book = await _bookRepository.GetByIdAsync(model.Id);
        if (book == null)
            return OperationResult.Fail("", "Không tìm thấy sách cần cập nhật ảnh.");

        if (model.CoverImageFile == null)
            return OperationResult.Fail(nameof(model.CoverImageFile), "Vui lòng chọn một file ảnh.");

        // Bước 1: validate + lưu file MỚI trước (chưa đụng gì tới ảnh cũ).
        // Nếu file mới không hợp lệ -> giữ nguyên ảnh cũ, trả lỗi rõ ràng, không thay đổi gì trong DB.
        var uploadResult = await _fileUpload.SaveBookCoverAsync(model.CoverImageFile);
        if (!uploadResult.Success)
        {
            _logger.LogWarning("Book cover upload rejected. BookId={BookId}, Reason={Reason}", book.Id, uploadResult.ErrorMessage);
            await _audit.LogAsync("Warning", "UploadRejected", "Book", book.BookCode,
                $"Upload ảnh bìa bị từ chối cho sách Id={book.Id}: {uploadResult.ErrorMessage}", "Failed");
            return OperationResult.Fail(nameof(model.CoverImageFile), uploadResult.ErrorMessage ?? "File không hợp lệ.");
        }

        var oldImageUrl = book.CoverImageUrl;
        var isReplace = !string.IsNullOrEmpty(oldImageUrl);

        // Bước 2: cập nhật DB trỏ sang ảnh MỚI.
        book.CoverImageUrl = uploadResult.RelativeUrl;
        book.UpdatedAt = DateTime.Now;

        try
        {
            await _bookRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // DB lưu thất bại -> dọn file mới vừa lưu, GIỮ NGUYÊN ảnh cũ + record cũ, không để rác lại trên đĩa.
            _fileUpload.DeleteBookCover(uploadResult.RelativeUrl);
            _logger.LogError(ex, "Failed to save new cover image reference. BookId={BookId}", book.Id);
            await _audit.LogAsync("Error", "UploadRejected", "Book", book.BookCode,
                $"Lưu ảnh bìa mới thất bại cho sách Id={book.Id}, đã rollback file.", "Failed");
            return OperationResult.Fail("", "Không thể lưu ảnh bìa vào cơ sở dữ liệu. Vui lòng thử lại.");
        }

        // Bước 3: DB đã lưu thành công -> giờ mới an toàn để xoá ảnh CŨ (nếu có).
        if (isReplace)
        {
            _fileUpload.DeleteBookCover(oldImageUrl);
        }

        var action = isReplace ? "ReplaceBookImage" : "UploadBookImage";
        _logger.LogInformation("Book cover {Action}. BookId={BookId}", action, book.Id);
        await _audit.LogAsync("Information", action, "Book", book.BookCode,
            $"{(isReplace ? "Thay" : "Tải lên")} ảnh bìa cho sách '{book.Title}' (Id={book.Id}).");

        return OperationResult.Ok();
    }

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var c = await _bookRepository.GetCountsAsync(_settings.LowStockThreshold);
        return new DashboardViewModel
        {
            TotalBooks = c.Total,
            ActiveBooks = c.Active,
            DeletedBooks = c.Deleted,
            LowStockBooks = c.LowStock,
            CreatedTodayBooks = c.CreatedToday,
            LogsToday = await _audit.CountTodayAsync()
        };
    }

    private bool TrySetOriginalRowVersion(Book book, string rowVersionBase64)
    {
        try
        {
            var bytes = Convert.FromBase64String(rowVersionBase64);
            _bookRepository.SetOriginalRowVersion(book, bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
