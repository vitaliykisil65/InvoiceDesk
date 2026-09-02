using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>Read-only projection of a payment for the list.</summary>
public class PaymentRowViewModel
{
    public PaymentRowViewModel(Payment payment)
    {
        Id = payment.Id;
        InvoiceNumber = payment.Invoice?.Number ?? string.Empty;
        ClientName = payment.Invoice?.Client?.Name ?? string.Empty;
        PaidOnText = CultureText.FormatDate(payment.PaidOn);
        AmountText = CultureText.FormatMoney(payment.Amount, payment.Invoice?.Currency ?? "EUR");
        Method = payment.Method ?? string.Empty;
        Reference = payment.Reference ?? string.Empty;
    }

    public int Id { get; }

    public string InvoiceNumber { get; }

    public string ClientName { get; }

    public string PaidOnText { get; }

    public string AmountText { get; }

    public string Method { get; }

    public string Reference { get; }
}

/// <summary>One entry of the invoice picker in the payment form.</summary>
public class InvoicePickerOption
{
    public InvoicePickerOption(Invoice invoice)
    {
        Id = invoice.Id;
        Label = LocalizedStrings.Format(
            "Payments_InvoiceOption",
            invoice.Number,
            invoice.Client?.Name ?? string.Empty,
            CultureText.FormatMoney(invoice.OutstandingAmount, invoice.Currency));
    }

    public int Id { get; }

    public string Label { get; }
}
