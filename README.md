# Payments Idempotency TDD API

A robust, production-ready Payment API built with ASP.NET Core, demonstrating idempotent payment processing with comprehensive test coverage using Test-Driven Development (TDD).

## 🎯 Problem Statement

In fintech applications, payment processing must be:
- **Reliable**: Payments should not be lost or duplicated
- **Idempotent**: Retrying the same request should be safe and produce the same result
- **Testable**: Business logic should be thoroughly validated through automated tests

This project addresses these challenges by implementing an idempotent payment API with proper retry handling.

## 🏗️ Architecture Overview

```
src/
├── Payments.Api/
│   ├── Application/          # API entry point and configuration
│   │   └── Program.cs        # Minimal API endpoints
│   ├── Domain/               # Business entities
│   │   └── Payment.cs        # Payment domain model
│   ├── Infrastructure/       # Data access layer
│   │   ├── Implementations/
│   │   │   └── PaymentRepository.cs
│   │   └── Interfaces/
│   │       └── IPaymentRepository.cs
│   └── Service/              # Business logic layer
│       ├── Implementations/
│       │   ├── PaymentService.cs
│       │   └── RetryService.cs
│       └── Interfaces/
│           ├── IPaymentService.cs
│           └── IRetryService.cs
tests/
├── Payments.UnitTests/       # Service-level unit tests
├── Payments.IntegrationTests/ # API endpoint tests
└── Payments.FunctionalTests/ # End-to-end scenarios
```

## 🛠️ Tech Stack

- **Framework**: .NET 10 / ASP.NET Core Minimal APIs
- **Database**: SQLite (with EF Core) - easily swappable
- **Testing**: xUnit, Moq, WebApplicationFactory
- **Architecture**: Clean Architecture with Repository Pattern

## ✨ Key Reliability Features

### Idempotency
- Every payment request requires an `idempotencyKey`
- Duplicate requests (same key) return the original payment
- Prevents double-charging customers during network issues

### Retry Handling
- Built-in retry service with exponential backoff
- Only retries transient failures (timeouts, network errors)
- Safe to use with idempotent operations

### Database Constraints
- Unique index on `IdempotencyKey` for database-level protection
- Ensures no duplicate payments even under race conditions

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run the API

```bash
cd src/Payments.Api
dotnet run
```

The API will start at `https://localhost:5001` (or HTTP on port 5000).

### Build

```bash
dotnet build
```

### Run Tests

```bash
# Run all tests
dotnet test

# Run specific test projects
dotnet test tests/Payments.UnitTests
dotnet test tests/Payments.IntegrationTests
dotnet test tests/Payments.FunctionalTests
```

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [API_USAGE.md](./API_USAGE.md) | API endpoints, request/response formats |
| [IDEMPOTENCY.md](./IDEMPOTENCY.md) | How idempotency works in this API |
| [RETRIES.md](./RETRIES.md) | Retry behavior and configuration |
| [RED_GREEN_REFACTOR.md](./RED_GREEN_REFACTOR.md) | TDD process documentation |

## 🧪 Test Coverage

- **25 Unit Tests**: PaymentService and RetryService logic
- **7 Integration Tests**: API endpoint behavior
- **6 Functional Tests**: End-to-end payment scenarios

## 📝 License

This project is for educational and demonstration purposes.

---

*Built with ❤️ using Test-Driven Development*
