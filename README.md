# InvoiceDesk

A Windows desktop app for small business invoicing, built with WPF on .NET 9.
It manages clients, services and invoices, records payments, exports PDFs and
reports on revenue — all against a local database, with no account and no cloud.

This is a portfolio project: the goal is to show production-shaped desktop work,
not to ship a commercial product.

## Highlights

- **Custom window chrome** — own title bar, drag, snap and resize, no default
  Windows frame.
- **Light, dark and system theme** — every brush is a dynamic resource, so the
  theme switches live. System mode follows the Windows app theme and reacts when
  it changes.
- **English and Ukrainian interface** — strings live in `.resx` and are bound
  through a markup extension, so the language switches without a restart.
  English is the invariant fallback for anything untranslated. Until the user
  picks a language, the app follows the one Windows is running in.
- **Local SQLite database** — EF Core with migrations applied on startup, and a
  first run that seeds eight months of sample trading so the app opens on a
  dashboard with something in it.
- **Clients** — searchable list next to an editor: add a client, correct one, or
  archive it. Clients are archived rather than deleted, so an invoice never
  loses the company it was issued to.
- **Invoices** — every invoice in one table, filtered by status, client or a
  number typed into the search box, with what is issued and what is still owed
  totalled underneath. Drafts can be sent or deleted; anything the client has
  seen stays on the books.
- **Invoice editor** — the client, the dates and the positions being billed, with
  lines typed by hand or pulled off the price list, and net, discount, VAT and
  the outstanding balance recalculated as the numbers are entered. Invoice
  numbers are issued automatically and restart every January. Once an invoice
  has been sent it opens read-only: the client already holds that document.
- **Payments** — every payment on one screen next to a form for recording a new
  one, with the invoice picker offering only what can still receive money.
  Recording or removing a payment moves the invoice between sent, partially paid
  and paid on its own; the status is a consequence of the money, not a field
  somebody remembers to change.
- **PDF invoices** — a laid-out document with the seller's details, the client
  block, the lines and the totals, rendered with QuestPDF. It is saved where
  Settings say: asked for with a save dialog, or straight into the reports
  folder, and revealed in Explorer afterwards if that is switched on.
- **Reports** — revenue for a calendar year or a custom date range, broken down
  by month and by client, with invoiced, paid, outstanding and tax totals, and
  export to CSV or PDF. The aggregation lives in the domain project next to the
  money rules and is unit tested the same way.
- **My Company** — the seller's own details on their own page: address, tax
  number, bank and IBAN for the PDF, plus the defaults a new invoice starts
  from — currency, payment term and invoice number prefix.
- **Price list** — the services and products invoices are billed from, each with
  its unit, price and VAT rate, entered in the language the interface is in.
- **Dashboard** — revenue for the current month, outstanding and overdue totals,
  an eight month revenue chart and the latest invoices, all computed from the
  data rather than hard-coded.
- **Configurable storage** — the folder exports are written to and the folder for
  backups are both the user's to choose; defaults sit under Documents, so an
  install into Program Files still has somewhere writable.
- **Logging and error handling** — Serilog writes a daily log, mirrors anything
  carrying an exception into a separate error log, and unhandled exceptions on
  the dispatcher, on background threads and in unobserved tasks are caught
  rather than closing the window on the user.

## Architecture

| Project | Contents |
| --- | --- |
| `InvoiceDesk.Domain` | Entities, the money, status and reporting rules that go with them, and the store abstractions |
| `InvoiceDesk.Data` | EF Core and SQLite: context, entity configurations, migrations, seeding |
| `InvoiceDesk.Wpf` | Views, view models, services, documents, themes and resources |
| `InvoiceDesk.Domain.Tests` | Unit tests for the money, status, numbering and reporting rules |

- MVVM with `CommunityToolkit.Mvvm`; views never reach into each other.
- Dependency injection through the .NET Generic Host; every view model and the
  shell window are resolved from the container.
- View models are mapped to views by `DataTemplate`, so navigation is a matter
  of setting a property.
- The domain project knows nothing about EF Core or WPF. View models read and
  write through the four store interfaces it declares — `IClientStore`,
  `IProductStore`, `IInvoiceStore` and `IPaymentStore` — and never touch a
  `DbContext` themselves.
- Data is read through short-lived contexts from `IDbContextFactory` — a window
  that stays open for hours has no business holding a context open with it.
- Settings are persisted as JSON under `%AppData%\InvoiceDesk\settings.json`;
  the database sits next to them as `invoicedesk.db`, and the logs in a `Logs`
  folder beside both.

## Status

Feature complete. Clients, the price list, invoices and the invoice editor,
payments, reports, PDF export, the company profile, theming, localization,
settings and logging all work end to end against the local database, and the
whole thing installs from an MSI. The money, status, invoice numbering and
reporting rules are covered by unit tests.

## Install

The installer is a per-machine MSI: it puts the program in Program Files, adds a
Start menu shortcut, and registers in Programs and Features so it uninstalls the
way anything else on Windows does. Installing a newer build replaces the current
one rather than adding a second entry.

Uninstalling removes only what was installed. The database, the settings and the
logs stay in `%AppData%\InvoiceDesk`.

Building it takes two commands — publish the app, then package it:

```
dotnet publish src/InvoiceDesk.Wpf -p:PublishProfile=win-x64
dotnet build installer/InvoiceDesk.Installer.wixproj
```

The result is `installer/Output/InvoiceDesk-1.0.0-x64.msi`. The published build
is self-contained, so the machine it lands on does not need the .NET 9 Desktop
Runtime installed first. WiX comes from NuGet with the rest of the build, so
there is nothing to install by hand.

## Build and run

Requires the .NET 9 SDK on Windows.

```
dotnet build
dotnet test
dotnet run --project src/InvoiceDesk.Wpf
```

The EF Core tools are pinned in the local manifest, so migrations need no global
install:

```
dotnet tool restore
dotnet ef migrations add <Name> --project src/InvoiceDesk.Data
```
