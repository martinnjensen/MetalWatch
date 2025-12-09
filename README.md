# MetalWatch

A cloud-based service that tracks heavy metal concerts in Copenhagen from [heavymetal.dk](https://heavymetal.dk/koncertkalender?landsdel=koebenhavn) and sends personalized notifications about concerts matching your preferences.

## Features

- **Daily Automated Scraping**: Checks the concert calendar daily for new shows
- **Smart Matching**: Filters concerts based on your favorite artists, venues, and keywords
- **Pluggable Notifications**: Email notifications with easy extensibility for other channels (Slack, Discord, etc.)
- **Cloud-Ready**: Designed for Azure Functions deployment in European regions
- **Concert Tracking**: Maintains history to identify new concerts and detect cancellations

## Architecture

### Project Structure

```
MetalWatch/
├── src/
│   ├── MetalWatch.Core/              # Core business logic (domain layer)
│   │   ├── Models/                   # Domain entities
│   │   │   ├── Concert.cs           # Concert entity
│   │   │   ├── ConcertPreferences.cs # User preferences
│   │   │   └── NotificationResult.cs # Notification response
│   │   ├── Interfaces/               # Contracts/abstractions
│   │   │   ├── IConcertScraper.cs   # Scraper contract
│   │   │   ├── IConcertMatcher.cs   # Matching logic contract
│   │   │   ├── INotificationService.cs # Pluggable notifications
│   │   │   └── IDataStore.cs        # Storage abstraction
│   │   └── Services/                 # Core orchestration services
│   │       ├── ConcertScraperService.cs
│   │       ├── ConcertMatcherService.cs
│   │       └── ConcertTrackerService.cs
│   │
│   ├── MetalWatch.Infrastructure/    # External dependencies (infrastructure layer)
│   │   ├── Scrapers/
│   │   │   └── HeavyMetalDkScraper.cs # Specific scraper implementation
│   │   ├── Storage/
│   │   │   ├── JsonDataStore.cs     # JSON file storage (local dev)
│   │   │   └── BlobStorageDataStore.cs # Azure Blob storage (production)
│   │   └── Notifications/
│   │       └── EmailNotificationService.cs # Email implementation
│   │
│   ├── MetalWatch.Function/          # Azure Function host
│   │   ├── DailyConcertCheck.cs     # Timer trigger function
│   │   ├── host.json
│   │   └── local.settings.json
│   │
└── tests/
    └── MetalWatch.Tests/             # Unit and integration tests
```

### Key Design Decisions

**1. Pluggable Notifications**
- All notification services implement `INotificationService`
- Easy to add new channels (Slack, Discord, Telegram) without modifying core logic
- Configuration-driven selection of notification provider

**2. Storage Abstraction**
- `IDataStore` interface allows switching between storage backends
- JSON files for local development
- Azure Blob Storage for production
- Easy to add database support if needed

**3. Clean Architecture**
- Core layer contains business logic, independent of frameworks
- Infrastructure layer contains external dependencies
- Function layer is thin hosting layer

## Technology Stack

- **Language**: C# (.NET 8.0)
- **Web Scraping**: HtmlAgilityPack
- **Email**: MailKit
- **Cloud Platform**: Azure Functions (Consumption Plan)
- **Storage**: Azure Blob Storage
- **Deployment Region**: West Europe (Netherlands) or North Europe (Ireland)

## Configuration

The service is configured via `appsettings.json` or Azure Function application settings:

```json
{
  "MetalWatch": {
    "SourceUrl": "https://heavymetal.dk/koncertkalender?landsdel=koebenhavn",
    "Preferences": {
      "FavoriteArtists": ["Metallica", "Iron Maiden"],
      "FavoriteVenues": ["Pumpehuset", "VEGA"],
      "Keywords": ["thrash", "death metal", "black metal"],
      "StartDate": "2025-01-01",
      "EndDate": "2025-12-31"
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
      "Type": "BlobStorage",
      "BlobConnectionString": "***",
      "ContainerName": "concert-data"
    }
  }
}
```

## Concert Matching Logic

Concerts are scored based on relevance:

- **Exact artist match**: +100 points
- **Favorite venue**: +50 points
- **Keyword match in artist name**: +25 points per keyword

Concerts with a score > 0 are included in notifications.

## Daily Workflow

1. **Fetch** - Download concert calendar HTML page
2. **Parse** - Extract concert entries using HtmlAgilityPack
3. **Compare** - Identify new concerts since last check
4. **Match** - Score concerts against user preferences
5. **Notify** - Send email digest of relevant concerts
6. **Store** - Update concert history in storage

## Development Setup

### Prerequisites

- .NET 8.0 SDK
- Azure Functions Core Tools (for local testing)
- Azure Storage Emulator or Azurite (for local storage testing)

### Local Development

1. Clone the repository
2. Copy `local.settings.json.example` to `local.settings.json` and configure
3. Run Azure Functions locally:
   ```bash
   cd src/MetalWatch.Function
   func start
   ```

### Testing

Run unit tests:
```bash
dotnet test
```

## Deployment

### Azure Functions (Recommended)

1. **Create Azure Resources**:
   - Resource Group in West Europe
   - Storage Account for function app and concert data
   - Function App (Consumption Plan, .NET 8)

2. **Configure Application Settings**:
   - Add all configuration from `appsettings.json`
   - Store sensitive values (email password, connection strings) in Azure Key Vault

3. **Deploy**:
   ```bash
   func azure functionapp publish <function-app-name>
   ```

4. **Schedule**: Timer trigger runs daily at 8:00 AM CET (CRON: `0 0 8 * * *`)

### Alternative: Hetzner Cloud (German Provider)

Deploy as a containerized application with systemd timer for scheduling.

### Cost Estimation (Azure West Europe)

- Azure Functions: ~€0-5/month
- Blob Storage: ~€0.10/month
- **Total**: ~€5/month

## Extending with New Notification Channels

To add a new notification channel:

1. Create a new class implementing `INotificationService`:
   ```csharp
   public class SlackNotificationService : INotificationService
   {
       public async Task<NotificationResult> SendNotificationAsync(
           List<Concert> concerts,
           CancellationToken cancellationToken)
       {
           // Implementation
       }
   }
   ```

2. Register in dependency injection container:
   ```csharp
   services.AddScoped<INotificationService, SlackNotificationService>();
   ```

3. Update configuration to select the new provider

## Roadmap

- [ ] Implement core scraping logic
- [ ] Implement concert matching service
- [ ] Implement email notification service
- [ ] Create Azure Function with timer trigger
- [ ] Add unit tests
- [ ] Deploy to Azure
- [ ] Add web dashboard for managing preferences
- [ ] Add support for multiple regions beyond Copenhagen
- [ ] Add support for Slack notifications
- [ ] Add support for Discord notifications
- [ ] Implement ML-based concert recommendations

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
