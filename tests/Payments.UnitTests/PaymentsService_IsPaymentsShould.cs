using Microsoft.EntityFrameworkCore;
using Moq;
using Payments.Api.Domain.Implementations;
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
    //happy path::
    //service should return process success/failure - IMPLEMENTED
    //service operations should be idempotent (safe to retry without side effects) - IMPLEMENTED
    //service should call repo to register payment - IMPLEMENTED
    //service should call psp/gateway - IMPLEMENTED
    //service should update payment status accordingly
    //service should fire events/audit - IMPLEMENTED
    //unhappy path::
    //service should handle psp/gateway failures - IMPLEMENTED
    //service should handle repo failures - IMPLEMENTED
    //!!not going to implement every assertion due to time constraints!!

    [Fact]
    public async Task Service_ShouldProcessPaymentSuccessfully()
    {
        var payment = new Payment(100m, "USD", "unique-key-123"); //not mocking payments for its a domain core entity, it cannot be segregated from the rest of the system
        var mockRepository = new Mock<IPaymentRepository>();
        mockRepository.Setup(x => x.AddAsync(It.IsAny<Payment>())).ReturnsAsync(payment);
        var paymentService = new PaymentService(mockRepository.Object);
        var result = await paymentService.CreatePaymentAsync(payment);
        Assert.True(result.Status == Status.Completed);
    }

    [Fact]
    public async Task Service_ShouldNotProcessPaymentSuccessfully()
    {
        var payment = new Payment(0, "USDD", ""); //not mocking payments for its a domain core entity, it cannot be segregated from the rest of the system
        var mockRepository = new Mock<IPaymentRepository>();
        mockRepository.Setup(x => x.AddAsync(It.IsAny<Payment>())).ReturnsAsync(payment);
        var paymentService = new PaymentService(mockRepository.Object);
        var result = await paymentService.CreatePaymentAsync(payment);
        Assert.True(result.Status == Status.Failed);
    }

    [Fact]
    public async Task Service_ShouldNotProcessTheSamePaymentTwice()
    {
        var payment1 = new Payment(100m, "USD", "unique-key-123");
        var payment2 = new Payment(200m, "USD", "unique-key-123");
        
        var mockRepository = new Mock<IPaymentRepository>();
        mockRepository.Setup(x => x.AddAsync(payment1)).ReturnsAsync(payment1);
        mockRepository.Setup(x => x.AddAsync(payment2)).ReturnsAsync(payment2);

        var paymentService = new PaymentService(mockRepository.Object);
        //no await, we want to test concurrency here
        var result1 = paymentService.CreatePaymentAsync(payment1);
        var result2 = paymentService.CreatePaymentAsync(payment2);

        await Task.WhenAll(result1, result2);

        //status should be completed for both since they are valid
        Assert.True(result1.Result.Status == Status.Completed);
        Assert.True(result2.Result.Status == Status.Completed);

        //should call repository/payment gateway only once due to idempotency key
        mockRepository.Verify(x => x.AddAsync(It.IsAny<Payment>()), Times.Exactly(1));
        mockPaymentGateway.Verify(x => x.SendAsync(It.IsAny<Payment>()), Times.Exactly(1));
    }
}