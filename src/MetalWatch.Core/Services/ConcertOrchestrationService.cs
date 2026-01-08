namespace MetalWatch.Core.Services;

using MetalWatch.Core.Events;
using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates the concert discovery workflow.
/// Retrieves due sources, coordinates scraping, new concert detection, storage, and event publishing.
/// </summary>
public class ConcertOrchestrationService : IConcertOrchestrationService
{
    private readonly IScraperFactory _scraperFactory;
    private readonly IDataStore _dataStore;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ConcertOrchestrationService> _logger;

    /// <summary>
    /// Initializes a new instance of ConcertOrchestrationService
    /// </summary>
    public ConcertOrchestrationService(
        IScraperFactory scraperFactory,
        IDataStore dataStore,
        IEventBus eventBus,
        ILogger<ConcertOrchestrationService> logger)
    {
        _scraperFactory = scraperFactory;
        _dataStore = dataStore;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<List<OrchestrationResult>> ExecuteDueWorkflowsAsync(
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement workflow logic
        throw new NotImplementedException();
    }
}
