using System.ComponentModel.DataAnnotations;

namespace TienAnhGold.Models
{
    public class Admin
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên không được để trống")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [MaxLength(100)]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; } = "Admin"; // Vai trò cố định là Admin

        public bool IsActive { get; set; } = true; // Trạng thái hoạt động
    }
}