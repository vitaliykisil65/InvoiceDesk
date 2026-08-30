namespace InvoiceDesk.Domain.Entities;

/// <summary>A single billable position inside an invoice.</summary>
public class InvoiceLine
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public int? ProductId { get; set; }

    /// <summary>The price list entry this line came from, if any.</summary>
    public Product? Product { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Unit { get; set; } = "pcs";

    public decimal Quantity { get; set; } = 1m;

    public decimal UnitPrice { get; set; }

    public decimal TaxRate { get; set; }

    /// <summary>Line total before tax.</summary>
    public decimal NetAmount => Math.Round(Quantity * UnitPrice, 2);

    public decimal TaxAmount => Math.Round(NetAmount * TaxRate / 100m, 2);

    public decimal GrossAmount => NetAmount + TaxAmount;
}
