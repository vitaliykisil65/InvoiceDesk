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
- **Configurable storage** — reports, attachments and backups each get a folder
  the user picks; defaults sit under the user's Documents folder so an install
  into Program Files still has somewhere writable.
- **Dashboard** — revenue for the current month, outstanding and overdue totals,
  an eight month revenue chart and the latest invoices, all computed from the
  data rather than hard-coded.

## Architecture

| Project | Contents |
| --- | --- |
| `InvoiceDesk.Domain` | Entities and the money and status rules that go with them |
| `InvoiceDesk.Wpf` | Views, view models, services, themes and resources |

- MVVM with `CommunityToolkit.Mvvm`; views never reach into each other.
- Dependency injection through the .NET Generic Host; every view model and the
  shell window are resolved from the container.
- View models are mapped to views by `DataTemplate`, so navigation is a matter
  of setting a property.
- Settings are persisted as JSON under `%AppData%\InvoiceDesk\settings.json`.

## Status

Working today: shell and navigation, theming, localization, settings, and a
dashboard driven by seeded sample data.

Next: EF Core with SQLite behind the existing data store interface, the invoice
list and editor, PDF export, backups, and a Windows installer.

## Build and run

Requires the .NET 9 SDK on Windows.

```
dotnet build
dotnet run --project src/InvoiceDesk.Wpf
```
