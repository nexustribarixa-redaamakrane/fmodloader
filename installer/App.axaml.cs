using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FModLoaderInstaller.ViewModels;
using FModLoaderInstaller.Views;

namespace FModLoaderInstaller;

public partial class App : Application
{
    /// <summary>
    /// Set to <c>true</c> by <see cref="Program"/> when launched with
    /// <c>--uninstall</c> or as <c>uninstall.exe</c>.
    /// </summary>
    public static bool IsUninstallMode { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
            desktop.MainWindow = new SplashWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
