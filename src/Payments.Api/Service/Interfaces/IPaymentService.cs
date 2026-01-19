using Payments.Api.Domain.Interfaces;

namespace Payments.Api.Service.Interfaces;

public interface IPaymentService
{
    Task<IPayment?> GetPaymentByIdAsync(Guid id);
    Task<IPayment> CreatePaymentAsync(IPayment payment);
}
