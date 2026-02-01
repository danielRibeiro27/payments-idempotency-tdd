using Payments.Api.Service.Interfaces;
using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Domain.Implementations;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Globalization;

namespace Payments.Api.Service.Implementations;

// Didn't create a UseCase layer since it's a simple CRUD application.
// The service is specific to the domain and orchestrates domain operations.
// In this scenario, it's unnecessary to create another layer of abstraction.
public class PaymentService (IPaymentRepository paymentRepository, IPaymentGateway paymentGateway) : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly IPaymentGateway _paymentGateway = paymentGateway;
    public async Task<PaymentIntent> CreatePaymentAsync(PaymentIntent paymentIntent)
    {
        if (!paymentIntent.IsValid())
        {
            paymentIntent.Status = Status.Invalid;
            return paymentIntent;
        }

        (bool added, PaymentIntent? p) = await _paymentRepository.GetOrAddByIdempotencyKey(paymentIntent);
        if (!added && p != null)
        {
            // Same idempotency key, check payload hash
            var existingHash = Hasher.ComputeSha256Hash($"{p.Amount.ToString(CultureInfo.InvariantCulture)}|{p.Currency.ToUpperInvariant()}");
            var incomingHash = Hasher.ComputeSha256Hash($"{paymentIntent.Amount.ToString(CultureInfo.InvariantCulture)}|{paymentIntent.Currency.ToUpperInvariant()}");

            if (existingHash != incomingHash)
                throw new InvalidOperationException("A payment with the same idempotency key and different payload already exists.");
            return p;
        }

        // Call PSP/Gateway
        // Not necessary to do it async since it's already consuming from a queue.
        // We should get immediate response from PSP/Gateway about the payment status and inform the client accordingly.
        try{
            string response = await _paymentGateway.Process(paymentIntent);
            paymentIntent.Status = response == "Success" ? Status.Completed : Status.Failed; 
        }
        catch {
            // Call retry logic here for PSP/Gateway with fallback option (omitted for brevity)
        }

        return await _paymentRepository.UpdateAsync(paymentIntent);
    }

    public async Task<PaymentIntent?> GetPaymentByIdAsync(Guid id)
    {
        return await _paymentRepository.GetByIdAsync(id);
    }
}
