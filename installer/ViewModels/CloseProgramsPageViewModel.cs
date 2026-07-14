using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FModLoaderInstaller.ViewModels;

/// <summary>
/// Wizard page that scans for running fModLoader processes and blocks navigation
/// until all have been closed.
/// </summary>
public partial class CloseProgramsPageViewModel : WizardPageBase
{
    private readonly DispatcherTimer _scanTimer;

    /// <summary>Processes that must be closed before proceeding.</summary>
    public ObservableCollection<string> RunningProcesses { get; } = new();

    [ObservableProperty] private bool _hasRunningProcesses;
    [ObservableProperty] private bool _canProceed;

    public CloseProgramsPageViewModel()
    {
        PageTitle = "Close Programs";
        PageSubtitle = "The following programs must be closed before uninstalling.";
        CanGoBack = true;
        CanGoNext = true; // Never disable the Next button

        _scanTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _scanTimer.Tick += (_, _) => ScanForProcesses();
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        ScanForProcesses();
        _scanTimer.Start();
    }

    public override void OnNavigatedFrom()
    {
        base.OnNavigatedFrom();
        _scanTimer.Stop();
    }

    private void ScanForProcesses()
    {
        var found = Process.GetProcesses()
            .Where(p =>
            {
                try
                {
                    return p.ProcessName.Contains("fmodloader", StringComparison.OrdinalIgnoreCase)
                        || p.ProcessName.Contains("fmod_loader", StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            })
            .Select(p =>
            {
                try { return $"{p.ProcessName}.exe  (PID {p.Id})"; }
                catch { return p.ProcessName; }
            })
            .ToList();

        RunningProcesses.Clear();
        foreach (var item in found)
            RunningProcesses.Add(item);

        HasRunningProcesses = RunningProcesses.Count > 0;
        CanGoNext = true; // Keep Next button enabled

        // If no running processes, we can proceed
        CanProceed = !HasRunningProcesses;
    }
}
