namespace MetalWatch.Core.Events;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent significant occurrences in the system
/// that other parts of the application may need to react to.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// The timestamp when the event occurred
    /// </summary>
    DateTime OccurredAt { get; }
}
