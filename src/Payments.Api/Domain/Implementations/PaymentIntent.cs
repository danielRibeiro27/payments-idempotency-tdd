using Microsoft.AspNetCore.Http.HttpResults;

namespace Payments.Api.Domain.Implementations;

//expand payment intent to customer/payer objects in future iterations
public class PaymentIntent(decimal amount, string currency, Guid idempotencyKey)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal Amount { get; set; } = amount;
    public string Currency { get; set; } = currency;
    public Status Status { get; set; } = Status.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid IdempotencyKey { get; set; } = idempotencyKey; // Could be a separate DB entity in future iterations

    public bool IsValid()
    {
        if(Id == Guid.Empty) return false;
        if(Amount == 0) return false; // Allows negative amounts for refunds
        if(string.IsNullOrWhiteSpace(Currency.ToUpperInvariant()) || Currency.ToUpperInvariant().Length != 3) return false;
        if(CreatedAt > DateTime.UtcNow || CreatedAt == DateTime.MinValue) return false;
        if(IdempotencyKey == Guid.Empty) return false;
        return true;
    }
}
