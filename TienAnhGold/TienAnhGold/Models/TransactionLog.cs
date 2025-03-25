using EllipticCurve.Utils;
using System;

namespace TienAnhGold.Models
{
    // Lớp TransactionLog đại diện cho lịch sử thay đổi trạng thái của đơn hàng
    public class TransactionLog
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public DateTime LogDate { get; set; } = DateTime.Now;
        public string? Action { get; set; } 
        public string? Notes { get; set; }
        public Order Order { get; set; }
    }
}