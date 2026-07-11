using System.ComponentModel.DataAnnotations;
namespace AspLab06Final.Mvc.Models;

public class Book
{
    public int Id { get; set; }

    // Mã định danh nghiệp vụ duy nhất (business key)
    public string BookCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int MinStock { get; set; } = 3;

    public int GenreId { get; set; }
    public Genre? Genre { get; set; }

    // Đường dẫn tương đối tới ảnh bìa sách, VD: /uploads/books/{guid}.jpg
    // Luôn là tên file do server sinh ra (Guid) - KHÔNG bao giờ lưu tên file người dùng gửi lên (Lab06 - Secure Upload)
    public string? CoverImageUrl { get; set; }

    // Audit fields - theo dõi vòng đời dữ liệu
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Concurrency token - chống lỗi "Last Save Wins"
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}