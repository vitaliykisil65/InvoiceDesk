using InvoiceDesk.Domain.Entities;

namespace InvoiceDesk.Domain.Abstractions;

/// <summary>
/// Records money received against invoices. Every change here moves the invoice
/// through its lifecycle, so the store recalculates the status as it writes.
/// </summary>
public interface IPaymentStore
{
    /// <summary>Every payment with its invoice and client, most recent first.</summary>
    Task<IReadOnlyList<Payment>> GetAsync(CancellationToken cancellationToken = default);

    Task<int> AddAsync(Payment payment, CancellationToken cancellationToken = default);

    Task DeleteAsync(int paymentId, CancellationToken cancellationToken = default);
}