namespace AspLab06Final.Mvc.ViewModels;

public class BookListItemViewModel
{
    public int Id { get; set; }
    public string BookCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int MinStock { get; set; }
    public string GenreName { get; set; } = string.Empty;
    public bool IsLowStock { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CoverImageUrl { get; set; }
}