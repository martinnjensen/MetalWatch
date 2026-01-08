namespace MetalWatch.Tests.Services;

using FluentAssertions;
using MetalWatch.Core.Events;
using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Models;
using MetalWatch.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class ConcertOrchestrationServiceTests
{
    private readonly Mock<IScraperFactory> _mockScraperFactory;
    private readonly Mock<IConcertScraper> _mockScraper;
    private readonly Mock<IDataStore> _mockDataStore;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<ILogger<ConcertOrchestrationService>> _mockLogger;
    private readonly ConcertOrchestrationService _service;

    private const string TestSourceUrl = "https://heavymetal.dk/koncertkalender";

    public ConcertOrchestrationServiceTests()
    {
        _mockScraperFactory = new Mock<IScraperFactory>();
        _mockScraper = new Mock<IConcertScraper>();
        _mockDataStore = new Mock<IDataStore>();
        _mockEventBus = new Mock<IEventBus>();
        _mockLogger = new Mock<ILogger<ConcertOrchestrationService>>();

        _mockScraperFactory
            .Setup(f => f.GetScraper(It.IsAny<string>()))
            .Returns(_mockScraper.Object);

        _service = new ConcertOrchestrationService(
            _mockScraperFactory.Object,
            _mockDataStore.Object,
            _mockEventBus.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WithNewConcerts_ReturnsSuccessWithNewConcertsCount()
    {
        // Arrange
        var previousConcerts = new List<Concert>();
        var scrapedConcerts = CreateTestConcerts(3);

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(previousConcerts);

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = scrapedConcerts,
                ConcertsScraped = scrapedConcerts.Count
            });

        // Act
        var result = await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Assert
        result.Success.Should().BeTrue();
        result.ConcertsScraped.Should().Be(3);
        result.NewConcertsCount.Should().Be(3);
        result.SourceUrl.Should().Be(TestSourceUrl);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WithNoNewConcerts_ReturnsSuccessWithZeroNewConcerts()
    {
        // Arrange
        var existingConcerts = CreateTestConcerts(2);
        var scrapedConcerts = CreateTestConcerts(2); // Same concerts

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(existingConcerts);

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = scrapedConcerts,
                ConcertsScraped = scrapedConcerts.Count
            });

        // Act
        var result = await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Assert
        result.Success.Should().BeTrue();
        result.ConcertsScraped.Should().Be(2);
        result.NewConcertsCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WithMixedConcerts_IdentifiesOnlyNewOnes()
    {
        // Arrange
        var existingConcerts = CreateTestConcerts(2);
        var newConcert = CreateConcert("new-concert", "New Venue", DateTime.Today.AddDays(30));
        var scrapedConcerts = new List<Concert>(existingConcerts) { newConcert };

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(existingConcerts);

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = scrapedConcerts,
                ConcertsScraped = scrapedConcerts.Count
            });

        // Act
        var result = await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Assert
        result.Success.Should().BeTrue();
        result.ConcertsScraped.Should().Be(3);
        result.NewConcertsCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_SavesAllScrapedConcerts()
    {
        // Arrange
        var scrapedConcerts = CreateTestConcerts(3);

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = scrapedConcerts,
                ConcertsScraped = scrapedConcerts.Count
            });

        // Act
        await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Assert
        _mockDataStore.Verify(
            d => d.SaveConcertsAsync(It.Is<List<Concert>>(c => c.Count == 3)),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WithNewConcerts_PublishesNewConcertsFoundEvent()
    {
        // Arrange
        var scrapedConcerts = CreateTestConcerts(2);
        NewConcertsFoundEvent? publishedEvent = null;

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = scrapedConcerts,
                ConcertsScraped = scrapedConcerts.Count
            });

        _mockEventBus
            .Setup(e => e.PublishAsync(It.IsAny<NewConcertsFoundEvent>(), It.IsAny<CancellationToken>()))
            .Callback<NewConcertsFoundEvent, CancellationToken>((evt, ct) => publishedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.NewConcerts.Should().HaveCount(2);
        publishedEvent.SourceUrl.Should().Be(TestSourceUrl);
        result.EventsPublished.Should().Contain(nameof(NewConcertsFoundEvent));
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WithNoNewConcerts_DoesNotPublishEvent()
    {
        // Arrange
        var existingConcerts = CreateTestConcerts(2);

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(existingConcerts);

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = existingConcerts,
                ConcertsScraped = existingConcerts.Count
            });

        // Act
        var result = await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Assert
        _mockEventBus.Verify(
            e => e.PublishAsync(It.IsAny<NewConcertsFoundEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        result.EventsPublished.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WhenScraperFails_ReturnsFailureResult()
    {
        // Arrange
        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = false,
                ErrorMessage = "Failed to connect to website"
            });

        // Act
        var result = await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed to connect to website");
        result.ConcertsScraped.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_WhenScraperFails_DoesNotSaveConcerts()
    {
        // Arrange
        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = false,
                ErrorMessage = "Network error"
            });

        // Act
        await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Assert
        _mockDataStore.Verify(
            d => d.SaveConcertsAsync(It.IsAny<List<Concert>>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_SetsExecutedAtTimestamp()
    {
        // Arrange
        var beforeExecution = DateTime.UtcNow;

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = new List<Concert>(),
                ConcertsScraped = 0
            });

        // Act
        var result = await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Assert
        result.ExecutedAt.Should().BeOnOrAfter(beforeExecution);
        result.ExecutedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_GeneratesUniqueIdsForConcerts()
    {
        // Arrange
        var concert1 = CreateConcert("temp-id-1", "Venue A", DateTime.Today.AddDays(10));
        var concert2 = CreateConcert("temp-id-2", "Venue B", DateTime.Today.AddDays(20));
        var scrapedConcerts = new List<Concert> { concert1, concert2 };
        List<Concert>? savedConcerts = null;

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = scrapedConcerts,
                ConcertsScraped = scrapedConcerts.Count
            });

        _mockDataStore
            .Setup(d => d.SaveConcertsAsync(It.IsAny<List<Concert>>()))
            .Callback<List<Concert>>(concerts => savedConcerts = concerts)
            .Returns(Task.CompletedTask);

        // Act
        await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Assert
        savedConcerts.Should().NotBeNull();
        savedConcerts!.Should().HaveCount(2);
        savedConcerts[0].Id.Should().NotBe("temp-id-1");
        savedConcerts[1].Id.Should().NotBe("temp-id-2");
        savedConcerts[0].Id.Should().NotBe(savedConcerts[1].Id);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_SameConcertDetails_GeneratesSameId()
    {
        // Arrange - First run
        var concert = CreateConcert("temp", "VEGA", new DateTime(2025, 6, 15));
        concert.Artists = new List<string> { "Metallica" };

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = new List<Concert> { concert },
                ConcertsScraped = 1
            });

        string? firstRunId = null;
        _mockDataStore
            .Setup(d => d.SaveConcertsAsync(It.IsAny<List<Concert>>()))
            .Callback<List<Concert>>(concerts => firstRunId = concerts[0].Id)
            .Returns(Task.CompletedTask);

        await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Arrange - Second run with same concert
        var sameConcert = CreateConcert("different-temp", "VEGA", new DateTime(2025, 6, 15));
        sameConcert.Artists = new List<string> { "Metallica" };

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = new List<Concert> { sameConcert },
                ConcertsScraped = 1
            });

        string? secondRunId = null;
        _mockDataStore
            .Setup(d => d.SaveConcertsAsync(It.IsAny<List<Concert>>()))
            .Callback<List<Concert>>(concerts => secondRunId = concerts[0].Id)
            .Returns(Task.CompletedTask);

        // Act
        await _service.ExecuteWorkflowAsync(TestSourceUrl);

        // Assert
        firstRunId.Should().NotBeNull();
        secondRunId.Should().NotBeNull();
        firstRunId.Should().Be(secondRunId);
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_PassesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraper
            .Setup(s => s.ScrapeAsync(TestSourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult { Success = true, Concerts = new List<Concert>() });

        // Act
        await _service.ExecuteWorkflowAsync(TestSourceUrl, token);

        // Assert
        _mockScraper.Verify(
            s => s.ScrapeAsync(TestSourceUrl, token),
            Times.Once);
    }

    private static List<Concert> CreateTestConcerts(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateConcert(
                $"concert-{i}",
                $"Venue {i}",
                DateTime.Today.AddDays(i * 7)))
            .ToList();
    }

    private static Concert CreateConcert(string id, string venue, DateTime date)
    {
        return new Concert
        {
            Id = id,
            Venue = venue,
            Date = date,
            DayOfWeek = date.DayOfWeek.ToString(),
            ConcertUrl = $"https://example.com/{id}",
            Artists = new List<string> { $"Artist for {id}" }
        };
    }
}
