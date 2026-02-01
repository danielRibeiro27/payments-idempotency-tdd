# Payments Idempotency API

A minimal .NET 10 API implementing an **idempotent Payment Intent** system, developed using **Test-Driven Development (TDD)**.

## Overview

This API ensures payment consistency by handling concurrent requests safely through idempotency keys. Duplicate requests with the same key return the existing result without reprocessing.

## Tech Stack

- **.NET 10** (Minimal API)
- **Entity Framework Core** + SQLite (in-memory)
- **xUnit** + **Moq** (unit testing)
- **Swagger/OpenAPI**

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/payments/{id:guid}` | Retrieve payment by ID (404 if not found) |
| `POST` | `/api/payments` | Create payment intent (201 on success, 409 on idempotency conflict) |

## Domain Model

### PaymentIntent

```csharp
public class PaymentIntent(decimal amount, string currency, Guid idempotencyKey)
```

**Properties:** `Id`, `Amount`, `Currency`, `Status`, `CreatedAt`, `IdempotencyKey`

**Validation Rules:**
- `Id` cannot be empty
- `Amount` cannot be 0 (negative allowed for refunds)
- `Currency` must be exactly 3 characters (ISO 4217)
- `CreatedAt` cannot be in the future or `DateTime.MinValue`
- `IdempotencyKey` cannot be empty

### Status Enum

`Undefined` | `Pending` | `Completed` | `Failed` | `Refunded` | `Invalid`

## Idempotency Behavior

| Scenario | Result |
|----------|--------|
| New idempotency key | Create & process payment |
| Same key + same payload | Return existing payment (no reprocessing) |
| Same key + different payload | `409 Conflict` |

## Project Structure

```
src/Payments.Api/
├── Application/        # Program.cs (entry point)
├── Domain/             # PaymentIntent, StatusEnum
├── Infrastructure/     # Repository, Gateway, DbContext
└── Service/            # PaymentService, Hasher

tests/
├── Payments.UnitTests/         # Domain & service tests
├── Payments.IntegrationTests/  # Placeholder
└── Payments.FunctionalTests/   # Placeholder
```

## Running

```bash
dotnet run --project src/Payments.Api
```

## Testing

```bash
dotnet test
```

## TDD Approach

Development followed **Red → Green → Refactor** cycles:

1. **Domain validation** — Entity properties and `IsValid()` method
2. **Service success path** — Payment processing with mocked dependencies
3. **Service failure path** — Handling invalid payments
4. **Idempotency guarantees** — Concurrent requests, single processing

See [TDD_PROCESS_LOGS.md](tests/TDD_PROCESS_LOGS.md) for detailed progression.

## Key Design Decisions

- **No Use Case layer** — Simple CRUD; service orchestrates domain operations directly
- **Payload hashing** — SHA256 hash comparison for idempotency conflict detection
- **Simulated PSP** — Gateway returns `Success` for `Amount > 0`, `Failure` otherwise
- **Unique constraint** — `IdempotencyKey` has a database-level unique index

---

## Out of Scope

- Transaction security (handled by messaging queue jobs)
- Retry logic for transient failures
- Timeout/fallback handling
- Dead Letter Queue (DLQ) for persistent failures
- Encryption & monitoring

