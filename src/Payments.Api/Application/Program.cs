using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payments.Api.Domain;
using Payments.Api.Infrastructure;
using Payments.Api.Infrastructure.Implementations;
using Payments.Api.Infrastructure.Interfaces;
using Payments.Api.Service.Implementations;
using Payments.Api.Service.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddDbContext<PaymentsDbContext>(options => options.UseSqlite("Data Source=memory:payments.db"));

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi(); // Cria o endpoint /openapi/v1.json

using(var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();

// Minimal API endpoints
app.MapGet("/api/payments/{id:guid}", async (Guid id, [FromServices]IPaymentService paymentService) =>
{
    var payment = await paymentService.GetPaymentByIdAsync(id);
    if (payment == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(payment);
}).WithName("GetPayment");

app.MapPost("/api/payments", async (Payment payment, [FromServices]IPaymentService paymentService) =>
{
    try
    {
        var createdPayment = await paymentService.CreatePaymentAsync(payment);
        return Results.CreatedAtRoute("GetPayment", new { id = createdPayment.Id }, createdPayment);
    }
    catch (System.Data.DBConcurrencyException)
    {
        return Results.Conflict("A payment with the same idempotency key already exists.");
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(ex.Message);
    }
}).WithName("CreatePayment");

app.Run();
