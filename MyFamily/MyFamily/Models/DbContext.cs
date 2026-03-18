using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace MyFamily.Models
{
    public class MyDbContext : DbContext
    {
        public MyDbContext() : base("MyDb") 
        { 
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<MyDbContext, MyFamily.Migrations.Configuration>());
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
    }
}