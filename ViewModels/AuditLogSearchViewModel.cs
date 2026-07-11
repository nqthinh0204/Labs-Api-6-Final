namespace AspLab06Final.Mvc.ViewModels;

// Lab06 Feature 3 - form lọc Audit Log theo user / action / result / khoảng ngày
public class AuditLogSearchViewModel
{
    public string? UserName { get; set; }
    public string? Action { get; set; }
    public string? Result { get; set; }

    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
    public DateTime? FromDate { get; set; }

    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
    public DateTime? ToDate { get; set; }

    // true khi người dùng đã bấm nút "Tìm kiếm" (để phân biệt với lần load trang đầu tiên)
    public bool HasSearched { get; set; }

    public List<AuditLogListItemViewModel> Results { get; set; } = new();

    // Danh sách action gợi ý cho dropdown lọc - khớp với các Action thực tế được ghi log trong hệ thống
    public static readonly string[] KnownActions =
    {
        "Create", "Edit", "SoftDelete", "Restore", "AdjustStock",
        "UploadBookImage", "ReplaceBookImage", "UploadRejected",
        "CreateSale", "Login", "Logout", "Register", "AccessDenied", "NotFound"
    };

    public static readonly string[] KnownResults = { "Success", "Denied", "Failed" };
}
