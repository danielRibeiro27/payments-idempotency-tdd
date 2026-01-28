using Payments.Api.Domain.Implementations;

namespace Payments.Api.Service.Interfaces;

public interface IPaymentService
{
    Task<Payment?> GetPaymentByIdAsync(Guid id);
    Task<Payment?> GetPaymentByIdempotencyKeyAsync(string idempotencyKey);
    Task<Payment> CreatePaymentAsync(Payment payment);
}
