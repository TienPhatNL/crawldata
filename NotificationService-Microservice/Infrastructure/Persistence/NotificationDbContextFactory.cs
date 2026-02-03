using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NotificationService.Infrastructure.Persistence;

/// <summary>
/// Factory for creating DbContext instances at design time (for migrations)
/// </summary>
public class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Create DbContextOptionsBuilder
        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        
        // Use a dummy connection string for migration generation
        // The actual connection string will be provided by Aspire at runtime
        var connectionString = "Server=localhost;Database=NotificationServiceDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";
        
        optionsBuilder.UseSqlServer(connectionString, 
            sqlOptions => sqlOptions.MigrationsAssembly("Infrastructure"));

        return new NotificationDbContext(optionsBuilder.Options);
    }
}
