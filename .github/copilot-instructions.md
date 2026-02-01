# Copilot agent instructions guide

## Repository goal
Implement an idempotent Payment Intent API, guided by TDD, focusing on domain rules and payment flow consistency.

## Architecture and entry points
- Minimal .NET 10 API in [src/Payments.Api/Application/Program.cs](src/Payments.Api/Application/Program.cs).
- Persistence with EF Core using SQLite in-memory via `PaymentsDbContext` in [src/Payments.Api/Infrastructure/PaymentsDbContext.cs](src/Payments.Api/Infrastructure/PaymentsDbContext.cs).
- GET endpoint: `/api/payments/{id:guid}` returns 404 when not found.
- POST endpoint: `/api/payments` creates an intent and returns 201; idempotency conflicts return 409.

## Domain rules
- Core entity: `PaymentIntent` in [src/Payments.Api/Domain/Implementations/PaymentIntent.cs](src/Payments.Api/Domain/Implementations/PaymentIntent.cs).
- Validation (`IsValid()`):
  - `Id` cannot be empty.
  - `Amount` cannot be 0 (negative values are allowed for refunds).
  - `Currency` must have 3 characters.
  - `CreatedAt` cannot be in the future or `DateTime.MinValue`.
  - `IdempotencyKey` cannot be empty.
- Status values live in the `Status` enum: [src/Payments.Api/Domain/Implementations/StatusEnum.cs](src/Payments.Api/Domain/Implementations/StatusEnum.cs).
- `CurrencyEnum.cs` is empty: if you add currencies, keep the ISO 4217 3-letter format.

## Idempotency and consistency
- Creation must use `GetOrAddByIdempotencyKey` from the repository in [src/Payments.Api/Infrastructure/Interfaces/IPaymentRepository.cs](src/Payments.Api/Infrastructure/Interfaces/IPaymentRepository.cs).
- If the key exists and the payload is different, throw `InvalidOperationException` (see `Hasher` in [src/Payments.Api/Service/Hasher.cs](src/Payments.Api/Service/Hasher.cs)).
- If the key exists and the payload is the same, return the existing record without reprocessing.

## Services and infrastructure
- `PaymentService` in [src/Payments.Api/Service/Implementations/PaymentService.cs](src/Payments.Api/Service/Implementations/PaymentService.cs) orchestrates validation, idempotency, and status updates.
- `PaymentGateway` in [src/Payments.Api/Infrastructure/Implementations/PaymentGateway.cs](src/Payments.Api/Infrastructure/Implementations/PaymentGateway.cs) simulates a PSP; success when `Amount > 0`.
- Concrete repository in [src/Payments.Api/Infrastructure/Implementations/PaymentRepository.cs](src/Payments.Api/Infrastructure/Implementations/PaymentRepository.cs) uses EF Core and catches `DbUpdateException` to detect idempotency collisions.

## Tests
- Unit tests use xUnit and Moq in [tests/Payments.UnitTests](tests/Payments.UnitTests).
- Avoid breaking idempotency expectations: concurrent calls should not duplicate `Process()` or `UpdateAsync()`.
- Integration and functional tests are placeholders in [tests/Payments.IntegrationTests/PlaceholderTests.cs](tests/Payments.IntegrationTests/PlaceholderTests.cs) and [tests/Payments.FunctionalTests/PlaceholderTests.cs](tests/Payments.FunctionalTests/PlaceholderTests.cs).

## Change guidelines
- Preserve the public API and interface contracts in [src/Payments.Api/Infrastructure/Interfaces](src/Payments.Api/Infrastructure/Interfaces) and [src/Payments.Api/Service/Interfaces](src/Payments.Api/Service/Interfaces).
- Changes that affect behavior must include or update tests.
- Avoid reformatting code outside the scope.
- When adding dependencies, update [src/Payments.Api/Payments.Api.csproj](src/Payments.Api/Payments.Api.csproj) and test projects as needed.
