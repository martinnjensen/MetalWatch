namespace MetalWatch.Tests.Events;

using FluentAssertions;
using MetalWatch.Core.Events;
using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Models;
using MetalWatch.Infrastructure.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class NotificationEventHandlerTests
{
    private readonly Mock<IDataStore> _mockDataStore;
    private readonly Mock<IConcertMatcher> _mockMatcher;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<NotificationEventHandler>> _mockLogger;
    private readonly NotificationEventHandler _handler;

    public NotificationEventHandlerTests()
    {
        _mockDataStore = new Mock<IDataStore>();
        _mockMatcher = new Mock<IConcertMatcher>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<NotificationEventHandler>>();

        _handler = new NotificationEventHandler(
            _mockDataStore.Object,
            _mockMatcher.Object,
            _mockNotificationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_LoadsPreferencesFromDataStore()
    {
        // Arrange
        var concerts = CreateTestConcerts(2);
        var preferences = CreateTestPreferences();
        var evt = CreateNewConcertsFoundEvent(concerts);

        _mockDataStore
            .Setup(d => d.GetPreferencesAsync())
            .ReturnsAsync(preferences);

        _mockMatcher
            .Setup(m => m.FindMatches(It.IsAny<List<Concert>>(), It.IsAny<ConcertPreferences>()))
            .Returns(new List<Concert>());

        _mockNotificationService
            .Setup(n => n.SendNotificationAsync(It.IsAny<List<Concert>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true, Message = "OK" });

        // Act
        await _handler.HandleAsync(evt, CancellationToken.None);

        // Assert
        _mockDataStore.Verify(d => d.GetPreferencesAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_MatchesConcertsAgainstPreferences()
    {
        // Arrange
        var concerts = CreateTestConcerts(3);
        var preferences = CreateTestPreferences();
        var evt = CreateNewConcertsFoundEvent(concerts);

        _mockDataStore
            .Setup(d => d.GetPreferencesAsync())
            .ReturnsAsync(preferences);

        _mockMatcher
            .Setup(m => m.FindMatches(It.IsAny<List<Concert>>(), It.IsAny<ConcertPreferences>()))
            .Returns(new List<Concert>());

        _mockNotificationService
            .Setup(n => n.SendNotificationAsync(It.IsAny<List<Concert>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true, Message = "OK" });

        // Act
        await _handler.HandleAsync(evt, CancellationToken.None);

        // Assert
        _mockMatcher.Verify(
            m => m.FindMatches(
                It.Is<List<Concert>>(c => c.Count == 3),
                It.Is<ConcertPreferences>(p => p == preferences)),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithMatchingConcerts_SendsNotification()
    {
        // Arrange
        var concerts = CreateTestConcerts(3);
        var matchingConcerts = concerts.Take(2).ToList();
        var preferences = CreateTestPreferences();
        var evt = CreateNewConcertsFoundEvent(concerts);

        _mockDataStore
            .Setup(d => d.GetPreferencesAsync())
            .ReturnsAsync(preferences);

        _mockMatcher
            .Setup(m => m.FindMatches(It.IsAny<List<Concert>>(), It.IsAny<ConcertPreferences>()))
            .Returns(matchingConcerts);

        _mockNotificationService
            .Setup(n => n.SendNotificationAsync(It.IsAny<List<Concert>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true, Message = "OK", ConcertsNotified = 2 });

        // Act
        await _handler.HandleAsync(evt, CancellationToken.None);

        // Assert
        _mockNotificationService.Verify(
            n => n.SendNotificationAsync(
                It.Is<List<Concert>>(c => c.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNoMatchingConcerts_DoesNotSendNotification()
    {
        // Arrange
        var concerts = CreateTestConcerts(3);
        var preferences = CreateTestPreferences();
        var evt = CreateNewConcertsFoundEvent(concerts);

        _mockDataStore
            .Setup(d => d.GetPreferencesAsync())
            .ReturnsAsync(preferences);

        _mockMatcher
            .Setup(m => m.FindMatches(It.IsAny<List<Concert>>(), It.IsAny<ConcertPreferences>()))
            .Returns(new List<Concert>());

        // Act
        await _handler.HandleAsync(evt, CancellationToken.None);

        // Assert
        _mockNotificationService.Verify(
            n => n.SendNotificationAsync(It.IsAny<List<Concert>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenNotificationFails_DoesNotThrow()
    {
        // Arrange
        var concerts = CreateTestConcerts(2);
        var preferences = CreateTestPreferences();
        var evt = CreateNewConcertsFoundEvent(concerts);

        _mockDataStore
            .Setup(d => d.GetPreferencesAsync())
            .ReturnsAsync(preferences);

        _mockMatcher
            .Setup(m => m.FindMatches(It.IsAny<List<Concert>>(), It.IsAny<ConcertPreferences>()))
            .Returns(concerts);

        _mockNotificationService
            .Setup(n => n.SendNotificationAsync(It.IsAny<List<Concert>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = false, Message = "SMTP error" });

        // Act
        var act = async () => await _handler.HandleAsync(evt, CancellationToken.None);

        // Assert - should not throw even when notification fails
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_WhenNotificationThrows_DoesNotPropagate()
    {
        // Arrange
        var concerts = CreateTestConcerts(2);
        var preferences = CreateTestPreferences();
        var evt = CreateNewConcertsFoundEvent(concerts);

        _mockDataStore
            .Setup(d => d.GetPreferencesAsync())
            .ReturnsAsync(preferences);

        _mockMatcher
            .Setup(m => m.FindMatches(It.IsAny<List<Concert>>(), It.IsAny<ConcertPreferences>()))
            .Returns(concerts);

        _mockNotificationService
            .Setup(n => n.SendNotificationAsync(It.IsAny<List<Concert>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Network failure"));

        // Act
        var act = async () => await _handler.HandleAsync(evt, CancellationToken.None);

        // Assert - notification failures should not propagate (per architecture)
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleAsync_PassesCancellationToken()
    {
        // Arrange
        var concerts = CreateTestConcerts(1);
        var preferences = CreateTestPreferences();
        var evt = CreateNewConcertsFoundEvent(concerts);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _mockDataStore
            .Setup(d => d.GetPreferencesAsync())
            .ReturnsAsync(preferences);

        _mockMatcher
            .Setup(m => m.FindMatches(It.IsAny<List<Concert>>(), It.IsAny<ConcertPreferences>()))
            .Returns(concerts);

        _mockNotificationService
            .Setup(n => n.SendNotificationAsync(It.IsAny<List<Concert>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true, Message = "OK" });

        // Act
        await _handler.HandleAsync(evt, token);

        // Assert
        _mockNotificationService.Verify(
            n => n.SendNotificationAsync(It.IsAny<List<Concert>>(), token),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyNewConcerts_DoesNotCallMatcher()
    {
        // Arrange
        var evt = CreateNewConcertsFoundEvent(new List<Concert>());

        // Act
        await _handler.HandleAsync(evt, CancellationToken.None);

        // Assert
        _mockMatcher.Verify(
            m => m.FindMatches(It.IsAny<List<Concert>>(), It.IsAny<ConcertPreferences>()),
            Times.Never);
    }

    [Fact]
    public void Constructor_SubscribesToEventBus()
    {
        // Arrange
        var mockEventBus = new Mock<IEventBus>();
        var mockDataStore = new Mock<IDataStore>();
        var mockMatcher = new Mock<IConcertMatcher>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockLogger = new Mock<ILogger<NotificationEventHandler>>();

        // Act
        var handler = new NotificationEventHandler(
            mockDataStore.Object,
            mockMatcher.Object,
            mockNotificationService.Object,
            mockLogger.Object,
            mockEventBus.Object);

        // Assert
        mockEventBus.Verify(
            e => e.Subscribe<NewConcertsFoundEvent>(It.IsAny<Func<NewConcertsFoundEvent, CancellationToken, Task>>()),
            Times.Once);
    }

    private static List<Concert> CreateTestConcerts(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Concert
            {
                Id = $"concert-{i}",
                Venue = $"Venue {i}",
                Date = DateTime.Today.AddDays(i * 7),
                DayOfWeek = DateTime.Today.AddDays(i * 7).DayOfWeek.ToString(),
                ConcertUrl = $"https://example.com/concert-{i}",
                Artists = new List<string> { $"Artist {i}" }
            })
            .ToList();
    }

    private static ConcertPreferences CreateTestPreferences()
    {
        return new ConcertPreferences
        {
            FavoriteArtists = new List<string> { "Metallica", "Iron Maiden" },
            FavoriteVenues = new List<string> { "VEGA", "Pumpehuset" },
            Keywords = new List<string> { "metal", "rock" }
        };
    }

    private static NewConcertsFoundEvent CreateNewConcertsFoundEvent(List<Concert> concerts)
    {
        return new NewConcertsFoundEvent
        {
            NewConcerts = concerts,
            SourceUrl = "https://heavymetal.dk/koncertkalender",
            FoundAt = DateTime.UtcNow
        };
    }
}
