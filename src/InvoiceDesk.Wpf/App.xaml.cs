using System.Windows;
using InvoiceDesk.Data;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;
using InvoiceDesk.Wpf.ViewModels;
using InvoiceDesk.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvoiceDesk.Wpf;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<SettingsService>();
                services.AddSingleton<StorageService>();
                services.AddSingleton<ThemeService>();
                services.AddSingleton<LocalizationService>();
                services.AddInvoiceDeskData(AppPaths.DatabaseFile);

                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<ShellWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        await _host.StartAsync();

        // Migrate and, on a first run, seed before anything asks for data.
        await _host.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();

        // Restore what the user picked last time; both default to following Windows.
        var settings = _host.Services.GetRequiredService<SettingsService>().Current;
        _host.Services.GetRequiredService<LocalizationService>().Apply(settings.Language);
        _host.Services.GetRequiredService<ThemeService>().Apply(settings.Theme);

        // The culture is settled by now, so the dashboard formats its money and
        // month names correctly on the very first render.
        await _host.Services.GetRequiredService<DashboardViewModel>().LoadAsync();

        _host.Services.GetRequiredService<ShellWindow>().Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
