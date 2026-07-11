using System.ComponentModel.DataAnnotations;

namespace AspLab06Final.Mvc.ViewModels;

// Form điều chỉnh tồn kho nhanh - CHỈ nhận lượng thay đổi và RowVersion,
// không cho sửa các field khác của Book.
public class BookAdjustStockViewModel
{
    public int Id { get; set; }
    public string BookCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }

    [Range(-100000, 100000, ErrorMessage = "Lượng điều chỉnh không hợp lệ.")]
    [Display(Name = "Lượng thay đổi (+ nhập / - xuất)")]
    public int Delta { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}