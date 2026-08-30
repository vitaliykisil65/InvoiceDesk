# InvoiceDesk

A WPF desktop app for small business invoicing, built as an Upwork portfolio
piece. General working agreements live in the parent directory's `CLAUDE.md`;
this file covers only what is specific to this product.

## Product rules

- **Localization.** English is the default and the invariant fallback. The user
  can switch to Ukrainian in Settings; the switch applies without a restart.
  Never hard-code display strings in XAML or view models — every user-facing
  string goes through a resource lookup (`Strings.resx`, `Loc` markup
  extension).
- **Theme.** Three options in Settings: light, dark, and system (follows the
  Windows app theme and reacts to changes at runtime). Every brush is a
  `DynamicResource`, so nothing needs a restart to re-skin.
- **Storage.** File locations are configurable in Settings: where reports and
  exported PDFs are saved, where backups are written, and where invoice
  attachments live. Defaults sit under the user's Documents folder, never next
  to the executable.
- Both the theme and the language choice, and all storage paths, are persisted
  in user settings and restored on the next launch.

## Architecture

| Project | Contents |
| --- | --- |
| `InvoiceDesk.Domain` | Entities, money and status rules, store abstractions |
| `InvoiceDesk.Data` | EF Core + SQLite: context, configurations, migrations, seeding |
| `InvoiceDesk.Wpf` | Views, view models, services, themes and resources |

- MVVM with `CommunityToolkit.Mvvm`; views never reach into each other.
- Dependency injection through the .NET Generic Host; every view model and the
  shell window are resolved from the container.
- View models are mapped to views by `DataTemplate`, so navigation is a matter
  of setting a property.
- The Domain project stays free of EF Core and WPF references. The WPF project
  talks to data through `IInvoiceDataStore`, never through `DbContext`.
- Settings are JSON under `%AppData%\InvoiceDesk\settings.json`; the database is
  `invoicedesk.db` in the same folder.

## Build

```
dotnet build
dotnet run --project src/InvoiceDesk.Wpf
```

EF Core tooling is pinned in the local tool manifest:

```
dotnet tool restore
dotnet ef migrations add <Name> --project src/InvoiceDesk.Data
```
