using System.Collections.ObjectModel;
using System.Globalization;
using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;

namespace InvoiceDesk.Wpf.ViewModels;

public class DashboardViewModel : PageViewModel
{
    private const double MaxBarHeight = 90d;

    private readonly IClientStore _clientStore;

    private readonly IInvoiceStore _invoiceStore;

    private readonly SettingsService _settings;

    private IReadOnlyList<Client> _clients = [];

    private IReadOnlyList<Invoice> _invoices = [];

    public DashboardViewModel(IClientStore clientStore, IInvoiceStore invoiceStore, SettingsService settings)
    {
        _clientStore = clientStore;
        _invoiceStore = invoiceStore;
        _settings = settings;
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

        // Only the formatting depends on the language, so this reformats what
        // was already loaded instead of going back to the database.
        Rebuild();
    }

    /// <summary>Reads the current data and rebuilds everything on the page.</summary>
    public override async Task OnActivatedAsync(CancellationToken cancellationToken = default)
    {
        _clients = await _clientStore.GetAsync(cancellationToken: cancellationToken);
        _invoices = await _invoiceStore.GetAsync(cancellationToken);

        Rebuild();
    }

    private void Rebuild()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var paidThisMonth = _invoices
            .SelectMany(invoice => invoice.Payments)
            .Where(payment => payment.PaidOn >= monthStart)
            .Sum(payment => payment.Amount);

        var outstanding = _invoices
            .Where(invoice => invoice.Status is not (InvoiceStatus.Draft or InvoiceStatus.Paid))
            .Sum(invoice => invoice.OutstandingAmount);

        var overdue = _invoices
            .Where(invoice => invoice.Status == InvoiceStatus.Overdue)
            .Sum(invoice => invoice.OutstandingAmount);

        RevenueThisMonth = FormatMoney(paidThisMonth);
        OutstandingTotal = FormatMoney(outstanding);
        OverdueTotal = FormatMoney(overdue);
        ClientCount = _clients.Count.ToString(CultureInfo.CurrentUICulture);
        CurrentMonthLabel = LocalizedStrings.Format(
            "Dashboard_RevenueForMonth",
            monthStart.ToString("MMMM", CultureInfo.CurrentUICulture));
        DataSummary = LocalizedStrings.Format(
            "Dashboard_Summary",
            _clients.Count,
            _invoices.Count);

        BuildChart(monthStart);
        BuildRecentInvoices();

        RaiseSummaryChanged();
    }

    private void RaiseSummaryChanged()
    {
        OnPropertyChanged(nameof(RevenueThisMonth));
        OnPropertyChanged(nameof(OutstandingTotal));
        OnPropertyChanged(nameof(OverdueTotal));
        OnPropertyChanged(nameof(ClientCount));
        OnPropertyChanged(nameof(CurrentMonthLabel));
        OnPropertyChanged(nameof(DataSummary));
    }

    private void BuildChart(DateTime currentMonthStart)
    {
        MonthlyRevenue.Clear();
        var buckets = new List<MonthlyRevenuePoint>();

        for (var monthsBack = 7; monthsBack >= 0; monthsBack--)
        {
            var start = currentMonthStart.AddMonths(-monthsBack);
            var end = start.AddMonths(1);

            var amount = _invoices
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

        var rows = _invoices
            .OrderByDescending(invoice => invoice.IssuedOn)
            .Take(6)
            .Select(invoice => new InvoiceRowViewModel(invoice));

        foreach (var row in rows)
        {
            RecentInvoices.Add(row);
        }
    }

    /// <summary>
    /// Aggregates like the month's revenue can mix invoices in different
    /// currencies, so this reports them under the company's own currency rather
    /// than pretending the total belongs to any single invoice.
    /// </summary>
    private string FormatMoney(decimal amount) => string.Create(
        CultureInfo.CurrentUICulture,
        $"{CultureText.CurrencySymbol(_settings.Current.Company.DefaultCurrency)}{amount:N0}");
}

public class MonthlyRevenuePoint
{
    public string Label { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public bool IsCurrent { get; init; }

    public double BarHeight { get; set; }
}
