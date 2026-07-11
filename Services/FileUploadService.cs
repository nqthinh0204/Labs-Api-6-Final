using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace AspLab06Final.Mvc.Services;

// Lab06 - Secure File Upload cho ảnh bìa sách.
// 3 lớp phòng thủ độc lập: (1) whitelist extension, (2) giới hạn kích thước, (3) kiểm tra magic bytes thực tế của file
// (không tin FileName hay Content-Type do client tự khai báo, vì cả hai đều có thể bị giả mạo dễ dàng).
public class FileUploadService : IFileUploadService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

    private readonly string _uploadRootPath;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(IWebHostEnvironment env, ILogger<FileUploadService> logger)
    {
        _uploadRootPath = Path.Combine(env.WebRootPath, "uploads", "books");
        _logger = logger;
        Directory.CreateDirectory(_uploadRootPath);
    }

    public async Task<FileUploadResult> SaveBookCoverAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return new FileUploadResult(false, null, "Vui lòng chọn một file ảnh.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return new FileUploadResult(false, null, $"File vượt quá kích thước tối đa cho phép ({MaxFileSizeBytes / 1024 / 1024} MB).");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return new FileUploadResult(false, null, "Định dạng file không được hỗ trợ. Chỉ chấp nhận: .jpg, .jpeg, .png, .webp.");
        }

        if (!await MatchesImageSignatureAsync(file, extension))
        {
            return new FileUploadResult(false, null, "Nội dung file không khớp với định dạng ảnh đã khai báo (nghi ngờ file giả mạo phần mở rộng).");
        }

        // Tên file luôn do server sinh (GUID) - không bao giờ dùng lại tên file gốc của client,
        // vừa tránh path traversal (../../..), vừa tránh ghi đè file trùng tên.
        var safeFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(_uploadRootPath, safeFileName);

        // Phòng thủ thêm: xác nhận đường dẫn cuối cùng vẫn nằm trong thư mục upload cho phép.
        var uploadRootResolved = Path.GetFullPath(_uploadRootPath);
        var fullPathResolved = Path.GetFullPath(fullPath);
        if (!fullPathResolved.StartsWith(uploadRootResolved, StringComparison.Ordinal))
        {
            _logger.LogWarning("Phát hiện nghi vấn path traversal khi lưu ảnh bìa sách.");
            return new FileUploadResult(false, null, "Đường dẫn file không hợp lệ.");
        }

        try
        {
            // FileMode.CreateNew: nếu (cực hiếm) trùng GUID thì báo lỗi thay vì âm thầm ghi đè file khác.
            await using var stream = new FileStream(fullPathResolved, FileMode.CreateNew, FileAccess.Write);
            await file.CopyToAsync(stream);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Lỗi khi ghi file ảnh bìa sách xuống đĩa.");
            return new FileUploadResult(false, null, "Không thể lưu file, vui lòng thử lại.");
        }

        var relativeUrl = $"/uploads/books/{safeFileName}";
        return new FileUploadResult(true, relativeUrl, null);
    }

    public void DeleteBookCover(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return;
        }

        // Chỉ lấy phần tên file, bỏ mọi phần thư mục (đề phòng relativeUrl bị thao túng)
        var fileName = Path.GetFileName(relativeUrl);
        var fullPath = Path.Combine(_uploadRootPath, fileName);
        var uploadRootResolved = Path.GetFullPath(_uploadRootPath);
        var fullPathResolved = Path.GetFullPath(fullPath);

        if (!fullPathResolved.StartsWith(uploadRootResolved, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            if (File.Exists(fullPathResolved))
            {
                File.Delete(fullPathResolved);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Không thể xoá file ảnh cũ {File}", fullPathResolved);
        }
    }

    // Đọc 12 byte đầu tiên (magic number / file signature) để xác thực loại file THẬT SỰ,
    // độc lập với phần mở rộng hay Content-Type mà trình duyệt gửi lên.
    private static async Task<bool> MatchesImageSignatureAsync(IFormFile file, string extension)
    {
        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAsync(header.AsMemory(0, 12));
            if (read < 4)
            {
                return false;
            }
        }

        bool IsJpeg() => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

        bool IsPng() => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                        && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;

        bool IsWebp() => header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                          && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => IsJpeg(),
            ".png" => IsPng(),
            ".webp" => IsWebp(),
            _ => false
        };
    }
}
