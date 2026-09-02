using InvoiceDesk.Domain.Entities;

namespace InvoiceDesk.Domain.Reporting;

/// <summary>One month's revenue figures, for invoices issued in that month.</summary>
public class MonthlyRevenueRow
{
    public DateTime Month { get; init; }

    public decimal Invoiced { get; init; }

    public decimal Paid { get; init; }

    public decimal Outstanding { get; init; }
}

/// <summary>One client's revenue figures, for invoices issued in the report range.</summary>
public class ClientRevenueRow
{
    public string ClientName { get; init; } = string.Empty;

    public decimal Invoiced { get; init; }

    public decimal Paid { get; init; }

    public decimal Outstanding { get; init; }
}

/// <summary>
/// Aggregates invoices issued within a date range into the figures the reports
/// screen shows: totals, a monthly breakdown, a per-client breakdown and how
/// many invoices sit in each status. A draft has not been sent, so it is
/// counted toward the status breakdown but never toward revenue.
/// </summary>
public class RevenueReport
{
    public IReadOnlyList<MonthlyRevenueRow> MonthlyRows { get; init; } = [];

    public IReadOnlyList<ClientRevenueRow> ClientRows { get; init; } = [];

    public IReadOnlyDictionary<InvoiceStatus, int> StatusCounts { get; init; } =
        new Dictionary<InvoiceStatus, int>();

    public decimal Invoiced { get; init; }

    public decimal Paid { get; init; }

    public decimal Outstanding { get; init; }

    public decimal TaxTotal { get; init; }

    public static RevenueReport Build(IEnumerable<Invoice> invoices, DateTime from, DateTime to)
    {
        var inRange = invoices
            .Where(invoice => invoice.IssuedOn.Date >= from.Date && invoice.IssuedOn.Date <= to.Date)
            .ToList();

        var counted = inRange.Where(invoice => invoice.Status != InvoiceStatus.Draft).ToList();

        var monthlyRows = counted
            .GroupBy(invoice => new DateTime(invoice.IssuedOn.Year, invoice.IssuedOn.Month, 1))
            .OrderBy(group => group.Key)
            .Select(group => new MonthlyRevenueRow
            {
                Month = group.Key,
                Invoiced = Math.Round(group.Sum(invoice => invoice.GrandTotal), 2),
                Paid = Math.Round(group.Sum(invoice => invoice.PaidAmount), 2),
                Outstanding = Math.Round(group.Sum(invoice => invoice.OutstandingAmount), 2)
            })
            .ToList();

        var clientRows = counted
            .GroupBy(invoice => invoice.Client?.Name ?? string.Empty)
            .Select(group => new ClientRevenueRow
            {
                ClientName = group.Key,
                Invoiced = Math.Round(group.Sum(invoice => invoice.GrandTotal), 2),
                Paid = Math.Round(group.Sum(invoice => invoice.PaidAmount), 2),
                Outstanding = Math.Round(group.Sum(invoice => invoice.OutstandingAmount), 2)
            })
            .OrderByDescending(row => row.Invoiced)
            .ToList();

        var statusCounts = Enum.GetValues<InvoiceStatus>()
            .ToDictionary(status => status, status => inRange.Count(invoice => invoice.Status == status));

        return new RevenueReport
        {
            MonthlyRows = monthlyRows,
            ClientRows = clientRows,
            StatusCounts = statusCounts,
            Invoiced = Math.Round(counted.Sum(invoice => invoice.GrandTotal), 2),
            Paid = Math.Round(counted.Sum(invoice => invoice.PaidAmount), 2),
            Outstanding = Math.Round(counted.Sum(invoice => invoice.OutstandingAmount), 2),
            TaxTotal = Math.Round(counted.Sum(invoice => invoice.TaxTotal), 2)
        };
    }
}
