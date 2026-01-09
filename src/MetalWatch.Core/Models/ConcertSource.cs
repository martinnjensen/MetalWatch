namespace MetalWatch.Core.Models;

/// <summary>
/// Represents a concert source to be scraped
/// Contains configuration for scraping schedule and scraper selection
/// </summary>
public class ConcertSource
{
    /// <summary>
    /// Unique identifier for the source
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Human-readable name for the source (e.g., "HeavyMetal.dk")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The scraper type to use for this source (e.g., "HeavyMetalDk")
    /// Used with IScraperFactory.GetScraperByName()
    /// </summary>
    public required string ScraperType { get; set; }

    /// <summary>
    /// The URL to scrape for concerts
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// How often this source should be scraped (defaults to 24 hours)
    /// </summary>
    public TimeSpan ScrapeInterval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// When this source was last scraped (null if never attempted)
    /// Updated on both success and failure to prevent excessive retries
    /// </summary>
    public DateTime? LastScrapedAt { get; set; }

    /// <summary>
    /// Whether the last scrape attempt was successful (null if never attempted)
    /// </summary>
    public bool? LastScrapeSuccess { get; set; }

    /// <summary>
    /// Error message from the last failed scrape attempt (null if never failed or last attempt succeeded)
    /// </summary>
    public string? LastScrapeError { get; set; }

    /// <summary>
    /// Whether this source is enabled for scraping
    /// </summary>
    public bool Enabled { get; set; } = true;
}
