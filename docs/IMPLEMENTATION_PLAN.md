# MetalWatch Implementation Plan

This document tracks implementation progress and future roadmap for MetalWatch.

## ✅ Completed Work

### Phase 0: Project Setup and HeavyMetalDk Scraper Implementation

**Goal**: Establish foundational architecture and implement first working scraper

**What was done**:

**Project Structure:**
- Created .NET 10.0 solution with four projects:
  - `MetalWatch.Core` - Domain models and interfaces (Clean Architecture core)
  - `MetalWatch.Infrastructure` - Scrapers, storage, notifications
  - `MetalWatch.Worker` - Background service host
  - `MetalWatch.Tests` - Unit and integration tests
- Configured nullable reference types (`<Nullable>enable</Nullable>`)
- Added key NuGet packages: HtmlAgilityPack, xUnit, FluentAssertions, Microsoft.Extensions.Logging

**Domain Models:**
- Created `Concert` model with `required` properties (Id, Venue, ConcertUrl, DayOfWeek)
- Created `ConcertPreferences` model for user filtering preferences
- Created result models: `ScraperResult` and `NotificationResult`
- Used `required` keyword for compile-time property initialization safety

**Core Interfaces:**
- `IConcertScraper` - Web scraping abstraction
- `IScraperFactory` - Strategy pattern for scraper selection
- `IConcertMatcher` - Concert matching and scoring
- `IDataStore` - Storage abstraction (JSON files, S3-compatible)
- `INotificationService` - Pluggable notifications

**HeavyMetalDk Scraper:**
- Built table-based HTML parser using HtmlAgilityPack
- Extracts concerts from heavymetal.dk calendar page
- Handles Danish-specific markers ("Aflyst", "Ny") and translates to standard domain model
- Supports festivals, multi-artist shows, and year rollover
- Returns standardized `Concert` objects with proper error handling

**Supporting Services:**
- `ConcertMatcherService` with scoring algorithm (exact artist match: +100, favorite venue: +50, keywords: +25)
- `JsonDataStore` for local development storage
- `ScraperFactory` for routing URLs to appropriate scrapers

**Test Infrastructure:**
- Created HTML test fixtures in `tests/MetalWatch.Tests/Fixtures/HeavyMetalDk/`
- 13 unit tests covering parsing scenarios
- 8 integration tests for end-to-end workflows
- All 23 tests passing (100% success rate)
- Test coverage: ~85%+ of core scraping logic

**Key decisions**:
- Used .NET 10.0 for latest language features (`required` keyword)
- Chose Worker Service over Azure Functions for EU deployment flexibility
- No Azure dependencies (S3-compatible storage instead)
- Domain model uses standard .NET types; scrapers handle site-specific translations
- Result objects preferred over exceptions for flow control
- All external integrations use interfaces for testability

## 🚧 In Progress

Nothing currently in progress.

## 📋 Upcoming Work

### Phase 1: Complete Core Workflow
**Goal**: Get the basic end-to-end workflow running locally

**What to build**:

#### 1. Domain Events & Event Bus
**Files**:
- `src/MetalWatch.Core/Events/IDomainEvent.cs`
- `src/MetalWatch.Core/Events/IEventBus.cs`
- `src/MetalWatch.Core/Events/ConcertsScrapedEvent.cs`
- `src/MetalWatch.Core/Events/NewConcertsFoundEvent.cs`
- `src/MetalWatch.Infrastructure/Events/InMemoryEventBus.cs`

**Design**: Event-driven architecture for decoupling concerns
- Domain events represent significant occurrences in the system
- Event bus for publishing and subscribing to events
- Separate transaction scopes for scraping/storage vs notifications

**Tasks**:
- Create `IDomainEvent` marker interface
- Create `IEventBus` interface:
  - `Task PublishAsync<TEvent>(TEvent domainEvent)` where TEvent : IDomainEvent
  - `void Subscribe<TEvent>(Func<TEvent, Task> handler)` where TEvent : IDomainEvent
- Create `ConcertsScrapedEvent`:
  - SourceUrl (string)
  - ScrapedConcerts (List<Concert>)
  - ScrapedAt (DateTime)
