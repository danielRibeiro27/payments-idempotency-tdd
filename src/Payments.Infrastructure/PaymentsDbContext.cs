using Microsoft.EntityFrameworkCore;
using Payments.Domain;

namespace Payments.Infrastructure;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
    }

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
        });
    }
}
