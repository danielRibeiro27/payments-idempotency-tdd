using Payments.Api.Service.Interfaces;
using Payments.Api.Domain;
using Payments.Api.Infrastructure.Interfaces;

namespace Payments.Api.Service.Implementations;

/// <summary>
/// Service responsible for payment operations with idempotency support.
/// </summary>
public class PaymentService(IPaymentRepository paymentRepository) : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository = paymentRepository;

    /// <summary>
    /// Creates a new payment with idempotency support.
    /// If a payment with the same idempotency key already exists, returns the existing payment.
    /// </summary>
    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        // Check for existing payment with same idempotency key (idempotency check)
        if (!string.IsNullOrEmpty(payment.IdempotencyKey))
        {
            var existingPayment = await _paymentRepository.GetByIdempotencyKeyAsync(payment.IdempotencyKey);
            if (existingPayment != null)
            {
                // Return existing payment for idempotent behavior
                return existingPayment;
            }
        }

        // Set defaults for new payment
        if (payment.Id == Guid.Empty)
        {
            payment.Id = Guid.NewGuid();
        }

        if (payment.CreatedAt == default)
        {
            payment.CreatedAt = DateTime.UtcNow;
        }

        if (string.IsNullOrEmpty(payment.Status))
        {
            payment.Status = "Pending";
        }

        return await _paymentRepository.AddAsync(payment);
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid id)
    {
        return await _paymentRepository.GetByIdAsync(id);
    }
}
