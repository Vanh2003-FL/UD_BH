using System;
using System.Collections.Generic;

namespace MyFamily.Models
{
    public enum OrderStatus
    {
        PendingNotPaid = 0,        // Chưa Lãy - Chưa Trả Tiền
        PendingPaid = 1,           // Chưa Lãy - Đã Trả Tiền
        DeliveredNotPaid = 2,      // Đã Lãy - Chưa Trả Tiền
        DeliveredPaid = 3          // Đã Lãy - Đã Trả Tiền
    }

    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DebtAmount { get; set; }
        public decimal CostAmount { get; set; }
        public decimal ProfitAmount { get; set; }

        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }
        
        public int? UserId { get; set; }  // Link to User for data isolation - nullable for flexibility
        public virtual User User { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.PendingNotPaid;

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}