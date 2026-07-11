using AspLab06Final.Mvc.Data;
using AspLab06Final.Mvc.Models;
using Microsoft.EntityFrameworkCore;

namespace AspLab06Final.Mvc.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAndSaveAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }

    public Task<List<AuditLog>> GetRecentAsync(int take = 100)
        => _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();

    public Task<int> CountTodayAsync()
    {
        var today = DateTime.Now.Date;
        return _context.AuditLogs.CountAsync(a => a.CreatedAt >= today);
    }

    // Lab06 Feature 3: tìm kiếm/lọc log theo user, action, result, khoảng ngày.
    // Toàn bộ điều kiện dùng LINQ (EF Core tự tham số hoá) - không nối chuỗi SQL từ input người dùng.
    public Task<List<AuditLog>> SearchAsync(string? userName, string? action, string? result, DateTime? fromDate, DateTime? toDate, int take = 200)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var u = userName.Trim();
            query = query.Where(a => a.UserName != null && a.UserName.Contains(u));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(result))
        {
            query = query.Where(a => a.Result == result);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            var toDateExclusive = toDate.Value.Date.AddDays(1); // lấy trọn ngày ToDate
            query = query.Where(a => a.CreatedAt < toDateExclusive);
        }

        return query.OrderByDescending(a => a.CreatedAt).Take(take).ToListAsync();
    }

    public Task<int> CountTodayByResultAsync(string result)
    {
        var today = DateTime.Now.Date;
        return _context.AuditLogs.CountAsync(a => a.CreatedAt >= today && a.Result == result);
    }

    public Task<int> CountTodayByActionsAsync(IReadOnlyCollection<string> actions)
    {
        var today = DateTime.Now.Date;
        return _context.AuditLogs.CountAsync(a => a.CreatedAt >= today && actions.Contains(a.Action));
    }
}