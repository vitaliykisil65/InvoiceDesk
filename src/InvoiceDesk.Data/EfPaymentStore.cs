using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceDesk.Data;

/// <inheritdoc cref="IPaymentStore" />
public class EfPaymentStore : IPaymentStore
{
    private readonly IDbContextFactory<InvoiceDeskContext> _contextFactory;

    public EfPaymentStore(IDbContextFactory<InvoiceDeskContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<int> AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        context.Payments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);

        await UpdateStatusAsync(context, payment.InvoiceId, cancellationToken);

        return payment.Id;
    }

    public async Task DeleteAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var stored = await context.Payments
            .FirstOrDefaultAsync(candidate => candidate.Id == paymentId, cancellationToken);

        if (stored is null)
        {
            return;
        }

        var invoiceId = stored.InvoiceId;

        context.Payments.Remove(stored);
        await context.SaveChangesAsync(cancellationToken);

        await UpdateStatusAsync(context, invoiceId, cancellationToken);
    }

    /// <summary>Money in or out of an invoice moves it along its lifecycle.</summary>
    private static async Task UpdateStatusAsync(
        InvoiceDeskContext context,
        int invoiceId,
        CancellationToken cancellationToken)
    {
        var invoice = await context.Invoices
            .Include(candidate => candidate.Lines)
            .Include(candidate => candidate.Payments)
            .FirstOrDefaultAsync(candidate => candidate.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            return;
        }

        invoice.ApplyStatus(DateTime.Today);
        await context.SaveChangesAsync(cancellationToken);
    }
}
