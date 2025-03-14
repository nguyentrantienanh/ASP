using System.ComponentModel.DataAnnotations.Schema;

namespace TienAnhGold.Models
{
    public class CartItem
    {
        // Lớp CartItem đại diện cho sản phẩm trong giỏ hàng
        public int Id { get; set; }
        public int UserId { get; set; }
        public int GoldId { get; set; }
        public int Quantity { get; set; }

        [ForeignKey("GoldId")]
        public Gold? Gold { get; set; }
    }
}
