namespace MetalWatch.Core.Models;

/// <summary>
/// User preferences for concert matching and filtering
/// </summary>
public class ConcertPreferences
{
    /// <summary>
    /// List of favorite artist names for matching
    /// </summary>
    public List<string> FavoriteArtists { get; set; } = new();

    /// <summary>
    /// List of favorite venue names for matching
    /// </summary>
    public List<string> FavoriteVenues { get; set; } = new();

    /// <summary>
    /// Keywords to search for in artist names (e.g., "thrash", "death metal")
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// Optional start date filter - only concerts on or after this date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Optional end date filter - only concerts on or before this date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Email address for notifications
    /// </summary>
    public string? NotificationEmail { get; set; }
}
