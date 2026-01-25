namespace Payments.Api.Domain.Implementations;

public enum Status
{
    Undefined,
    Pending,
    Completed,
    Failed,
    Refunded
}