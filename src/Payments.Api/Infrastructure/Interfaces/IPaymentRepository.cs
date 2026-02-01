using Payments.Api.Domain.Implementations;
namespace Payments.Api.Infrastructure.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentIntent?> GetByIdAsync(Guid id);
    Task<PaymentIntent> UpdateAsync(PaymentIntent payment);
    Task<(bool added, PaymentIntent? p)> GetOrAddByIdempotencyKey(PaymentIntent payment);
}
