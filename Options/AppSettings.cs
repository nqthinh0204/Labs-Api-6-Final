namespace AspLab06Final.Mvc.Options;

public class AppSettings
{
    public string AppName { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
    public bool EnableSeedData { get; set; }
    public int LowStockThreshold { get; set; } = 5;
}