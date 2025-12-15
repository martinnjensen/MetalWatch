namespace MetalWatch.Core.Interfaces;

using MetalWatch.Core.Models;

/// <summary>
/// Interface for concert matching and scoring logic
/// </summary>
public interface IConcertMatcher
{
    /// <summary>
    /// Finds concerts that match user preferences
    /// Filters out cancelled concerts and those outside date range
    /// Returns concerts sorted by relevance score (highest first)
    /// </summary>
    /// <param name="concerts">List of concerts to filter</param>
    /// <param name="preferences">User preferences</param>
    /// <returns>Filtered and sorted list of matching concerts</returns>
    List<Concert> FindMatches(List<Concert> concerts, ConcertPreferences preferences);

    /// <summary>
    /// Calculates a relevance score for a concert based on user preferences
    /// Scoring: Exact artist match +100, Favorite venue +50, Keyword match +25 each
    /// </summary>
    /// <param name="concert">Concert to score</param>
    /// <param name="preferences">User preferences</param>
    /// <returns>Relevance score (0 = no match)</returns>
    int CalculateRelevanceScore(Concert concert, ConcertPreferences preferences);
}
