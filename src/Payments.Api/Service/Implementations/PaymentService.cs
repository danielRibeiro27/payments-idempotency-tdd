using Payments.Api.Service.Interfaces;
using Payments.Api.Domain;
using Payments.Api.Infrastructure.Interfaces;

namespace Payments.Api.Service.Implementations;

public class PaymentService (IPaymentRepository paymentRepository): IPaymentService 
{
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        return await _paymentRepository.AddAsync(payment);
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid id)
    {
        return await _paymentRepository.GetByIdAsync(id);
    }
}
