namespace MetalWatch.Core.Interfaces;

using MetalWatch.Core.Models;

/// <summary>
/// Interface for the concert orchestration service
/// Coordinates the workflow of scraping, detecting new concerts, and publishing events
/// </summary>
public interface IConcertOrchestrationService
{
    /// <summary>
    /// Executes the concert discovery workflow for a given source URL
    /// 1. Loads previously discovered concerts from storage
    /// 2. Scrapes concerts from the source URL
    /// 3. Generates unique IDs for scraped concerts
    /// 4. Identifies new concerts (not previously seen)
    /// 5. Saves all scraped concerts to storage
    /// 6. Publishes NewConcertsFoundEvent if new concerts were found
    /// </summary>
    /// <param name="sourceUrl">The URL of the concert source to scrape</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing workflow execution details</returns>
    Task<OrchestrationResult> ExecuteWorkflowAsync(
        string sourceUrl,
        CancellationToken cancellationToken = default);
}
