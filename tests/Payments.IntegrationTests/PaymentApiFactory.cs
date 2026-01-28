using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payments.Api.Infrastructure;

namespace Payments.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Configures an in-memory database to isolate tests from the production database.
/// </summary>
public class PaymentApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public PaymentApiFactory()
    {
        _databaseName = $"PaymentsTestDb_{Guid.NewGuid()}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
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
            services.AddDbContext<PaymentsDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Build service provider
            var sp = services.BuildServiceProvider();

            // Create and seed database
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<PaymentsDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
