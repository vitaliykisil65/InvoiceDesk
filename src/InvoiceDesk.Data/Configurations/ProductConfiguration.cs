using InvoiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceDesk.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.Property(product => product.Name).IsRequired().HasMaxLength(200);
        builder.Property(product => product.Description).HasMaxLength(1000);
        builder.Property(product => product.Unit).IsRequired().HasMaxLength(20);
        builder.Property(product => product.UnitPrice).HasPrecision(18, 2);
        builder.Property(product => product.TaxRate).HasPrecision(5, 2);

        builder.HasIndex(product => product.Name);
    }
}
