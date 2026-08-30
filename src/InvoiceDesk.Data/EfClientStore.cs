using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceDesk.Data;

/// <inheritdoc cref="IClientStore" />
public class EfClientStore : IClientStore
{
    private readonly IDbContextFactory<InvoiceDeskContext> _contextFactory;

    public EfClientStore(IDbContextFactory<InvoiceDeskContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Client>> GetAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Clients.AsNoTracking();

        if (!includeArchived)
        {
            query = query.Where(client => !client.IsArchived);
        }

        return await query
            .OrderBy(client => client.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SaveAsync(Client client, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (client.Id == 0)
        {
            context.Clients.Add(client);
            await context.SaveChangesAsync(cancellationToken);

            return client.Id;
        }

        // Only the fields the editor owns are copied over, so an update never
        // resets CreatedAt or un-archives a client behind the user's back.
        var stored = await context.Clients
            .FirstOrDefaultAsync(candidate => candidate.Id == client.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Client {client.Id} no longer exists.");

        stored.Name = client.Name;
        stored.ContactPerson = client.ContactPerson;
        stored.Email = client.Email;
        stored.Phone = client.Phone;
        stored.Address = client.Address;
        stored.TaxNumber = client.TaxNumber;
        stored.Notes = client.Notes;

        await context.SaveChangesAsync(cancellationToken);

        return stored.Id;
    }

    public async Task SetArchivedAsync(
        int clientId,
        bool isArchived,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var stored = await context.Clients
            .FirstOrDefaultAsync(candidate => candidate.Id == clientId, cancellationToken);

        if (stored is null || stored.IsArchived == isArchived)
        {
            return;
        }

        stored.IsArchived = isArchived;
        await context.SaveChangesAsync(cancellationToken);
    }
}
