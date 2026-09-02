using System.Globalization;
using System.IO;
using System.Text;
using InvoiceDesk.Domain.Reporting;
using InvoiceDesk.Wpf.Documents;
using InvoiceDesk.Wpf.ViewModels;
using Microsoft.Win32;
using QuestPDF.Fluent;

namespace InvoiceDesk.Wpf.Services;

/// <summary>
/// Writes a revenue report to CSV or PDF, following the same save-location
/// rules as <see cref="InvoicePdfService"/>: asked where with a save dialog,
/// or straight into the reports folder, then optionally revealed in Explorer.
/// </summary>
public class ReportExportService
{
    private readonly SettingsService _settings;

    private readonly StorageService _storage;

    public ReportExportService(SettingsService settings, StorageService storage)
    {
        _settings = settings;
        _storage = storage;
    }

    /// <summary>Writes the report as CSV. Returns the path, or null if the user cancelled the save dialog.</summary>
    public string? ExportCsv(RevenueReport report, DateTime from, DateTime to)
    {
        var path = ResolvePath(FileName(from, to, "csv"), "CSV (*.csv)|*.csv", ".csv");

        if (path is null)
        {
            return null;
        }

        File.WriteAllText(path, BuildCsv(report), Encoding.UTF8);
        AfterExport(path);

        return path;
    }

    /// <summary>Renders and saves the report as PDF. Returns the path, or null if the user cancelled the save dialog.</summary>
    public string? ExportPdf(RevenueReport report, DateTime from, DateTime to, CompanyProfile company)
    {
        var path = ResolvePath(FileName(from, to, "pdf"), "PDF (*.pdf)|*.pdf", ".pdf");

        if (path is null)
        {
            return null;
        }

        new ReportDocument(report, company, from, to).GeneratePdf(path);
        AfterExport(path);

        return path;
    }

    private void AfterExport(string path)
    {
        if (_settings.Current.OpenFolderAfterExport)
        {
            _storage.OpenInExplorer(Path.GetDirectoryName(path) ?? _storage.ReportsFolder);
        }
    }

    private string? ResolvePath(string fileName, string filter, string extension) =>
        _settings.Current.AskWhereToSave
            ? PromptForPath(fileName, filter, extension)
            : Path.Combine(_storage.ReportsFolder, fileName);

    private static string? PromptForPath(string fileName, string filter, string extension)
    {
        var dialog = new SaveFileDialog
        {
            FileName = fileName,
            DefaultExt = extension,
            Filter = filter
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static string FileName(DateTime from, DateTime to, string extension) =>
        $"Report-{from:yyyyMMdd}-{to:yyyyMMdd}.{extension}";

    private static string BuildCsv(RevenueReport report)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Month,Invoiced,Paid,Outstanding");
        foreach (var row in report.MonthlyRows)
        {
            builder.AppendLine(string.Join(',',
                row.Month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                row.Invoiced.ToString(CultureInfo.InvariantCulture),
                row.Paid.ToString(CultureInfo.InvariantCulture),
                row.Outstanding.ToString(CultureInfo.InvariantCulture)));
        }

        builder.AppendLine();
        builder.AppendLine("Client,Invoiced,Paid,Outstanding");
        foreach (var row in report.ClientRows)
        {
            builder.AppendLine(string.Join(',',
                CsvField(row.ClientName),
                row.Invoiced.ToString(CultureInfo.InvariantCulture),
                row.Paid.ToString(CultureInfo.InvariantCulture),
                row.Outstanding.ToString(CultureInfo.InvariantCulture)));
        }

        builder.AppendLine();
        builder.AppendLine("Invoiced,Paid,Outstanding,Tax");
        builder.AppendLine(string.Join(',',
            report.Invoiced.ToString(CultureInfo.InvariantCulture),
            report.Paid.ToString(CultureInfo.InvariantCulture),
            report.Outstanding.ToString(CultureInfo.InvariantCulture),
            report.TaxTotal.ToString(CultureInfo.InvariantCulture)));

        return builder.ToString();
    }

    private static string CsvField(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
