using InvoiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceDesk.Data.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");

        builder.Property(client => client.Name).IsRequired().HasMaxLength(200);
        builder.Property(client => client.ContactPerson).HasMaxLength(200);
        builder.Property(client => client.Email).HasMaxLength(200);
        builder.Property(client => client.Phone).HasMaxLength(50);
        builder.Property(client => client.Address).HasMaxLength(500);
        builder.Property(client => client.TaxNumber).HasMaxLength(50);
        builder.Property(client => client.Notes).HasMaxLength(2000);

        // The client list is sorted and searched by name, and archived clients
        // are filtered out of pickers.
        builder.HasIndex(client => client.Name);
        builder.HasIndex(client => client.IsArchived);
    }
}
