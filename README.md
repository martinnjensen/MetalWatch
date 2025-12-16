# MetalWatch

A cloud-based service that tracks heavy metal concerts in Copenhagen from [heavymetal.dk](https://heavymetal.dk/koncertkalender?landsdel=koebenhavn) and sends personalized notifications about concerts matching your preferences.

## Features

- **Daily Automated Scraping**: Checks the concert calendar daily for new shows
- **Smart Matching**: Filters concerts based on your favorite artists, venues, and keywords
- **Pluggable Notifications**: Email notifications with easy extensibility for other channels (Slack, Discord, etc.)
- **Cloud-Ready**: Designed for cloud deployment in European regions
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
│   │   │   └── S3CompatibleDataStore.cs # S3-compatible storage (production)
│   │   └── Notifications/
│   │       └── EmailNotificationService.cs # Email implementation
│   │
│   ├── MetalWatch.Worker/            # Background worker service
│   │   ├── DailyConcertCheckWorker.cs # Scheduled worker
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
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
- S3-compatible object storage for production (works with EU sovereign cloud providers)
- Easy to add database support if needed

**3. Clean Architecture**
- Core layer contains business logic, independent of frameworks
- Infrastructure layer contains external dependencies
- Worker layer is thin hosting layer

## Technology Stack

- **Language**: C# (.NET 10.0)
- **Web Scraping**: HtmlAgilityPack
- **Email**: MailKit
- **Deployment**: Containerized worker service with systemd timers or Kubernetes CronJob
- **Storage**: S3-compatible object storage (e.g., MinIO, OpenStack Swift)
- **Deployment Region**: EU sovereign cloud providers (e.g., OVHcloud, Hetzner, IONOS, or local EU infrastructure)

## Configuration

The service is configured via `appsettings.json` or environment variables:

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

- .NET 10.0 SDK
- Docker (optional, for MinIO local testing)

### Local Development

1. Clone the repository
2. Copy `appsettings.Development.json.example` to `appsettings.Development.json` and configure
3. (Optional) Start local MinIO for S3-compatible storage testing:
   ```bash
   docker run -p 9000:9000 -p 9001:9001 minio/minio server /data --console-address ":9001"
   ```
4. Run the worker service locally:
   ```bash
   cd src/MetalWatch.Worker
   dotnet run
   ```

### Testing

Run unit tests:
```bash
dotnet test
```

## Deployment

### Option 1: Docker Container with systemd Timer (Recommended for EU Sovereignty)

1. **Build Docker Image**:
   ```bash
   docker build -t metalwatch:latest .
   ```

2. **Deploy to EU Sovereign Cloud** (e.g., Hetzner, OVHcloud, IONOS):
   - Create a VM in an EU region
   - Install Docker
   - Set up environment variables or mount `appsettings.json`
   - Run container with systemd timer or cron for daily execution

3. **Schedule**: Use systemd timer or cron to run daily at 8:00 AM CET

### Option 2: Kubernetes CronJob

1. **Create Kubernetes Deployment**:
   ```yaml
   apiVersion: batch/v1
   kind: CronJob
   metadata:
     name: metalwatch-daily
   spec:
     schedule: "0 8 * * *"  # Daily at 8:00 AM
     jobTemplate:
       spec:
         template:
           spec:
             containers:
             - name: metalwatch
               image: metalwatch:latest
               envFrom:
               - secretRef:
                   name: metalwatch-config
   ```

2. **Deploy to EU Kubernetes cluster** (e.g., on OVHcloud Managed Kubernetes, Hetzner Cloud, or self-hosted)

### Option 3: Standalone Service

Deploy as a long-running .NET Worker Service with internal scheduling (uses `BackgroundService` with timer).

### Cost Estimation (EU Sovereign Cloud)

**Hetzner Cloud (German Provider)**:
- CX11 VM (2 vCPU, 2GB RAM): ~€4/month
- Object Storage (250 GB included): ~€5/month
- **Total**: ~€9/month

**OVHcloud (French Provider)**:
- B2-7 VM (2 vCPU, 7GB RAM): ~€7/month
- Object Storage (Pay-as-you-go): ~€0.01/GB
- **Total**: ~€7-10/month

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
- [ ] Create Worker Service with scheduled background job
- [ ] Add unit tests
- [ ] Deploy to EU sovereign cloud
- [ ] Add web dashboard for managing preferences
- [ ] Add support for multiple regions beyond Copenhagen
- [ ] Add support for Slack notifications
- [ ] Add support for Discord notifications
- [ ] Implement ML-based concert recommendations

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
