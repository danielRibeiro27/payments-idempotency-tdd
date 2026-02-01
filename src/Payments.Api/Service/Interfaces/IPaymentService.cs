using Payments.Api.Domain.Implementations;

namespace Payments.Api.Service.Interfaces;

public interface IPaymentService
{
    Task<PaymentIntent?> GetPaymentByIdAsync(Guid id);
    Task<PaymentIntent> CreatePaymentAsync(PaymentIntent payment);
}
