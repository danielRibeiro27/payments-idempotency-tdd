# Red-Green-Refactor: TDD Process Documentation

This document outlines the Test-Driven Development (TDD) process used to build this Payment API.

## TDD Cycle Overview

```
┌─────────────────────────────────────────────────┐
│                                                 │
│   ┌─────────┐    ┌─────────┐    ┌──────────┐  │
│   │   RED   │───▶│  GREEN  │───▶│ REFACTOR │  │
│   │ (Write  │    │ (Make   │    │ (Clean   │  │
│   │  test)  │    │  pass)  │    │   up)    │  │
│   └─────────┘    └─────────┘    └──────────┘  │
│        ▲                              │        │
│        └──────────────────────────────┘        │
│                                                 │
└─────────────────────────────────────────────────┘
```

## Phase 1: Domain Model Tests

### RED: Define Payment Entity Tests

```csharp
// Test: Payment should be created with valid properties
[Fact]
public void Payment_ShouldBeCreatedSuccessfully()
{
    // Expected: Payment entity can be instantiated
}

[Fact]
public void Property_Id_ShouldBeUnique()
{
    // Expected: Each payment gets a unique GUID
}
```

### GREEN: Implement Payment Entity

```csharp
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public Status Status { get; set; } = Status.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string IdempotencyKey { get; set; }
}
```

### REFACTOR: Add Validation

```csharp
public bool IsValid()
{
    if (Id == Guid.Empty) return false;
    if (Amount == 0) return false;
    if (string.IsNullOrWhiteSpace(Currency) || Currency.Length != 3) return false;
    if (string.IsNullOrWhiteSpace(IdempotencyKey)) return false;
    return true;
}
```

---

## Phase 2: Service Layer Tests

### RED: Define Service Behavior Tests

```csharp
// Test: Service should process payment successfully
[Fact]
public async Task Service_ShouldProcessPaymentSuccessfully()
{
    // Expected: Valid payment gets Completed status
}

// Test: Service should be idempotent
[Fact]
public async Task Service_ShouldNotProcessTheSamePaymentTwice()
{
    // Expected: Same idempotency key = same payment returned
    // Expected: Repository called only once
}
```

### GREEN: Implement PaymentService

```csharp
public async Task<Payment> CreatePaymentAsync(Payment payment)
{
    // Check idempotency
    var existing = await _repo.GetByIdempotencyKeyAsync(payment.IdempotencyKey);
    if (existing != null) return existing;
    
    // Process payment
    if (payment.IsValid())
    {
        var success = await _gateway.SendAsync(payment);
        payment.Status = success ? Status.Completed : Status.Failed;
    }
    else
    {
        payment.Status = Status.Failed;
    }
    
    return await _repo.AddAsync(payment);
}
```

### REFACTOR: Add Thread-Safety

```csharp
private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

public async Task<Payment> CreatePaymentAsync(Payment payment)
{
    var lockObj = _locks.GetOrAdd(payment.IdempotencyKey, _ => new SemaphoreSlim(1, 1));
    await lockObj.WaitAsync();
    try
    {
        // ... idempotent processing
    }
    finally
    {
        lockObj.Release();
    }
}
```

---

## Phase 3: Retry Policy Tests

### RED: Define Retry Behavior Tests

```csharp
[Fact]
public async Task ExecuteAsync_ShouldRetryOnTransientException()
{
    // Expected: Retry happens on HttpRequestException
}

[Fact]
public async Task ExecuteAsync_ShouldNotRetryOnBusinessException()
{
    // Expected: No retry on InvalidOperationException
}

[Fact]
public async Task ExecuteAsync_ShouldRespectMaxRetries()
{
    // Expected: Stops after max retry count
}
```

### GREEN: Implement Retry Policy

```csharp
public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
{
    var attempts = 0;
    var delay = _options.InitialDelayMs;
    
    while (true)
    {
        attempts++;
        try
        {
            return await operation();
        }
        catch (Exception ex) when (IsTransient(ex) && attempts <= _options.MaxRetries)
        {
            await Task.Delay(delay);
            delay = CalculateNextDelay(delay);
        }
    }
}
```

### REFACTOR: Add Exponential Backoff with Jitter

```csharp
private int CalculateNextDelay(int currentDelay)
{
    var nextDelay = (int)(currentDelay * _options.BackoffMultiplier);
    nextDelay = Math.Min(nextDelay, _options.MaxDelayMs);
    
    if (_options.UseJitter)
    {
        var jitter = (int)(nextDelay * 0.25);
        nextDelay += _random.Next(-jitter, jitter + 1);
    }
    
    return nextDelay;
}
```

---

## Phase 4: Integration Tests

### RED: Define API Endpoint Tests

```csharp
[Fact]
public async Task CreatePayment_ShouldReturnCreated_WhenValid()
{
    // Expected: POST /api/payments returns 201 Created
}

[Fact]
public async Task CreatePayment_ShouldBeIdempotent()
{
    // Expected: Same key returns same payment
}

[Fact]
public async Task ConcurrentRequests_ShouldCreateOnlyOne()
{
    // Expected: Race condition handled correctly
}
```

### GREEN: Implement API Endpoints

```csharp
app.MapPost("/api/payments", async (Payment payment, IPaymentService service) =>
{
    var created = await service.CreatePaymentAsync(payment);
    return Results.CreatedAtRoute("GetPayment", new { id = created.Id }, created);
});
```

### REFACTOR: Error Handling

```csharp
app.MapPost("/api/payments", async (Payment payment, IPaymentService service) =>
{
    try
    {
        var created = await service.CreatePaymentAsync(payment);
        return Results.CreatedAtRoute("GetPayment", new { id = created.Id }, created);
    }
    catch (DBConcurrencyException)
    {
        return Results.Conflict("Payment already exists");
    }
});
```

---

## Test Metrics

### Test Counts

| Category | Count | Coverage |
|----------|-------|----------|
| Domain Unit Tests | 8 | Payment entity, validation |
| Service Unit Tests | 7 | Idempotency, gateway integration |
| Retry Policy Tests | 9 | Backoff, error handling |
| Integration Tests | 7 | API endpoints, concurrency |
| **Total** | **31** | |

### Test Principles Applied

- ✅ **Fast** - Unit tests run in <1 second
- ✅ **Independent** - No test depends on another
- ✅ **Repeatable** - Same results every run
- ✅ **Self-Validating** - Clear pass/fail
- ✅ **Timely** - Written before/with code

---

## Notes for Future Development

*This section is a template for tracking TDD progress on new features.*

### Feature: [Feature Name]

**Date Started:** _____

#### RED Phase
- [ ] Test 1: _____
- [ ] Test 2: _____
- [ ] Test 3: _____

**Tests Written:** ___ | **Tests Passing:** 0

#### GREEN Phase
- [ ] Implementation notes: _____
- [ ] Edge cases handled: _____

**Tests Passing:** ___ / ___

#### REFACTOR Phase
- [ ] Code smells addressed: _____
- [ ] Performance improvements: _____
- [ ] Documentation updated: _____

**Final Status:** ⬜ Complete | ⬜ In Progress | ⬜ Blocked

---

## Resources

- [TDD by Example - Kent Beck](https://www.amazon.com/Test-Driven-Development-Kent-Beck/dp/0321146530)
- [xUnit Documentation](https://xunit.net/)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
