namespace MetalWatch.Core.Models;

/// <summary>
/// Result object returned from orchestration workflow execution for a single source
/// Contains details about scraping, new concert detection, and events published
/// </summary>
public class OrchestrationResult
{
    /// <summary>
    /// Indicates if the orchestration workflow completed successfully for this source
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The ID of the source that was processed
    /// </summary>
    public required string SourceId { get; set; }

    /// <summary>
    /// The name of the source that was processed
    /// </summary>
    public required string SourceName { get; set; }

    /// <summary>
    /// Total number of concerts scraped from the source
    /// </summary>
    public int ConcertsScraped { get; set; }

    /// <summary>
    /// Number of new concerts discovered (not previously seen)
    /// </summary>
    public int NewConcertsCount { get; set; }

    /// <summary>
    /// List of domain events that were published during execution
    /// </summary>
    public List<string> EventsPublished { get; set; } = new();

    /// <summary>
    /// Error message if orchestration failed (null if successful)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when the orchestration was executed
    /// </summary>
    public DateTime ExecutedAt { get; set; }
}
