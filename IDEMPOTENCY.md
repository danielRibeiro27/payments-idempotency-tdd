# Idempotency in the Payment API

This document explains how idempotency is implemented and why it's critical for payment processing.

## What is Idempotency?

Idempotency means that making the same request multiple times produces the same result as making it once. For payment APIs, this is crucial:

- **Problem**: Network issues can cause timeouts, leaving clients unsure if their request succeeded.
- **Risk**: Retrying without idempotency could create duplicate payments.
- **Solution**: Idempotency keys allow safe retries.

## How It Works

### 1. Client Provides Idempotency Key

Every payment request must include a unique `idempotencyKey`:

```json
{
  "amount": 100.00,
  "currency": "USD",
  "idempotencyKey": "order-12345-payment-attempt-1"
}
```

### 2. Server Checks for Existing Payment

When a request arrives, the server:

1. Looks up the `idempotencyKey` in the database
2. If found: Returns the existing payment (no new payment created)
3. If not found: Creates and stores a new payment

```csharp
// PaymentService.CreatePaymentAsync
var existingPayment = await _repository.GetByIdempotencyKeyAsync(payment.IdempotencyKey);
if (existingPayment != null)
{
    return existingPayment;  // Return existing, don't create new
}
// ... create new payment
```

### 3. Database Enforces Uniqueness

A unique index on `IdempotencyKey` provides database-level protection:

```csharp
// PaymentsDbContext
entity.HasIndex(p => p.IdempotencyKey).IsUnique();
```

This prevents duplicates even in race condition scenarios.

## Idempotency Semantics

### Same Key, Same Request = Same Response

```
Request 1: idempotencyKey="abc" → Creates Payment #1
Request 2: idempotencyKey="abc" → Returns Payment #1 (same ID, same data)
```

### Different Keys = Different Payments

```
Request 1: idempotencyKey="abc" → Creates Payment #1
Request 2: idempotencyKey="xyz" → Creates Payment #2 (different ID)
```

### Concurrent Requests

Even with concurrent requests using the same idempotency key, only one payment is created:

```
Time 0ms: Request A (key="abc") starts
Time 1ms: Request B (key="abc") starts
Time 5ms: Request A creates Payment #1
Time 6ms: Request B finds Payment #1, returns it
```

## Edge Cases

### 1. Empty Idempotency Key

If the idempotency key is empty or null, the payment is still created but idempotency check is skipped:

```csharp
if (!string.IsNullOrEmpty(payment.IdempotencyKey))
{
    // Check for existing payment
}
```

**Recommendation**: Always require non-empty idempotency keys in production.

### 2. Key Collision with Different Data

If the same key is used with different payment data:

| Scenario | Behavior |
|----------|----------|
| Same key, same amount/currency | Returns existing payment |
| Same key, different amount | Returns existing payment (first write wins) |

**Assumption**: Clients should generate unique keys per distinct operation. If you need to change the amount, use a new idempotency key.

### 3. Key Expiration

Current implementation stores keys permanently. For production:

- Consider adding a TTL (e.g., 24 hours)
- Clean up old entries periodically
- This reduces storage and allows key reuse after expiration

## Storage Strategy

### Current Implementation

| Field | Storage |
|-------|---------|
| `IdempotencyKey` | VARCHAR, NOT NULL |
| Index | UNIQUE on `IdempotencyKey` |

### Why Database-Level?

1. **Durability**: Keys survive server restarts
2. **Shared State**: Works in multi-instance deployments
3. **Race Condition Safety**: Database handles concurrent inserts

### Alternative Approaches

| Approach | Pros | Cons |
|----------|------|------|
| Database (current) | Durable, simple | Slower lookups |
| Redis | Fast, built-in TTL | Another dependency |
| In-memory | Fastest | Lost on restart, no multi-instance |

## Best Practices for Clients

1. **Generate unique keys**: Use UUIDs or composite keys like `{userId}-{orderId}-{action}-{timestamp}`

2. **Persist before sending**: Store the idempotency key locally before making the API call

3. **Retry with same key**: On timeout, retry using the exact same key

4. **Don't reuse keys**: Each distinct operation should have a unique key

## Testing Idempotency

The test suite verifies idempotency behavior:

```csharp
[Fact]
public async Task CreatePayment_WithSameIdempotencyKey_ShouldReturnSamePayment()
{
    // Create first payment
    var response1 = await client.PostAsJsonAsync("/api/payments", payment);
    var payment1 = await response1.Content.ReadFromJsonAsync<Payment>();

    // Retry with same idempotency key
    var response2 = await client.PostAsJsonAsync("/api/payments", payment);
    var payment2 = await response2.Content.ReadFromJsonAsync<Payment>();

    // Both return same payment
    Assert.Equal(payment1.Id, payment2.Id);
}
```

## Related Documentation

- [RETRIES.md](./RETRIES.md) - How retries work with idempotency
- [API_USAGE.md](./API_USAGE.md) - How to use the API
