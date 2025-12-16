namespace MetalWatch.Infrastructure.Scrapers;

using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Models;
using System.Text.RegularExpressions;

/// <summary>
/// Scraper implementation for heavymetal.dk concert calendar
/// Parses table-based HTML structure with monthly groupings
/// Each concert is a table row with date, artists, venue, and info link
/// </summary>
public partial class HeavyMetalDkScraper : IConcertScraper
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HeavyMetalDkScraper> _logger;

    // Regex for matching date patterns like "15/12 (man)"
    [GeneratedRegex(@"(\d{1,2})/(\d{1,2})\s*\((\w+)\)", RegexOptions.Compiled)]
    private static partial Regex DatePatternRegex();

    // Regex for matching month headers like "December 2025"
    [GeneratedRegex(@"(Januar|Februar|Marts|April|Maj|Juni|Juli|August|September|Oktober|November|December)\s+(\d{4})", RegexOptions.IgnoreCase)]
    private static partial Regex MonthHeaderRegex();

    public HeavyMetalDkScraper(IHttpClientFactory httpClientFactory, ILogger<HeavyMetalDkScraper> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ScraperName => "HeavyMetalDk";

    public bool SupportsUrl(string url)
    {
        return !string.IsNullOrWhiteSpace(url) &&
               url.Contains("heavymetal.dk", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ScraperResult> ScrapeAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting scraping of {Url}", url);

            var html = await DownloadHtmlAsync(url, cancellationToken);
            var concerts = ParseHtml(html);

            _logger.LogInformation("Successfully scraped {Count} concerts", concerts.Count);

            return new ScraperResult
            {
                Success = true,
                Concerts = concerts,
                ConcertsScraped = concerts.Count,
                ScrapedAt = DateTime.UtcNow
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while scraping {Url}", url);
            return new ScraperResult
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}",
                ScrapedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while scraping {Url}", url);
            return new ScraperResult
            {
                Success = false,
                ErrorMessage = $"Scraping failed: {ex.Message}",
                ScrapedAt = DateTime.UtcNow
            };
        }
    }

    private async Task<string> DownloadHtmlAsync(string url, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "MetalWatch/1.0 (Concert Calendar Tracker)");
        client.Timeout = TimeSpan.FromSeconds(30);

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private List<Concert> ParseHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var concerts = new List<Concert>();

        // Find all month headers (h2 elements like "December 2025")
        var monthHeaders = doc.DocumentNode.SelectNodes("//h2");

        if (monthHeaders == null || !monthHeaders.Any())
        {
            _logger.LogWarning("No month headers found in HTML");
            return concerts;
        }

        foreach (var monthHeader in monthHeaders)
        {
            var monthText = monthHeader.InnerText.Trim();
            var monthMatch = MonthHeaderRegex().Match(monthText);

            if (!monthMatch.Success)
                continue;

            int currentMonth = ParseDanishMonth(monthMatch.Groups[1].Value);
            int currentYear = int.Parse(monthMatch.Groups[2].Value);

            _logger.LogDebug("Processing month: {Month} {Year}", monthMatch.Groups[1].Value, currentYear);

            // Find the next table after this header
            // Use contains() instead of exact match because there may be additional classes
            var table = monthHeader.SelectSingleNode("following-sibling::table[contains(@class, 'event-table')]");

            if (table == null)
            {
                _logger.LogWarning("No table found after month header {Month} {Year}", monthMatch.Groups[1].Value, currentYear);
                continue;
            }

            // Process each row in the table
            var rows = table.SelectNodes(".//tr[@itemprop='event']");

            if (rows == null || !rows.Any())
            {
                _logger.LogDebug("No event rows found for {Month} {Year}", monthMatch.Groups[1].Value, currentYear);
                continue;
            }

            foreach (var row in rows)
            {
                try
                {
                    var concert = ParseConcertRow(row, currentYear);
                    if (concert != null && IsValidConcert(concert))
                    {
                        concerts.Add(concert);
                        _logger.LogDebug("Added concert: {Id}", concert.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse concert row");
                }
            }
        }

        return concerts;
    }

    private Concert? ParseConcertRow(HtmlNode row, int yearContext)
    {
        // Extract date from meta tag and text
        var dateMetaNode = row.SelectSingleNode(".//meta[@itemprop='startDate']");
        var dateCellNode = row.SelectSingleNode(".//td[@class='event-date']");

        if (dateMetaNode == null || dateCellNode == null)
        {
            _logger.LogWarning("Date information missing in concert row");
            return null;
        }

        var isoDate = dateMetaNode.GetAttributeValue("content", "");
        if (!DateTime.TryParse(isoDate, out var concertDate))
        {
            _logger.LogWarning("Failed to parse ISO date: {Date}", isoDate);
            return null;
        }

        // Extract day of week from text like "15/12 (man)"
        var dateText = dateCellNode.InnerText.Trim();
        var dateMatch = DatePatternRegex().Match(dateText);
        var dayOfWeek = dateMatch.Success ? dateMatch.Groups[3].Value : "";

        // Check for "Aflyst" (cancelled) and "Ny" (new) markers in date cell
        var isCancelled = dateText.Contains("Aflyst", StringComparison.OrdinalIgnoreCase);
        var isNew = dateText.Contains("Ny", StringComparison.Ordinal);

        // Extract venue (required)
        var venue = "";
        var venueLink = row.SelectSingleNode(".//td[@class='event-venue']//a[@itemprop='url']");
        if (venueLink != null)
        {
            // Venue text includes location like "Pumpehuset, København V"
            // We'll extract just the venue name (before the comma)
            var venueText = venueLink.InnerText.Trim();
            var commaIndex = venueText.IndexOf(',');
            venue = commaIndex > 0 ? venueText.Substring(0, commaIndex).Trim() : venueText;
        }

        // Extract concert info URL and ID (required)
        var concertUrl = "";
        var concertId = "";
        var infoLink = row.SelectSingleNode(".//td[@class='event-meta']//a");
        if (infoLink != null)
        {
            var href = infoLink.GetAttributeValue("href", "");
            if (!string.IsNullOrWhiteSpace(href))
            {
                concertUrl = href.StartsWith("http") ? href : $"https://heavymetal.dk{href}";
                concertId = ExtractIdFromUrl(href);
            }
        }

        var concert = new Concert
        {
            Id = concertId,
            Date = concertDate,
            DayOfWeek = dayOfWeek,
            Venue = venue,
            ConcertUrl = concertUrl,
            IsCancelled = isCancelled,
            IsNew = isNew,
            ScrapedAt = DateTime.UtcNow
        };

        // Extract artists
        var artistsSpan = row.SelectSingleNode(".//span[@itemprop='name' and @class='artists']");
        if (artistsSpan != null)
        {
            // Check for festival (first link in <strong> tag)
            var festivalLink = artistsSpan.SelectSingleNode(".//strong/a");
            if (festivalLink != null)
            {
                concert.IsFestival = true;

                // Extract festival name and add it as first "artist"
                var festivalName = festivalLink.InnerText.Trim();
                if (!string.IsNullOrWhiteSpace(festivalName))
                {
                    concert.Artists.Add(festivalName);
                }
            }

            // Extract all artist links (including those after the festival name)
            var artistLinks = artistsSpan.SelectNodes(".//a[contains(@href, '/artist/')]");
            if (artistLinks != null)
            {
                foreach (var artistLink in artistLinks)
                {
                    var artistName = artistLink.InnerText.Trim();
                    if (!string.IsNullOrWhiteSpace(artistName))
                    {
                        concert.Artists.Add(artistName);
                    }
                }
            }
        }

        return concert;
    }

    private static string ExtractIdFromUrl(string href)
    {
        // Extract slug from "/koncert/metallica-pumpehuset-2025-12-15" -> "metallica-pumpehuset-2025-12-15"
        return href.Replace("/koncert/", "").Trim('/');
    }

    private static bool IsValidConcert(Concert concert)
    {
        // A valid concert must have at least one artist, a venue, and a concert URL
        return concert.Artists.Any() &&
               !string.IsNullOrWhiteSpace(concert.Venue) &&
               !string.IsNullOrWhiteSpace(concert.ConcertUrl);
    }

    private static int ParseDanishMonth(string danishMonth)
    {
        return danishMonth.ToLowerInvariant() switch
        {
            "januar" => 1,
            "februar" => 2,
            "marts" => 3,
            "april" => 4,
            "maj" => 5,
            "juni" => 6,
            "juli" => 7,
            "august" => 8,
            "september" => 9,
            "oktober" => 10,
            "november" => 11,
            "december" => 12,
            _ => DateTime.Now.Month // Fallback to current month
        };
    }
}
