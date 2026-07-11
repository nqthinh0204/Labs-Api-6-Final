using AspLab06Final.Mvc.Data;
using AspLab06Final.Mvc.Models;
using AspLab06Final.Mvc.Services;
using AspLab06Final.Mvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AspLab06Final.Mvc.Controllers;

// Toàn bộ action ở đây được phép truy cập ẩn danh (đây chính là "cửa vào" của hệ thống).
// Các trang còn lại của ứng dụng mặc định yêu cầu đăng nhập (xem FallbackPolicy trong Program.cs).
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _audit;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogService audit,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // UserName == Email đối với mọi tài khoản trong hệ thống (xem DbInitializer / Register)
        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in. Email={Email}", model.Email);
            await _audit.LogAsync("Information", "Login", "Auth", model.Email, $"Đăng nhập thành công: {model.Email}.");

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account locked out after repeated failed logins. Email={Email}", model.Email);
            await _audit.LogAsync("Warning", "Login", "Auth", model.Email,
                $"Tài khoản {model.Email} tạm thời bị khoá do đăng nhập sai nhiều lần liên tiếp.", "Failed");
            ModelState.AddModelError("", "Tài khoản tạm thời bị khoá do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau.");
            return View(model);
        }

        // Cố tình dùng thông báo lỗi CHUNG CHUNG (không nói rõ "sai email" hay "sai mật khẩu")
        // để tránh lộ thông tin email nào đã tồn tại trong hệ thống (user enumeration).
        _logger.LogWarning("Invalid login attempt. Email={Email}", model.Email);
        await _audit.LogAsync("Warning", "Login", "Auth", model.Email,
            $"Đăng nhập thất bại (sai email hoặc mật khẩu): {model.Email}.", "Failed");
        ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true // môi trường học tập: bỏ qua bước xác nhận email qua mail thật
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // QUAN TRỌNG: tài khoản tự đăng ký LUÔN chỉ được gán role "User" (thấp quyền nhất).
            // Không bao giờ cho phép người dùng tự chọn/tự cấp role Admin hoặc Staff qua form đăng ký công khai này -
            // việc gán role Admin/Staff chỉ được thực hiện thủ công (seed data) hoặc qua công cụ quản trị riêng.
            await _userManager.AddToRoleAsync(user, DbInitializer.Roles.User);

            _logger.LogInformation("New user registered. Email={Email}", model.Email);
            await _audit.LogAsync("Information", "Register", "Auth", model.Email, $"Tài khoản mới đăng ký: {model.Email}.");

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }
        await _audit.LogAsync("Warning", "Register", "Auth", model.Email, $"Đăng ký thất bại cho email {model.Email}.", "Failed");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name;
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out. Email={Email}", userName);
        await _audit.LogAsync("Information", "Logout", "Auth", userName, $"Đăng xuất: {userName}.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> AccessDenied(string? returnUrl = null)
    {
        // Ghi log NGAY TẠI ĐÂY: đây là nơi duy nhất mọi request bị AuthorizationMiddleware từ chối sẽ đi qua,
        // nên đây là điểm tập trung tốt nhất để đếm "AccessDeniedToday" cho Security Dashboard (Feature 3).
        await _audit.LogAsync("Warning", "AccessDenied", "Auth", returnUrl,
            $"Người dùng '{User.Identity?.Name ?? "(ẩn danh)"}' bị từ chối truy cập vào: {returnUrl ?? "(không rõ)"}.", "Denied");
        return View();
    }
}
