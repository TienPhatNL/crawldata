using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure Services - let Aspire assign ports automatically
var sqlServer = builder.AddSqlServer("sqlserver");

var userDb = sqlServer.AddDatabase("UserServiceDb");
var classroomDb = sqlServer.AddDatabase("ClassroomServiceDb");
var webCrawlerDb = sqlServer.AddDatabase("WebCrawlerDb");
var notificationDb = sqlServer.AddDatabase("NotificationServiceDb");

var redis = builder.AddRedis("redis")
    .WithDataVolume("crawldata-redis-data")
    .WithEnvironment("REDIS_MAXMEMORY", "256mb")
    .WithEnvironment("REDIS_MAXMEMORY_POLICY", "allkeys-lru");

var kafka = builder.AddKafka("kafka")  // No port here—let WithEndpoint handle it
    .WithDataVolume("crawldata-kafka-data")
    .WithEnvironment("KAFKA_LOG_DIRS", "/var/lib/kafka/data")

    // KRaft Controller (fixes UnknownHostException)
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@localhost:29093")  // localhost works regardless of container name

    // Listeners (internal/external separation)
    .WithEnvironment("KAFKA_LISTENERS",
        "INTERNAL://0.0.0.0:29092," +  // Docker-internal
        "EXTERNAL://0.0.0.0:9092," +  // Host-external
        "CONTROLLER://0.0.0.0:29093")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS",
        "INTERNAL://localhost:29092," +     // For internal .NET services
        "EXTERNAL://host.docker.internal:9092")  // For external Docker agents
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP",
        "INTERNAL:PLAINTEXT,EXTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "INTERNAL")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")

    // Fixed External Endpoint (key fix: isExternal=true binds to 0.0.0.0)
    .WithEndpoint(
    port: 29092,
    targetPort: 29092,
    name: "internal",
    scheme: "tcp",
    isProxied: false,
    isExternal: false)    // Host-only
    .WithEndpoint(
        port: 9092,              // Host port (fixed)
        targetPort: 9092,        // Container port
        name: "broker",
        scheme: "tcp",
        isProxied: false,        // Skip proxy (avoids bind errors)
        isExternal: true,         // Expose on 0.0.0.0 (all interfaces) for external Docker access
        env: "ASPIRE_ALLOW_UNSECURED_TRANSPORT"
    );

// Override Aspire's automatic localhost→host.docker.internal translation for Kafka
// This prevents services from trying to connect to the network IP (e.g., 192.168.1.216:9092)
// Forces all services to use localhost:9092 instead
builder.Configuration["ConnectionStrings:kafka"] = "localhost:9092";

// Crawl4AI Python Agents (external containers)
// Note: These run as separate Python/Docker containers, not .NET projects
// Start them separately: docker-compose -f docker-compose.webcrawler-test.yml up -d crawl4ai-agent-1

// Get Gemini API key from configuration or use default for testing
var geminiApiKey = builder.Configuration["GEMINI_API_KEY"] ?? "AIzaSyDsvVjF9XRwZy9naPVpHIU5qXT2GF5SaZU";

// Only essential microservices
var userService = builder.AddProject("userservice", "../UserService-Microservice/Application/Application.csproj")
    .WithReference(userDb)
    .WithReference(redis)
    .WithReference(kafka)
    .WithHttpEndpoint(port: 5001);

var classroomService = builder.AddProject("classroomservice", "../ClassroomService-Microservice/Application/Application.csproj")
    .WithReference(classroomDb)
    .WithReference(redis)
    .WithReference(kafka)
    .WithReference(userService)
    .WithHttpEndpoint(port: 5006);

var webCrawlerService = builder.AddProject("webcrawlerservice", "../WebCrawlerService-Microservice/Application/Application.csproj")
    .WithReference(webCrawlerDb)
    .WithReference(redis)
    .WithReference(kafka)
    .WithEnvironment("Crawl4AI__BaseUrl", "http://localhost:8004")
    .WithEnvironment("LlmSettings__GeminiApiKey", geminiApiKey)
    .WithEnvironment("JwtSettings__SecretKey", "YourSuperSecretKeyThatIsAtLeast32CharactersLong!")
    .WithEnvironment("JwtSettings__Issuer", "CrawlDataPlatform")
    .WithEnvironment("JwtSettings__Audience", "CrawlDataUsers")
    .WithHttpEndpoint(port: 5014);

// Add WebCrawlerService reference to ClassroomService for service discovery
classroomService.WithReference(webCrawlerService);

var notificationService = builder.AddProject("notificationservice", "../NotificationService-Microservice/Application/Application.csproj")
    .WithReference(notificationDb)
    .WithReference(redis)
    .WithReference(kafka)
    .WithHttpEndpoint(port: 5015);

var apiGateway = builder.AddProject("apigateway", "../CrawlData.ApiGateway/CrawlData.ApiGateway.csproj")
	.WithReference(userService)
	.WithReference(classroomService)
	.WithReference(notificationService)
	.WithReference(webCrawlerService)
	.WithHttpEndpoint(port: 8080);

System.Console.WriteLine("Starting Aspire with HTTP endpoints...");
System.Console.WriteLine("- Infrastructure: SQL Server, Redis, Kafka");
System.Console.WriteLine("- Services:");
System.Console.WriteLine("  • UserService: http://localhost:5001");
System.Console.WriteLine("  • ClassroomService: http://localhost:5006");
System.Console.WriteLine("  • WebCrawlerService: http://localhost:5014");
System.Console.WriteLine("  • NotificationService: http://localhost:5015");
System.Console.WriteLine("  • API Gateway: http://localhost:8090");
System.Console.WriteLine("- Aspire Dashboard: https://localhost:15000");
System.Console.WriteLine("\n⚠️  External Services (start manually):");
System.Console.WriteLine("  • Crawl4AI Agents:");
System.Console.WriteLine("    docker-compose -f docker-compose.webcrawler-test.yml up -d crawl4ai-agent-1 crawl4ai-agent-2 crawl4ai-agent-3");

builder.Build().Run();