namespace MetalWatch.Worker;

using MetalWatch.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background service that orchestrates concert scraping and notification workflow.
/// Runs once on startup for development/testing purposes.
/// </summary>
public class ConcertScrapingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConcertScrapingHostedService> _logger;

    public ConcertScrapingHostedService(
        IServiceProvider serviceProvider,
        ILogger<ConcertScrapingHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Concert scraping service started");

        try
        {
            // Wait a moment for the host to fully start
            await Task.Delay(1000, stoppingToken);

            _logger.LogInformation("Starting concert orchestration...");

            // Create a scope to get scoped services
            using var scope = _serviceProvider.CreateScope();
            var orchestrationService = scope.ServiceProvider.GetRequiredService<IConcertOrchestrationService>();

            // Run orchestration
            var results = await orchestrationService.ExecuteDueWorkflowsAsync(stoppingToken);

            _logger.LogInformation("Orchestration completed. Processed {ResultCount} source(s)", results.Count);

            foreach (var result in results)
            {
                if (result.Success)
                {
                    _logger.LogInformation(
                        "Source: {SourceName} - Scraped: {ScrapedCount}, New: {NewCount}",
                        result.SourceName,
                        result.ConcertsScraped,
                        result.NewConcertsCount);
                }
                else
                {
                    _logger.LogError("Error scraping {SourceName}: {Error}", result.SourceName, result.ErrorMessage);
                }
            }

            _logger.LogInformation("Concert scraping service completed. Press Ctrl+C to exit.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Concert scraping service cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in concert scraping service");
            throw;
        }
    }
}
