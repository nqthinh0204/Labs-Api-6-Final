namespace AspLab06Final.Mvc.ViewModels;

// ==== Form tạo đơn bán (GET/POST /Sales/Create) ====

public class SaleCreateLineViewModel
{
    public int BookId { get; set; }
    public string BookCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }

    // Input người dùng nhập - CHỈ dùng field này từ client; Price/AvailableQuantity luôn được đọc lại
    // từ DB phía server khi xử lý (không tin dữ liệu giá/tồn kho do client gửi lên).
    public int QuantityToSell { get; set; }
}

public class SaleCreateViewModel
{
    public List<SaleCreateLineViewModel> Lines { get; set; } = new();
}

// ==== Lịch sử bán hàng (GET /Sales) ====

public class SaleListItemViewModel
{
    public int Id { get; set; }
    public string SaleCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserName { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
}

// ==== Chi tiết 1 đơn bán (GET /Sales/Detail/{id}) ====

public class SaleDetailItemViewModel
{
    public string BookTitleSnapshot { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class SaleDetailViewModel
{
    public int Id { get; set; }
    public string SaleCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedByUserName { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SaleDetailItemViewModel> Items { get; set; } = new();
}
