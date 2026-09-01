using InvoiceDesk.Domain.Entities;

namespace InvoiceDesk.Domain.Abstractions;

/// <summary>
/// Reads and writes invoices together with their lines. Filtering and searching
/// stay in the view models: a small business has hundreds of invoices, not
/// millions, and a list held in memory answers a keystroke without a round trip.
/// </summary>
public interface IInvoiceStore
{
    /// <summary>Invoices with their lines, payments and client, newest issue date first.</summary>
    Task<IReadOnlyList<Invoice>> GetAsync(CancellationToken cancellationToken = default);

    Task<Invoice?> GetByIdAsync(int invoiceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts an invoice with no id yet, updates one that has it, and replaces
    /// its lines with the ones passed in. Returns the id.
    /// </summary>
    Task<int> SaveAsync(Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>Deletes a draft. An invoice that was ever sent is kept for the record.</summary>
    Task DeleteDraftAsync(int invoiceId, CancellationToken cancellationToken = default);

    /// <summary>The next free number for the given year, in the app's numbering format.</summary>
    Task<string> GetNextNumberAsync(string prefix, int year, CancellationToken cancellationToken = default);
}
