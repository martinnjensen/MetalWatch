# MetalWatch Architecture

## Overview

MetalWatch is a concert monitoring service that scrapes heavy metal concerts from various concert sites and sends personalized notifications based on user preferences.

**Core Purpose**: Help heavy metal fans discover concerts they'll love without manually checking websites daily.

The system follows Clean Architecture principles with clear separation between domain logic, infrastructure concerns, and hosting.

## Technology Stack

- **Language**: C# (.NET 10.0)
- **Architecture**: Clean Architecture (Core → Infrastructure → Worker)
- **Key Libraries**:
  - `HtmlAgilityPack` - HTML parsing for web scraping
  - `MailKit` - Email notifications
  - `xUnit` - Testing framework
- **Deployment**: Worker service designed for EU sovereign cloud providers
- **Storage**: S3-compatible object storage (MinIO, OpenStack Swift, etc.)

## Project Structure

```
MetalWatch/
├── src/
│   ├── MetalWatch.Core/           # Domain logic (framework-independent)
│   │   ├── Models/                # Domain entities
│   │   ├── Interfaces/            # Contracts/abstractions
│   │   └── Services/              # Core business services
│   ├── MetalWatch.Infrastructure/ # External integrations
│   │   ├── Scrapers/              # Concert source implementations
│   │   ├── Storage/               # Data persistence
│   │   └── Notifications/         # Notification channels
│   └── MetalWatch.Worker/         # Background service host
├── tests/
│   └── MetalWatch.Tests/          # Unit and integration tests
└── docs/                          # Architecture and planning docs
```

## Layer Breakdown

### Core Layer (MetalWatch.Core)

The core layer contains all business logic and is independent of external frameworks and infrastructure.

#### Models

**Concert** - Represents a concert event

```csharp
public class Concert
{
    public required string Id { get; set; }           // Unique identifier (URL slug)
    public DateTime Date { get; set; }                // Concert date
    public required string DayOfWeek { get; set; }    // Day of week (standardized by scraper)
    public List<string> Artists { get; set; } = new(); // Performing artists
    public required string Venue { get; set; }        // Venue name
    public required string ConcertUrl { get; set; }   // Link to concert page
    public bool IsCancelled { get; set; }             // Concert cancelled
    public bool IsNew { get; set; }                   // Newly added concert
    public bool IsFestival { get; set; }              // Multi-artist event
    public DateTime ScrapedAt { get; set; }           // When data was collected
}
```

**Important**: The domain model uses standard .NET types. Individual scrapers handle translation from site-specific formats (e.g., Danish day names, date formats, cancellation markers) to these standardized properties.

**ConcertPreferences** - User preferences for filtering

```csharp
public class ConcertPreferences
{
    public List<string> FavoriteArtists { get; set; } = new();
    public List<string> FavoriteVenues { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? NotificationEmail { get; set; }
}
```

**NotificationResult** - Result of notification operations

```csharp
public class NotificationResult
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public int ConcertsNotified { get; set; }
    public DateTime SentAt { get; set; }
}
```

**ScraperResult** - Result of scraping operations

```csharp
public class ScraperResult
{
    public bool Success { get; set; }
    public List<Concert> Concerts { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int ConcertsScraped { get; set; }
    public DateTime ScrapedAt { get; set; }
}
```

#### Interfaces

**IConcertScraper** - Web scraping abstraction

```csharp
public interface IConcertScraper
{
    Task<ScraperResult> ScrapeAsync(string url, CancellationToken cancellationToken = default);
}
```

- Abstracts the web scraping functionality
- Allows testing with mock scrapers
- Supports multiple concert sources

**IConcertMatcher** - Concert matching logic

```csharp
public interface IConcertMatcher
{
    List<Concert> FindMatches(List<Concert> concerts, ConcertPreferences preferences);
    int CalculateRelevanceScore(Concert concert, ConcertPreferences preferences);
}
```

- Encapsulates matching logic
- Scores concerts based on preferences
- Filters and ranks results

**INotificationService** - Notification abstraction

```csharp
public interface INotificationService
{
    Task<NotificationResult> SendNotificationAsync(
        List<Concert> concerts,
        CancellationToken cancellationToken = default);
}
```

- Pluggable notification system
- Supports multiple implementations (email, Slack, Discord)
- Returns result for logging/monitoring

**IDataStore** - Storage abstraction

```csharp
public interface IDataStore
{
    Task<List<Concert>> GetPreviousConcertsAsync();
    Task SaveConcertsAsync(List<Concert> concerts);
    Task<ConcertPreferences> GetPreferencesAsync();
}
```

- Abstract storage layer
- Supports different backends (JSON, S3-compatible, Database)
- Handles concert history and preferences

**IScraperFactory** - Creates appropriate scraper for source

```csharp
public interface IScraperFactory
{
    IConcertScraper CreateScraper(string sourceUrl);
}
```

#### Services

**ConcertMatcherService** - Implements matching algorithm

- Implements scoring algorithm
- Filters concerts by date range
- Returns sorted results by relevance

**Scoring Algorithm**:

| Match Type | Points |
|------------|--------|
| Exact artist match (case-insensitive) | +100 |
| Favorite venue | +50 |
| Keyword in artist name | +25 per keyword |

Concerts with score > 0 are included in notifications, sorted by score descending.

### Infrastructure Layer (MetalWatch.Infrastructure)

The infrastructure layer implements the interfaces defined in Core using external dependencies.

#### Scrapers

**HeavyMetalDkScraper** - Scraper for heavymetal.dk

- Implements `IConcertScraper` for heavymetal.dk
- Uses HtmlAgilityPack for HTML parsing
- Handles Danish date format parsing
- Extracts concert details from page structure
- Translates Danish markers ("Aflyst", "Ny") to boolean flags
- Normalizes venue names (strips location suffixes)

