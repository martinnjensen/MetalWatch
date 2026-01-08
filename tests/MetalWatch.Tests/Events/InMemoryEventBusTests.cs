namespace MetalWatch.Tests.Events;

using FluentAssertions;
using MetalWatch.Core.Events;
using MetalWatch.Core.Models;
using MetalWatch.Infrastructure.Events;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class InMemoryEventBusTests
{
    private readonly Mock<ILogger<InMemoryEventBus>> _mockLogger;
    private readonly InMemoryEventBus _eventBus;

    public InMemoryEventBusTests()
    {
        _mockLogger = new Mock<ILogger<InMemoryEventBus>>();
        _eventBus = new InMemoryEventBus(_mockLogger.Object);
    }

    [Fact]
    public async Task PublishAsync_WithSubscribedHandler_InvokesHandler()
    {
        // Arrange
        var receivedEvent = (TestEvent?)null;
        _eventBus.Subscribe<TestEvent>(async (evt, ct) =>
        {
            receivedEvent = evt;
            await Task.CompletedTask;
        });

        var testEvent = new TestEvent { Message = "Hello, World!" };

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.Message.Should().Be("Hello, World!");
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_InvokesAllHandlers()
    {
        // Arrange
        var invocationCount = 0;
        _eventBus.Subscribe<TestEvent>(async (evt, ct) =>
        {
            Interlocked.Increment(ref invocationCount);
            await Task.CompletedTask;
        });
        _eventBus.Subscribe<TestEvent>(async (evt, ct) =>
        {
            Interlocked.Increment(ref invocationCount);
            await Task.CompletedTask;
        });
        _eventBus.Subscribe<TestEvent>(async (evt, ct) =>
        {
            Interlocked.Increment(ref invocationCount);
            await Task.CompletedTask;
        });

        var testEvent = new TestEvent { Message = "Test" };

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        invocationCount.Should().Be(3);
    }

    [Fact]
    public async Task PublishAsync_WithNoHandlers_CompletesSuccessfully()
    {
        // Arrange
        var testEvent = new TestEvent { Message = "No handlers" };

        // Act
        var act = async () => await _eventBus.PublishAsync(testEvent);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithDifferentEventTypes_OnlyInvokesMatchingHandlers()
    {
        // Arrange
        var testEventReceived = false;
        var otherEventReceived = false;

        _eventBus.Subscribe<TestEvent>(async (evt, ct) =>
        {
            testEventReceived = true;
            await Task.CompletedTask;
        });
        _eventBus.Subscribe<OtherTestEvent>(async (evt, ct) =>
        {
            otherEventReceived = true;
            await Task.CompletedTask;
        });

        var testEvent = new TestEvent { Message = "Test" };

        // Act
        await _eventBus.PublishAsync(testEvent);

        // Assert
        testEventReceived.Should().BeTrue();
        otherEventReceived.Should().BeFalse();
    }

    [Fact]
    public async Task PublishAsync_PassesCancellationTokenToHandler()
    {
        // Arrange
        var receivedCancellationToken = CancellationToken.None;
        _eventBus.Subscribe<TestEvent>(async (evt, ct) =>
        {
            receivedCancellationToken = ct;
            await Task.CompletedTask;
        });

        var testEvent = new TestEvent { Message = "Test" };
        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;

        // Act
        await _eventBus.PublishAsync(testEvent, expectedToken);

        // Assert
        receivedCancellationToken.Should().Be(expectedToken);
    }

    [Fact]
    public async Task PublishAsync_WithConcertsScrapedEvent_InvokesHandler()
    {
        // Arrange
        var receivedEvent = (ConcertsScrapedEvent?)null;
        _eventBus.Subscribe<ConcertsScrapedEvent>(async (evt, ct) =>
        {
            receivedEvent = evt;
            await Task.CompletedTask;
        });

        var concerts = new List<Concert>
        {
            new Concert
            {
                Id = "test-concert-1",
                DayOfWeek = "man",
                Venue = "Test Venue",
                ConcertUrl = "https://example.com/concert",
                Artists = new List<string> { "Test Artist" },
                Date = new DateTime(2025, 12, 15)
            }
        };

        var scrapedEvent = new ConcertsScrapedEvent
        {
            SourceUrl = "https://heavymetal.dk/koncertkalender",
            ScrapedConcerts = concerts,
            ScrapedAt = DateTime.UtcNow
        };

        // Act
        await _eventBus.PublishAsync(scrapedEvent);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.SourceUrl.Should().Be("https://heavymetal.dk/koncertkalender");
        receivedEvent.ScrapedConcerts.Should().HaveCount(1);
        receivedEvent.ScrapedConcerts[0].Id.Should().Be("test-concert-1");
    }

    [Fact]
    public async Task PublishAsync_WithNewConcertsFoundEvent_InvokesHandler()
    {
        // Arrange
        var receivedEvent = (NewConcertsFoundEvent?)null;
        _eventBus.Subscribe<NewConcertsFoundEvent>(async (evt, ct) =>
        {
            receivedEvent = evt;
            await Task.CompletedTask;
        });

        var concerts = new List<Concert>
        {
            new Concert
            {
                Id = "new-concert-1",
                DayOfWeek = "fre",
                Venue = "New Venue",
                ConcertUrl = "https://example.com/new-concert",
                Artists = new List<string> { "New Artist" },
                Date = new DateTime(2025, 12, 20)
            }
        };

        var newConcertsEvent = new NewConcertsFoundEvent
        {
            NewConcerts = concerts,
            SourceUrl = "https://heavymetal.dk/koncertkalender",
            FoundAt = DateTime.UtcNow
        };

        // Act
        await _eventBus.PublishAsync(newConcertsEvent);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.NewConcerts.Should().HaveCount(1);
        receivedEvent.SourceUrl.Should().Be("https://heavymetal.dk/koncertkalender");
    }

    [Fact]
    public async Task PublishAsync_WithHandlerException_PropagatesException()
    {
        // Arrange
        _eventBus.Subscribe<TestEvent>(async (evt, ct) =>
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Handler failed");
        });

        var testEvent = new TestEvent { Message = "Test" };

        // Act
        var act = async () => await _eventBus.PublishAsync(testEvent);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Handler failed");
    }

    [Fact]
    public async Task PublishAsync_AfterMultipleSubscriptions_MaintainsAllHandlers()
    {
        // Arrange
        var messages = new List<string>();

        _eventBus.Subscribe<TestEvent>(async (evt, ct) =>
        {
            messages.Add("Handler1");
            await Task.CompletedTask;
        });

        // Publish first event
        await _eventBus.PublishAsync(new TestEvent { Message = "First" });

        // Subscribe another handler
        _eventBus.Subscribe<TestEvent>(async (evt, ct) =>
        {
            messages.Add("Handler2");
            await Task.CompletedTask;
        });

        messages.Clear();

        // Act - publish second event
        await _eventBus.PublishAsync(new TestEvent { Message = "Second" });

        // Assert - both handlers should be invoked
        messages.Should().HaveCount(2);
        messages.Should().Contain("Handler1");
        messages.Should().Contain("Handler2");
    }

    // Test event classes for testing purposes
    private class TestEvent : IDomainEvent
    {
        public required string Message { get; init; }
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
    }

    private class OtherTestEvent : IDomainEvent
    {
        public required int Value { get; init; }
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
    }
}
