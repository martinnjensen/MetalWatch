namespace MetalWatch.Core.Interfaces;

using MetalWatch.Core.Models;

/// <summary>
/// Interface for the concert orchestration service
/// Coordinates the workflow of retrieving due sources, scraping, detecting new concerts, and publishing events
/// </summary>
public interface IConcertOrchestrationService
{
    /// <summary>
    /// Executes the concert discovery workflow for all sources due for scraping.
    ///
    /// For each due source:
    /// 1. Gets the appropriate scraper via IScraperFactory
    /// 2. Scrapes concerts from the source URL
    /// 3. Generates unique IDs for scraped concerts
    /// 4. Loads previously discovered concerts from storage
    /// 5. Identifies new concerts (not previously seen)
    /// 6. Saves all scraped concerts to storage
    /// 7. Publishes NewConcertsFoundEvent if new concerts were found
    /// 8. Updates the source's LastScrapedAt timestamp
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of results containing workflow execution details for each source</returns>
    Task<List<OrchestrationResult>> ExecuteDueWorkflowsAsync(CancellationToken cancellationToken = default);
}
