using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;
using InvoiceDesk.Wpf.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceDesk.Wpf.Documents;

/// <summary>
/// Renders one invoice as a single PDF page: who issued it, who it is for, what
/// is billed, and what it comes to. A declarative layout, built once per export
/// so it always matches what is on screen.
/// </summary>
public class InvoiceDocument : IDocument
{
    private readonly Invoice _invoice;

    private readonly CompanyProfile _company;

    public InvoiceDocument(Invoice invoice, CompanyProfile company)
    {
        _invoice = invoice;
        _company = company;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(style => style.FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingTop(16).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(_company.Name).FontSize(16).Bold();
                AddLine(column, _company.Address);
                AddLine(column, _company.TaxNumber, "InvoicePdf_TaxNumber");
                AddLine(column, _company.Email);
                AddLine(column, _company.Phone);
            });

            row.ConstantItem(200).Column(column =>
            {
                column.Item().AlignRight().Text(LocalizedStrings.Get("InvoicePdf_Title")).FontSize(18).Bold();
                column.Item().AlignRight().Text(_invoice.Number).FontSize(13);
                column.Item().PaddingTop(6).AlignRight()
                    .Text(LocalizedStrings.Get($"Status_{_invoice.Status}")).FontSize(10).SemiBold();
                column.Item().PaddingTop(6).AlignRight()
                    .Text(LocalizedStrings.Format("InvoicePdf_Issued", CultureText.FormatDate(_invoice.IssuedOn)));
                column.Item().AlignRight()
                    .Text(LocalizedStrings.Format("InvoicePdf_Due", CultureText.FormatDate(_invoice.DueOn)));
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(ComposeClient);
            column.Item().PaddingTop(20).Element(ComposeLines);
            column.Item().PaddingTop(12).AlignRight().Element(ComposeTotals);

            if (!string.IsNullOrWhiteSpace(_invoice.Notes))
            {
                column.Item().PaddingTop(20).Column(notes =>
                {
                    notes.Item().Text(LocalizedStrings.Get("InvoiceEditor_Notes")).Bold();
                    notes.Item().Text(_invoice.Notes);
                });
            }
        });
    }

    private void ComposeClient(IContainer container)
    {
        var client = _invoice.Client;

        container.Column(column =>
        {
            column.Item().Text(LocalizedStrings.Get("InvoicePdf_BillTo")).FontSize(9).FontColor(Colors.Grey.Darken1);
            column.Item().Text(client?.Name ?? string.Empty).Bold();
            AddLine(column, client?.Address);
            AddLine(column, client?.TaxNumber, "InvoicePdf_TaxNumber");
            AddLine(column, client?.Email);
            AddLine(column, client?.Phone);
        });
    }

    private void ComposeLines(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.ConstantColumn(50);
                columns.ConstantColumn(50);
                columns.ConstantColumn(70);
                columns.ConstantColumn(50);
                columns.ConstantColumn(70);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text(LocalizedStrings.Get("InvoiceEditor_LineDescription"));
                header.Cell().Element(HeaderCell).Text(LocalizedStrings.Get("InvoiceEditor_LineUnit"));
                header.Cell().Element(HeaderCell).AlignRight().Text(LocalizedStrings.Get("InvoiceEditor_LineQuantity"));
                header.Cell().Element(HeaderCell).AlignRight().Text(LocalizedStrings.Get("InvoiceEditor_LineUnitPrice"));
                header.Cell().Element(HeaderCell).AlignRight().Text(LocalizedStrings.Get("InvoiceEditor_LineTaxRate"));
                header.Cell().Element(HeaderCell).AlignRight().Text(LocalizedStrings.Get("InvoiceEditor_LineNet"));

                static IContainer HeaderCell(IContainer cell) => cell
                    .DefaultTextStyle(style => style.SemiBold().FontSize(9))
                    .PaddingBottom(4)
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten1);
            });

            foreach (var line in _invoice.Lines)
            {
                table.Cell().Element(BodyCell).Text(line.Description);
                table.Cell().Element(BodyCell).Text(line.Unit);
                table.Cell().Element(BodyCell).AlignRight().Text(CultureText.FormatNumber(line.Quantity));
                table.Cell().Element(BodyCell).AlignRight().Text(CultureText.FormatMoney(line.UnitPrice, _invoice.Currency));
                table.Cell().Element(BodyCell).AlignRight().Text($"{CultureText.FormatNumber(line.TaxRate)}%");
                table.Cell().Element(BodyCell).AlignRight().Text(CultureText.FormatMoney(line.NetAmount, _invoice.Currency));

                static IContainer BodyCell(IContainer cell) => cell
                    .PaddingVertical(5)
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten3);
            }
        });
    }

    private void ComposeTotals(IContainer container)
    {
        container.Width(220).Column(column =>
        {
            TotalRow(column, "InvoiceEditor_Net", CultureText.FormatMoney(_invoice.NetTotal, _invoice.Currency));
            TotalRow(column, "InvoiceEditor_DiscountTotal", CultureText.FormatMoney(-_invoice.DiscountAmount, _invoice.Currency));
            TotalRow(column, "InvoiceEditor_Tax", CultureText.FormatMoney(_invoice.TaxTotal, _invoice.Currency));
            TotalRow(column, "InvoiceEditor_GrandTotal", CultureText.FormatMoney(_invoice.GrandTotal, _invoice.Currency), bold: true);
            TotalRow(column, "InvoiceEditor_Paid", CultureText.FormatMoney(_invoice.PaidAmount, _invoice.Currency));
            TotalRow(column, "InvoiceEditor_Outstanding", CultureText.FormatMoney(_invoice.OutstandingAmount, _invoice.Currency), bold: true);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            if (!string.IsNullOrWhiteSpace(_company.Bank) || !string.IsNullOrWhiteSpace(_company.Iban))
            {
                column.Item().Text(text =>
                {
                    text.Span($"{_company.Bank}  ").FontSize(9);
                    text.Span(_company.Iban).FontSize(9);
                });
            }

            if (!string.IsNullOrWhiteSpace(_company.InvoiceFooter))
            {
                column.Item().PaddingTop(2).Text(_company.InvoiceFooter).FontSize(9)
                    .FontColor(Colors.Grey.Darken1);
            }

            column.Item().PaddingTop(6).AlignCenter().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Medium));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private static void TotalRow(ColumnDescriptor column, string labelKey, string value, bool bold = false)
    {
        column.Item().Row(row =>
        {
            var label = row.RelativeItem().Text(LocalizedStrings.Get(labelKey)).FontSize(bold ? 11 : 10);
            var amount = row.ConstantItem(100).AlignRight().Text(value).FontSize(bold ? 11 : 10);

            if (bold)
            {
                label.Bold();
                amount.Bold();
            }
        });
    }

    /// <summary>Skips a line entirely when the field behind it was never filled in.</summary>
    private static void AddLine(ColumnDescriptor column, string? value, string? formatKey = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        column.Item().Text(formatKey is null ? value : LocalizedStrings.Format(formatKey, value)).FontSize(9);
    }
}
