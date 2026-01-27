# Retry Behavior

This document explains the retry mechanism in the Payment API and why it's safe when combined with idempotency.

## Overview

The `RetryService` provides automatic retry logic for operations that may fail due to transient issues. It's designed to work safely with idempotent operations.

## What is Retried

### Transient Failures (Retriable)

| Exception Type | Example Scenario |
|----------------|------------------|
| `TimeoutException` | Database query timeout |
| `TaskCanceledException` | HTTP request timeout |
| `HttpRequestException` | Network connectivity issues |
| `InvalidOperationException` (with "transient" in message) | Transient database errors |

### Non-Transient Failures (NOT Retriable)

| Exception Type | Example Scenario |
|----------------|------------------|
| `ArgumentException` | Invalid input data |
| `InvalidOperationException` | Business rule violation |
| `NullReferenceException` | Programming error |
| Any other exception | Unknown errors |

## Backoff Strategy

The retry service uses **exponential backoff** to avoid overwhelming recovering services:

```
Attempt 1: Fails → Wait 100ms
Attempt 2: Fails → Wait 200ms
Attempt 3: Fails → Wait 400ms
Attempt 4: Fails → Wait 800ms (capped at 5000ms)
...
```

### Configuration

| Parameter | Default | Description |
|-----------|---------|-------------|
| `maxRetries` | 3 | Maximum retry attempts (0 = no retries) |
| `BaseDelayMs` | 100ms | Initial delay before first retry |
| `MaxDelayMs` | 5000ms | Maximum delay cap |

### Formula

```csharp
delay = min(BaseDelayMs * 2^(attempt-1), MaxDelayMs)
```

## Usage

### Service Registration

The retry service is registered as a singleton:

```csharp
builder.Services.AddSingleton<IRetryService, RetryService>();
```

### Example: Retrying a Database Operation

```csharp
var result = await _retryService.ExecuteWithRetryAsync(async () =>
{
    return await _database.QueryAsync<Payment>("SELECT * FROM Payments WHERE Id = @Id", id);
}, maxRetries: 3);
```

### Example: Retrying Without Return Value

```csharp
await _retryService.ExecuteWithRetryAsync(async () =>
{
    await _externalService.NotifyPaymentCreatedAsync(payment);
}, maxRetries: 2);
```

## Why It's Safe with Idempotency

The retry mechanism is safe because of the idempotency implementation:

### Scenario: Timeout on Payment Creation

```
1. Client sends CreatePayment(idempotencyKey="abc")
2. Server creates payment, but response times out
3. Client doesn't know if payment was created
4. Client retries with same idempotencyKey="abc"
5. Server finds existing payment, returns it
6. Client receives confirmation
```

**Result**: Only one payment exists, client got confirmation.

### Scenario: Retry Inside Server

```csharp
await _retryService.ExecuteWithRetryAsync(async () =>
{
    var payment = await _paymentService.CreatePaymentAsync(paymentRequest);
    // If this fails transiently, retry will:
    // 1. Call CreatePaymentAsync again
    // 2. Service checks idempotencyKey
    // 3. Returns existing payment (no duplicate)
    return payment;
});
```

## Limits

### Why We Limit Retries

1. **Avoid infinite loops**: A truly broken service won't recover with more retries
2. **Fail fast for real errors**: Don't waste time on non-transient failures
3. **Preserve resources**: Each retry consumes CPU, memory, and connections

### When to Increase Max Retries

Consider more retries when:
- External services have known recovery times
- Network is unreliable but eventually consistent
- Operation cost of failure is very high

### When to Decrease Max Retries

Consider fewer retries when:
- User is waiting (latency matters)
- Resource constraints are tight
- You have alternative fallback mechanisms

## Monitoring Recommendations

In production, consider:

1. **Log retry attempts**: Track how often retries occur
2. **Alert on exhausted retries**: Know when the system is struggling
3. **Metric: retry ratio**: High ratios indicate underlying issues

```csharp
// Example logging (not implemented in base service)
_logger.LogWarning("Retry {Attempt}/{MaxRetries} for operation due to {Exception}",
    attempt, maxRetries, ex.GetType().Name);
```

## Testing

The retry service is thoroughly tested:

```csharp
[Fact]
public async Task ExecuteWithRetryAsync_WhenTransientFailureThenSuccess_ShouldRetryAndSucceed()
{
    int attemptCount = 0;
    var result = await _retryService.ExecuteWithRetryAsync(async () =>
    {
        attemptCount++;
        if (attemptCount < 2)
        {
            throw new TimeoutException("Transient failure");
        }
        return "success";
    }, maxRetries: 3);

    Assert.Equal("success", result);
    Assert.Equal(2, attemptCount);
}
```

## Related Documentation

- [IDEMPOTENCY.md](./IDEMPOTENCY.md) - How idempotency protects against duplicates
- [API_USAGE.md](./API_USAGE.md) - How to use the API
