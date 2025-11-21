# Messaging & Integration - QuietMatch

> **Event-driven architecture and inter-service communication**
>
> This document defines how QuietMatch microservices communicate asynchronously via events and commands.

---

## Table of Contents

- [Communication Patterns](#communication-patterns)
- [Message Broker Strategy](#message-broker-strategy)
- [Event Design](#event-design)
- [Command Design](#command-design)
- [Message Abstraction Layer](#message-abstraction-layer)
- [Outbox Pattern](#outbox-pattern)
- [Message Routing](#message-routing)
- [Error Handling](#error-handling)
- [Testing Messaging](#testing-messaging)

---

## Communication Patterns

QuietMatch uses **three communication patterns**:

### 1. Synchronous (REST/gRPC)
**When**: Immediate response needed, query operations

**Examples**:
- Client queries profile: `GET /api/v1/profiles/{id}`
- MatchingService calls ProfileService via gRPC: `GetProfile(memberId)`

**Protocol**: REST (external), gRPC (internal)

---

### 2. Asynchronous (Event-Driven)
**When**: Fire-and-forget, multiple subscribers, eventual consistency

**Examples**:
- User accepts match → `MatchAccepted` event
- Subscribers: SchedulingService (create date), NotificationService (send email), AnalyticsService (track)

**Protocol**: Message broker (RabbitMQ/Azure Service Bus)

---

### 3. Request-Reply (Commands)
**When**: Asynchronous operation with expected response

**Examples**:
- SAGA: Reserve availability slot → `ReserveAvailabilitySlot` command → `SlotReserved` reply

**Protocol**: Message broker with correlation IDs

---

## Message Broker Strategy

### Local Development: RabbitMQ

**Why RabbitMQ?**
- ✅ Easy Docker setup
- ✅ Mature, well-documented
- ✅ Management UI (http://localhost:15672)
- ✅ Supports all patterns (pub/sub, queues, exchanges)

**Setup**:
```yaml
# docker-compose.yml
rabbitmq:
  image: rabbitmq:3.13-management-alpine
  ports:
    - "5672:5672"   # AMQP
    - "15672:15672" # Management UI
```

---

### Azure Production: Azure Service Bus

**Why Azure Service Bus?**
- ✅ Fully managed (no infrastructure)
- ✅ FIFO guarantees (sessions)
- ✅ Dead letter queues
- ✅ Duplicate detection
- ✅ Azure integration (Managed Identity)

**Migration**: Seamless via MassTransit abstraction

---

### Abstraction: MassTransit

**Why MassTransit?**
- ✅ Abstracts RabbitMQ and Azure Service Bus
- ✅ Built-in SAGA support
- ✅ Automatic retry logic
- ✅ Convention-based routing
- ✅ Observability (OpenTelemetry)

**Alternatives Considered**:
- **NServiceBus**: Excellent, but commercial ($$$)
- **CAP**: Good for outbox, less mature than MassTransit
- **Raw RabbitMQ Client**: No abstraction, hard to migrate

**Decision**: MassTransit (open-source, industry-standard)

---

## Event Design

### Naming Convention

**Pattern**: `{Aggregate}{ActionPastTense}`

**Examples**:
- `MatchAccepted` (not `AcceptMatch`)
- `ProfileUpdated` (not `UpdateProfile`)
- `BlindDateScheduled` (not `ScheduleBlindDate`)

**Why Past Tense?** Events represent something that **happened** (facts).

---

### Event Structure

```csharp
// Domain event (internal to service)
public record ProfileUpdated(
    MemberId MemberId,
    PreferenceSet NewPreferences,
    DateTime UpdatedAt) : DomainEvent;

// Integration event (cross-service)
public record ProfileUpdatedIntegrationEvent
{
    public Guid MemberId { get; init; }
    public Dictionary<string, object> ChangedFields { get; init; }
    public DateTime UpdatedAt { get; init; }
}
```

**Best Practices**:
- ✅ Immutable (use `record`)
- ✅ Include timestamp
- ✅ Include correlation ID (for tracing)
- ✅ Minimal payload (send IDs, not full objects)
- ✅ Versioned (future: add version field for schema evolution)

---

### Event Examples

#### MatchAccepted
```csharp
public record MatchAccepted
{
    public Guid MatchId { get; init; }
    public Guid InitiatorId { get; init; }
    public Guid PartnerId { get; init; }
    public DateTime AcceptedAt { get; init; }
    public Guid CorrelationId { get; init; } // For tracing
}
```

#### ProfileCompleted
```csharp
public record ProfileCompleted
{
    public Guid MemberId { get; init; }
    public DateTime CompletedAt { get; init; }
    public bool HasPersonalityProfile { get; init; }
    public bool HasPreferences { get; init; }
}
```

---

## Command Design

### Naming Convention

**Pattern**: `{Verb}{Aggregate}`

**Examples**:
- `ReserveAvailabilitySlot`
- `SendNotification`
- `CreateBlindDate`

**Why Imperative?** Commands are **instructions** (do something).

---

### Command Structure

```csharp
public record ReserveAvailabilitySlot
{
    public Guid MemberId1 { get; init; }
    public Guid MemberId2 { get; init; }
    public DateTime PreferredStartTime { get; init; }
    public VenueType VenueType { get; init; }
    public Guid CorrelationId { get; init; } // SAGA correlation
}
```

**Command vs Event**:
- **Command**: Directed to **one handler** (request)
- **Event**: Broadcast to **multiple subscribers** (notification)

---

## Message Abstraction Layer

### Core Interfaces

```csharp
// BuildingBlocks.Messaging/IMessagePublisher.cs
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}

// BuildingBlocks.Messaging/IMessageConsumer.cs
public interface IMessageConsumer<T> where T : class
{
    Task ConsumeAsync(T message, CancellationToken ct = default);
}
```

---

### MassTransit Implementation

**Publisher**:
```csharp
public class MassTransitMessagePublisher : IMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        await _publishEndpoint.Publish(message, ct);
    }
}
```

**Consumer**:
```csharp
public class MatchAcceptedConsumer : IConsumer<MatchAccepted>
{
    private readonly IMediator _mediator;
    private readonly ILogger<MatchAcceptedConsumer> _logger;

    public async Task Consume(ConsumeContext<MatchAccepted> context)
    {
        var message = context.Message;

        _logger.LogInformation("Processing MatchAccepted: {MatchId}", message.MatchId);

        // Delegate to application layer (CQRS command)
        await _mediator.Send(new StartBlindDateSagaCommand(
            message.MatchId,
            message.InitiatorId,
            message.PartnerId));
    }
}
```

**Registration** (Program.cs):
```csharp
builder.Services.AddMassTransit(config =>
{
    // Register consumers
    config.AddConsumer<MatchAcceptedConsumer>();
    config.AddConsumer<ProfileCompletedConsumer>();

    // Configure transport
    if (builder.Environment.IsDevelopment())
    {
        // RabbitMQ (local)
        config.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("rabbitmq", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });

            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        // Azure Service Bus (production)
        config.UsingAzureServiceBus((context, cfg) =>
        {
            cfg.Host(builder.Configuration["AzureServiceBus:ConnectionString"]);
            cfg.ConfigureEndpoints(context);
        });
    }
});
```

---

## Outbox Pattern

### Problem
Ensure **atomicity** between database write and message publishing:

```
1. Save entity to database
2. Publish event to message broker
   ❌ What if step 2 fails? Event lost!
   ❌ What if step 1 fails after step 2? Duplicate event!
```

### Solution: Transactional Outbox

**How It Works**:
1. Save entity + outbox message in **same transaction**
2. Background worker publishes outbox messages
3. Mark messages as published

```csharp
// Outbox message entity
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public string Payload { get; set; } // JSON serialized event
    public DateTime CreatedAt { get; set; }
    public bool Published { get; set; }
    public DateTime? PublishedAt { get; set; }
}

// Save entity + outbox in transaction
public async Task AcceptMatchAsync(MatchId matchId)
{
    using var transaction = await _dbContext.Database.BeginTransactionAsync();

    // Step 1: Update match
    var match = await _matchRepo.GetByIdAsync(matchId);
    match.Accept();
    await _matchRepo.UpdateAsync(match);

    // Step 2: Save event to outbox (same transaction)
    var outboxMessage = new OutboxMessage
    {
        Id = Guid.NewGuid(),
        EventType = nameof(MatchAccepted),
        Payload = JsonSerializer.Serialize(new MatchAccepted(
            matchId,
            match.InitiatorId,
            match.PartnerId,
            DateTime.UtcNow)),
        CreatedAt = DateTime.UtcNow,
        Published = false
    };
    _dbContext.OutboxMessages.Add(outboxMessage);

    await _dbContext.SaveChangesAsync();
    await transaction.CommitAsync(); // Atomic!
}

// Background worker publishes outbox messages
public class OutboxPublisherBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var unpublished = await _dbContext.OutboxMessages
                .Where(m => !m.Published)
                .OrderBy(m => m.CreatedAt)
                .Take(100)
                .ToListAsync(stoppingToken);

            foreach (var message in unpublished)
            {
                try
                {
                    // Deserialize and publish
                    var eventType = Type.GetType(message.EventType);
                    var eventData = JsonSerializer.Deserialize(message.Payload, eventType);

                    await _publisher.PublishAsync(eventData, stoppingToken);

                    // Mark as published
                    message.Published = true;
                    message.PublishedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish outbox message {Id}", message.Id);
                    // Retry on next iteration
                }
            }

            await _dbContext.SaveChangesAsync(stoppingToken);
            await Task.Delay(1000, stoppingToken); // Poll every second
        }
    }
}
```

**Benefits**:
- ✅ Guaranteed delivery (if DB write succeeds, event will be published)
- ✅ No lost messages
- ✅ No duplicate events (idempotency handled separately)

---

## Message Routing

### Topic-Based Routing

**RabbitMQ**: Exchanges + Routing Keys

**Example**:
```
Exchange: "QuietMatch.Events"
Routing Key: "Match.Accepted"

Queues:
- "SchedulingService.MatchAccepted" (binds to "Match.Accepted")
- "NotificationService.MatchAccepted" (binds to "Match.Accepted")
- "AnalyticsService.AllEvents" (binds to "Match.*")
```

**MassTransit handles this automatically** based on consumer registration.

---

### Message Correlation

**Use Case**: Track message flow across services

**Implementation**: Correlation ID

```csharp
// Service A publishes event
await _publisher.PublishAsync(new MatchAccepted
{
    MatchId = matchId,
    CorrelationId = Guid.NewGuid() // Or from incoming request
});

// Service B consumes event
public async Task Consume(ConsumeContext<MatchAccepted> context)
{
    var correlationId = context.Message.CorrelationId;

    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId
    }))
    {
        _logger.LogInformation("Processing MatchAccepted");
        // All logs now include CorrelationId
    }
}
```

**Benefit**: Trace messages across microservices in logs/telemetry.

---

## Error Handling

### 1. Retry Strategy

**MassTransit Configuration**:
```csharp
config.UsingRabbitMq((context, cfg) =>
{
    cfg.UseMessageRetry(retry =>
    {
        retry.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
        // Retry 3 times: 1s, 2s, 4s delays
    });

    cfg.ConfigureEndpoints(context);
});
```

---

### 2. Dead Letter Queue (DLQ)

**What**: Queue for messages that fail after all retries

**MassTransit**: Automatic DLQ creation

**Monitoring**:
```csharp
// Check DLQ messages periodically
public async Task<IEnumerable<DlqMessage>> GetDeadLetterMessagesAsync()
{
    // Azure Service Bus: Dead letter sub-queue
    // RabbitMQ: _error exchange
    return await _dlqRepository.GetAllAsync();
}
```

**Manual Replay**:
```csharp
// Admin endpoint to replay DLQ message
[HttpPost("admin/dlq/{messageId}/replay")]
public async Task<IActionResult> ReplayDlqMessage(Guid messageId)
{
    var message = await _dlqRepo.GetByIdAsync(messageId);
    await _publisher.PublishAsync(message.Payload);
    await _dlqRepo.DeleteAsync(messageId);
    return Ok();
}
```

---

### 3. Idempotency

**Problem**: Messages may be delivered more than once (at-least-once delivery)

**Solution**: Idempotent handlers

```csharp
public class ReserveAvailabilitySlotConsumer : IConsumer<ReserveAvailabilitySlot>
{
    public async Task Consume(ConsumeContext<ReserveAvailabilitySlot> context)
    {
        var command = context.Message;

        // Check if already processed (idempotency key = CorrelationId)
        var existing = await _reservationRepo.GetByCorrelationIdAsync(command.CorrelationId);
        if (existing != null)
        {
            _logger.LogInformation("Slot already reserved, skipping (idempotent)");
            return; // No-op
        }

        // Process command
        var slot = await _availabilityService.ReserveAsync(
            command.MemberId1,
            command.MemberId2,
            command.PreferredStartTime);

        // Store with correlation ID for future idempotency checks
        await _reservationRepo.AddAsync(new Reservation
        {
            SlotId = slot.Id,
            CorrelationId = command.CorrelationId,
            CreatedAt = DateTime.UtcNow
        });
    }
}
```

---

## Testing Messaging

### Unit Tests (In-Memory)

```csharp
[Fact]
public async Task WhenMatchAccepted_ShouldPublishEvent()
{
    // Arrange
    var mockPublisher = new Mock<IMessagePublisher>();
    var service = new MatchingService(mockPublisher.Object, ...);

    // Act
    await service.AcceptMatchAsync(matchId);

    // Assert
    mockPublisher.Verify(p => p.PublishAsync(
        It.Is<MatchAccepted>(e => e.MatchId == matchId),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

---

### Integration Tests (Real Broker)

```csharp
[Fact]
public async Task MatchAccepted_ShouldTriggerScheduling()
{
    // Arrange: Start RabbitMQ in Testcontainers
    await using var rabbitMqContainer = new RabbitMqBuilder().Build();
    await rabbitMqContainer.StartAsync();

    var harness = new InMemoryTestHarness();
    var consumer = harness.Consumer<MatchAcceptedConsumer>();

    await harness.Start();

    // Act: Publish event
    await harness.InputQueueSendEndpoint.Send<MatchAccepted>(new { MatchId = matchId, ... });

    // Assert: Consumer received message
    Assert.True(await harness.Consumed.Any<MatchAccepted>());
    Assert.True(await consumer.Consumed.Any<MatchAccepted>());

    await harness.Stop();
}
```

---

## Event Catalog

| Event | Publisher | Subscribers | Trigger |
|-------|-----------|-------------|---------|
| **UserRegistered** | IdentityService | ProfileService, NotificationService | New user signs in |
| **ProfileCompleted** | ProfileService | MatchingService | User finishes questionnaire |
| **MatchProposed** | MatchingService | NotificationService, AnalyticsService | Algorithm generates match |
| **MatchAccepted** | MatchingService | SchedulingService, NotificationService, AnalyticsService | User accepts match |
| **BlindDateScheduled** | SchedulingService | NotificationService, AnalyticsService | SAGA completes |
| **BlindDateCancelled** | SchedulingService | NotificationService, MatchingService | User/system cancels |
| **SubscriptionActivated** | PaymentService | ProfileService, AnalyticsService | Payment succeeds |

---

## Best Practices

1. **Keep events small**: Send IDs, not full objects
2. **Version events**: Add version field for future schema changes
3. **Use correlation IDs**: Essential for tracing
4. **Implement idempotency**: Handlers must handle duplicates
5. **Monitor DLQs**: Set up alerts for dead letter messages
6. **Use outbox pattern**: Ensure atomicity for critical events
7. **Log everything**: Include CorrelationId in all logs

---

**Last Updated**: 2025-11-20
**Document Owner**: Integration Team
