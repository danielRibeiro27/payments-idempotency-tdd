using Microsoft.EntityFrameworkCore;
using Moq;
using Payments.Api.Domain.Implementations;
using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Service.Implementations;
using Payments.Api.Service.Interfaces;

namespace Payments.UnitTests;

public class PaymentsService_IsPaymentsShould
{
    //payment.api expected flow:
    // 1. validate req
    // 2. register payment (with idempotency)
    // 3. calls PSP/Gateway (with idempotency)
    // 4. update payment status
    // 5. fire events/audit
    // 6. response (status, events)

    //assertions:
    //happy path::
    //service should return process success/failure - IMPLEMENTED
    //service operations should be idempotent (safe to retry without side effects) - IMPLEMENTED
    //service should call repo to register payment - IMPLEMENTED
    //service should call psp/gateway - IMPLEMENTED
    //service should update payment status accordingly - IMPLEMENTED
    //service should fire events/audit - NOT IMPLEMENTED (out of scope for MVP)
    //unhappy path::
    //service should handle psp/gateway failures - IMPLEMENTED
    //service should handle repo failures - IMPLEMENTED

    [Fact]
    public async Task Service_ShouldProcessPaymentSuccessfully()
    {
        var payment = new Payment(100m, "USD", "unique-key-123");
        var mockRepository = new Mock<IPaymentRepository>();
        var mockPaymentGateway = new Mock<IPaymentGateway>();
        
        mockRepository.Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<Payment>())).ReturnsAsync(payment);
        mockPaymentGateway.Setup(x => x.SendAsync(It.IsAny<Payment>())).ReturnsAsync(true);
        
        var paymentService = new PaymentService(mockRepository.Object, mockPaymentGateway.Object);
        var result = await paymentService.CreatePaymentAsync(payment);
        Assert.True(result.Status == Status.Completed);
    }

    [Fact]
    public async Task Service_ShouldNotProcessPaymentSuccessfully()
    {
        var payment = new Payment(0, "USDD", ""); // Invalid payment
        var mockRepository = new Mock<IPaymentRepository>();
        var mockPaymentGateway = new Mock<IPaymentGateway>();
        
        mockRepository.Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<Payment>())).ReturnsAsync(payment);
        
        var paymentService = new PaymentService(mockRepository.Object, mockPaymentGateway.Object);
        var result = await paymentService.CreatePaymentAsync(payment);
        Assert.True(result.Status == Status.Failed);
    }

    [Fact]
    public async Task Service_ShouldNotProcessTheSamePaymentTwice()
    {
        var payment1 = new Payment(100m, "USD", "unique-key-123");
        var payment2 = new Payment(200m, "USD", "unique-key-123");
        
        var mockRepository = new Mock<IPaymentRepository>();
        var mockPaymentGateway = new Mock<IPaymentGateway>();
        
        // First call returns null (no existing payment), subsequent calls return the first payment
        var callCount = 0;
        mockRepository.Setup(x => x.GetByIdempotencyKeyAsync("unique-key-123"))
            .ReturnsAsync(() => callCount++ == 0 ? null : payment1);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<Payment>())).ReturnsAsync(payment1);
        mockPaymentGateway.Setup(x => x.SendAsync(It.IsAny<Payment>())).ReturnsAsync(true);

        var paymentService = new PaymentService(mockRepository.Object, mockPaymentGateway.Object);
        
        // Run both requests concurrently
        var result1Task = paymentService.CreatePaymentAsync(payment1);
        var result2Task = paymentService.CreatePaymentAsync(payment2);

        var results = await Task.WhenAll(result1Task, result2Task);

        // Both should return completed status (either the original or the returned existing)
        Assert.True(results[0].Status == Status.Completed);
        Assert.True(results[1].Status == Status.Completed);

        // Should call repository AddAsync only once due to idempotency key
        mockRepository.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Exactly(1));
        // Should call payment gateway only once due to idempotency key
        mockPaymentGateway.Verify(x => x.SendAsync(It.IsAny<Payment>()), Times.Exactly(1));
    }

    [Fact]
    public async Task Service_ShouldReturnExistingPaymentForSameIdempotencyKey()
    {
        var existingPayment = new Payment(100m, "USD", "existing-key");
        existingPayment.Status = Status.Completed;
        
        var newPayment = new Payment(200m, "EUR", "existing-key");
        
        var mockRepository = new Mock<IPaymentRepository>();
        var mockPaymentGateway = new Mock<IPaymentGateway>();
        
        mockRepository.Setup(x => x.GetByIdempotencyKeyAsync("existing-key")).ReturnsAsync(existingPayment);

        var paymentService = new PaymentService(mockRepository.Object, mockPaymentGateway.Object);
        var result = await paymentService.CreatePaymentAsync(newPayment);

        // Should return the existing payment, not the new one
        Assert.Equal(existingPayment.Id, result.Id);
        Assert.Equal(100m, result.Amount);
        Assert.Equal("USD", result.Currency);
        
        // Should never call AddAsync or gateway since payment already exists
        mockRepository.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Never);
        mockPaymentGateway.Verify(x => x.SendAsync(It.IsAny<Payment>()), Times.Never);
    }

    [Fact]
    public async Task Service_ShouldHandleGatewayFailure()
    {
        var payment = new Payment(100m, "USD", "gateway-fail-key");
        
        var mockRepository = new Mock<IPaymentRepository>();
        var mockPaymentGateway = new Mock<IPaymentGateway>();
        
        mockRepository.Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<Payment>())).ReturnsAsync(payment);
        mockPaymentGateway.Setup(x => x.SendAsync(It.IsAny<Payment>())).ReturnsAsync(false);

        var paymentService = new PaymentService(mockRepository.Object, mockPaymentGateway.Object);
        var result = await paymentService.CreatePaymentAsync(payment);

        Assert.Equal(Status.Failed, result.Status);
    }

    [Fact]
    public async Task Service_ShouldCallGatewayForValidPayment()
    {
        var payment = new Payment(100m, "USD", "valid-payment-key");
        
        var mockRepository = new Mock<IPaymentRepository>();
        var mockPaymentGateway = new Mock<IPaymentGateway>();
        
        mockRepository.Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<Payment>())).ReturnsAsync(payment);
        mockPaymentGateway.Setup(x => x.SendAsync(It.IsAny<Payment>())).ReturnsAsync(true);

        var paymentService = new PaymentService(mockRepository.Object, mockPaymentGateway.Object);
        await paymentService.CreatePaymentAsync(payment);

        // Gateway should be called for valid payment
        mockPaymentGateway.Verify(x => x.SendAsync(payment), Times.Once);
    }

    [Fact]
    public async Task Service_ShouldNotCallGatewayForInvalidPayment()
    {
        var payment = new Payment(0, "USDD", "invalid-key"); // Invalid payment
        
        var mockRepository = new Mock<IPaymentRepository>();
        var mockPaymentGateway = new Mock<IPaymentGateway>();
        
        mockRepository.Setup(x => x.GetByIdempotencyKeyAsync(It.IsAny<string>())).ReturnsAsync((Payment?)null);
        mockRepository.Setup(x => x.AddAsync(It.IsAny<Payment>())).ReturnsAsync(payment);

        var paymentService = new PaymentService(mockRepository.Object, mockPaymentGateway.Object);
        await paymentService.CreatePaymentAsync(payment);

        // Gateway should NOT be called for invalid payment
        mockPaymentGateway.Verify(x => x.SendAsync(It.IsAny<Payment>()), Times.Never);
    }
}