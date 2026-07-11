namespace AspLab06Final.Mvc.ViewModels;

// Feature 1 - Tìm kiếm & lọc sách đang hoạt động theo nhiều điều kiện.
public class BookSearchViewModel
{
    public string? Keyword { get; set; }
    public string? StockStatus { get; set; } // all | low | out | in
    public List<BookListItemViewModel> Results { get; set; } = new();
    public bool HasSearched { get; set; }
}