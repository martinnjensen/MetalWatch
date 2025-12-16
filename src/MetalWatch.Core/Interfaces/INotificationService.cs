namespace MetalWatch.Core.Interfaces;

using MetalWatch.Core.Models;

/// <summary>
/// Interface for notification service implementations
/// Supports pluggable notification channels (Email, Slack, Discord, etc.)
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification about matched concerts
    /// </summary>
    /// <param name="concerts">List of concerts to notify about</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<NotificationResult> SendNotificationAsync(
        List<Concert> concerts,
        CancellationToken cancellationToken = default);
}
