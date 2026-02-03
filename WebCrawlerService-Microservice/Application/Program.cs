using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebCrawlerService.Application;
using WebCrawlerService.Application.Common.Middleware;
using WebCrawlerService.Application.Common.Security;
using WebCrawlerService.Infrastructure;
using WebCrawlerService.Infrastructure.Common;
using WebCrawlerService.Infrastructure.Contexts;
using WebCrawlerService.Infrastructure.Persistence.Interceptors;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container (MUST be first to register interceptors)
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add Crawl4AI services (Application layer)
builder.Services.AddScoped<WebCrawlerService.Application.Services.Crawl4AI.IGeminiService, WebCrawlerService.Application.Services.Crawl4AI.GeminiService>();
builder.Services.AddScoped<WebCrawlerService.Domain.Interfaces.ICrawl4AIClientService, WebCrawlerService.Application.Services.Crawl4AI.Crawl4AIClientService>();
builder.Services.AddScoped<WebCrawlerService.Application.Services.Crawl4AI.IPromptAnalyzerService, WebCrawlerService.Application.Services.Crawl4AI.PromptAnalyzerService>();
builder.Services.AddScoped<WebCrawlerService.Application.Services.Crawl4AI.ISmartCrawlerOrchestrationService, WebCrawlerService.Application.Services.Crawl4AI.SmartCrawlerOrchestrationService>();

// Add Kafka consumer for smart crawl requests from ClassroomService
builder.Services.AddHostedService<WebCrawlerService.Application.Messaging.SmartCrawlRequestConsumer>();

// Add database with connection resilience using Aspire
// Interceptors are registered in Infrastructure and will be resolved automatically
builder.AddSqlServerDbContext<CrawlerDbContext>("WebCrawlerDb", configureDbContextOptions: dbOptions =>
{
    dbOptions.UseSqlServer(sqlOptions => sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorNumbersToAdd: null));
});

// Add Redis for caching
builder.AddRedisClient("redis");

builder.Services.AddControllers();

// JWT Authentication and Authorization are configured in Infrastructure/DependencyInjection.cs
// (Commented out to avoid duplicate registration - policies are also defined there)

// Add CORS - SignalR-compatible configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "null",                       // For file:// protocol testing
                "http://localhost:5500",      // Live Server default port
                "http://127.0.0.1:5500",      // Live Server alternative
                "http://localhost:5501",      // Alternative Live Server port
                "http://127.0.0.1:5501",
                "http://localhost:3000",
                "http://localhost:3001",     
                "http://localhost:3002",     
                "http://localhost:3003",          
                "http://localhost:4200",      // Angular default
                "http://localhost:8080",      // Vue default
                "http://localhost:5173",      // Vite default
                "https://ai-enhance-six.vercel.app",      // Production frontend
                "https://ai-enhance-staff.vercel.app",    // Staff frontend
                "https://ai-enhance-admin.vercel.app"     // Admin frontend
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebCrawlerService API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
        c.DisplayRequestDuration();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelsExpandDepth(-1); // Hide schemas section
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.EnableValidator();
    });
}

// Only redirect to HTTPS in production (FIX: This prevents Authorization header loss)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// IMPORTANT: CORS must come before Authentication and Authorization
app.UseCors(); // Use default policy (AllowAnyOrigin)

// Add Authentication & Authorization in correct order
app.UseAuthentication();
app.UseAuthorization();

// Ensure database is created and migrated with retry logic
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CrawlerDbContext>();
    var logger = app.Logger;

    const int maxRetries = 10;
    var retryCount = 0;

    // Add initial delay to allow SQL Server container to fully initialize
    if (retryCount == 0)
    {
        logger.LogInformation("Waiting 5 seconds for SQL Server to fully initialize...");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    while (retryCount < maxRetries)
    {
        try
        {
            logger.LogInformation("Attempting database migration (attempt {Attempt}/{MaxRetries})", retryCount + 1, maxRetries);
            await context.Database.MigrateAsync();
            logger.LogInformation("WebCrawlerService database migration completed successfully");
            break; // Success - exit retry loop
        }
        catch (Exception ex) when (retryCount < maxRetries - 1)
        {
            retryCount++;
            var exponentialDelay = Math.Pow(2, Math.Min(retryCount, 6)); // Cap at 64 seconds
            var baseDelay = 3; // Base 3 second delay
            var totalDelay = TimeSpan.FromSeconds(exponentialDelay + baseDelay);

            logger.LogWarning(ex, "Database migration attempt {Attempt} failed. Retrying in {Delay} seconds... (Error: {ErrorMessage})",
                retryCount, totalDelay.TotalSeconds, ex.Message.Split('\n')[0]);
            await Task.Delay(totalDelay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WebCrawlerService database migration failed after {MaxRetries} attempts. This may indicate SQL Server is not ready or authentication issues.", maxRetries);
            throw;
        }
    }
}

// Set migration completed flag to allow background services to start
StartupCoordinator.IsMigrationCompleted = true;
app.Logger.LogInformation("✅ Database migration completed - background services can now start processing");

app.MapControllers();

app.MapDefaultEndpoints();

// Map SignalR hub for real-time crawl monitoring
app.MapHub<WebCrawlerService.Application.Hubs.CrawlHub>("/hubs/crawl");

app.Run();