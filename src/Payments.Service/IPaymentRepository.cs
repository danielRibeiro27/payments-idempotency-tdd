using Payments.Domain;

namespace Payments.Service;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment> AddAsync(Payment payment);
}
