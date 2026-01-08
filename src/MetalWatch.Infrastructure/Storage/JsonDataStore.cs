namespace MetalWatch.Infrastructure.Storage;

using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Models;
using System.Text.Json;

/// <summary>
/// JSON file-based data storage implementation
/// Suitable for local development and single-user scenarios
/// </summary>
public class JsonDataStore : IDataStore
{
    private readonly string _dataDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonDataStore(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "concert-data");
        Directory.CreateDirectory(_dataDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<List<Concert>> GetPreviousConcertsAsync()
    {
        var path = Path.Combine(_dataDirectory, "concerts.json");

        if (!File.Exists(path))
            return new List<Concert>();

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<Concert>>(json, _jsonOptions) ?? new List<Concert>();
        }
        catch (JsonException)
        {
            // If JSON is malformed, return empty list
            return new List<Concert>();
        }
    }

    public async Task SaveConcertsAsync(List<Concert> concerts)
    {
        var path = Path.Combine(_dataDirectory, "concerts.json");
        var json = JsonSerializer.Serialize(concerts, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<ConcertPreferences> GetPreferencesAsync()
    {
        var path = Path.Combine(_dataDirectory, "preferences.json");

        if (!File.Exists(path))
            return new ConcertPreferences();

        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<ConcertPreferences>(json, _jsonOptions)
                ?? new ConcertPreferences();
        }
        catch (JsonException)
        {
            // If JSON is malformed, return default
            return new ConcertPreferences();
        }
    }

    public async Task SavePreferencesAsync(ConcertPreferences preferences)
    {
        var path = Path.Combine(_dataDirectory, "preferences.json");
        var json = JsonSerializer.Serialize(preferences, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    /// <inheritdoc />
    public Task<List<ConcertSource>> GetSourcesDueForScrapingAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Implement source retrieval logic
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task UpdateSourceScrapedAsync(
        string sourceId,
        DateTime scrapedAt,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement scrape status update logic
        throw new NotImplementedException();
    }
}
