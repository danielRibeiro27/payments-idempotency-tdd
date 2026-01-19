using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Domain.Interfaces;
using Payments.Api.Domain.Implementations;

namespace Payments.Api.Infrastructure.Implementations;

public class PaymentRepository(PaymentsDbContext context) : IPaymentRepository
{
    private readonly PaymentsDbContext _context = context;

    public async Task<IPayment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments.FindAsync(id);
    }

    public async Task<IPayment> AddAsync(IPayment payment)
    {
        var paymentEntity = payment as Payment ?? throw new InvalidCastException();
        _context.Payments.Add(paymentEntity);
        await _context.SaveChangesAsync();
        return payment;
    }
}
