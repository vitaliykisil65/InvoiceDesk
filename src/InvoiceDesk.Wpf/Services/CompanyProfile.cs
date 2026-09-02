namespace InvoiceDesk.Wpf.Services;

/// <summary>
/// The seller's own details: what goes on every invoice as who issued it, and
/// the defaults a new invoice starts from.
/// </summary>
public class CompanyProfile
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string TaxNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Bank { get; set; } = string.Empty;

    public string Iban { get; set; } = string.Empty;

    public string DefaultCurrency { get; set; } = "EUR";

    /// <summary>How many days a fresh invoice gets to be paid, unless changed by hand.</summary>
    public int PaymentTermDays { get; set; } = 14;

    /// <summary>Prefix a new invoice number starts with, e.g. "INV" in "INV-2026-0001".</summary>
    public string InvoiceNumberPrefix { get; set; } = "INV";

    /// <summary>Printed at the bottom of every invoice PDF, e.g. payment instructions.</summary>
    public string InvoiceFooter { get; set; } = string.Empty;

    public CompanyProfile Clone() => new()
    {
        Name = Name,
        Address = Address,
        TaxNumber = TaxNumber,
        Email = Email,
        Phone = Phone,
        Bank = Bank,
        Iban = Iban,
        DefaultCurrency = DefaultCurrency,
        PaymentTermDays = PaymentTermDays,
        InvoiceNumberPrefix = InvoiceNumberPrefix,
        InvoiceFooter = InvoiceFooter
    };
}
