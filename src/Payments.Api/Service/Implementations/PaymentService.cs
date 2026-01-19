using Payments.Api.Service.Interfaces;
using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Domain.Interfaces;

namespace Payments.Api.Service.Implementations;

public class PaymentService (IPaymentRepository paymentRepository): IPaymentService 
{
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    public async Task<IPayment> CreatePaymentAsync(IPayment payment)
    {
        return await _paymentRepository.AddAsync(payment);
    }

    public async Task<IPayment?> GetPaymentByIdAsync(Guid id)
    {
        return await _paymentRepository.GetByIdAsync(id);
    }
}
