using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Svipp.Infrastructure;

public class SvippDbContextFactory : IDesignTimeDbContextFactory<SvippDbContext>
{
    public SvippDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SvippDbContext>();
        
        // Use a default connection string for migrations
        var connectionString = Environment.GetEnvironmentVariable("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=svipp_dev_db;Username=svipp_dev;Password=svipp_dev_password";
        
        optionsBuilder.UseNpgsql(connectionString);
        
        return new SvippDbContext(optionsBuilder.Options);
    }
}

