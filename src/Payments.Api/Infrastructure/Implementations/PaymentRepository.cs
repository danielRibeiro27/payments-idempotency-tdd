using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Domain.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Payments.Api.Infrastructure.Implementations;

public class PaymentRepository(PaymentsDbContext context) : IPaymentRepository
{
    private readonly PaymentsDbContext _context = context;

    public async Task<PaymentIntent?> GetByIdAsync(Guid id)
    {
        return await _context.Payments.FindAsync(id);
    }

    public async Task<PaymentIntent> UpdateAsync(PaymentIntent payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<(bool added, PaymentIntent? p)> GetOrAddByIdempotencyKey(PaymentIntent payment)
    {
        try
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return (true, payment);
        }
        catch (DbUpdateException)
        {
            var existingPayment = await _context.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == payment.IdempotencyKey);
            return (false, existingPayment);
        }
    }
}
