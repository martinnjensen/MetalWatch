namespace MetalWatch.Tests.Integration;

using FluentAssertions;
using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Models;
using MetalWatch.Core.Services;
using MetalWatch.Infrastructure.Scrapers;
using MetalWatch.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using Xunit;

/// <summary>
/// Integration tests demonstrating the complete scraping workflow
/// Tests the interaction between scraper, matcher, and storage components
/// </summary>
public class ScraperIntegrationTests : IDisposable
{
    private readonly string _testDataDirectory;

    public ScraperIntegrationTests()
    {
        // Create unique test directory for each test run
        _testDataDirectory = Path.Combine(Path.GetTempPath(), $"metalwatch-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataDirectory);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDataDirectory))
        {
            Directory.Delete(_testDataDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task EndToEndWorkflow_ScrapeMatchAndStore_WorksCorrectly()
    {
        // Arrange
        var html = LoadFixture("full-calendar-2025-12-15.html");
        var scraper = CreateScraperWithMockedHttp(html);
        var matcher = new ConcertMatcherService();
        var dataStore = new JsonDataStore(_testDataDirectory);

        var preferences = new ConcertPreferences
        {
            FavoriteArtists = new List<string> { "Katatonia", "Einherjer" },
            FavoriteVenues = new List<string> { "Pumpehuset" },
            Keywords = new List<string> { "black" },
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2026, 12, 31)
        };

        // Act - Step 1: Scrape concerts
        var scrapeResult = await scraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);

        // Assert scraping worked
        scrapeResult.Success.Should().BeTrue();
        scrapeResult.Concerts.Should().HaveCountGreaterThan(0);

        // Act - Step 2: Match concerts against preferences
        var matches = matcher.FindMatches(scrapeResult.Concerts, preferences);

        // Assert matching worked
        matches.Should().NotBeEmpty();
        matches.Should().OnlyContain(c => !c.IsCancelled); // No cancelled concerts

        // Verify sorted by relevance score (descending)
        var scores = matches.Select(c => matcher.CalculateRelevanceScore(c, preferences)).ToList();
        scores.Should().BeInDescendingOrder(); // Sorted by score

        // Act - Step 3: Save to storage
        await dataStore.SaveConcertsAsync(scrapeResult.Concerts);
        await dataStore.SavePreferencesAsync(preferences);

        // Assert storage worked
        var storedConcerts = await dataStore.GetPreviousConcertsAsync();
        var storedPreferences = await dataStore.GetPreferencesAsync();

        storedConcerts.Should().HaveCount(scrapeResult.Concerts.Count);
        storedPreferences.FavoriteArtists.Should().BeEquivalentTo(preferences.FavoriteArtists);

        // Act - Step 4: Identify new concerts on subsequent scrape
        var newConcertsOnly = scrapeResult.Concerts
            .Where(c => !storedConcerts.Any(s => s.Id == c.Id))
            .ToList();

        // Since we're scraping the same data, there should be no new concerts
        newConcertsOnly.Should().BeEmpty();
    }

    [Fact]
    public async Task ScraperFactory_AutoSelectsScraper_BasedOnUrl()
    {
        // Arrange
        var html = LoadFixture("single-concert.html");
        var scraper = CreateScraperWithMockedHttp(html);
        var factory = new ScraperFactory(new[] { scraper });

        // Act
        var selectedScraper = factory.GetScraper("https://heavymetal.dk/koncertkalender");

        // Assert
        selectedScraper.Should().NotBeNull();
        selectedScraper.ScraperName.Should().Be("HeavyMetalDk");

        var result = await selectedScraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ConcertMatcher_ScoresCorrectly_BasedOnPreferences()
    {
        // Arrange
        var html = LoadFixture("full-calendar-2025-12-15.html");
        var scraper = CreateScraperWithMockedHttp(html);
        var matcher = new ConcertMatcherService();

        var preferences = new ConcertPreferences
        {
            FavoriteArtists = new List<string> { "Einherjer" }, // +100
            FavoriteVenues = new List<string> { "Pumpehuset" }, // +50
            Keywords = new List<string>() // No keywords
        };

        // Act
        var scrapeResult = await scraper.ScrapeAsync("https://heavymetal.dk/koncertkalender", CancellationToken.None);
        var festivalConcert = scrapeResult.Concerts.FirstOrDefault(c => c.Artists.Contains("Einherjer"));

        // Assert
        festivalConcert.Should().NotBeNull();

        var score = matcher.CalculateRelevanceScore(festivalConcert!, preferences);

        // Einherjer (favorite artist) + Pumpehuset (favorite venue) = 150 points
        score.Should().Be(150);
    }

    [Fact]
    public async Task JsonDataStore_PersistsAndRetrievesData_Correctly()
    {
        // Arrange
        var dataStore = new JsonDataStore(_testDataDirectory);

        var concerts = new List<Concert>
        {
            new Concert
            {
                Id = "test-concert-1",
                Date = DateTime.Now.AddDays(7),
                DayOfWeek = "man",
                Artists = new List<string> { "Test Band" },
                Venue = "Test Venue",
                ConcertUrl = "https://example.com/concert/test",
                ScrapedAt = DateTime.UtcNow
            }
        };

        var preferences = new ConcertPreferences
        {
            FavoriteArtists = new List<string> { "Metallica" },
            FavoriteVenues = new List<string> { "Pumpehuset" }
        };

        // Act
        await dataStore.SaveConcertsAsync(concerts);
        await dataStore.SavePreferencesAsync(preferences);

        var retrievedConcerts = await dataStore.GetPreviousConcertsAsync();
        var retrievedPreferences = await dataStore.GetPreferencesAsync();

        // Assert
        retrievedConcerts.Should().HaveCount(1);
        retrievedConcerts[0].Id.Should().Be("test-concert-1");
        retrievedConcerts[0].Artists.Should().Contain("Test Band");

        retrievedPreferences.FavoriteArtists.Should().Contain("Metallica");
        retrievedPreferences.FavoriteVenues.Should().Contain("Pumpehuset");
    }

    [Fact]
    public void ScraperFactory_ThrowsException_WhenNoScraperSupportsUrl()
    {
        // Arrange
        var scraper = CreateScraperWithMockedHttp("");
        var factory = new ScraperFactory(new[] { scraper });

        // Act & Assert
        var act = () => factory.GetScraper("https://unsupported-site.com");
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*No scraper found*");
    }

    [Fact]
    public void ScraperFactory_ThrowsException_WhenScraperNameNotFound()
    {
        // Arrange
        var scraper = CreateScraperWithMockedHttp("");
        var factory = new ScraperFactory(new[] { scraper });

        // Act & Assert
        var act = () => factory.GetScraperByName("NonExistentScraper");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Scraper not found*");
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

    private static HeavyMetalDkScraper CreateScraperWithMockedHttp(string htmlResponse)
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

        var mockLogger = new Mock<ILogger<HeavyMetalDkScraper>>();

        return new HeavyMetalDkScraper(mockFactory.Object, mockLogger.Object);
    }
}
