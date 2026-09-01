using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceDesk.Data;

/// <inheritdoc cref="IProductStore" />
public class EfProductStore : IProductStore
{
    private readonly IDbContextFactory<InvoiceDeskContext> _contextFactory;

    public EfProductStore(IDbContextFactory<InvoiceDeskContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Product>> GetAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Products.AsNoTracking();

        if (!includeArchived)
        {
            query = query.Where(product => !product.IsArchived);
        }

        return await query
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SaveAsync(Product product, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (product.Id == 0)
        {
            context.Products.Add(product);
            await context.SaveChangesAsync(cancellationToken);

            return product.Id;
        }

        // Only the fields the editor owns are copied over, so an update never
        // un-archives a product behind the user's back.
        var stored = await context.Products
            .FirstOrDefaultAsync(candidate => candidate.Id == product.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Product {product.Id} no longer exists.");

        stored.Name = product.Name;
        stored.Description = product.Description;
        stored.Unit = product.Unit;
        stored.UnitPrice = product.UnitPrice;
        stored.TaxRate = product.TaxRate;

        await context.SaveChangesAsync(cancellationToken);

        return stored.Id;
    }

    public async Task SetArchivedAsync(
        int productId,
        bool isArchived,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var stored = await context.Products
            .FirstOrDefaultAsync(candidate => candidate.Id == productId, cancellationToken);

        if (stored is null || stored.IsArchived == isArchived)
        {
            return;
        }

        stored.IsArchived = isArchived;
        await context.SaveChangesAsync(cancellationToken);
    }
}
