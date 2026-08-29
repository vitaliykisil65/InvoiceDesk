namespace InvoiceDesk.Domain.Entities;

/// <summary>A sellable product or service kept in the price list.</summary>
public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Unit { get; set; } = "pcs";

    public decimal UnitPrice { get; set; }

    public decimal TaxRate { get; set; }

    public bool IsArchived { get; set; }
}
