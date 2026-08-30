# InvoiceDesk

A Windows desktop app for small business invoicing, built with WPF on .NET 9.
It manages clients, services and invoices, tracks payments, and reports on
revenue — all against a local database, with no account and no cloud.

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
  English is the invariant fallback for anything untranslated. The default
  follows the machine locale.
- **Local SQLite database** — EF Core with migrations applied on startup, and a
  first run that seeds eight months of sample trading so the app opens on a
  dashboard with something in it.
- **Configurable storage** — reports, attachments and backups each get a folder
  the user picks; defaults sit under the user's Documents folder so an install
  into Program Files still has somewhere writable.
- **Clients** — searchable list next to an editor: add a client, correct one, or
  archive it. Clients are archived rather than deleted, so an invoice never
  loses the company it was issued to.
- **Dashboard** — revenue for the current month, outstanding and overdue totals,
  an eight month revenue chart and the latest invoices, all computed from the
  data rather than hard-coded.

## Architecture

| Project | Contents |
| --- | --- |
| `InvoiceDesk.Domain` | Entities, the money and status rules that go with them, and the storage abstraction |
| `InvoiceDesk.Data` | EF Core and SQLite: context, entity configurations, migrations, seeding |
| `InvoiceDesk.Wpf` | Views, view models, services, themes and resources |

- MVVM with `CommunityToolkit.Mvvm`; views never reach into each other.
- Dependency injection through the .NET Generic Host; every view model and the
  shell window are resolved from the container.
- View models are mapped to views by `DataTemplate`, so navigation is a matter
  of setting a property.
- View models read through `IInvoiceDataStore` and never see a `DbContext`, so
  the WPF project carries no reference to EF Core.
- Data is read through short-lived contexts from `IDbContextFactory` — a window
  that stays open for hours has no business holding a context open with it.
- Settings are persisted as JSON under `%AppData%\InvoiceDesk\settings.json`;
  the database sits next to them as `invoicedesk.db`.

## Status

Working today: shell and navigation, theming, localization, settings, a SQLite
database created and migrated on first launch, a dashboard driven by it, and a
clients screen where records are created, edited and archived.

Next: the price list, the invoice list and editor, PDF export, backups, and a
Windows installer.

## Build and run

Requires the .NET 9 SDK on Windows.

```
dotnet build
dotnet run --project src/InvoiceDesk.Wpf
```

The EF Core tools are pinned in the local manifest, so migrations need no global
install:

```
dotnet tool restore
dotnet ef migrations add <Name> --project src/InvoiceDesk.Data
```
