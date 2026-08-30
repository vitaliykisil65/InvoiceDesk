using InvoiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceDesk.Data.Configurations;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("InvoiceLines");

        builder.Property(line => line.Description).IsRequired().HasMaxLength(500);
        builder.Property(line => line.Unit).IsRequired().HasMaxLength(20);
        builder.Property(line => line.Quantity).HasPrecision(18, 3);
        builder.Property(line => line.UnitPrice).HasPrecision(18, 2);
        builder.Property(line => line.TaxRate).HasPrecision(5, 2);

        // A line keeps the price it was written with, so editing or removing a
        // product later never rewrites history.
        builder.HasOne(line => line.Product)
            .WithMany()
            .HasForeignKey(line => line.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(line => line.NetAmount);
        builder.Ignore(line => line.TaxAmount);
        builder.Ignore(line => line.GrossAmount);
    }
}
