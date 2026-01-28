# Payment API Expected Flow

1. Validate request
2. Register payment (with idempotency)
3. Call PSP/Gateway (with idempotency)
4. Update payment status
5. Fire events/audit
6. Response (status, events)

## Assertions Status

### Domain Tests
- [x] Payment should be created successfully
- [x] Payment properties should be created correctly
- [x] Payment IDs should be unique
- [x] Payment validation should work correctly

### Service Tests  
- [x] Payment should be processed successfully
- [x] Payment operations should be idempotent
- [x] Payment should be added to DB
- [x] Service should call PSP/Gateway
- [x] Payment status should be updated accordingly
- [x] Gateway failures should be handled

### Retry Policy Tests
- [x] Retry on transient exceptions
- [x] Respect max retry count  
- [x] No retry on business errors
- [x] Result-based retry predicate

### Integration Tests
- [x] Create payment endpoint
- [x] Get payment endpoint
- [x] Idempotent behavior
- [x] Concurrent request handling
- [x] Gateway failure handling

## Not Implemented (Out of Scope for MVP)
- [ ] Events/audit firing
- [ ] Functional/E2E tests