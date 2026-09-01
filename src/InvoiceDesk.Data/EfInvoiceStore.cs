using InvoiceDesk.Domain;
using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceDesk.Data;

/// <summary>
/// EF Core implementation of <see cref="IInvoiceStore"/>. Reads are no-tracking
/// and go through short-lived contexts from a factory: a desktop app keeps its
/// window open for hours, and a single long-lived context would hold on to every
/// entity it ever loaded.
/// </summary>
public class EfInvoiceStore : IInvoiceStore
{
    private readonly IDbContextFactory<InvoiceDeskContext> _contextFactory;

    public EfInvoiceStore(IDbContextFactory<InvoiceDeskContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Invoice>> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var invoices = await WithDetails(context.Invoices.AsNoTracking())
            .OrderByDescending(invoice => invoice.IssuedOn)
            .ThenByDescending(invoice => invoice.Id)
            .ToListAsync(cancellationToken);

        // "Overdue" is a function of today's date, so it is derived on read
        // rather than left to go stale in the database.
        var today = DateTime.Today;
        foreach (var invoice in invoices)
        {
            invoice.ApplyStatus(today);
        }

        return invoices;
    }

    public async Task<Invoice?> GetByIdAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var invoice = await WithDetails(context.Invoices.AsNoTracking())
            .FirstOrDefaultAsync(candidate => candidate.Id == invoiceId, cancellationToken);

        invoice?.ApplyStatus(DateTime.Today);

        return invoice;
    }

    public async Task<int> SaveAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var stored = invoice.Id == 0
            ? NewInvoice(context)
            : await context.Invoices
                .Include(candidate => candidate.Lines)
                .Include(candidate => candidate.Payments)
                .FirstOrDefaultAsync(candidate => candidate.Id == invoice.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Invoice {invoice.Id} no longer exists.");

        stored.Number = invoice.Number;
        stored.ClientId = invoice.ClientId;
        stored.IssuedOn = invoice.IssuedOn;
        stored.DueOn = invoice.DueOn;
        stored.Currency = invoice.Currency;
        stored.DiscountPercent = invoice.DiscountPercent;
        stored.Notes = invoice.Notes;
        stored.Status = invoice.Status;

        // An invoice carries a handful of lines, so replacing them outright is
        // both simpler and cheaper than matching them up one by one.
        context.InvoiceLines.RemoveRange(stored.Lines);
        stored.Lines = [.. invoice.Lines.Select(CopyOf)];

        // Payments are on the stored invoice, so the status is resolved from
        // what the database actually holds rather than from the editor's copy.
        stored.ApplyStatus(DateTime.Today);

        await context.SaveChangesAsync(cancellationToken);

        return stored.Id;
    }

    public async Task DeleteDraftAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var stored = await context.Invoices
            .FirstOrDefaultAsync(candidate => candidate.Id == invoiceId, cancellationToken);

        if (stored is null || stored.Status != InvoiceStatus.Draft)
        {
            return;
        }

        // Lines and payments are cascade deleted by the model configuration.
        context.Invoices.Remove(stored);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GetNextNumberAsync(
        string prefix,
        int year,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var numbers = await context.Invoices
            .AsNoTracking()
            .Select(invoice => invoice.Number)
            .ToListAsync(cancellationToken);

        return InvoiceNumbers.Next(prefix, year, numbers);
    }

    /// <summary>
    /// Two collection includes in one query would multiply the rows, so the
    /// lines and payments are fetched as separate round trips.
    /// </summary>
    private static IQueryable<Invoice> WithDetails(IQueryable<Invoice> invoices) =>
        invoices
            .AsSplitQuery()
            .Include(invoice => invoice.Client)
            .Include(invoice => invoice.Lines)
            .Include(invoice => invoice.Payments);

    private static Invoice NewInvoice(InvoiceDeskContext context)
    {
        var invoice = new Invoice();
        context.Invoices.Add(invoice);

        return invoice;
    }

    private static InvoiceLine CopyOf(InvoiceLine line) => new()
    {
        ProductId = line.ProductId,
        Description = line.Description,
        Unit = line.Unit,
        Quantity = line.Quantity,
        UnitPrice = line.UnitPrice,
        TaxRate = line.TaxRate
    };
}
