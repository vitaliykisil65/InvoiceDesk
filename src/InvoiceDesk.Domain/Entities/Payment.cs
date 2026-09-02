namespace InvoiceDesk.Domain.Entities;

/// <summary>Money received against an invoice. An invoice may be paid in parts.</summary>
public class Payment
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    public Invoice? Invoice { get; set; }

    public DateTime PaidOn { get; set; }

    public decimal Amount { get; set; }

    public string? Method { get; set; }

    public string? Reference { get; set; }
}