using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FModLoaderInstaller.Models;
using FModLoaderInstaller.Services;

namespace FModLoaderInstaller.ViewModels;

/// <summary>
/// Executes the actual uninstallation: reads uninstall.dat, removes files,
/// deletes directories, removes registry entries, deletes shortcuts.
/// </summary>
public partial class UninstallingPageViewModel : WizardPageBase
{
    private readonly UninstallConfig _uninstallConfig;
    private readonly InstallerConfig _config;
    private bool _started;
    private CancellationTokenSource? _cts;

    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _statusText = "Preparing uninstallation…";
    [ObservableProperty] private string _currentFile = "";
    [ObservableProperty] private int _completedFiles;
    [ObservableProperty] private int _totalFiles;
    [ObservableProperty] private bool _uninstallComplete;
    [ObservableProperty] private bool _uninstallFailed;
    [ObservableProperty] private string _errorMessage = "";

    public UninstallingPageViewModel(UninstallConfig uninstallConfig, InstallerConfig config)
    {
        _uninstallConfig = uninstallConfig;
        _config = config;
        PageTitle = "Uninstalling";
        PageSubtitle = "Please wait while fModLoader is being removed from your computer.";
        CanGoNext = false;
        CanGoBack = false;
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        StartUninstallation();
    }

    public async void StartUninstallation()
    {
        if (_started) return;
        _started = true;
        _cts = new CancellationTokenSource();

        try
        {
            // ── 1. Read uninstall.dat ────────────────────────────────────────
            StatusText = "Reading uninstall manifest…";
            ProgressValue = 5;
            await Task.Delay(200, _cts.Token);

            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var uninstallDataPath = Path.Combine(exeDir, "uninstall.dat");

            string[]? files = null;
            string[]? directories = null;
            string[]? shortcuts = null;

            if (File.Exists(uninstallDataPath))
            {
                var json = await File.ReadAllTextAsync(uninstallDataPath, _cts.Token);
                var data = JsonSerializer.Deserialize<UninstallData>(json);
                if (data != null)
                {
                    files = data.Files;
                    directories = data.Directories;
                    shortcuts = data.Shortcuts;
                }
            }

            // ── 2. Delete shortcuts ──────────────────────────────────────────
            StatusText = "Removing shortcuts…";
            ProgressValue = 15;
            await Task.Delay(150, _cts.Token);

            if (shortcuts != null)
            {
                TotalFiles = shortcuts.Length;
                for (int i = 0; i < shortcuts.Length; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var s = shortcuts[i];
                    CurrentFile = s;
                    try { if (File.Exists(s)) File.Delete(s); } catch { }
                    CompletedFiles = i + 1;
                    ProgressValue = 15 + (10.0 * (i + 1) / shortcuts.Length);
                    await Task.Delay(20, _cts.Token);
                }
            }

            // ── 3. Remove registry entries ───────────────────────────────────
            StatusText = "Removing registry entries…";
            ProgressValue = 30;
            CurrentFile = "";
            await Task.Delay(200, _cts.Token);

            var registryService = new RegistryService();
            registryService.RemoveUninstallEntry(_config);
            registryService.RemoveAppPath(_config);

            if (!_uninstallConfig.KeepFileAssociations)
                registryService.RemoveFileAssociations(_config);

            ProgressValue = 45;
            await Task.Delay(100, _cts.Token);

            // ── 4. Delete files ──────────────────────────────────────────────
            StatusText = "Deleting files…";
            if (files != null && files.Length > 0)
            {
                TotalFiles = files.Length;
                CompletedFiles = 0;
                for (int i = 0; i < files.Length; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var f = files[i];
                    CurrentFile = f;
                    try { if (File.Exists(f)) File.Delete(f); } catch { }
                    CompletedFiles = i + 1;
                    ProgressValue = 45 + (40.0 * (i + 1) / files.Length);
                    await Task.Delay(10, _cts.Token);
                }
            }

            // ── 5. Delete directories ────────────────────────────────────────
            StatusText = "Removing directories…";
            ProgressValue = 88;
            CurrentFile = "";
            await Task.Delay(150, _cts.Token);

            if (directories != null)
            {
                foreach (var dir in directories)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    CurrentFile = dir;
                    try
                    {
                        if (Directory.Exists(dir))
                            Directory.Delete(dir, recursive: true);
                    }
                    catch { }
                    await Task.Delay(20, _cts.Token);
                }
            }

            // Also try the configured install directory itself
            try
            {
                if (Directory.Exists(_config.TargetDirectory))
                    Directory.Delete(_config.TargetDirectory, recursive: true);
            }
            catch { }

            // ── 6. Schedule self-delete ──────────────────────────────────────
            StatusText = "Finalising…";
            ProgressValue = 95;
            CurrentFile = "";
            await Task.Delay(200, _cts.Token);

            ScheduleSelfDelete();

            ProgressValue = 100;
            StatusText = "Uninstallation complete!";
            UninstallComplete = true;
            CanGoNext = true;
        }
        catch (OperationCanceledException)
        {
            StatusText = "Uninstallation cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.ToString();
            UninstallFailed = true;
            StatusText = "Uninstallation failed.";
        }
    }

    public void CancelUninstallation() => _cts?.Cancel();

    /// <summary>
    /// Schedules self-deletion of the uninstaller executable via cmd.exe after a short delay.
    /// </summary>
    private static void ScheduleSelfDelete()
    {
        try
        {
            var currentExe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (currentExe == null) return;

            var cmd = $"/C ping 127.0.0.1 -n 3 > nul & del \"{currentExe}\"";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = cmd,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            });
        }
        catch { /* Best effort */ }
    }

    // ── Nested data class (mirrors Program.cs.UninstallData) ──────────────────
    private class UninstallData
    {
        public string[]? Files { get; set; }
        public string[]? Directories { get; set; }
        public string[]? Shortcuts { get; set; }
    }
}
