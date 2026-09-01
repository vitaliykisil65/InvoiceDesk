using InvoiceDesk.Domain.Entities;

namespace InvoiceDesk.Domain.Tests;

/// <summary>
/// The status is derived from the payments and the due date, so an invoice can
/// never be marked paid while money is still owed on it.
/// </summary>
public class InvoiceStatusTests
{
    private static readonly DateTime Today = new(2026, 6, 15);

    [Fact]
    public void ADraftStaysADraft_EvenPastItsDueDate()
    {
        var invoice = Sent(dueInDays: -30);
        invoice.Status = InvoiceStatus.Draft;

        Assert.Equal(InvoiceStatus.Draft, invoice.ResolveStatus(Today));
    }

    [Fact]
    public void AnUnpaidInvoiceWithinItsTermIsSent()
    {
        Assert.Equal(InvoiceStatus.Sent, Sent(dueInDays: 7).ResolveStatus(Today));
    }

    [Fact]
    public void APartiallyPaidInvoiceWithinItsTermSaysSo()
    {
        var invoice = Sent(dueInDays: 7);
        invoice.Payments.Add(new Payment { Amount = 40m });

        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.ResolveStatus(Today));
    }

    [Fact]
    public void AnythingStillOwedAfterTheDueDateIsOverdue()
    {
        var invoice = Sent(dueInDays: -1);
        invoice.Payments.Add(new Payment { Amount = 40m });

        Assert.Equal(InvoiceStatus.Overdue, invoice.ResolveStatus(Today));
    }

    [Fact]
    public void SettlingAnOverdueInvoiceMarksItPaid()
    {
        var invoice = Sent(dueInDays: -20);
        invoice.Payments.Add(new Payment { Amount = 100m });

        Assert.Equal(InvoiceStatus.Paid, invoice.ResolveStatus(Today));
    }

    [Fact]
    public void ApplyStatus_WritesTheResolvedStatusBack()
    {
        var invoice = Sent(dueInDays: -3);

        invoice.ApplyStatus(Today);

        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);
    }

    /// <summary>An invoice for 100 with no tax, due the given number of days from today.</summary>
    private static Invoice Sent(int dueInDays) => new()
    {
        Status = InvoiceStatus.Sent,
        IssuedOn = Today.AddDays(-14),
        DueOn = Today.AddDays(dueInDays),
        Lines = [new InvoiceLine { Quantity = 1m, UnitPrice = 100m, TaxRate = 0m }]
    };
}
