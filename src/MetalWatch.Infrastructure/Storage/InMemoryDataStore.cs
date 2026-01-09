namespace MetalWatch.Infrastructure.Storage;

using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Models;

/// <summary>
/// In-memory implementation of IDataStore for local development and testing.
/// Initializes with an empty concert list and heavymetal.dk as the default source.
/// </summary>
public class InMemoryDataStore : IDataStore
{
    private readonly List<Concert> _concerts = new();
    private ConcertPreferences _preferences = new();
    private readonly List<ConcertSource> _sources;

    /// <summary>
    /// Initializes a new instance of InMemoryDataStore with default heavymetal.dk source
    /// </summary>
    public InMemoryDataStore()
    {
        _sources = new List<ConcertSource>
        {
            new ConcertSource
            {
                Id = "heavymetal-dk",
                Name = "HeavyMetal.dk",
                ScraperType = "HeavyMetalDk",
                Url = "https://heavymetal.dk/koncertkalender/",
                Enabled = true,
                ScrapeInterval = TimeSpan.FromHours(24)
            }
        };
    }

    /// <inheritdoc />
    public Task<List<Concert>> GetPreviousConcertsAsync()
    {
        return Task.FromResult(new List<Concert>(_concerts));
    }

    /// <inheritdoc />
    public Task SaveConcertsAsync(List<Concert> concerts)
    {
        _concerts.Clear();
        _concerts.AddRange(concerts);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ConcertPreferences> GetPreferencesAsync()
    {
        return Task.FromResult(_preferences);
    }

    /// <inheritdoc />
    public Task SavePreferencesAsync(ConcertPreferences preferences)
    {
        _preferences = preferences;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<List<ConcertSource>> GetSourcesDueForScrapingAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTime.UtcNow;
        var dueForScraping = _sources
            .Where(s => s.Enabled &&
                       (s.LastScrapedAt == null || s.LastScrapedAt.Value.Add(s.ScrapeInterval) < now))
            .ToList();

        return Task.FromResult(dueForScraping);
    }

    /// <inheritdoc />
    public Task UpdateSourceScrapedAsync(
        string sourceId,
        DateTime scrapedAt,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var source = _sources.FirstOrDefault(s => s.Id == sourceId);
        if (source != null)
        {
            source.LastScrapedAt = scrapedAt;
            source.LastScrapeSuccess = success;
            source.LastScrapeError = errorMessage;
        }

        return Task.CompletedTask;
    }
}
