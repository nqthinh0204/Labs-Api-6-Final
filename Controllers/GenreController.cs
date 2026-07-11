using AspLab06Final.Mvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspLab06Final.Mvc.Controllers;

// Lab06: minh hoạ cách dùng [Authorize(Roles=...)] trực tiếp - phù hợp cho luật đơn giản kiểu
// "chỉ cần đã đăng nhập với 1 trong các role sau", khi không cần gắn ý nghĩa nghiệp vụ riêng như các Policy khác
// (CanManageBook, CanAdjustStock...). Xem Câu 3 - Problem Solving để biết khi nào nên dùng Roles và khi nào nên dùng Policy.
[Authorize(Roles = "Admin,Staff,User")]
public class GenresController : Controller
{
    private readonly IGenreService _genreService;

    public GenresController(IGenreService genreService)
    {
        _genreService = genreService;
    }

    public async Task<IActionResult> Index()
    {
        var genres = await _genreService.GetGenreListAsync();
        return View(genres);
    }
}