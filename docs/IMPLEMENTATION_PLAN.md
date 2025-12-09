# MetalWatch Implementation Plan

## Phase 1: Core Infrastructure (Days 1-2)

### 1.1 Project Setup
- [ ] Create solution file `MetalWatch.sln`
- [ ] Create `MetalWatch.Core` class library project (.NET 8.0)
- [ ] Create `MetalWatch.Infrastructure` class library project (.NET 8.0)
- [ ] Create `MetalWatch.Function` Azure Functions project (.NET 8.0)
- [ ] Create `MetalWatch.Tests` test project (xUnit)
- [ ] Add project references
- [ ] Add NuGet packages to all projects

**NuGet Packages**:

Core:
```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
```

Infrastructure:
```xml
<PackageReference Include="HtmlAgilityPack" Version="1.11.59" />
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
<PackageReference Include="MailKit" Version="4.3.0" />
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
```

Function:
```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.21.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="1.17.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Timer" Version="4.3.0" />
<PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="2.21.0" />
```

Tests:
```xml
<PackageReference Include="xUnit" Version="2.6.2" />
<PackageReference Include="xUnit.runner.visualstudio" Version="2.5.4" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
```

### 1.2 Core Models
Create in `MetalWatch.Core/Models/`:

- [ ] `Concert.cs` - Main concert entity with all properties
- [ ] `ConcertPreferences.cs` - User preference configuration
- [ ] `NotificationResult.cs` - Result of notification operations

### 1.3 Core Interfaces
Create in `MetalWatch.Core/Interfaces/`:

- [ ] `IConcertScraper.cs` - Web scraping abstraction
- [ ] `IConcertMatcher.cs` - Concert matching logic
- [ ] `INotificationService.cs` - Pluggable notifications
- [ ] `IDataStore.cs` - Storage abstraction
- [ ] `IConcertTrackerService.cs` - Main orchestration service

## Phase 2: Web Scraping Implementation (Day 2)

### 2.1 Scraper Service
Create `MetalWatch.Infrastructure/Scrapers/HeavyMetalDkScraper.cs`:

- [ ] Implement `IConcertScraper` interface
- [ ] Add HTTP client with proper User-Agent
- [ ] Parse HTML using HtmlAgilityPack
- [ ] Implement state machine for grouping concert elements:
  - Detect date patterns: `dd/mm (day)`
  - Collect artist links: `/artist/*`
  - Extract venue links: `/spillested/*`
  - Capture concert URLs: `/koncert/*`
- [ ] Handle special markers:
  - "Aflyst" → `IsCancelled = true`
  - "Ny" → `IsNew = true`
- [ ] Parse Danish date format to DateTime
- [ ] Handle encoding (UTF-8 for Danish characters: æ, ø, å)
- [ ] Generate concert IDs from URL slugs
- [ ] Add error handling and logging

**Key Implementation Details**:
```csharp
// Date parsing: "30/12 (man)" → DateTime
var dateMatch = Regex.Match(text, @"(\d{1,2})/(\d{1,2})\s*\((\w+)\)");
if (dateMatch.Success)
{
    int day = int.Parse(dateMatch.Groups[1].Value);
    int month = int.Parse(dateMatch.Groups[2].Value);
    // Determine year (handle year rollover)
    // Parse day of week for validation
}
```

### 2.2 Scraper Tests
Create `MetalWatch.Tests/Scrapers/HeavyMetalDkScraperTests.cs`:

- [ ] Test parsing single concert entry
- [ ] Test parsing multiple concerts
- [ ] Test cancelled concert detection
- [ ] Test new concert detection
- [ ] Test festival detection (multiple artists)
- [ ] Test Danish date parsing
- [ ] Test malformed HTML handling
- [ ] Test empty page handling

## Phase 3: Matching Logic (Day 3)

### 3.1 Matcher Service
Create `MetalWatch.Core/Services/ConcertMatcherService.cs`:

- [ ] Implement `IConcertMatcher` interface
- [ ] Implement scoring algorithm:
  - Exact artist match: +100 points
  - Favorite venue: +50 points
  - Keyword in artist name: +25 points per keyword
- [ ] Filter concerts by date range
- [ ] Filter out cancelled concerts
- [ ] Return sorted list by relevance score
- [ ] Handle case-insensitive matching
- [ ] Handle partial artist name matches

**Scoring Logic**:
```csharp
public int CalculateRelevanceScore(Concert concert, ConcertPreferences prefs)
{
    int score = 0;

    // Favorite artist (case-insensitive)
    if (concert.Artists.Any(a =>
        prefs.FavoriteArtists.Contains(a, StringComparer.OrdinalIgnoreCase)))
        score += 100;

    // Favorite venue
    if (prefs.FavoriteVenues.Contains(concert.Venue,
        StringComparer.OrdinalIgnoreCase))
        score += 50;

    // Keywords in artist names
    foreach (var keyword in prefs.Keywords)
    {
        if (concert.Artists.Any(a =>
            a.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            score += 25;
    }

    return score;
}
```

