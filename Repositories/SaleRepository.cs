using AspLab06Final.Mvc.Data;
using AspLab06Final.Mvc.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AspLab06Final.Mvc.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly AppDbContext _context;

    public SaleRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
        => _context.Database.BeginTransactionAsync();

    public async Task AddSaleAsync(Sale sale)
        => await _context.Sales.AddAsync(sale);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public Task<List<Sale>> GetHistoryAsync(int take = 100)
        => _context.Sales
            .Include(s => s.Items)
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Take(take)
            .ToListAsync();

    public Task<Sale?> GetDetailAsync(int id)
        => _context.Sales
            .Include(s => s.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
}
