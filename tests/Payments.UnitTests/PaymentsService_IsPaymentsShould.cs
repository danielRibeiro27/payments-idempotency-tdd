using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Moq;
using Payments.Api.Domain.Implementations;
using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Service.Implementations;

namespace Payments.UnitTests;

public class PaymentsService_IsPaymentsShould
{
    [Fact]
    public async Task Service_ShouldProcessPaymentSuccessfully()
    {
        var payment = new PaymentIntent(100m, "USD", Guid.NewGuid()); // Not mocking PaymentIntent as it's a core domain entity
        var mockRepository = new Mock<IPaymentRepository>();
        mockRepository.Setup(x => x.UpdateAsync(It.IsAny<PaymentIntent>())).ReturnsAsync(payment);
        mockRepository.Setup(x => x.GetOrAddByIdempotencyKey(It.IsAny<PaymentIntent>())).ReturnsAsync((true, payment));

        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(x => x.Process(It.IsAny<PaymentIntent>())).ReturnsAsync("Success");

        var paymentService = new PaymentService(mockRepository.Object, mockGateway.Object);
        var result = await paymentService.CreatePaymentAsync(payment);
        Assert.True(result.Status == Status.Completed);
    }

    [Fact]
    public async Task Service_ShouldNotProcessPaymentSuccessfully()
    {
        var payment = new PaymentIntent(0, "USDD", Guid.NewGuid()); // Not mocking PaymentIntent as it's a core domain entity
        var mockRepository = new Mock<IPaymentRepository>();
        mockRepository.Setup(x => x.UpdateAsync(It.IsAny<PaymentIntent>())).ReturnsAsync(payment);

        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(x => x.Process(It.IsAny<PaymentIntent>())).ReturnsAsync("Success");

        var paymentService = new PaymentService(mockRepository.Object, mockGateway.Object);
        var result = await paymentService.CreatePaymentAsync(payment);
        Assert.True(result.Status == Status.Invalid);
    }

    [Fact]
    public async Task Service_Idempotency_SamePayload_ShouldReturnValidStatus_And_ShouldNotCallRepoOrGatewayMultipleTimes()
    {
        Guid idempotencyKey1 = Guid.NewGuid();
        Guid idempotencyKey2 = idempotencyKey1; //Same key to simulate retry
        var payment1 = new PaymentIntent(100m, "USD", idempotencyKey1);
        var payment2 = new PaymentIntent(100m, "USD", idempotencyKey2); // Same payload with same idempotency key (retry simulation)
        
        var store = new ConcurrentDictionary<Guid, PaymentIntent>();

        // Repo add method stores in the concurrent dictionary
        var mockRepository = new Mock<IPaymentRepository>();
        mockRepository
            .Setup(x => x.UpdateAsync(It.IsAny<PaymentIntent>()))
            .ReturnsAsync((PaymentIntent p) =>
            {
                store.TryUpdate(p.IdempotencyKey, p, store[p.IdempotencyKey]);
                return p;
            });

        // GetOrAddByIdempotencyKey gets values from the concurrent dictionary
        mockRepository
            .Setup(x => x.GetOrAddByIdempotencyKey(It.IsAny<PaymentIntent>()))
            .ReturnsAsync((PaymentIntent p) => 
                store.TryAdd(p.IdempotencyKey, p) ? (true, p) : (false, store[p.IdempotencyKey]));

        // Mock gateway
        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(x => x.Process(It.IsAny<PaymentIntent>()))
            .ReturnsAsync("Success");

        var paymentService = new PaymentService(mockRepository.Object, mockGateway.Object);

        // Concurrent calls
        Task<PaymentIntent> task1 = Task.Run(() => paymentService.CreatePaymentAsync(payment1));
        Task<PaymentIntent> task2 = Task.Run(() => paymentService.CreatePaymentAsync(payment2));

        await Task.WhenAll(task1, task2);

        // Either one completed, the other pending (not processed again) or both completed (race condition)
        Assert.True((task1?.Result.Status == Status.Completed && task2?.Result.Status == Status.Pending) || // 202: client should poll for status
                    (task2?.Result.Status == Status.Completed && task1?.Result.Status == Status.Pending) || // 202: client should poll for status
                    task1?.Result.Status == Status.Completed && task2?.Result.Status == Status.Completed); 
                    
        // Should call update and process only once
        mockRepository.Verify(x => x.UpdateAsync(It.IsAny<PaymentIntent>()), Times.Exactly(1));
        mockGateway.Verify(x => x.Process(It.IsAny<PaymentIntent>()), Times.Exactly(1));
    }
}