using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Domain;

namespace Payments.Api.Infrastructure.Implementations;

public class PaymentRepository(PaymentsDbContext context) : IPaymentRepository
{
    private readonly PaymentsDbContext _context = context;

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await _context.Payments.FindAsync(id);
    }

    public async Task<Payment> AddAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }
}
