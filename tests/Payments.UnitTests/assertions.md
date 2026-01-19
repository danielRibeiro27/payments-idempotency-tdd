# Payment API Expected Flow

1. Validate request
2. Register payment (with idempotency)
3. Call PSP/Gateway (with idempotency)
4. Update payment status
5. Fire events/audit
6. Response (status, events)

## Assertions

- [ ] Payment should be processed successfully
- [ ] Payment operations should be idempotent
- [ ] Payment should be added to DB
- [ ] Service should call PSP/Gateway
- [ ] Payment status should be updated accordingly
- [ ] Events/audit should be fired
- [ ] Response should contain correct status and events