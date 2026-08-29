using InvoiceDesk.Domain.Entities;

namespace InvoiceDesk.Wpf.Services;

/// <summary>
/// In-memory seed data used while the UI is being built. It is replaced by the
/// EF Core repositories once the SQLite layer lands, so the view models talk to
/// this through <see cref="IInvoiceDataStore"/> only.
/// </summary>
public interface IInvoiceDataStore
{
    IReadOnlyList<Client> Clients { get; }

    IReadOnlyList<Invoice> Invoices { get; }
}

public class SampleDataStore : IInvoiceDataStore
{
    private readonly List<Client> _clients = [];
    private readonly List<Invoice> _invoices = [];

    public SampleDataStore()
    {
        Seed();
    }

    public IReadOnlyList<Client> Clients => _clients;

    public IReadOnlyList<Invoice> Invoices => _invoices;

    private void Seed()
    {
        string[] names =
        [
            "Northwind Ltd", "Blue Harbor", "Kravitz Design", "Orion Media",
            "Solstice Labs", "Ferro Logistics", "Marlow & Sons", "Vertex Studio"
        ];

        for (var index = 0; index < names.Length; index++)
        {
            _clients.Add(new Client
            {
                Id = index + 1,
                Name = names[index],
                ContactPerson = $"Contact {index + 1}",
                Email = $"billing@{names[index].Split(' ')[0].ToLowerInvariant()}.com",
                Phone = $"+49 30 5550{index:D2}",
                Address = "Berlin, Germany",
                TaxNumber = $"DE{123456700 + index}"
            });
        }

        var random = new Random(20260829);
        var today = DateTime.Today;
        var invoiceId = 1;

        // Eight months of history so the dashboard chart has something to draw.
        for (var monthsBack = 7; monthsBack >= 0; monthsBack--)
        {
            var month = new DateTime(today.Year, today.Month, 1).AddMonths(-monthsBack);
            var invoicesInMonth = random.Next(3, 6);

            for (var i = 0; i < invoicesInMonth; i++)
            {
                var client = _clients[random.Next(_clients.Count)];
                var issuedOn = month.AddDays(random.Next(0, 26));
                var invoice = new Invoice
                {
                    Id = invoiceId,
                    Number = $"INV-{invoiceId:D4}",
                    ClientId = client.Id,
                    Client = client,
                    IssuedOn = issuedOn,
                    DueOn = issuedOn.AddDays(14),
                    Status = InvoiceStatus.Sent,
                    Currency = "EUR"
                };

                var lineCount = random.Next(1, 4);
                for (var line = 0; line < lineCount; line++)
                {
                    invoice.Lines.Add(new InvoiceLine
                    {
                        InvoiceId = invoice.Id,
                        Description = line switch
                        {
                            0 => "UI design",
                            1 => "Development",
                            _ => "Support retainer"
                        },
                        Unit = "h",
                        Quantity = random.Next(4, 40),
                        UnitPrice = random.Next(45, 120),
                        TaxRate = 19m
                    });
                }

                // Most invoices are settled; a few stay open or slip past the due date.
                var roll = random.NextDouble();
                if (issuedOn < today.AddDays(-20) && roll < 0.75)
                {
                    invoice.Payments.Add(new Payment
                    {
                        InvoiceId = invoice.Id,
                        PaidOn = invoice.DueOn.AddDays(-random.Next(0, 6)),
                        Amount = invoice.GrandTotal,
                        Method = "Bank transfer"
                    });
                }
                else if (roll < 0.85)
                {
                    invoice.Payments.Add(new Payment
                    {
                        InvoiceId = invoice.Id,
                        PaidOn = today.AddDays(-random.Next(1, 10)),
                        Amount = Math.Round(invoice.GrandTotal / 2m, 2),
                        Method = "Bank transfer"
                    });
                }
                else if (monthsBack == 0 && roll > 0.95)
                {
                    invoice.Status = InvoiceStatus.Draft;
                }

                invoice.Status = invoice.ResolveStatus(today);
                _invoices.Add(invoice);
                invoiceId++;
            }
        }

        _invoices.Reverse();
    }
}
