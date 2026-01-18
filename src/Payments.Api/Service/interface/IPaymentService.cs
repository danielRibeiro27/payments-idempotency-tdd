using Payments.Domain;

namespace Payments.Service;

public interface IPaymentService
{
    Task<Payment?> GetPaymentByIdAsync(Guid id);
    Task<Payment> CreatePaymentAsync(Payment payment);
}
