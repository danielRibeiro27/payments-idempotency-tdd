# Payments Idempotency TDD API

A production-grade Payment API demonstrating idempotency patterns, retry mechanisms, and comprehensive TDD practices for fintech applications.

## 🎯 Problem Statement

In financial systems, duplicate transactions can have severe consequences - customers may be charged multiple times, accounts can become inconsistent, and trust is eroded. This project demonstrates how to build a reliable payment system that:

- **Prevents duplicate payments** through idempotency key-based processing
- **Handles transient failures** with intelligent retry mechanisms  
- **Maintains data consistency** even under concurrent requests
- **Follows TDD practices** ensuring high test coverage and reliability

## 🏗 Architecture

```
Payments.Api/
├── Application/          # Entry point, DI configuration, API endpoints
├── Domain/              
│   └── Implementations/  # Core domain models (Payment, Status)
├── Service/
│   ├── Interfaces/       # IPaymentService, IPaymentGateway, IRetryPolicy
│   └── Implementations/  # Business logic with idempotency handling
└── Infrastructure/
    ├── Interfaces/       # IPaymentRepository
    └── Implementations/  # Data access layer

tests/
├── Payments.UnitTests/       # Domain & service unit tests
├── Payments.IntegrationTests/# API endpoint integration tests
└── Payments.FunctionalTests/ # End-to-end scenarios (TBD)
```

The architecture follows clean architecture principles with clear separation:
- **Domain Layer**: Pure business logic and entities (no external dependencies)
- **Service Layer**: Orchestrates business operations, handles idempotency and retries
- **Infrastructure Layer**: Database access and external integrations
- **Application Layer**: API configuration and HTTP endpoint definitions

## 🛡 Key Reliability Features

### Idempotency
Every payment request requires an idempotency key. The system guarantees:
- Same key + same request = same response (no duplicate payments)
- Concurrent requests with same key result in exactly one payment
- Thread-safe locking prevents race conditions

### Retry Mechanism
Transient failures are handled with exponential backoff:
- 3 retries by default (4 total attempts)
- Delays: 100ms → 200ms → 400ms (with 25% jitter)
- Only network/timeout errors trigger retries
- Business logic failures fail immediately

See [IDEMPOTENCY.md](IDEMPOTENCY.md) and [RETRIES.md](RETRIES.md) for detailed documentation.

## 💻 Tech Stack

- **.NET 10** - Latest LTS runtime
- **ASP.NET Core Minimal APIs** - Lightweight HTTP endpoints
- **Entity Framework Core** - ORM with SQLite (in-memory for tests)
- **xUnit** - Testing framework
- **Moq** - Mocking library

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK

### Run the API
```bash
cd src/Payments.Api
dotnet run
```

The API will be available at `https://localhost:5001` (or `http://localhost:5000`).

### Run Tests
```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/Payments.UnitTests

# Integration tests only
dotnet test tests/Payments.IntegrationTests
```

### API Documentation
When running in Development mode, OpenAPI spec is available at `/openapi/v1.json`.

## 📊 Test Coverage

- **24 Unit Tests**: Domain validation, service logic, idempotency, retry policy
- **7 Integration Tests**: API endpoints, idempotency behavior, concurrent requests

## 📚 Documentation

- [API_USAGE.md](API_USAGE.md) - Endpoint reference and examples
- [IDEMPOTENCY.md](IDEMPOTENCY.md) - Idempotency semantics and implementation
- [RETRIES.md](RETRIES.md) - Retry behavior and safety guarantees
- [RED_GREEN_REFACTOR.md](RED_GREEN_REFACTOR.md) - TDD process documentation

## 📝 License

MIT
