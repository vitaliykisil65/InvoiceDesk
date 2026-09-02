using InvoiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceDesk.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.Property(invoice => invoice.Number).IsRequired().HasMaxLength(32);
        builder.Property(invoice => invoice.Currency).IsRequired().HasMaxLength(3);
        builder.Property(invoice => invoice.DiscountPercent).HasPrecision(5, 2);
        builder.Property(invoice => invoice.Notes).HasMaxLength(2000);

        // Stored as text: a database opened outside the app should be readable,
        // and the numeric value of an enum member is an implementation detail.
        builder.Property(invoice => invoice.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(invoice => invoice.Number).IsUnique();
        builder.HasIndex(invoice => invoice.IssuedOn);
        builder.HasIndex(invoice => invoice.Status);

        // A client with invoices cannot be deleted; the UI archives instead.
        builder.HasOne(invoice => invoice.Client)
            .WithMany(client => client.Invoices)
            .HasForeignKey(invoice => invoice.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(invoice => invoice.Lines)
            .WithOne()
            .HasForeignKey(line => line.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(invoice => invoice.Payments)
            .WithOne(payment => payment.Invoice)
            .HasForeignKey(payment => payment.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Totals are derived from the lines and payments, so they are computed
        // in the domain rather than stored and kept in sync by hand.
        builder.Ignore(invoice => invoice.NetTotal);
        builder.Ignore(invoice => invoice.DiscountAmount);
        builder.Ignore(invoice => invoice.TaxTotal);
        builder.Ignore(invoice => invoice.GrandTotal);
        builder.Ignore(invoice => invoice.PaidAmount);
        builder.Ignore(invoice => invoice.OutstandingAmount);
    }
}
