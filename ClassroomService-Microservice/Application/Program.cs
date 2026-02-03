using ClassroomService.Application;
using ClassroomService.Infrastructure;
using ClassroomService.Infrastructure.Persistence;
using ClassroomService.Infrastructure.BackgroundServices;
using ClassroomService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add database with connection resilience using Aspire
builder.AddSqlServerDbContext<ClassroomDbContext>("ClassroomServiceDb", configureDbContextOptions: dbOptions =>
{
    dbOptions.UseSqlServer(sqlOptions => sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorNumbersToAdd: null));
});

// Add Redis client for Aspire (needed for connection string injection)
builder.AddRedisClient("redis");

// Add services to the container.
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Register background services
builder.Services.AddHostedService<ClassroomService.Application.Services.DatabaseMigrationHostedService>();
builder.Services.AddHostedService<ClassroomService.Application.Services.DatabaseSeedingHostedService>();
builder.Services.AddHostedService<ClassroomService.Infrastructure.BackgroundServices.TermAutoActivationService>();
builder.Services.AddHostedService<CourseAutoInactivationService>();
builder.Services.AddHostedService<AssignmentStatusUpdateService>();

// Add health checks for Aspire monitoring
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ClassroomDbContext>(
        name: "database",
        tags: new[] { "db", "sql", "ready" })
    .AddCheck("userservice_http",
        () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("UserService HTTP client configured"),
        tags: new[] { "http", "dependency" });

builder.Services.AddControllers();

// Add SignalR for real-time collaboration
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Configuration.GetValue<bool>("SignalR:EnableDetailedErrors", true);
    options.KeepAliveInterval = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("SignalR:KeepAliveInterval", 15));
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("SignalR:ClientTimeoutInterval", 30));
    options.HandshakeTimeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("SignalR:HandshakeTimeout", 15));
    options.MaximumReceiveMessageSize = builder.Configuration.GetValue<int>("SignalR:MaximumReceiveMessageSize", 52428800); // 50MB default
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
        Title = "ClassroomService API",
        Version = "v1"
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClassroomService API v1");
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

// Map SignalR Hubs
app.MapHub<ClassroomService.Application.Hubs.ReportCollaborationHub>("/hubs/report-collaboration");
app.MapHub<ClassroomService.Application.Hubs.ChatHub>("/hubs/chat");

// Map controllers
app.MapControllers();

// Map default endpoints (includes health checks)
app.MapHealthChecks("/health");

// Map SignalR hub for crawler chat communication
app.MapHub<ClassroomService.Application.Hubs.CrawlerChatHub>("/hubs/crawler-chat");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ClassroomDbContext>();
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
            
            logger.LogInformation("ClassroomService database migration completed successfully!");
            
            // Seed initial data if needed
            await SeedInitialDataAsync(scope.ServiceProvider);
            
            break; // Success - exit retry loop
        }
        catch (Exception ex) when (retryCount < maxRetries - 1)
        {
            retryCount++;
            var exponentialDelay = Math.Pow(2, Math.Min(retryCount, 7)); // Cap at 128 seconds
            var baseDelay = 5; // Base 5 second delay for Docker startup
            var totalDelay = TimeSpan.FromSeconds(exponentialDelay + baseDelay);
            
            logger.LogWarning(ex, "Database migration attempt {Attempt} failed. Retrying in {Delay} seconds... (Error: {ErrorMessage})", 
                retryCount, totalDelay.TotalSeconds, ex.Message);
            
            await Task.Delay(totalDelay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed after {MaxRetries} attempts. Cannot start service without database.", maxRetries);
            logger.LogError(" Make sure SQL Server Docker container is running:");
            logger.LogError("   docker ps | grep sql");
            logger.LogError("   OR start Aspire which will start SQL Server automatically");
            throw; // Throw to prevent service from starting with broken database
        }
    }
    
    logger.LogInformation("ClassroomService starting up...");
}

app.Run();

