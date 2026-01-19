using Payments.Api.Domain.Interfaces;

namespace Payments.Api.Domain.Implementations;

public class Payment(decimal amount, string currency, string idempotencyKey) : IPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal Amount { get; set; } = amount;
    public string Currency { get; set; } = currency; //enum preferred
    public string Status { get; set; } = "Pending"; //enum preferred
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string IdempotencyKey { get; set; } = idempotencyKey;
}
