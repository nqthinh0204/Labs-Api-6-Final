using AspLab06Final.Mvc.Data;
using AspLab06Final.Mvc.Models;
using Microsoft.EntityFrameworkCore;

namespace AspLab06Final.Mvc.Repositories;

public class GenreRepository : IGenreRepository
{
    private readonly AppDbContext _context;

    public GenreRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Genre>> GetAllWithBooksReadOnlyAsync()
        => _context.Genres.Include(g => g.Books).AsNoTracking().ToListAsync();

    public Task<List<Genre>> GetAllAsync()
        => _context.Genres.AsNoTracking().OrderBy(g => g.Name).ToListAsync();
}