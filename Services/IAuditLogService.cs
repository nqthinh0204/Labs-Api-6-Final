using AspLab06Final.Mvc.ViewModels;

namespace AspLab06Final.Mvc.Services;

public interface IAuditLogService
{
    // entity: "Book" | "Sale" | "Auth" ...; result: "Success" | "Denied" | "Failed" (mặc định "Success")
    // UserName và IpAddress KHÔNG cần truyền vào - service tự lấy từ HttpContext hiện tại để tránh bị giả mạo.
    Task LogAsync(string level, string action, string entity, string? entityKey, string message, string result = "Success");

    Task<List<AuditLogListItemViewModel>> GetRecentAsync(int take = 100);
    Task<int> CountTodayAsync();

    // Lab06 Feature 3
    Task<AuditLogSearchViewModel> SearchAsync(AuditLogSearchViewModel filter);
    Task<SecurityDashboardViewModel> GetSecurityDashboardAsync();
}