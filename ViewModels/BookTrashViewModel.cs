namespace AspLab06Final.Mvc.ViewModels;

public class BookTrashItemViewModel
{
    public int Id { get; set; }
    public string BookCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime? DeletedAt { get; set; }
}