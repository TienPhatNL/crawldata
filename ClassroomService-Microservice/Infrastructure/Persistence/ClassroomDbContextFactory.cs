using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ClassroomService.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating ClassroomDbContext during migrations
/// </summary>
public class ClassroomDbContextFactory : IDesignTimeDbContextFactory<ClassroomDbContext>
{
    public ClassroomDbContext CreateDbContext(string[] args)
    {
        // Build configuration - look in Application project
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Application");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Get connection string
        var connectionString = configuration.GetConnectionString("ClassroomDb");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'ClassroomDb' not found.");
        }

        // Build DbContext options
        var optionsBuilder = new DbContextOptionsBuilder<ClassroomDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ClassroomDbContext(optionsBuilder.Options);
    }
}
