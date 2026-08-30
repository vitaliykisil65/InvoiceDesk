using InvoiceDesk.Domain.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceDesk.Data;

/// <summary>Registers the storage layer, so the host only names a file path.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInvoiceDeskData(this IServiceCollection services, string databasePath)
    {
        var folder = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        // Built rather than concatenated: the path comes from the user profile
        // and can contain spaces or quotes.
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();

        services.AddDbContextFactory<InvoiceDeskContext>(options => options.UseSqlite(connectionString));
        services.AddSingleton<IInvoiceDataStore, EfInvoiceDataStore>();
        services.AddSingleton<DatabaseInitializer>();

        return services;
    }
}
