using Payments.Api.Domain;

namespace Payments.Api.Service.Interfaces;

public interface IPaymentService
{
    Task<Payment?> GetPaymentByIdAsync(Guid id);
    Task<Payment> CreatePaymentAsync(Payment payment);
}
