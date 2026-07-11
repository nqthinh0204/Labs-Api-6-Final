namespace AspLab06Final.Mvc.ViewModels;

public class AuditLogListItemViewModel
{
    public int Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityKey { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string Result { get; set; } = "Success";
    public DateTime CreatedAt { get; set; }
}