# Red-Green-Refactor: TDD Process

This document provides a template for tracking the Test-Driven Development (TDD) process used in this project.

## TDD Cycle Overview

```
┌─────────────────────┐
│    1. RED           │
│  Write failing test │
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐
│    2. GREEN         │
│ Make test pass      │
│ (minimal code)      │
└─────────┬───────────┘
          │
          ▼
┌─────────────────────┐
│    3. REFACTOR      │
│ Improve code        │
│ (tests still pass)  │
└─────────────────────┘
```

---

## Feature: Payment Creation

### RED Phase
*Write a failing test first*

- [ ] Test: `CreatePaymentAsync_WithValidPayment_ShouldReturnCreatedPayment`
- [ ] Test fails because: (implementation missing)

### GREEN Phase
*Write minimal code to pass*

- [ ] Implement `PaymentService.CreatePaymentAsync`
- [ ] Test passes

### REFACTOR Phase
*Improve without changing behavior*

- [ ] Extract validation logic
- [ ] Add logging
- [ ] Tests still pass

---

## Feature: Idempotency

### RED Phase
*Write a failing test first*

- [ ] Test: `CreatePaymentAsync_WithExistingIdempotencyKey_ShouldReturnExistingPayment`
- [ ] Test fails because: (idempotency not implemented)

### GREEN Phase
*Write minimal code to pass*

- [ ] Add `GetByIdempotencyKeyAsync` to repository
- [ ] Check for existing payment before creating
- [ ] Test passes

### REFACTOR Phase
*Improve without changing behavior*

- [ ] Add unique index on IdempotencyKey
- [ ] Improve error handling
- [ ] Tests still pass

---

## Feature: Retry Service

### RED Phase
*Write a failing test first*

- [ ] Test: `ExecuteWithRetryAsync_WhenTransientFailureThenSuccess_ShouldRetryAndSucceed`
- [ ] Test fails because: (service doesn't exist)

### GREEN Phase
*Write minimal code to pass*

- [ ] Create `RetryService` with basic retry loop
- [ ] Test passes

### REFACTOR Phase
*Improve without changing behavior*

- [ ] Add exponential backoff
- [ ] Add max delay cap
- [ ] Improve transient detection
- [ ] Tests still pass

---

## TDD Log Template

Use this template to document each TDD cycle:

### Cycle: [Feature Name]

**Date**: YYYY-MM-DD

**RED**:
- Test name: 
- Expected behavior:
- Why it fails:

**GREEN**:
- Changes made:
- Test result:

**REFACTOR**:
- Improvements:
- Tests still passing: Yes/No

---

## Guidelines for TDD

### Writing Good Tests

1. **One assertion per test** (when possible)
2. **Descriptive names**: `MethodName_Scenario_ExpectedBehavior`
3. **Arrange-Act-Assert** structure
4. **Independent tests**: No test depends on another

### When to Write Tests First

- New features
- Bug fixes (write test that reproduces bug)
- Edge cases
- Integration points

### When Tests May Come After

- Exploratory prototyping (spike)
- UI layout changes
- One-off scripts

### Test Pyramid

```
     /\
    /  \  E2E Tests (few)
   /────\
  /      \ Integration Tests (some)
 /────────\
/          \ Unit Tests (many)
```

---

## Notes

*Add your TDD observations, lessons learned, and insights here*

- 
- 
- 
