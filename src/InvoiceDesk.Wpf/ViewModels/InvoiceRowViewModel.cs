using System.Globalization;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>Read-only projection of an invoice for list and table rows.</summary>
public class InvoiceRowViewModel
{
    public InvoiceRowViewModel(Invoice invoice)
    {
        Id = invoice.Id;
        Number = invoice.Number;
        ClientName = invoice.Client?.Name ?? string.Empty;
        Status = invoice.Status;
        IsDraft = invoice.Status == InvoiceStatus.Draft;
        StatusText = LocalizedStrings.Get(StatusKey(invoice.Status));
        IssuedText = invoice.IssuedOn.ToString("dd MMM yyyy", CultureInfo.CurrentUICulture);

        // A draft has not been sent, so it owes nothing and is due nowhere.
        DueText = IsDraft
            ? "—"
            : invoice.DueOn.ToString("dd MMM", CultureInfo.CurrentUICulture);
        TotalText = FormatMoney(invoice.GrandTotal);
        OutstandingText = invoice.OutstandingAmount > 0m ? FormatMoney(invoice.OutstandingAmount) : "—";
    }

    public int Id { get; }

    public string Number { get; }

    public string ClientName { get; }

    public InvoiceStatus Status { get; }

    public bool IsDraft { get; }

    public string StatusText { get; }

    public string IssuedText { get; }

    public string DueText { get; }

    public string TotalText { get; }

    public string OutstandingText { get; }

    private static string FormatMoney(decimal amount) =>
        string.Create(CultureInfo.CurrentUICulture, $"€{amount:N2}");

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
