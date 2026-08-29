using System.Collections.ObjectModel;
using System.Globalization;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;

namespace InvoiceDesk.Wpf.ViewModels;

public class DashboardViewModel : PageViewModel
{
    private const double MaxBarHeight = 90d;

    private readonly IInvoiceDataStore _store;

    public DashboardViewModel(IInvoiceDataStore store)
    {
        _store = store;
        Reload();
    }

    public override string TitleKey => "Nav_Dashboard";

    public override string Icon => "";

    public string RevenueThisMonth { get; private set; } = string.Empty;

    public string OutstandingTotal { get; private set; } = string.Empty;

    public string OverdueTotal { get; private set; } = string.Empty;

    public string ClientCount { get; private set; } = string.Empty;

    public string CurrentMonthLabel { get; private set; } = string.Empty;

    public string DataSummary { get; private set; } = string.Empty;

    public ObservableCollection<MonthlyRevenuePoint> MonthlyRevenue { get; } = [];

    public ObservableCollection<InvoiceRowViewModel> RecentInvoices { get; } = [];

    /// <summary>Month names and formatted totals follow the interface language.</summary>
    public override void OnLanguageChanged()
    {
        base.OnLanguageChanged();
        Reload();

        OnPropertyChanged(nameof(RevenueThisMonth));
        OnPropertyChanged(nameof(OutstandingTotal));
        OnPropertyChanged(nameof(OverdueTotal));
        OnPropertyChanged(nameof(ClientCount));
        OnPropertyChanged(nameof(CurrentMonthLabel));
        OnPropertyChanged(nameof(DataSummary));
    }

    private void Reload()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var paidThisMonth = _store.Invoices
            .SelectMany(invoice => invoice.Payments)
            .Where(payment => payment.PaidOn >= monthStart)
            .Sum(payment => payment.Amount);

        var outstanding = _store.Invoices
            .Where(invoice => invoice.Status is not (InvoiceStatus.Draft or InvoiceStatus.Paid))
            .Sum(invoice => invoice.OutstandingAmount);

        var overdue = _store.Invoices
            .Where(invoice => invoice.Status == InvoiceStatus.Overdue)
            .Sum(invoice => invoice.OutstandingAmount);

        RevenueThisMonth = FormatMoney(paidThisMonth);
        OutstandingTotal = FormatMoney(outstanding);
        OverdueTotal = FormatMoney(overdue);
        ClientCount = _store.Clients.Count.ToString(CultureInfo.CurrentUICulture);
        CurrentMonthLabel = LocalizedStrings.Format(
            "Dashboard_RevenueForMonth",
            monthStart.ToString("MMMM", CultureInfo.CurrentUICulture));
        DataSummary = LocalizedStrings.Format(
            "Dashboard_Summary",
            _store.Clients.Count,
            _store.Invoices.Count);

        BuildChart(monthStart);
        BuildRecentInvoices();
    }

    private void BuildChart(DateTime currentMonthStart)
    {
        MonthlyRevenue.Clear();
        var buckets = new List<MonthlyRevenuePoint>();

        for (var monthsBack = 7; monthsBack >= 0; monthsBack--)
        {
            var start = currentMonthStart.AddMonths(-monthsBack);
            var end = start.AddMonths(1);

            var amount = _store.Invoices
                .SelectMany(invoice => invoice.Payments)
                .Where(payment => payment.PaidOn >= start && payment.PaidOn < end)
                .Sum(payment => payment.Amount);

            buckets.Add(new MonthlyRevenuePoint
            {
                Label = start.ToString("MMM", CultureInfo.CurrentUICulture),
                Amount = amount,
                IsCurrent = monthsBack == 0
            });
        }

        var peak = buckets.Max(bucket => bucket.Amount);
        foreach (var bucket in buckets)
        {
            bucket.BarHeight = peak <= 0m ? 2d : Math.Max(2d, (double)(bucket.Amount / peak) * MaxBarHeight);
            MonthlyRevenue.Add(bucket);
        }
    }

    private void BuildRecentInvoices()
    {
        RecentInvoices.Clear();

        var rows = _store.Invoices
            .OrderByDescending(invoice => invoice.IssuedOn)
            .Take(6)
            .Select(invoice => new InvoiceRowViewModel(invoice));

        foreach (var row in rows)
        {
            RecentInvoices.Add(row);
        }
    }

    private static string FormatMoney(decimal amount) =>
        string.Create(CultureInfo.CurrentUICulture, $"€{amount:N0}");
}

public class MonthlyRevenuePoint
{
    public string Label { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public bool IsCurrent { get; init; }

    public double BarHeight { get; set; }
}
