namespace InvoiceDesk.Domain.Entities;

/// <summary>An invoice issued to a client.</summary>
public class Invoice
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public int ClientId { get; set; }

    public Client? Client { get; set; }

    public DateTime IssuedOn { get; set; }

    public DateTime DueOn { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public string Currency { get; set; } = "EUR";

    /// <summary>Discount applied to the net total, in percent.</summary>
    public decimal DiscountPercent { get; set; }

    public string? Notes { get; set; }

    public List<InvoiceLine> Lines { get; set; } = [];

    public List<Payment> Payments { get; set; } = [];

    public decimal NetTotal => Math.Round(Lines.Sum(line => line.NetAmount), 2);

    public decimal DiscountAmount => Math.Round(NetTotal * DiscountPercent / 100m, 2);

    public decimal TaxTotal => Math.Round(Lines.Sum(line => line.TaxAmount) * (1m - DiscountPercent / 100m), 2);

    public decimal GrandTotal => NetTotal - DiscountAmount + TaxTotal;

    public decimal PaidAmount => Math.Round(Payments.Sum(payment => payment.Amount), 2);

    public decimal OutstandingAmount => GrandTotal - PaidAmount;

    /// <summary>
    /// Applies the status the payments and the due date imply. Called on every
    /// write, so what is stored matches what the totals say.
    /// </summary>
    public void ApplyStatus(DateTime today) => Status = ResolveStatus(today);

    /// <summary>Recalculates the status from payments and the due date.</summary>
    public InvoiceStatus ResolveStatus(DateTime today)
    {
        if (Status == InvoiceStatus.Draft)
        {
            return InvoiceStatus.Draft;
        }

        if (OutstandingAmount <= 0m)
        {
            return InvoiceStatus.Paid;
        }

        if (DueOn.Date < today.Date)
        {
            return InvoiceStatus.Overdue;
        }

        return PaidAmount > 0m ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Sent;
    }
}
