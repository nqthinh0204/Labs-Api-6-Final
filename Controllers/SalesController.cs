using AspLab06Final.Mvc.Services;
using AspLab06Final.Mvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspLab06Final.Mvc.Controllers;

// Lab06 - Policy "CanRecordSale": Admin + Staff được tạo/xem đơn bán sách (nghiệp vụ vận hành hàng ngày).
// Khác với "CanManageBook" (chỉ Admin) vốn dùng để quản lý DANH MỤC sách (giá, mô tả, xoá/khôi phục).
[Authorize(Policy = "CanRecordSale")]
public class SalesController : Controller
{
    private readonly ISaleService _saleService;

    public SalesController(ISaleService saleService)
    {
        _saleService = saleService;
    }

    // GET /Sales - lịch sử các đơn đã bán
    public async Task<IActionResult> Index()
    {
        var sales = await _saleService.GetHistoryAsync();
        return View(sales);
    }

    // GET /Sales/Create
    public async Task<IActionResult> Create()
    {
        var model = await _saleService.GetCreateModelAsync();
        return View(model);
    }

    // POST /Sales/Create - luồng nghiệp vụ nhiều bước có transaction (xem SaleService.CreateSaleAsync)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleCreateViewModel model)
    {
        var result = await _saleService.CreateSaleAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(result.ErrorKey, result.ErrorMessage);

            // Nạp lại tồn kho/giá MỚI NHẤT từ DB, nhưng vẫn giữ số lượng người dùng vừa nhập
            // để họ không phải gõ lại toàn bộ form sau khi sửa lỗi.
            var fresh = await _saleService.GetCreateModelAsync();
            var entered = model.Lines.ToDictionary(l => l.BookId, l => l.QuantityToSell);
            foreach (var line in fresh.Lines)
            {
                if (entered.TryGetValue(line.BookId, out var qty))
                {
                    line.QuantityToSell = qty;
                }
            }
            return View(fresh);
        }

        TempData["SuccessMessage"] = "Tạo đơn bán sách thành công.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Sales/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var sale = await _saleService.GetDetailAsync(id);
        if (sale == null)
        {
            return NotFound();
        }
        return View(sale);
    }
}
