using System.Globalization;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>Read-only projection of a price list entry for the list on the left.</summary>
public class ProductListItemViewModel
{
    public ProductListItemViewModel(Product product)
    {
        Id = product.Id;
        Name = product.Name;
        IsArchived = product.IsArchived;

        PriceText = string.Create(CultureInfo.CurrentUICulture, $"{product.UnitPrice:N2}");

        // "19.00" reads as noise next to a unit, so trailing zeros are dropped.
        Subtitle = product.IsArchived
            ? LocalizedStrings.Get("Products_ArchivedTag")
            : LocalizedStrings.Format(
                "Products_Subtitle",
                product.Unit,
                product.TaxRate.ToString("0.##", CultureInfo.CurrentUICulture));
    }

    public int Id { get; }

    public string Name { get; }

    public string Subtitle { get; }

    public string PriceText { get; }

    public bool IsArchived { get; }
}
