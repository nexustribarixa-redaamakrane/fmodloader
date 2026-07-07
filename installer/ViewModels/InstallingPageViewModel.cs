using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FModLoaderInstaller.Models;
using FModLoaderInstaller.Services;

namespace FModLoaderInstaller.ViewModels;

public partial class InstallingPageViewModel : WizardPageBase
{
    private readonly InstallerConfig _config;
    private readonly InstallerService _installerService;

    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _statusText = "Preparing installation…";
    [ObservableProperty] private string _currentFile = "";
    [ObservableProperty] private int _completedFiles;
    [ObservableProperty] private int _totalFiles;
    [ObservableProperty] private bool _installComplete;
    [ObservableProperty] private bool _installFailed;
    [ObservableProperty] private bool _installCancelled;
    [ObservableProperty] private string _errorMessage = "";

    private bool _started;
    private CancellationTokenSource? _cts;

    public InstallingPageViewModel(InstallerConfig config, InstallerService installerService)
    {
        _config = config;
        _installerService = installerService;
        PageTitle = "Installing";
        PageSubtitle = "Please wait while fModLoader is being installed.";
        CanGoNext = false;
        CanGoBack = false;
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        StartInstallation();
    }

    public async void StartInstallation()
    {
        if (_started) return;
        _started = true;
        _cts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<InstallerProgress>(p =>
            {
                ProgressValue = p.Percentage;
                StatusText = p.StatusMessage;
                CurrentFile = p.CurrentFile;
                CompletedFiles = p.CompletedFiles;
                TotalFiles = p.TotalFiles;
            });

            StatusText = "Copying files…";
            await _installerService.InstallAsync(_config, progress, _cts.Token);

            StatusText = "Writing registry entries…";
            ProgressValue = 92;

            var registryService = new RegistryService();
            registryService.WriteFileAssociations(_config);
            registryService.WriteAppPath(_config);
            registryService.WriteUninstallEntry(_config);

            ProgressValue = 96;
            StatusText = "Creating shortcuts…";

            var shortcutService = new ShortcutService();
            shortcutService.CreateShortcuts(_config);

            ProgressValue = 100;
            StatusText = "Installation complete!";
            InstallComplete = true;
            CanGoNext = true;
        }
        catch (OperationCanceledException)
        {
            InstallCancelled = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.ToString();
            InstallFailed = true;
        }
    }

    public void CancelInstallation()
    {
        _cts?.Cancel();
    }
}
