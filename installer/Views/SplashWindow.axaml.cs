using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using FModLoaderInstaller.Models;
using FModLoaderInstaller.ViewModels;
using Microsoft.Win32;

namespace FModLoaderInstaller.Views;

public partial class SplashWindow : Window
{
    private const string MutexName = "fModLoaderInstaller_Mutex_Unique_ID";
    private static Mutex? _singleInstanceMutex;

    private readonly DispatcherTimer _timer;
    private double _progress;

    public SplashWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(15)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _progress += 1.5;
        if (_progress >= 100)
        {
            _progress = 100;
            _timer.Stop();

            Dispatcher.UIThread.Post(OpenNextWindow);
        }

        var progressBar = this.FindControl<ProgressBar>("SplashProgressBar");
        if (progressBar != null)
            progressBar.Value = _progress;

        var statusText = this.FindControl<TextBlock>("StatusTextBlock");
        if (statusText != null)
        {
            if (App.IsUninstallMode)
            {
                if (_progress > 80)
                    statusText.Text = "Starting uninstaller…";
                else if (_progress > 50)
                    statusText.Text = "Loading uninstall manifest…";
                else if (_progress > 25)
                    statusText.Text = "Verifying installation…";
                else
                    statusText.Text = "Loading uninstaller resources…";
            }
            else
            {
                if (_progress > 80)
                    statusText.Text = "Starting installer…";
                else if (_progress > 50)
                    statusText.Text = "Configuring installer environment…";
                else if (_progress > 25)
                    statusText.Text = "Verifying platform architecture…";
            }
        }
    }

    private void OpenNextWindow()
    {
        if (App.IsUninstallMode)
        {
            OpenUninstallerOrError();
        }
        else
        {
            OpenInstallerOrError();
        }
    }

    // ── Installer path ───────────────────────────────────────────────────────

    private void OpenInstallerOrError()
    {
        // Single-instance mutex check
        _singleInstanceMutex = new Mutex(initiallyOwned: true, name: MutexName, out bool createdNew);
        if (!createdNew)
        {
            // Another installer instance is already running
            ShowSafetyWindow(new AnotherInstanceWindow());
            return;
        }

        OpenMainWindow();
    }

    private void OpenMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            this.Close();
        }
    }

    // ── Uninstaller path ─────────────────────────────────────────────────────

    private void OpenUninstallerOrError()
    {
        // Existence check: look for registry entry and/or install directory
        if (!IsInstallationPresent())
        {
            ShowSafetyWindow(new ProgramNotFoundWindow());
            return;
        }

        OpenMainWindow();
    }

    /// <summary>
    /// Returns true if fModLoader appears to be installed (registry key or directory found).
    /// </summary>
    private static bool IsInstallationPresent()
    {
        var config = new InstallerConfig();

        // 1. Check registry uninstall entry
        try
        {
            var subkey = $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{config.AppShortName}";
            using var key = Registry.LocalMachine.OpenSubKey(subkey);
            if (key != null)
                return true;
        }
        catch { }

        // 2. Fall back to install directory presence
        try
        {
            if (Directory.Exists(config.TargetDirectory))
                return true;
        }
        catch { }

        // 3. Also check per-machine AppPath registry key
        try
        {
            var appPathKey = $@"Software\{config.AppPublisher}\{config.AppShortName}";
            using var key = Registry.LocalMachine.OpenSubKey(appPathKey);
            if (key != null)
                return true;
        }
        catch { }

        return false;
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private void ShowSafetyWindow(Window safetyWindow)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = safetyWindow;
            safetyWindow.Show();
            this.Close();
        }
    }
}
