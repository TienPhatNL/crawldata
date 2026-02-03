using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Application.Services;

/// <summary>
/// Background hosted service that runs database seeding operations after the application starts.
/// This prevents seeding from blocking the main startup thread and allows the HTTP server to start immediately.
/// </summary>
public class DatabaseSeedingHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DatabaseSeedingHostedService> _logger;

    public DatabaseSeedingHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DatabaseSeedingHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatabaseSeedingHostedService starting in background...");

        // Run seeding in background task to not block startup
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();

                // Seed initial data (Terms, CourseCodes, Topics)
                await SeedInitialDataAsync(scope.ServiceProvider);

                // Seed classroom entities (Courses, Enrollments, Groups, Assignments)
                var kafkaUserService = scope.ServiceProvider.GetRequiredService<ClassroomService.Domain.Interfaces.IKafkaUserService>();
                await SeedClassroomEntitiesAsync(scope.ServiceProvider, kafkaUserService);

                _logger.LogInformation("DatabaseSeedingHostedService completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DatabaseSeedingHostedService failed - seeding is optional, service continues running");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DatabaseSeedingHostedService stopping...");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Seeds initial reference data: Terms, CourseCodes, and Topics
    /// </summary>
    private async Task SeedInitialDataAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ClassroomDbContext>();
        var logger = serviceProvider.GetRequiredService<ILogger<DatabaseSeedingHostedService>>();

        try
        {
            logger.LogInformation("Checking if database needs seeding...");

            // Check if Terms exist
            var termCount = await context.Terms.CountAsync();
            logger.LogInformation("Current term count in database: {Count}", termCount);
            
            // Seed Terms - Force re-seed if Spring 2026 has wrong dates
            var spring2026Existing = await context.Terms.FirstOrDefaultAsync(t => t.Name == "Spring 2026");
            var needsTermReseed = spring2026Existing == null || spring2026Existing.StartDate.Month != 1; // Should start in January
            
            logger.LogInformation("Term check: Spring 2026 exists={Exists}, StartMonth={Month}, NeedsReseed={NeedsReseed}", 
                spring2026Existing != null, 
                spring2026Existing?.StartDate.Month, 
                needsTermReseed);
            
            if (termCount == 0 || needsTermReseed)
            {
                if (needsTermReseed && spring2026Existing != null)
                {
                    logger.LogInformation("Spring 2026 term has incorrect dates (starts {Month}), re-seeding all terms...", spring2026Existing.StartDate.Month);
                    // Remove all existing terms
                    var existingTerms = await context.Terms.ToListAsync();
                    logger.LogInformation("Removing {Count} existing terms", existingTerms.Count);
                    context.Terms.RemoveRange(existingTerms);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Existing terms removed successfully");
                }
                
                logger.LogInformation("Seeding default terms...");

                var terms = new List<Term>
                {
                    // PAST TERM - Fall 2025 (ended December 2025)
                    new Term
                    {
                        Id = Guid.NewGuid(),
                        Name = "Fall 2025",
                        Description = "Fall semester 2025 (September - December) - PAST",
                        StartDate = new DateTime(2025, 9, 1),
                        EndDate = new DateTime(2025, 12, 31),
                        IsActive = false, // Past term - inactive
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    // ACTIVE TERM - Spring 2026 (January 5 - April 30, 2026)
                    new Term
                    {
                        Id = Guid.NewGuid(),
                        Name = "Spring 2026",
                        Description = "Spring semester 2026 (January - April) - ACTIVE NOW",
                        StartDate = new DateTime(2026, 1, 5),
                        EndDate = new DateTime(2026, 4, 30),
                        IsActive = true, // Currently active
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    // FUTURE TERM - Summer 2026 (starts May 2026)
                    new Term
                    {
                        Id = Guid.NewGuid(),
                        Name = "Summer 2026",
                        Description = "Summer semester 2026 (May - August) - FUTURE",
                        StartDate = new DateTime(2026, 5, 1),
                        EndDate = new DateTime(2026, 8, 31),
                        IsActive = false, // Future term - will be activated by background service
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    // Additional future terms
                    new Term
                    {
                        Id = Guid.NewGuid(),
                        Name = "Fall 2026",
                        Description = "Fall semester 2026 (September - December)",
                        StartDate = new DateTime(2026, 9, 1),
                        EndDate = new DateTime(2026, 12, 20),
                        IsActive = false, // Future term - will be activated by background service
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Term
                    {
                        Id = Guid.NewGuid(),
                        Name = "Spring 2027",
                        Description = "Spring semester 2027 (January - May)",
                        StartDate = new DateTime(2027, 1, 15),
                        EndDate = new DateTime(2027, 5, 31),
                        IsActive = false, // Future term - will be activated by background service
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                context.Terms.AddRange(terms);
                await context.SaveChangesAsync();

                logger.LogInformation("Terms created successfully: Fall 2025 (PAST), Spring 2026 (ACTIVE), Summer 2026 (FUTURE), Fall 2026, Spring 2027");
            }

            // Check if we already have course codes
            if (!await context.CourseCodes.AnyAsync())
            {
                logger.LogInformation("Database is empty, seeding digital marketing course codes...");

                // Create digital marketing course codes
                var courseCodes = new List<CourseCode>
                {
                    new CourseCode
                    {
                        Id = Guid.NewGuid(),
                        Code = "ADS301m",
                        Title = "Google Ads and SEO",
                        Description = "Master Google Ads campaigns, search engine optimization techniques, keyword research, and analytics for digital advertising.",
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
                        Description = "Learn to analyze marketing data using Google Analytics, create dashboards, track ROI, and make data-driven decisions.",
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
                        Description = "Develop comprehensive digital marketing strategies including content marketing, social media, and campaign planning.",
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
                        Description = "Introduction to fundamental marketing concepts including the marketing mix, consumer behavior, and market segmentation.",
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
                        Description = "Study consumer psychology, decision-making processes, and behavioral insights for effective marketing strategies.",
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
                        Description = "Learn to create cohesive multi-channel marketing campaigns across digital and traditional media platforms.",
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
                logger.LogInformation("Seeding assignment topics...");

                var topics = new List<Topic>
                {
                    new Topic
                    {
                        Id = Guid.NewGuid(),
                        Name = "LAB",
                        Description = "Hands-on practical exercises and skill-building sessions",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Topic
                    {
                        Id = Guid.NewGuid(),
                        Name = "Assignment",
                        Description = "Individual or group tasks to apply course concepts",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Topic
                    {
                        Id = Guid.NewGuid(),
                        Name = "Project",
                        Description = "Major comprehensive projects requiring extended work and integration of multiple skills",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                context.Topics.AddRange(topics);
                await context.SaveChangesAsync();

                logger.LogInformation("Assignment topics created successfully (3 types: LAB, Assignment, Project)");
            }
            else
            {
                logger.LogInformation("Database already has topics, skipping seed");
            }

            // Seed TopicWeights for testing (after Topics and CourseCodes are created)
            // This runs regardless of whether Topics were just created or already existed
            await SeedTopicWeightsAsync(context, logger);

            logger.LogInformation("ClassroomService initial data seeding completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding ClassroomService initial data");
            // Don't throw - seeding is optional, service can still run
        }
    }

    /// <summary>
    /// Seeds classroom entities for testing: Courses, Enrollments, Groups, and Assignments
    /// </summary>
    private async Task SeedClassroomEntitiesAsync(IServiceProvider serviceProvider, ClassroomService.Domain.Interfaces.IKafkaUserService kafkaUserService)
    {
        var context = serviceProvider.GetRequiredService<ClassroomDbContext>();
        var logger = serviceProvider.GetRequiredService<ILogger<DatabaseSeedingHostedService>>();

        try
        {
            logger.LogInformation("Checking if classroom entities need seeding...");

            // Fixed GUIDs for seed users (must match UserService seed data)
            var lecturerId = Guid.Parse("10000000-0000-0000-0000-000000000001");
            var studentId = Guid.Parse("20000000-0000-0000-0000-000000000001");

            // Fetch actual lecturer information from UserService
            var lecturer = await kafkaUserService.GetUserByIdAsync(lecturerId);
            var lecturerName = lecturer?.FullName ?? "Unknown Lecturer";
            logger.LogInformation("Using lecturer: {LecturerName} (ID: {LecturerId})", lecturerName, lecturerId);

            // Check if we already have courses
            if (await context.Courses.AnyAsync())
            {
                logger.LogInformation("Courses already exist, skipping classroom entities seed");
                return;
            }

            logger.LogInformation("Seeding digital marketing classroom entities...");

            // Get terms for testing
            var fall2025 = await context.Terms.FirstOrDefaultAsync(t => t.Name == "Fall 2025");
            var spring2026 = await context.Terms.FirstOrDefaultAsync(t => t.Name == "Spring 2026");
            var summer2026 = await context.Terms.FirstOrDefaultAsync(t => t.Name == "Summer 2026");
                
            // Get digital marketing course codes
            var ads301Code = await context.CourseCodes.FirstOrDefaultAsync(c => c.Code == "ADS301m");
            var dma301Code = await context.CourseCodes.FirstOrDefaultAsync(c => c.Code == "DMA301m");
            var mkt101Code = await context.CourseCodes.FirstOrDefaultAsync(c => c.Code == "MKT101");
            
            // Get topics
            var labTopic = await context.Topics.FirstOrDefaultAsync(t => t.Name == "LAB");
            var assignmentTopic = await context.Topics.FirstOrDefaultAsync(t => t.Name == "Assignment");
            var projectTopic = await context.Topics.FirstOrDefaultAsync(t => t.Name == "Project");

            if (fall2025 == null || spring2026 == null || summer2026 == null || 
                ads301Code == null || dma301Code == null || mkt101Code == null ||
                labTopic == null || assignmentTopic == null || projectTopic == null)
            {
                logger.LogWarning("Required reference data not found. Run SeedInitialDataAsync first.");
                return;
            }
            
            logger.LogInformation("Creating courses in Fall 2025 (PAST), Spring 2026 (ACTIVE), and Summer 2026 (FUTURE)");

            // === 1. CREATE DIGITAL MARKETING COURSES ===
            
            // FALL 2025 (PAST TERM) - ADS301m
            var courseFall2025 = new Course
            {
                Id = Guid.NewGuid(),
                CourseCodeId = ads301Code.Id,
                UniqueCode = "F25ADS",
                Name = $"ADS301m#F25ADS - {lecturerName}",
                Description = "Fall 2025 - Google Ads & SEO (PAST TERM - Can update weights)",
                LecturerId = lecturerId,
                TermId = fall2025.Id,
                Status = CourseStatus.Inactive,
                CreatedAt = DateTime.UtcNow.AddMonths(-4),
                UpdatedAt = DateTime.UtcNow.AddMonths(-4)
            };

            // SPRING 2026 (ACTIVE TERM) - DMA301m
            var courseSpring2026 = new Course
            {
                Id = Guid.NewGuid(),
                CourseCodeId = dma301Code!.Id,
                UniqueCode = "S26DMA",
                Name = $"DMA301m#S26DMA - {lecturerName}",
                Description = "Spring 2026 - Marketing Analytics (ACTIVE TERM - Cannot update weights)",
                LecturerId = lecturerId,
                TermId = spring2026.Id,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            };

            // SUMMER 2026 (FUTURE TERM) - MKT101
            var courseSummer2026 = new Course
            {
                Id = Guid.NewGuid(),
                CourseCodeId = mkt101Code!.Id,
                UniqueCode = "SU26MKT",
                Name = $"MKT101#SU26MKT - {lecturerName}",
                Description = "Summer 2026 - Marketing Fundamentals (FUTURE TERM - Can update weights)",
                LecturerId = lecturerId,
                TermId = summer2026.Id,
                Status = CourseStatus.Inactive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Courses.AddRange(courseFall2025, courseSpring2026, courseSummer2026);
            await context.SaveChangesAsync();
            logger.LogInformation("Created 3 test courses: Fall 2025 (PAST), Spring 2026 (ACTIVE), Summer 2026 (FUTURE)");

            // === 2. CREATE ENROLLMENTS ===
            var enrollmentFall = new CourseEnrollment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                StudentId = studentId,
                JoinedAt = DateTime.UtcNow.AddDays(-120),
                Status = EnrollmentStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-120),
                UpdatedAt = DateTime.UtcNow.AddDays(-120)
            };

            var enrollmentSpring = new CourseEnrollment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSpring2026.Id,
                StudentId = studentId,
                JoinedAt = DateTime.UtcNow.AddDays(-30),
                Status = EnrollmentStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            };

            var enrollmentSummer = new CourseEnrollment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSummer2026.Id,
                StudentId = studentId,
                JoinedAt = DateTime.UtcNow,
                Status = EnrollmentStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.CourseEnrollments.AddRange(enrollmentFall, enrollmentSpring, enrollmentSummer);
            await context.SaveChangesAsync();
            logger.LogInformation("Created 3 enrollments across different terms");

            // === 3. CREATE GROUP (for active Spring 2026 course) ===
            var group1 = new Group
            {
                Id = Guid.NewGuid(),
                CourseId = courseSpring2026.Id,
                Name = "Spring Analytics Team",
                Description = "Team for Spring 2026 analytics project",
                MaxMembers = 4,
                IsLocked = false,
                CreatedAt = DateTime.UtcNow.AddDays(-50),
                UpdatedAt = DateTime.UtcNow.AddDays(-50)
            };

            context.Groups.Add(group1);
            await context.SaveChangesAsync();
            logger.LogInformation("Created group: {GroupName}", group1.Name);

            // === 4. CREATE GROUP MEMBER (using EnrollmentId, not StudentId) ===
            var groupMember = new GroupMember
            {
                Id = Guid.NewGuid(),
                GroupId = group1.Id,
                EnrollmentId = enrollmentSpring.Id,
                IsLeader = true,
                Role = GroupMemberRole.Leader,
                JoinedAt = DateTime.UtcNow.AddDays(-50),
                CreatedAt = DateTime.UtcNow.AddDays(-50),
                UpdatedAt = DateTime.UtcNow.AddDays(-50)
            };

            context.GroupMembers.Add(groupMember);
            await context.SaveChangesAsync();
            logger.LogInformation("Added student to group as leader");

            // === 5. CREATE ASSIGNMENTS ===
            // ADS301m: Google Ads and SEO (7 assignments, 100% total weight)
            var ads1 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                TopicId = labTopic.Id,
                Title = "Google Ads Campaign Setup",
                Description = "Create and configure your first Google Ads search campaign with proper targeting, ad groups, and budget settings. Must include:\n- Campaign structure\n- Keyword selection\n- Ad copy variations\n- Budget and bidding strategy",
                Format = "Campaign setup screenshots + Configuration documentation (PDF)",
                StartDate = DateTime.UtcNow.AddDays(-50),
                DueDate = DateTime.UtcNow.AddDays(-43),
                Status = AssignmentStatus.Closed,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 20m, // LAB weight for ADS301m
                CreatedAt = DateTime.UtcNow.AddDays(-50),
                UpdatedAt = DateTime.UtcNow.AddDays(-50)
            };

            var ads2 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                TopicId = assignmentTopic.Id,
                Title = "Keyword Research Report",
                Description = "Conduct comprehensive keyword research for a product/service using Google Keyword Planner and document:\n- Search volume analysis\n- Competition levels\n- Cost per click (CPC)\n- Long-tail keyword opportunities",
                Format = "Keyword research spreadsheet (Excel/Google Sheets) + Analysis report (PDF)",
                StartDate = DateTime.UtcNow.AddDays(-45),
                DueDate = DateTime.UtcNow.AddDays(-31),
                Status = AssignmentStatus.Closed,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 50m, // Assignment weight for ADS301m
                CreatedAt = DateTime.UtcNow.AddDays(-45),
                UpdatedAt = DateTime.UtcNow.AddDays(-45)
            };

            var ads3 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                TopicId = labTopic.Id,
                Title = "On-Page SEO Audit",
                Description = "Perform technical SEO audit on a website covering:\n- Meta tags optimization\n- Header structure (H1-H6)\n- Internal linking strategy\n- Site speed analysis\n- Mobile optimization",
                Format = "SEO audit report (PDF) + Recommendations checklist",
                StartDate = DateTime.UtcNow.AddDays(-40),
                DueDate = DateTime.UtcNow.AddDays(-19),
                Status = AssignmentStatus.Closed,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 20m, // LAB weight for ADS301m
                CreatedAt = DateTime.UtcNow.AddDays(-40),
                UpdatedAt = DateTime.UtcNow.AddDays(-40)
            };

            var ads4 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                TopicId = assignmentTopic.Id,
                Title = "A/B Testing Ad Copy",
                Description = "Design and analyze A/B test results for ad copy variations:\n- Create 2-3 ad variations\n- Measure CTR and conversion rate\n- Analyze quality score impact\n- Document optimization insights",
                Format = "Test results spreadsheet + Analysis presentation (PowerPoint/PDF)",
                StartDate = DateTime.UtcNow.AddDays(-35),
                DueDate = DateTime.UtcNow.AddDays(-7),
                Status = AssignmentStatus.Closed,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 50m, // Assignment weight for ADS301m
                CreatedAt = DateTime.UtcNow.AddDays(-35),
                UpdatedAt = DateTime.UtcNow.AddDays(-35)
            };

            var ads5 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                TopicId = projectTopic.Id,
                Title = "Landing Page Optimization Project",
                Description = "Create and optimize a landing page for PPC campaigns:\n- Clear call-to-action (CTA)\n- Persuasive copywriting\n- Conversion tracking implementation\n- Mobile responsiveness\n- A/B testing elements",
                Format = "Live landing page URL + Optimization report (PDF) + Analytics setup",
                StartDate = DateTime.UtcNow.AddDays(-30),
                DueDate = DateTime.UtcNow.AddDays(5),
                Status = AssignmentStatus.Closed,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 30m, // Project weight for ADS301m
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            };

            var ads6 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                TopicId = assignmentTopic.Id,
                Title = "Competitor Ad Analysis",
                Description = "Analyze top 3 competitors' Google Ads strategies:\n- Keyword targeting\n- Ad copy and messaging\n- Ad extensions usage\n- Landing page analysis\n- Competitive positioning",
                Format = "Competitive analysis report (PDF) + Strategy recommendations",
                StartDate = DateTime.UtcNow.AddDays(-25),
                DueDate = DateTime.UtcNow.AddDays(17),
                Status = AssignmentStatus.Closed,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 50m, // Assignment weight for ADS301m
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            };

            var ads7 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                TopicId = assignmentTopic.Id,
                Title = "Campaign Performance Report",
                Description = "Compile final report analyzing campaign metrics:\n- Impressions and clicks\n- Click-through rate (CTR)\n- Conversions and conversion rate\n- Cost per acquisition (CPA)\n- Return on investment (ROI)\n- Optimization recommendations",
                Format = "Comprehensive campaign report (PDF) + Metrics dashboard",
                StartDate = DateTime.UtcNow.AddDays(-20),
                DueDate = DateTime.UtcNow.AddDays(29),
                Status = AssignmentStatus.Closed,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 50m, // Assignment weight for ADS301m
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            };

            // TEST ASSIGNMENT: Beauty Products Price Analysis with HTML Description
            var adsTest = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                TopicId = labTopic.Id,
                Title = "Beauty Products Market Analysis - Crawler Test",
                Description = @"<h1>Beauty & Skincare Product Data Collection Assignment</h1>
<p>This assignment focuses on collecting and analyzing <strong>beauty product pricing and information</strong> from Vietnamese online beauty retailers to understand <em>market trends and consumer preferences</em>.</p>

<h2>Objectives:</h2>
<ul>
    <li>Collect product data from <strong>beauty e-commerce platforms</strong> (Picare.vn)</li>
    <li>Extract product details including: <strong>brand, product name, category, price</strong></li>
    <li>Track <span style='color: red;'>promotional discounts</span> and skincare bundles</li>
    <li>Analyze pricing strategies across different beauty categories</li>
    <li>Identify trending skincare and body care products</li>
</ul>

<h2>Requirements:</h2>
<ol>
    <li><strong>Data Collection:</strong> Gather data for 10 beauty/body care products in the page</li>
    <li><strong>Product Information Extraction:</strong> Include detailed product specifications and descriptions</li>
    <li><strong>Price Comparison:</strong> Create comparative analysis of similar products across platforms</li>
    <li><strong>Category Analysis:</strong> Group products by category (moisturizers, cleansers, body care, etc.)</li>
    <li><strong>Brand Analysis:</strong> Identify top brands and their pricing strategies</li>
</ol>

<h3>Deliverables:</h3>
<table border='1'>
    <tr>
        <th>Item</th>
        <th>Format</th>
        <th>Points</th>
    </tr>
    <tr>
        <td>Raw Product Data</td>
        <td>Excel/CSV</td>
        <td>40</td>
    </tr>
    <tr>
        <td>Market Analysis Report</td>
        <td>Report (PDF)</td>
        <td>30</td>
    </tr>
    <tr>
        <td>Pricing Visualization</td>
        <td>PowerPoint/Tableau</td>
        <td>30</td>
    </tr>
</table>

<p><strong>Tip:</strong> Use our <a href='#crawler'>smart crawler tool</a> to automate beauty product data collection from Picare.vn!</p>
<p><em>Note:</em> Pay attention to product images, ratings, and customer reviews if available.</p>

<script>alert('This script should be removed');</script>
<style>.test { color: pink; }</style>",
                Format = "Excel spreadsheet with product data + Market analysis report (PDF) + Pricing visualization",
                StartDate = DateTime.UtcNow.AddDays(-10),
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = AssignmentStatus.Closed,
                IsGroupAssignment = true,
                MaxPoints = 100,
                WeightPercentageSnapshot = 20m, // LAB weight for ADS301m
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            };

            // DMA301m: Digital Marketing Analytics (3 assignments - 2 Assignment, 1 Project)
            var dma5 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSpring2026.Id,
                TopicId = assignmentTopic.Id,
                Title = "ROI & Attribution Modeling",
                Description = "Calculate marketing ROI and implement attribution:\n- ROI calculation for different channels\n- Attribution model comparison\n- Budget allocation recommendations\n- Cost per acquisition analysis\n- Lifetime value projections",
                Format = "ROI analysis spreadsheet + Attribution model report (PDF)",
                StartDate = DateTime.UtcNow.AddDays(-30),
                DueDate = DateTime.UtcNow.AddDays(5),
                Status = AssignmentStatus.Active,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 20m, // Assignment weight 40% ÷ 2 = 20% each
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            };

            var dma6 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSpring2026.Id,
                TopicId = projectTopic.Id,
                Title = "Marketing Analytics Dashboard",
                Description = "Build comprehensive marketing dashboard:\n- Integrate GA4, Google Ads, and social media\n- Real-time metrics visualization\n- Custom KPI tracking\n- Automated reporting\n- Executive summary view\n\nThis is a GROUP assignment - collaborate using the team workspace.",
                Format = "Live dashboard (Google Data Studio/Looker) + Documentation + Presentation",
                StartDate = DateTime.UtcNow.AddDays(-25),
                DueDate = DateTime.UtcNow.AddDays(20),
                Status = AssignmentStatus.Active,
                IsGroupAssignment = true,
                MaxPoints = 100,
                WeightPercentageSnapshot = 60m, // Project weight 60% (single project)
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            };

            // MKT101: Marketing Principles (7 assignments, 100% total weight)
            var mkt1 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSummer2026.Id,
                TopicId = assignmentTopic.Id,
                Title = "Market Segmentation Analysis",
                Description = "Identify and analyze target market segments:\n- Demographic segmentation\n- Psychographic profiles\n- Behavioral patterns\n- Geographic considerations\n- Segment prioritization",
                Format = "Market segmentation report (PDF) + Target persona profiles",
                StartDate = DateTime.UtcNow.AddDays(100),
                DueDate = DateTime.UtcNow.AddDays(114),
                Status = AssignmentStatus.Scheduled,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 100m, // Assignment weight for MKT101
                CreatedAt = DateTime.UtcNow.AddDays(-50),
                UpdatedAt = DateTime.UtcNow.AddDays(-50)
            };

            var mkt2 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSummer2026.Id,
                TopicId = assignmentTopic.Id,
                Title = "SWOT Analysis Report",
                Description = "Conduct comprehensive SWOT analysis for a brand:\n- Strengths assessment\n- Weaknesses identification\n- Opportunities exploration\n- Threats evaluation\n- Strategic recommendations",
                Format = "SWOT analysis matrix + Strategic plan (PDF)",
                StartDate = DateTime.UtcNow.AddDays(105),
                DueDate = DateTime.UtcNow.AddDays(119),
                Status = AssignmentStatus.Scheduled,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 100m, // Assignment weight for MKT101
                CreatedAt = DateTime.UtcNow.AddDays(-45),
                UpdatedAt = DateTime.UtcNow.AddDays(-45)
            };

            var mkt3 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSummer2026.Id,
                TopicId = labTopic.Id,
                Title = "Consumer Behavior Study",
                Description = "Research consumer decision-making process:\n- Purchase decision stages\n- Influencing factors\n- Brand perception analysis\n- Post-purchase behavior\n- Recommendations for marketers",
                Format = "Research report (PDF) + Survey results + Analysis presentation",
                StartDate = DateTime.UtcNow.AddDays(110),
                DueDate = DateTime.UtcNow.AddDays(131),
                Status = AssignmentStatus.Scheduled,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 100m, // Using Assignment weight for MKT101
                CreatedAt = DateTime.UtcNow.AddDays(-40),
                UpdatedAt = DateTime.UtcNow.AddDays(-40)
            };

            var mkt4 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSummer2026.Id,
                TopicId = assignmentTopic.Id,
                Title = "Marketing Mix (4Ps) Analysis",
                Description = "Analyze Product, Price, Place, and Promotion strategies:\n- Product positioning and features\n- Pricing strategy comparison\n- Distribution channels\n- Promotional tactics\n- Competitor benchmarking",
                Format = "4Ps analysis report (PDF) + Competitive comparison matrix",
                StartDate = DateTime.UtcNow.AddDays(115),
                DueDate = DateTime.UtcNow.AddDays(143),
                Status = AssignmentStatus.Scheduled,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 100m, // Assignment weight for MKT101
                CreatedAt = DateTime.UtcNow.AddDays(-35),
                UpdatedAt = DateTime.UtcNow.AddDays(-35)
            };

            var mkt5 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSummer2026.Id,
                TopicId = labTopic.Id,
                Title = "Brand Positioning Workshop",
                Description = "Develop brand positioning strategy:\n- Positioning statement creation\n- Perceptual mapping\n- Unique value proposition\n- Brand differentiation\n- Target audience alignment",
                Format = "Brand positioning document (PDF) + Perceptual map + Presentation",
                StartDate = DateTime.UtcNow.AddDays(120),
                DueDate = DateTime.UtcNow.AddDays(155),
                Status = AssignmentStatus.Scheduled,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 100m, // Using Assignment weight for MKT101
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            };

            var mkt6 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSummer2026.Id,
                TopicId = assignmentTopic.Id,
                Title = "Competitive Analysis",
                Description = "Identify and analyze competitive landscape:\n- Direct and indirect competitors\n- Competitive advantages\n- Market positioning\n- Marketing strategies comparison\n- Opportunity gaps",
                Format = "Competitive analysis report (PDF) + Strategy matrix",
                StartDate = DateTime.UtcNow.AddDays(125),
                DueDate = DateTime.UtcNow.AddDays(167),
                Status = AssignmentStatus.Scheduled,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 100m, // Assignment weight for MKT101
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            };

            var mkt7 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSummer2026.Id,
                TopicId = projectTopic.Id,
                Title = "Marketing Plan Project",
                Description = "Develop comprehensive marketing plan:\n- Executive summary\n- Situation analysis\n- Marketing objectives\n- Strategy and tactics\n- Budget allocation\n- Metrics and KPIs\n- Implementation timeline",
                Format = "Complete marketing plan (PDF) + Budget spreadsheet + Presentation",
                StartDate = DateTime.UtcNow.AddDays(130),
                DueDate = DateTime.UtcNow.AddDays(180),
                Status = AssignmentStatus.Scheduled,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 100m, // Using Assignment weight for MKT101
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            };

            // === ADDITIONAL NON-OVERDUE ASSIGNMENTS FOR TESTING ===
            // ADS301m - Additional LAB assignments (to test weight distribution with multiple LABs)
            var ads8 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                TopicId = labTopic.Id,
                Title = "Google Search Console Setup",
                Description = "Set up Google Search Console for website:\n- Verify domain ownership\n- Submit XML sitemap\n- Monitor search performance\n- Review crawl errors",
                Format = "Setup guide (PDF) + Screenshots of configuration",
                StartDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14),
                Status = AssignmentStatus.Closed,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 20m, // LAB weight for ADS301m
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var ads9 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                TopicId = labTopic.Id,
                Title = "Google Analytics 4 Implementation",
                Description = "Implement GA4 tracking:\n- Set up data streams\n- Configure conversion events\n- Create custom dimensions\n- Test tracking implementation",
                Format = "Implementation report (PDF) + Event tracking documentation",
                StartDate = DateTime.UtcNow.AddDays(1),
                DueDate = DateTime.UtcNow.AddDays(21),
                Status = AssignmentStatus.Closed,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 20m, // LAB weight for ADS301m
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var ads10 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseFall2025.Id,
                TopicId = assignmentTopic.Id,
                Title = "Backlink Strategy Analysis",
                Description = "Analyze backlink profile and develop acquisition strategy:\n- Audit existing backlinks\n- Identify high-authority link opportunities\n- Create outreach plan\n- Document link building tactics",
                Format = "Backlink analysis report (PDF) + Outreach template",
                StartDate = DateTime.UtcNow.AddDays(2),
                DueDate = DateTime.UtcNow.AddDays(28),
                Status = AssignmentStatus.Closed,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 50m, // Assignment weight for ADS301m
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // DMA301m - Additional assignment (completing the 2 assignments needed)
            var dma7 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSpring2026.Id,
                TopicId = assignmentTopic.Id,
                Title = "Customer Journey Mapping",
                Description = "Create customer journey map with analytics touchpoints:\n- Awareness stage metrics\n- Consideration phase tracking\n- Conversion funnel analysis\n- Post-purchase engagement",
                Format = "Journey map visualization + Analytics setup guide",
                StartDate = DateTime.UtcNow.AddDays(-20),
                DueDate = DateTime.UtcNow.AddDays(15),
                Status = AssignmentStatus.Active,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 20m, // Assignment weight 40% ÷ 2 = 20% each
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            };

            // MKT101 - Additional assignments (to test 100% Assignment weight distribution)
            var mkt8 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSummer2026.Id,
                TopicId = assignmentTopic.Id,
                Title = "SWOT Analysis Exercise",
                Description = "Conduct SWOT analysis for a real company:\n- Identify Strengths and Weaknesses\n- Analyze Opportunities and Threats\n- Strategic recommendations\n- Competitive positioning",
                Format = "SWOT analysis report (PDF) + Strategic recommendations",
                StartDate = DateTime.UtcNow.AddDays(151),
                DueDate = DateTime.UtcNow.AddDays(165),
                Status = AssignmentStatus.Scheduled,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 100m, // Assignment weight for MKT101
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var mkt9 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSummer2026.Id,
                TopicId = assignmentTopic.Id,
                Title = "Product Positioning Statement",
                Description = "Create positioning statement for new product:\n- Target audience definition\n- Point of difference\n- Reasons to believe\n- Brand essence",
                Format = "Positioning statement document (PDF) + Market research",
                StartDate = DateTime.UtcNow.AddDays(152),
                DueDate = DateTime.UtcNow.AddDays(172),
                Status = AssignmentStatus.Scheduled,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 100m, // Assignment weight for MKT101
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var mkt10 = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseSummer2026.Id,
                TopicId = assignmentTopic.Id,
                Title = "Brand Audit Analysis",
                Description = "Perform comprehensive brand audit:\n- Brand awareness metrics\n- Brand perception study\n- Competitive brand comparison\n- Brand equity assessment",
                Format = "Brand audit report (PDF) + Presentation",
                StartDate = DateTime.UtcNow.AddDays(153),
                DueDate = DateTime.UtcNow.AddDays(175),
                Status = AssignmentStatus.Scheduled,
                IsGroupAssignment = false,
                MaxPoints = 100,
                WeightPercentageSnapshot = 100m, // Assignment weight for MKT101
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Assignments.AddRange(
                ads1, ads2, ads3, ads4, ads5, ads6, ads7, adsTest, ads8, ads9, ads10,
                dma5, dma6, dma7,
                mkt1, mkt2, mkt3, mkt4, mkt5, mkt6, mkt7, mkt8, mkt9, mkt10
            );
            await context.SaveChangesAsync();
            logger.LogInformation("Created 23 digital marketing assignments across 3 courses (ADS:10, DMA:3, MKT:10)");

            // === 6. LINK GROUP TO ASSIGNMENT (assign dma6 Marketing Analytics Dashboard to group1) ===
            group1.AssignmentId = dma6.Id;
            context.Groups.Update(group1);
            await context.SaveChangesAsync();
            logger.LogInformation("Assigned '{AssignmentTitle}' to group '{GroupName}'", dma6.Title, group1.Name);

            logger.LogInformation("========================================");
            logger.LogInformation("Digital Marketing Classroom Seeding Completed!");
            logger.LogInformation("========================================");
            logger.LogInformation("Test Account: student@crawldata.com / Student@123456");
            logger.LogInformation("Enrolled Courses: 3");
            logger.LogInformation("  - ADS301m: Google Ads and SEO (10 assignments: 5 LAB, 4 Assignment, 1 Project)");
            logger.LogInformation("  - DMA301m: Digital Marketing Analytics (3 assignments: 2 Assignment, 1 Project)");
            logger.LogInformation("  - MKT101: Marketing Principles (10 assignments: all Assignment type)");
            logger.LogInformation("Total Assignments: 23 (3 DMA active assignments for Spring 2026)");
            logger.LogInformation("========================================");
            logger.LogInformation("Weight Distribution Test Data:");
            logger.LogInformation("  - ADS301m: LAB(30%) ÷ 5 assignments = 6% each");
            logger.LogInformation("  - ADS301m: Assignment(30%) ÷ 4 = 7.5% each");
            logger.LogInformation("  - ADS301m: Project(40%) ÷ 1 = 40%");
            logger.LogInformation("  - DMA301m: Assignment(40%) ÷ 2 = 20% each");
            logger.LogInformation("  - DMA301m: Project(60%) ÷ 1 = 60%");
            logger.LogInformation("  - MKT101: Assignment(100%) ÷ 10 = 10% each");
            logger.LogInformation("========================================");
            logger.LogInformation("Test Assignment: 'Beauty Products Market Analysis - Crawler Test'");
            logger.LogInformation("  - URL: https://picare.vn/danh-muc/cham-soc-co-the (Vietnamese beauty products)");
            logger.LogInformation("  - Contains HTML tags (h1, p, strong, ul, table, script, style)");
            logger.LogInformation("  - Perfect for testing assignment context + HTML stripping");
            logger.LogInformation("========================================");
            logger.LogInformation("Group Membership: Analytics Insights Team (Leader)");
            logger.LogInformation("========================================");
            logger.LogInformation("Ready for digital marketing education platform!");
            logger.LogInformation("========================================");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding classroom entities");
            // Don't throw - seeding is optional, service can still run
        }
    }

    /// <summary>
    /// Seeds TopicWeights for testing - Configure standard weights for different topics
    /// </summary>
    private async Task SeedTopicWeightsAsync(ClassroomDbContext context, ILogger<DatabaseSeedingHostedService> logger)
    {
        try
        {
            // Fixed GUID for Staff user (must match UserService seed data)
            var staffId = Guid.Parse("30000000-0000-0000-0000-000000000001");

            // Clear existing TopicWeights to ensure clean seeding
            var existingWeights = await context.TopicWeights.ToListAsync();
            if (existingWeights.Any())
            {
                context.TopicWeights.RemoveRange(existingWeights);
                await context.SaveChangesAsync();
                logger.LogInformation("Cleared {Count} existing TopicWeights for clean seeding", existingWeights.Count);
            }

            logger.LogInformation("Seeding TopicWeights for testing...");

            // Get CourseCodes
            var ads301Code = await context.CourseCodes.FirstOrDefaultAsync(c => c.Code == "ADS301m");
            var dma301Code = await context.CourseCodes.FirstOrDefaultAsync(c => c.Code == "DMA301m");
            var mkt101Code = await context.CourseCodes.FirstOrDefaultAsync(c => c.Code == "MKT101");

            // Get Topics
            var labTopic = await context.Topics.FirstOrDefaultAsync(t => t.Name == "LAB");
            var assignmentTopic = await context.Topics.FirstOrDefaultAsync(t => t.Name == "Assignment");
            var projectTopic = await context.Topics.FirstOrDefaultAsync(t => t.Name == "Project");

            if (ads301Code == null || dma301Code == null || mkt101Code == null ||
                labTopic == null || assignmentTopic == null || projectTopic == null)
            {
                logger.LogWarning("Required CourseCodes or Topics not found, skipping TopicWeight seeding");
                return;
            }

            // Hardcoded GUIDs for ADS301m weights (for easier testing and history seeding)
            var adsLabWeightId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var adsAssignmentWeightId = Guid.Parse("11111111-2222-2222-2222-222222222222");
            var adsProjectWeightId = Guid.Parse("11111111-3333-3333-3333-333333333333");
            
            var topicWeights = new List<TopicWeight>
            {
                // === ADS301m: Google Ads and SEO ===
                // SCENARIO: These weights were created in early Fall 2025, then updated mid-term
                // Assignment snapshots use OLD values (20%, 50%, 30%)
                // Current TopicWeights show NEW values (25%, 45%, 30%)
                // This makes it easy to test: GET TopicWeights will show 25%/45%/30%, assignments show 20%/50%/30%
                
                // LAB: 25% (updated from initial 20%)
                new TopicWeight
                {
                    Id = adsLabWeightId,
                    TopicId = labTopic.Id,
                    CourseCodeId = ads301Code.Id,
                    SpecificCourseId = null,
                    WeightPercentage = 25m,
                    Description = "LAB assignments for ADS301m - UPDATED mid Fall 2025",
                    ConfiguredBy = staffId,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4), // Created 4 months ago (early Fall 2025)
                    UpdatedAt = DateTime.UtcNow.AddMonths(-2) // Updated 2 months ago (mid Fall 2025)
                },
                // Assignment: 45% (updated from initial 50%)
                new TopicWeight
                {
                    Id = adsAssignmentWeightId,
                    TopicId = assignmentTopic.Id,
                    CourseCodeId = ads301Code.Id,
                    SpecificCourseId = null,
                    WeightPercentage = 45m,
                    Description = "Regular assignments for ADS301m - UPDATED mid Fall 2025",
                    ConfiguredBy = staffId,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4), // Created 4 months ago (early Fall 2025)
                    UpdatedAt = DateTime.UtcNow.AddMonths(-2) // Updated 2 months ago (mid Fall 2025)
                },
                // Project: 30% (unchanged)
                new TopicWeight
                {
                    Id = adsProjectWeightId,
                    TopicId = projectTopic.Id,
                    CourseCodeId = ads301Code.Id,
                    SpecificCourseId = null,
                    WeightPercentage = 30m,
                    Description = "Final project for ADS301m - Unchanged since creation",
                    ConfiguredBy = staffId,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4), // Created 4 months ago (early Fall 2025)
                    UpdatedAt = DateTime.UtcNow.AddMonths(-4) // No updates
                },

                // === DMA301m: Digital Marketing Analytics ===
                // SCENARIO: These weights were created in Fall 2025 (PAST), then updated once
                // Spring 2026 (ACTIVE) is now using the latest config from Fall 2025
                // This demonstrates: "create some weight history for the past term which the active term is currently using the newest update"
                
                // Assignment: 40% (updated from initial 30%)
                new TopicWeight
                {
                    Id = Guid.NewGuid(),
                    TopicId = assignmentTopic.Id,
                    CourseCodeId = dma301Code.Id,
                    SpecificCourseId = null,
                    WeightPercentage = 40m,
                    Description = "Regular assignments for DMA301m - UPDATED in Fall 2025 term",
                    ConfiguredBy = staffId,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6), // Created 6 months ago (Fall 2025)
                    UpdatedAt = DateTime.UtcNow.AddMonths(-5) // Updated 5 months ago (still in Fall 2025)
                },
                // Project: 60% (updated from initial 40%)
                new TopicWeight
                {
                    Id = Guid.NewGuid(),
                    TopicId = projectTopic.Id,
                    CourseCodeId = dma301Code.Id,
                    SpecificCourseId = null,
                    WeightPercentage = 60m,
                    Description = "Group project for DMA301m - UPDATED in Fall 2025 term",
                    ConfiguredBy = staffId,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6), // Created 6 months ago (Fall 2025)
                    UpdatedAt = DateTime.UtcNow.AddMonths(-5) // Updated 5 months ago (still in Fall 2025)
                },

                // === MKT101: Marketing Principles ===
                // SCENARIO: Future term (Summer 2026) - CAN be updated
                // Assignment: 100% (no LAB or Project yet)
                new TopicWeight
                {
                    Id = Guid.NewGuid(),
                    TopicId = assignmentTopic.Id,
                    CourseCodeId = mkt101Code.Id,
                    SpecificCourseId = null,
                    WeightPercentage = 100m,
                    Description = "Regular assignments for MKT101 - Market analysis (Future term - can modify)",
                    ConfiguredBy = staffId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.TopicWeights.AddRange(topicWeights);
            await context.SaveChangesAsync();

            logger.LogInformation("TopicWeights seeded successfully:");
            logger.LogInformation("  - ADS301m (Fall 2025 PAST - CAN UPDATE): LAB=25%, Assignment=45%, Project=30% (UPDATED from 20%/50%/30%)");
            logger.LogInformation("  - DMA301m (Spring 2026 ACTIVE - CANNOT UPDATE): Assignment=40%, Project=60% (Updated from Fall 2025)");
            logger.LogInformation("  - MKT101 (Summer 2026 FUTURE - CAN UPDATE): Assignment=100%");
            logger.LogInformation("  - Note: ADS301m assignments still have OLD snapshot values (20%/50%/30%) for easy testing");
            
            // === SEED HISTORY RECORDS ===
            // Demonstrate weight history for both ADS301m (PAST) and DMA301m (ACTIVE using PAST config)
            await SeedTopicWeightHistoryAsync(context, logger, topicWeights, staffId, 
                ads301Code.Id, dma301Code.Id, labTopic.Id, assignmentTopic.Id, projectTopic.Id,
                adsLabWeightId, adsAssignmentWeightId, adsProjectWeightId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding TopicWeights");
            // Don't throw - seeding is optional
        }
    }
    
    /// <summary>
    /// Seeds TopicWeightHistory to demonstrate audit trail
    /// Shows how weights were created and updated in Fall 2025 (PAST term)
    /// - ADS301m: Updated mid-term (assignments have OLD snapshots, TopicWeights have NEW values)
    /// - DMA301m: Updated in Fall 2025, now being used by Spring 2026 (ACTIVE term)
    /// </summary>
    private async Task SeedTopicWeightHistoryAsync(
        ClassroomDbContext context, 
        ILogger<DatabaseSeedingHostedService> logger,
        List<TopicWeight> topicWeights,
        Guid staffId,
        Guid ads301CodeId,
        Guid dma301CodeId,
        Guid labTopicId,
        Guid assignmentTopicId,
        Guid projectTopicId,
        Guid adsLabWeightId,
        Guid adsAssignmentWeightId,
        Guid adsProjectWeightId)
    {
        try
        {
            logger.LogInformation("Seeding TopicWeightHistory for ADS301m and DMA301m (demonstrates audit trail)...");
            
            // Get Fall 2025 term (where the updates happened)
            var fall2025 = await context.Terms.FirstOrDefaultAsync(t => t.Name == "Fall 2025");
            if (fall2025 == null)
            {
                logger.LogWarning("Fall 2025 term not found, skipping history seeding");
                return;
            }
            
            // Find the DMA301m weights we just created
            var dma301Assignment = topicWeights.FirstOrDefault(tw => 
                tw.CourseCodeId == dma301CodeId && tw.TopicId == assignmentTopicId);
            var dma301Project = topicWeights.FirstOrDefault(tw => 
                tw.CourseCodeId == dma301CodeId && tw.TopicId == projectTopicId);
            
            if (dma301Assignment == null || dma301Project == null)
            {
                logger.LogWarning("DMA301m weights not found, skipping DMA301m history seeding");
                return;
            }
            
            var historyRecords = new List<TopicWeightHistory>
            {
                // ========================================
                // ADS301m HISTORY (Fall 2025 PAST TERM)
                // ========================================
                // Demonstrates: Assignments have OLD snapshots (20%/50%/30%)
                // but TopicWeights show NEW values (25%/45%/30%)
                
                // === LAB WEIGHT HISTORY ===
                // 1. Initial creation: 20%
                new TopicWeightHistory
                {
                    Id = Guid.NewGuid(),
                    TopicWeightId = adsLabWeightId,
                    TopicId = labTopicId,
                    CourseCodeId = ads301CodeId,
                    TermId = fall2025.Id,
                    TermName = fall2025.Name,
                    OldWeightPercentage = null,
                    NewWeightPercentage = 20m,
                    ModifiedBy = staffId,
                    ModifiedAt = DateTime.UtcNow.AddMonths(-4), // 4 months ago (early Fall 2025)
                    Action = Domain.Enums.WeightHistoryAction.Created,
                    ChangeReason = "Initial configuration for ADS301m Fall 2025",
                    AffectedTerms = "Fall 2025"
                },
                // 2. Update: 20% -> 25%
                new TopicWeightHistory
                {
                    Id = Guid.NewGuid(),
                    TopicWeightId = adsLabWeightId,
                    TopicId = labTopicId,
                    CourseCodeId = ads301CodeId,
                    TermId = fall2025.Id,
                    TermName = fall2025.Name,
                    OldWeightPercentage = 20m,
                    NewWeightPercentage = 25m,
                    ModifiedBy = staffId,
                    ModifiedAt = DateTime.UtcNow.AddMonths(-2), // 2 months ago (mid Fall 2025)
                    Action = Domain.Enums.WeightHistoryAction.Updated,
                    ChangeReason = "Increased LAB weight to emphasize hands-on practice",
                    AffectedTerms = "Fall 2025"
                },
                
                // === ASSIGNMENT WEIGHT HISTORY ===
                // 1. Initial creation: 50%
                new TopicWeightHistory
                {
                    Id = Guid.NewGuid(),
                    TopicWeightId = adsAssignmentWeightId,
                    TopicId = assignmentTopicId,
                    CourseCodeId = ads301CodeId,
                    TermId = fall2025.Id,
                    TermName = fall2025.Name,
                    OldWeightPercentage = null,
                    NewWeightPercentage = 50m,
                    ModifiedBy = staffId,
                    ModifiedAt = DateTime.UtcNow.AddMonths(-4), // 4 months ago (early Fall 2025)
                    Action = Domain.Enums.WeightHistoryAction.Created,
                    ChangeReason = "Initial configuration for ADS301m Fall 2025",
                    AffectedTerms = "Fall 2025"
                },
                // 2. Update: 50% -> 45%
                new TopicWeightHistory
                {
                    Id = Guid.NewGuid(),
                    TopicWeightId = adsAssignmentWeightId,
                    TopicId = assignmentTopicId,
                    CourseCodeId = ads301CodeId,
                    TermId = fall2025.Id,
                    TermName = fall2025.Name,
                    OldWeightPercentage = 50m,
                    NewWeightPercentage = 45m,
                    ModifiedBy = staffId,
                    ModifiedAt = DateTime.UtcNow.AddMonths(-2), // 2 months ago (mid Fall 2025)
                    Action = Domain.Enums.WeightHistoryAction.Updated,
                    ChangeReason = "Reduced assignment weight to rebalance with LAB increase",
                    AffectedTerms = "Fall 2025"
                },
                
                // === PROJECT WEIGHT HISTORY ===
                // 1. Initial creation: 30% (unchanged)
                new TopicWeightHistory
                {
                    Id = Guid.NewGuid(),
                    TopicWeightId = adsProjectWeightId,
                    TopicId = projectTopicId,
                    CourseCodeId = ads301CodeId,
                    TermId = fall2025.Id,
                    TermName = fall2025.Name,
                    OldWeightPercentage = null,
                    NewWeightPercentage = 30m,
                    ModifiedBy = staffId,
                    ModifiedAt = DateTime.UtcNow.AddMonths(-4), // 4 months ago (early Fall 2025)
                    Action = Domain.Enums.WeightHistoryAction.Created,
                    ChangeReason = "Initial configuration for ADS301m Fall 2025",
                    AffectedTerms = "Fall 2025"
                },
                
                // ========================================
                // DMA301m HISTORY (Fall 2025 -> Spring 2026)
                // ========================================
                // === ASSIGNMENT WEIGHT HISTORY ===
                // 1. Initial creation: 50%
                new TopicWeightHistory
                {
                    Id = Guid.NewGuid(),
                    TopicWeightId = dma301Assignment.Id,
                    TopicId = assignmentTopicId,
                    CourseCodeId = dma301CodeId,
                    TermId = fall2025.Id,
                    TermName = fall2025.Name,
                    OldWeightPercentage = null, // null = creation
                    NewWeightPercentage = 50m,
                    ModifiedBy = staffId,
                    ModifiedAt = DateTime.UtcNow.AddMonths(-6), // 6 months ago (during Fall 2025)
                    Action = Domain.Enums.WeightHistoryAction.Created,
                    ChangeReason = "Initial configuration for DMA301m Fall 2025",
                    AffectedTerms = "Fall 2025"
                },
                // 2. Update: 50% -> 40%
                new TopicWeightHistory
                {
                    Id = Guid.NewGuid(),
                    TopicWeightId = dma301Assignment.Id,
                    TopicId = assignmentTopicId,
                    CourseCodeId = dma301CodeId,
                    TermId = fall2025.Id,
                    TermName = fall2025.Name,
                    OldWeightPercentage = 50m,
                    NewWeightPercentage = 40m,
                    ModifiedBy = staffId,
                    ModifiedAt = DateTime.UtcNow.AddMonths(-5), // 5 months ago (still in Fall 2025)
                    Action = Domain.Enums.WeightHistoryAction.Updated,
                    ChangeReason = "Reduced assignment weight to give more emphasis to project (rebalanced to total 100%)",
                    AffectedTerms = "Fall 2025"
                },
                
                // === PROJECT WEIGHT HISTORY ===
                // 1. Initial creation: 50%
                new TopicWeightHistory
                {
                    Id = Guid.NewGuid(),
                    TopicWeightId = dma301Project.Id,
                    TopicId = projectTopicId,
                    CourseCodeId = dma301CodeId,
                    TermId = fall2025.Id,
                    TermName = fall2025.Name,
                    OldWeightPercentage = null, // null = creation
                    NewWeightPercentage = 50m,
                    ModifiedBy = staffId,
                    ModifiedAt = DateTime.UtcNow.AddMonths(-6), // 6 months ago (during Fall 2025)
                    Action = Domain.Enums.WeightHistoryAction.Created,
                    ChangeReason = "Initial configuration for DMA301m Fall 2025",
                    AffectedTerms = "Fall 2025"
                },
                // 2. Update: 50% -> 60%
                new TopicWeightHistory
                {
                    Id = Guid.NewGuid(),
                    TopicWeightId = dma301Project.Id,
                    TopicId = projectTopicId,
                    CourseCodeId = dma301CodeId,
                    TermId = fall2025.Id,
                    TermName = fall2025.Name,
                    OldWeightPercentage = 50m,
                    NewWeightPercentage = 60m,
                    ModifiedBy = staffId,
                    ModifiedAt = DateTime.UtcNow.AddMonths(-5), // 5 months ago (still in Fall 2025)
                    Action = Domain.Enums.WeightHistoryAction.Updated,
                    ChangeReason = "Increased project weight to emphasize practical analytics skills (rebalanced to total 100%)",
                    AffectedTerms = "Fall 2025"
                }
            };
            
            context.TopicWeightHistories.AddRange(historyRecords);
            await context.SaveChangesAsync();
            
            logger.LogInformation("TopicWeightHistory seeded successfully:");
            logger.LogInformation("  - ADS301m LAB: Created at 20% -> Updated to 25% (Fall 2025)");
            logger.LogInformation("  - ADS301m Assignment: Created at 50% -> Updated to 45% (Fall 2025)");
            logger.LogInformation("  - ADS301m Project: Created at 30% (unchanged)");
            logger.LogInformation("  - ADS301m OLD total: 20% + 50% + 30% = 100%, NEW total: 25% + 45% + 30% = 100%");
            logger.LogInformation("  - ADS301m Assignments still have OLD snapshots (20%/50%/30%) for easy testing");
            logger.LogInformation("");
            logger.LogInformation("  - DMA301m Assignment: Created at 50% -> Updated to 40% (Fall 2025)");
            logger.LogInformation("  - DMA301m Project: Created at 50% -> Updated to 60% (Fall 2025)");
            logger.LogInformation("  - DMA301m OLD total: 50% + 50% = 100%, NEW total: 40% + 60% = 100%");
            logger.LogInformation("  - Spring 2026 (ACTIVE) is now using DMA301m weights from Fall 2025");
            logger.LogInformation("  - Cannot modify DMA301m now because Spring 2026 is ACTIVE");
            logger.LogInformation("  - CAN modify ADS301m because Fall 2025 is PAST");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding TopicWeightHistory");
            // Don't throw - seeding is optional
        }
    }
}

