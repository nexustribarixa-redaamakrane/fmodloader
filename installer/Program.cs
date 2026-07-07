using Avalonia;
using System;
using System.IO;
using System.Linq;

namespace FModLoaderInstaller;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Detect uninstall mode via --uninstall flag or executable name
        bool isUninstall = args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase)
            || Path.GetFileNameWithoutExtension(
                System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "")
                .Equals("uninstall", StringComparison.OrdinalIgnoreCase);

        App.IsUninstallMode = isUninstall;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
