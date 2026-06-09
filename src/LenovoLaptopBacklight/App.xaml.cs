using System.Windows;
using System.Windows.Threading;
using LenovoLaptopBacklight.Cli;
using LenovoLaptopBacklight.Localization;
using LenovoLaptopBacklight.Services.Config;
using LenovoLaptopBacklight.Services.Ec;

namespace LenovoLaptopBacklight;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Catch any unhandled exceptions and show them instead of silently crashing
        DispatcherUnhandledException += OnUnhandledException;

        // Headless mode: --apply / --set / --read (invoked by the scheduled task as SYSTEM)
        if (e.Args.Length > 0 && e.Args[0].StartsWith("--"))
        {
            int exitCode = HeadlessRunner.Run(e.Args);
            Shutdown(exitCode);
            return;
        }

        // Apply the saved UI language before any window is created
        Loc.I.SetLanguage(ConfigStore.Load().Language);

        new MainWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        EcAccess.Shutdown();
        base.OnExit(e);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ConfigStore.Log($"UNHANDLED: {e.Exception}");
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nDetails have been written to the log file.",
            "Laptop Backlight Control — Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}

