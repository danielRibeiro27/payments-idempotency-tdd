using Moq;
using Payments.Api.Domain;
using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Service.Implementations;

namespace Payments.UnitTests;

/// <summary>
/// Unit tests for PaymentService covering create, get, and idempotency behavior.
/// </summary>
public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _mockRepository;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _mockRepository = new Mock<IPaymentRepository>();
        _paymentService = new PaymentService(_mockRepository.Object);
    }

    #region CreatePaymentAsync Tests

    [Fact]
    public async Task CreatePaymentAsync_WithValidPayment_ShouldReturnCreatedPayment()
    {
        // Arrange
        var payment = new Payment
        {
            Amount = 100.00m,
            Currency = "USD",
            IdempotencyKey = "test-key-123"
        };

        _mockRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()))
            .ReturnsAsync((Payment?)null);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        // Act
        var result = await _paymentService.CreatePaymentAsync(payment);

        // Assert: payment should be returned with generated Id
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(100.00m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task CreatePaymentAsync_WithValidPayment_ShouldSetDefaultStatus()
    {
        // Arrange
        var payment = new Payment
        {
            Amount = 50.00m,
            Currency = "EUR",
            IdempotencyKey = "test-key-456"
        };

        _mockRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()))
            .ReturnsAsync((Payment?)null);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        // Act
        var result = await _paymentService.CreatePaymentAsync(payment);

        // Assert: status should default to "Pending"
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public async Task CreatePaymentAsync_WithValidPayment_ShouldSetCreatedAtTimestamp()
    {
        // Arrange
        var payment = new Payment
        {
            Amount = 75.00m,
            Currency = "GBP",
            IdempotencyKey = "test-key-789"
        };

        _mockRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()))
            .ReturnsAsync((Payment?)null);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        var beforeCreation = DateTime.UtcNow;

        // Act
        var result = await _paymentService.CreatePaymentAsync(payment);

        var afterCreation = DateTime.UtcNow;

        // Assert: CreatedAt should be set to a recent UTC time
        Assert.True(result.CreatedAt >= beforeCreation && result.CreatedAt <= afterCreation);
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldCallRepository()
    {
        // Arrange
        var payment = new Payment
        {
            Amount = 200.00m,
            Currency = "CAD",
            IdempotencyKey = "test-key-repo"
        };

        _mockRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()))
            .ReturnsAsync((Payment?)null);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        // Act
        await _paymentService.CreatePaymentAsync(payment);

        // Assert: AddAsync should be called exactly once
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public async Task CreatePaymentAsync_WithExistingIdempotencyKey_ShouldReturnExistingPayment()
    {
        // Arrange
        var existingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 100.00m,
            Currency = "USD",
            Status = "Completed",
            IdempotencyKey = "duplicate-key",
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var newPayment = new Payment
        {
            Amount = 100.00m,
            Currency = "USD",
            IdempotencyKey = "duplicate-key"
        };

        _mockRepository.Setup(r => r.GetByIdempotencyKeyAsync("duplicate-key"))
            .ReturnsAsync(existingPayment);

        // Act
        var result = await _paymentService.CreatePaymentAsync(newPayment);

        // Assert: should return the existing payment, not create a new one
        Assert.Equal(existingPayment.Id, result.Id);
        Assert.Equal(existingPayment.Status, result.Status);
        Assert.Equal(existingPayment.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public async Task CreatePaymentAsync_WithExistingIdempotencyKey_ShouldNotCallAddAsync()
    {
        // Arrange
        var existingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = 100.00m,
            Currency = "USD",
            Status = "Completed",
            IdempotencyKey = "existing-key",
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdempotencyKeyAsync("existing-key"))
            .ReturnsAsync(existingPayment);

        var newPayment = new Payment
        {
            Amount = 100.00m,
            Currency = "USD",
            IdempotencyKey = "existing-key"
        };

        // Act
        await _paymentService.CreatePaymentAsync(newPayment);

        // Assert: AddAsync should NOT be called for duplicate idempotency key
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task CreatePaymentAsync_WithDifferentIdempotencyKeys_ShouldCreateBothPayments()
    {
        // Arrange
        var payment1 = new Payment
        {
            Amount = 100.00m,
            Currency = "USD",
            IdempotencyKey = "key-1"
        };

        var payment2 = new Payment
        {
            Amount = 200.00m,
            Currency = "EUR",
            IdempotencyKey = "key-2"
        };

        _mockRepository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>()))
            .ReturnsAsync((Payment?)null);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        // Act
        var result1 = await _paymentService.CreatePaymentAsync(payment1);
        var result2 = await _paymentService.CreatePaymentAsync(payment2);

        // Assert: both payments should be created with different IDs
        Assert.NotEqual(result1.Id, result2.Id);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreatePaymentAsync_WithEmptyIdempotencyKey_ShouldStillCreatePayment()
    {
        // Arrange
        var payment = new Payment
        {
            Amount = 50.00m,
            Currency = "USD",
            IdempotencyKey = ""
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Payment>()))
            .ReturnsAsync((Payment p) => p);

        // Act
        var result = await _paymentService.CreatePaymentAsync(payment);

        // Assert: payment should be created even with empty idempotency key
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    #endregion

    #region GetPaymentByIdAsync Tests

    [Fact]
    public async Task GetPaymentByIdAsync_WithExistingId_ShouldReturnPayment()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var existingPayment = new Payment
        {
            Id = paymentId,
            Amount = 150.00m,
            Currency = "USD",
            Status = "Completed",
            IdempotencyKey = "get-test-key",
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(paymentId))
            .ReturnsAsync(existingPayment);

        // Act
        var result = await _paymentService.GetPaymentByIdAsync(paymentId);

        // Assert: should return the payment with matching ID
        Assert.NotNull(result);
        Assert.Equal(paymentId, result.Id);
        Assert.Equal(150.00m, result.Amount);
    }

    [Fact]
    public async Task GetPaymentByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(nonExistingId))
            .ReturnsAsync((Payment?)null);

        // Act
        var result = await _paymentService.GetPaymentByIdAsync(nonExistingId);

        // Assert: should return null for non-existing payment
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPaymentByIdAsync_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(paymentId))
            .ReturnsAsync((Payment?)null);

        // Act
        await _paymentService.GetPaymentByIdAsync(paymentId);

        // Assert: repository should be called with the correct ID
        _mockRepository.Verify(r => r.GetByIdAsync(paymentId), Times.Once);
    }

    #endregion
}

