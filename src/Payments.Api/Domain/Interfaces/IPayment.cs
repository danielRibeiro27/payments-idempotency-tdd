namespace Payments.Api.Domain.Interfaces;

public interface IPayment
{
    Guid Id { get; }
    decimal Amount { get; }
    string Currency { get; }
    string IdempotencyKey { get; }
}