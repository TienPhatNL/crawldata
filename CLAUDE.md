# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A web crawling and analysis platform built with .NET 8 microservices architecture. Serves educational institutions, researchers, and businesses with AI-powered data extraction capabilities.

**User Roles:**
- **Student**: Limited crawling (4 URLs per assignment), assignment submissions
- **Lecturer**: Classroom management, assignment creation, grading, institution verification required
- **Staff**: User approval workflows, bulk account creation, technical support
- **Admin**: Complete system oversight and management
- **PaidUser**: Premium features, higher quotas, API access, tiered subscriptions (Free/Basic/Pro/Enterprise)

## Running the Application

### Primary Method: .NET Aspire (Recommended)
```bash
cd CrawlData.AppHost
dotnet run
```

**Access Points:**
- Aspire Dashboard: https://localhost:15000 (service monitoring)
- API Gateway: http://localhost:8090 (main entry point)
- UserService: http://localhost:5001
- DataExtractionService: http://localhost:5002
- SubscriptionService: http://localhost:5003
- ClassroomService: http://localhost:5006
- WebCrawlerService: http://localhost:5014
- ReportGenerationService: http://localhost:5015

### Alternative: Docker Compose
```bash
docker-compose up -d        # Start all services
docker-compose logs -f      # View logs
docker-compose down         # Stop services
```

API Gateway with Docker: http://localhost:8080

## Database Management

Each microservice has its own database. Common commands:

```bash
# Run migrations
dotnet ef database update --project WebCrawlerService-Microservice/Infrastructure
dotnet ef database update --project UserService-Microservice/Infrastructure

# Add migration
dotnet ef migrations add MigrationName --project [Service]/Infrastructure

# Drop database (dev only)
dotnet ef database drop --project [Service]/Infrastructure
```

## Architecture

### Microservices

1. **UserService-Microservice**: Authentication, authorization, user management, subscription tracking
2. **WebCrawlerService-Microservice**: Multi-agent crawling (HTTP, Selenium, Playwright, Scrapy), job queue, rate limiting
3. **DataExtractionService-Microservice**: AI-powered analysis (OpenAI, Claude), template-based extraction
4. **SubscriptionService-Microservice**: Stripe payments, quota enforcement, tiered access
5. **ReportGenerationService-Microservice**: Multi-format exports (PDF, Excel, HTML, JSON, CSV)
6. **ClassroomService-Microservice**: Educational workflows, assignments, grading

### Clean Architecture Pattern

All services follow this structure:
```
ServiceName-Microservice/
├── Domain/              # Entities, enums, events (no external dependencies)
├── Application/         # Controllers, services, business logic
└── Infrastructure/      # EF contexts, repositories, external integrations
```

**Key Principles:**
- Domain layer is pure business logic with no infrastructure dependencies
- Application layer orchestrates workflows and handles requests
- Infrastructure layer implements data access and external service integration
- All entities inherit from `BaseAuditableEntity`
- Soft deletes use `ISoftDelete` interface
- Primary keys are `Guid` types

### Service Communication

- **Synchronous**: gRPC for inter-service calls (high performance)
- **Asynchronous**: Kafka for event streaming and domain events
- **Real-time**: SignalR for monitoring and notifications
- **Caching**: Redis for sessions and distributed cache
- **API Gateway**: YARP-based reverse proxy with authentication/rate limiting

### Orchestration

**Aspire** (`CrawlData.AppHost/Program.cs`):
- Automatic service discovery and configuration
- Manages SQL Server, Redis, Kafka infrastructure
- Service dependency injection and health monitoring
- Dashboard for observability at https://localhost:15000

**Service Defaults** (`CrawlData.ServiceDefaults`):
- Shared logging, monitoring, health check configurations
- OpenTelemetry integration
- Common middleware and authentication setup

## Important Workflows

### Lecturer Account Approval

When lecturers register, they require staff/admin approval:

1. Registration creates user with `Status = PendingApproval`
2. Staff/Admin approves via `ApproveUserCommandHandler`
3. Status becomes:
   - `Active` if email already confirmed
   - `Pending` if email not confirmed (will become `Active` upon email confirmation)
4. **Lecturers receive approval email automatically** via `IEmailService.SendUserApprovedEmailAsync()`

See: `/root/projects/crawldata/UserService-Microservice/Application/Features/Users/Commands/ApproveUserCommandHandler.cs:43-76`

### Email Confirmation Flow

- Email verification tokens sent via `EmailService`
- Confirmation handled in `ConfirmEmailCommandHandler`
- Status transitions: `Pending` → `Active` OR `PendingApproval` remains unchanged
- Lecturers must both confirm email AND get approved by staff to become active

## Entity Conventions

- **Base class**: All entities inherit from `BaseAuditableEntity` (provides Id, CreatedAt, UpdatedAt, etc.)
- **Soft delete**: Implement `ISoftDelete` interface (adds IsDeleted, DeletedAt, DeletedBy)
- **Primary keys**: Always `Guid` type
- **Navigation properties**: Mark as `virtual` for lazy loading
- **Collections**: Initialize as empty lists: `= new List<T>()`
- **Nullable**: Use `string?`, `DateTime?`, etc. where appropriate
- **Computed properties**: Use expression-bodied members (e.g., `public bool IsEmailConfirmed => EmailConfirmedAt.HasValue;`)

## Code Style

- **Namespaces**: File-scoped namespaces
- **Nullable reference types**: Enabled globally
- **Implicit usings**: Enabled
- **Naming**:
  - Classes/Methods/Properties: PascalCase
  - Private fields: _camelCase with underscore prefix
  - Local variables: camelCase
- **Dependency Injection**: Register in extension methods, use appropriate lifetimes (Scoped for app services, Singleton for config)

## Key Domain Concepts

### User Status Enum
- `Pending`: Email not confirmed yet
- `Active`: Fully active account (can login)
- `PendingApproval`: Waiting for staff approval (lecturers)
- `Suspended`: Suspended by admin/staff
- `Inactive`: Temporarily disabled
- `Deleted`: Soft deleted

### User Roles
- `Student`, `Lecturer`, `Staff`, `Admin`, `PaidUser`
- Role-based business logic in `RoleBasedBusinessLogic/` directory

### Subscription Tiers
- `Free` (4 URLs), `Basic`, `Pro`, `Enterprise`
- Quota enforcement handled by SubscriptionService
- Stripe integration for payments

## Testing

```bash
# Run all tests
dotnet test

# Run tests for specific service
dotnet test WebCrawlerService-Microservice/
```

## Common Development Tasks

### Adding a New Feature

1. Start in Domain layer (entities, enums, events)
2. Add application logic (commands/queries, handlers)
3. Implement infrastructure (repositories, external services)
4. Update database with migrations
5. Add API endpoints in controllers

### Modifying Approval Logic

See `ApproveUserCommandHandler.cs` - handles both approval status and email notifications for lecturers. Status depends on email confirmation state.

### Adding Email Templates

Implement in `Infrastructure/Services/EmailService.cs` and `IEmailService.cs` interface.

## Configuration

Required environment variables (see `.env.example`):
- `OPENAI_API_KEY`: For AI content analysis
- `CLAUDE_API_KEY`: Alternative AI provider
- `STRIPE_SECRET_KEY` / `STRIPE_PUBLISHABLE_KEY`: Payment processing
- `PAYPAL_CLIENT_ID` / `PAYPAL_CLIENT_SECRET`: Alternative payments

Database connections managed automatically by Aspire with default credentials.
