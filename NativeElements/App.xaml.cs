using Microsoft.Extensions.DependencyInjection;
using NativeElements.Data;
using System.Diagnostics;

namespace NativeElements;

public partial class App : Application
{
    private static bool _showingUnhandledError;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            ReportUnhandledException(args.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            ReportUnhandledException(args.Exception, "TaskScheduler.UnobservedTaskException");
            args.SetObserved();
        };

        // Use AppShell for tabbed navigation
        MainPage = new AppShell();
        
        // Initialize database lazily on first use (not blocking)
        _ = Task.Run(async () => await DatabaseService.Initialize());
    }

    private static void ReportUnhandledException(Exception? ex, string source)
    {
        var message = ex?.ToString() ?? "Unknown unhandled exception";
        Debug.WriteLine($"[{source}] {message}");

        if (_showingUnhandledError)
        {
            return;
        }

        _showingUnhandledError = true;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (Current?.MainPage != null)
                {
                    await Current.MainPage.DisplayAlertAsync("Unexpected Error", message, "OK");
                }
            }
            catch
            {
                // Do not throw from global exception reporter.
            }
            finally
            {
                _showingUnhandledError = false;
            }
        });
    }
}
