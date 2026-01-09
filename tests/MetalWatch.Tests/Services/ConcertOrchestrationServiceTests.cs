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

    public ConcertOrchestrationServiceTests()
    {
        _mockScraperFactory = new Mock<IScraperFactory>();
        _mockScraper = new Mock<IConcertScraper>();
        _mockDataStore = new Mock<IDataStore>();
        _mockEventBus = new Mock<IEventBus>();
        _mockLogger = new Mock<ILogger<ConcertOrchestrationService>>();

        _service = new ConcertOrchestrationService(
            _mockScraperFactory.Object,
            _mockDataStore.Object,
            _mockEventBus.Object,
            _mockLogger.Object);
    }

    #region Source Retrieval Tests

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_RetrievesDueSourcesFromDataStore()
    {
        // Arrange
        var sources = new List<ConcertSource>();
        _mockDataStore
            .Setup(d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);

        // Act
        await _service.ExecuteDueWorkflowsAsync();

        // Assert
        _mockDataStore.Verify(
            d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_WithNoSources_ReturnsEmptyList()
    {
        // Arrange
        _mockDataStore
            .Setup(d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConcertSource>());

        // Act
        var results = await _service.ExecuteDueWorkflowsAsync();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_WithNoSources_DoesNotCallScraper()
    {
        // Arrange
        _mockDataStore
            .Setup(d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConcertSource>());

        // Act
        await _service.ExecuteDueWorkflowsAsync();

        // Assert
        _mockScraperFactory.Verify(
            f => f.GetScraperByName(It.IsAny<string>()),
            Times.Never);
    }

    #endregion

    #region Scraper Selection Tests

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_GetsScraperBySourceScraperType()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        SetupSingleSourceScenario(source, new List<Concert>(), new List<Concert>());

        // Act
        await _service.ExecuteDueWorkflowsAsync();

        // Assert
        _mockScraperFactory.Verify(
            f => f.GetScraperByName("HeavyMetalDk"),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_WithMultipleSources_GetsScraperForEach()
    {
        // Arrange
        var source1 = CreateTestSource("source-1", "HeavyMetalDk");
        var source2 = CreateTestSource("source-2", "Songkick");
        var sources = new List<ConcertSource> { source1, source2 };

        _mockDataStore
            .Setup(d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraperFactory
            .Setup(f => f.GetScraperByName(It.IsAny<string>()))
            .Returns(_mockScraper.Object);

        _mockScraper
            .Setup(s => s.ScrapeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult { Success = true, Concerts = new List<Concert>() });

        // Act
        await _service.ExecuteDueWorkflowsAsync();

        // Assert
        _mockScraperFactory.Verify(f => f.GetScraperByName("HeavyMetalDk"), Times.Once);
        _mockScraperFactory.Verify(f => f.GetScraperByName("Songkick"), Times.Once);
    }

    #endregion

    #region Scraping Tests

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_ScrapesSourceUrl()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk", "https://heavymetal.dk/concerts");
        SetupSingleSourceScenario(source, new List<Concert>(), new List<Concert>());

        // Act
        await _service.ExecuteDueWorkflowsAsync();

        // Assert
        _mockScraper.Verify(
            s => s.ScrapeAsync("https://heavymetal.dk/concerts", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_WithNewConcerts_ReturnsSuccessWithNewConcertsCount()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        var scrapedConcerts = CreateTestConcerts(3);
        SetupSingleSourceScenario(source, new List<Concert>(), scrapedConcerts);

        // Act
        var results = await _service.ExecuteDueWorkflowsAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        results[0].ConcertsScraped.Should().Be(3);
        results[0].NewConcertsCount.Should().Be(3);
        results[0].SourceId.Should().Be("source-1");
        results[0].SourceName.Should().Be("HeavyMetal.dk");
    }

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_WithNoNewConcerts_ReturnsSuccessWithZeroNewConcerts()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        var existingConcerts = CreateTestConcerts(2);
        SetupSingleSourceScenario(source, existingConcerts, existingConcerts);

        // Act
        var results = await _service.ExecuteDueWorkflowsAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        results[0].ConcertsScraped.Should().Be(2);
        results[0].NewConcertsCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_WithMixedConcerts_IdentifiesOnlyNewOnes()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        var existingConcerts = CreateTestConcerts(2);
        var newConcert = CreateConcert("new-concert", "New Venue", DateTime.Today.AddDays(30));
        var scrapedConcerts = new List<Concert>(existingConcerts) { newConcert };
        SetupSingleSourceScenario(source, existingConcerts, scrapedConcerts);

        // Act
        var results = await _service.ExecuteDueWorkflowsAsync();

        // Assert
        results[0].Success.Should().BeTrue();
        results[0].ConcertsScraped.Should().Be(3);
        results[0].NewConcertsCount.Should().Be(1);
    }

    #endregion

    #region Concert Persistence Tests

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_SavesAllScrapedConcerts()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        var scrapedConcerts = CreateTestConcerts(3);
        SetupSingleSourceScenario(source, new List<Concert>(), scrapedConcerts);

        // Act
        await _service.ExecuteDueWorkflowsAsync();

        // Assert
        _mockDataStore.Verify(
            d => d.SaveConcertsAsync(It.Is<List<Concert>>(c => c.Count == 3)),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_WhenScraperFails_DoesNotSaveConcerts()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        _mockDataStore
            .Setup(d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConcertSource> { source });

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraperFactory
            .Setup(f => f.GetScraperByName(It.IsAny<string>()))
            .Returns(_mockScraper.Object);

        _mockScraper
            .Setup(s => s.ScrapeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult { Success = false, ErrorMessage = "Network error" });

        // Act
        await _service.ExecuteDueWorkflowsAsync();

        // Assert
        _mockDataStore.Verify(
            d => d.SaveConcertsAsync(It.IsAny<List<Concert>>()),
            Times.Never);
    }

    #endregion

    #region Event Publishing Tests

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_WithNewConcerts_PublishesNewConcertsFoundEvent()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        var scrapedConcerts = CreateTestConcerts(2);
        NewConcertsFoundEvent? publishedEvent = null;

        SetupSingleSourceScenario(source, new List<Concert>(), scrapedConcerts);

        _mockEventBus
            .Setup(e => e.PublishAsync(It.IsAny<NewConcertsFoundEvent>(), It.IsAny<CancellationToken>()))
            .Callback<NewConcertsFoundEvent, CancellationToken>((evt, ct) => publishedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        var results = await _service.ExecuteDueWorkflowsAsync();

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.NewConcerts.Should().HaveCount(2);
        publishedEvent.SourceUrl.Should().Be(source.Url);
        results[0].EventsPublished.Should().Contain(nameof(NewConcertsFoundEvent));
    }

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_WithNoNewConcerts_DoesNotPublishEvent()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        var existingConcerts = CreateTestConcerts(2);
        SetupSingleSourceScenario(source, existingConcerts, existingConcerts);

        // Act
        var results = await _service.ExecuteDueWorkflowsAsync();

        // Assert
        _mockEventBus.Verify(
            e => e.PublishAsync(It.IsAny<NewConcertsFoundEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        results[0].EventsPublished.Should().BeEmpty();
    }

    #endregion

    #region Failure Handling Tests

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_WhenScraperFails_ReturnsFailureResult()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        _mockDataStore
            .Setup(d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConcertSource> { source });

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraperFactory
            .Setup(f => f.GetScraperByName(It.IsAny<string>()))
            .Returns(_mockScraper.Object);

        _mockScraper
            .Setup(s => s.ScrapeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult { Success = false, ErrorMessage = "Failed to connect" });

        // Act
        var results = await _service.ExecuteDueWorkflowsAsync();

        // Assert
        results.Should().HaveCount(1);
        results[0].Success.Should().BeFalse();
        results[0].ErrorMessage.Should().Be("Failed to connect");
        results[0].ConcertsScraped.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_WhenOneSourceFails_ContinuesWithOtherSources()
    {
        // Arrange
        var source1 = CreateTestSource("source-1", "HeavyMetalDk");
        var source2 = CreateTestSource("source-2", "Songkick");
        var sources = new List<ConcertSource> { source1, source2 };
        var concertsForSource2 = CreateTestConcerts(2);

        var scraper1 = new Mock<IConcertScraper>();
        var scraper2 = new Mock<IConcertScraper>();

        _mockDataStore
            .Setup(d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sources);

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraperFactory
            .Setup(f => f.GetScraperByName("HeavyMetalDk"))
            .Returns(scraper1.Object);
        _mockScraperFactory
            .Setup(f => f.GetScraperByName("Songkick"))
            .Returns(scraper2.Object);

        scraper1
            .Setup(s => s.ScrapeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult { Success = false, ErrorMessage = "Network error" });

        scraper2
            .Setup(s => s.ScrapeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = concertsForSource2,
                ConcertsScraped = concertsForSource2.Count
            });

        // Act
        var results = await _service.ExecuteDueWorkflowsAsync();

        // Assert
        results.Should().HaveCount(2);
        results[0].Success.Should().BeFalse();
        results[0].SourceId.Should().Be("source-1");
        results[1].Success.Should().BeTrue();
        results[1].SourceId.Should().Be("source-2");
        results[1].ConcertsScraped.Should().Be(2);
    }

    #endregion

    #region Scrape Status Update Tests

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_OnSuccess_UpdatesSourceWithSuccessStatus()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        var scrapedConcerts = CreateTestConcerts(2);
        SetupSingleSourceScenario(source, new List<Concert>(), scrapedConcerts);

        // Act
        await _service.ExecuteDueWorkflowsAsync();

        // Assert
        _mockDataStore.Verify(
            d => d.UpdateSourceScrapedAsync(
                "source-1",
                It.IsAny<DateTime>(),
                true,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_OnFailure_StillUpdatesSourceWithFailureStatus()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        _mockDataStore
            .Setup(d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConcertSource> { source });

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraperFactory
            .Setup(f => f.GetScraperByName(It.IsAny<string>()))
            .Returns(_mockScraper.Object);

        _mockScraper
            .Setup(s => s.ScrapeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult { Success = false, ErrorMessage = "Network error" });

        // Act
        await _service.ExecuteDueWorkflowsAsync();

        // Assert - should still update to prevent excessive retries
        _mockDataStore.Verify(
            d => d.UpdateSourceScrapedAsync(
                "source-1",
                It.IsAny<DateTime>(),
                false,
                "Network error",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Timestamp and Metadata Tests

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_SetsExecutedAtTimestamp()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        var beforeExecution = DateTime.UtcNow;
        SetupSingleSourceScenario(source, new List<Concert>(), new List<Concert>());

        // Act
        var results = await _service.ExecuteDueWorkflowsAsync();

        // Assert
        results[0].ExecutedAt.Should().BeOnOrAfter(beforeExecution);
        results[0].ExecutedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    #endregion

    #region Concert ID Generation Tests

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_GeneratesUniqueIdsForConcerts()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        var concert1 = CreateConcert("temp-id-1", "Venue A", DateTime.Today.AddDays(10));
        var concert2 = CreateConcert("temp-id-2", "Venue B", DateTime.Today.AddDays(20));
        var scrapedConcerts = new List<Concert> { concert1, concert2 };
        List<Concert>? savedConcerts = null;

        _mockDataStore
            .Setup(d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConcertSource> { source });

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraperFactory
            .Setup(f => f.GetScraperByName(It.IsAny<string>()))
            .Returns(_mockScraper.Object);

        _mockScraper
            .Setup(s => s.ScrapeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
        await _service.ExecuteDueWorkflowsAsync();

        // Assert
        savedConcerts.Should().NotBeNull();
        var concerts = savedConcerts!;
        concerts.Should().HaveCount(2);
        concerts[0].Id.Should().NotBe("temp-id-1");
        concerts[1].Id.Should().NotBe("temp-id-2");
        concerts[0].Id.Should().NotBe(concerts[1].Id);
    }

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_SameConcertDetails_GeneratesSameId()
    {
        // Arrange - First run
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        var concert = CreateConcert("temp", "VEGA", new DateTime(2025, 6, 15));
        concert.Artists = new List<string> { "Metallica" };

        _mockDataStore
            .Setup(d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConcertSource> { source });

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(new List<Concert>());

        _mockScraperFactory
            .Setup(f => f.GetScraperByName(It.IsAny<string>()))
            .Returns(_mockScraper.Object);

        _mockScraper
            .Setup(s => s.ScrapeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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

        await _service.ExecuteDueWorkflowsAsync();

        // Arrange - Second run with same concert
        var sameConcert = CreateConcert("different-temp", "VEGA", new DateTime(2025, 6, 15));
        sameConcert.Artists = new List<string> { "Metallica" };

        _mockScraper
            .Setup(s => s.ScrapeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
        await _service.ExecuteDueWorkflowsAsync();

        // Assert
        firstRunId.Should().NotBeNull();
        secondRunId.Should().NotBeNull();
        firstRunId.Should().Be(secondRunId);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteDueWorkflowsAsync_PassesCancellationToken()
    {
        // Arrange
        var source = CreateTestSource("source-1", "HeavyMetalDk");
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        SetupSingleSourceScenario(source, new List<Concert>(), new List<Concert>());

        // Act
        await _service.ExecuteDueWorkflowsAsync(token);

        // Assert
        _mockDataStore.Verify(
            d => d.GetSourcesDueForScrapingAsync(token),
            Times.Once);
        _mockScraper.Verify(
            s => s.ScrapeAsync(It.IsAny<string>(), token),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupSingleSourceScenario(
        ConcertSource source,
        List<Concert> existingConcerts,
        List<Concert> scrapedConcerts)
    {
        _mockDataStore
            .Setup(d => d.GetSourcesDueForScrapingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConcertSource> { source });

        _mockDataStore
            .Setup(d => d.GetPreviousConcertsAsync())
            .ReturnsAsync(existingConcerts);

        _mockScraperFactory
            .Setup(f => f.GetScraperByName(It.IsAny<string>()))
            .Returns(_mockScraper.Object);

        _mockScraper
            .Setup(s => s.ScrapeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScraperResult
            {
                Success = true,
                Concerts = scrapedConcerts,
                ConcertsScraped = scrapedConcerts.Count
            });
    }

    private static ConcertSource CreateTestSource(
        string id,
        string scraperType,
        string url = "https://example.com/concerts")
    {
        return new ConcertSource
        {
            Id = id,
            Name = "HeavyMetal.dk",
            ScraperType = scraperType,
            Url = url,
            ScrapeInterval = TimeSpan.FromHours(6),
            Enabled = true
        };
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

    #endregion
}
