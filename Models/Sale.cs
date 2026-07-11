namespace AspLab06Final.Mvc.Models;

// Lab06 - Luồng nghiệp vụ nhiều bước có transaction: tạo Đơn Bán Sách (Sale) + trừ tồn kho (Book.Quantity)
// Toàn bộ việc "kiểm tra tồn kho -> trừ tồn kho -> tạo Sale + SaleItems" phải thành công cùng nhau
// hoặc rollback toàn bộ (xem SaleService.CreateSaleAsync).
public class Sale
{
    public int Id { get; set; }

    // Mã đơn bán, sinh tự động dạng SALE-yyyyMMdd-xxxx
    public string SaleCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Username (Identity) của nhân viên thực hiện bán hàng - phục vụ audit / truy vết
    public string? CreatedByUserName { get; set; }

    public decimal TotalAmount { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}
