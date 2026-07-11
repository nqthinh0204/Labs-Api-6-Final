using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AspLab06Final.Mvc.ViewModels;

// Lab06 Feature 2 - form upload/thay ảnh bìa sách an toàn
public class BookImageUploadViewModel
{
    public int Id { get; set; }
    public string BookCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? CurrentCoverImageUrl { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn một file ảnh.")]
    [Display(Name = "Ảnh bìa mới")]
    public IFormFile? CoverImageFile { get; set; }
}
