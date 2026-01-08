namespace MetalWatch.Infrastructure.Events;

using MetalWatch.Core.Events;
using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Event handler that processes NewConcertsFoundEvent.
/// Matches concerts against user preferences and sends notifications.
/// </summary>
public class NotificationEventHandler
{
    private readonly IDataStore _dataStore;
    private readonly IConcertMatcher _matcher;
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of NotificationEventHandler without event bus subscription.
    /// Use this constructor for testing when you want to call HandleAsync directly.
    /// </summary>
    public NotificationEventHandler(
        IDataStore dataStore,
        IConcertMatcher matcher,
        INotificationService notificationService,
        ILogger<NotificationEventHandler> logger)
    {
        _dataStore = dataStore;
        _matcher = matcher;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of NotificationEventHandler with event bus subscription.
    /// Automatically subscribes to NewConcertsFoundEvent.
    /// </summary>
    public NotificationEventHandler(
        IDataStore dataStore,
        IConcertMatcher matcher,
        INotificationService notificationService,
        ILogger<NotificationEventHandler> logger,
        IEventBus eventBus)
        : this(dataStore, matcher, notificationService, logger)
    {
        eventBus.Subscribe<NewConcertsFoundEvent>(HandleAsync);
    }

    /// <summary>
    /// Handles the NewConcertsFoundEvent by matching concerts and sending notifications.
    /// </summary>
    /// <param name="evt">The event containing new concerts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task HandleAsync(NewConcertsFoundEvent evt, CancellationToken cancellationToken)
    {
        // Early return if no concerts
        if (evt.NewConcerts == null || evt.NewConcerts.Count == 0)
        {
            _logger.LogInformation("No new concerts to process");
            return;
        }

        // Load preferences from data store
        var preferences = await _dataStore.GetPreferencesAsync();

        // Match concerts against user preferences
        var matchingConcerts = _matcher.FindMatches(evt.NewConcerts, preferences);

        // If no matches, don't send notification
        if (matchingConcerts.Count == 0)
        {
            _logger.LogInformation("No matching concerts found for preferences");
            return;
        }

        // Send notification (with exception handling to prevent propagation)
        try
        {
            var result = await _notificationService.SendNotificationAsync(matchingConcerts, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Notification sent for {Count} matching concerts",
                    matchingConcerts.Count);
            }
            else
            {
                _logger.LogWarning("Notification failed: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            // Notification failures should not propagate (per architecture decision)
            _logger.LogError(ex, "Exception while sending notification");
        }
    }
}
