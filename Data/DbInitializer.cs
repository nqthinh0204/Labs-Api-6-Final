using AspLab06Final.Mvc.Models;
using Microsoft.AspNetCore.Identity;

namespace AspLab06Final.Mvc.Data;

// Lab06 - Seed 3 role (Admin/Staff/User) và 3 tài khoản demo tương ứng.
// Chạy 1 lần khi ứng dụng khởi động (idempotent: kiểm tra tồn tại trước khi tạo).
public static class DbInitializer
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Staff = "Staff";
        public const string User = "User";
    }

    public static async Task SeedIdentityAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        foreach (var roleName in new[] { Roles.Admin, Roles.Staff, Roles.User })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation("Đã tạo role {Role}", roleName);
            }
        }

        await EnsureUserAsync(userManager, logger,
            email: "admin@bookstore.test",
            fullName: "Quản Trị Viên",
            password: "Admin@123",
            role: Roles.Admin);

        await EnsureUserAsync(userManager, logger,
            email: "staff@bookstore.test",
            fullName: "Nhân Viên Bán Hàng",
            password: "Staff@123",
            role: Roles.Staff);

        await EnsureUserAsync(userManager, logger,
            email: "user@bookstore.test",
            fullName: "Khách Hàng Thường",
            password: "User@123",
            role: Roles.User);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email,
        string fullName,
        string password,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true // môi trường demo/học tập: bỏ qua bước xác nhận email qua mail thật
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, role);
            logger.LogInformation("Đã seed tài khoản {Email} với role {Role}", email, role);
        }
        else
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Không thể seed tài khoản {Email}: {Errors}", email, errors);
        }
    }
}
