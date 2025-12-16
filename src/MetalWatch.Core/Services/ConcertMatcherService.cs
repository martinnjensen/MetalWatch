namespace MetalWatch.Core.Services;

using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Models;

/// <summary>
/// Service for matching concerts against user preferences
/// Implements scoring algorithm to rank concert relevance
/// </summary>
public class ConcertMatcherService : IConcertMatcher
{
    /// <summary>
    /// Finds concerts that match user preferences
    /// Filters out cancelled concerts and those outside date range
    /// Returns concerts sorted by relevance score (highest first)
    /// </summary>
    public List<Concert> FindMatches(List<Concert> concerts, ConcertPreferences preferences)
    {
        if (concerts == null || !concerts.Any())
            return new List<Concert>();

        if (preferences == null)
            return concerts;

        return concerts
            .Where(c => !c.IsCancelled) // Filter out cancelled concerts
            .Where(c => IsWithinDateRange(c, preferences))
            .Select(c => new { Concert = c, Score = CalculateRelevanceScore(c, preferences) })
            .Where(x => x.Score > 0) // Only include concerts with some relevance
            .OrderByDescending(x => x.Score) // Highest score first
            .ThenBy(x => x.Concert.Date) // Then by date (earliest first)
            .Select(x => x.Concert)
            .ToList();
    }

    /// <summary>
    /// Calculates relevance score for a concert
    /// Scoring: Exact artist match +100, Favorite venue +50, Keyword match +25 each
    /// </summary>
    public int CalculateRelevanceScore(Concert concert, ConcertPreferences preferences)
    {
        if (concert == null || preferences == null)
            return 0;

        int score = 0;

        // Exact artist match: +100 points per matched artist
        if (preferences.FavoriteArtists != null && preferences.FavoriteArtists.Any())
        {
            foreach (var favoriteArtist in preferences.FavoriteArtists)
            {
                if (concert.Artists.Any(a => a.Equals(favoriteArtist, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 100;
                }
            }
        }

        // Favorite venue: +50 points
        if (preferences.FavoriteVenues != null && preferences.FavoriteVenues.Any())
        {
            if (preferences.FavoriteVenues.Any(v => v.Equals(concert.Venue, StringComparison.OrdinalIgnoreCase)))
            {
                score += 50;
            }
        }

        // Keyword match in artist names: +25 points per keyword
        if (preferences.Keywords != null && preferences.Keywords.Any())
        {
            foreach (var keyword in preferences.Keywords)
            {
                if (concert.Artists.Any(a => a.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                {
                    score += 25;
                }
            }
        }

        return score;
    }

    private static bool IsWithinDateRange(Concert concert, ConcertPreferences preferences)
    {
        // Check start date
        if (preferences.StartDate.HasValue && concert.Date < preferences.StartDate.Value)
            return false;

        // Check end date
        if (preferences.EndDate.HasValue && concert.Date > preferences.EndDate.Value)
            return false;

        return true;
    }
}
