using System.IO;
using Serilog;

namespace InvoiceDesk.Wpf.Services;

/// <summary>
/// Wires up the global Serilog logger before anything else runs. Everything
/// goes to the daily app log; anything with an exception attached is mirrored
/// into its own error log, so a support request never has to be answered by
/// grepping through routine startup noise.
/// </summary>
public static class LoggingSetup
{
    public static void Configure()
    {
        Directory.CreateDirectory(AppPaths.LogsFolder);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(AppPaths.LogsFolder, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .WriteTo.Logger(errorLog => errorLog
                .Filter.ByIncludingOnly(logEvent => logEvent.Exception is not null)
                .WriteTo.File(
                    Path.Combine(AppPaths.LogsFolder, "errors-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30))
            .CreateLogger();
    }
}
