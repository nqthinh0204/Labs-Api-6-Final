# Secure Mini Bookstore MVC — Lab06 (Final)

ASP.NET Core MVC (.NET 10) + EF Core (SQLite) + ASP.NET Core Identity.
Chủ đề: **Bookstore** (Sách / Thể loại / Đơn bán sách), kế thừa toàn bộ Lab05 (CRUD, Soft Delete, Search, AdjustStock, Audit Log, Health Check, API).

## ⚠️ BƯỚC BẮT BUỘC TRƯỚC KHI CHẠY (đọc trước!)

Model đã thay đổi nhiều so với Lab05 (thêm Identity, thêm cột `CoverImageUrl`/`UserName`/`IpAddress`/`Result`,
thêm bảng `Sales`/`SaleItems`). Migration mới **chưa được tạo sẵn** trong bộ code này —
bạn cần tự chạy lệnh `dotnet ef migrations add` trên máy của bạn (xem lý do ở mục "Ghi chú" cuối file).

```bash
cd AspLab06Final.Mvc
dotnet restore
dotnet ef migrations add Lab06IdentitySecurityFinal
dotnet ef database update
dotnet run
```

Nếu bạn đã có sẵn file `.db` cũ từ Lab05, hãy xoá nó trước khi chạy `database update` để tránh xung đột dữ liệu cũ:
```bash
rm -f *.db bookstore*.db 2>/dev/null
```

Sau khi `dotnet run`, mở `https://localhost:xxxx` (xem cổng trong `Properties/launchSettings.json`).

## Tài khoản demo (seed sẵn khi chạy lần đầu)

| Role  | Email                 | Mật khẩu   | Quyền chính                                              |
|-------|-----------------------|------------|-----------------------------------------------------------|
| Admin | admin@bookstore.test  | Admin@123  | Toàn quyền: CRUD sách, xoá mềm/khôi phục, upload ảnh, Audit Log |
| Staff | staff@bookstore.test  | Staff@123  | Xem sách, điều chỉnh tồn kho, tạo/xem đơn bán sách        |
| User  | user@bookstore.test   | User@123   | Chỉ xem danh mục sách/thể loại                            |

Cũng có thể tự đăng ký tài khoản mới tại `/Account/Register` — tài khoản tự đăng ký luôn được gán role **User**.

## Kiến trúc

```
Controller -> Service -> Repository -> AppDbContext (SQLite)
```

- **Models**: Book, Genre, AuditLog, Sale, SaleItem, ApplicationUser (IdentityUser)
- **Identity**: Cookie authentication, 3 role (Admin/Staff/User), khoá tài khoản sau 5 lần đăng nhập sai
- **Authorization Policy** (đặt tên theo nghiệp vụ, cấu hình tại `Program.cs`):
  - `CanViewBook` (Admin, Staff, User) — xem danh mục sách
  - `CanManageBook` (Admin) — tạo/sửa/xoá mềm/khôi phục sách
  - `CanAdjustStock` (Admin, Staff) — điều chỉnh tồn kho (Feature 1)
  - `CanUploadBookImage` (Admin) — upload/thay ảnh bìa (Feature 2)
  - `CanViewAuditLog` (Admin) — xem Audit Log & Security Dashboard (Feature 3)
  - `CanRecordSale` (Admin, Staff) — tạo/xem đơn bán sách (luồng transaction)
  - Toàn bộ endpoint còn lại mặc định yêu cầu đăng nhập qua `FallbackPolicy` (secure-by-default)

## Tính năng chính (Câu 1)

