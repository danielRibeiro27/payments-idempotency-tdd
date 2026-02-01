using Payments.Api.Domain.Implementations;
using Payments.Api.Infrastructure.Interfaces;

namespace Payments.Api.Infrastructure.Implementations;
public class PaymentGateway : IPaymentGateway
{
    public async Task<string> Process(PaymentIntent paymentIntent)
    {
        //Simulate call to PSP/Gateway
        await Task.Delay(100); //Simulating network delay
        //For simplicity, we assume all payments with amount > 0 are successful
        return paymentIntent.Amount > 0 ? "Success" : "Failure";
    }
}
