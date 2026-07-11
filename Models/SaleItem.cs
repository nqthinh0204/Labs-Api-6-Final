namespace AspLab06Final.Mvc.Models;

// Chi tiết từng dòng sách trong một đơn bán (Sale)
public class SaleItem
{
    public int Id { get; set; }

    public int SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    // Snapshot tại thời điểm bán - để nếu sau này sách bị đổi tên/giá thì lịch sử đơn bán không bị thay đổi theo
    public string BookTitleSnapshot { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
}
