using Payments.Api.Domain.Implementations;

namespace Payments.UnitTests;

//payment.api expected flow:
// 1. validate req
// 2. register payment (with idempotency)
// 3. calls PSP/Gateway (with idempotency)
// 4. update payment status
// 5. fire events/audit
// 6. response (status, events)

//assertions:
//domain model should be created successfully
//domain model properties should be created correctly
//domain model keys should be unique and valid
//domain model status should be set correctly
//domain model should validate itself correctly

public class PaymentsDomain_IsPaymentsShould
{
    [Fact]
    public void Payment_ShouldBeCreatedSuccessfuly()
    {
        Assert.IsType<Payment>(new Payment(100, "USD", "unique-key-123"));
    }

    [Fact]
    public void Property_Id_ShouldBeCreatedCorrectly()
    {
        var payment = new Payment(100, "USD", "unique-key-123");
        Assert.NotEqual(Guid.Empty, payment.Id);
    }

    [Fact]
    public void Property_Amount_ShouldBeCreatedCorrectly()
    {
        var payment = new Payment(150.75m, "EUR", "idempotency-key-456");
        Assert.True(payment.Amount != 0);
    }

    [Fact]
    public void Property_Currency_ShouldBeCreatedCorrectly()
    {
        var payment = new Payment(200, "GBP", "idempotency-key-789");
        Assert.False(string.IsNullOrWhiteSpace(payment.Currency));
    }
    
    [Fact]
    public void Property_Status_ShouldBeCreatedCorrectly()
    {
        var payment = new Payment(75, "AUD", "idempotency-key-111");
        Assert.False(payment.Status == Status.Undefined);
    }

    [Fact]
    public void Property_CreatedAt_ShouldBeCreatedCorrectly()
    {
        var payment = new Payment(300, "CAD", "unique-key-222");
        Assert.False(payment.CreatedAt == DateTime.MinValue);
    }

    [Fact]
    public void Property_IdempotencyKey_ShouldBeCreatedCorrectly()
    {
        var payment = new Payment(50, "JPY", "unique-key-000");
        Assert.False(string.IsNullOrWhiteSpace(payment.IdempotencyKey));
    }

    [Fact]
    public void Property_Id_ShouldBeUnique()
    {
        var payment1 = new Payment(120, "CHF", "idempotency-key-333");
        var payment2 = new Payment(120, "CHF", "idempotency-key-333");
        Assert.True(payment1.Id != payment2.Id);
    }
}