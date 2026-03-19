using System.Data.Entity.Migrations;
using MyFamily.Models;

namespace MyFamily.Migrations
{
    public sealed class Configuration : DbMigrationsConfiguration<MyDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;  // Allow automatic migrations to alter schema
        }

        protected override void Seed(MyDbContext context)
        {
            context.Users.AddOrUpdate(x => x.Username,
                new User
                {
                    Username = "admin",
                    Password = "123456",
                    Role = "Admin"
                });
        }
    }
}