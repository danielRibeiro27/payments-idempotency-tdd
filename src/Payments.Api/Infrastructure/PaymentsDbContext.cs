using Microsoft.EntityFrameworkCore;
using Payments.Api.Domain.Implementations;

namespace Payments.Api.Infrastructure;

public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public virtual DbSet<Payment> Payments { get; set; } //virtual for mocking

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Amount).IsRequired();
            entity.Property(p => p.Currency).IsRequired().HasMaxLength(3);
            entity.Property(p => p.Status).IsRequired().HasMaxLength(50);
            entity.Property(p => p.CreatedAt).IsRequired();
            entity.Property(p => p.IdempotencyKey).IsRequired();
            // Unique index on IdempotencyKey ensures no duplicate payments
            entity.HasIndex(p => p.IdempotencyKey).IsUnique();
        });
    }
}
