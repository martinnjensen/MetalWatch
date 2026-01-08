namespace MetalWatch.Tests.Notifications;

using FluentAssertions;
using MetalWatch.Core.Models;
using MetalWatch.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class ConsoleNotificationServiceTests
{
    private readonly Mock<ILogger<ConsoleNotificationService>> _mockLogger;
    private readonly ConsoleNotificationService _service;

    public ConsoleNotificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ConsoleNotificationService>>();
        _service = new ConsoleNotificationService(_mockLogger.Object);
    }

    [Fact]
    public async Task SendNotificationAsync_WithConcerts_ReturnsSuccess()
    {
        // Arrange
        var concerts = new List<Concert>
        {
            CreateConcert("concert-1", "VEGA", new DateTime(2025, 6, 15), new List<string> { "Metallica" })
        };

        // Act
        var result = await _service.SendNotificationAsync(concerts);

        // Assert
        result.Success.Should().BeTrue();
        result.ConcertsNotified.Should().Be(1);
    }

    [Fact]
    public async Task SendNotificationAsync_WithEmptyList_ReturnsSuccessWithZeroConcerts()
    {
        // Arrange
        var concerts = new List<Concert>();

        // Act
        var result = await _service.SendNotificationAsync(concerts);

        // Assert
        result.Success.Should().BeTrue();
        result.ConcertsNotified.Should().Be(0);
        result.Message.Should().Contain("No concerts");
    }

    [Fact]
    public async Task SendNotificationAsync_WithMultipleConcerts_ReturnsCorrectCount()
    {
        // Arrange
        var concerts = new List<Concert>
        {
            CreateConcert("concert-1", "VEGA", new DateTime(2025, 6, 15), new List<string> { "Metallica" }),
            CreateConcert("concert-2", "Pumpehuset", new DateTime(2025, 7, 20), new List<string> { "Iron Maiden" }),
            CreateConcert("concert-3", "Store Vega", new DateTime(2025, 8, 10), new List<string> { "Slayer" })
        };

        // Act
        var result = await _service.SendNotificationAsync(concerts);

        // Assert
        result.Success.Should().BeTrue();
        result.ConcertsNotified.Should().Be(3);
    }

    [Fact]
    public async Task SendNotificationAsync_SetsSentAtTimestamp()
    {
        // Arrange
        var concerts = new List<Concert>
        {
            CreateConcert("concert-1", "VEGA", new DateTime(2025, 6, 15), new List<string> { "Metallica" })
        };
        var beforeSend = DateTime.UtcNow;

        // Act
        var result = await _service.SendNotificationAsync(concerts);

        // Assert
        result.SentAt.Should().BeOnOrAfter(beforeSend);
        result.SentAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task SendNotificationAsync_WithConcerts_ReturnsSuccessMessage()
    {
        // Arrange
        var concerts = new List<Concert>
        {
            CreateConcert("concert-1", "VEGA", new DateTime(2025, 6, 15), new List<string> { "Metallica" })
        };

        // Act
        var result = await _service.SendNotificationAsync(concerts);

        // Assert
        result.Message.Should().NotBeNullOrEmpty();
        result.Message.Should().Contain("1");
    }

    [Fact]
    public async Task SendNotificationAsync_WithFestival_IncludesInOutput()
    {
        // Arrange
        var concert = CreateConcert("festival-1", "Roskilde", new DateTime(2025, 7, 1),
            new List<string> { "Metallica", "Iron Maiden", "Slayer", "Megadeth" });
        concert.IsFestival = true;

        var concerts = new List<Concert> { concert };

        // Act
        var result = await _service.SendNotificationAsync(concerts);

        // Assert
        result.Success.Should().BeTrue();
        result.ConcertsNotified.Should().Be(1);
    }

    [Fact]
    public async Task SendNotificationAsync_WithCancelledConcert_IncludesInOutput()
    {
        // Arrange
        var concert = CreateConcert("cancelled-1", "VEGA", new DateTime(2025, 6, 15),
            new List<string> { "Cancelled Band" });
        concert.IsCancelled = true;

        var concerts = new List<Concert> { concert };

        // Act
        var result = await _service.SendNotificationAsync(concerts);

        // Assert
        result.Success.Should().BeTrue();
        result.ConcertsNotified.Should().Be(1);
    }

    [Fact]
    public async Task SendNotificationAsync_RespectsCancellationToken()
    {
        // Arrange
        var concerts = new List<Concert>
        {
            CreateConcert("concert-1", "VEGA", new DateTime(2025, 6, 15), new List<string> { "Metallica" })
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await _service.SendNotificationAsync(concerts, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static Concert CreateConcert(string id, string venue, DateTime date, List<string> artists)
    {
        return new Concert
        {
            Id = id,
            Venue = venue,
            Date = date,
            DayOfWeek = date.DayOfWeek.ToString(),
            ConcertUrl = $"https://example.com/{id}",
            Artists = artists
        };
    }
}
