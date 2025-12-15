namespace MetalWatch.Tests.Scrapers;

using FluentAssertions;
using MetalWatch.Infrastructure.Scrapers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using Xunit;

public class HeavyMetalDkScraperTests
{
    private readonly Mock<ILogger<HeavyMetalDkScraper>> _mockLogger;

    public HeavyMetalDkScraperTests()
    {
        _mockLogger = new Mock<ILogger<HeavyMetalDkScraper>>();
    }

    [Fact]
    public async Task ScrapeAsync_WithSingleConcert_ParsesCorrectly()
    {
        // Arrange
        var html = LoadFixture("single-concert.html");
        var scraper = CreateScraperWithMockedHttp(html);

        // Act
        var result = await scraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Concerts.Should().HaveCount(1);

        var concert = result.Concerts[0];
        concert.Date.Should().Be(new DateTime(2025, 12, 18));
        concert.DayOfWeek.Should().Be("tor");
        concert.Artists.Should().ContainSingle().Which.Should().Be("Katatonia");
        concert.Venue.Should().Be("Amager Bio");
        concert.ConcertUrl.Should().Be("https://heavymetal.dk/koncert/katatonia-amager-bio-18-december-2025");
        concert.Id.Should().Be("katatonia-amager-bio-18-december-2025");
        concert.IsCancelled.Should().BeFalse();
        concert.IsNew.Should().BeFalse();
        concert.IsFestival.Should().BeFalse();
    }

    [Fact]
    public async Task ScrapeAsync_WithFullCalendar_ParsesMultipleConcerts()
    {
        // Arrange
        var html = LoadFixture("full-calendar-2025-12-15.html");
        var scraper = CreateScraperWithMockedHttp(html);

        // Act
        var result = await scraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Concerts.Should().HaveCount(5); // Updated fixture has 5 concerts
        result.ConcertsScraped.Should().Be(5);

        // Verify first concert (December)
        var firstConcert = result.Concerts[0];
        firstConcert.Date.Year.Should().Be(2025);
        firstConcert.Date.Month.Should().Be(12);
        firstConcert.Date.Day.Should().Be(18);

        // Verify year rollover (January concert)
        var januaryConcert = result.Concerts.FirstOrDefault(c => c.Date.Month == 1);
        januaryConcert.Should().NotBeNull();
        januaryConcert!.Date.Year.Should().Be(2026);
    }

    [Fact]
    public async Task ScrapeAsync_WithFestival_MarksAsFestival()
    {
        // Arrange
        var html = LoadFixture("festival-event.html");
        var scraper = CreateScraperWithMockedHttp(html);

        // Act
        var result = await scraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Concerts.Should().ContainSingle();

        var festival = result.Concerts[0];
        festival.IsFestival.Should().BeTrue();
        festival.Artists.Should().Contain("Udgårdsfest 2025");
        festival.Artists.Should().Contain("Einherjer");
        festival.Artists.Should().Contain("Finsterforst");
        festival.Artists.Should().Contain("Hamferð");
        festival.Artists.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task ScrapeAsync_WithCancelledConcert_MarksCancellation()
    {
        // Arrange
        var html = LoadFixture("cancelled-concert.html");
        var scraper = CreateScraperWithMockedHttp(html);

        // Act
        var result = await scraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Concerts.Should().ContainSingle();

        var concert = result.Concerts[0];
        concert.IsCancelled.Should().BeTrue();
        concert.Artists.Should().Contain("Iron Maiden");
    }

    [Fact]
    public async Task ScrapeAsync_WithNewConcert_MarksAsNew()
    {
        // Arrange
        var html = LoadFixture("new-concert.html");
        var scraper = CreateScraperWithMockedHttp(html);

        // Act
        var result = await scraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Concerts.Should().ContainSingle();

        var concert = result.Concerts[0];
        concert.IsNew.Should().BeTrue();
        concert.Artists.Should().Contain("Slayer");
        concert.Date.Should().Be(new DateTime(2026, 3, 20));
    }

    [Fact]
    public async Task ScrapeAsync_WithMultiArtistShow_IncludesAllArtists()
    {
        // Arrange
        var html = LoadFixture("full-calendar-2025-12-15.html");
        var scraper = CreateScraperWithMockedHttp(html);

        // Act
        var result = await scraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);

        // Assert
        var multiArtistShow = result.Concerts.FirstOrDefault(c => c.Artists.Count > 1 && !c.IsFestival);
        multiArtistShow.Should().NotBeNull();
        multiArtistShow!.Artists.Should().Contain("Katatonia");
        multiArtistShow.Artists.Should().Contain("Evergrey");
        multiArtistShow.Artists.Should().Contain("Klogr");
    }

