using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InvoiceDesk.Data;

/// <summary>
/// Used only by <c>dotnet ef</c> when scaffolding migrations. The file it names
/// is never created at runtime; the app passes its own path from the user
/// profile instead.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<InvoiceDeskContext>
{
    public InvoiceDeskContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InvoiceDeskContext>()
            .UseSqlite("Data Source=invoicedesk.design.db")
            .Options;

        return new InvoiceDeskContext(options);
    }
}
