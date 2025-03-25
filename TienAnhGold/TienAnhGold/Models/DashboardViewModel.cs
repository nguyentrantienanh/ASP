namespace TienAnhGold.Models
{
    // Lớp DashboardViewModel dùng để lưu thông tin thống kê trên trang Dashboard
    public class DashboardViewModel
    {
        public int UserCount { get; set; } // Số lượng tài khoản người dùng (chỉ cho admin)
        public int EmployeeCount { get; set; } // Số lượng tài khoản nhân viên (chỉ cho admin)
        public int OrderCount { get; set; } // Số lượng đơn hàng chờ xác nhận
        public decimal TotalRevenue { get; set; } // Doanh thu (chỉ cho admin)
        public int GoldCount { get; set; } // Số lượng sản phẩm vàng
        public List<Order> RecentOrders { get; set; } //
        public int ApprovedOrderCount { get; internal set; }
    }
}
