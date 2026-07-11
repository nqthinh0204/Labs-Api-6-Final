using AspLab06Final.Mvc.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace AspLab06Final.Mvc.Repositories;

public interface ISaleRepository
{
    // Transaction tường minh cho luồng nghiệp vụ nhiều bước (trừ tồn kho nhiều sách + tạo Sale/SaleItems)
    Task<IDbContextTransaction> BeginTransactionAsync();

    Task AddSaleAsync(Sale sale);
    Task SaveChangesAsync();

    Task<List<Sale>> GetHistoryAsync(int take = 100);
    Task<Sale?> GetDetailAsync(int id);
}
