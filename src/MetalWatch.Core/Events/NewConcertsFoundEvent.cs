namespace MetalWatch.Core.Events;

using MetalWatch.Core.Models;

/// <summary>
/// Domain event raised when new concerts are discovered.
/// Contains all new concerts (unfiltered) - filtering against user
/// preferences happens in the notification handler.
/// </summary>
public class NewConcertsFoundEvent : IDomainEvent
{
    /// <summary>
    /// The newly discovered concerts (not previously seen)
    /// </summary>
    public required List<Concert> NewConcerts { get; init; }

    /// <summary>
    /// The URL of the source where the concerts were found
    /// </summary>
    public required string SourceUrl { get; init; }

    /// <summary>
    /// The timestamp when the new concerts were discovered
    /// </summary>
    public required DateTime FoundAt { get; init; }

    /// <inheritdoc />
    public DateTime OccurredAt => FoundAt;
}
