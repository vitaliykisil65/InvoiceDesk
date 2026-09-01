using InvoiceDesk.Domain.Entities;

namespace InvoiceDesk.Domain.Tests;

/// <summary>
/// The money on an invoice is computed, never stored, so these tests are what
/// guarantees the editor and the dashboard agree on what a customer owes.
/// </summary>
public class InvoiceTotalsTests
{
    [Fact]
    public void LineAmounts_AreRoundedToCents()
    {
        var line = new InvoiceLine { Quantity = 3m, UnitPrice = 33.333m, TaxRate = 19m };

        Assert.Equal(100.00m, line.NetAmount);
        Assert.Equal(19.00m, line.TaxAmount);
        Assert.Equal(119.00m, line.GrossAmount);
    }

    [Fact]
    public void GrandTotal_SumsLinesAndTax()
    {
        var invoice = InvoiceWith(
            new InvoiceLine { Quantity = 10m, UnitPrice = 85m, TaxRate = 19m },
            new InvoiceLine { Quantity = 2m, UnitPrice = 120m, TaxRate = 19m });

        Assert.Equal(1090.00m, invoice.NetTotal);
        Assert.Equal(207.10m, invoice.TaxTotal);
        Assert.Equal(1297.10m, invoice.GrandTotal);
    }

    [Fact]
    public void Discount_ReducesBothTheNetTotalAndTheTax()
    {
        var invoice = InvoiceWith(new InvoiceLine { Quantity = 10m, UnitPrice = 100m, TaxRate = 20m });
        invoice.DiscountPercent = 10m;

        Assert.Equal(1000.00m, invoice.NetTotal);
        Assert.Equal(100.00m, invoice.DiscountAmount);
        Assert.Equal(180.00m, invoice.TaxTotal);
        Assert.Equal(1080.00m, invoice.GrandTotal);
    }

    [Fact]
    public void Outstanding_DropsWithEveryPayment()
    {
        var invoice = InvoiceWith(new InvoiceLine { Quantity = 1m, UnitPrice = 500m, TaxRate = 0m });
        invoice.Payments.Add(new Payment { Amount = 200m });
        invoice.Payments.Add(new Payment { Amount = 150m });

        Assert.Equal(350.00m, invoice.PaidAmount);
        Assert.Equal(150.00m, invoice.OutstandingAmount);
    }

    [Fact]
    public void Overpayment_LeavesNothingOutstanding()
    {
        var invoice = InvoiceWith(new InvoiceLine { Quantity = 1m, UnitPrice = 100m, TaxRate = 0m });
        invoice.Payments.Add(new Payment { Amount = 120m });

        Assert.True(invoice.OutstandingAmount < 0m);
        Assert.Equal(InvoiceStatus.Paid, invoice.ResolveStatus(DateTime.Today));
    }

    private static Invoice InvoiceWith(params InvoiceLine[] lines) => new()
    {
        Status = InvoiceStatus.Sent,
        IssuedOn = DateTime.Today,
        DueOn = DateTime.Today.AddDays(14),
        Lines = [.. lines]
    };
}
