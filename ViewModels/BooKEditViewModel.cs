using System.ComponentModel.DataAnnotations;

namespace AspLab06Final.Mvc.ViewModels;

// Kế thừa các rule validation của Create, bổ sung Id và RowVersion (concurrency token).
public class BookEditViewModel : BookCreateViewModel
{
    public int Id { get; set; }

    // RowVersion được đưa vào hidden field dạng Base64 để server biết
    // phiên bản dữ liệu tại thời điểm user mở form Edit.
    public string RowVersion { get; set; } = string.Empty;

    // Chỉ để hiển thị (không submit qua form Edit) - việc đổi ảnh nằm ở action UploadImage riêng.
    public string? CurrentCoverImageUrl { get; set; }
}