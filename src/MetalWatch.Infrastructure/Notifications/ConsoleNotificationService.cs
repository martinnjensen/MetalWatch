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
        cancellationToken.ThrowIfCancellationRequested();

        var timestamp = DateTime.UtcNow;

        if (concerts.Count == 0)
        {
            _logger.LogInformation("No concerts to notify about");
            return Task.FromResult(new NotificationResult
            {
                Success = true,
                Message = "No concerts to notify about",
                ConcertsNotified = 0,
                SentAt = timestamp
            });
        }

        _logger.LogInformation("Notifying about {Count} matched concert(s)", concerts.Count);
        Console.WriteLine($"\n=== {concerts.Count} Matched Concert(s) ===\n");

        foreach (var concert in concerts)
        {
            var status = concert.IsCancelled ? "[CANCELLED] " : "";
            var type = concert.IsFestival ? "Festival" : "Concert";
            var artists = string.Join(", ", concert.Artists);

            Console.WriteLine($"{status}{type}: {artists}");
            Console.WriteLine($"  Date: {concert.Date:yyyy-MM-dd} ({concert.DayOfWeek})");
            Console.WriteLine($"  Venue: {concert.Venue}");
            Console.WriteLine($"  URL: {concert.ConcertUrl}");
            Console.WriteLine();
        }

        var message = $"Successfully notified about {concerts.Count} concert(s)";
        _logger.LogInformation("{Message}", message);

        return Task.FromResult(new NotificationResult
        {
            Success = true,
            Message = message,
            ConcertsNotified = concerts.Count,
            SentAt = timestamp
        });
    }
}
