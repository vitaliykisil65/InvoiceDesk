using InvoiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceDesk.Data;

/// <summary>
/// The SQLite database behind the app. One file, no server, no account — which
/// is what a small business installing this on a single machine expects.
/// </summary>
public class InvoiceDeskContext : DbContext
{
    public InvoiceDeskContext(DbContextOptions<InvoiceDeskContext> options)
        : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDeskContext).Assembly);
    }
}
