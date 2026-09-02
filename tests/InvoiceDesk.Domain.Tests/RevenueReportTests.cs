using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Domain.Reporting;

namespace InvoiceDesk.Domain.Tests;

/// <summary>
/// The reports screen only ever shows what this builder computes, so these
/// tests are what guarantees the range boundaries, the monthly and per-client
/// grouping, and the draft exclusion are all correct.
/// </summary>
public class RevenueReportTests
{
    [Fact]
    public void InvoicesOutsideTheRange_AreExcluded()
    {
        var inRange = InvoiceWith(new DateTime(2026, 3, 15), 100m);
        var beforeRange = InvoiceWith(new DateTime(2026, 2, 28), 200m);
        var afterRange = InvoiceWith(new DateTime(2026, 4, 1), 300m);

        var report = RevenueReport.Build(
            [inRange, beforeRange, afterRange],
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 31));

        Assert.Equal(100m, report.Invoiced);
    }

    [Fact]
    public void RangeBoundaries_AreInclusive()
    {
        var firstDay = InvoiceWith(new DateTime(2026, 3, 1), 50m);
        var lastDay = InvoiceWith(new DateTime(2026, 3, 31), 75m);

        var report = RevenueReport.Build(
            [firstDay, lastDay],
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 31));

        Assert.Equal(125m, report.Invoiced);
    }

    [Fact]
    public void Drafts_AreNotCountedAsRevenue()
    {
        var draft = InvoiceWith(new DateTime(2026, 3, 10), 500m, InvoiceStatus.Draft);
        var sent = InvoiceWith(new DateTime(2026, 3, 12), 100m, InvoiceStatus.Sent);

        var report = RevenueReport.Build(
            [draft, sent],
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 31));

        Assert.Equal(100m, report.Invoiced);
        Assert.Equal(2, report.StatusCounts[InvoiceStatus.Draft] + report.StatusCounts[InvoiceStatus.Sent]);
        Assert.Equal(1, report.StatusCounts[InvoiceStatus.Draft]);
    }

    [Fact]
    public void MonthlyRows_GroupByIssueMonthInOrder()
    {
        var march = InvoiceWith(new DateTime(2026, 3, 5), 100m);
        var april = InvoiceWith(new DateTime(2026, 4, 5), 50m);
        var anotherMarch = InvoiceWith(new DateTime(2026, 3, 20), 20m);

        var report = RevenueReport.Build(
            [march, april, anotherMarch],
            new DateTime(2026, 3, 1),
            new DateTime(2026, 4, 30));

        Assert.Equal(2, report.MonthlyRows.Count);
        Assert.Equal(new DateTime(2026, 3, 1), report.MonthlyRows[0].Month);
        Assert.Equal(120m, report.MonthlyRows[0].Invoiced);
        Assert.Equal(new DateTime(2026, 4, 1), report.MonthlyRows[1].Month);
        Assert.Equal(50m, report.MonthlyRows[1].Invoiced);
    }

    [Fact]
    public void ClientRows_SumPerClient_OrderedByInvoicedDescending()
    {
        var acme = InvoiceWith(new DateTime(2026, 3, 5), 100m, client: "Acme");
        var beta = InvoiceWith(new DateTime(2026, 3, 6), 250m, client: "Beta");
        var acmeAgain = InvoiceWith(new DateTime(2026, 3, 7), 60m, client: "Acme");

        var report = RevenueReport.Build(
            [acme, beta, acmeAgain],
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 31));

        Assert.Equal(2, report.ClientRows.Count);
        Assert.Equal("Beta", report.ClientRows[0].ClientName);
        Assert.Equal(250m, report.ClientRows[0].Invoiced);
        Assert.Equal("Acme", report.ClientRows[1].ClientName);
        Assert.Equal(160m, report.ClientRows[1].Invoiced);
    }

    private static Invoice InvoiceWith(
        DateTime issuedOn,
        decimal netAmount,
        InvoiceStatus status = InvoiceStatus.Sent,
        string client = "Client") => new()
    {
        Status = status,
        IssuedOn = issuedOn,
        DueOn = issuedOn.AddDays(14),
        Client = new Client { Name = client },
        Lines = [new InvoiceLine { Quantity = 1m, UnitPrice = netAmount, TaxRate = 0m }]
    };
}
