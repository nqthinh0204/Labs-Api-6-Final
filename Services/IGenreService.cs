using AspLab06Final.Mvc.ViewModels;

namespace AspLab06Final.Mvc.Services;

public interface IGenreService
{
    Task<List<GenreListItemViewModel>> GetGenreListAsync();
    Task<List<GenreListItemViewModel>> GetGenreOptionsAsync();
}