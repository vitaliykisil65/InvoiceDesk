using InvoiceDesk.Domain.Entities;

namespace InvoiceDesk.Domain.Abstractions;

/// <summary>
/// Reads and writes the price list. Like clients, products are archived rather
/// than deleted: an invoice issued last year keeps the position it was billed
/// for, even after the service is no longer offered.
/// </summary>
public interface IProductStore
{
    Task<IReadOnlyList<Product>> GetAsync(bool includeArchived = false, CancellationToken cancellationToken = default);

    /// <summary>Inserts a product with no id yet, updates one that has it. Returns the id.</summary>
    Task<int> SaveAsync(Product product, CancellationToken cancellationToken = default);

    Task SetArchivedAsync(int productId, bool isArchived, CancellationToken cancellationToken = default);
}
