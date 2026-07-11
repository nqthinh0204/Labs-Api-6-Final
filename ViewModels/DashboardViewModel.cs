namespace AspLab06Final.Mvc.ViewModels;

public class DashboardViewModel
{
    public int TotalBooks { get; set; }       // gồm cả đã soft delete
    public int ActiveBooks { get; set; }      // IsDeleted = false
    public int DeletedBooks { get; set; }     // trong Trash
    public int LowStockBooks { get; set; }    // cần chú ý nhập thêm
    public int CreatedTodayBooks { get; set; }
    public int LogsToday { get; set; }

    // Chỉ được gán giá trị (khác null) khi người xem Dashboard có quyền CanViewAuditLog (Admin) - Lab06 Feature 3
    public SecurityDashboardViewModel? Security { get; set; }
}