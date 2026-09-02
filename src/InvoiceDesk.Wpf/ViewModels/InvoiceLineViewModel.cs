using CommunityToolkit.Mvvm.ComponentModel;
using InvoiceDesk.Domain.Entities;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>
/// One editable position of an invoice. Quantities, prices and rates are held
/// as text while they are being typed — half of "12.5" is not a number — and
/// turned into a domain line only when the editor asks for one.
/// </summary>
public partial class InvoiceLineViewModel : ObservableObject
{
    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _unit = "pcs";

    [ObservableProperty]
    private string _quantity = "1";

    [ObservableProperty]
    private string _unitPrice = "0";

    [ObservableProperty]
    private string _taxRate = "0";

    public InvoiceLineViewModel()
    {
    }

    public InvoiceLineViewModel(InvoiceLine line)
    {
        ProductId = line.ProductId;
        _description = line.Description;
        _unit = line.Unit;
        _quantity = CultureText.FormatNumber(line.Quantity);
        _unitPrice = CultureText.FormatNumber(line.UnitPrice);
        _taxRate = CultureText.FormatNumber(line.TaxRate);
    }

    public InvoiceLineViewModel(Product product)
    {
        ProductId = product.Id;
        _description = product.Name;
        _unit = product.Unit;
        _unitPrice = CultureText.FormatNumber(product.UnitPrice);
        _taxRate = CultureText.FormatNumber(product.TaxRate);
    }

    /// <summary>The price list entry this line came from, if it came from one.</summary>
    public int? ProductId { get; }

    /// <summary>The invoice's currency; set by the editor since a line does not carry its own.</summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>True when the row has a description and numbers that can be billed.</summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Description)
        && CultureText.ParseNumber(Quantity) is > 0m
        && CultureText.ParseNumber(UnitPrice) is >= 0m
        && CultureText.ParseNumber(TaxRate) is >= 0m and <= 100m;

    public string NetText => CultureText.FormatMoney(ToLine().NetAmount, Currency);

    /// <summary>The domain line this row stands for; unreadable numbers count as zero.</summary>
    public InvoiceLine ToLine() => new()
    {
        ProductId = ProductId,
        Description = Description.Trim(),
        Unit = string.IsNullOrWhiteSpace(Unit) ? "pcs" : Unit.Trim(),
        Quantity = CultureText.ParseNumber(Quantity) ?? 0m,
        UnitPrice = CultureText.ParseNumber(UnitPrice) ?? 0m,
        TaxRate = CultureText.ParseNumber(TaxRate) ?? 0m
    };

    /// <summary>Re-reads the numbers after a language switch changed how they are written.</summary>
    public void OnLanguageChanged() => OnPropertyChanged(nameof(NetText));

    partial void OnQuantityChanged(string value) => OnPropertyChanged(nameof(NetText));

    partial void OnUnitPriceChanged(string value) => OnPropertyChanged(nameof(NetText));
}
