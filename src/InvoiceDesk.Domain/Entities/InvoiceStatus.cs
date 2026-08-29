namespace InvoiceDesk.Domain.Entities;

/// <summary>Lifecycle state of an invoice.</summary>
public enum InvoiceStatus
{
    Draft,
    Sent,
    PartiallyPaid,
    Paid,
    Overdue
}
