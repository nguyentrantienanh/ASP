using EllipticCurve.Utils;
using System;

namespace TienAnhGold.Models
{
    public class TransactionLog
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public DateTime LogDate { get; set; } = DateTime.Now;
        public string Action { get; set; } // "Confirmed", "Paid", "Completed"
        public string Notes { get; set; }
        public Order Order { get; set; }
    }
}