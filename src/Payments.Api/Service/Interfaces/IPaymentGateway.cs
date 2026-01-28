using Payments.Api.Domain.Implementations;

namespace Payments.Api.Service.Interfaces;

/// <summary>
/// Interface for Payment Service Provider (PSP) / Payment Gateway integration.
/// Handles communication with external payment processors.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Sends a payment request to the external PSP/Gateway.
    /// </summary>
    /// <param name="payment">The payment to process</param>
    /// <returns>True if the payment was processed successfully, false otherwise</returns>
    Task<bool> SendAsync(Payment payment);
}
