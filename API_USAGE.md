# API Usage Guide

This document describes the Payment API endpoints, request/response formats, and usage examples.

## Base URL

- Development: `http://localhost:5000` or `https://localhost:5001`
- Production: Configure via environment variables

## Endpoints

### Create Payment

Creates a new payment or returns an existing payment if the idempotency key was already used.

**Endpoint:** `POST /api/payments`

**Request Body:**
```json
{
  "amount": 100.50,
  "currency": "USD",
  "idempotencyKey": "unique-request-id-123"
}
```

**Request Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `amount` | decimal | Yes | Payment amount (non-zero, allows negative for refunds) |
| `currency` | string | Yes | ISO 4217 currency code (exactly 3 characters) |
| `idempotencyKey` | string | Yes | Unique identifier for this payment request |

**Success Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "amount": 100.50,
  "currency": "USD",
  "status": 2,
  "createdAt": "2024-01-15T10:30:00Z",
  "idempotencyKey": "unique-request-id-123"
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Unique payment identifier |
| `amount` | decimal | Payment amount |
| `currency` | string | Currency code |
| `status` | int | Payment status (0=Undefined, 1=Pending, 2=Completed, 3=Failed, 4=Refunded) |
| `createdAt` | datetime | UTC timestamp of creation |
| `idempotencyKey` | string | The idempotency key used |

**Idempotent Behavior:**
If you send the same `idempotencyKey` again, you'll receive the original payment response:
- Same HTTP 201 status code
- Same payment data (original amount, currency, etc.)
- No new payment is created

### Get Payment

Retrieves a payment by its ID.

**Endpoint:** `GET /api/payments/{id}`

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | GUID | Payment identifier |

**Success Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "amount": 100.50,
  "currency": "USD",
  "status": 2,
  "createdAt": "2024-01-15T10:30:00Z",
  "idempotencyKey": "unique-request-id-123"
}
```

**Not Found Response (404):**
Returns empty response with 404 status code.

## Error Handling

### Validation Errors

If a payment fails validation (e.g., invalid amount, currency, or idempotency key), the payment is still created but with `status: 3` (Failed):

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "amount": 0,
  "currency": "INVALID",
  "status": 3,
  "createdAt": "2024-01-15T10:30:00Z",
  "idempotencyKey": ""
}
```

### Conflict Errors (409)

Returned when:
- Database concurrency conflict occurs
- Invalid operation exception during processing

```
A payment with the same idempotency key already exists.
```

## Status Codes

| Status | Description |
|--------|-------------|
| 0 - Undefined | Initial/unknown state |
| 1 - Pending | Payment is being processed |
| 2 - Completed | Payment successfully processed |
| 3 - Failed | Payment failed (validation or gateway error) |
| 4 - Refunded | Payment has been refunded |

## Usage Examples

### cURL Examples

**Create a payment:**
```bash
curl -X POST http://localhost:5000/api/payments \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 150.00,
    "currency": "EUR",
    "idempotencyKey": "order-12345-payment"
  }'
```

**Get a payment:**
```bash
curl http://localhost:5000/api/payments/550e8400-e29b-41d4-a716-446655440000
```

### HTTP File (for VS Code REST Client)

See `src/Payments.Api/Application/Payments.Api.http` for interactive API testing.

## Best Practices

### Idempotency Key Generation

Generate idempotency keys that are:
1. **Unique per operation** - Use UUIDs or combine order ID + action type
2. **Deterministic for retries** - Same operation should use same key
3. **Client-generated** - Don't rely on server to generate

Good examples:
- `order-{orderId}-payment`
- `{userId}-{timestamp}-{random}`
- UUID v4

Bad examples:
- Sequential numbers (collision risk)
- Empty strings
- Server-generated timestamps

### Retry Strategy

For transient failures:
1. Use the **same idempotency key** for retries
2. Implement exponential backoff (100ms, 200ms, 400ms...)
3. Set a maximum retry count (3-5 recommended)
4. Don't retry validation/business errors

See [RETRIES.md](RETRIES.md) for detailed guidance.
