using AspLab06Final.Mvc.Models;
using AspLab06Final.Mvc.Repositories;
using AspLab06Final.Mvc.ViewModels;
using Microsoft.AspNetCore.Http;

namespace AspLab06Final.Mvc.Services;

public class AuditLogService : IAuditLogService
{
    // Các Action được xem là "thao tác nhạy cảm" (thay đổi dữ liệu nghiệp vụ) - dùng cho Security Dashboard (Feature 3)
    private static readonly string[] SensitiveActions =
    {
        "Create", "Edit", "SoftDelete", "Restore", "AdjustStock",
        "UploadBookImage", "ReplaceBookImage", "CreateSale"
    };

    private readonly IAuditLogRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(IAuditLogRepository repository, IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task LogAsync(string level, string action, string entity, string? entityKey, string message, string result = "Success")
    {
        // UserName / IpAddress lấy trực tiếp từ HttpContext hiện tại (không nhận từ tham số bên ngoài)
        // để đảm bảo log phản ánh đúng danh tính thật của request, không thể bị code gọi giả mạo.
        var httpContext = _httpContextAccessor.HttpContext;
        var userName = httpContext?.User?.Identity?.IsAuthenticated == true
            ? httpContext.User.Identity!.Name
            : null;
        var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();

        return _repository.AddAndSaveAsync(new AuditLog
        {
            Level = level,
            Action = action,
            Entity = entity,
            EntityKey = entityKey,
            Message = message,
            UserName = userName,
            IpAddress = ipAddress,
            Result = result,
            CreatedAt = DateTime.Now
        });
    }

    public async Task<List<AuditLogListItemViewModel>> GetRecentAsync(int take = 100)
    {
        var logs = await _repository.GetRecentAsync(take);
        return logs.Select(ToListItem).ToList();
    }

    public Task<int> CountTodayAsync() => _repository.CountTodayAsync();

    // Lab06 Feature 3 - tìm kiếm/lọc Audit Log
    public async Task<AuditLogSearchViewModel> SearchAsync(AuditLogSearchViewModel filter)
    {
        var logs = await _repository.SearchAsync(filter.UserName, filter.Action, filter.Result, filter.FromDate, filter.ToDate);
        filter.Results = logs.Select(ToListItem).ToList();
        filter.HasSearched = true;
        return filter;
    }

    // Lab06 Feature 3 - số liệu Security Dashboard
    public async Task<SecurityDashboardViewModel> GetSecurityDashboardAsync()
    {
        var accessDenied = await _repository.CountTodayByResultAsync("Denied");
        var sensitive = await _repository.CountTodayByActionsAsync(SensitiveActions);
        var rejectedUploads = await _repository.CountTodayByActionsAsync(new[] { "UploadRejected" });

        return new SecurityDashboardViewModel
        {
            AccessDeniedToday = accessDenied,
            SensitiveActionsToday = sensitive,
            RejectedUploadsToday = rejectedUploads
        };
    }

    private static AuditLogListItemViewModel ToListItem(AuditLog a) => new()
    {
        Id = a.Id,
        Level = a.Level,
        Action = a.Action,
        Entity = a.Entity,
        EntityKey = a.EntityKey,
        Message = a.Message,
        UserName = a.UserName,
        IpAddress = a.IpAddress,
        Result = a.Result,
        CreatedAt = a.CreatedAt
    };
}
