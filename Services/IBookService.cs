using AspLab06Final.Mvc.ViewModels;

namespace AspLab06Final.Mvc.Services;

// Kết quả thao tác nghiệp vụ trả về cho Controller.
// ErrorKey = tên field để gắn vào ModelState (rỗng = lỗi chung).
public record OperationResult(bool Success, string ErrorKey = "", string ErrorMessage = "")
{
    public static OperationResult Ok() => new(true);
    public static OperationResult Fail(string key, string message) => new(false, key, message);
}

public interface IBookService
{
    Task<List<BookListItemViewModel>> GetActiveListAsync();
    Task<BookSearchViewModel> SearchAsync(string? keyword, string? stockStatus);
    Task<BookDetailViewModel?> GetDetailAsync(int id);

    Task<OperationResult> CreateAsync(BookCreateViewModel model);

    Task<BookEditViewModel?> GetEditModelAsync(int id);
    Task<OperationResult> UpdateAsync(BookEditViewModel model);

    Task<BookDetailViewModel?> GetDeleteModelAsync(int id);
    Task<OperationResult> SoftDeleteAsync(int id);

    Task<List<BookTrashItemViewModel>> GetTrashAsync();
    Task<OperationResult> RestoreAsync(int id);

    Task<BookAdjustStockViewModel?> GetAdjustStockModelAsync(int id);
    Task<OperationResult> AdjustStockAsync(BookAdjustStockViewModel model);

    // Lab06 Feature 2 - Upload/thay ảnh bìa sách an toàn
    Task<BookImageUploadViewModel?> GetImageUploadModelAsync(int id);
    Task<OperationResult> UploadCoverImageAsync(BookImageUploadViewModel model);

    Task<DashboardViewModel> GetDashboardAsync();
}