using Microsoft.EntityFrameworkCore;
using Moq;
using Payments.Api.Domain.Implementations;
using Payments.Api.Domain.Interfaces;
using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Service.Implementations;

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
    //service should return process success/failure
    //service operations should be idempotent (safe to retry without side effects)
    //service should call repo to register payment
    //service should call psp/gateway
    //service should update payment status accordingly
    //service should fire events/audit
    //service should response correct status/events

    private IPayment PaymentBuilder(decimal amount, string currency, string idempotencyKey)
    {
        var mockPayment = new Mock<IPayment>();
        mockPayment.Setup(p => p.Amount).Returns(amount);
        mockPayment.Setup(p => p.Currency).Returns(currency);
        mockPayment.Setup(p => p.IdempotencyKey).Returns(idempotencyKey);
        return mockPayment.Object;
    }


    [Fact]
    public async Task Service_ShouldProcessPaymentSuccessfully()
    {
        var payment = PaymentBuilder(100m, "USD", "unique-key-123");
        var mockRepository = new Mock<IPaymentRepository>();
        mockRepository.Setup(x => x.AddAsync(It.IsAny<IPayment>())).ReturnsAsync(payment);
        var paymentService = new PaymentService(mockRepository.Object);
        var result = await paymentService.CreatePaymentAsync(payment);
        Assert.True(result.Status == "Success");
    }
}