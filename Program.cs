using System.Text.Json;
using AspLab06Final.Mvc.Data;
using AspLab06Final.Mvc.Models;
using AspLab06Final.Mvc.Options;
using AspLab06Final.Mvc.Repositories;
using AspLab06Final.Mvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog (structured log -> Console + File rolling theo ngày) ----------
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/lab05-.txt", rollingInterval: RollingInterval.Day));

builder.Services.AddControllersWithViews();

// Cần cho AuditLogService/SaleService lấy được User/IP của request hiện tại từ bên trong Service layer.
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Identity: Cookie-based auth + Role-based Authorization (Lab06) ----------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Yêu cầu mật khẩu vừa đủ cho môi trường học tập (không quá khắt khe nhưng vẫn có ràng buộc thật).
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = true;

    // Khoá tạm tài khoản sau nhiều lần đăng nhập sai - chống brute-force cơ bản.
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true; // chặn JavaScript đọc cookie phiên -> giảm rủi ro XSS đánh cắp session
    options.Cookie.SameSite = SameSiteMode.Lax; // phòng thủ CSRF theo chiều sâu, bổ sung cho anti-forgery token
});

builder.Services.AddAuthorization(options =>
{
    // Secure by default: MỌI endpoint (MVC action lẫn Minimal API) đều yêu cầu đăng nhập
    // trừ khi được đánh dấu [AllowAnonymous] / .AllowAnonymous() một cách tường minh.
    // Nhờ vậy nếu lỡ quên gắn [Authorize] ở 1 action mới, action đó KHÔNG bị lộ ra ngoài cho anonymous.
    options.FallbackPolicy = options.DefaultPolicy;

    // Các Policy có tên rõ nghĩa (đặt tên theo NGHIỆP VỤ, không đặt theo role) - dễ đọc & dễ đổi luật sau này.
    options.AddPolicy("CanViewBook", policy => policy.RequireRole("Admin", "Staff", "User"));
    options.AddPolicy("CanManageBook", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanAdjustStock", policy => policy.RequireRole("Admin", "Staff")); // Feature 1
    options.AddPolicy("CanUploadBookImage", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanViewAuditLog", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanRecordSale", policy => policy.RequireRole("Admin", "Staff"));
});

// DI: Repository + Service (Controller -> Service -> Repository -> DbContext)
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();

builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ISaleService, SaleService>();
builder.Services.AddSingleton<IFileUploadService, FileUploadService>();

// ---------- ProblemDetails: thêm traceId + timestamp cho mọi response lỗi ----------
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
    };
});

// ---------- Health Checks: live (self) + ready (database) ----------
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("Application is running."), tags: new[] { "live" })
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "ready" });

var app = builder.Build();

// Áp dụng migration + seed role/tài khoản demo (Admin/Staff/User)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbInitializer.SeedIdentityAsync(scope.ServiceProvider);
}

// ---------- Exception handling: khác nhau giữa Development và Production ----------
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // dev: xem chi tiết lỗi để debug
}
else
{
    app.UseExceptionHandler("/Home/Error"); // prod: trang lỗi an toàn, không lộ stack trace
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// QUAN TRỌNG: Authentication phải nằm TRƯỚC Authorization ("xác định anh là ai" trước "anh có được phép không").
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---------- Health endpoints (luôn công khai - hệ thống giám sát/load balancer không đăng nhập được) ----------
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
}).AllowAnonymous();

// /health/ready: JSON gồm status tổng, danh sách checks + mô tả ngắn
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description ?? e.Value.Exception?.Message ?? "OK"
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { WriteIndented = true }));
    }
}).AllowAnonymous();

// ---------- Minimal API: trả ProblemDetails chuẩn khi không tìm thấy ----------
// Thiết kế có chủ đích: API tra cứu sách để công khai (AllowAnonymous), giống một API thông tin sản phẩm
// thông thường - KHÔNG áp dụng FallbackPolicy (đăng nhập bằng cookie) cho các endpoint API dạng này.
app.MapGet("/api/products/{id:int}", async (int id, AppDbContext db, HttpContext http,
    ILogger<Program> logger) =>
{
    var book = await db.Books.AsNoTracking()
        .Select(b => new { b.Id, b.BookCode, b.Title, b.Author, b.Price, b.Quantity })
        .FirstOrDefaultAsync(b => b.Id == id);

    if (book == null)
    {
        logger.LogWarning("API book not found. BookId={BookId}", id);
        return Results.Problem(
            type: "https://bookstore.example/problems/book-not-found",
            title: "Book not found",
            detail: $"The book with id {id} was not found.",
            statusCode: StatusCodes.Status404NotFound,
            instance: http.Request.Path,
            extensions: new Dictionary<string, object?> { ["errorCode"] = "BOOK_NOT_FOUND" });
    }
    return Results.Ok(book);
}).AllowAnonymous();

// Alias /api/books/{id} cho đúng nghiệp vụ Bookstore
app.MapGet("/api/books/{id:int}", async (int id, AppDbContext db, HttpContext http,
    ILogger<Program> logger) =>
{
    var book = await db.Books.AsNoTracking()
        .Select(b => new { b.Id, b.BookCode, b.Title, b.Author, b.Price, b.Quantity })
        .FirstOrDefaultAsync(b => b.Id == id);

    if (book == null)
    {
        logger.LogWarning("API book not found. BookId={BookId}", id);
        return Results.Problem(
            type: "https://bookstore.example/problems/book-not-found",
            title: "Book not found",
            detail: $"The book with id {id} was not found.",
            statusCode: StatusCodes.Status404NotFound,
            instance: http.Request.Path,
            extensions: new Dictionary<string, object?> { ["errorCode"] = "BOOK_NOT_FOUND" });
    }
    return Results.Ok(book);
}).AllowAnonymous();

// ---------- Bonus: API search + ValidationProblemDetails (Lab06 Feature khuyến khích) ----------
app.MapGet("/api/books/search", async (string? keyword, AppDbContext db, HttpContext http) =>
{
    if (string.IsNullOrWhiteSpace(keyword) || keyword.Trim().Length > 100)
    {
        var errors = new Dictionary<string, string[]>
        {
            ["keyword"] = new[] { "Từ khoá tìm kiếm không được để trống và tối đa 100 ký tự." }
        };
        return Results.ValidationProblem(errors,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid search keyword",
            type: "https://bookstore.example/problems/invalid-search",
            instance: http.Request.Path,
            extensions: new Dictionary<string, object?> { ["errorCode"] = "INVALID_SEARCH_KEYWORD" });
    }

    var k = keyword.Trim();
    // LINQ tham số hoá qua EF Core - an toàn trước SQL injection dù input chứa ký tự đặc biệt (', --, ; ...).
    var books = await db.Books.AsNoTracking()
        .Where(b => b.Title.Contains(k) || b.Author.Contains(k) || b.BookCode.Contains(k))
        .Select(b => new { b.Id, b.BookCode, b.Title, b.Author, b.Price, b.Quantity })
        .Take(20)
        .ToListAsync();

    if (books.Count == 0)
    {
        return Results.Problem(
            type: "https://bookstore.example/problems/book-search-empty",
            title: "No books found",
            detail: "Không tìm thấy sách nào khớp với từ khoá đã cho.",
            statusCode: StatusCodes.Status404NotFound,
            instance: http.Request.Path,
            extensions: new Dictionary<string, object?> { ["errorCode"] = "BOOK_SEARCH_EMPTY" });
    }

    return Results.Ok(books);
}).AllowAnonymous();

app.Run();
