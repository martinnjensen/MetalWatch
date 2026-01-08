namespace MetalWatch.Core.Interfaces;

using MetalWatch.Core.Models;

/// <summary>
/// Interface for data storage operations
/// Abstracts storage implementation (JSON files, S3, database, etc.)
/// </summary>
public interface IDataStore
{
    /// <summary>
    /// Retrieves the list of previously scraped concerts
    /// Used to identify new concerts since last check
    /// </summary>
    /// <returns>List of previously stored concerts</returns>
    Task<List<Concert>> GetPreviousConcertsAsync();

    /// <summary>
    /// Saves the current list of concerts
    /// Overwrites previous concert data
    /// </summary>
    /// <param name="concerts">Concerts to save</param>
    Task SaveConcertsAsync(List<Concert> concerts);

    /// <summary>
    /// Retrieves user preferences
    /// </summary>
    /// <returns>Stored preferences or default if none exist</returns>
    Task<ConcertPreferences> GetPreferencesAsync();

    /// <summary>
    /// Saves user preferences
    /// </summary>
    /// <param name="preferences">Preferences to save</param>
    Task SavePreferencesAsync(ConcertPreferences preferences);

    /// <summary>
    /// Retrieves concert sources that are due for scraping
    /// A source is due if: Enabled AND (LastScrapedAt is null OR LastScrapedAt + ScrapeInterval &lt; Now)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of sources due for scraping</returns>
    Task<List<ConcertSource>> GetSourcesDueForScrapingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the scrape status for a source after a scrape attempt.
    /// Always called after scraping, regardless of success or failure, to prevent excessive retries.
    /// </summary>
    /// <param name="sourceId">The source ID to update</param>
    /// <param name="scrapedAt">The timestamp when scraping was attempted</param>
    /// <param name="success">Whether the scrape was successful</param>
    /// <param name="errorMessage">Error message if scrape failed (null if successful)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateSourceScrapedAsync(
        string sourceId,
        DateTime scrapedAt,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}
