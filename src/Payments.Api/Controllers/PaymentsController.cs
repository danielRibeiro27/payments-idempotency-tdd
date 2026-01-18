using Microsoft.AspNetCore.Mvc;
using Payments.Domain;
using Payments.Service;

namespace Payments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetPayment(Guid id)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id);
        if (payment == null)
        {
            return NotFound();
        }
        return Ok(payment);
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment([FromBody] Payment payment)
    {
        var createdPayment = await _paymentService.CreatePaymentAsync(payment);
        return CreatedAtAction(nameof(GetPayment), new { id = createdPayment.Id }, createdPayment);
    }
}
