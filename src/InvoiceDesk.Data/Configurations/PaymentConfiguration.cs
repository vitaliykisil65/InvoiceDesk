using InvoiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceDesk.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.Property(payment => payment.Amount).HasPrecision(18, 2);
        builder.Property(payment => payment.Method).HasMaxLength(100);
        builder.Property(payment => payment.Reference).HasMaxLength(100);

        // The dashboard buckets payments by month.
        builder.HasIndex(payment => payment.PaidOn);
    }
}
