using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Domain.Reporting;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;
using Serilog;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>
/// Revenue, tax and per-client figures for a chosen period, with export to CSV
/// and PDF. The period is either a calendar year or a custom date range; the
/// domain's <see cref="RevenueReport"/> does the actual aggregation, this view
/// model only reads the invoices, picks the range and formats the result.
/// </summary>
public partial class ReportsViewModel : PageViewModel
{
    private readonly IInvoiceStore _invoices;

    private readonly SettingsService _settings;

    private readonly ReportExportService _export;

    private IReadOnlyList<Invoice> _loaded = [];

    [ObservableProperty]
    private int _selectedYear = DateTime.Today.Year;

    [ObservableProperty]
    private bool _isCustomRange;

    [ObservableProperty]
    private string _fromDate = string.Empty;

    [ObservableProperty]
    private string _toDate = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public ReportsViewModel(IInvoiceStore invoices, SettingsService settings, ReportExportService export)
    {
        _invoices = invoices;
        _settings = settings;
        _export = export;
    }

    public override string TitleKey => "Nav_Reports";

    public override string Icon => "";

    public ObservableCollection<int> Years { get; } = [];

    public ObservableCollection<MonthlyReportRowViewModel> MonthlyRows { get; } = [];

    public ObservableCollection<ClientReportRowViewModel> ClientRows { get; } = [];

    public ObservableCollection<StatusCountRowViewModel> StatusCounts { get; } = [];

    public string DateHint => LocalizedStrings.Format("Reports_DateHint", CultureText.DatePattern);

    public string InvoicedText { get; private set; } = string.Empty;

    public string PaidText { get; private set; } = string.Empty;

    public string OutstandingText { get; private set; } = string.Empty;

    public string TaxText { get; private set; } = string.Empty;

    public bool IsEmpty => MonthlyRows.Count == 0;

    public override async Task OnActivatedAsync(CancellationToken cancellationToken = default)
    {
        _loaded = await _invoices.GetAsync(cancellationToken);

        BuildYearOptions();
        Rebuild();
    }

    public override void OnLanguageChanged()
    {
        base.OnLanguageChanged();

        Rebuild();

        OnPropertyChanged(nameof(DateHint));
    }

    partial void OnSelectedYearChanged(int value) => Rebuild();

    partial void OnIsCustomRangeChanged(bool value) => Rebuild();

    partial void OnFromDateChanged(string value) => Rebuild();

    partial void OnToDateChanged(string value) => Rebuild();

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        var (from, to) = CurrentRange();
        var report = RevenueReport.Build(_loaded, from, to);

        await RunExportAsync(() => _export.ExportCsv(report, from, to));
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        var (from, to) = CurrentRange();
        var report = RevenueReport.Build(_loaded, from, to);

        await RunExportAsync(() => _export.ExportPdf(report, from, to, _settings.Current.Company));
    }

    private async Task RunExportAsync(Func<string?> export)
    {
        try
        {
            IsBusy = true;

            var path = await Task.Run(export);
            StatusMessage = path is null
                ? string.Empty
                : LocalizedStrings.Format("Reports_Exported", Path.GetFileName(path));
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to export the report");
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildYearOptions()
    {
        var currentYear = DateTime.Today.Year;
        var earliestYear = _loaded.Count == 0
            ? currentYear
            : _loaded.Min(invoice => invoice.IssuedOn.Year);

        var selected = SelectedYear;

        Years.Clear();
        for (var year = currentYear; year >= earliestYear; year--)
        {
            Years.Add(year);
        }

        SelectedYear = Years.Contains(selected) ? selected : currentYear;
    }

    private (DateTime From, DateTime To) CurrentRange()
    {
        if (IsCustomRange)
        {
            var from = CultureText.ParseDate(FromDate);
            var to = CultureText.ParseDate(ToDate);

            if (from is not null && to is not null && from <= to)
            {
                return (from.Value, to.Value);
            }
        }

        return (new DateTime(SelectedYear, 1, 1), new DateTime(SelectedYear, 12, 31));
    }

    private void Rebuild()
    {
        var (from, to) = CurrentRange();
        var report = RevenueReport.Build(_loaded, from, to);
        var currency = _settings.Current.Company.DefaultCurrency;

        InvoicedText = CultureText.FormatMoney(report.Invoiced, currency);
        PaidText = CultureText.FormatMoney(report.Paid, currency);
        OutstandingText = CultureText.FormatMoney(report.Outstanding, currency);
        TaxText = CultureText.FormatMoney(report.TaxTotal, currency);

        MonthlyRows.Clear();
        foreach (var row in report.MonthlyRows)
        {
            MonthlyRows.Add(new MonthlyReportRowViewModel(row, currency));
        }

        ClientRows.Clear();
        foreach (var row in report.ClientRows)
        {
            ClientRows.Add(new ClientReportRowViewModel(row, currency));
        }

        StatusCounts.Clear();
        foreach (var status in Enum.GetValues<InvoiceStatus>())
        {
            StatusCounts.Add(new StatusCountRowViewModel(status, report.StatusCounts.GetValueOrDefault(status)));
        }

        OnPropertyChanged(nameof(InvoicedText));
        OnPropertyChanged(nameof(PaidText));
        OnPropertyChanged(nameof(OutstandingText));
        OnPropertyChanged(nameof(TaxText));
        OnPropertyChanged(nameof(IsEmpty));
    }
}

public class MonthlyReportRowViewModel
{
    public MonthlyReportRowViewModel(MonthlyRevenueRow row, string currency)
    {
        MonthLabel = row.Month.ToString("MMMM yyyy", CultureInfo.CurrentUICulture);
        InvoicedText = CultureText.FormatMoney(row.Invoiced, currency);
        PaidText = CultureText.FormatMoney(row.Paid, currency);
        OutstandingText = CultureText.FormatMoney(row.Outstanding, currency);
    }

    public string MonthLabel { get; }

    public string InvoicedText { get; }

    public string PaidText { get; }

    public string OutstandingText { get; }
}

public class ClientReportRowViewModel
{
    public ClientReportRowViewModel(ClientRevenueRow row, string currency)
    {
        ClientName = row.ClientName;
        InvoicedText = CultureText.FormatMoney(row.Invoiced, currency);
        PaidText = CultureText.FormatMoney(row.Paid, currency);
        OutstandingText = CultureText.FormatMoney(row.Outstanding, currency);
    }

    public string ClientName { get; }

    public string InvoicedText { get; }

    public string PaidText { get; }

    public string OutstandingText { get; }
}

public class StatusCountRowViewModel
{
    public StatusCountRowViewModel(InvoiceStatus status, int count)
    {
        StatusText = LocalizedStrings.Get($"Status_{status}");
        Count = count;
    }

    public string StatusText { get; }

    public int Count { get; }
}
