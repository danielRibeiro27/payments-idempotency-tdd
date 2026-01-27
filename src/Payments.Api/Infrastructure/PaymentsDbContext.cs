using Microsoft.EntityFrameworkCore;
using Payments.Api.Domain;

namespace Payments.Api.Infrastructure;

public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments { get; set; }

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
            
            // Unique index on IdempotencyKey for idempotency enforcement
            entity.HasIndex(p => p.IdempotencyKey).IsUnique();
        });
    }
}
