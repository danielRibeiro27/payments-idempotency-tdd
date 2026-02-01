# Payment Intent API Expected Flow

1. Validate request
2. Register payment intent (with idempotency)
3. Call PSP/Gateway (with idempotency)
4. Update payment intent status
5. Fire events/audit
6. Response (status, events)

## Assertions

- [ ] Payment intent should be processed successfully
- [ ] Payment intent operations should be idempotent
- [ ] Payment intent should be added to DB
- [ ] Service should call PSP/Gateway
- [ ] Payment intent status should be updated accordingly
- [ ] Events/audit should be fired
- [ ] Response should contain correct status and events