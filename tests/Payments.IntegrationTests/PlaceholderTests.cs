using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Payments.Api.Domain.Implementations;
using Payments.Api.Infrastructure;
using Payments.Api.Service.Implementations;
using Payments.Api.Service.Interfaces;

namespace Payments.IntegrationTests;

/// <summary>
/// DTO for deserializing payment responses
/// </summary>
public class PaymentResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

/// <summary>
/// Integration tests for the Payment API endpoints.
/// Uses WebApplicationFactory to create an in-memory test server.
/// </summary>
public class PaymentApiIntegrationTests : IClassFixture<PaymentApiFactory>
{
    private readonly PaymentApiFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public PaymentApiIntegrationTests(PaymentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePayment_ShouldReturnCreated_WhenPaymentIsValid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payment = new
        {
            amount = 100.50m,
            currency = "USD",
            idempotencyKey = $"test-key-{Guid.NewGuid()}"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/payments", payment);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var createdPayment = await response.Content.ReadFromJsonAsync<PaymentResponse>(_jsonOptions);
        Assert.NotNull(createdPayment);
        Assert.Equal(100.50m, createdPayment.Amount);
        Assert.Equal("USD", createdPayment.Currency);
        Assert.Equal(Status.Completed, createdPayment.Status);
    }

    [Fact]
    public async Task CreatePayment_ShouldReturnSamePayment_WhenIdempotencyKeyIsDuplicated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var idempotencyKey = $"idempotent-key-{Guid.NewGuid()}";
        
        var payment1 = new
        {
            amount = 150.00m,
            currency = "EUR",
            idempotencyKey = idempotencyKey
        };
        
        var payment2 = new
        {
            amount = 200.00m, // Different amount
            currency = "GBP", // Different currency
            idempotencyKey = idempotencyKey // Same idempotency key
        };

        // Act
        var response1 = await client.PostAsJsonAsync("/api/payments", payment1);
        var response2 = await client.PostAsJsonAsync("/api/payments", payment2);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        
        var createdPayment1 = await response1.Content.ReadFromJsonAsync<PaymentResponse>(_jsonOptions);
        var createdPayment2 = await response2.Content.ReadFromJsonAsync<PaymentResponse>(_jsonOptions);
        
        Assert.NotNull(createdPayment1);
        Assert.NotNull(createdPayment2);
        
        // Both responses should return the FIRST payment (idempotent behavior)
        Assert.Equal(createdPayment1.Id, createdPayment2.Id);
        Assert.Equal(150.00m, createdPayment2.Amount); // Should be the original amount
        Assert.Equal("EUR", createdPayment2.Currency); // Should be the original currency
    }

    [Fact]
    public async Task GetPayment_ShouldReturnPayment_WhenPaymentExists()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payment = new
        {
            amount = 75.25m,
            currency = "CAD",
            idempotencyKey = $"get-test-key-{Guid.NewGuid()}"
        };

        var createResponse = await client.PostAsJsonAsync("/api/payments", payment);
        var createdPayment = await createResponse.Content.ReadFromJsonAsync<PaymentResponse>(_jsonOptions);

        // Act
        var response = await client.GetAsync($"/api/payments/{createdPayment!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var retrievedPayment = await response.Content.ReadFromJsonAsync<PaymentResponse>(_jsonOptions);
        Assert.NotNull(retrievedPayment);
        Assert.Equal(createdPayment.Id, retrievedPayment.Id);
        Assert.Equal(75.25m, retrievedPayment.Amount);
    }

    [Fact]
    public async Task GetPayment_ShouldReturnNotFound_WhenPaymentDoesNotExist()
    {
        // Arrange
        var client = _factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/payments/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePayment_ShouldReturnFailed_WhenPaymentIsInvalid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var invalidPayment = new
        {
            amount = 0, // Invalid: amount is 0
            currency = "INVALID", // Invalid: currency is too long
            idempotencyKey = "" // Invalid: empty idempotency key
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/payments", invalidPayment);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var createdPayment = await response.Content.ReadFromJsonAsync<PaymentResponse>(_jsonOptions);
        Assert.NotNull(createdPayment);
        Assert.Equal(Status.Failed, createdPayment.Status);
    }

    [Fact]
    public async Task CreatePayment_ConcurrentRequests_ShouldOnlyCreateOne()
    {
        // Arrange
        var client = _factory.CreateClient();
        var idempotencyKey = $"concurrent-key-{Guid.NewGuid()}";
        
        var payments = Enumerable.Range(1, 5).Select(i => new
        {
            amount = 100.00m + i,
            currency = "USD",
            idempotencyKey = idempotencyKey
        }).ToList();

        // Act - Send all requests concurrently
        var tasks = payments.Select(p => client.PostAsJsonAsync("/api/payments", p));
        var responses = await Task.WhenAll(tasks);

        // Assert - All should succeed
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        
        // All should return the same payment (idempotent)
        var createdPayments = new List<PaymentResponse>();
        foreach (var response in responses)
        {
            var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(_jsonOptions);
            Assert.NotNull(payment);
            createdPayments.Add(payment);
        }
        
        // All should have the same ID (only one payment was created)
        var firstId = createdPayments[0].Id;
        Assert.All(createdPayments, p => Assert.Equal(firstId, p.Id));
    }
}

/// <summary>
/// Integration tests for gateway failure handling.
/// Note: Retry policy is thoroughly tested at the unit test level.
/// This test verifies the API properly handles gateway failures.
/// </summary>
public class PaymentApiGatewayIntegrationTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task CreatePayment_WithGatewayFailure_ShouldReturnFailedStatus()
    {
        // Arrange
        var mockGateway = new Mock<IPaymentGateway>();
        mockGateway.Setup(g => g.SendAsync(It.IsAny<Payment>()))
            .ReturnsAsync(false); // Gateway returns failure

        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Remove all Entity Framework service descriptors
                    var descriptorsToRemove = services
                        .Where(d => d.ServiceType.FullName?.Contains("EntityFramework") == true ||
                                   d.ServiceType.FullName?.Contains("PaymentsDbContext") == true ||
                                   d.ServiceType == typeof(DbContextOptions<PaymentsDbContext>) ||
                                   d.ImplementationType?.FullName?.Contains("EntityFramework") == true)
                        .ToList();

                    foreach (var descriptor in descriptorsToRemove)
                    {
                        services.Remove(descriptor);
                    }

                    // Add fresh in-memory database
                    var dbName = $"GatewayFailTestDb_{Guid.NewGuid()}";
                    services.AddDbContext<PaymentsDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(dbName);
                    });
                    
                    // Replace gateway with mock that always fails
                    var gatewayDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPaymentGateway));
                    if (gatewayDescriptor != null)
                        services.Remove(gatewayDescriptor);
                    services.AddScoped<IPaymentGateway>(_ => mockGateway.Object);

                    // Build and seed
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                    db.Database.EnsureCreated();
                });
            });

        var client = factory.CreateClient();

        var payment = new
        {
            amount = 100.00m,
            currency = "USD",
            idempotencyKey = $"gateway-fail-key-{Guid.NewGuid()}"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/payments", payment);

        // Assert - Payment should be created but with Failed status
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var createdPayment = JsonSerializer.Deserialize<PaymentResponse>(content, _jsonOptions);
        Assert.NotNull(createdPayment);
        Assert.Equal(Status.Failed, createdPayment.Status);
        
        // Gateway should have been called
        mockGateway.Verify(g => g.SendAsync(It.IsAny<Payment>()), Times.Once);
    }
}
