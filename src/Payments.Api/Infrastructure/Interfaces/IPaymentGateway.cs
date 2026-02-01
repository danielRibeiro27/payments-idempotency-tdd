namespace Payments.Api.Infrastructure.Interfaces
{
    using Payments.Api.Domain.Implementations;

    public interface IPaymentGateway
    {
        Task<string> Process(PaymentIntent paymentIntent);
    }
}