- Đăng ký / đăng nhập / đăng xuất, phân quyền theo role
- CRUD Sách (Controller → Service → Repository), soft delete + Thùng rác + khôi phục
- Concurrency control bằng RowVersion (chống mất dữ liệu khi 2 người sửa cùng lúc)
- Audit Log tự động ghi: ai (UserName) - từ đâu (IpAddress) - làm gì (Action) - kết quả (Result)
- Upload ảnh bìa sách an toàn: whitelist đuôi file, giới hạn dung lượng, kiểm tra magic bytes, tên file GUID
- Luồng nghiệp vụ nhiều bước có transaction: **Bán Sách** (`/Sales/Create`) — kiểm tra & trừ tồn kho nhiều dòng sách
  + tạo đơn bán, tất cả cùng thành công hoặc cùng rollback
- Health Check: `/health/live`, `/health/ready` (public, không cần đăng nhập)
- API: `/api/books/{id}`, `/api/products/{id}` (alias), `/api/books/search?keyword=` — trả `ProblemDetails`/`ValidationProblemDetails` chuẩn khi lỗi

## Tính năng mở rộng (Câu 2)

1. **Tách quyền tồn kho khỏi quyền quản lý**: Staff được `AdjustStock` (điều chỉnh nhanh) nhưng KHÔNG được sửa giá/xoá/khôi phục — 2 policy `CanAdjustStock` và `CanManageBook` tách biệt.
2. **Thay ảnh bìa an toàn**: file mới được lưu trước, DB chỉ trỏ sang ảnh mới sau khi lưu file thành công; ảnh cũ chỉ bị xoá sau khi DB cập nhật thành công. Nếu file mới không hợp lệ hoặc DB lỗi → ảnh cũ vẫn nguyên vẹn (xem `BookService.UploadCoverImageAsync`).
3. **Audit Log Search + Security Dashboard**: lọc log theo user/action/result/khoảng ngày (`/AuditLogs/Search`, AsNoTracking + LINQ); Dashboard hiển thị số AccessDenied / thao tác nhạy cảm / upload bị từ chối trong ngày (chỉ Admin thấy).
4. **Bonus**: API tìm kiếm sách `/api/books/search?keyword=` trả `ValidationProblemDetails` (400) khi keyword rỗng/quá dài, `ProblemDetails` (404) khi không có kết quả.

## Cấu trúc thư mục quan trọng

```
Controllers/   AccountController, BookController, GenreController, AuditLogController, SalesController, HomeController
Services/      BookService, GenreService, AuditLogService, SaleService, FileUploadService
Repositories/  BookRepository, GenreRepository, AuditLogRepository, SaleRepository
Models/        Book, Genre, AuditLog, Sale, SaleItem, ApplicationUser
ViewModels/    (tách riêng khỏi Entity - chống overposting)
Data/          AppDbContext (IdentityDbContext), DbInitializer (seed role/tài khoản)
Views/         Account, Books, Genres, AuditLogs, Sales, Home, Shared
wwwroot/uploads/books/   nơi lưu ảnh bìa sách upload lên (đã .gitignore nội dung, giữ .gitkeep)
```

## Ghi chú quan trọng

- Migration mới (`Lab06IdentitySecurityFinal`) **chưa được sinh sẵn** trong bộ code này vì môi trường soạn code
  không có quyền truy cập `nuget.org` để tải các gói EF Core Tools cần thiết cho việc build/generate migration.
  Lệnh `dotnet ef migrations add` cần được chạy trên máy của bạn (đã có sẵn NuGet đầy đủ) — xem hướng dẫn ở đầu file.
- Vì lý do trên, code **chưa được build/run thử** tại nơi soạn thảo. Toàn bộ đã được rà soát thủ công kỹ lưỡng
  (đối chiếu tên method/property giữa các lớp, cân bằng dấu ngoặc, chữ ký hàm...), nhưng bạn vẫn nên chạy
  `dotnet build` ngay sau khi migrate để chắc chắn không có lỗi phát sinh, rồi báo lại nếu gặp vấn đề.
- Nên `git commit` theo từng nhóm thay đổi nhỏ (Identity, Policy, Audit Log, Upload ảnh, Sales...) thay vì 1 commit lớn,
  đúng tinh thần "Definition of Done" của đề bài.
