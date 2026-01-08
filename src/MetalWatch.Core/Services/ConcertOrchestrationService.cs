namespace MetalWatch.Core.Services;

using System.Security.Cryptography;
using System.Text;
using MetalWatch.Core.Events;
using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates the concert discovery workflow.
/// Retrieves due sources, coordinates scraping, new concert detection, storage, and event publishing.
/// </summary>
public class ConcertOrchestrationService : IConcertOrchestrationService
{
    private readonly IScraperFactory _scraperFactory;
    private readonly IDataStore _dataStore;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ConcertOrchestrationService> _logger;

    /// <summary>
    /// Initializes a new instance of ConcertOrchestrationService
    /// </summary>
    public ConcertOrchestrationService(
        IScraperFactory scraperFactory,
        IDataStore dataStore,
        IEventBus eventBus,
        ILogger<ConcertOrchestrationService> logger)
    {
        _scraperFactory = scraperFactory;
        _dataStore = dataStore;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<OrchestrationResult>> ExecuteDueWorkflowsAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new List<OrchestrationResult>();

        // Get sources due for scraping
        var sources = await _dataStore.GetSourcesDueForScrapingAsync(cancellationToken);

        if (sources.Count == 0)
        {
            _logger.LogInformation("No sources due for scraping");
            return results;
        }

        // Process each source
        foreach (var source in sources)
        {
            var result = await ProcessSourceAsync(source, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private async Task<OrchestrationResult> ProcessSourceAsync(
        ConcertSource source,
        CancellationToken cancellationToken)
    {
        var executedAt = DateTime.UtcNow;

        _logger.LogInformation("Processing source {SourceName} ({SourceId})", source.Name, source.Id);

        // Get the appropriate scraper
        var scraper = _scraperFactory.GetScraperByName(source.ScraperType);

        // Scrape concerts from the source
        var scraperResult = await scraper.ScrapeAsync(source.Url, cancellationToken);

        if (!scraperResult.Success)
        {
            _logger.LogWarning(
                "Scraping failed for source {SourceId}: {ErrorMessage}",
                source.Id,
                scraperResult.ErrorMessage);

            // Update source with failure status
            await _dataStore.UpdateSourceScrapedAsync(
                source.Id,
                executedAt,
                false,
                scraperResult.ErrorMessage,
                cancellationToken);

            return new OrchestrationResult
            {
                Success = false,
                SourceId = source.Id,
                SourceName = source.Name,
                ConcertsScraped = 0,
                NewConcertsCount = 0,
                ErrorMessage = scraperResult.ErrorMessage,
                ExecutedAt = executedAt
            };
        }

        // Generate unique IDs for scraped concerts
        var scrapedConcerts = scraperResult.Concerts ?? new List<Concert>();
        foreach (var concert in scrapedConcerts)
        {
            concert.Id = GenerateConcertId(concert);
        }

        // Get previous concerts to identify new ones
        var previousConcerts = await _dataStore.GetPreviousConcertsAsync();
        var previousIds = previousConcerts.Select(c => c.Id).ToHashSet();

        // Identify new concerts
        var newConcerts = scrapedConcerts
            .Where(c => !previousIds.Contains(c.Id))
            .ToList();

        // Save all scraped concerts
        await _dataStore.SaveConcertsAsync(scrapedConcerts);

        // Track published events
        var eventsPublished = new List<string>();

        // Publish event if new concerts found
        if (newConcerts.Count > 0)
        {
            var newConcertsEvent = new NewConcertsFoundEvent
            {
                NewConcerts = newConcerts,
                SourceUrl = source.Url,
                FoundAt = executedAt
            };

            await _eventBus.PublishAsync(newConcertsEvent, cancellationToken);
            eventsPublished.Add(nameof(NewConcertsFoundEvent));

            _logger.LogInformation(
                "Published NewConcertsFoundEvent with {Count} new concerts from {SourceId}",
                newConcerts.Count,
                source.Id);
        }

        // Update source with success status
        await _dataStore.UpdateSourceScrapedAsync(
            source.Id,
            executedAt,
            true,
            null,
            cancellationToken);

        return new OrchestrationResult
        {
            Success = true,
            SourceId = source.Id,
            SourceName = source.Name,
            ConcertsScraped = scrapedConcerts.Count,
            NewConcertsCount = newConcerts.Count,
            EventsPublished = eventsPublished,
            ExecutedAt = executedAt
        };
    }

    /// <summary>
    /// Generates a deterministic unique ID for a concert based on its content.
    /// Same concert details will always produce the same ID.
    /// </summary>
    private static string GenerateConcertId(Concert concert)
    {
        // Build a deterministic string from concert properties
        var artists = concert.Artists != null && concert.Artists.Count > 0
            ? string.Join("|", concert.Artists.OrderBy(a => a))
            : string.Empty;

        var concertKey = $"{concert.Venue}|{concert.Date:yyyy-MM-dd}|{artists}";

        // Generate SHA256 hash
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(concertKey));

        // Return first 16 characters of hex string for a compact but unique ID
        return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
    }
}
