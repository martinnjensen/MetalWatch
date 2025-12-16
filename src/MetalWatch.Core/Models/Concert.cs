namespace MetalWatch.Core.Models;

/// <summary>
/// Represents a concert event
/// </summary>
public class Concert
{
    /// <summary>
    /// Unique identifier derived from concert URL slug
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Date of the concert
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Day of week in Danish (e.g., "man", "tir", "ons")
    /// </summary>
    public required string DayOfWeek { get; set; }

    /// <summary>
    /// List of artists performing (supports multi-artist shows and festivals)
    /// </summary>
    public List<string> Artists { get; set; } = new();

    /// <summary>
    /// Venue name where the concert takes place
    /// </summary>
    public required string Venue { get; set; }

    /// <summary>
    /// Full URL to the concert details page
    /// </summary>
    public required string ConcertUrl { get; set; }

    /// <summary>
    /// Indicates if the concert has been cancelled
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Indicates if this is a newly added concert
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// Indicates if this is a festival or multi-artist event
    /// </summary>
    public bool IsFestival { get; set; }

    /// <summary>
    /// Timestamp when this concert data was scraped
    /// </summary>
    public DateTime ScrapedAt { get; set; }
}
