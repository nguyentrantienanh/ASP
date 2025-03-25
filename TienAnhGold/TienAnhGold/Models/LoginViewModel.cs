using System.ComponentModel.DataAnnotations;

namespace TienAnhGold.Models
{
    // Lớp LoginViewModel dùng để lưu thông tin đăng nhập
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }
    }
}