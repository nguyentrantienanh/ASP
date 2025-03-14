namespace TienAnhGold.Models
{
    public class DashboardViewModel
    {
        public int UserCount { get; set; } // Số lượng tài khoản người dùng (chỉ cho admin)
        public int EmployeeCount { get; set; } // Số lượng tài khoản nhân viên (chỉ cho admin)
        public int OrderCount { get; set; } // Số lượng đơn hàng chờ xác nhận
        public decimal TotalRevenue { get; set; } // Doanh thu (chỉ cho admin)
        public int GoldCount { get; set; } // Số lượng sản phẩm vàng
    }
}
