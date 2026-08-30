using InvoiceDesk.Domain.Entities;

namespace InvoiceDesk.Data;

/// <summary>
/// Fills an empty database with a believable eight months of trading history,
/// so a fresh install opens on a dashboard with something to show instead of a
/// set of empty screens. Seeding runs only when there are no invoices yet, and
/// the random generator is seeded with a constant so every install of this
/// portfolio build looks the same.
/// </summary>
public static class SampleDataSeeder
{
    public static void Seed(InvoiceDeskContext context, DateTime today)
    {
        var clients = BuildClients();
        var products = BuildProducts();

        context.Clients.AddRange(clients);
        context.Products.AddRange(products);
        context.Invoices.AddRange(BuildInvoices(clients, products, today));
    }

    private static List<Client> BuildClients()
    {
        string[] names =
        [
            "Northwind Ltd", "Blue Harbor", "Kravitz Design", "Orion Media",
            "Solstice Labs", "Ferro Logistics", "Marlow & Sons", "Vertex Studio"
        ];

        return names.Select((name, index) => new Client
        {
            Name = name,
            ContactPerson = $"Contact {index + 1}",
            Email = $"billing@{name.Split(' ')[0].ToLowerInvariant()}.com",
            Phone = $"+49 30 5550{index:D2}",
            Address = "Berlin, Germany",
            TaxNumber = $"DE{123456700 + index}"
        }).ToList();
    }

    private static List<Product> BuildProducts() =>
    [
        new() { Name = "UI design", Unit = "h", UnitPrice = 85m, TaxRate = 19m },
        new() { Name = "Development", Unit = "h", UnitPrice = 95m, TaxRate = 19m },
        new() { Name = "Support retainer", Unit = "mo", UnitPrice = 640m, TaxRate = 19m },
        new() { Name = "Consulting", Unit = "h", UnitPrice = 120m, TaxRate = 19m }
    ];

    private static List<Invoice> BuildInvoices(
        IReadOnlyList<Client> clients,
        IReadOnlyList<Product> products,
        DateTime today)
    {
        var random = new Random(20260829);
        var invoices = new List<Invoice>();
        var number = 1;

        for (var monthsBack = 7; monthsBack >= 0; monthsBack--)
        {
            var month = new DateTime(today.Year, today.Month, 1).AddMonths(-monthsBack);
            var invoicesInMonth = random.Next(3, 6);

            for (var index = 0; index < invoicesInMonth; index++)
            {
                var issuedOn = month.AddDays(random.Next(0, 26));
                var invoice = new Invoice
                {
                    Number = $"INV-{number:D4}",
                    Client = clients[random.Next(clients.Count)],
                    IssuedOn = issuedOn,
                    DueOn = issuedOn.AddDays(14),
                    Status = InvoiceStatus.Sent,
                    Currency = "EUR"
                };

                var lineCount = random.Next(1, 4);
                for (var line = 0; line < lineCount; line++)
                {
                    var product = products[Math.Min(line, products.Count - 1)];
                    invoice.Lines.Add(new InvoiceLine
                    {
                        Product = product,
                        Description = product.Name,
                        Unit = product.Unit,
                        Quantity = random.Next(4, 40),
                        UnitPrice = product.UnitPrice,
                        TaxRate = product.TaxRate
                    });
                }

                AddPayments(invoice, random, today, monthsBack);
                invoice.Status = invoice.ResolveStatus(today);

                invoices.Add(invoice);
                number++;
            }
        }

        return invoices;
    }

    /// <summary>Most invoices are settled; a few stay open or slip past the due date.</summary>
    private static void AddPayments(Invoice invoice, Random random, DateTime today, int monthsBack)
    {
        var roll = random.NextDouble();

        if (invoice.IssuedOn < today.AddDays(-20) && roll < 0.75)
        {
            invoice.Payments.Add(new Payment
            {
                PaidOn = invoice.DueOn.AddDays(-random.Next(0, 6)),
                Amount = invoice.GrandTotal,
                Method = "Bank transfer"
            });
        }
        else if (roll < 0.85)
        {
            invoice.Payments.Add(new Payment
            {
                PaidOn = today.AddDays(-random.Next(1, 10)),
                Amount = Math.Round(invoice.GrandTotal / 2m, 2),
                Method = "Bank transfer"
            });
        }
        else if (monthsBack == 0 && roll > 0.95)
        {
            invoice.Status = InvoiceStatus.Draft;
        }
    }
}