// Helper method to seed initial data
async Task SeedInitialDataAsync(IServiceProvider serviceProvider)
{
    var context = serviceProvider.GetRequiredService<ClassroomDbContext>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Checking if database needs seeding...");
        
        // NOTE: Term seeding is now handled by DatabaseSeedingHostedService
        // to allow for more flexible test scenarios (PAST/ACTIVE/FUTURE terms)
        
        // Check if we already have course codes
        if (!await context.CourseCodes.AnyAsync())
        {
            logger.LogInformation("Database is empty, seeding digital marketing course codes...");
            
            // Create digital marketing course codes only - no courses
            var courseCodes = new List<CourseCode>
            {
                new CourseCode
                {
                    Id = Guid.NewGuid(),
                    Code = "ADS301m",
                    Title = "Google Ads and SEO",
                    Description = "Master paid search advertising with Google Ads and organic search optimization through SEO best practices. Learn campaign setup, keyword research, ad copywriting, landing page optimization, and technical SEO.",
                    Department = "Digital Marketing",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CourseCode
                {
                    Id = Guid.NewGuid(),
                    Code = "DMA301m",
                    Title = "Digital Marketing Analytics",
                    Description = "Learn to measure and analyze digital marketing performance using Google Analytics 4, attribution modeling, conversion tracking, and data visualization. Build comprehensive marketing dashboards.",
                    Department = "Digital Marketing",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CourseCode
                {
                    Id = Guid.NewGuid(),
                    Code = "DMS301m",
                    Title = "Digital Marketing Strategy",
                    Description = "Develop integrated digital marketing strategies combining SEO, SEM, social media, content marketing, and email campaigns. Learn to create comprehensive marketing plans and measure ROI.",
                    Department = "Digital Marketing",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CourseCode
                {
                    Id = Guid.NewGuid(),
                    Code = "MKT101",
                    Title = "Marketing Principles",
                    Description = "Foundational marketing concepts including market segmentation, consumer behavior, the marketing mix (4Ps), brand positioning, and competitive analysis.",
                    Department = "Marketing",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CourseCode
                {
                    Id = Guid.NewGuid(),
                    Code = "MKT201",
                    Title = "Consumer Behavior",
                    Description = "Advanced study of consumer psychology, decision-making processes, purchase influencers, and behavioral economics in marketing contexts.",
                    Department = "Marketing",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new CourseCode
                {
                    Id = Guid.NewGuid(),
                    Code = "MKT304",
                    Title = "Integrated Marketing Communications",
                    Description = "Develop integrated marketing communication strategies across multiple channels including advertising, PR, social media, and content marketing.",
                    Department = "Marketing",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.CourseCodes.AddRange(courseCodes);
            await context.SaveChangesAsync();
            
            logger.LogInformation("Digital marketing course codes created successfully (6 codes)");
            logger.LogInformation("Note: No sample courses created - users can create courses through the API");
        }
        else
        {
            logger.LogInformation("Database already has course codes, skipping seed");
        }
        
        // Seed Topics if none exist
        if (!await context.Topics.AnyAsync())
        {
            logger.LogInformation("Seeding assignment topic types...");
            
            var topics = new List<Topic>
            {
                new Topic
                {
                    Id = Guid.NewGuid(),
                    Name = "LAB",
                    Description = "Hands-on practical exercises and workshops",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Topic
                {
                    Id = Guid.NewGuid(),
                    Name = "Assignment",
                    Description = "Individual or group tasks and deliverables",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Topic
                {
                    Id = Guid.NewGuid(),
                    Name = "Project",
                    Description = "Major comprehensive projects and capstone work",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Topics.AddRange(topics);
            await context.SaveChangesAsync();
            
            logger.LogInformation("Assignment topic types created successfully (3 types: LAB, Assignment, Project)");
        }
        else
        {
            logger.LogInformation("Database already has topics, skipping seed");
        }
        
        // Seed TopicWeights if none exist
        if (!await context.TopicWeights.AnyAsync())
        {
            logger.LogInformation("Seeding topic weights for testing...");
            
            // Get course codes and topics
            var ads301 = await context.CourseCodes.FirstOrDefaultAsync(c => c.Code == "ADS301m");
            var dma301 = await context.CourseCodes.FirstOrDefaultAsync(c => c.Code == "DMA301m");
            var mkt101 = await context.CourseCodes.FirstOrDefaultAsync(c => c.Code == "MKT101");
            
            var labTopic = await context.Topics.FirstOrDefaultAsync(t => t.Name == "LAB");
            var assignmentTopic = await context.Topics.FirstOrDefaultAsync(t => t.Name == "Assignment");
            var projectTopic = await context.Topics.FirstOrDefaultAsync(t => t.Name == "Project");
            
            var weights = new List<TopicWeight>();
            
            // ADS301m - Full configuration (100% total) - All topics configured
            if (ads301 != null && labTopic != null && assignmentTopic != null && projectTopic != null)
            {
                weights.Add(new TopicWeight
                {
                    Id = Guid.NewGuid(),
                    TopicId = labTopic.Id,
                    CourseCodeId = ads301.Id,
                    WeightPercentage = 30m,
                    Description = "LAB exercises for Google Ads and SEO",
                    ConfiguredBy = Guid.Parse("30000000-0000-0000-0000-000000000001"), // Staff
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                
                weights.Add(new TopicWeight
                {
                    Id = Guid.NewGuid(),
                    TopicId = assignmentTopic.Id,
                    CourseCodeId = ads301.Id,
                    WeightPercentage = 30m,
                    Description = "Assignments for Google Ads and SEO",
                    ConfiguredBy = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                
                weights.Add(new TopicWeight
                {
                    Id = Guid.NewGuid(),
                    TopicId = projectTopic.Id,
                    CourseCodeId = ads301.Id,
                    WeightPercentage = 40m,
                    Description = "Final project for Google Ads and SEO",
                    ConfiguredBy = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                
                logger.LogInformation("Configured weights for ADS301m: LAB(30%), Assignment(30%), Project(40%)");
            }
            
            // DMA301m - Partial configuration (70% total) - LAB intentionally NOT configured for testing
            if (dma301 != null && assignmentTopic != null && projectTopic != null)
            {
                weights.Add(new TopicWeight
                {
                    Id = Guid.NewGuid(),
                    TopicId = assignmentTopic.Id,
                    CourseCodeId = dma301.Id,
                    WeightPercentage = 30m,
                    Description = "Assignments for Digital Marketing Analytics",
                    ConfiguredBy = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                
                weights.Add(new TopicWeight
                {
                    Id = Guid.NewGuid(),
                    TopicId = projectTopic.Id,
                    CourseCodeId = dma301.Id,
                    WeightPercentage = 40m,
                    Description = "Analytics dashboard project",
                    ConfiguredBy = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                
                logger.LogInformation("Configured weights for DMA301m: Assignment(30%), Project(40%) - LAB NOT configured for testing");
            }
            
            // MKT101 - Minimal configuration (100% on one topic) - Only Assignment configured
            if (mkt101 != null && assignmentTopic != null)
            {
                weights.Add(new TopicWeight
                {
                    Id = Guid.NewGuid(),
                    TopicId = assignmentTopic.Id,
                    CourseCodeId = mkt101.Id,
                    WeightPercentage = 100m,
                    Description = "All assessments as assignments for Marketing Principles",
                    ConfiguredBy = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                
                logger.LogInformation("Configured weights for MKT101: Assignment(100%) - LAB and Project NOT configured for testing");
            }
            
            // DMS301m, MKT201, MKT304 - NO weights configured for testing filter with empty results
            logger.LogInformation("DMS301m, MKT201, MKT304 - NO weights configured for testing empty filter results");
            
            if (weights.Any())
            {
                context.TopicWeights.AddRange(weights);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} topic weight configurations", weights.Count);
            }
        }
        else
        {
            logger.LogInformation("Database already has topic weights, skipping seed");
        }
        
        logger.LogInformation("ClassroomService initial data seeding completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding ClassroomService initial data");
        // Don't throw - seeding is optional, service can still run
    }
}