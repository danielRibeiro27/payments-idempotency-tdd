using Microsoft.AspNetCore.Http.HttpResults;

namespace Payments.Api.Domain.Implementations;

public class Payment(decimal amount, string currency, string idempotencyKey)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal Amount { get; set; } = amount;
    public string Currency { get; set; } = currency;
    public Status Status { get; set; } = Status.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string IdempotencyKey { get; set; } = idempotencyKey;

    public bool IsValid()
    {
        if(Id == Guid.Empty) return false;
        if(Amount == 0) return false; //allows negative amounts for refunds
        if(string.IsNullOrWhiteSpace(Currency) || Currency.Length != 3) return false;
        if(CreatedAt > DateTime.UtcNow || CreatedAt == DateTime.MinValue) return false;
        if(string.IsNullOrWhiteSpace(IdempotencyKey)) return false;
        return true;
    }
}
