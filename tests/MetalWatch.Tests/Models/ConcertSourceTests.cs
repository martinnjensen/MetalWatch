namespace MetalWatch.Tests.Models;

using FluentAssertions;
using MetalWatch.Core.Models;
using Xunit;

public class ConcertSourceTests
{
    [Fact]
    public void ScrapeInterval_DefaultsTo24Hours()
    {
        // Arrange & Act
        var source = new ConcertSource
        {
            Id = "test-source",
            Name = "Test Source",
            ScraperType = "TestScraper",
            Url = "https://example.com"
        };

        // Assert
        source.ScrapeInterval.Should().Be(TimeSpan.FromHours(24));
    }
}
