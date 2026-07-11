namespace AspLab06Final.Mvc.Models;

// Lưu lại các thao tác quan trọng (Create / Edit / SoftDelete / Restore / AdjustStock / Login / AccessDenied ...)
// Lab06: mở rộng để trả lời được "ai" (UserName), "từ đâu" (IpAddress), "kết quả ra sao" (Result)
public class AuditLog
{
    public int Id { get; set; }
    public string Level { get; set; } = "Information"; // Information / Warning / Error
    public string Action { get; set; } = string.Empty;  // Create, Edit, SoftDelete, Restore, AdjustStock, Login, Logout, AccessDenied, UploadRejected, CreateSale, NotFound...
    public string Entity { get; set; } = "Book";        // Book, Sale, Auth, ...
    public string? EntityKey { get; set; }              // BookCode, SaleCode, Email... tùy Entity
    public string Message { get; set; } = string.Empty; // structured message đã render

    // Ai đã thực hiện thao tác (username Identity), lấy từ HttpContext, KHÔNG do người gọi tự truyền vào để tránh giả mạo
    public string? UserName { get; set; }

    // Địa chỉ IP nguồn của request, lấy từ HttpContext.Connection.RemoteIpAddress
    public string? IpAddress { get; set; }

    // Kết quả: Success / Denied / Failed - phục vụ Security Dashboard (Feature 3)
    public string Result { get; set; } = "Success";

    public DateTime CreatedAt { get; set; }
}