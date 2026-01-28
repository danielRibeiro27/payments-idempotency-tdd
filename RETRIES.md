# Retry Mechanism

This document explains the retry behavior implemented in this Payment API, including what is retried, backoff strategy, limits, and safety guarantees.

## Why Retry?

Payment processing involves external systems (PSP/gateways) that can experience:

- **Network timeouts** - Connection dropped mid-request
- **Transient errors** - Temporary service unavailability
- **Rate limiting** - Too many requests (429 responses)
- **Infrastructure issues** - DNS failures, load balancer errors

Without retries, these recoverable failures would require manual intervention or customer retry, leading to poor user experience.

## What Gets Retried

### Retriable Errors ✅

| Error Type | Example | Retry? |
|------------|---------|--------|
| Network failure | `HttpRequestException` | ✅ Yes |
| Timeout | `TaskCanceledException` | ✅ Yes |
| Operation cancelled | `OperationCanceledException` | ✅ Yes |
| Timeout exception | `TimeoutException` | ✅ Yes |

### Non-Retriable Errors ❌

| Error Type | Example | Retry? |
|------------|---------|--------|
| Validation error | Invalid amount/currency | ❌ No |
| Business logic | Insufficient funds | ❌ No |
| Authentication | Invalid API key | ❌ No |
| Authorization | Permission denied | ❌ No |
| Not found | Unknown resource | ❌ No |
| Conflict | Duplicate key | ❌ No |

```csharp
private static bool IsTransientException(Exception ex)
{
    return ex is HttpRequestException
        || ex is TaskCanceledException
        || ex is OperationCanceledException
        || ex is TimeoutException;
}
```

## Retry Configuration

### Default Settings

```csharp
new RetryOptions
{
    MaxRetries = 3,           // 4 total attempts (1 initial + 3 retries)
    InitialDelayMs = 100,     // First retry after 100ms
    BackoffMultiplier = 2.0,  // Delay doubles each retry
    MaxDelayMs = 5000,        // Cap at 5 seconds
    UseJitter = true          // +/- 25% randomization
}
```

### Timing Breakdown

| Attempt | Delay Before | Cumulative Time |
|---------|--------------|-----------------|
| 1 | 0ms | 0ms |
| 2 | ~100ms | ~100ms |
| 3 | ~200ms | ~300ms |
| 4 | ~400ms | ~700ms |

*With jitter, actual delays vary +/- 25%*

## Exponential Backoff

### Why Exponential?

Linear retry (same delay each time) can:
- Overwhelm recovering services
- Create "thundering herd" when many clients retry simultaneously
- Not give enough recovery time

Exponential backoff gives progressively more time:

```
Attempt 1: Wait 100ms
Attempt 2: Wait 200ms  (2x previous)
Attempt 3: Wait 400ms  (2x previous)
Attempt 4: Wait 800ms  (2x previous, but capped at MaxDelayMs)
```

### Jitter

To prevent synchronized retries across clients, we add randomness:

```csharp
// Without jitter - all clients retry at exact same time
delay = 100ms → 200ms → 400ms

// With jitter (+/- 25%) - clients spread out
delay = 75-125ms → 150-250ms → 300-500ms
```

## Safety with Idempotency

Retries are **safe** because of our idempotency guarantees:

```
Attempt 1: Create payment (key: abc-123) → Network timeout
Attempt 2: Create payment (key: abc-123) → Success
Result: Only ONE payment exists
```

### The Retry Flow

```
Client                    API                     Gateway
  |                        |                        |
  |-- POST payment ------->|                        |
  |                        |-- Send to gateway ---->|
  |                        |        [TIMEOUT]       |
  |<- [Connection reset] --|                        |
  |                        |                        |
  |   [Wait 100ms]         |                        |
  |                        |                        |
  |-- POST payment ------->|                        |
  |   (same idempotency)   |                        |
  |                        |-- Check idempotency    |
  |                        |-- Key not found        |
  |                        |-- Send to gateway ---->|
  |                        |<- Success -------------|
  |                        |-- Store payment        |
  |<- 201 Created ---------|                        |
```

### Edge Case: First Request Succeeded

If the first attempt actually succeeded but the response was lost:

```
Client                    API                     Gateway
  |                        |                        |
  |-- POST payment ------->|                        |
  |                        |-- Send to gateway ---->|
  |                        |<- Success -------------|
  |                        |-- Store payment        |
  |        [Response lost mid-transit]              |
  |<- [Connection reset] --|                        |
  |                        |                        |
  |   [Wait 100ms]         |                        |
  |                        |                        |
  |-- POST payment ------->|                        |
  |   (same idempotency)   |                        |
  |                        |-- Check idempotency    |
  |                        |-- Key FOUND            |
  |<- Return existing -----|                        |
```

**No duplicate payment!** The existing payment is returned.

## Implementation Details

### Retry Policy Interface

```csharp
public interface IRetryPolicy
{
    Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        Func<T, bool>? shouldRetry = null
    );
}
```

### Usage in Gateway

```csharp
public class PaymentGateway : IPaymentGateway
{
    private readonly IRetryPolicy _retryPolicy;

    public async Task<bool> SendAsync(Payment payment)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            // Actual PSP call - retried on transient failures
            return await _pspClient.ProcessPayment(payment);
        });
    }
}
```

### Result-Based Retry

You can also retry based on result values:

```csharp
await _retryPolicy.ExecuteAsync(
    async () => await CheckPaymentStatus(id),
    shouldRetry: status => status == "pending"  // Retry while pending
);
```

## Limits and Circuit Breaking

### Current Limits

- **Max 3 retries** (4 total attempts)
- **Max 5 second delay** between retries
- **~1 second total** retry window

### Future Considerations

For production, consider adding:

1. **Circuit Breaker** - Stop retrying when service is down
   ```
   10 failures in 1 minute → Open circuit for 30 seconds
   ```

2. **Retry Budgets** - Limit total retries across requests
   ```
   Max 20% of requests can be retries
   ```

3. **Deadline Propagation** - Stop retrying if overall deadline passed
   ```
   Request deadline: 30 seconds
   After 25 seconds of retries → Stop and fail
   ```

## Monitoring and Logging

In production, track:

- Retry count per request
- Total time spent retrying
- Success rate after N retries
- Error types that trigger retries

```csharp
_logger.LogWarning(
    "Retry {Attempt} of {MaxRetries} for payment {PaymentId}. " +
    "Error: {ErrorType}",
    attempt, maxRetries, paymentId, ex.GetType().Name
);
```

## Testing Retries

Unit tests verify retry behavior:

```csharp
[Fact]
public async Task ShouldRetryOnTransientException()
{
    var callCount = 0;
    var policy = new ExponentialBackoffRetryPolicy(options);

    await policy.ExecuteAsync(async () =>
    {
        callCount++;
        if (callCount < 3)
            throw new HttpRequestException("Network error");
        return "success";
    });

    Assert.Equal(3, callCount); // 1 initial + 2 retries
}
```

See `tests/Payments.UnitTests/RetryPolicy_IsRetryShould.cs` for complete test coverage.
