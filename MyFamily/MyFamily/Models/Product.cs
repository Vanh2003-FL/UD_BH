namespace MyFamily.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int? UserId { get; set; }  // Optional - if product is shared, leave null
        public virtual User User { get; set; }
    }
}