using CrawlData.ApiGateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (health checks, telemetry, etc.)
builder.AddServiceDefaults();

// Redis (nếu cần caching/rate limiting)
builder.AddRedisClient("redis");

// Load YARP configuration từ appsettings
builder.Services.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
	.AddServiceDiscoveryDestinationResolver(); // Quan trọng: kích hoạt service discovery của Aspire

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy.WithOrigins("http://localhost:3000",
                "http://localhost:3001",
                "http://localhost:3002",
                "http://localhost:3003",
                "http://localhost:4200",
                "http://localhost:5173",
                "http://localhost:5500",
                "http://localhost:5501",
                "http://127.0.0.1:5500",
                "http://127.0.0.1:5501",
                "https://ai-enhance-six.vercel.app",
                "https://ai-enhance-staff.vercel.app",
                "https://ai-enhance-admin.vercel.app")
			.WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
			.AllowAnyHeader()
			.AllowCredentials();
	});
});

// JWT Authentication (nếu gateway xử lý auth)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidAudience = builder.Configuration["Jwt:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
		};
		// Không challenge 401 ở gateway (để backend xử lý)
		options.Events = new JwtBearerEvents
		{
			OnChallenge = context =>
			{
				context.HandleResponse();
				return Task.CompletedTask;
			}
		};
	});

builder.Services.AddAuthorization();

// Controllers và Swagger (nếu cần custom endpoint)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Swagger config (bạn đã comment, có thể mở lại nếu cần)

// Build app
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
	// app.UseSwagger(); app.UseSwaggerUI(); // mở lại nếu cần
}

app.UseCors("AllowAll");
app.UseMiddleware<CorrelationIdMiddleware>();
// app.UseMiddleware<RateLimitingMiddleware>(); // nếu bạn đã implement
// app.UseMiddleware<ApiKeyMiddleware>(); // nếu có

app.UseAuthentication();
app.UseAuthorization();

// Map controllers (nếu có)
app.MapControllers();

// Map YARP reverse proxy
app.MapReverseProxy();

// Health checks
app.MapDefaultEndpoints();

app.Run();