    [Fact]
    public async Task ScrapeAsync_WithNetworkError_ReturnsFailureResult()
    {
        // Arrange
        var scraper = CreateScraperWithMockedHttpError();

        // Act
        var result = await scraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Network error");
        result.Concerts.Should().BeEmpty();
    }

    [Theory]
    [InlineData("https://heavymetal.dk/koncertkalender", true)]
    [InlineData("https://heavymetal.dk/koncertkalender?landsdel=koebenhavn", true)]
    [InlineData("https://www.heavymetal.dk", true)]
    [InlineData("https://billetto.dk/events", false)]
    [InlineData("https://google.com", false)]
    [InlineData("", false)]
    [InlineData(null!, false)]
    public void SupportsUrl_ValidatesUrlCorrectly(string url, bool expected)
    {
        // Arrange
        var scraper = CreateScraperWithMockedHttp("");

        // Act
        var result = scraper.SupportsUrl(url);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ScraperName_ReturnsCorrectName()
    {
        // Arrange
        var scraper = CreateScraperWithMockedHttp("");

        // Act & Assert
        scraper.ScraperName.Should().Be("HeavyMetalDk");
    }

    [Fact]
    public async Task ScrapeAsync_WithYearRollover_HandlesCorrectly()
    {
        // Arrange
        var html = LoadFixture("full-calendar-2025-12-15.html");
        var scraper = CreateScraperWithMockedHttp(html);

        // Act
        var result = await scraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);

        // Assert
        var decemberConcerts = result.Concerts.Where(c => c.Date.Month == 12).ToList();
        var januaryConcerts = result.Concerts.Where(c => c.Date.Month == 1).ToList();

        decemberConcerts.Should().NotBeEmpty();
        januaryConcerts.Should().NotBeEmpty();

        // December concerts should be in 2025
        decemberConcerts.Should().OnlyContain(c => c.Date.Year == 2025);

        // January concerts should be in 2026 (next year)
        januaryConcerts.Should().OnlyContain(c => c.Date.Year == 2026);
    }

    [Fact]
    public async Task ScrapeAsync_SetsScrapedAtTimestamp()
    {
        // Arrange
        var html = LoadFixture("single-concert.html");
        var scraper = CreateScraperWithMockedHttp(html);
        var beforeScrape = DateTime.UtcNow;

        // Act
        var result = await scraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);
        var afterScrape = DateTime.UtcNow;

        // Assert
        result.ScrapedAt.Should().BeOnOrAfter(beforeScrape).And.BeOnOrBefore(afterScrape);
        result.Concerts.Should().OnlyContain(c => c.ScrapedAt >= beforeScrape && c.ScrapedAt <= afterScrape);
    }

    // Helper methods

    private static string LoadFixture(string filename)
    {
        var fixturePath = Path.Combine("Fixtures", "HeavyMetalDk", filename);
        if (!File.Exists(fixturePath))
        {
            throw new FileNotFoundException($"Fixture file not found: {fixturePath}");
        }
        return File.ReadAllText(fixturePath, Encoding.UTF8);
    }

    private HeavyMetalDkScraper CreateScraperWithMockedHttp(string htmlResponse)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(htmlResponse, Encoding.UTF8, "text/html")
            });

        var client = new HttpClient(mockHandler.Object);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        return new HeavyMetalDkScraper(mockFactory.Object, _mockLogger.Object);
    }

    private HeavyMetalDkScraper CreateScraperWithMockedHttpError()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var client = new HttpClient(mockHandler.Object);
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        return new HeavyMetalDkScraper(mockFactory.Object, _mockLogger.Object);
    }
}