- Create `NewConcertsFoundEvent`:
  - NewConcerts (List<Concert>) - ALL new concerts (unfiltered)
  - SourceUrl (string)
  - FoundAt (DateTime)
- Implement `InMemoryEventBus` (simple in-memory pub/sub for Phase 1)
- Unit tests for event bus

**Note**: Phase 1 uses in-memory event bus. Future: Can swap with message queue (RabbitMQ, Azure Service Bus) for distributed scenarios.

#### 2. Orchestration Service
**Files**:
- `src/MetalWatch.Core/Interfaces/IConcertOrchestrationService.cs`
- `src/MetalWatch.Core/Models/OrchestrationResult.cs`
- `src/MetalWatch.Core/Services/ConcertOrchestrationService.cs`

**Design**: Per-source orchestration with event-driven workflow
- Each source has its own workflow execution
- Orchestrator accepts a source URL as parameter
- Publishes domain events at key steps
- Does NOT directly call notification service
- In Phase 1: Single source (heavymetal.dk)
- Future phases: Multiple sources with independent schedules

**Tasks**:
- Create `IConcertOrchestrationService` interface with `ExecuteWorkflowAsync(string sourceUrl)` method
- Create `OrchestrationResult` model:
  - Success (bool)
  - SourceUrl (string)
  - ConcertsScraped (int)
  - NewConcerts (int)
  - EventsPublished (List<string>)
  - ErrorMessage (string?)
  - ExecutedAt (DateTime)
- Implement orchestration workflow (separate transaction scopes):

  **Transaction 1: Scraping & Storage**
  1. Load previously discovered concerts from storage (represents current state of all sources)
  2. Scrape concerts from specified source URL
  3. Generate unique IDs for scraped concerts (Hash of Date + Venue + FirstArtist)
  4. Identify new concerts: concerts with IDs not in previously discovered set
  5. Save all currently scraped concerts to storage (replaces previous state for this source)
  6. **Publish `NewConcertsFoundEvent` with ALL new concerts** (triggers notification asynchronously)

  **Why save all scraped concerts:**
  - Storage represents "concerts currently listed on website"
  - Concerts that disappear from website naturally drop out
  - Next run: only concerts with new IDs are considered "new"
  - Prevents duplicate notifications (concert only "new" once when first discovered)

  **Transaction 2: Notification (handled by event subscriber)**
  - Separate event handler subscribes to `NewConcertsFoundEvent`
  - Event contains ALL new concerts (not filtered yet)
  - Handler loads user preferences from storage
  - Handler matches concerts against preferences using `IConcertMatcher`
  - Handler calls `INotificationService` with matching concerts only
  - If notification fails, scraping/storage is NOT rolled back

- Inject dependencies: `IScraperFactory`, `IDataStore`, `IEventBus`, `ILogger`
- Remove direct dependency on `INotificationService` from orchestrator
- Use result objects for flow control (no exceptions)
- Comprehensive error handling and logging at each step
- Unit tests with mocked dependencies (including mocked event bus)

**Concert ID Strategy**:
- **Change from Phase 0**: Concert.Id is no longer extracted from URL slug
- Generate unique ID based on concert properties: Hash(Date + Venue + FirstArtist)
- Use deterministic hash function (e.g., SHA256, truncated)
- Same concert from multiple sources = same ID (automatic deduplication)
- ID generation happens in orchestration service, not in scrapers
- Scrapers can stop setting the Id property (orchestrator will override)

**Model Update Required**:
- Update `Concert.cs` XML comments: Change "Unique identifier derived from concert URL slug" to "Unique identifier generated from concert properties (date, venue, artist)"
- Keep Id as `required string` but value is set by orchestration, not scraper

#### 3. Mock Notification Service
**File**: `src/MetalWatch.Infrastructure/Notifications/ConsoleNotificationService.cs`

**Tasks**:
- Implement `INotificationService` interface for console output
- Format matched concerts with date, artists, venue, score, URL
- Highlight favorites with ⭐ emoji
- Use colored console output (green for favorites)
- Return `NotificationResult` with success status
- Unit tests with sample concerts

#### 4. Notification Event Handler
**File**: `src/MetalWatch.Infrastructure/Events/NotificationEventHandler.cs`