### 3.2 Matcher Tests
Create `MetalWatch.Tests/Services/ConcertMatcherServiceTests.cs`:

- [ ] Test exact artist match scoring
- [ ] Test venue match scoring
- [ ] Test keyword match scoring
- [ ] Test combined scoring
- [ ] Test date range filtering
- [ ] Test cancelled concert filtering
- [ ] Test case-insensitive matching
- [ ] Test sorting by relevance

## Phase 4: Storage Implementation (Day 3)

### 4.1 JSON Storage (Local Development)
Create `MetalWatch.Infrastructure/Storage/JsonDataStore.cs`:

- [ ] Implement `IDataStore` interface
- [ ] Store concerts in `concert-data/concerts.json`
- [ ] Store preferences in `concert-data/preferences.json`
- [ ] Ensure directory creation
- [ ] Handle file not found (first run)
- [ ] Use `System.Text.Json` for serialization
- [ ] Add proper error handling

### 4.2 Blob Storage (Production)
Create `MetalWatch.Infrastructure/Storage/BlobStorageDataStore.cs`:

- [ ] Implement `IDataStore` interface
- [ ] Use Azure.Storage.Blobs SDK
- [ ] Store concerts as `concerts.json` blob
- [ ] Store preferences as `preferences.json` blob
- [ ] Handle blob not found (first run)
- [ ] Add retry logic
- [ ] Add proper error handling

### 4.3 Storage Tests
Create `MetalWatch.Tests/Storage/DataStoreTests.cs`:

- [ ] Test JSON storage round-trip
- [ ] Test blob storage round-trip (using Azurite)
- [ ] Test handling missing files/blobs
- [ ] Test concurrent access handling

## Phase 5: Notification Implementation (Day 4)

### 5.1 Email Service
Create `MetalWatch.Infrastructure/Notifications/EmailNotificationService.cs`:

- [ ] Implement `INotificationService` interface
- [ ] Use MailKit for SMTP
- [ ] Build HTML email template
- [ ] Include concert details:
  - Date and day of week
  - Artist names (highlight favorites)
  - Venue (highlight favorites)
  - Link to concert page
- [ ] Add email header with count and date
- [ ] Add footer with preference instructions
- [ ] Handle SMTP errors gracefully
- [ ] Add configuration for SMTP settings

**Email Template Example**:
```html
<html>
<body>
  <h1>🎸 5 New Metal Concerts Found</h1>
  <p>Date: Tuesday, 9 December 2025</p>

  <div style="border: 1px solid #ccc; padding: 10px; margin: 10px 0;">
    <h3>15/12 (søn)</h3>
    <p><strong style="color: #d32f2f;">Metallica</strong> ⭐ (Favorite)</p>
    <p>Venue: <strong>Pumpehuset</strong> ⭐ (Favorite)</p>
    <p><a href="https://heavymetal.dk/koncert/metallica-pumpehuset">View Details</a></p>
  </div>

  <!-- More concerts... -->

  <hr>
  <p><small>To update preferences, modify your configuration.</small></p>
</body>
</html>
```

### 5.2 Notification Tests
Create `MetalWatch.Tests/Notifications/EmailNotificationServiceTests.cs`:

- [ ] Test email composition
- [ ] Test HTML generation
- [ ] Test favorite highlighting
- [ ] Mock SMTP for unit tests
- [ ] Test error handling

## Phase 6: Orchestration Service (Day 4)

### 6.1 Tracker Service
Create `MetalWatch.Core/Services/ConcertTrackerService.cs`:

- [ ] Implement `IConcertTrackerService` interface
- [ ] Orchestrate the full workflow:
  1. Load previous concerts from storage
  2. Load user preferences
  3. Scrape current concerts
  4. Identify new concerts (not in previous list)
  5. Match new concerts against preferences
  6. Send notifications if matches found
  7. Save updated concert list
- [ ] Add comprehensive logging
- [ ] Handle errors gracefully
- [ ] Return execution summary

