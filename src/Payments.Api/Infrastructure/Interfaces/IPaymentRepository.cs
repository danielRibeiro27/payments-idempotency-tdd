using Payments.Api.Domain.Interfaces;

namespace Payments.Api.Infrastructure.Interfaces;

public interface IPaymentRepository
{
    Task<IPayment?> GetByIdAsync(Guid id);
    Task<IPayment> AddAsync(IPayment payment);
}
