using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceDesk.Data;

/// <summary>
/// EF Core implementation of <see cref="IInvoiceDataStore"/>. Reads are
/// no-tracking and go through short-lived contexts from a factory: a desktop
/// app keeps its window open for hours, and a single long-lived context would
/// hold on to every entity it ever loaded.
/// </summary>
public class EfInvoiceDataStore : IInvoiceDataStore
{
    private readonly IDbContextFactory<InvoiceDeskContext> _contextFactory;

    public EfInvoiceDataStore(IDbContextFactory<InvoiceDeskContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Client>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Clients
            .AsNoTracking()
            .Where(client => !client.IsArchived)
            .OrderBy(client => client.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Two collection includes in one query would multiply the rows, so the
        // lines and payments are fetched as separate round trips.
        var invoices = await context.Invoices
            .AsNoTracking()
            .AsSplitQuery()
            .Include(invoice => invoice.Client)
            .Include(invoice => invoice.Lines)
            .Include(invoice => invoice.Payments)
            .OrderByDescending(invoice => invoice.IssuedOn)
            .ThenByDescending(invoice => invoice.Id)
            .ToListAsync(cancellationToken);

        // "Overdue" is a function of today's date, so it is derived on read
        // rather than left to go stale in the database.
        var today = DateTime.Today;
        foreach (var invoice in invoices)
        {
            invoice.Status = invoice.ResolveStatus(today);
        }

        return invoices;
    }
}
