using AspLab06Final.Mvc.Data;
using AspLab06Final.Mvc.Options;
using AspLab06Final.Mvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AspLab06Final.Mvc.Controllers;

// Không gắn [Authorize] tường minh: controller này thừa hưởng FallbackPolicy toàn cục
// (RequireAuthenticatedUser, cấu hình trong Program.cs) - tức là mặc định vẫn cần đăng nhập,
// GIỐNG NHƯ đã gắn [Authorize] ở mọi action, chỉ khác là không phải lặp lại thủ công.
public class HomeController : Controller
{
    private readonly AppSettings _settings;
    private readonly IBookService _bookService;
    private readonly IAuditLogService _auditLogService;

    public HomeController(IOptions<AppSettings> options, IBookService bookService, IAuditLogService auditLogService)
    {
        _settings = options.Value;
        _bookService = bookService;
        _auditLogService = auditLogService;
    }

    // GET / - Dashboard tổng quan (yêu cầu đăng nhập - xem FallbackPolicy)
    public async Task<IActionResult> Index()
    {
        ViewData["AppName"] = _settings.AppName;
        ViewData["SupportEmail"] = _settings.SupportEmail;
        var model = await _bookService.GetDashboardAsync();

        // Lab06 Feature 3: chỉ nạp số liệu Security Dashboard nếu người xem có quyền CanViewAuditLog (Admin).
        // Tương đương User.IsInRole("Admin") vì policy "CanViewAuditLog" hiện chỉ yêu cầu role Admin (xem Program.cs).
        if (User.IsInRole(DbInitializer.Roles.Admin))
        {
            model.Security = await _auditLogService.GetSecurityDashboardAsync();
        }

        return View(model);
    }

    // Trang mặc định của template ASP.NET Core MVC - giữ lại cho đầy đủ, không phải trọng tâm của Lab06.
    [AllowAnonymous]
    public IActionResult Privacy() => View();

    // Trang lỗi phải luôn truy cập được kể cả khi chưa đăng nhập (tránh vòng lặp redirect Login <-> Error).
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["TraceId"] = HttpContext.TraceIdentifier;
        return View();
    }

    [AllowAnonymous]
    [ActionName("StatusCode")]
    public IActionResult StatusCodePage(int code)
    {
        ViewData["Code"] = code;
        return View("StatusCode");
    }
}
