using System.Globalization;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>Read-only projection of an invoice for list and table rows.</summary>
public class InvoiceRowViewModel
{
    public InvoiceRowViewModel(Invoice invoice)
    {
        Number = invoice.Number;
        ClientName = invoice.Client?.Name ?? string.Empty;
        Status = invoice.Status;
        StatusText = LocalizedStrings.Get(StatusKey(invoice.Status));
        DueText = invoice.Status == InvoiceStatus.Draft
            ? "—"
            : invoice.DueOn.ToString("dd MMM", CultureInfo.CurrentUICulture);
        TotalText = string.Create(CultureInfo.CurrentUICulture, $"€{invoice.GrandTotal:N0}");
    }

    public string Number { get; }

    public string ClientName { get; }

    public InvoiceStatus Status { get; }

    public string StatusText { get; }

    public string DueText { get; }

    public string TotalText { get; }

    private static string StatusKey(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "Status_Draft",
        InvoiceStatus.Sent => "Status_Sent",
        InvoiceStatus.PartiallyPaid => "Status_PartiallyPaid",
        InvoiceStatus.Paid => "Status_Paid",
        InvoiceStatus.Overdue => "Status_Overdue",
        _ => "Status_Draft"
    };
}
