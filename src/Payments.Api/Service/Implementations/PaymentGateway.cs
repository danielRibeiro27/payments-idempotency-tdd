using Payments.Api.Domain.Implementations;
using Payments.Api.Service.Interfaces;

namespace Payments.Api.Service.Implementations;

/// <summary>
/// Default implementation of the Payment Gateway with retry support.
/// In a real scenario, this would integrate with a PSP like Stripe, Adyen, etc.
/// </summary>
public class PaymentGateway : IPaymentGateway
{
    private readonly IRetryPolicy _retryPolicy;

    public PaymentGateway(IRetryPolicy retryPolicy)
    {
        _retryPolicy = retryPolicy;
    }

    public async Task<bool> SendAsync(Payment payment)
    {
        // Use retry policy to handle transient failures
        // Safe because idempotency is guaranteed at the payment service level
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            // Simulated PSP call - in production this would make HTTP calls to the PSP
            // For demo purposes, always returns success if payment is valid
            await Task.Delay(1); // Simulate network latency
            return payment.IsValid();
        });
    }
}