**Design**: Event subscriber that handles notification logic
- Subscribes to `NewConcertsFoundEvent`
- Separate transaction scope from orchestration
- Receives ALL new concerts (unfiltered)
- Performs matching against user preferences
- If notification fails, scraping/storage is unaffected

**Tasks**:
- Create `NotificationEventHandler` class
- Subscribe to `NewConcertsFoundEvent` in constructor
- Implement handler:
  - Extract ALL new concerts from event (unfiltered)
  - Load user preferences from storage using `IDataStore`
  - Match concerts against preferences using `IConcertMatcher`
  - Call `INotificationService.SendNotificationAsync()` with matching concerts only
  - Log notification result (success/failure)
  - If notification fails, log error but don't throw exception
- Inject dependencies: `IDataStore`, `IConcertMatcher`, `INotificationService`, `ILogger`
- Unit tests with mocked dependencies (storage, matcher, notification service, event bus)

#### 5. Configuration System
**Files**:
- `src/MetalWatch.Core/Models/MetalWatchConfiguration.cs`
- `src/MetalWatch.Worker/appsettings.json`
- `src/MetalWatch.Worker/appsettings.Development.json`

**Tasks**:
- Create `MetalWatchConfiguration` model:
  - `SourceUrl` (string) - Single source for Phase 1
  - `Preferences` (ConcertPreferences) - Global preferences
  - `Storage` (StorageConfiguration) - Storage settings
- Create `appsettings.json`:
  - Single source URL: heavymetal.dk Copenhagen calendar
  - Sample global preferences: favorite artists, venues, keywords
  - Storage configuration (data directory)
- Create `appsettings.Development.json` with debug logging
- Configuration binding tests

**Note**: Phase 1 uses single source. Future phase will change `SourceUrl` to `Sources` array with per-source schedules.

#### 6. Console Application Setup
**Files**:
- `src/MetalWatch.Worker/Program.cs`
- `src/MetalWatch.Worker/MetalWatch.Worker.csproj`

**Tasks**:
- Create `Program.cs` with `HostBuilder` and dependency injection
- Register all services:
  - `IConcertOrchestrationService` → `ConcertOrchestrationService`
  - `IConcertMatcher` → `ConcertMatcherService`
  - `IScraperFactory` → `ScraperFactory`
  - `IDataStore` → `JsonDataStore`
  - `INotificationService` → `ConsoleNotificationService`
  - `IEventBus` → `InMemoryEventBus` (singleton)
  - `NotificationEventHandler` (register and initialize to subscribe to events)
- Configure logging (console logger)
- Load and bind configuration from appsettings.json
- Initialize event handlers (ensure NotificationEventHandler subscribes)
- Get configured source URL from configuration
- Call orchestrator: `await orchestrator.ExecuteWorkflowAsync(sourceUrl)`
- Display results (concerts scraped, new concerts, events published)
- Exit with appropriate status code (0 = success, 1 = error)
- Add NuGet packages: Microsoft.Extensions.Hosting, Configuration.Json, Http
- Ensure appsettings files copy to output directory
- Add project references to Core and Infrastructure

**Note**: In Phase 1, single source URL from config. Future: loop through multiple sources with scheduling.

#### 7. Integration Testing
**File**: `tests/MetalWatch.Tests/Integration/WorkflowIntegrationTests.cs`

**Tasks**:
- End-to-end test with real components (mock HTTP for scraper)
- Test first run (no previous concerts)
- Test subsequent run (some new concerts)
- Test no new concerts scenario
- Test no matches scenario
- Test event publishing and handling
- Verify notification triggered via event (not direct call)
- Verify orchestration produces expected results

#### 8. Containerization
**Files**:
- `Dockerfile`
- `.dockerignore`

**Tasks**:
- Create multi-stage Dockerfile:
  - Stage 1: Build - restore packages and compile
  - Stage 2: Runtime - minimal runtime image
- Use .NET 10 SDK for build, .NET 10 runtime for execution
- Copy appsettings.json to container
- Set working directory and entry point
- Create `.dockerignore` to exclude unnecessary files (bin, obj, .git, etc.)
- Support configuration override via environment variables
- **Stateless design**: Service should not depend on local filesystem state
  - For now: Service runs fresh each time (no persistence between runs)
  - All concerts are treated as "new" every run
  - Future: Will connect to external storage (S3-compatible in Phase 5)

