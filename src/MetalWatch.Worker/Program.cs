using MetalWatch.Core.Events;
using MetalWatch.Core.Interfaces;
using MetalWatch.Core.Services;
using MetalWatch.Infrastructure.Events;
using MetalWatch.Infrastructure.Notifications;
using MetalWatch.Infrastructure.Scrapers;
using MetalWatch.Infrastructure.Storage;
using MetalWatch.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.SetMinimumLevel(builder.Environment.IsDevelopment()
        ? LogLevel.Debug
        : LogLevel.Information);
});

// HttpClient for scrapers
builder.Services.AddHttpClient();

// Register individual scrapers
builder.Services.AddSingleton<IConcertScraper, HeavyMetalDkScraper>();

// Core services
builder.Services.AddSingleton<IScraperFactory, ScraperFactory>();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddSingleton<IConcertMatcher, ConcertMatcherService>();
builder.Services.AddScoped<IConcertOrchestrationService, ConcertOrchestrationService>();

// Data store - environment-specific
if (builder.Environment.IsDevelopment())
{
    // Local development: in-memory storage with heavymetal.dk pre-configured
    builder.Services.AddSingleton<IDataStore, InMemoryDataStore>();
}
else
{
    // Production: JSON file storage
    builder.Services.AddSingleton<IDataStore, JsonDataStore>();
}

// Notification services - environment-specific
if (builder.Environment.IsDevelopment())
{
    // Local development: console output
    builder.Services.AddSingleton<INotificationService, ConsoleNotificationService>();
}
else
{
    // Production: console for now, will add email later
    builder.Services.AddSingleton<INotificationService, ConsoleNotificationService>();
}

// Event handlers
// NotificationEventHandler will auto-subscribe via constructor that takes IEventBus
builder.Services.AddSingleton<NotificationEventHandler>();

// Hosted service
builder.Services.AddHostedService<ConcertScrapingHostedService>();

var host = builder.Build();

// Trigger creation of event handler to ensure subscription
_ = host.Services.GetRequiredService<NotificationEventHandler>();

host.Run();
