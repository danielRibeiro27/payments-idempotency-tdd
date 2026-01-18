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
        var newPayment = new Payment
        {
            Id = Guid.NewGuid(),
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status,
            CreatedAt = DateTime.UtcNow
        };
        return await _repository.AddAsync(newPayment);
    }
}