**Workflow Logic**:
```csharp
public async Task<TrackerResult> CheckAndNotifyAsync(CancellationToken cancellationToken)
{
    _logger.LogInformation("Starting concert check");

    // Load data
    var previousConcerts = await _dataStore.GetPreviousConcertsAsync();
    var preferences = await _dataStore.GetPreferencesAsync();

    // Scrape
    var currentConcerts = await _scraper.ScrapeAsync(_sourceUrl, cancellationToken);

    // Find new
    var newConcerts = currentConcerts
        .Where(c => !previousConcerts.Any(p => p.Id == c.Id))
        .ToList();

    _logger.LogInformation("Found {Count} new concerts", newConcerts.Count);

    // Match
    var matches = _matcher.FindMatches(newConcerts, preferences);

    // Notify
    if (matches.Any())
    {
        await _notificationService.SendNotificationAsync(matches, cancellationToken);
    }

    // Save
    await _dataStore.SaveConcertsAsync(currentConcerts);

    return new TrackerResult { NewConcerts = newConcerts.Count, Matches = matches.Count };
}
```

### 6.2 Tracker Tests
Create `MetalWatch.Tests/Services/ConcertTrackerServiceTests.cs`:

- [ ] Test full workflow with mocks
- [ ] Test new concert detection
- [ ] Test no matches scenario
- [ ] Test notification triggered
- [ ] Test storage updated
- [ ] Test error handling in each step

## Phase 7: Azure Function (Day 5)

### 7.1 Function App Setup
Create `MetalWatch.Function/DailyConcertCheck.cs`:

- [ ] Create timer trigger function
- [ ] Set CRON schedule: `0 0 8 * * *` (8 AM daily)
- [ ] Configure dependency injection
- [ ] Add logging
- [ ] Invoke tracker service
- [ ] Handle exceptions

**Function Implementation**:
```csharp
public class DailyConcertCheck
{
    private readonly IConcertTrackerService _trackerService;
    private readonly ILogger<DailyConcertCheck> _logger;

    public DailyConcertCheck(IConcertTrackerService trackerService,
        ILogger<DailyConcertCheck> logger)
    {
        _trackerService = trackerService;
        _logger = logger;
    }

    [Function("DailyConcertCheck")]
    public async Task Run([TimerTrigger("0 0 8 * * *")] TimerInfo timer,
        FunctionContext context)
    {
        _logger.LogInformation("Starting daily concert check at {Time}", DateTime.UtcNow);

        try
        {
            var result = await _trackerService.CheckAndNotifyAsync();
            _logger.LogInformation("Check complete: {NewConcerts} new, {Matches} matches",
                result.NewConcerts, result.Matches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during concert check");
            throw;
        }
    }
}
```

### 7.2 Dependency Injection Configuration
Create `MetalWatch.Function/Program.cs`:

- [ ] Configure host builder
- [ ] Register all services
- [ ] Configure logging
- [ ] Load configuration

```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddScoped<IConcertScraper, HeavyMetalDkScraper>();
        services.AddScoped<IConcertMatcher, ConcertMatcherService>();
        services.AddScoped<INotificationService, EmailNotificationService>();
        services.AddScoped<IDataStore, BlobStorageDataStore>(); // or JsonDataStore
        services.AddScoped<IConcertTrackerService, ConcertTrackerService>();
    })
    .Build();

await host.RunAsync();
```

### 7.3 Configuration Files
- [ ] Create `host.json` with function settings
- [ ] Create `local.settings.json.example` template
- [ ] Document all required configuration values

### 7.4 Local Testing
- [ ] Test function locally with Azure Functions Core Tools
- [ ] Test with JSON storage
- [ ] Test with Azurite (blob storage emulator)
- [ ] Verify timer trigger works
- [ ] Test end-to-end flow

## Phase 8: Azure Deployment (Days 5-6)

### 8.1 Azure Resource Creation

Using Azure Portal or CLI:

- [ ] Create Resource Group (West Europe)
- [ ] Create Storage Account (general purpose v2)
- [ ] Create Blob Container: `concert-data`
- [ ] Create Function App:
  - Runtime: .NET 8 Isolated
  - OS: Windows or Linux
  - Plan: Consumption
  - Region: West Europe
- [ ] Enable Application Insights
- [ ] (Optional) Create Key Vault for secrets

**Azure CLI Commands**:
```bash
# Create resource group
az group create --name rg-metalwatch --location westeurope

# Create storage account
az storage account create \
  --name stmetalwatch \
  --resource-group rg-metalwatch \
  --location westeurope \
  --sku Standard_LRS

# Create function app
az functionapp create \
  --name func-metalwatch \
  --resource-group rg-metalwatch \
  --storage-account stmetalwatch \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --consumption-plan-location westeurope
```

### 8.2 Application Settings Configuration

- [ ] Add all configuration settings to Function App
- [ ] Add email SMTP settings
- [ ] Add storage connection string
- [ ] Add concert preferences
- [ ] Test configuration

