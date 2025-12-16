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
- Orchestration Service - Coordinates workflow (scrape → match → notify)
- Mock Notification Service - Writes results to console/log
- Configuration System - Load preferences from appsettings.json
- Console runner for manual execution

**Done when**: Can run manually to scrape concerts, match preferences, and see results in console

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
