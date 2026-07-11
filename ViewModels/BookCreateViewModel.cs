using System.ComponentModel.DataAnnotations;

namespace AspLab06Final.Mvc.ViewModels;

// ViewModel cho form Create - KHÔNG bind trực tiếp Entity (chống overposting).
// User không thể gửi CreatedAt / IsDeleted / RowVersion qua form này.
public class BookCreateViewModel
{
    [Required(ErrorMessage = "Tên sách là bắt buộc.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Tên sách phải từ 3 đến 200 ký tự.")]
    [Display(Name = "Tên sách")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã sách (BookCode) là bắt buộc.")]
    [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "BookCode chỉ gồm chữ in hoa, số và dấu '-'.")]
    [StringLength(20, ErrorMessage = "BookCode tối đa 20 ký tự.")]
    [Display(Name = "Mã sách")]
    public string BookCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tác giả là bắt buộc.")]
    [StringLength(150)]
    [Display(Name = "Tác giả")]
    public string Author { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Nhà xuất bản")]
    public string Publisher { get; set; } = string.Empty;

    [Range(1000, 100000000, ErrorMessage = "Giá phải từ 1.000 đến 100.000.000.")]
    [Display(Name = "Giá bán")]
    public decimal Price { get; set; }

    [Range(0, 100000, ErrorMessage = "Số lượng tồn kho phải từ 0 đến 100.000.")]
    [Display(Name = "Số lượng tồn")]
    public int Quantity { get; set; }

    [Range(0, 1000, ErrorMessage = "Ngưỡng tồn kho tối thiểu phải từ 0 đến 1.000.")]
    [Display(Name = "Tồn kho tối thiểu")]
    public int MinStock { get; set; } = 3;

    [Required(ErrorMessage = "Vui lòng chọn thể loại.")]
    [Display(Name = "Thể loại")]
    public int GenreId { get; set; }
}