**Required Settings**:
```json
{
  "MetalWatch:SourceUrl": "https://heavymetal.dk/koncertkalender?landsdel=koebenhavn",
  "MetalWatch:Preferences:FavoriteArtists": "[\"Artist1\",\"Artist2\"]",
  "MetalWatch:Preferences:FavoriteVenues": "[\"Venue1\",\"Venue2\"]",
  "MetalWatch:Preferences:Keywords": "[\"thrash\",\"death metal\"]",
  "MetalWatch:Notification:Email:To": "your@email.com",
  "MetalWatch:Notification:Email:From": "concerts@yourdomain.com",
  "MetalWatch:Notification:Email:SmtpHost": "smtp.gmail.com",
  "MetalWatch:Notification:Email:SmtpPort": "587",
  "MetalWatch:Notification:Email:Username": "your@gmail.com",
  "MetalWatch:Notification:Email:Password": "@Microsoft.KeyVault(...)",
  "MetalWatch:Storage:Type": "BlobStorage",
  "MetalWatch:Storage:BlobConnectionString": "...",
  "MetalWatch:Storage:ContainerName": "concert-data"
}
```

### 8.3 Deployment

- [ ] Build solution in Release mode
- [ ] Publish function app using Azure Functions Core Tools:
  ```bash
  func azure functionapp publish func-metalwatch
  ```
- [ ] Verify deployment in Azure Portal
- [ ] Check function logs in Application Insights

### 8.4 Verification

- [ ] Manually trigger function to test
- [ ] Verify email received
- [ ] Check blob storage for concert data
- [ ] Monitor Application Insights for errors
- [ ] Verify timer trigger schedule

## Phase 9: Documentation & Polish (Day 6)

### 9.1 Documentation
- [ ] Update README with setup instructions
- [ ] Document configuration options
- [ ] Add deployment guide
- [ ] Add troubleshooting section
- [ ] Document how to extend with new notification types

### 9.2 Code Quality
- [ ] Add XML documentation comments
- [ ] Run code analysis
- [ ] Fix any warnings
- [ ] Ensure consistent coding style
- [ ] Add proper exception handling everywhere

### 9.3 Testing
- [ ] Achieve >80% code coverage
- [ ] Add integration tests
- [ ] Test edge cases
- [ ] Load test scraping logic

## Future Enhancements (Post-MVP)

### Short Term
- [ ] Add Slack notification support
- [ ] Add Discord notification support
- [ ] Support multiple regions (not just Copenhagen)
- [ ] Add web dashboard for managing preferences
- [ ] Add database storage option (PostgreSQL/SQL Server)

### Medium Term
- [ ] Implement ML-based recommendations
- [ ] Add artist genre classification
- [ ] Support multiple users with different preferences
- [ ] Add ticket price tracking
- [ ] Add concert reminder notifications

### Long Term
- [ ] Support multiple concert sources (other websites)
- [ ] Mobile app for preference management
- [ ] Social features (share concerts with friends)
- [ ] Integration with Spotify/Apple Music
- [ ] Calendar export (iCal format)

## Risk Management

### Technical Risks

**Risk**: Website structure changes break scraper
- **Mitigation**: Add comprehensive tests, monitor scraping errors, implement alerts
- **Recovery**: Update scraper logic, deploy quickly

**Risk**: Email service unreliable
- **Mitigation**: Add retry logic, fallback notification channel
- **Recovery**: Queue failed notifications for retry

**Risk**: Azure costs exceed budget
- **Mitigation**: Use consumption plan, monitor costs, set budget alerts
- **Recovery**: Optimize execution, reduce frequency if needed

### Operational Risks

**Risk**: Missed concert notifications
- **Mitigation**: Application Insights monitoring, alert on function failures
- **Recovery**: Manual notification, fix issue

**Risk**: Spam complaints from email notifications
- **Mitigation**: Clear unsubscribe instructions, respect user preferences
- **Recovery**: Implement proper email unsubscribe flow

## Success Metrics

### Phase 1 (MVP)
- [ ] Successfully scrapes concerts daily
- [ ] Accurately identifies new concerts
- [ ] Sends email notifications reliably
- [ ] Runs at <€5/month cost
- [ ] 99% uptime for function execution

### Phase 2 (Growth)
- [ ] Support 10+ users
- [ ] Add 2+ notification channels
- [ ] Support 3+ regions
- [ ] Maintain <€10/month cost

## Timeline Summary

- **Day 1**: Project setup, core models, interfaces
- **Day 2**: Web scraping implementation
- **Day 3**: Matching logic, storage implementation
- **Day 4**: Notifications, orchestration service
- **Day 5**: Azure Function, local testing
- **Day 6**: Azure deployment, documentation

**Total**: 6 days for MVP deployment
