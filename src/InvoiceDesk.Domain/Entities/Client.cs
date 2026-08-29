namespace InvoiceDesk.Domain.Entities;

/// <summary>A customer the company issues invoices to.</summary>
public class Client
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? TaxNumber { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsArchived { get; set; }

    public List<Invoice> Invoices { get; set; } = [];
}
