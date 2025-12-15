namespace MetalWatch.Core.Interfaces;

using MetalWatch.Core.Models;

/// <summary>
/// Interface for concert scraping implementations
/// Enables extensibility - different scrapers for different concert websites
/// </summary>
public interface IConcertScraper
{
    /// <summary>
    /// Scrapes concert data from the specified URL
    /// </summary>
    /// <param name="url">URL to scrape</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing scraped concerts or error information</returns>
    Task<ScraperResult> ScrapeAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name of this scraper (e.g., "HeavyMetalDk")
    /// Used for logging and debugging
    /// </summary>
    string ScraperName { get; }

    /// <summary>
    /// Checks if this scraper supports the given URL
    /// Enables automatic scraper selection based on URL pattern
    /// </summary>
    /// <param name="url">URL to check</param>
    /// <returns>True if this scraper can handle the URL</returns>
    bool SupportsUrl(string url);
}
