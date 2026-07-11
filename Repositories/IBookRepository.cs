using AspLab06Final.Mvc.Models;

namespace AspLab06Final.Mvc.Repositories;

public interface IBookRepository
{
    // Đọc danh sách đang hoạt động (query filter tự loại bản ghi đã soft delete)
    Task<List<Book>> GetActiveListAsync();
    Task<List<Book>> SearchActiveAsync(string? keyword, string? stockStatus, int lowStockThreshold);

    // Đọc 1 bản ghi
    Task<Book?> GetByIdReadOnlyAsync(int id);   // AsNoTracking (Detail)
    Task<Book?> GetByIdAsync(int id);           // Tracking (Edit / Delete / AdjustStock)

    // Trash / Restore (cần IgnoreQueryFilters để thấy bản ghi đã xóa mềm)
    Task<List<Book>> GetTrashAsync();
    Task<Book?> GetDeletedByIdAsync(int id);

    // Kiểm tra mã nghiệp vụ trùng (tính cả bản ghi đã soft delete)
    Task<bool> BookCodeExistsAsync(string bookCode, int? excludeId = null);

    Task AddAsync(Book book);

    // Gán OriginalValue cho RowVersion để EF đưa vào WHERE khi UPDATE (concurrency)
    void SetOriginalRowVersion(Book book, byte[] rowVersion);

    // Bỏ theo dõi entity (dùng sau khi gặp concurrency conflict để không lưu lại thay đổi lỗi)
    void Detach(Book book);

    Task SaveChangesAsync();

    // Số liệu cho Dashboard
    Task<DashboardCounts> GetCountsAsync(int lowStockThreshold);
}

public record DashboardCounts(int Total, int Active, int Deleted, int LowStock, int CreatedToday);