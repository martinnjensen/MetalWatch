namespace MetalWatch.Core.Interfaces;

using MetalWatch.Core.Models;

/// <summary>
/// Interface for data storage operations
/// Abstracts storage implementation (JSON files, S3, database, etc.)
/// </summary>
public interface IDataStore
{
    /// <summary>
    /// Retrieves the list of previously scraped concerts
    /// Used to identify new concerts since last check
    /// </summary>
    /// <returns>List of previously stored concerts</returns>
    Task<List<Concert>> GetPreviousConcertsAsync();

    /// <summary>
    /// Saves the current list of concerts
    /// Overwrites previous concert data
    /// </summary>
    /// <param name="concerts">Concerts to save</param>
    Task SaveConcertsAsync(List<Concert> concerts);

    /// <summary>
    /// Retrieves user preferences
    /// </summary>
    /// <returns>Stored preferences or default if none exist</returns>
    Task<ConcertPreferences> GetPreferencesAsync();

    /// <summary>
    /// Saves user preferences
    /// </summary>
    /// <param name="preferences">Preferences to save</param>
    Task SavePreferencesAsync(ConcertPreferences preferences);
}
