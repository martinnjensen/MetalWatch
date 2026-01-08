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
    public Task HandleAsync(NewConcertsFoundEvent evt, CancellationToken cancellationToken)
    {
        // TODO: Implement event handling logic
        throw new NotImplementedException();
    }
}
