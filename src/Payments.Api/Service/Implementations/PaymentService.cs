using Payments.Api.Service.Interfaces;
using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Domain.Implementations;

namespace Payments.Api.Service.Implementations;

//didnt created a useCase layer since its a simple CRUD application
//the service its specific for the domain and orchestrates domain operations 
//in that scenario its unecessary to create another layer of abstraction
public class PaymentService (IPaymentRepository paymentRepository) : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    public async Task<Payment> CreatePaymentAsync(Payment payment)
    {
        //call PSP/Gateway here (omitted for brevity), assuming success
        //not necessary to do it async since its already consuming a from a queue
        //we should get immediate response from PSP/Gateway about the payment status
        //and inform the client accordingly
        payment.Status = payment.IsValid() ? Status.Completed : Status.Failed; //update status based on PSP/Gateway response
        return await _paymentRepository.AddAsync(payment);
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid id)
    {
        return await _paymentRepository.GetByIdAsync(id);
    }
}
