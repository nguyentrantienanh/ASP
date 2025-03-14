using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TienAnhGold.Models
{
    public class Gold
    {
        public int Id { get; set; } // Mã sản phẩm

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        public string? Name { get; set; } // Tên sản phẩm

        public string? ImagePath { get; set; } // Đường dẫn ảnh trong thư mục wwwroot/img/

        [NotMapped] // Không lưu vào database
        public IFormFile? ImageFile { get; set; } // File ảnh tải lên

        [Required(ErrorMessage = "Loại vàng không được để trống")]
        public string? Material { get; set; } // Chất liệu (VD: Vàng 18K, 24K)

        [Required(ErrorMessage = "Trọng lượng không được để trống")]
        [Range(0.1, double.MaxValue, ErrorMessage = "Trọng lượng phải lớn hơn 0")]
        public double Weight { get; set; } // Khối lượng (gram)

        public string? Description { get; set; } // Miêu tả sản phẩm

        [Required(ErrorMessage = "Giá không được để trống")]
        [Range(1000, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 1000")]
        public decimal Price { get; set; } // Giá (VND)

        [Required(ErrorMessage = "Số lượng không được để trống")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải là số dương")]
        public int Quantity { get; set; } // Số lượng sản phẩm

        public bool IsOutOfStock { get; set; } // Trạng thái hết hàng
    }
}
