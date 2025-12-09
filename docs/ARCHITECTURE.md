# MetalWatch Architecture

## Overview

MetalWatch follows Clean Architecture principles with clear separation between domain logic, infrastructure concerns, and hosting.

## Layer Breakdown

### Core Layer (MetalWatch.Core)

The core layer contains all business logic and is independent of external frameworks and infrastructure.

#### Models

**Concert.cs**
```csharp
public class Concert
{
    public string Id { get; set; }              // Unique identifier (URL slug)
    public DateTime Date { get; set; }          // Concert date
    public string DayOfWeek { get; set; }       // Day of week (in Danish)
    public List<string> Artists { get; set; }   // Performing artists
    public string Venue { get; set; }           // Venue name
    public string ConcertUrl { get; set; }      // Link to concert page
    public bool IsCancelled { get; set; }       // Marked as "Aflyst"
    public bool IsNew { get; set; }             // Marked as "Ny"
    public bool IsFestival { get; set; }        // Multi-artist event
    public DateTime ScrapedAt { get; set; }     // When data was collected
}
```

**ConcertPreferences.cs**
```csharp
public class ConcertPreferences
{
    public List<string> FavoriteArtists { get; set; }
    public List<string> FavoriteVenues { get; set; }
    public List<string> Keywords { get; set; }  // Genre keywords to match
    public DateTime? StartDate { get; set; }    // Filter concerts after date
    public DateTime? EndDate { get; set; }      // Filter concerts before date
    public string NotificationEmail { get; set; }
}
```

**NotificationResult.cs**
```csharp
public class NotificationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int ConcertsNotified { get; set; }
}
```

#### Interfaces

**IConcertScraper.cs**
- Abstracts the web scraping functionality
- Allows testing with mock scrapers
- Can support multiple concert sources

```csharp
public interface IConcertScraper
{
    Task<List<Concert>> ScrapeAsync(string url, CancellationToken cancellationToken = default);
}
```

**IConcertMatcher.cs**
- Encapsulates matching logic
- Scores concerts based on preferences
- Filters and ranks results

```csharp
public interface IConcertMatcher
{
    List<Concert> FindMatches(List<Concert> concerts, ConcertPreferences preferences);
    int CalculateRelevanceScore(Concert concert, ConcertPreferences preferences);
}
```

**INotificationService.cs**
- Pluggable notification system
- Supports multiple implementations (email, Slack, Discord)
- Returns result for logging/monitoring

```csharp
public interface INotificationService
{
    Task<NotificationResult> SendNotificationAsync(
        List<Concert> concerts,
        CancellationToken cancellationToken = default);
}
```

**IDataStore.cs**
- Abstract storage layer
- Supports different backends (JSON, Blob, Database)
- Handles concert history and preferences

```csharp
public interface IDataStore
{
    Task<List<Concert>> GetPreviousConcertsAsync();
    Task SaveConcertsAsync(List<Concert> concerts);
    Task<ConcertPreferences> GetPreferencesAsync();
}
```

#### Services

**ConcertScraperService.cs**
- Orchestrates scraping process
- Handles parsing and normalization
- Error handling for network issues

**ConcertMatcherService.cs**
- Implements scoring algorithm
- Filters concerts by date range
- Returns sorted results by relevance

**ConcertTrackerService.cs**
- Main orchestrator service
- Coordinates scraping, matching, and notification
- Implements the daily workflow logic

### Infrastructure Layer (MetalWatch.Infrastructure)

The infrastructure layer implements the interfaces defined in Core using external dependencies.

#### Scrapers

**HeavyMetalDkScraper.cs**
- Implements `IConcertScraper` for heavymetal.dk
- Uses HtmlAgilityPack for HTML parsing
- Handles Danish date format parsing
- Extracts concert details from page structure

**Parsing Strategy**:
1. Load HTML document
2. Find all text nodes and anchor tags
3. Use state machine to group related elements:
   - Date pattern: `dd/mm (Day)` → Start new concert
   - Artist links: `/artist/*` → Add to current concert
   - Venue links: `/spillested/*` → Set venue for current concert
   - Concert links: `/koncert/*` → Set URL for current concert
4. Handle special markers ("Aflyst", "Ny")
5. Normalize dates to DateTime objects

**Key XPath/Selectors**:
- Concert entries: Sequential text and anchor nodes
- Artists: `//a[contains(@href, '/artist/')]`
- Venues: `//a[contains(@href, '/spillested/')]`
- Concert details: `//a[contains(@href, '/koncert/')]`

#### Storage

**JsonDataStore.cs**
- File-based storage for local development
- Stores concerts in `concert-data/concerts.json`
- Stores preferences in `concert-data/preferences.json`
- Simple and portable

**BlobStorageDataStore.cs**
- Production storage using Azure Blob Storage
- Stores data as JSON blobs
- Leverages Azure's durability and availability
- Cost-effective for small data volumes

#### Notifications

**EmailNotificationService.cs**
- Implements email notifications using MailKit
- Generates HTML email with concert details
- Includes relevance indicators (favorite artist/venue)
- Links directly to concert pages

**Email Template Structure**:
```
Subject: 🎸 X New Metal Concerts in Copenhagen - [Date]

Body:
- Header with date and count
- List of concerts:
  - Date and day of week
  - Artist names (highlighted if favorite)
  - Venue (highlighted if favorite)
  - Direct link to concert page
- Footer with how to update preferences
```

### Function Layer (MetalWatch.Function)

Thin hosting layer for Azure Functions.

