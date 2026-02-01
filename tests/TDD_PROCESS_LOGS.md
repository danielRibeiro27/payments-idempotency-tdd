# TDD Progress Log — Payments Domain & Service

This document records the **Red → Green → Refactor** progression followed during the implementation of the Payments domain and service layer.  
The goal is traceability of design decisions, not completeness of refactors.

---

## 1. Domain: `Payments` Entity

### RED
- `Domain.Payments` should be a valid record.

### GREEN
- Added required properties.
- Added domain-level validations.

### REFACTOR
- Not necessary.

---

## 2. Service: Process Success Path

### RED
- `Domain.Service` should return **process success**.
- Test method needs to instantiate the service but requires a repository.
- No mocking library available.

### GREEN
- Added a mocking library.
- Service can now be instantiated with mocks.

---

## 3. Abstraction for Mocking

### RED
- Service depends on a concrete domain type.
- No interface available for mocking.

### GREEN
- Introduced `IPayment` abstraction (but removed after).
- Mocking becomes possible.

---

## 4. Repository Injection

### RED
- Payment service requires repository injection.
- Repository must be mocked.

### GREEN
- Repository interface injected.
- Mock repository created and wired.

---

## 5. Payment Status Modeling

### RED
- Payment entity has no definition for status.

### GREEN
- Added `Status` property to the entity.
- Service now defines and updates payment status.

### REFACTOR
- Not necessary.

---

## 6. Service: Process Failure Path

### RED
- `Domain.Service` should return **process failure**.
- Create method does not check for payment validity.

### RED (continued)
- Create method checks validity, but entity has no `IsValid` definition.

### GREEN
- Added domain validation logic.
- Service now respects domain validation results.

### REFACTOR
- Not necessary.

---

## 7. Concurrency & Idempotency Behavior

### RED
- Payment intent should return the same status when two requests run concurrently.

### RED (unexpected)
- Initial unit test passed, meaning the logic was incorrect (test should fail).
- Test was not exercising real concurrency behavior.

### RED (forced failure)
- Added a mocked gateway that fails on the second payment intent.
- Test now fails as expected.

### STILL RED
- Even with `GetByIdempotencyKey`, the mock returned the same instance.
- Root cause: passing direct object references caused both requests to point to the same memory location.

### GREEN
- Refactored repository and service layers to handle concurrency correctly.
- Ensured independent execution paths per request.
- Gateway invoked exactly once per idempotency key.

### RESULT
- Correct concurrent behavior achieved.
- One payment processed.
- One final status persisted.
- No duplicated side effects.

---

## Final Notes

- Several steps required **no refactor phase** because changes were minimal and localized.
- TDD made architectural pressure explicit (interfaces, injection points, boundaries).
- Concurrency bugs were surfaced only after intentionally **breaking the test**.
- The approach validated both **design correctness** and **test quality**.
