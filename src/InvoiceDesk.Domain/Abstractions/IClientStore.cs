using InvoiceDesk.Domain.Entities;

namespace InvoiceDesk.Domain.Abstractions;

/// <summary>
/// Reads and writes the client list. Clients are archived rather than deleted:
/// an invoice keeps pointing at the company it was issued to, however long ago
/// that company stopped being a customer.
/// </summary>
public interface IClientStore
{
    Task<IReadOnlyList<Client>> GetAsync(bool includeArchived = false, CancellationToken cancellationToken = default);

    /// <summary>Inserts a client with no id yet, updates one that has it. Returns the id.</summary>
    Task<int> SaveAsync(Client client, CancellationToken cancellationToken = default);

    Task SetArchivedAsync(int clientId, bool isArchived, CancellationToken cancellationToken = default);
}
