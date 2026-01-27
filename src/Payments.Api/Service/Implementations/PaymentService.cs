using System.Collections.Concurrent;
using Payments.Api.Service.Interfaces;
using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Domain.Implementations;

namespace Payments.Api.Service.Implementations;

/// <summary>
/// Service for handling payment operations with idempotency and gateway integration.
/// Uses a combination of in-memory locks and database checks for idempotency.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    
    // Thread-safe dictionary for in-flight request tracking (prevents race conditions)
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _idempotencyLocks = new();

    public PaymentService(IPaymentRepository paymentRepository, IPaymentGateway paymentGateway)
    {
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
    }

    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        // Get or create a lock for this idempotency key
        var lockObj = _idempotencyLocks.GetOrAdd(payment.IdempotencyKey, _ => new SemaphoreSlim(1, 1));
        
        await lockObj.WaitAsync();
        try
        {
            // Check if payment with this idempotency key already exists
            var existingPayment = await _paymentRepository.GetByIdempotencyKeyAsync(payment.IdempotencyKey);
            if (existingPayment != null)
            {
                // Return existing payment (idempotent behavior)
                return existingPayment;
            }

            // Validate and call PSP/Gateway
            if (payment.IsValid())
            {
                var gatewaySuccess = await _paymentGateway.SendAsync(payment);
                payment.Status = gatewaySuccess ? Status.Completed : Status.Failed;
            }
            else
            {
                payment.Status = Status.Failed;
            }

            // Persist the payment
            return await _paymentRepository.AddAsync(payment);
        }
        finally
        {
            lockObj.Release();
            // Clean up lock after some time to prevent memory leaks
            // In production, consider using a distributed lock with TTL
        }
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid id)
    {
        return await _paymentRepository.GetByIdAsync(id);
    }

    public async Task<Payment?> GetPaymentByIdempotencyKeyAsync(string idempotencyKey)
    {
        return await _paymentRepository.GetByIdempotencyKeyAsync(idempotencyKey);
    }
}
