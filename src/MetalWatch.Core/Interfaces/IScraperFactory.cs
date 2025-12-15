namespace MetalWatch.Core.Interfaces;

/// <summary>
/// Factory for creating and selecting the appropriate concert scraper
/// Implements the Strategy pattern for extensibility
/// </summary>
public interface IScraperFactory
{
    /// <summary>
    /// Gets the appropriate scraper for the given URL
    /// Auto-selects based on SupportsUrl() method
    /// </summary>
    /// <param name="url">URL to scrape</param>
    /// <returns>Scraper that supports this URL</returns>
    /// <exception cref="NotSupportedException">If no scraper supports the URL</exception>
    IConcertScraper GetScraper(string url);

    /// <summary>
    /// Gets a scraper by its name
    /// </summary>
    /// <param name="name">Scraper name (e.g., "HeavyMetalDk")</param>
    /// <returns>Scraper with the specified name</returns>
    /// <exception cref="ArgumentException">If no scraper with that name exists</exception>
    IConcertScraper GetScraperByName(string name);

    /// <summary>
    /// Gets all registered scrapers
    /// </summary>
    /// <returns>Collection of all available scrapers</returns>
    IEnumerable<IConcertScraper> GetAllScrapers();
}
