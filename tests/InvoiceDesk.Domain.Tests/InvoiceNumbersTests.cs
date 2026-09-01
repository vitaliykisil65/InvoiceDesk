namespace InvoiceDesk.Domain.Tests;

public class InvoiceNumbersTests
{
    [Fact]
    public void TheFirstInvoiceOfTheYearStartsAtOne()
    {
        Assert.Equal("INV-2026-0001", InvoiceNumbers.Next("INV", 2026, []));
    }

    [Fact]
    public void TheNextNumberFollowsTheHighestOneInUse()
    {
        string[] existing = ["INV-2026-0001", "INV-2026-0007", "INV-2026-0004"];

        Assert.Equal("INV-2026-0008", InvoiceNumbers.Next("INV", 2026, existing));
    }

    [Fact]
    public void TheCounterRestartsInANewYear()
    {
        string[] existing = ["INV-2025-0042"];

        Assert.Equal("INV-2026-0001", InvoiceNumbers.Next("INV", 2026, existing));
    }

    [Fact]
    public void NumbersInAnotherShapeAreIgnored()
    {
        string[] existing = ["2026/13", "INV-0009", "ACME-2026-0100"];

        Assert.Equal("INV-2026-0001", InvoiceNumbers.Next("INV", 2026, existing));
    }

    [Fact]
    public void AnEmptyPrefixFallsBackToTheDefault()
    {
        Assert.Equal("INV-2026-0003", InvoiceNumbers.Format(" ", 2026, 3));
    }
}
