using Microsoft.EntityFrameworkCore;

namespace InvoiceDesk.Data;

/// <summary>
/// Brings the database file up to date on startup: applies any pending
/// migrations, then seeds sample data if there is nothing in it yet.
/// </summary>
public class DatabaseInitializer
{
    private readonly IDbContextFactory<InvoiceDeskContext> _contextFactory;

    public DatabaseInitializer(IDbContextFactory<InvoiceDeskContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        await context.Database.MigrateAsync(cancellationToken);

        if (await context.Invoices.AnyAsync(cancellationToken))
        {
            return;
        }

        SampleDataSeeder.Seed(context, DateTime.Today);
        await context.SaveChangesAsync(cancellationToken);
    }
}
