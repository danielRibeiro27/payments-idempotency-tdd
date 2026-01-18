using Payments.Domain;
using Payments.Service;

namespace Payments.Infrastructure;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;

    public PaymentService(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        payment.Id = Guid.NewGuid();
        payment.CreatedAt = DateTime.UtcNow;
        return await _repository.AddAsync(payment);
    }
}
