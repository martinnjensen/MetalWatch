namespace MetalWatch.Infrastructure.Scrapers;

using MetalWatch.Core.Interfaces;

/// <summary>
/// Factory for creating and selecting concert scrapers
/// Implements Strategy pattern - auto-selects scraper based on URL
/// </summary>
public class ScraperFactory : IScraperFactory
{
    private readonly IEnumerable<IConcertScraper> _scrapers;

    public ScraperFactory(IEnumerable<IConcertScraper> scrapers)
    {
        _scrapers = scrapers ?? throw new ArgumentNullException(nameof(scrapers));
    }

    public IConcertScraper GetScraper(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty", nameof(url));

        var scraper = _scrapers.FirstOrDefault(s => s.SupportsUrl(url));

        if (scraper == null)
            throw new NotSupportedException($"No scraper found for URL: {url}");

        return scraper;
    }

    public IConcertScraper GetScraperByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scraper name cannot be null or empty", nameof(name));

        var scraper = _scrapers.FirstOrDefault(s =>
            s.ScraperName.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
            throw new ArgumentException($"Scraper not found: {name}", nameof(name));

        return scraper;
    }

    public IEnumerable<IConcertScraper> GetAllScrapers()
    {
        return _scrapers;
    }
}
