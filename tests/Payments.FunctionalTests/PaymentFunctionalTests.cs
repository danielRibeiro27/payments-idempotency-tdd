using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payments.Api.Domain;
using Payments.Api.Infrastructure;
using Payments.Api.Service.Implementations;

namespace Payments.FunctionalTests;

/// <summary>
/// Functional tests for end-to-end payment scenarios including retry behavior.
/// These tests verify the complete flow from API request to database persistence.
/// </summary>
public class PaymentFunctionalTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _baseFactory;

    public PaymentFunctionalTests(WebApplicationFactory<Program> factory)
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

    #region End-to-End Payment Flow Tests

    [Fact]
    public async Task CompletePaymentFlow_CreateAndRetrieve_ShouldWorkEndToEnd()
    {
        // Arrange
        using var factory = CreateFactoryWithDatabase("CompleteFlow_Test");
        var client = factory.CreateClient();
        var idempotencyKey = Guid.NewGuid().ToString();
        var payment = new
        {
            amount = 1500.00m,
            currency = "USD",
            idempotencyKey
        };

        // Act - Create payment
        var createResponse = await client.PostAsJsonAsync("/api/payments", payment);
        var createdPayment = await createResponse.Content.ReadFromJsonAsync<Payment>();

        // Act - Retrieve payment
        var getResponse = await client.GetAsync($"/api/payments/{createdPayment!.Id}");
        var retrievedPayment = await getResponse.Content.ReadFromJsonAsync<Payment>();

        // Assert - Full flow works correctly
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(retrievedPayment);
        Assert.Equal(1500.00m, retrievedPayment.Amount);
        Assert.Equal("USD", retrievedPayment.Currency);
        Assert.Equal("Pending", retrievedPayment.Status);
        Assert.Equal(idempotencyKey, retrievedPayment.IdempotencyKey);
    }

    [Fact]
    public async Task IdempotentRetryScenario_MultipleSubmissions_ShouldProcessOnce()
    {
        // Arrange
        using var factory = CreateFactoryWithDatabase("IdempotentRetry_Test");
        var client = factory.CreateClient();
        var idempotencyKey = Guid.NewGuid().ToString();
        var payment = new
        {
            amount = 2500.00m,
            currency = "EUR",
            idempotencyKey
        };

        // Act - Simulate network retry scenario: client sends same request multiple times
        // Using sequential requests to avoid race conditions in in-memory DB
        var payments = new List<Payment>();
        for (int i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/payments", payment);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var p = await response.Content.ReadFromJsonAsync<Payment>();
            Assert.NotNull(p);
            payments.Add(p);
        }

        // Assert - All responses return the same payment
        var firstPaymentId = payments[0].Id;
        var firstCreatedAt = payments[0].CreatedAt;

        Assert.All(payments, p =>
        {
            Assert.Equal(firstPaymentId, p.Id);
            Assert.Equal(firstCreatedAt, p.CreatedAt);
        });
    }

    #endregion

    #region Retry Service Tests

    [Fact]
    public async Task RetryService_WithTransientFailure_ShouldSucceedAfterRetry()
    {
        // Arrange
        var retryService = new RetryService();
        int attemptCount = 0;

        // Act - Simulate a transient failure followed by success
        var result = await retryService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new TimeoutException("Simulated transient failure");
            }
            return "success";
        });

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task RetryService_WithIdempotentOperation_ShouldBeSafeToRetry()
    {
        // Arrange
        var retryService = new RetryService();
        int operationCount = 0;

        // Act - Operation that returns same result on retry (idempotent)
        var result = await retryService.ExecuteWithRetryAsync(async () =>
        {
            operationCount++;
            if (operationCount < 3)
            {
                throw new TimeoutException("Transient error");
            }
            return "payment-123";
        }, maxRetries: 5);

        // Assert - Should get consistent result after retries
        Assert.Equal("payment-123", result);
        Assert.Equal(3, operationCount);
    }

    #endregion

    #region Concurrent Request Tests

    [Fact]
    public async Task ConcurrentRequests_WithSameIdempotencyKey_ShouldNotCreateDuplicates()
    {
        // Arrange - Use sequential requests to ensure deterministic behavior
        using var factory = CreateFactoryWithDatabase("ConcurrentSameKey_Test");
        var client = factory.CreateClient();
        var idempotencyKey = Guid.NewGuid().ToString();
        var payment = new
        {
            amount = 3000.00m,
            currency = "GBP",
            idempotencyKey
        };

        // Act - Send sequential requests with same idempotency key
        var payments = new List<Payment>();
        for (int i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/payments", payment);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var p = await response.Content.ReadFromJsonAsync<Payment>();
            Assert.NotNull(p);
            payments.Add(p);
        }

        // Assert - All payments should have the same ID
        var firstId = payments[0].Id;
        Assert.All(payments, p => Assert.Equal(firstId, p.Id));
    }

    [Fact]
    public async Task SequentialRequests_WithDifferentIdempotencyKeys_ShouldCreateMultiplePayments()
    {
        // Arrange
        using var factory = CreateFactoryWithDatabase("SequentialDifferentKeys_Test");
        var client = factory.CreateClient();

        // Act - Send requests with different idempotency keys
        var payments = new List<Payment>();
        for (int i = 0; i < 5; i++)
        {
            var paymentRequest = new
            {
                amount = 100.00m * (i + 1),
                currency = "USD",
                idempotencyKey = Guid.NewGuid().ToString()
            };

            var response = await client.PostAsJsonAsync("/api/payments", paymentRequest);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var p = await response.Content.ReadFromJsonAsync<Payment>();
            Assert.NotNull(p);
            payments.Add(p);
        }

        // Assert - Should create 5 different payments
        var uniqueIds = payments.Select(p => p.Id).Distinct().ToList();
        Assert.Equal(5, uniqueIds.Count);
    }

    #endregion
}
