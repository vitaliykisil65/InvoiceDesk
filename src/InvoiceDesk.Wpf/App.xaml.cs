using System.Windows;
using System.Windows.Threading;
using InvoiceDesk.Data;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;
using InvoiceDesk.Wpf.ViewModels;
using InvoiceDesk.Wpf.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestPDF.Infrastructure;
using Serilog;

namespace InvoiceDesk.Wpf;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        LoggingSetup.Configure();
        QuestPDF.Settings.License = LicenseType.Community;

        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<SettingsService>();
                services.AddSingleton<ConfirmationService>();
                services.AddSingleton<StorageService>();
                services.AddSingleton<InvoicePdfService>();
                services.AddSingleton<ThemeService>();
                services.AddSingleton<LocalizationService>();
                services.AddSingleton<NavigationService>();
                services.AddInvoiceDeskData(AppPaths.DatabaseFile);

                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<ClientsViewModel>();
                services.AddSingleton<InvoicesViewModel>();
                services.AddSingleton<PaymentsViewModel>();
                services.AddSingleton<ProductsViewModel>();
                services.AddSingleton<InvoiceEditorViewModel>();
                services.AddSingleton<CompanyViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<ShellWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
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

            Log.Information("InvoiceDesk started");
        }
        catch (Exception exception)
        {
            // Nothing has been shown yet, so there is nowhere else to report this.
            Log.Fatal(exception, "The application failed to start");
            MessageBox.Show(exception.Message, LocalizedStrings.Get("Common_ConfirmTitle"), MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        Log.Information("InvoiceDesk exiting");
        Log.CloseAndFlush();

        base.OnExit(e);
    }

    /// <summary>
    /// Anything that reaches here would otherwise crash the process silently.
    /// The window stays open: whatever failed is logged, and most UI actions
    /// can simply be retried.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled exception on the UI thread");
        MessageBox.Show(e.Exception.Message, LocalizedStrings.Get("Common_ConfirmTitle"), MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    /// <summary>An exception here is already fatal to the process; this only makes sure it is logged first.</summary>
    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e) =>
        Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception outside the UI thread");

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }
}