**DailyConcertCheck.cs**
- Timer trigger: `0 0 8 * * *` (8 AM daily)
- Dependency injection setup
- Logging configuration
- Invokes `IConcertTrackerService`

**host.json**
- Function app configuration
- Logging levels
- Timer trigger settings

**local.settings.json**
- Local development settings
- Connection strings
- Application configuration

## Data Flow

```
Timer Trigger (8:00 AM)
    ↓
DailyConcertCheck Function
    ↓
ConcertTrackerService.CheckAndNotifyAsync()
    ↓
┌──────────────────────────────────────────┐
│ 1. Load previous concerts from storage  │
│ 2. Load user preferences                │
│ 3. Scrape current concerts               │
│ 4. Identify new concerts                 │
│ 5. Match against preferences            │
│ 6. Send notifications                    │
│ 7. Save updated concert list             │
└──────────────────────────────────────────┘
    ↓
Log results
```

## Dependency Injection

All services are registered in the DI container:

```csharp
services.AddScoped<IConcertScraper, HeavyMetalDkScraper>();
services.AddScoped<IConcertMatcher, ConcertMatcherService>();
services.AddScoped<INotificationService, EmailNotificationService>();
services.AddScoped<IDataStore, BlobStorageDataStore>(); // or JsonDataStore
services.AddScoped<IConcertTrackerService, ConcertTrackerService>();
```

## Extensibility Points

### Adding a New Notification Channel

1. Create class implementing `INotificationService`
2. Add configuration section for the channel
3. Register in DI container
4. Optionally support multiple channels simultaneously

Example: Slack Notification
```csharp
public class SlackNotificationService : INotificationService
{
    private readonly string _webhookUrl;

    public SlackNotificationService(IConfiguration config)
    {
        _webhookUrl = config["MetalWatch:Notification:Slack:WebhookUrl"];
    }

    public async Task<NotificationResult> SendNotificationAsync(
        List<Concert> concerts,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            text = $"🎸 {concerts.Count} new concerts found!",
            attachments = concerts.Select(c => new
            {
                title = string.Join(", ", c.Artists),
                text = $"{c.Date:dd/MM} - {c.Venue}",
                title_link = c.ConcertUrl
            })
        };

        // Send to Slack webhook
        // ...
    }
}
```

### Adding a New Concert Source

1. Create class implementing `IConcertScraper`
2. Implement parsing logic for the new site
3. Register in DI container
4. Update configuration with new source URL

### Adding Database Storage

1. Create class implementing `IDataStore`
2. Add Entity Framework Core models
3. Implement CRUD operations
4. Register in DI with connection string

## Configuration Management

**Local Development**: `local.settings.json`
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  },
  "MetalWatch": {
    // Configuration here
  }
}
```

**Production**: Azure Function Application Settings
- Use Azure Key Vault references for secrets
- Example: `@Microsoft.KeyVault(SecretUri=https://...)`

## Error Handling

**Scraping Failures**:
- Retry logic with exponential backoff
- Log detailed error information
- Send alert if multiple consecutive failures

**Notification Failures**:
- Log but don't block the workflow
- Store failed notifications for retry
- Consider fallback notification channel

**Storage Failures**:
- Critical failure - should alert immediately
- Without storage, we can't track concert history

## Monitoring and Observability

**Logging**:
- Structured logging using `ILogger`
- Log levels: Information, Warning, Error
- Key events: Scrape start/end, matches found, notifications sent

**Metrics** (via Azure Application Insights):
- Concert count per execution
- Match count per execution
- Notification success/failure rate
- Execution duration

**Alerts**:
- Failed function executions
- Zero concerts scraped (possible site change)
- Notification failures

## Security Considerations

**Credentials**:
- Store in Azure Key Vault
- Never commit to source control
- Rotate regularly

**Web Scraping**:
- Respect robots.txt
- Implement rate limiting
- Add User-Agent header
- Cache results appropriately

**Data Privacy**:
- User preferences stored securely
- No PII logged
- Comply with GDPR (European deployment)

## Performance Optimization

**Scraping**:
- Single HTTP request per execution
- Efficient HTML parsing
- Minimal memory footprint

**Storage**:
- JSON files < 1 MB
- Blob storage with CDN if needed
- No database overhead for simple use case

**Notifications**:
- Batch emails if multiple recipients
- Async processing
- Timeout handling

## Testing Strategy

**Unit Tests**:
- Core services with mocked dependencies
- Matching algorithm validation
- Date parsing edge cases

**Integration Tests**:
- Full workflow with real scraper
- Storage round-trip tests
- Notification delivery tests

**End-to-End Tests**:
- Deployed function execution
- Azure resources connectivity
- Email delivery verification

## Deployment Architecture

```
Azure Resource Group (West Europe)
├── Function App (Consumption Plan)
│   ├── Application Insights
│   └── Application Settings
├── Storage Account
│   ├── Function App Storage
│   └── Blob Container (concert-data)
└── Key Vault (optional)
    ├── Email Password
    └── Storage Connection String
```

## Cost Breakdown

**Azure Functions Consumption Plan**:
- 1 execution/day = ~30 executions/month
- ~5 seconds per execution
- Free tier: 1M executions, 400,000 GB-s
- Cost: €0 (within free tier)

**Blob Storage**:
- ~10 KB concert data
- ~1 KB preferences
- Minimal transactions
- Cost: <€0.10/month

**Application Insights**:
- Minimal telemetry
- Free tier: 5 GB/month
- Cost: €0 (within free tier)

**Total Monthly Cost**: <€1
