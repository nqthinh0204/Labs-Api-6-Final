using AspLab06Final.Mvc.Services;
using AspLab06Final.Mvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspLab06Final.Mvc.Controllers;

// Lab06 - chỉ Admin được xem Audit Log (dữ liệu nhạy cảm: ai đã làm gì, từ IP nào).
[Authorize(Policy = "CanViewAuditLog")]
public class AuditLogsController : Controller
{
    private readonly IAuditLogService _audit;

    public AuditLogsController(IAuditLogService audit)
    {
        _audit = audit;
    }

    // GET /AuditLogs - 100 log gần nhất
    public async Task<IActionResult> Index()
    {
        var logs = await _audit.GetRecentAsync(100);
        return View(logs);
    }

    // GET /AuditLogs/Search - Lab06 Feature 3: lọc theo user / action / result / khoảng ngày
    public async Task<IActionResult> Search(AuditLogSearchViewModel filter)
    {
        // Trang load lần đầu (chưa bấm Tìm kiếm): không có tham số nào -> chỉ hiển thị form trống.
        var hasAnyFilter = !string.IsNullOrWhiteSpace(filter.UserName)
            || !string.IsNullOrWhiteSpace(filter.Action)
            || !string.IsNullOrWhiteSpace(filter.Result)
            || filter.FromDate.HasValue
            || filter.ToDate.HasValue;

        if (!hasAnyFilter)
        {
            return View(filter);
        }

        var result = await _audit.SearchAsync(filter);
        return View(result);
    }
}
