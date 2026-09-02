using System.Globalization;
using InvoiceDesk.Domain.Reporting;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;
using InvoiceDesk.Wpf.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InvoiceDesk.Wpf.Documents;

/// <summary>
/// Renders a revenue report as a single PDF page: the period, the totals, a
/// monthly breakdown and a per-client breakdown. Built the same way as
/// <see cref="InvoiceDocument"/>, on the same QuestPDF layout.
/// </summary>
public class ReportDocument : IDocument
{
    private readonly RevenueReport _report;

    private readonly CompanyProfile _company;

    private readonly DateTime _from;

    private readonly DateTime _to;

    public ReportDocument(RevenueReport report, CompanyProfile company, DateTime from, DateTime to)
    {
        _report = report;
        _company = company;
        _from = from;
        _to = to;
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
            page.Footer().AlignCenter().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(8).FontColor(Colors.Grey.Medium));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(_company.Name).FontSize(16).Bold();
                column.Item().Text(LocalizedStrings.Get("ReportPdf_Title")).FontSize(12);
            });

            row.ConstantItem(220).AlignRight().Text(
                LocalizedStrings.Format("ReportPdf_Period", CultureText.FormatDate(_from), CultureText.FormatDate(_to)));
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(ComposeTotals);
            column.Item().PaddingTop(20).Element(ComposeMonthly);
            column.Item().PaddingTop(20).Element(ComposeClients);
        });
    }

    private void ComposeTotals(IContainer container)
    {
        container.Row(row =>
        {
            TotalCell(row, "Reports_Invoiced", _report.Invoiced);
            TotalCell(row, "Reports_Paid", _report.Paid);
            TotalCell(row, "Reports_Outstanding", _report.Outstanding);
            TotalCell(row, "Reports_Tax", _report.TaxTotal);
        });
    }

    private void TotalCell(RowDescriptor row, string labelKey, decimal amount)
    {
        row.RelativeItem().Column(column =>
        {
            column.Item().Text(LocalizedStrings.Get(labelKey)).FontSize(9).FontColor(Colors.Grey.Darken1);
            column.Item().Text(CultureText.FormatMoney(amount, _company.DefaultCurrency)).FontSize(14).Bold();
        });
    }

    private void ComposeMonthly(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text(LocalizedStrings.Get("Reports_MonthlyBreakdown")).Bold();
            column.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text(LocalizedStrings.Get("Reports_ColumnMonth"));
                    header.Cell().Element(HeaderCell).AlignRight().Text(LocalizedStrings.Get("Reports_Invoiced"));
                    header.Cell().Element(HeaderCell).AlignRight().Text(LocalizedStrings.Get("Reports_Paid"));
                    header.Cell().Element(HeaderCell).AlignRight().Text(LocalizedStrings.Get("Reports_Outstanding"));
                });

                foreach (var row in _report.MonthlyRows)
                {
                    table.Cell().Element(BodyCell).Text(row.Month.ToString("MMMM yyyy", CultureInfo.CurrentUICulture));
                    table.Cell().Element(BodyCell).AlignRight().Text(CultureText.FormatMoney(row.Invoiced, _company.DefaultCurrency));
                    table.Cell().Element(BodyCell).AlignRight().Text(CultureText.FormatMoney(row.Paid, _company.DefaultCurrency));
                    table.Cell().Element(BodyCell).AlignRight().Text(CultureText.FormatMoney(row.Outstanding, _company.DefaultCurrency));
                }
            });
        });
    }

    private void ComposeClients(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text(LocalizedStrings.Get("Reports_ClientBreakdown")).Bold();
            column.Item().PaddingTop(6).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text(LocalizedStrings.Get("Reports_ColumnClient"));
                    header.Cell().Element(HeaderCell).AlignRight().Text(LocalizedStrings.Get("Reports_Invoiced"));
                    header.Cell().Element(HeaderCell).AlignRight().Text(LocalizedStrings.Get("Reports_Paid"));
                    header.Cell().Element(HeaderCell).AlignRight().Text(LocalizedStrings.Get("Reports_Outstanding"));
                });

                foreach (var row in _report.ClientRows)
                {
                    table.Cell().Element(BodyCell).Text(row.ClientName);
                    table.Cell().Element(BodyCell).AlignRight().Text(CultureText.FormatMoney(row.Invoiced, _company.DefaultCurrency));
                    table.Cell().Element(BodyCell).AlignRight().Text(CultureText.FormatMoney(row.Paid, _company.DefaultCurrency));
                    table.Cell().Element(BodyCell).AlignRight().Text(CultureText.FormatMoney(row.Outstanding, _company.DefaultCurrency));
                }
            });
        });
    }

    private static IContainer HeaderCell(IContainer cell) => cell
        .DefaultTextStyle(style => style.SemiBold().FontSize(9))
        .PaddingBottom(4)
        .BorderBottom(1)
        .BorderColor(Colors.Grey.Lighten1);

    private static IContainer BodyCell(IContainer cell) => cell
        .PaddingVertical(5)
        .BorderBottom(1)
        .BorderColor(Colors.Grey.Lighten3);
}
