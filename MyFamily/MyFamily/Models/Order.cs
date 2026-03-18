using System;
using System.Collections.Generic;

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

    public virtual ICollection<OrderDetail> OrderDetails { get; set; }
}