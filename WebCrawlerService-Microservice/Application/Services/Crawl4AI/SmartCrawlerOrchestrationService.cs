using System.Globalization;
using System.Text;
using System.Text.Json;
using MediatR;
using WebCrawlerService.Domain.Entities;
using WebCrawlerService.Domain.Enums;
using WebCrawlerService.Domain.Events;
using WebCrawlerService.Domain.Interfaces;
using WebCrawlerService.Domain.Models;
using WebCrawlerService.Domain.Models.Crawl4AI;
using WebCrawlerService.Infrastructure.Contexts;
using WebCrawlerService.Application.DTOs.DataVisualization;

namespace WebCrawlerService.Application.Services.Crawl4AI;

/// <summary>
/// Orchestrates intelligent crawling workflow
/// </summary>
public class SmartCrawlerOrchestrationService : ISmartCrawlerOrchestrationService
{
    private static readonly string[] SimilarProductKeywords =
    {
        "tương tự", "google", "link sản phẩm", "similar product", "find similar", "link tuong tu",
        "mua ở đâu", "giống trên google", "google link", "link tương tự"
    };

    private static readonly string[] ProductNameFields = { "productName", "product_name", "name", "title", "itemName" };
    private static readonly string[] BrandFields = { "brand", "manufacturer", "seller", "vendor" };
    private static readonly string[] PriceFields = { "price", "salePrice", "priceValue", "price_text", "priceText" };
    private static readonly string[] CurrencyFields = { "currency", "currencyCode", "priceCurrency" };
    private static readonly string[] VariantFields = { "variant", "color", "model" };

    private const int MaxProductsFromCrawl = 12;
    private const int MaxProductsForGoogleSearch = 3;
    private const int MaxGoogleLinksPerProduct = 3;

    private readonly IPromptAnalyzerService _promptAnalyzer;
    private readonly ICrawl4AIClientService _crawl4AIClient;
    private readonly IRepository<CrawlJob> _crawlJobRepo;
    private readonly IRepository<PromptHistory> _promptHistoryRepo;
    private readonly IRepository<NavigationStrategy> _navigationStrategyRepo;
    private readonly IRepository<CrawlResult> _crawlResultRepo;
    private readonly CrawlerDbContext _context;
    private readonly ILogger<SmartCrawlerOrchestrationService> _logger;
    private readonly IMediator _mediator;
    private readonly IUserQuotaService _quotaService;
    private readonly IGoogleProductSearchService _googleProductSearch;

    public SmartCrawlerOrchestrationService(
        IPromptAnalyzerService promptAnalyzer,
        ICrawl4AIClientService crawl4AIClient,
        IRepository<CrawlJob> crawlJobRepo,
        IRepository<PromptHistory> promptHistoryRepo,
        IRepository<NavigationStrategy> navigationStrategyRepo,
        IRepository<CrawlResult> crawlResultRepo,
        CrawlerDbContext context,
        ILogger<SmartCrawlerOrchestrationService> logger,
        IMediator mediator,
        IUserQuotaService quotaService,
        IGoogleProductSearchService googleProductSearch)
    {
        _promptAnalyzer = promptAnalyzer;
        _crawl4AIClient = crawl4AIClient;
        _crawlJobRepo = crawlJobRepo;
        _promptHistoryRepo = promptHistoryRepo;
        _navigationStrategyRepo = navigationStrategyRepo;
        _crawlResultRepo = crawlResultRepo;
        _context = context;
        _logger = logger;
        _mediator = mediator;
        _quotaService = quotaService;
        _googleProductSearch = googleProductSearch;
    }

