using InvoiceDesk.Domain.Entities;

namespace InvoiceDesk.Domain.Abstractions;

/// <summary>
/// Read access to the stored invoicing data. The view models depend on this and
/// never on a <c>DbContext</c>, so the storage technology stays replaceable and
/// the WPF project keeps no reference to EF Core.
/// </summary>
public interface IInvoiceDataStore
{
    Task<IReadOnlyList<Client>> GetClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoices with their lines, payments and client, newest issue date first.
    /// </summary>
    Task<IReadOnlyList<Invoice>> GetInvoicesAsync(CancellationToken cancellationToken = default);
}
