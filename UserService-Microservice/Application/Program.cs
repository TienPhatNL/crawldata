using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MediatR;
using UserService.Application;
using UserService.Infrastructure;
using UserService.Infrastructure.Persistence;
using UserService.Application.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add database with connection resilience
builder.AddSqlServerDbContext<UserDbContext>("UserServiceDb", configureDbContextOptions: dbOptions =>
{
    dbOptions.UseSqlServer(sqlOptions => sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorNumbersToAdd: null));
});

// Add Redis for caching
builder.AddRedisClient("redis");

// Add application layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add controllers and API explorer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "UserService API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Set to true in production
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Authorization with policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOrAdmin", policy => policy.RequireRole("Staff", "Admin"));
    options.AddPolicy("LecturerOnly", policy => policy.RequireRole("Lecturer"));
    options.AddPolicy("StudentOnly", policy => policy.RequireRole("Student"));
    options.AddPolicy("PaidUserOnly", policy => policy.RequireRole("PaidUser"));
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Add global exception handler middleware
app.ConfigureExceptionHandler();

// Enable Swagger in all environments for API documentation access
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UserService API V1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at root
});

// Only redirect to HTTPS in production (FIX: This prevents Authorization header loss)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map default endpoints (includes health checks)
app.MapDefaultEndpoints();