    public async Task<CrawlJobResult> ExecuteIntelligentCrawlAsync(
        Guid userId,
        string prompt,
        string url,
        Guid? assignmentId = null,
        Guid? groupId = null,
        Guid? conversationThreadId = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var jobId = Guid.NewGuid();
        const int requiredUnits = 1;
        const string quotaExceededMessage = "Quota exceeded. Please upgrade your plan to continue crawling.";

        try
        {
            _logger.LogInformation("Starting intelligent crawl for user {UserId}. URL: {Url}, Prompt: {Prompt}, AssignmentId: {AssignmentId}, GroupId: {GroupId}, ConversationId: {ConversationId}",
                userId, url, prompt, assignmentId, groupId, conversationThreadId);

            if (!await _quotaService.CheckQuotaAsync(userId, requiredUnits, cancellationToken))
            {
                _logger.LogWarning("User {UserId} has insufficient quota for crawl request", userId);

                var failedJob = new CrawlJob
                {
                    Id = jobId,
                    UserId = userId,
                    Urls = new[] { url },
                    CrawlerType = CrawlerType.Crawl4AI,
                    Status = JobStatus.Failed,
                    ErrorMessage = quotaExceededMessage,
                    FailedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    UserPrompt = prompt,
                    AssignmentId = assignmentId,
                    GroupId = groupId,
                    ConversationThreadId = conversationThreadId,
                    CreatedAt = DateTime.UtcNow
                };

                await _crawlJobRepo.AddAsync(failedJob);
                await _context.SaveChangesAsync();

                await _mediator.Publish(new CrawlerFailedEvent(failedJob, quotaExceededMessage, url), cancellationToken);

                return new CrawlJobResult
                {
                    JobId = failedJob.Id,
                    Success = false,
                    ResultCount = 0,
                    ExecutionTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                    ErrorMessage = quotaExceededMessage
                };
            }

            await _quotaService.DeductQuotaAsync(userId, requiredUnits, jobId, cancellationToken);

            // Step 1: Analyze prompt to understand intent
            var analysis = await _promptAnalyzer.AnalyzePromptAsync(prompt, url, cancellationToken);
            _logger.LogInformation("Prompt analysis complete. Intent: {Intent}, Navigation required: {RequiresNav}",
                analysis.Intent, analysis.RequiresNavigation);

            // Step 2: Save prompt history
            var promptHistory = new PromptHistory
            {
                UserId = userId,
                PromptText = prompt,
                Type = PromptType.Crawl,
                ProcessedAt = DateTime.UtcNow,
                ResponseText = JsonSerializer.Serialize(analysis)
            };
            await _promptHistoryRepo.AddAsync(promptHistory);
            await _context.SaveChangesAsync(); // Save 1: Get PromptHistory.Id

            // Step 3: Create crawl job
            var job = new CrawlJob
            {
                Id = jobId,
                UserId = userId,
                Urls = new[] { url },
                CrawlerType = CrawlerType.Crawl4AI,
                Status = JobStatus.InProgress,
                Priority = Priority.Normal,
                SessionType = NavigationSessionType.Continuous,
                ParentPromptId = promptHistory.Id,
                UserPrompt = prompt,
                AssignmentId = assignmentId,
                GroupId = groupId,
                ConversationThreadId = conversationThreadId,
                CreatedAt = DateTime.UtcNow
            };

            await _crawlJobRepo.AddAsync(job);
            await _context.SaveChangesAsync(); // Save 2: Get CrawlJob.Id
            _logger.LogInformation("Created crawl job {JobId} for conversation {ConversationId}",
                job.Id, conversationThreadId);

            // Step 4: Check for existing navigation strategy
            NavigationStrategy? existingStrategy = null;
            if (analysis.RequiresNavigation)
            {
                var domain = new Uri(url).Host;
                existingStrategy = await FindMatchingStrategyAsync(domain, prompt);

                if (existingStrategy != null)
                {
                    _logger.LogInformation("Found existing navigation strategy {StrategyId} with success rate {SuccessRate}",
                        existingStrategy.Id, existingStrategy.SuccessRate);
                    job.NavigationStrategyId = existingStrategy.Id;
                    await _context.SaveChangesAsync();
                }
            }

            // Step 5: Execute crawl via crawl4ai
            List<NavigationStep>? steps = null;
            if (existingStrategy != null)
            {
                steps = JsonSerializer.Deserialize<List<NavigationStep>>(existingStrategy.NavigationStepsJson);
            }

            var crawlResponse = await _crawl4AIClient.IntelligentCrawlAsync(
                url,
                prompt,
                steps,
                jobId: job.Id.ToString(),
                userId: userId.ToString(),
                cancellationToken
            );

            _logger.LogInformation("crawl4ai execution complete. Success: {Success}, Items extracted: {Count}",
                crawlResponse.Success, crawlResponse.Data.Count);


                // Step 6: Save results and set job status
                if (crawlResponse.Success && crawlResponse.Data != null && crawlResponse.Data.Count > 0)
                {
                    foreach (var item in crawlResponse.Data)
                    {
                        var crawlResult = new CrawlResult
                        {
                            CrawlJobId = job.Id,
                            Url = crawlResponse.NavigationResult?.FinalUrl ?? url,
                            Content = JsonSerializer.Serialize(item),
                            ExtractedDataJson = JsonSerializer.Serialize(item),
                            PromptUsed = prompt,
                            IsSuccess = true,
                            CrawledAt = DateTime.UtcNow
                        };
                        await _crawlResultRepo.AddAsync(crawlResult);
                    }

                    // Step 7: Save/update navigation strategy if navigation was used
                    if (analysis.RequiresNavigation && crawlResponse.NavigationResult?.ExecutedSteps?.Count > 0)
                    {
                        await SaveOrUpdateNavigationStrategyAsync(
                            url,
                            prompt,
                            crawlResponse,
                            existingStrategy,
                            job.Id
                        );
                    }

                    // Step 8: Update job status and conversation name
                    job.Status = JobStatus.Completed;
                    job.ResultCount = crawlResponse.Data.Count;
                    job.CompletedAt = DateTime.UtcNow;
                    
                    // DEBUG: Log the raw conversation name value from Python response
                    _logger.LogInformation("🔍 DEBUG: Received ConversationName from Python: '{ConversationName}' (null: {IsNull})", 
                        crawlResponse.ConversationName ?? "<NULL>", 
                        crawlResponse.ConversationName == null);
                    
                    job.ConversationName = crawlResponse.ConversationName; // Store conversation name from Python
                    
                    if (!string.IsNullOrEmpty(crawlResponse.ConversationName))
                    {
                        _logger.LogInformation("✅ Stored conversation name for job {JobId}: {ConversationName}", 
                            job.Id, crawlResponse.ConversationName);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ ConversationName is null or empty for job {JobId}", job.Id);
                    }
                }
                else
                {
                    job.Status = JobStatus.Failed;
                    job.ErrorMessage = crawlResponse.Error ?? "No data returned from agent.";
                    job.CompletedAt = DateTime.UtcNow;
                }

            // Step 9: Update prompt history with result
            promptHistory.CrawlJobId = job.Id;
            promptHistory.ProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            // Save all changes
            await _context.SaveChangesAsync();

            // Publish domain event for completion (success or failure)
            try
            {
                await _mediator.Publish(new JobCompletedEvent(job), cancellationToken);
                _logger.LogInformation("Published JobCompletedEvent for job {JobId} with status {Status}", 
                    job.Id, job.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish JobCompletedEvent for job {JobId}", job.Id);
                // Don't throw - job is already saved, event publish failure shouldn't fail the operation
            }

            _logger.LogInformation("Intelligent crawl completed for job {JobId}. Status: {Status}, Items: {Count}",
                job.Id, job.Status, job.ResultCount);

            return new CrawlJobResult
            {
                JobId = job.Id,
                Success = crawlResponse.Success,
                ResultCount = crawlResponse.Data.Count,
                ExecutionTimeMs = crawlResponse.ExecutionTimeMs,
                ErrorMessage = crawlResponse.Error,
                ConversationName = crawlResponse.ConversationName // Pass conversation name from Python
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing intelligent crawl for user {UserId}", userId);

            return new CrawlJobResult
            {
                JobId = Guid.Empty,
                Success = false,
                ResultCount = 0,
                ExecutionTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<CrawlJobResult?> GetJobResultAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _crawlJobRepo.GetByIdAsync(jobId);
        if (job == null)
        {
            return null;
        }

        return new CrawlJobResult
        {
            JobId = job.Id,
            Success = job.Status == JobStatus.Completed,
            ResultCount = job.ResultCount,
            ExecutionTimeMs = job.CompletedAt.HasValue ?
                (job.CompletedAt.Value - job.CreatedAt).TotalMilliseconds : 0,
            ErrorMessage = job.ErrorMessage,
            ConversationName = job.ConversationName
        };
    }

    /// <summary>
    /// Execute intelligent crawl from Kafka event (fire-and-forget pattern)
    /// Creates job with provided JobId and processes in background
    /// </summary>
    public async Task ExecuteIntelligentCrawlFromEventAsync(
        object crawlRequestObj,
        CancellationToken cancellationToken = default)
    {
        // Deserialize the event (dynamic cast from consumer)
        var json = JsonSerializer.Serialize(crawlRequestObj);
        var crawlRequest = JsonSerializer.Deserialize<SmartCrawlRequestEventDto>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (crawlRequest == null)
        {
            _logger.LogError("Failed to deserialize SmartCrawlRequestEvent");
            return;
        }

        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "Starting intelligent crawl from Kafka event: JobId {JobId}, URL {Url}, User {UserId}",
                crawlRequest.JobId, crawlRequest.Url, crawlRequest.SenderId);

            if (!await _quotaService.CheckQuotaAsync(crawlRequest.SenderId, 1, cancellationToken))
            {
                _logger.LogWarning(
                    "User {UserId} has insufficient quota for crawl request {JobId}",
                    crawlRequest.SenderId,
                    crawlRequest.JobId);

                const string quotaExceededMessage = "Quota exceeded. Please upgrade your plan to continue crawling.";
                var failedJob = new CrawlJob
                {
                    Id = crawlRequest.JobId,
                    UserId = crawlRequest.SenderId,
                    Urls = new[] { crawlRequest.Url },
                    CrawlerType = CrawlerType.Crawl4AI,
                    Status = JobStatus.Failed,
                    ErrorMessage = quotaExceededMessage,
                    FailedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    UserPrompt = crawlRequest.UserPrompt,
                    AssignmentId = crawlRequest.AssignmentId,
                    GroupId = crawlRequest.GroupId,
                    ConversationThreadId = crawlRequest.ConversationId,
                    CreatedAt = DateTime.UtcNow
                };

                await _crawlJobRepo.AddAsync(failedJob);
                await _context.SaveChangesAsync();
                await _mediator.Publish(new CrawlerFailedEvent(failedJob, quotaExceededMessage, crawlRequest.Url), cancellationToken);
                return;
            }

            await _quotaService.DeductQuotaAsync(crawlRequest.SenderId, 1, crawlRequest.JobId, cancellationToken);

            // Step 1: Create crawl job with provided JobId (from ClassroomService)
            var job = new CrawlJob
            {
                Id = crawlRequest.JobId, // Use provided JobId (not generate new)
                UserId = crawlRequest.SenderId,
                Urls = new[] { crawlRequest.Url },
                CrawlerType = CrawlerType.Crawl4AI,
                Status = JobStatus.Queued,
                Priority = Priority.Normal,
                SessionType = NavigationSessionType.Continuous,
                UserPrompt = crawlRequest.UserPrompt,
                AssignmentId = crawlRequest.AssignmentId,
                GroupId = crawlRequest.GroupId,
                ConversationThreadId = crawlRequest.ConversationId,
                ConfigurationJson = JsonSerializer.Serialize(new
                {
                    crawlRequest.ConversationId,
                    crawlRequest.AssignmentId,
                    crawlRequest.GroupId,
                    crawlRequest.SenderName,
                    Source = "ClassroomService",
                    crawlRequest.MaxPages,
                    crawlRequest.EnableNavigationPlanning
                }),
                CreatedAt = DateTime.UtcNow
            };

            await _crawlJobRepo.AddAsync(job);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Created crawl job {JobId} from Kafka event. ConversationId: {ConversationId}, MaxPages: {MaxPages}",
                job.Id, crawlRequest.ConversationId, crawlRequest.MaxPages?.ToString() ?? "null");

            // Step 2: Submit crawl job to Python (fire-and-forget) - returns immediately
            var submissionResult = await _crawl4AIClient.SubmitCrawlJobAsync(
                crawlRequest.Url,
                crawlRequest.UserPrompt,
                jobId: job.Id.ToString(),
                userId: crawlRequest.SenderId.ToString(),
                navigationSteps: null, // Let Python plan navigation
                maxPages: crawlRequest.MaxPages,  // Pass through from ClassroomService
                cancellationToken
            );

            if (!submissionResult.IsAccepted)
            {
                _logger.LogError("Failed to submit crawl job {JobId} to crawl4ai agent: {Error}", job.Id, submissionResult.ErrorMessage);
                job.Status = JobStatus.Failed;
                job.ErrorMessage = submissionResult.ErrorMessage ?? "Failed to submit job to crawl4ai agent";
                job.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Publish completion event for failed submission
                try
                {
                    await _mediator.Publish(new JobCompletedEvent(job), cancellationToken);
                }
                catch (Exception publishEx)
                {
                    _logger.LogError(publishEx, "Failed to publish JobCompletedEvent for failed job {JobId}", job.Id);
                }
                return;
            }

            // Check if completed synchronously
            if (submissionResult.IsCompletedSynchronously && submissionResult.SyncResponse != null)
            {
                await ProcessSynchronousCompletionAsync(job, submissionResult.SyncResponse, cancellationToken);
                return;
            }

            // Step 3: Update job status to InProgress (Python is now processing in background)
            job.Status = JobStatus.InProgress;
            job.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Publish domain event for job started
            try
            {
                await _mediator.Publish(new JobStartedEvent(job), cancellationToken);
                _logger.LogInformation("Published JobStartedEvent for job {JobId}", job.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish JobStartedEvent for job {JobId}", job.Id);
                // Don't throw - job is already started
            }

            _logger.LogInformation(
                "Crawl job {JobId} submitted to crawl4ai agent (fire-and-forget). Status: InProgress. " +
                "Results will be delivered via Kafka topic 'crawler.job.progress'",
                job.Id);

            // NOTE: Results are now processed by CrawlJobResultConsumer when Python publishes
            // 'CrawlJobCompleted' event to Kafka. This method returns immediately.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing crawl from Kafka event: JobId {JobId}", crawlRequest?.JobId);

            // Try to update job status to failed
            try
            {
                if (crawlRequest != null)
                {
                    var job = await _crawlJobRepo.GetByIdAsync(crawlRequest.JobId);
                    if (job != null)
                    {
                        job.Status = JobStatus.Failed;
                        job.ErrorMessage = ex.Message;
                        job.CompletedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        // Publish completion event for exception case
                        try
                        {
                            await _mediator.Publish(new JobCompletedEvent(job), cancellationToken);
                        }
                        catch (Exception publishEx)
                        {
                            _logger.LogError(publishEx, "Failed to publish JobCompletedEvent after exception for job {JobId}", job.Id);
                        }
                    }
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update job status after error");
            }
        }
    }

    private async Task<NavigationStrategy?> FindMatchingStrategyAsync(string domain, string prompt)
    {
        var strategies = await _navigationStrategyRepo.GetAsync();

        return strategies
            .Where(s => s.Domain == domain && s.IsActive)
            .OrderByDescending(s => s.SuccessRate)
            .FirstOrDefault();
    }

    private async Task SaveOrUpdateNavigationStrategyAsync(
        string url,
        string prompt,
        Crawl4AIResponse crawlResponse,
        NavigationStrategy? existingStrategy,
        Guid jobId)
    {
        try
        {
            var domain = new Uri(url).Host;

            // Convert executed steps to NavigationStep format
            var navigationSteps = crawlResponse.NavigationResult?.ExecutedSteps?
                .Select((step, index) => new NavigationStep
                {
                    Order = index,
                    Action = step.GetValueOrDefault("action")?.ToString() ?? "unknown",
                    Target = step.GetValueOrDefault("selector")?.ToString(),
                    Description = step.GetValueOrDefault("description")?.ToString(),
                    Parameters = step
                }).ToList() ?? new List<NavigationStep>();

            var stepsJson = JsonSerializer.Serialize(navigationSteps);

            if (existingStrategy != null)
            {
                // Update existing strategy
                existingStrategy.TimesUsed++;
                existingStrategy.SuccessCount++;
                existingStrategy.AverageExecutionTimeMs =
                    (existingStrategy.AverageExecutionTimeMs * (existingStrategy.TimesUsed - 1) +
                     crawlResponse.ExecutionTimeMs) / existingStrategy.TimesUsed;
                existingStrategy.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated navigation strategy {StrategyId}. Success rate: {SuccessRate}",
                    existingStrategy.Id, existingStrategy.SuccessRate);
            }
            else
            {
                // Create new strategy
                var newStrategy = new NavigationStrategy
                {
                    Name = $"Auto-learned: {domain}",
                    Domain = domain,
                    UrlPattern = url,
                    Type = NavigationStrategyType.Learned,
                    NavigationStepsJson = stepsJson,
                    TimesUsed = 1,
                    SuccessCount = 1,
                    FailureCount = 0,
                    AverageExecutionTimeMs = crawlResponse.ExecutionTimeMs,
                    CreatedByJobId = jobId,
                    IsTemplate = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _navigationStrategyRepo.AddAsync(newStrategy);
                _logger.LogInformation("Created new navigation strategy {StrategyId} for domain {Domain}",
                    newStrategy.Id, domain);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving navigation strategy");
        }
    }

    public async Task<string?> ProcessUserMessageAsync(
        Guid conversationId,
        string userMessage,
        string? csvContext = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Find ALL completed jobs for this conversation
            var jobs = await _crawlJobRepo.FindAsync(
                j => j.ConversationThreadId == conversationId && j.Status == JobStatus.Completed,
                cancellationToken
            );
            
            // 2. Get all completed jobs (with reasonable limits)
            const int MaxJobs = 10;
            // NOTE: Each crawled product is stored as 1 CrawlResult record.
            // Increased from 50 to 500 to ensure all products are included in RAG context
            // when user asks questions like "how many products total?"
            const int MaxResultsPerJob = 500;
            var shouldSearchSimilarProducts = IsSimilarProductRequest(userMessage);
            var productDescriptors = new List<ProductDescriptor>(MaxProductsFromCrawl);
            
            var completedJobs = jobs
                .Where(j => j.CompletedAt.HasValue)
                .OrderByDescending(j => j.CompletedAt)
                .Take(MaxJobs)
                .ToList();
            
            // 3. Collect results from all jobs
            var crawlContextParts = new List<string>();
            var totalProductsInContext = 0;
            
            foreach (var job in completedJobs)
            {
                var results = await _crawlResultRepo.FindAsync(
                    r => r.CrawlJobId == job.Id && r.IsSuccess,
                    cancellationToken);
                
                var allResults = results.ToList();
                var jobResults = allResults
                    .OrderByDescending(r => r.CrawledAt)
                    .Take(MaxResultsPerJob)
                    .ToList();
                
                // LOG: Track exact counts for debugging
                _logger.LogInformation(
                    "📊 RAG Context - Job {JobId}: DB has {TotalInDb} results, using {UsedCount} (MaxResultsPerJob={Max})",
                    job.Id, allResults.Count, jobResults.Count, MaxResultsPerJob);
                
                if (!jobResults.Any()) continue;
                
                totalProductsInContext += jobResults.Count;
                
                // Add job header for context with EXACT count
                var jobHeader = $"=== Crawl Job: {job.UserPrompt ?? "N/A"} | URL: {job.Urls.FirstOrDefault() ?? "N/A"} | TOTAL PRODUCTS: {jobResults.Count} ===\n";
                crawlContextParts.Add(jobHeader);
                
                // Add all results from this job with source metadata injected
                foreach (var result in jobResults)
                {
                    // Inject source metadata into product JSON for comparison queries
                    var productJsonWithSource = InjectSourceMetadata(
                        result.Content ?? string.Empty,
                        job.Id,
                        job.Urls.FirstOrDefault() ?? "unknown",
                        job.UserPrompt ?? "N/A"
                    );
                    
                    crawlContextParts.Add(productJsonWithSource);

                    if (shouldSearchSimilarProducts && productDescriptors.Count < MaxProductsFromCrawl)
                    {
                        var extracted = ExtractProductsFromResult(result);
                        foreach (var descriptor in extracted)
                        {
                            productDescriptors.Add(descriptor);
                            if (productDescriptors.Count >= MaxProductsFromCrawl)
                            {
                                break;
                            }
                        }
                    }
                }
                
                crawlContextParts.Add(""); // Empty line between jobs
            }
            
            // Add summary with exact count at the end
            if (totalProductsInContext > 0)
            {
                crawlContextParts.Add($"\n=== SUMMARY: TOTAL {totalProductsInContext} PRODUCTS IN THIS CONTEXT ===\n");
            }
            
            _logger.LogInformation("📊 RAG Context built: {TotalProducts} total products from {JobCount} jobs", 
                totalProductsInContext, completedJobs.Count);
            
            var crawlContext = crawlContextParts.Any() 
                ? string.Join("\n", crawlContextParts) 
                : string.Empty;
            
            // 4. Combine contexts (crawl results + CSV data)
            var contexts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(crawlContext))
            {
                contexts.Add("Crawled Data:");
                contexts.Add(crawlContext);
            }
            
            if (!string.IsNullOrWhiteSpace(csvContext))
            {
                contexts.Add("\nUploaded CSV Data:");
                contexts.Add(csvContext);
            }

            if (shouldSearchSimilarProducts)
            {
                var (searchDescriptors, skipFiltering) = BuildDescriptorsForGoogleSearch(
                    productDescriptors,
                    userMessage);

                if (searchDescriptors.Any())
                {
                    var googleResults = await FetchGoogleSimilarProductsAsync(
                        searchDescriptors,
                        userMessage,
                        skipFiltering,
                        cancellationToken);
                    if (googleResults.Any())
                    {
                        contexts.Add("\nGoogle Similar Product Links:");
                        contexts.Add(FormatGoogleResultsForContext(googleResults));
                    }
                }
            }
            
            // 5. If no context available, return null
            if (!contexts.Any())
            {
                return null;
            }
            
            var combinedContext = string.Join("\n\n", contexts);
            
            // 6. Ask Agent with combined context
            return await _crawl4AIClient.AskQuestionAsync(combinedContext, userMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user message for conversation {ConversationId}", conversationId);
            return null;
        }
    }

    private async Task<List<GoogleProductSearchResult>> FetchGoogleSimilarProductsAsync(
        List<ProductDescriptor> descriptors,
        string userMessage,
        bool skipFiltering,
        CancellationToken cancellationToken)
    {
        var normalizedMessage = NormalizeText(userMessage);
        var targetedDescriptors = skipFiltering
            ? descriptors
            : FilterDescriptorsByUserMessage(descriptors, normalizedMessage);

        var uniqueProducts = targetedDescriptors
            .Where(d => !string.IsNullOrWhiteSpace(d.Name))
            .GroupBy(d => d.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(MaxProductsForGoogleSearch)
            .ToList();

        var allResults = new List<GoogleProductSearchResult>();

        foreach (var product in uniqueProducts)
        {
            var hits = await _googleProductSearch.SearchSimilarProductsAsync(
                product,
                MaxGoogleLinksPerProduct,
                cancellationToken);

            foreach (var hit in hits.Where(h => !string.IsNullOrWhiteSpace(h.Link)))
            {
                allResults.Add(hit);
            }
        }

        return allResults;
    }

    private static List<ProductDescriptor> FilterDescriptorsByUserMessage(
        List<ProductDescriptor> descriptors,
        string normalizedMessage)
    {
        if (string.IsNullOrWhiteSpace(normalizedMessage))
        {
            return descriptors;
        }
        var messageKeywords = ExtractKeywords(normalizedMessage);
        var matches = descriptors
            .Where(d =>
            {
                var nameNormalized = NormalizeText(d.Name);
                var brandNormalized = NormalizeText(d.Brand);

                bool ContainsMessage(string text) =>
                    !string.IsNullOrWhiteSpace(text) &&
                    (text.Contains(normalizedMessage) || normalizedMessage.Contains(text));

                if (ContainsMessage(nameNormalized) || ContainsMessage(brandNormalized))
                {
                    return true;
                }

                return messageKeywords.Any(keyword =>
                    (!string.IsNullOrWhiteSpace(nameNormalized) && nameNormalized.Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(brandNormalized) && brandNormalized.Contains(keyword)));
            })
            .ToList();
        return matches.Any() ? matches : descriptors;
    }

    private static (List<ProductDescriptor> descriptors, bool skipFiltering) BuildDescriptorsForGoogleSearch(
        List<ProductDescriptor> descriptorsFromCrawl,
        string triggerMessage)
    {
        if (TryBuildDescriptorFromUserMessage(triggerMessage, out var userDescriptor))
        {
            return (new List<ProductDescriptor> { userDescriptor }, true);
        }

        if (descriptorsFromCrawl.Any())
        {
            return (descriptorsFromCrawl, false);
        }

        var fallbackQuery = triggerMessage?.Trim();
        if (string.IsNullOrWhiteSpace(fallbackQuery))
        {
            return (new List<ProductDescriptor>(), true);
        }

        return (new List<ProductDescriptor>
        {
            new ProductDescriptor { Name = fallbackQuery }
        }, true);
    }

    private static bool TryBuildDescriptorFromUserMessage(string? message, out ProductDescriptor descriptor)
    {
        descriptor = default!;
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var workingText = message;
        var lower = message.ToLowerInvariant();

        var markers = new[]
        {
            "tương tự",
            "link google",
            "google cho tôi",
            "google cho tao",
            "google cho mình",
            "cho tôi link",
            "cho tao link",
            "google"
        };

        foreach (var marker in markers)
        {
            var index = lower.IndexOf(marker, StringComparison.Ordinal);
            if (index >= 0)
            {
                var start = index + marker.Length;
                if (start < message.Length)
                {
                    workingText = message[start..];
                    lower = workingText.ToLowerInvariant();
                }
            }
        }

        var pipeIndex = workingText.IndexOf('|');
        if (pipeIndex >= 0)
        {
            workingText = workingText[..pipeIndex];
        }

        var cleaned = workingText.Trim(' ', '-', ':', ',', '|');
        if (cleaned.Length < 3)
        {
            return false;
        }

        descriptor = new ProductDescriptor
        {
            Name = cleaned
        };
        return true;
    }

    private static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var formD = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var ch in formD)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var lower = char.ToLowerInvariant(ch);
            sb.Append(char.IsLetterOrDigit(lower) ? lower : ' ');
        }

        return string.Join(" ", sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static List<string> ExtractKeywords(string normalizedText)
    {
        return normalizedText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FormatGoogleResultsForContext(IEnumerable<GoogleProductSearchResult> results)
    {
        var sb = new StringBuilder();
        foreach (var group in results.GroupBy(r => r.ProductName))
        {
            sb.AppendLine($"Product: {group.Key}");
            foreach (var hit in group.Take(MaxGoogleLinksPerProduct))
            {
                sb.AppendLine($"- {hit.Title ?? hit.DisplayLink}: {hit.Link}");
                if (!string.IsNullOrWhiteSpace(hit.Snippet))
                {
                    sb.AppendLine($"  Snippet: {hit.Snippet}");
                }
            }
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    private static bool IsSimilarProductRequest(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return false;
        }

        var normalized = userMessage.ToLowerInvariant();
        return SimilarProductKeywords.Any(keyword => normalized.Contains(keyword));
    }

    /// <summary>
    /// Inject source metadata into product JSON to enable comparison queries.
    /// Adds: _source (job type), _crawl_job_id, _source_url
    /// </summary>
    private static string InjectSourceMetadata(string productJson, Guid jobId, string sourceUrl, string jobPrompt)
    {
        try
        {
            using var doc = JsonDocument.Parse(productJson);
            var root = doc.RootElement;
            
            // Create new object with source metadata
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                
                // Add source metadata first
                writer.WriteString("_source", "crawl_job");
                writer.WriteString("_crawl_job_id", jobId.ToString());
                writer.WriteString("_source_url", sourceUrl);
                writer.WriteString("_job_prompt", jobPrompt);
                
                // Copy all original properties
                foreach (var property in root.EnumerateObject())
                {
                    property.WriteTo(writer);
                }
                
                writer.WriteEndObject();
            }
            
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            // If not valid JSON, return as-is
            return productJson;
        }
    }

    private static IEnumerable<ProductDescriptor> ExtractProductsFromResult(CrawlResult result)
    {
        var collected = new List<ProductDescriptor>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var source in new[] { result.ExtractedDataJson, result.Content })
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(source);
                ExtractFromElement(doc.RootElement, result.Url, collected, seen);
            }
            catch (JsonException)
            {
                // Skip invalid JSON blobs
                continue;
            }

            if (collected.Count >= MaxProductsFromCrawl)
            {
                break;
            }
        }

        return collected;
    }

    private static void ExtractFromElement(
        JsonElement element,
        string? sourceUrl,
        List<ProductDescriptor> output,
        HashSet<string> seenNames)
    {
        if (output.Count >= MaxProductsFromCrawl)
        {
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var child in element.EnumerateArray())
                {
                    ExtractFromElement(child, sourceUrl, output, seenNames);
                    if (output.Count >= MaxProductsFromCrawl)
                    {
                        break;
                    }
                }
                break;

            case JsonValueKind.Object:
                if (TryCreateDescriptor(element, sourceUrl, out var descriptor))
                {
                    if (seenNames.Add(descriptor.Name) && output.Count < MaxProductsFromCrawl)
                    {
                        output.Add(descriptor);
                    }
                }
                else
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        {
                            ExtractFromElement(property.Value, sourceUrl, output, seenNames);
                            if (output.Count >= MaxProductsFromCrawl)
                            {
                                break;
                            }
                        }
                    }
                }
                break;
        }
    }

    private static bool TryCreateDescriptor(
        JsonElement element,
        string? sourceUrl,
        out ProductDescriptor descriptor)
    {
        descriptor = default!;
        var name = TryGetFirstString(element, ProductNameFields);
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var brand = TryGetFirstString(element, BrandFields);
        var additional = TryGetFirstString(element, VariantFields);
        var price = TryGetPrice(element, out var priceCurrency);
        var currency = TryGetFirstString(element, CurrencyFields) ?? priceCurrency;

        descriptor = new ProductDescriptor
        {
            Name = name.Trim(),
            Brand = brand?.Trim(),
            Price = price,
            Currency = currency?.ToUpperInvariant(),
            SourceUrl = sourceUrl,
            AdditionalNotes = additional
        };

        return true;
    }

    private static string? TryGetFirstString(JsonElement element, string[] keys)
    {
        foreach (var key in keys)
        {
            if (element.TryGetProperty(key, out var value))
            {
                switch (value.ValueKind)
                {
                    case JsonValueKind.String:
                        return value.GetString();
                    case JsonValueKind.Number:
                        return value.ToString();
                }
            }
        }

        return null;
    }

    private static decimal? TryGetPrice(JsonElement element, out string? currency)
    {
        currency = null;
        foreach (var key in PriceFields)
        {
            if (!element.TryGetProperty(key, out var value))
            {
                continue;
            }

            switch (value.ValueKind)
            {
                case JsonValueKind.Number when value.TryGetDecimal(out var numeric):
                    return numeric;

                case JsonValueKind.String:
                    var text = value.GetString();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    currency = DetectCurrency(text);
                    var digitsOnly = new string(text.Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',').ToArray());
                    if (string.IsNullOrWhiteSpace(digitsOnly))
                    {
                        continue;
                    }

                    if (decimal.TryParse(
                        digitsOnly.Replace(",", string.Empty),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var parsed))
                    {
                        return parsed;
                    }
                    break;
            }
        }

        return null;
    }

    private static string? DetectCurrency(string text)
    {
        var normalized = text.ToLowerInvariant();
        if (normalized.Contains("vnd") || normalized.Contains("đ") || normalized.Contains("dong"))
        {
            return "VND";
        }

        if (normalized.Contains("usd") || normalized.Contains("$"))
        {
            return "USD";
        }

        if (normalized.Contains("eur") || normalized.Contains("€"))
        {
            return "EUR";
        }

        return null;
    }

    /// <summary>
    /// NEW: Generate summary + chart data across MULTIPLE jobs in the same conversation
    /// Returned DTO must be compatible with ClassroomService expectation.
    /// </summary>
    public async Task<CrawlJobSummaryDto?> GetConversationSummaryAsync(
        Guid conversationId,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            const int MaxJobs = 10;
            // NOTE: Increased from 50 to 500 to include all products in summary/chart generation
            const int MaxResultsPerJob = 500;
            const int MaxTotalChars = 120_000;

            var jobs = await _crawlJobRepo.FindAsync(
                j => j.ConversationThreadId == conversationId && j.Status == JobStatus.Completed,
                cancellationToken
            );

            var completedJobs = jobs
                .Where(j => j.CompletedAt.HasValue)
                .OrderByDescending(j => j.CompletedAt)
                .Take(MaxJobs)
                .ToList();

            if (!completedJobs.Any())
                return null;

            var sb = new System.Text.StringBuilder();

            // Context + instructions for agent
            sb.AppendLine("You are a data analyst.");
            sb.AppendLine("Use ONLY the crawl results below.");
            sb.AppendLine("User request may ask for statistics and charts.");
            sb.AppendLine();
            sb.AppendLine($"USER_REQUEST: {prompt}");
            sb.AppendLine();
            sb.AppendLine("Return STRICT JSON with schema:");
            sb.AppendLine("Do not include markdown, code fences, comments, or trailing commas.");
            sb.AppendLine(@"
{
  ""summaryText"": ""..."",
  ""insightHighlights"": [""..."", ""...""],
  ""chartPreviews"": [
    {
      ""title"": ""..."",
      ""chartType"": ""pie"",
      ""chartData"": { ""labels"": [""A""], ""datasets"": [{ ""data"": [1] }] }
    }
  ]
}
");
            sb.AppendLine("If you cannot make a chart, return chartPreviews: []");
            sb.AppendLine();

            var totalChars = 0;

            foreach (var job in completedJobs)
            {
                var results = await _crawlResultRepo.FindAsync(
                    r => r.CrawlJobId == job.Id && r.IsSuccess,
                    cancellationToken
                );

                var takeResults = results
                    .OrderByDescending(r => r.CrawledAt)
                    .Take(MaxResultsPerJob)
                    .ToList();

                if (!takeResults.Any()) continue;

                var header =
                    $"=== JOB {job.Id} | completedAt={job.CompletedAt:O} | url={job.Urls.FirstOrDefault()} | prompt={job.UserPrompt} | conversationName={job.ConversationName} ===\n";
                if (totalChars + header.Length > MaxTotalChars) break;

                sb.Append(header);
                totalChars += header.Length;

                foreach (var r in takeResults)
                {
                    var block = r.Content + "\n";
                    if (totalChars + block.Length > MaxTotalChars) break;

                    sb.Append(block);
                    totalChars += block.Length;
                }

                sb.AppendLine();
                totalChars += 1;

                if (totalChars >= MaxTotalChars) break;
            }

            var context = sb.ToString();
            if (string.IsNullOrWhiteSpace(context))
                return null;

            // Call agent with user's actual prompt - Python SmartRAG handles classification
            var raw = await _crawl4AIClient.AskQuestionAsync(context, prompt, cancellationToken);
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            try
            {
                var dto = System.Text.Json.JsonSerializer.Deserialize<CrawlJobSummaryDto>(raw, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                if (dto == null)
                {
                    return new CrawlJobSummaryDto
                    {
                        SummaryText = raw
                    };
                }

                // Safety: normalize null collections
                dto.InsightHighlights ??= new System.Collections.Generic.List<string>();
                dto.ChartPreviews ??= new System.Collections.Generic.List<ChartPreviewDto>();

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse conversation summary JSON. Returning raw text. Raw: {Raw}", raw);

                return new CrawlJobSummaryDto
                {
                    SummaryText = raw,
                    InsightHighlights = new System.Collections.Generic.List<string>(),
                    ChartPreviews = new System.Collections.Generic.List<ChartPreviewDto>()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating conversation summary for conversation {ConversationId}", conversationId);
            return null;
        }
    }

    private async Task ProcessSynchronousCompletionAsync(CrawlJob job, Crawl4AIResponse response, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Processing synchronous results for job {JobId}", job.Id);

            var itemsCount = response.Data.Count;
            var finalUrl = response.NavigationResult?.FinalUrl ?? job.Urls.FirstOrDefault() ?? "";
            var results = new List<CrawlResult>();

            foreach (var item in response.Data)
            {
                var contentJson = JsonSerializer.Serialize(item);
                var result = new CrawlResult
                {
                    CrawlJobId = job.Id,
                    Url = finalUrl,
                    Content = contentJson,
                    ExtractedDataJson = contentJson,
                    PromptUsed = job.UserPrompt ?? "",
                    IsSuccess = true,
                    CrawledAt = DateTime.UtcNow
                };
                await _crawlResultRepo.AddAsync(result);
                results.Add(result);
            }

            job.Status = JobStatus.Completed;
            job.ResultCount = itemsCount;
            job.CompletedAt = DateTime.UtcNow;
            job.UrlsProcessed = itemsCount;
            job.UrlsSuccessful = itemsCount;
            job.UrlsFailed = 0;
            job.ConversationName = response.ConversationName; // Store conversation name from Python agent

            await _context.SaveChangesAsync(ct);
            
            // Save navigation strategy if applicable
            await SaveOrUpdateNavigationStrategyAsync(job.Urls.FirstOrDefault() ?? "", job.UserPrompt ?? "", response, null, job.Id);

            await _mediator.Publish(new JobCompletedEvent(job), ct);

            _logger.LogInformation("Job {JobId} completed synchronously: ConversationName: '{ConversationName}', {Count} items in {Time}ms", 
                job.Id, job.ConversationName ?? "<null>", itemsCount, response.ExecutionTimeMs);
            
            // AUTO-SUMMARY (Optional: Log or trigger event)
            if (results.Any())
            {
                // We could generate a summary here, but we don't have a direct way to push it to the chat yet.
                // The user can ask for it via RAG.
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process synchronous completion for job {JobId}", job.Id);
            job.Status = JobStatus.Failed;
            job.ErrorMessage = $"Error processing results: {ex.Message}";
            await _context.SaveChangesAsync(ct);
            
            // Publish completion event even for failure
            try 
            {
                await _mediator.Publish(new JobCompletedEvent(job), ct);
            }
            catch {}
        }
    }

}

/// <summary>
/// DTO for SmartCrawlRequestEvent from Kafka
/// Must match ClassroomService.Domain.Events.SmartCrawlRequestEvent
/// </summary>
internal class SmartCrawlRequestEventDto
{
    public Guid JobId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string UserPrompt { get; set; } = string.Empty;
    public int? MaxPages { get; set; } // Nullable: null = empty UI field (Python handles default)
    public bool EnableNavigationPlanning { get; set; } = true;
    public DateTime Timestamp { get; set; }
    public string? MetadataJson { get; set; }
}
