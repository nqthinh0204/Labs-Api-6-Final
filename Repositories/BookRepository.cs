using AspLab06Final.Mvc.Data;
using AspLab06Final.Mvc.Models;
using Microsoft.EntityFrameworkCore;

namespace AspLab06Final.Mvc.Repositories;

public class BookRepository : IBookRepository
{
    private readonly AppDbContext _context;

    public BookRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Book>> GetActiveListAsync()
        => _context.Books
            .Include(b => b.Genre)
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<List<Book>> SearchActiveAsync(string? keyword, string? stockStatus, int lowStockThreshold)
    {
        // Query filter !IsDeleted vẫn áp dụng -> chỉ trả dữ liệu đang hoạt động.
        var query = _context.Books.Include(b => b.Genre).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            // LINQ tham số hoá -> an toàn, không nối chuỗi SQL từ input người dùng
            query = query.Where(b => b.Title.Contains(k)
                                  || b.Author.Contains(k)
                                  || b.BookCode.Contains(k));
        }

        query = stockStatus switch
        {
            "out" => query.Where(b => b.Quantity == 0),
            "low" => query.Where(b => b.Quantity > 0 && b.Quantity <= lowStockThreshold),
            "in" => query.Where(b => b.Quantity > lowStockThreshold),
            _ => query
        };

        return await query.OrderBy(b => b.Title).ToListAsync();
    }

    public Task<Book?> GetByIdReadOnlyAsync(int id)
        => _context.Books.Include(b => b.Genre).AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);

    public Task<Book?> GetByIdAsync(int id)
        => _context.Books.FirstOrDefaultAsync(b => b.Id == id);

    public Task<List<Book>> GetTrashAsync()
        => _context.Books
            .IgnoreQueryFilters()
            .Where(b => b.IsDeleted)
            .AsNoTracking()
            .OrderByDescending(b => b.DeletedAt)
            .ToListAsync();

    public Task<Book?> GetDeletedByIdAsync(int id)
        => _context.Books
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id && b.IsDeleted);

    public Task<bool> BookCodeExistsAsync(string bookCode, int? excludeId = null)
        => _context.Books
            .IgnoreQueryFilters() // mã phải duy nhất cả với bản ghi đã xóa mềm
            .AnyAsync(b => b.BookCode == bookCode && (excludeId == null || b.Id != excludeId));

    public async Task AddAsync(Book book)
        => await _context.Books.AddAsync(book);

    public void SetOriginalRowVersion(Book book, byte[] rowVersion)
        => _context.Entry(book).Property(b => b.RowVersion).OriginalValue = rowVersion;

    public void Detach(Book book)
        => _context.Entry(book).State = EntityState.Detached;

    public Task SaveChangesAsync()
        => _context.SaveChangesAsync();

    public async Task<DashboardCounts> GetCountsAsync(int lowStockThreshold)
    {
        var today = DateTime.Now.Date;
        var total = await _context.Books.IgnoreQueryFilters().CountAsync();
        var active = await _context.Books.CountAsync();
        var deleted = await _context.Books.IgnoreQueryFilters().CountAsync(b => b.IsDeleted);
        var lowStock = await _context.Books.CountAsync(b => b.Quantity <= lowStockThreshold);
        var createdToday = await _context.Books.CountAsync(b => b.CreatedAt >= today);
        return new DashboardCounts(total, active, deleted, lowStock, createdToday);
    }
}