**Architecture Note**:
The service is designed to be stateless. The `IDataStore` abstraction allows swapping storage implementations:
- **Phase 1**: `JsonDataStore` (local files, development only - optional persistence via volume mount)
- **Phase 5**: `S3CompatibleDataStore` (external storage, production)

For containerized deployment in Phase 1, you can either:
1. Run without volume mount → Fresh state every run → All concerts are "new"
2. Run with volume mount → Persistent state → Testing the "new concert detection" logic

**Example Dockerfile structure**:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
```

#### 9. Documentation & Verification
**Tasks**:
- Document how to run locally: `dotnet run --project src/MetalWatch.Worker`
- Document how to build Docker image: `docker build -t metalwatch .`
- Document how to run in container (stateless): `docker run metalwatch`
- Document how to run with local storage (testing): `docker run -v $(pwd)/concert-data:/app/concert-data metalwatch`
- Document how to override configuration with environment variables
- Document how to modify preferences in appsettings.json
- Manual testing:
  - Build and run tests: `dotnet test`
  - Run locally: `dotnet run --project src/MetalWatch.Worker`
  - Build Docker image
  - Run container (stateless mode)
  - Run container (with volume mount for testing persistence logic)
- Verify concerts scraped, matches found, output displayed
- Verify service works in stateless mode (treats all concerts as new)

**Done when**:
- Can run `dotnet run --project src/MetalWatch.Worker` successfully
- Can build Docker image without errors
- Can run container in stateless mode and see console output
- Console displays matched concerts with details
- All existing tests passing (23 tests)
- New integration tests passing
- Configuration loads from appsettings.json
- Service works correctly when running fresh each time (stateless)
- Volume mount optional for local testing of persistence logic

---

### Phase 2: CI/CD Pipeline
**Goal**: Automate build, test, and deployment

**What to build**:
- GitHub Actions workflow for build and test
- Automated test execution on PR
- Automated deployment pipeline

**Done when**: Every commit runs tests; merge to master triggers deployment

---

### Phase 3: Deployment + Background Worker
**Goal**: Deploy to production with scheduled execution

**What to build**:
- Worker Service with timed execution (e.g., daily at 8 AM)
- Containerization (Docker)
- Deployment to EU cloud provider
- Environment-specific configuration
- Secret management via environment variables
- Structured logging and health checks

**Done when**: Service running in production, scraping automatically on schedule

---

### Phase 4: Monitoring & Observability
**Goal**: Ensure reliability and visibility

**What to build**:
- Metrics (concerts scraped, matches found, notifications sent)
- Alerting for failures
- Log aggregation

**Done when**: Can monitor system health and troubleshoot issues

---

### Phase 5: Production Storage
**Goal**: Track concert history with persistent storage

**What to build**:
- S3-compatible storage implementation
- Store and retrieve concert history
- Track "new" vs "previously seen" concerts

**Done when**: Only notifies about newly discovered concerts

---

### Phase 6: Email Notifications
**Goal**: Send actual email notifications

**What to build**:
- Email Notification Service using MailKit
- HTML email template
- Replace mock notification with real implementation

**Done when**: Users receive emails when matching concerts are found

---

### Future Enhancements (Not Prioritized)
- Multi-source support (additional concert websites)
- Enhanced notifications (Slack, Discord, better templates)
- Web dashboard for preference management
- Multi-user support
- ML-based recommendations
- Geographic expansion

## Development Principles

As we continue development, we follow these principles:

1. **Type Safety**: Use `required` keyword, avoid nullable when not needed
2. **Clean Architecture**: Keep domain logic separate from infrastructure
3. **EU Sovereignty**: No vendor lock-in, S3-compatible storage
4. **Interface-Driven**: All external integrations use interfaces
5. **Test Coverage**: Write tests for new functionality
6. **Fail-Fast**: Catch errors at compile-time when possible

## Notes

- Implementation plan evolves as we learn and iterate
- Details added for completed work; future work remains high-level
- Refer to [IMPLEMENTATION_STATUS.md](../IMPLEMENTATION_STATUS.md) for detailed current status
- Refer to [ARCHITECTURE.md](ARCHITECTURE.md) for system design
