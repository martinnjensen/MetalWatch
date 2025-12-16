namespace MetalWatch.Core.Models;

/// <summary>
/// Result object returned from scraping operations
/// Enables graceful error handling without exceptions
/// </summary>
public class ScraperResult
{
    /// <summary>
    /// Indicates if the scraping operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// List of concerts scraped (empty if unsuccessful)
    /// </summary>
    public List<Concert> Concerts { get; set; } = new();

    /// <summary>
    /// Error message if scraping failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of concerts successfully scraped
    /// </summary>
    public int ConcertsScraped { get; set; }

    /// <summary>
    /// Timestamp when scraping occurred
    /// </summary>
    public DateTime ScrapedAt { get; set; }
}
