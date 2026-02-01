using Payments.Api.Domain.Implementations;

namespace Payments.UnitTests;

public class PaymentsDomain_IsPaymentsShould
{
    [Fact]
    public void PaymentIntent_ShouldBeCreatedSuccessfully()
    {
        Assert.IsType<PaymentIntent>(new PaymentIntent(100, "USD", Guid.NewGuid()));
    }

    [Fact]
    public void Property_Id_ShouldBeCreatedCorrectly()
    {
        var payment = new PaymentIntent(100, "USD", Guid.NewGuid());
        Assert.NotEqual(Guid.Empty, payment.Id);
    }

    [Fact]
    public void Property_Amount_ShouldBeCreatedCorrectly()
    {
        var payment = new PaymentIntent(150.75m, "EUR", Guid.NewGuid());
        Assert.True(payment.Amount != 0);
    }

    [Fact]
    public void Property_Currency_ShouldBeCreatedCorrectly()
    {
        var payment = new PaymentIntent(200, "GBP", Guid.NewGuid());
        Assert.False(string.IsNullOrWhiteSpace(payment.Currency));
    }
    
    [Fact]
    public void Property_Status_ShouldBeCreatedCorrectly()
    {
        var payment = new PaymentIntent(75, "AUD", Guid.NewGuid());
        Assert.False(payment.Status == Status.Undefined);
    }

    [Fact]
    public void Property_CreatedAt_ShouldBeCreatedCorrectly()
    {
        var payment = new PaymentIntent(300, "CAD", Guid.NewGuid());
        Assert.False(payment.CreatedAt == DateTime.MinValue);
    }

    [Fact]
    public void Property_IdempotencyKey_ShouldBeCreatedCorrectly()
    {
        var payment = new PaymentIntent(50, "JPY", Guid.NewGuid());
        Assert.False(payment.IdempotencyKey == Guid.Empty);
    }

    [Fact]
    public void Property_Id_ShouldBeUnique()
    {
        var payment1 = new PaymentIntent(120, "CHF", Guid.NewGuid());
        var payment2 = new PaymentIntent(120, "CHF", Guid.NewGuid());
        Assert.True(payment1.Id != payment2.Id);
    }
}