// Ensure database is created and migrated with retry logic
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    var logger = app.Logger;
    
    const int maxRetries = 10; // Increased from 5 to 10 for SQL Server initialization
    var retryCount = 0;
    
    // Add initial delay to allow SQL Server container to fully initialize
    if (retryCount == 0)
    {
        logger.LogInformation("Waiting 5 seconds for SQL Server to fully initialize...");
        
        // Log connection info (without password) for debugging
        var connectionString = context.Database.GetConnectionString();
        if (!string.IsNullOrEmpty(connectionString))
        {
            // Create a safe version of connection string for logging (mask password)
            var safeConnectionString = connectionString.Contains("Password=") 
                ? connectionString.Substring(0, connectionString.IndexOf("Password=")) + "Password=***;..."
                : connectionString;
            logger.LogInformation("Using connection string: {ConnectionString}", safeConnectionString);
        }
        
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
    
    while (retryCount < maxRetries)
    {
        try
        {
            logger.LogInformation("Attempting database migration (attempt {Attempt}/{MaxRetries})", retryCount + 1, maxRetries);
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migration completed successfully");
            
            // Seed initial data if needed
            await SeedInitialDataAsync(scope.ServiceProvider);
            break; // Success - exit retry loop
        }
        catch (Exception ex) when (retryCount < maxRetries - 1)
        {
            retryCount++;
            // Enhanced delay: exponential backoff + base delay for SQL Server stability
            var exponentialDelay = Math.Pow(2, Math.Min(retryCount, 6)); // Cap at 64 seconds
            var baseDelay = 3; // Base 3 second delay
            var totalDelay = TimeSpan.FromSeconds(exponentialDelay + baseDelay);
            
            logger.LogWarning(ex, "Database migration attempt {Attempt} failed. Retrying in {Delay} seconds... (Error: {ErrorMessage})", 
                retryCount, totalDelay.TotalSeconds, ex.Message.Split('\n')[0]);
            await Task.Delay(totalDelay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed after {MaxRetries} attempts. This may indicate SQL Server is not ready or authentication issues.", maxRetries);
            throw;
        }
    }
}

app.Run();

// Helper method to seed initial data
async Task SeedInitialDataAsync(IServiceProvider serviceProvider)
{
    var context = serviceProvider.GetRequiredService<UserDbContext>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Seed subscription tiers FIRST
        if (!await context.SubscriptionTiers.AnyAsync())
        {
            logger.LogInformation("Seeding default subscription tiers...");

            var tiers = new[]
            {
                new UserService.Domain.Entities.SubscriptionTier
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Name = "Free",
                    Description = "Free tier for students",
                    Level = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new UserService.Domain.Entities.SubscriptionTier
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    Name = "Basic",
                    Description = "Basic tier for regular uses",
                    Level = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new UserService.Domain.Entities.SubscriptionTier
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    Name = "Premium",
                    Description = "Premium tier for higher needs",
                    Level = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new UserService.Domain.Entities.SubscriptionTier
                {
                    Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    Name = "Enterprise",
                    Description = "Enterprise tier packed with features",
                    Level = 3,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.SubscriptionTiers.AddRange(tiers);
            await context.SaveChangesAsync();
            logger.LogInformation("Default subscription tiers created with fixed GUIDs:");
            logger.LogInformation("- Free (Level 0): aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            logger.LogInformation("- Basic (Level 1): bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            logger.LogInformation("- Premium (Level 2): cccccccc-cccc-cccc-cccc-cccccccccccc");
            logger.LogInformation("- Enterprise (Level 3): dddddddd-dddd-dddd-dddd-dddddddddddd");
        }
        
        // Seed subscription plans SECOND (referencing tiers)
        if (!await context.SubscriptionPlans.AnyAsync())
        {
            logger.LogInformation("Seeding default subscription plans...");

            var plans = new[]
            {
                new UserService.Domain.Entities.SubscriptionPlan
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Fixed GUID for Free plan
                    Name = "Free",
                    Description = "Basic crawling for students",
                    Price = 0,
                    Currency = "VND",
                    DurationDays = 0,
                    QuotaLimit = 4,
                    Features = new List<string> { "Basic crawling", "Email support" },
                    IsActive = true,
                    SubscriptionTierId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    CreatedAt = DateTime.UtcNow
                },
                new UserService.Domain.Entities.SubscriptionPlan
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), // Fixed GUID for Basic plan
                    Name = "Basic",
                    Description = "Enhanced crawling for regular users",
                    Price = 100000,
                    Currency = "VND",
                    DurationDays = 30,
                    QuotaLimit = 35,
                    Features = new List<string> { "Enhanced crawling", "Data export", "Email support" },
                    IsActive = true,
                    SubscriptionTierId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    CreatedAt = DateTime.UtcNow
                },
                new UserService.Domain.Entities.SubscriptionPlan
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), // Fixed GUID for Premium plan
                    Name = "Premium",
                    Description = "Advanced crawling for professionals",
                    Price = 250000,
                    Currency = "VND",
                    DurationDays = 30,
                    QuotaLimit = 100,
                    Features = new List<string> { "Advanced crawling", "API access", "Priority support", "Advanced analytics" },
                    IsActive = true,
                    SubscriptionTierId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    CreatedAt = DateTime.UtcNow
                },
                new UserService.Domain.Entities.SubscriptionPlan
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), // Fixed GUID for Enterprise plan
                    Name = "Enterprise",
                    Description = "Unlimited crawling for organizations",
                    Price = 500000,
                    Currency = "VND",
                    DurationDays = 30,
                    QuotaLimit = 350,
                    Features = new List<string> { "Unlimited advanced features", "24/7 support", "Custom integrations", "Dedicated account manager" },
                    IsActive = true,
                    SubscriptionTierId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.SubscriptionPlans.AddRange(plans);
            await context.SaveChangesAsync();
            logger.LogInformation("Default subscription plans created with fixed GUIDs:");
            logger.LogInformation("- Free: 11111111-1111-1111-1111-111111111111");
            logger.LogInformation("- Basic: 22222222-2222-2222-2222-222222222222");
            logger.LogInformation("- Premium: 33333333-3333-3333-3333-333333333333");
            logger.LogInformation("- Enterprise: 44444444-4444-4444-4444-444444444444");
        }

        // Check if we already have an admin user
        if (!await context.Users.AnyAsync(u => u.Role == UserService.Domain.Enums.UserRole.Admin))
        {
            var passwordHashingService = serviceProvider.GetRequiredService<UserService.Infrastructure.Services.IPasswordHashingService>();
            
            var adminUser = new UserService.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "viethoang@crawldata.com",
                FirstName = "Hoang",
                LastName = "Duong Viet",
                PasswordHash = passwordHashingService.HashPassword("Admin@123456"),
                Role = UserService.Domain.Enums.UserRole.Admin,
                Status = UserService.Domain.Enums.UserStatus.Active,
                EmailConfirmedAt = DateTime.UtcNow,
                CurrentSubscriptionPlanId = Guid.Parse("44444444-4444-4444-4444-444444444444"), // Enterprise plan
                CrawlQuotaLimit = int.MaxValue,
                QuotaResetDate = DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add Lecturer user for testing (Fixed GUID for seed data consistency)
            var lecturerUser = new UserService.Domain.Entities.User
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), // Fixed GUID for seed data
                Email = "luuka@crawldata.com",
                FirstName = "Ka",
                LastName = "Luu",
                PasswordHash = passwordHashingService.HashPassword("Lecturer@123456"),
                Role = UserService.Domain.Enums.UserRole.Lecturer,
                Status = UserService.Domain.Enums.UserStatus.Active,
                EmailConfirmedAt = DateTime.UtcNow,
                CurrentSubscriptionPlanId = null, // Lecturers don't need automatic plan
                CrawlQuotaLimit = 0, // No crawling quota
                QuotaResetDate = DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add additional lecturer for more courses
            var lecturer2User = new UserService.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "lecturer2@crawldata.com",
                FirstName = "Sarah",
                LastName = "Johnson",
                PasswordHash = passwordHashingService.HashPassword("Lecturer@123456"),
                Role = UserService.Domain.Enums.UserRole.Lecturer,
                Status = UserService.Domain.Enums.UserStatus.Active,
                EmailConfirmedAt = DateTime.UtcNow,
                CurrentSubscriptionPlanId = Guid.Parse("44444444-4444-4444-4444-444444444444"), // Enterprise plan
                CrawlQuotaLimit = 20,
                QuotaResetDate = DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add Student user for testing (Fixed GUID for seed data consistency)
            var studentUser = new UserService.Domain.Entities.User
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), // Fixed GUID for seed data
                Email = "kangmin@crawldata.com",
                FirstName = "Min",
                LastName = "Phan Kang",
                PasswordHash = passwordHashingService.HashPassword("Student@123456"),
                Role = UserService.Domain.Enums.UserRole.Student,
                Status = UserService.Domain.Enums.UserStatus.Active,
                EmailConfirmedAt = DateTime.UtcNow,
                CurrentSubscriptionPlanId = Guid.Parse("11111111-1111-1111-1111-111111111111"), // Free plan
                CrawlQuotaLimit = 4,
                QuotaResetDate = DateTime.UtcNow.AddYears(1),
                StudentId = "STU001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add more students for Excel import testing
            var student2User = new UserService.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "student2@crawldata.com",
                FirstName = "Bob",
                LastName = "Wilson",
                PasswordHash = passwordHashingService.HashPassword("Student@123456"),
                Role = UserService.Domain.Enums.UserRole.Student,
                Status = UserService.Domain.Enums.UserStatus.Active,
                EmailConfirmedAt = DateTime.UtcNow,
                CurrentSubscriptionPlanId = Guid.Parse("22222222-2222-2222-2222-222222222222"), // Basic plan
                CrawlQuotaLimit = 10,
                QuotaResetDate = DateTime.UtcNow.AddYears(1),
                StudentId = "STU002",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var student3User = new UserService.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "student3@crawldata.com",
                FirstName = "Carol",
                LastName = "Davis",
                PasswordHash = passwordHashingService.HashPassword("Student@123456"),
                Role = UserService.Domain.Enums.UserRole.Student,
                Status = UserService.Domain.Enums.UserStatus.Active,
                EmailConfirmedAt = DateTime.UtcNow,
                CurrentSubscriptionPlanId = Guid.Parse("33333333-3333-3333-3333-333333333333"), // Premium plan
                CrawlQuotaLimit = 50,
                QuotaResetDate = DateTime.UtcNow.AddYears(1),
                StudentId = "STU003",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var student4User = new UserService.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "student4@crawldata.com",
                FirstName = "David",
                LastName = "Miller",
                PasswordHash = passwordHashingService.HashPassword("Student@123456"),
                Role = UserService.Domain.Enums.UserRole.Student,
                Status = UserService.Domain.Enums.UserStatus.Active,
                EmailConfirmedAt = DateTime.UtcNow,
                CurrentSubscriptionPlanId = Guid.Parse("22222222-2222-2222-2222-222222222222"), // Basic plan
                CrawlQuotaLimit = 10,
                QuotaResetDate = DateTime.UtcNow.AddYears(1),
                StudentId = "STU004",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var student5User = new UserService.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "student5@crawldata.com",
                FirstName = "Emma",
                LastName = "Garcia",
                PasswordHash = passwordHashingService.HashPassword("Student@123456"),
                Role = UserService.Domain.Enums.UserRole.Student,
                Status = UserService.Domain.Enums.UserStatus.Active,
                EmailConfirmedAt = DateTime.UtcNow,
                CurrentSubscriptionPlanId = Guid.Parse("33333333-3333-3333-3333-333333333333"), // Premium plan
                CrawlQuotaLimit = 50,
                QuotaResetDate = DateTime.UtcNow.AddYears(1),
                StudentId = "STU005",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Students for template emails (matching Excel template)
            var student6User = new UserService.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "student1@university.edu",
                FirstName = "Hung",
                LastName = "Nguyen Van",
                PasswordHash = passwordHashingService.HashPassword("Student@123456"),
                Role = UserService.Domain.Enums.UserRole.Student,
                Status = UserService.Domain.Enums.UserStatus.Active,
                EmailConfirmedAt = DateTime.UtcNow,
                CurrentSubscriptionPlanId = Guid.Parse("22222222-2222-2222-2222-222222222222"), // Basic plan
                CrawlQuotaLimit = 10,
                QuotaResetDate = DateTime.UtcNow.AddYears(1),
                StudentId = "STU006",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var student7User = new UserService.Domain.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = "student2@university.edu",
                FirstName = "Bich",
                LastName = "Le Thi",
                PasswordHash = passwordHashingService.HashPassword("Student@123456"),
                Role = UserService.Domain.Enums.UserRole.Student,
                Status = UserService.Domain.Enums.UserStatus.Active,
                EmailConfirmedAt = DateTime.UtcNow,
                CurrentSubscriptionPlanId = Guid.Parse("22222222-2222-2222-2222-222222222222"), // Basic plan
                CrawlQuotaLimit = 10,
                QuotaResetDate = DateTime.UtcNow.AddYears(1),
                StudentId = "STU007",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add Staff user for testing (Fixed GUID for seed data consistency)
            var staffUser = new UserService.Domain.Entities.User
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), // Fixed GUID for seed data
                Email = "tienphat@crawldata.com",
                FirstName = "Phat",
                LastName = "Nguyen Le Tien",
                PasswordHash = passwordHashingService.HashPassword("Staff@123456"),
                Role = UserService.Domain.Enums.UserRole.Staff,
                Status = UserService.Domain.Enums.UserStatus.Active,
                EmailConfirmedAt = DateTime.UtcNow,
                CurrentSubscriptionPlanId = Guid.Parse("44444444-4444-4444-4444-444444444444"), // Enterprise plan
                CrawlQuotaLimit = 20,
                QuotaResetDate = DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            context.Users.Add(lecturerUser);
            context.Users.Add(lecturer2User);
            context.Users.Add(studentUser);
            context.Users.Add(student2User);
            context.Users.Add(student3User);
            context.Users.Add(student4User);
            context.Users.Add(student5User);
            context.Users.Add(student6User);
            context.Users.Add(student7User);
            context.Users.Add(staffUser);
            await context.SaveChangesAsync();
            
            logger.LogInformation("Default users created successfully:");
            logger.LogInformation("- Admin: admin@crawldata.com / Admin@123456");
            logger.LogInformation("- Lecturer: lecturer@crawldata.com / Lecturer@123456");
            logger.LogInformation("- Lecturer2: lecturer2@crawldata.com / Lecturer@123456");
            logger.LogInformation("- Student:  / Student@123456");
            logger.LogInformation("- Student2: student2@crawldata.com / Student@123456");
            logger.LogInformation("- Student3: student3@crawldata.com / Student@123456");
            logger.LogInformation("- Student4: student4@crawldata.com / Student@123456");
            logger.LogInformation("- Student5: student5@crawldata.com / Student@123456");
            logger.LogInformation("- Template Student1: student1@university.edu / Student@123456");
            logger.LogInformation("- Template Student2: student2@university.edu / Student@123456");
            logger.LogInformation("- Staff: staff@crawldata.com / Staff@123456");
            logger.LogInformation("Note: Lecturer GUIDs are now consistent with ClassroomService courses for testing");
        }

        // Seed default allowed email domains for student auto-creation
        if (!await context.AllowedEmailDomains.AnyAsync())
        {
            logger.LogInformation("Seeding default allowed email domains...");

            var defaultDomains = new[]
            {
                new UserService.Domain.Entities.AllowedEmailDomain
                {
                    Id = Guid.NewGuid(),
                    Domain = ".edu",
                    Description = "Educational institutions",
                    IsActive = true,
                    AllowSubdomains = true,
                    CreatedAt = DateTime.UtcNow
                },
                new UserService.Domain.Entities.AllowedEmailDomain
                {
                    Id = Guid.NewGuid(),
                    Domain = ".ac.uk",
                    Description = "UK Academic institutions",
                    IsActive = true,
                    AllowSubdomains = true,
                    CreatedAt = DateTime.UtcNow
                },
                new UserService.Domain.Entities.AllowedEmailDomain
                {
                    Id = Guid.NewGuid(),
                    Domain = "@university.edu",
                    Description = "University.edu email addresses",
                    IsActive = true,
                    AllowSubdomains = false,
                    CreatedAt = DateTime.UtcNow
                },
                new UserService.Domain.Entities.AllowedEmailDomain
                {
                    Id = Guid.NewGuid(),
                    Domain = "@fpt.edu.vn",
                    Description = "FPT University email addresses",
                    IsActive = true,
                    AllowSubdomains = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.AllowedEmailDomains.AddRange(defaultDomains);
            await context.SaveChangesAsync();

            logger.LogInformation("Default allowed email domains created:");
            logger.LogInformation("- .edu (with subdomains)");
            logger.LogInformation("- .ac.uk (with subdomains)");
            logger.LogInformation("- @university.edu (exact match)");
            logger.LogInformation("- @fpt.edu.vn (exact match)");
        }

        // ⚠️ DISABLED: Test student quota reset
        // This logic was forcefully resetting the test student's subscription to Free plan
        // every time the app restarted, which prevented subscription purchases from persisting.
        // If you need to reset a test account, do it manually via SQL or admin panel.
        
        // var testStudentId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        // var testStudent = await context.Users.FirstOrDefaultAsync(u => u.Id == testStudentId);
        // if (testStudent != null)
        // {
        //     var freePlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        //     var now = DateTime.UtcNow;
        //     var userChanged = false;

        //     if (testStudent.CrawlQuotaLimit != 4)
        //     {
        //         testStudent.CrawlQuotaLimit = 4;
        //         userChanged = true;
        //     }

        //     if (testStudent.CrawlQuotaUsed != 0)
        //     {
        //         testStudent.CrawlQuotaUsed = 0;
        //         userChanged = true;
        //     }

        //     if (testStudent.CurrentSubscriptionPlanId != freePlanId)
        //     {
        //         testStudent.CurrentSubscriptionPlanId = freePlanId;
        //         userChanged = true;
        //     }

        //     if (testStudent.QuotaResetDate == default || testStudent.QuotaResetDate <= now)
        //     {
        //         testStudent.QuotaResetDate = now.AddDays(30);
        //         userChanged = true;
        //     }

        //     if (userChanged)
        //     {
        //         testStudent.UpdatedAt = now;
        //     }

        //     var existingSubscription = await context.UserSubscriptions
        //         .FirstOrDefaultAsync(s => s.UserId == testStudentId);

        //     if (existingSubscription == null)
        //     {
        //         context.UserSubscriptions.Add(new UserService.Domain.Entities.UserSubscription
        //         {
        //             Id = Guid.NewGuid(),
        //             UserId = testStudentId,
        //             SubscriptionPlanId = freePlanId,
        //             StartDate = now,
        //             IsActive = true,
        //             QuotaLimit = 4,
        //             DataExtractionLimit = 0,
        //             ReportGenerationLimit = 0,
        //             Price = 0,
        //             Currency = "VND",
        //             CreatedAt = now,
        //             UpdatedAt = now
        //         });
        //     }
        //     else
        //     {
        //         if (existingSubscription.SubscriptionPlanId != freePlanId)
        //         {
        //             existingSubscription.SubscriptionPlanId = freePlanId;
        //         }

        //         if (existingSubscription.QuotaLimit != 4)
        //         {
        //             existingSubscription.QuotaLimit = 4;
        //         }

        //         if (!existingSubscription.IsActive)
        //         {
        //             existingSubscription.IsActive = true;
        //         }

        //         existingSubscription.EndDate = null;
        //         existingSubscription.UpdatedAt = now;
        //     }

        //     await context.SaveChangesAsync();
        //     logger.LogInformation("Updated test student quota to 4 for user {UserId}", testStudentId);
        // }
        
        logger.LogInformation("Initial data seeding completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding initial data");
    }
}
