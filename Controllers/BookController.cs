using AspLab06Final.Mvc.Services;
using AspLab06Final.Mvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AspLab06Final.Mvc.Controllers;

// Lab06: policy mặc định cho cả controller là "CanViewBook" (Admin, Staff, User đều xem được danh mục sách).
// Các action quản lý (Create/Edit/Delete/Restore/Trash) và các action nhạy cảm hơn (AdjustStock, UploadImage)
// override bằng policy riêng, hẹp hơn - xem chú thích tại từng action.
[Authorize(Policy = "CanViewBook")]
public class BooksController : Controller
{
    private readonly IBookService _bookService;
    private readonly IGenreService _genreService;

    public BooksController(IBookService bookService, IGenreService genreService)
    {
        _bookService = bookService;
        _genreService = genreService;
    }

    // GET /Books - danh sách sách đang hoạt động (IsDeleted = false)
    public async Task<IActionResult> Index()
    {
        var books = await _bookService.GetActiveListAsync();
        return View(books);
    }

    // GET /Books/Search?keyword=clean&stockStatus=low  (Lab05 Feature 1)
    public async Task<IActionResult> Search(string? keyword, string? stockStatus)
    {
        var model = await _bookService.SearchAsync(keyword, stockStatus);
        return View(model);
    }

    // GET /Books/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var book = await _bookService.GetDetailAsync(id);
        if (book == null) return NotFound($"Không tìm thấy sách có id = {id}");
        return View(book);
    }

    // GET /Books/Create - chỉ Admin được quản lý danh mục sách (CanManageBook)
    [Authorize(Policy = "CanManageBook")]
    public async Task<IActionResult> Create()
    {
        await PopulateGenresAsync();
        return View(new BookCreateViewModel());
    }

    // POST /Books/Create
    [Authorize(Policy = "CanManageBook")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateGenresAsync();
            return View(model);
        }

        var result = await _bookService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(result.ErrorKey, result.ErrorMessage);
            await PopulateGenresAsync();
            return View(model);
        }

        TempData["Success"] = "Đã thêm sách thành công.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Books/Edit/5
    [Authorize(Policy = "CanManageBook")]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _bookService.GetEditModelAsync(id);
        if (model == null) return NotFound($"Không tìm thấy sách có id = {id}");
        await PopulateGenresAsync();
        return View(model);
    }

    // POST /Books/Edit/5
    [Authorize(Policy = "CanManageBook")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookEditViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await PopulateGenresAsync();
            return View(model);
        }

        var result = await _bookService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(result.ErrorKey, result.ErrorMessage);
            await PopulateGenresAsync();
            return View(model);
        }

        TempData["Success"] = "Đã cập nhật sách thành công.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Books/Delete/5 - trang xác nhận xóa
    [Authorize(Policy = "CanManageBook")]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _bookService.GetDeleteModelAsync(id);
        if (model == null) return NotFound($"Không tìm thấy sách có id = {id}");
        return View(model);
    }

    // POST /Books/Delete/5 - xóa mềm
    [Authorize(Policy = "CanManageBook")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var result = await _bookService.SoftDeleteAsync(id);
        TempData[result.Success ? "Success" : "Error"] =
            result.Success ? "Đã xóa mềm sách (chuyển vào Thùng rác)." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    // GET /Books/Trash - danh sách đã xóa mềm
    [Authorize(Policy = "CanManageBook")]
    public async Task<IActionResult> Trash()
    {
        var model = await _bookService.GetTrashAsync();
        return View(model);
    }

    // POST /Books/Restore/5
    [Authorize(Policy = "CanManageBook")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var result = await _bookService.RestoreAsync(id);
        TempData[result.Success ? "Success" : "Error"] =
            result.Success ? "Đã khôi phục sách." : result.ErrorMessage;
        return RedirectToAction(nameof(Trash));
    }

    // GET /Books/AdjustStock/5  (Lab05 Feature 2 / Lab06 Feature 1: policy riêng CanAdjustStock, Admin + Staff)
    [Authorize(Policy = "CanAdjustStock")]
    public async Task<IActionResult> AdjustStock(int id)
    {
        var model = await _bookService.GetAdjustStockModelAsync(id);
        if (model == null) return NotFound($"Không tìm thấy sách có id = {id}");
        return View(model);
    }

    // POST /Books/AdjustStock/5
    [Authorize(Policy = "CanAdjustStock")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(int id, BookAdjustStockViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var result = await _bookService.AdjustStockAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(result.ErrorKey, result.ErrorMessage);
            return View(model);
        }

        TempData["Success"] = "Đã điều chỉnh tồn kho.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Books/UploadImage/5 - Lab06 Feature 2: chỉ Admin được upload/thay ảnh bìa sách
    [Authorize(Policy = "CanUploadBookImage")]
    public async Task<IActionResult> UploadImage(int id)
    {
        var model = await _bookService.GetImageUploadModelAsync(id);
        if (model == null) return NotFound($"Không tìm thấy sách có id = {id}");
        return View(model);
    }

    // POST /Books/UploadImage/5
    [Authorize(Policy = "CanUploadBookImage")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(int id, BookImageUploadViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var result = await _bookService.UploadCoverImageAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(result.ErrorKey, result.ErrorMessage);
            // Nạp lại ảnh hiện tại để hiển thị đúng (ảnh cũ vẫn còn nguyên vì upload thất bại).
            var reload = await _bookService.GetImageUploadModelAsync(id);
            if (reload != null) model.CurrentCoverImageUrl = reload.CurrentCoverImageUrl;
            return View(model);
        }

        TempData["Success"] = "Đã cập nhật ảnh bìa sách.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task PopulateGenresAsync()
    {
        var genres = await _genreService.GetGenreOptionsAsync();
        ViewBag.Genres = new SelectList(genres, nameof(GenreListItemViewModel.Id),
            nameof(GenreListItemViewModel.Name));
    }
}
