using System.Data.Entity.Migrations;

internal sealed class Configuration : DbMigrationsConfiguration<MyDbContext>
{
    public Configuration()
    {
        AutomaticMigrationsEnabled = false;
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