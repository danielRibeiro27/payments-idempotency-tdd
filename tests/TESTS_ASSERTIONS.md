# TESTS_ASSERTIONS.md

## Scope

This document defines the **test assertions** for the `payment-intent.api`, aligned with the expected service flow and a TDD approach.  
Not all assertions will be implemented due to time constraints; unimplemented items are explicitly acknowledged.

---

## Expected Payment Intent Flow

The API is expected to follow this sequence:

1. **Validate Request**
   - Validate request body and required headers.
   - `Idempotency-Key` header must be present (implemented as property).

2. **Register Payment Intent (Idempotent)**
   - Persist or retrieve a payment intent using the idempotency key.
   - Enforce payload consistency.

3. **Call PSP / Gateway (Idempotent)**
   - External PSP/Gateway call must occur **at most once** per idempotency key.

4. **Update Payment Intent Status**
   - Persist final state (`Pending`, `Completed`, `Failed`).

5. **Fire Events / Audit**
   - Emit domain events and/or audit logs.
   - Must not duplicate on retries.

6. **Return Response**
   - Return payment status and related events.
   - Response symmetry under concurrency is not required.

---

## Test Assertions

### Domain Model Assertions

- Domain model should be created successfully
- Domain model properties should be created correctly
- Domain model keys should be unique and valid
- Domain model status should be set correctly
- Domain model should validate itself correctly

These assertions are validated through **pure unit tests** and represent the minimum correctness guarantees of the Payments domain.

---

### Happy Path

#### Functional Behavior
- Service returns **process success or failure**  
  **Status:** Implemented

- Service registers a payment intent via the repository

- Service calls the PSP/Gateway

- Service updates payment intent status accordingly

- Service fires events and/or audit records

#### Idempotency Guarantees
- Service operations are idempotent (safe to retry without side effects)
  - Status is the same on retries  
    *(or one `Completed` and one `Pending` under concurrency)*
  - Repository calls are **not duplicated** on retries
  - PSP/Gateway calls are **not duplicated** on retries

#### API-Level Validation
- API validates presence of `Idempotency-Key` header  
  *(integration or functional tests)*

- Service rejects the same idempotency key with a **different payload**  
  *(integration or functional tests)*

- Service returns the already-processed payment when the idempotency key exists

---

### Unhappy Path

- Service handles PSP/Gateway failures gracefully
- Service handles repository failures gracefully

---

## Notes

- Not all assertions listed above will be implemented due to time constraints.
- The document represents the **desired correctness envelope**, not full test coverage.
- Additional assertions may be added as requirements evolve.
