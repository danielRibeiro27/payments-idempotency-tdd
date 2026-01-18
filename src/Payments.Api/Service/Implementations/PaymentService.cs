using Payments.Api.Service.Interfaces;
using Payments.Api.Domain;

namespace Payments.Api.Service.Implementations;

public class PaymentService : IPaymentService
{
    public Task<Payment> CreatePaymentAsync(Payment payment)
    {
        throw new NotImplementedException();
    }

    public Task<Payment?> GetPaymentByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}