**General Scraping Strategy**:
- Each concert source implements `IConcertScraper` interface
- Parse HTML using `HtmlAgilityPack` with XPath selectors
- Return standardized `Concert` objects regardless of source
- Handle encoding properly (UTF-8 for special characters)
- Extract required fields: Id, Venue, ConcertUrl
- Extract standard fields: Date (DateTime)
- Support optional fields: DayOfWeek, Artists, IsCancelled, IsNew, IsFestival

**ScraperFactory** - Routes to appropriate scraper

- Examines source URL to determine which scraper to use
- Returns appropriate `IConcertScraper` implementation
- Extensible for additional concert sources

#### Storage

**JsonDataStore** - File-based storage for development

- Stores concerts in `concert-data/concerts.json`
- Stores preferences in `concert-data/preferences.json`
- Simple and portable
- Used for local development

**S3CompatibleDataStore** - Production storage

- Uses S3-compatible object storage (MinIO, OpenStack Swift, etc.)
- Stores data as JSON blobs
- Supports any S3-compatible provider
- Cost-effective for small data volumes
- Works with EU sovereign cloud providers (Hetzner, OVHcloud, IONOS)

#### Notifications

**EmailNotificationService** - Email implementation

- Implements email notifications using MailKit
- Generates HTML email with concert details
- Includes relevance indicators (favorite artist/venue)
- Links directly to concert pages

**Email Template Structure**:
```
Subject: 🎸 X New Metal Concerts - [Date]

Body:
- Header with date and count
- List of concerts:
  - Date and day of week
  - Artist names (highlighted if favorite)
  - Venue (highlighted if favorite)
  - Direct link to concert page
- Footer with preference update instructions
```

### Worker Layer (MetalWatch.Worker)

Thin hosting layer for background service.

**DailyConcertCheckWorker** - Scheduled background worker

- Runs on configured schedule (e.g., daily at 8 AM)
- Dependency injection setup
- Logging configuration
- Invokes orchestration service

## Data Flow

```
Timer/Schedule
    ↓
Worker Service
    ↓
Orchestration Service
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
services.AddScoped<IDataStore, S3CompatibleDataStore>(); // or JsonDataStore for dev
services.AddScoped<IScraperFactory, ScraperFactory>();
```

## Configuration

### Environment-Specific Settings

- **Development**: `appsettings.Development.json`
- **Production**: Environment variables or config file
- **Secrets**: Use environment variables or secret managers (never commit)

### Key Configuration Sections

```json
{
  "MetalWatch": {
    "SourceUrl": "https://heavymetal.dk/koncertkalender?landsdel=koebenhavn",
    "Preferences": {
      "FavoriteArtists": ["Metallica", "Iron Maiden"],
      "FavoriteVenues": ["Pumpehuset", "VEGA"],
      "Keywords": ["thrash", "death metal"]
    },
    "Notification": {
      "Type": "Email",
      "Email": {
        "To": "your@email.com",
        "From": "concerts@yourdomain.com",
        "SmtpHost": "smtp.gmail.com",
        "SmtpPort": 587,
        "Username": "your@gmail.com",
        "Password": "***"
      }
    },
    "Storage": {
      "Type": "S3Compatible",
      "S3Endpoint": "https://s3.eu-central-1.example.com",
      "S3AccessKey": "***",
      "S3SecretKey": "***",
      "S3BucketName": "concert-data",
      "S3Region": "eu-central-1"
    }
  }
}
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
3. Translate site-specific formats to standardized `Concert` properties
4. Register in `ScraperFactory`
5. Add test fixtures and tests

### Adding Database Storage

1. Create class implementing `IDataStore`
2. Add Entity Framework Core models
3. Implement CRUD operations
4. Register in DI with connection string

## Error Handling

**Scraping Failures**:
- Return `ScraperResult` with `Success = false`
- Log detailed error information
- Continue workflow (don't crash)

**Notification Failures**:
- Return `NotificationResult` with `Success = false`
- Log but don't block the workflow
- Consider fallback notification channel

**Storage Failures**:
- Critical failure - should log and alert
- Without storage, we can't track concert history

## Performance Considerations

- Single HTTP request per scrape (downloads full calendar page)
- Efficient HTML parsing with XPath selectors
- Minimal memory footprint (<50MB typical)
- Fast execution (<5 seconds end-to-end)

## Deployment Architecture

### EU Sovereign Cloud Deployment

```
EU Cloud Provider (Hetzner/OVHcloud/IONOS)
├── VM or Container Service
│   └── Worker Service (scheduled execution)
├── S3-Compatible Object Storage
│   └── concert-data bucket
└── (Optional) Email Service
```

### Cost Estimation (EU Sovereign Cloud)

**Hetzner Cloud (German Provider)**:
- CX11 VM (2 vCPU, 2GB RAM): ~€4/month
- Object Storage (250 GB included): ~€5/month
- **Total**: ~€9/month

**OVHcloud (French Provider)**:
- B2-7 VM (2 vCPU, 7GB RAM): ~€7/month
- Object Storage (Pay-as-you-go): ~€0.01/GB
- **Total**: ~€7-10/month

## Security Considerations

**Credentials**:
- Store in environment variables or secret managers
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

## Known Issues & Quirks

### Website Scraping

- HTML structures may change without notice - monitor for parsing failures
- Implement graceful degradation when parsing fails
- Consider date format variations (DD/MM vs MM/DD, year handling)
- Normalize venue names (strip location suffixes, standardize formatting)

### Date Handling

- Always extract timezone information or default to event location timezone
- Handle year rollover properly (events in December/January)
- Support multiple date formats depending on source
