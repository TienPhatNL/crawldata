using NotificationService.Application;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add database with connection resilience using Aspire
builder.AddSqlServerDbContext<NotificationDbContext>("NotificationServiceDb", configureDbContextOptions: dbOptions =>
{
    dbOptions.UseSqlServer(sqlOptions => sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorNumbersToAdd: null));
});

// Add Redis client for Aspire (needed for connection string injection)
builder.AddRedisClient("redis");

// Add services to the container
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Add health checks for Aspire monitoring
builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotificationDbContext>(
        name: "database",
        tags: new[] { "db", "sql", "ready" });

builder.Services.AddControllers();

// Add SignalR for real-time notifications
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Configuration.GetValue<bool>("SignalR:EnableDetailedErrors", true);
    options.KeepAliveInterval = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("SignalR:KeepAliveInterval", 15));
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("SignalR:ClientTimeoutInterval", 30));
    options.HandshakeTimeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("SignalR:HandshakeTimeout", 15));
    options.MaximumReceiveMessageSize = builder.Configuration.GetValue<int>("SignalR:MaximumReceiveMessageSize", 32768);
});

// Add CORS - More permissive for development (needed for SignalR)
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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NotificationService API",
        Version = "v1",
        Description = "API for managing user notifications with real-time delivery via SignalR"
    });

    // Include XML comments for better documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Group operations by controller
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    c.DocInclusionPredicate((name, api) => true);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "NotificationService API v1");
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

// Only redirect to HTTPS in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// IMPORTANT: CORS must come before Authentication and Authorization
app.UseCors();

// Add Authentication & Authorization in correct order
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR Hub
app.MapHub<NotificationService.Application.Hubs.NotificationHub>("/hubs/notifications");

// Map controllers
app.MapControllers();

// Map default endpoints (includes health checks)
app.MapHealthChecks("/health");

// Apply database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    const int maxRetries = 15; // Increased retries for Docker startup
    var retryCount = 0;
    
    logger.LogInformation("Starting database migration...");
    logger.LogInformation("Waiting for SQL Server Docker container to be ready...");
    
    await Task.Delay(TimeSpan.FromSeconds(5));
    
    while (retryCount < maxRetries)
    {
        try
        {
            logger.LogInformation("Attempting database migration (attempt {Attempt}/{MaxRetries})", retryCount + 1, maxRetries);
            
            // Try to connect and migrate
            await context.Database.MigrateAsync();
            
            logger.LogInformation("NotificationService database migration completed successfully!");
            break;
        }
        catch (Exception ex)
        {
            retryCount++;
            
            if (retryCount >= maxRetries)
            {
                logger.LogError(ex, "Failed to migrate database after {MaxRetries} attempts. Application will start without migrations.", maxRetries);
                break;
            }
            
            logger.LogWarning(ex, "Database migration attempt {Attempt} failed. Retrying in 3 seconds...", retryCount);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

app.Run();
