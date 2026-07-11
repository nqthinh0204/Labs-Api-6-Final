using AspLab06Final.Mvc.Models;

namespace AspLab06Final.Mvc.Repositories;

public interface IGenreRepository
{
    Task<List<Genre>> GetAllWithBooksReadOnlyAsync();
    Task<List<Genre>> GetAllAsync();
}