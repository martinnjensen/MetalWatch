namespace MetalWatch.Infrastructure.Notifications;

using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Notification service that outputs matched concerts to the console.
/// Used for development and testing purposes.
/// </summary>
public class ConsoleNotificationService : INotificationService
{
    private readonly ILogger<ConsoleNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of ConsoleNotificationService
    /// </summary>
    public ConsoleNotificationService(ILogger<ConsoleNotificationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<NotificationResult> SendNotificationAsync(
        List<Concert> concerts,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement console notification logic
        throw new NotImplementedException();
    }
}
