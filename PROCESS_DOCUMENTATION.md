# Consolidated Resolutions

---

## A) Idempotency in Payment APIs

### Goal
- Prevent duplicate side effects (exactly-once execution).
- Identical synchronous responses are **not** required.

### Hard Guarantee
- **Database UNIQUE constraint on `IdempotencyKey`**.
- Works across threads, processes, and restarts.
- No in-memory locks needed.

### Rules
- Same key + same payload → return existing result.
- Same key + different payload → **409 Conflict**.

### Service Flow (`CreatePayment`)
1. Validate input.
2. Try insert by `IdempotencyKey`.
   - Exists + payload mismatch → conflict.
   - Exists + match → return existing (Pending/Completed).
   - New → call gateway once, persist result, return payment.

### Concurrency Outcome
- One DB row.
- Gateway called once.
- Responses may differ (Pending vs Completed).
- This is correct idempotency.

### Explicit Non‑Goals
- No `SemaphoreSlim` / in-memory locks.
- No in-flight task joining.
- No reliance on PSP idempotency.
- No over-engineering unless tests force it.

### Testing
- **Unit**: same key behavior, payload mismatch, validation.
- **Integration**: real DB constraint + concurrency → one row, one gateway call.

### Key Insight
- Idempotency protects **side effects**, not response symmetry.

---

## B) EF Core, Domain, and Mocking

### Principles
- Do **not** mock domain entities.
- Accept EF Core’s need for concrete types.

### Mock Boundaries Only
- Mock gateways and repository interfaces.
- Test EF repositories with integration tests.

### Service API
- Accept commands/DTOs.
- Create entities internally (factory).

### Testing Pyramid
- Domain: unit tests (invariants).
- Service: unit tests (mocks).
- Persistence: integration tests.

### Bottom Line
- Full isolation is unrealistic with EF Core.
- Prefer real domain + boundary mocks.

---

## C) xUnit Bootstrap via TDD (Book Context)

### Purpose
- Demonstrates building test infrastructure with TDD.

### Key Points
- `WasRun` is intentionally artificial.
- `TestCase` emerges from responsibility split.
- `run()` is the core xUnit primitive.
- Tiny steps preserve feedback without infrastructure.

### Takeaway
- Relevant for framework authors.
- Not required for everyday xUnit usage.