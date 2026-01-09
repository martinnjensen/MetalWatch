# AI Agent Guide for MetalWatch

This document provides guidelines for AI assistants working on the MetalWatch project.

## Quick Reference

- **Architecture**: See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for system design
- **Project Overview**: See [README.md](README.md) for features and setup
- **Current Status**: See [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md)

## Critical Design Principles

### 1. Type Safety First

**Use `required` keyword for non-nullable properties:**
```csharp
// GOOD - Compile-time safety
public class Concert
{
    public required string Id { get; set; }
    public required string Venue { get; set; }
    public string? OptionalField { get; set; }  // Only if truly optional
}

// BAD - Avoid empty string defaults
public string Id { get; set; } = string.Empty;
```

**Rationale**: Fail-fast at instantiation, not at usage. Prefer compile-time errors over runtime null references.

### 2. Clean Architecture Boundaries

- **Core layer**: Pure business logic, no external dependencies
- **Infrastructure layer**: Implements Core interfaces with concrete technologies
- **Worker layer**: Thin hosting layer, minimal logic

**Never**: Put infrastructure concerns (HTTP, storage, email) in Core layer.

### 3. EU Sovereignty

- **No Azure dependencies**: Project uses S3-compatible storage, not Azure Blob
- **EU deployment targets**: Hetzner, OVHcloud, IONOS, or self-hosted
- **Why**: Privacy, data sovereignty, avoiding vendor lock-in

### 4. Interface-Driven Design

All external integrations use interfaces for testability and flexibility.

### 5. Test-Driven Development (TDD)

**All implementation work MUST follow TDD:**

1. **Red Phase**: Write failing tests first
   - Create test class with test methods that define expected behavior
   - Create skeleton/stub implementation (interfaces, empty classes)
   - Commit with message: "Add [feature] tests (red phase)"

2. **Green Phase**: Implement logic to make tests pass
   - Write minimal code to pass the tests
   - Commit with message: "Implement [feature] logic (green phase)"

3. **Refactor Phase** (optional): Clean up while keeping tests green
   - Improve code quality without changing behavior
   - Commit with message: "Refactor [feature]"

**Example workflow:**
```bash
# Red phase - tests fail
git commit -m "Add ConcertTrackerService tests (red phase)"

# Green phase - tests pass
git commit -m "Implement ConcertTrackerService logic (green phase)"
```

**Rationale**: TDD ensures testable design, documents expected behavior, and catches regressions early.

## Code Conventions

### XML Documentation

All public APIs require XML documentation:
```csharp
/// <summary>
/// Scrapes concerts from the specified URL
/// </summary>
/// <param name="url">The concert calendar URL to scrape</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>List of scraped concerts</returns>
Task<ScraperResult> ScrapeAsync(string url, CancellationToken cancellationToken = default);
```

### Naming Conventions

- **Interfaces**: `IServiceName` (e.g., `IConcertScraper`)
- **Implementations**: `ConcreteServiceName` (e.g., `HeavyMetalDkScraper`)
- **Models**: Singular nouns (e.g., `Concert`, not `Concerts`)
- **Tests**: `ClassNameTests` (e.g., `HeavyMetalDkScraperTests`)

### Async/Await

- Always use `async`/`await` for I/O operations
- Always accept `CancellationToken` for async methods
- Name async methods with `Async` suffix

### Error Handling

- Use result objects like `ScraperResult`, `NotificationResult` (not exceptions for flow control)
- Log errors with structured logging (`ILogger<T>`)
- Throw exceptions only for truly exceptional cases

## Testing Guidelines

### Test Organization

- **Unit tests**: Mock all external dependencies
- **Integration tests**: Use real implementations with fixtures
- **Test naming**: `MethodName_Scenario_ExpectedResult`

### Test Fixtures

Store real HTML snapshots in `tests/MetalWatch.Tests/Fixtures/{SourceName}/`:
- Use actual HTML from live sites for realistic testing
- Cover edge cases: single concerts, festivals, cancelled events
- Name files descriptively (e.g., `cancelled-concert.html`)

**Always use fixtures for testing** - Don't hit live websites in tests.

### Example Test Structure

```csharp
[Fact]
public async Task ScrapeAsync_WithCancelledConcert_SetsCancelledFlag()
{
    // Arrange
    var scraper = new ConcertScraper(_logger);
    var html = File.ReadAllText("Fixtures/SourceName/cancelled-concert.html");

    // Act
    var result = await scraper.ScrapeAsync(html);

    // Assert
    result.Success.Should().BeTrue();
    result.Concerts.Should().HaveCount(1);
    result.Concerts[0].IsCancelled.Should().BeTrue();
}
```

### Test Coverage Expectations

- Core services: >90% coverage
- Infrastructure: >80% coverage
- Integration tests for all major workflows

## Common Tasks

### Adding a New Concert Source

1. Create class implementing `IConcertScraper` in `Infrastructure/Scrapers/`
2. Add test fixtures from the new source
3. Write tests for parsing logic
4. Implement scraper to return standardized `Concert` objects
5. Register in `ScraperFactory` with source configuration

### Adding a New Notification Channel

1. Create class implementing `INotificationService` in `Infrastructure/Notifications/`
2. Add configuration section in `appsettings.json`
3. Register in DI container
4. Add tests using mocked HTTP client or similar

### Adding a New Storage Backend

1. Create class implementing `IDataStore` in `Infrastructure/Storage/`
2. Implement CRUD operations
3. Add configuration for connection details
4. Register in DI container with conditional logic
5. Add round-trip integration tests

## Things to Avoid

❌ **Don't** add Azure-specific dependencies (Functions, Blob Storage, etc.)
❌ **Don't** use nullable properties when `required` is appropriate
❌ **Don't** put business logic in Infrastructure or Worker layers
❌ **Don't** make HTTP calls in unit tests (use fixtures or mocks)
❌ **Don't** use `= string.Empty` defaults (use `required string` or `string?`)
❌ **Don't** commit secrets (credentials, API keys, tokens) to version control
❌ **Don't** push or merge code containing secrets to the git repository
❌ **Don't** create documentation files unless explicitly requested
❌ **Don't** implement features without writing tests first (TDD red phase)

✅ **Do** use dependency injection for all services
✅ **Do** follow TDD: write failing tests before implementation
✅ **Do** commit after each TDD phase (red, green, refactor)
✅ **Do** use structured logging with `ILogger<T>`
✅ **Do** use `CancellationToken` for async operations
✅ **Do** keep commits focused and atomic

## Development Workflow

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~HeavyMetalDkScraperTests"

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Making Changes (TDD Workflow)

1. Create feature branch from `master`
2. **Red phase**: Write failing tests + skeleton code, commit
3. **Green phase**: Implement logic to make tests pass, commit
4. **Refactor** (optional): Clean up code, commit
5. Ensure all tests pass
6. Create pull request

## Debugging Tips

### Scraper Issues

1. Check HTML fixture matches current website structure
2. Verify XPath selectors in browser dev tools
3. Test with actual HTML saved from website
4. Check encoding (UTF-8) for special characters
5. Validate required properties are being extracted correctly

### Test Failures

1. Ensure fixtures are up to date
2. Check for hardcoded dates that may have expired
3. Verify required properties are set in test data
4. Use `.Should().BeEquivalentTo()` for complex object assertions

## Questions?

When in doubt:
1. Check existing code for patterns
2. Look at test examples
3. Review [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
4. Prioritize type safety and testability
