using AspLab06Final.Mvc.ViewModels;

namespace AspLab06Final.Mvc.Services;

public interface ISaleService
{
    Task<SaleCreateViewModel> GetCreateModelAsync();
    Task<OperationResult> CreateSaleAsync(SaleCreateViewModel model);
    Task<List<SaleListItemViewModel>> GetHistoryAsync();
    Task<SaleDetailViewModel?> GetDetailAsync(int id);
}
