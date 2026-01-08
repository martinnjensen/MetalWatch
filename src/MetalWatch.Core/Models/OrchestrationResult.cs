namespace MetalWatch.Core.Models;

/// <summary>
/// Result object returned from orchestration workflow execution
/// Contains details about scraping, new concert detection, and events published
/// </summary>
public class OrchestrationResult
{
    /// <summary>
    /// Indicates if the orchestration workflow completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The source URL that was processed
    /// </summary>
    public required string SourceUrl { get; set; }

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
