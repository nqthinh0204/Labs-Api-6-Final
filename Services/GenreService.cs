using AspLab06Final.Mvc.Options;
using AspLab06Final.Mvc.Repositories;
using AspLab06Final.Mvc.ViewModels;
using Microsoft.Extensions.Options;

namespace AspLab06Final.Mvc.Services;

public class GenreService : IGenreService
{
    private readonly IGenreRepository _genreRepository;
    private readonly AppSettings _settings;

    public GenreService(IGenreRepository genreRepository, IOptions<AppSettings> options)
    {
        _genreRepository = genreRepository;
        _settings = options.Value;
    }

    public async Task<List<GenreListItemViewModel>> GetGenreListAsync()
    {
        var genres = await _genreRepository.GetAllWithBooksReadOnlyAsync();
        return genres.Select(g => new GenreListItemViewModel
        {
            Id = g.Id,
            Name = g.Name,
            BookCount = g.Books.Count,
            Books = g.Books.Select(b => new BookListItemViewModel
            {
                Id = b.Id,
                BookCode = b.BookCode,
                Title = b.Title,
                Author = b.Author,
                Price = b.Price,
                Quantity = b.Quantity,
                GenreName = g.Name,
                IsLowStock = b.Quantity <= _settings.LowStockThreshold
            }).ToList()
        }).ToList();
    }

    public async Task<List<GenreListItemViewModel>> GetGenreOptionsAsync()
    {
        var genres = await _genreRepository.GetAllAsync();
        return genres.Select(g => new GenreListItemViewModel
        {
            Id = g.Id,
            Name = g.Name
        }).ToList();
    }
}