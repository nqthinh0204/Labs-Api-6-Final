namespace AspLab06Final.Mvc.ViewModels;

// Lab06 Feature 3 - số liệu bảo mật trong ngày, hiển thị trên Dashboard (chỉ Admin xem được)
public class SecurityDashboardViewModel
{
    // Số lần bị từ chối truy cập (AccessDenied) trong ngày hôm nay
    public int AccessDeniedToday { get; set; }

    // Số thao tác "nhạy cảm" (Create/Edit/SoftDelete/Restore/AdjustStock/Upload ảnh/Tạo đơn bán) trong ngày hôm nay
    public int SensitiveActionsToday { get; set; }

    // Số lượt upload ảnh bị từ chối (sai định dạng/quá dung lượng/giả mạo) trong ngày hôm nay
    public int RejectedUploadsToday { get; set; }
}
