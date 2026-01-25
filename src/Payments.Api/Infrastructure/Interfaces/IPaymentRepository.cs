using Payments.Api.Domain.Implementations;
namespace Payments.Api.Infrastructure.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment> AddAsync(Payment payment);
}
