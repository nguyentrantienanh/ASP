using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TienAnhGold.Models
{
    public class Order
    {
        public Order()
        {
            OrderDetails = new List<OrderDetail>(); // Khởi tạo mặc định để tránh null
        }

        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Liên kết với User

        public User User { get; set; }

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        public DateTime? ConfirmedAt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền không được âm")]
        public decimal? TotalAmount { get; set; }

        // Thuộc tính tính toán tổng giá trị đơn hàng
        [NotMapped]
        public decimal TotalPrice => OrderDetails?.Sum(od => od.Quantity * od.Price) ?? 0;

        // Thêm thông tin khách hàng (chỉ sử dụng khi checkout)
        [NotMapped]
        [Required(ErrorMessage = "Tên khách hàng không được để trống")]
        public string CustomerName { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string CustomerPhone { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string CustomerAddress { get; set; }

        // Thêm phương thức thanh toán (chỉ sử dụng khi checkout)
        [NotMapped]
        [Required(ErrorMessage = "Phương thức thanh toán không được để trống")]
        public string PaymentMethod { get; set; } // "COD" hoặc "BankTransfer"



        // Thêm trạng thái đơn hàng
        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public ICollection<OrderDetail> OrderDetails { get; set; }
    }

    public enum OrderStatus
    {
        Pending,            // Chờ duyệt
        Approved,           // Đã duyệt (hiển thị QR nếu là BankTransfer)
        AwaitingConfirmation, // Chờ admin xác nhận thanh toán
        Completed,           // Hoàn thành
            Deleted             // Đã xóa bởi admin
    }
}