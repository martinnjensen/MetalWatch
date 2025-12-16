namespace MetalWatch.Core.Models;

/// <summary>
/// Result object returned from notification operations
/// </summary>
public class NotificationResult
{
    /// <summary>
    /// Indicates if the notification was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result or error
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Number of concerts included in the notification
    /// </summary>
    public int ConcertsNotified { get; set; }

    /// <summary>
    /// Timestamp when notification was sent
    /// </summary>
    public DateTime SentAt { get; set; }
}
