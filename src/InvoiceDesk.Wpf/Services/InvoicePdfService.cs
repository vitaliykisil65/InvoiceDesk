using System.IO;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Documents;
using Microsoft.Win32;
using QuestPDF.Fluent;

namespace InvoiceDesk.Wpf.Services;

/// <summary>
/// Turns an invoice into a PDF and puts it where the user's settings say: asked
/// where with a save dialog, or straight into the reports folder, then
/// optionally revealed in Explorer. This is what finally makes
/// <see cref="AppSettings.AskWhereToSave"/> and
/// <see cref="AppSettings.OpenFolderAfterExport"/> do something.
/// </summary>
public class InvoicePdfService
{
    private readonly SettingsService _settings;

    private readonly StorageService _storage;

    public InvoicePdfService(SettingsService settings, StorageService storage)
    {
        _settings = settings;
        _storage = storage;
    }

    /// <summary>Renders and saves the PDF. Returns the path, or null if the user cancelled the save dialog.</summary>
    public string? Export(Invoice invoice)
    {
        var fileName = $"{invoice.Number}.pdf";
        var path = _settings.Current.AskWhereToSave ? PromptForPath(fileName) : Path.Combine(_storage.ReportsFolder, fileName);

        if (path is null)
        {
            return null;
        }

        new InvoiceDocument(invoice, _settings.Current.Company).GeneratePdf(path);

        if (_settings.Current.OpenFolderAfterExport)
        {
            _storage.OpenInExplorer(Path.GetDirectoryName(path) ?? _storage.ReportsFolder);
        }

        return path;
    }

    private static string? PromptForPath(string fileName)
    {
        var dialog = new SaveFileDialog
        {
            FileName = fileName,
            DefaultExt = ".pdf",
            Filter = "PDF (*.pdf)|*.pdf"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
