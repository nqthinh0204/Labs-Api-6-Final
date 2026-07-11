using AspLab06Final.Mvc.Models;

namespace AspLab06Final.Mvc.Repositories;

public interface IAuditLogRepository
{
    Task AddAndSaveAsync(AuditLog log);
    Task<List<AuditLog>> GetRecentAsync(int take = 100);
    Task<int> CountTodayAsync();

    // Lab06 Feature 3 - Audit Log Search + Security Dashboard
    Task<List<AuditLog>> SearchAsync(string? userName, string? action, string? result, DateTime? fromDate, DateTime? toDate, int take = 200);
    Task<int> CountTodayByResultAsync(string result);
    Task<int> CountTodayByActionsAsync(IReadOnlyCollection<string> actions);
}