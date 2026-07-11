namespace AspLab06Final.Mvc.Services;

// Kết quả của một lần lưu file: Success=false thì RelativeUrl luôn null và ErrorMessage giải thích lý do bị từ chối
public record FileUploadResult(bool Success, string? RelativeUrl, string? ErrorMessage);

public interface IFileUploadService
{
    // Kiểm tra whitelist extension + kích thước + magic bytes, rồi lưu file với tên do server sinh (Guid).
    // Trả về Success=false + ErrorMessage nếu file không hợp lệ - KHÔNG bao giờ throw ra ngoài cho lỗi validate thông thường.
    Task<FileUploadResult> SaveBookCoverAsync(IFormFile file);

    // Xoá 1 file ảnh bìa cũ (dùng khi thay ảnh mới thành công, hoặc khi cần rollback ảnh mới vừa lưu).
    void DeleteBookCover(string? relativeUrl);
}
