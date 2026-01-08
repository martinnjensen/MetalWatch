namespace MetalWatch.Core.Events;

using MetalWatch.Core.Models;

/// <summary>
/// Domain event raised when concerts have been scraped from a source.
/// Published after the scraping operation completes successfully.
/// </summary>
public class ConcertsScrapedEvent : IDomainEvent
{
    /// <summary>
    /// The URL of the source that was scraped
    /// </summary>
    public required string SourceUrl { get; init; }

    /// <summary>
    /// The concerts that were scraped from the source
    /// </summary>
    public required List<Concert> ScrapedConcerts { get; init; }

    /// <summary>
    /// The timestamp when the scraping occurred
    /// </summary>
    public required DateTime ScrapedAt { get; init; }

    /// <inheritdoc />
    public DateTime OccurredAt => ScrapedAt;
}
