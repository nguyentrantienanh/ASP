using System.ComponentModel.DataAnnotations;

namespace TienAnhGold.Models
{
    // Lớp CheckoutViewModel dùng để lưu thông tin đặt hàng
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Tên khách hàng không được để trống")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string CustomerPhone { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        public string CustomerAddress { get; set; }

        [Required(ErrorMessage = "Phương thức thanh toán không được để trống")]
        public string PaymentMethod { get; set; } // "COD" hoặc "BankTransfer"
    }
} 