namespace MyFamily.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public int? UserId { get; set; }  // Link to User for data isolation - nullable for flexibility
        public virtual User User { get; set; }
    }
}