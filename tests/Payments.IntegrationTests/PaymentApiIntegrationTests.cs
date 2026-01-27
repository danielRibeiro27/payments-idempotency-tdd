using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payments.Api.Domain;
using Payments.Api.Infrastructure;

namespace Payments.IntegrationTests;

/// <summary>
/// Integration tests for the Payment API endpoints.
/// Uses WebApplicationFactory with in-memory database for fast, isolated tests.
/// </summary>
public class PaymentApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public PaymentApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _baseFactory = factory;
    }

    /// <summary>
    /// Creates a WebApplicationFactory with an isolated in-memory database for this test.
    /// </summary>
    private WebApplicationFactory<Program> CreateFactoryWithDatabase(string databaseName)
    {
        return _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove all existing EF Core DbContext-related services
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<PaymentsDbContext>) ||
                                d.ServiceType == typeof(PaymentsDbContext) ||
                                d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing with a specific name
                services.AddDbContext<PaymentsDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                });
            });
        });
    }

    #region Create Payment Tests

    [Fact]
    public async Task CreatePayment_WithValidPayment_ShouldReturnCreatedStatus()
    {
        // Arrange
        using var factory = CreateFactoryWithDatabase("CreatePayment_Test");
        var client = factory.CreateClient();
        var payment = new
        {
            amount = 100.00m,
            currency = "USD",
            idempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/payments", payment);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdPayment = await response.Content.ReadFromJsonAsync<Payment>();
        Assert.NotNull(createdPayment);
        Assert.Equal(100.00m, createdPayment.Amount);
        Assert.Equal("USD", createdPayment.Currency);
        Assert.Equal("Pending", createdPayment.Status);
    }

    [Fact]
    public async Task CreatePayment_ShouldReturnLocationHeader()
    {
        // Arrange
        using var factory = CreateFactoryWithDatabase("LocationHeader_Test");
        var client = factory.CreateClient();
        var payment = new
        {
            amount = 50.00m,
            currency = "EUR",
            idempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/payments", payment);

        // Assert
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/api/payments/", response.Headers.Location.ToString());
    }

    #endregion

    #region Get Payment Tests

    [Fact]
    public async Task GetPayment_WithExistingId_ShouldReturnPayment()
    {
        // Arrange
        using var factory = CreateFactoryWithDatabase("GetPayment_Existing_Test");
        var client = factory.CreateClient();
        var payment = new
        {
            amount = 75.00m,
            currency = "GBP",
            idempotencyKey = Guid.NewGuid().ToString()
        };

        // Create a payment first
        var createResponse = await client.PostAsJsonAsync("/api/payments", payment);
        var createdPayment = await createResponse.Content.ReadFromJsonAsync<Payment>();

        // Act - use the same client to ensure same database
        var getResponse = await client.GetAsync($"/api/payments/{createdPayment!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var retrievedPayment = await getResponse.Content.ReadFromJsonAsync<Payment>();
        Assert.NotNull(retrievedPayment);
        Assert.Equal(createdPayment.Id, retrievedPayment.Id);
        Assert.Equal(75.00m, retrievedPayment.Amount);
    }

    [Fact]
    public async Task GetPayment_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        using var factory = CreateFactoryWithDatabase("GetPayment_NotFound_Test");
        var client = factory.CreateClient();
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/payments/{nonExistingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public async Task CreatePayment_WithSameIdempotencyKey_ShouldReturnSamePayment()
    {
        // Arrange - use same client for both requests (same database)
        using var factory = CreateFactoryWithDatabase("Idempotency_SameKey_Test");
        var client = factory.CreateClient();
        var idempotencyKey = Guid.NewGuid().ToString();
        var payment = new
        {
            amount = 200.00m,
            currency = "USD",
            idempotencyKey
        };

        // Act - Create first payment
        var response1 = await client.PostAsJsonAsync("/api/payments", payment);
        var payment1 = await response1.Content.ReadFromJsonAsync<Payment>();

        // Act - Try to create second payment with same idempotency key
        var response2 = await client.PostAsJsonAsync("/api/payments", payment);
        var payment2 = await response2.Content.ReadFromJsonAsync<Payment>();

        // Assert: Both requests should return the same payment
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        Assert.NotNull(payment1);
        Assert.NotNull(payment2);
        Assert.Equal(payment1.Id, payment2.Id);
        Assert.Equal(payment1.CreatedAt, payment2.CreatedAt);
    }

    [Fact]
    public async Task CreatePayment_WithDifferentIdempotencyKeys_ShouldCreateDifferentPayments()
    {
        // Arrange
        using var factory = CreateFactoryWithDatabase("Idempotency_DifferentKeys_Test");
        var client = factory.CreateClient();
        var payment1 = new
        {
            amount = 100.00m,
            currency = "USD",
            idempotencyKey = Guid.NewGuid().ToString()
        };
        var payment2 = new
        {
            amount = 100.00m,
            currency = "USD",
            idempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response1 = await client.PostAsJsonAsync("/api/payments", payment1);
        var response2 = await client.PostAsJsonAsync("/api/payments", payment2);

        var createdPayment1 = await response1.Content.ReadFromJsonAsync<Payment>();
        var createdPayment2 = await response2.Content.ReadFromJsonAsync<Payment>();

        // Assert: Different idempotency keys should create different payments
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        Assert.NotNull(createdPayment1);
        Assert.NotNull(createdPayment2);
        Assert.NotEqual(createdPayment1.Id, createdPayment2.Id);
    }

    [Fact]
    public async Task CreatePayment_IdempotentResubmit_ShouldNotDuplicatePayment()
    {
        // Arrange - sequential requests to avoid race conditions in in-memory DB
        using var factory = CreateFactoryWithDatabase("Idempotent_Resubmit_Test");
        var client = factory.CreateClient();
        var idempotencyKey = Guid.NewGuid().ToString();
        var payment = new
        {
            amount = 500.00m,
            currency = "EUR",
            idempotencyKey
        };

        // Act - Simulate multiple sequential retries with same idempotency key
        var payments = new List<Payment>();
        for (int i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/payments", payment);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var p = await response.Content.ReadFromJsonAsync<Payment>();
            Assert.NotNull(p);
            payments.Add(p);
        }

        // Assert - All responses should return the same payment ID
        var firstPaymentId = payments[0].Id;
        Assert.All(payments, p => Assert.Equal(firstPaymentId, p.Id));
    }

    #endregion
}
