using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FModLoaderInstaller.Models;
using FModLoaderInstaller.Services;

namespace FModLoaderInstaller.ViewModels;

/// <summary>
/// Master ViewModel that orchestrates the wizard — manages page stack, navigation, and shared state.
/// Dynamically builds either the installer or uninstaller page sequence based on
/// <see cref="App.IsUninstallMode"/>.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly InstallerConfig _config = new();
    private readonly UninstallConfig _uninstallConfig = new();
    private readonly InstallerService _installerService;
    private int _currentIndex;

    // ── Wizard pages ─────────────────────────────────────────────────────────
    public ObservableCollection<WizardPageBase> Pages { get; } = new();

    [ObservableProperty] private WizardPageBase? _currentPage;
    [ObservableProperty] private int _currentStep;
    [ObservableProperty] private int _totalSteps;
    [ObservableProperty] private bool _isInstalling;
    [ObservableProperty] private bool _isFinished;

    // ── Navigation state ─────────────────────────────────────────────────────
    [ObservableProperty] private bool _canNavigateBack;
    [ObservableProperty] private bool _canNavigateNext = true;
    [ObservableProperty] private string _nextButtonText = "Next";
    [ObservableProperty] private bool _showCancelButton = true;
    [ObservableProperty] private bool _showStepIndicator = true;
    [ObservableProperty] private bool _isCancelConfirmationVisible;
    [ObservableProperty] private bool _isCleaningUp;

    // ── Step names for the step indicator ────────────────────────────────────
    public ObservableCollection<string> StepNames { get; } = new();

    /// <summary>Title shown in the custom window chrome title bar.</summary>
    public string WindowTitle => App.IsUninstallMode ? "fModLoader Uninstall" : "fModLoader Setup";

    public MainViewModel()
    {
        _installerService = new InstallerService();

        if (App.IsUninstallMode)
            BuildUninstallerFlow();
        else
            BuildInstallerFlow();

        TotalSteps = Pages.Count - 1;
        _currentIndex = 0;
        NavigateToPage(0);
    }

    // ── Flow builders ────────────────────────────────────────────────────────

    private void BuildInstallerFlow()
    {
        StepNames.Clear();
        foreach (var s in new[] { "Welcome", "License", "Directory", "Start Menu", "Tasks", "Ready", "Install", "Done" })
            StepNames.Add(s);

        var preparePage = new PreparePageViewModel();
        var welcomePage = new WelcomePageViewModel();
        var licensePage = new LicensePageViewModel();
        var directoryPage = new DirectoryPageViewModel(_config);
        var startMenuPage = new StartMenuPageViewModel(_config);
        var tasksPage = new TasksPageViewModel(_config);
        var readyPage = new ReadyPageViewModel(_config);
        var installingPage = new InstallingPageViewModel(_config, _installerService);
        var finishPage = new FinishPageViewModel(_config);

        Pages.Add(preparePage);
        Pages.Add(welcomePage);

        if (!ElevationService.IsElevated())
        {
            var uacPage = new UACErrorPageViewModel();
            Pages.Add(uacPage);

            StepNames.Clear();
            foreach (var s in new[] { "Welcome", "UAC Error", "License", "Directory", "Start Menu", "Tasks", "Ready", "Install", "Done" })
                StepNames.Add(s);
        }

        Pages.Add(licensePage);
        Pages.Add(directoryPage);
        Pages.Add(startMenuPage);
        Pages.Add(tasksPage);
        Pages.Add(readyPage);
        Pages.Add(installingPage);
        Pages.Add(finishPage);

        // Auto-navigate from PreparePage once it completes
        preparePage.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PreparePageViewModel.PreparationComplete) && preparePage.PreparationComplete)
            {
                _currentIndex = 1;
                NavigateToPage(_currentIndex);
            }
        };
    }

    private void BuildUninstallerFlow()
    {
        StepNames.Clear();
        foreach (var s in new[] { "Welcome", "Close Apps", "Directory", "Extensions", "Uninstall", "Done" })
            StepNames.Add(s);

        var preparePage = new PreparePageViewModel();
        var welcomePage = new WelcomePageViewModel(isUninstall: true);
        var closeProgramsPage = new CloseProgramsPageViewModel();
        var directoryPage = new DirectoryPageViewModel(_config, readOnly: true);
        var extensionsPage = new UninstallExtensionsPageViewModel(_uninstallConfig);
        var uninstallingPage = new UninstallingPageViewModel(_uninstallConfig, _config);
        var finishPage = new FinishPageViewModel(_config, isUninstall: true);

        Pages.Add(preparePage);
        Pages.Add(welcomePage);
        Pages.Add(closeProgramsPage);
        Pages.Add(directoryPage);
        Pages.Add(extensionsPage);
        Pages.Add(uninstallingPage);
        Pages.Add(finishPage);

        // Auto-navigate from PreparePage
        preparePage.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PreparePageViewModel.PreparationComplete) && preparePage.PreparationComplete)
            {
                _currentIndex = 1;
                NavigateToPage(_currentIndex);
            }
        };

        // Auto-navigate from CloseProgramsPage once all programs are closed
        closeProgramsPage.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CloseProgramsPageViewModel.CanProceed) && closeProgramsPage.CanProceed)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (CurrentPage == closeProgramsPage)
                    {
                        _currentIndex = Pages.IndexOf(closeProgramsPage) + 1;
                        NavigateToPage(_currentIndex);
                    }
                });
            }
        };
    }

    // ── Navigation commands ──────────────────────────────────────────────────

    [RelayCommand]
    private void GoNext()
    {
        // ── Installer: trigger installation when on InstallingPage ──────────
        if (CurrentPage is InstallingPageViewModel installing && !installing.InstallComplete)
        {
            installing.StartInstallation();
            installing.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(InstallingPageViewModel.InstallComplete) && installing.InstallComplete)
                {
                    bool restartRequired = _config.AssocTtfm || _config.AssocOtfm || _config.AssocModcompatTtf || _config.AssocModcompatOtf || _config.AssocModcompatTtc;
                    if (restartRequired)
                    {
                        var restartPage = new RestartPageViewModel(_config);
                        Pages[Pages.Count - 1] = restartPage;
                    }
                    _currentIndex = Pages.IndexOf(installing) + 1;
                    NavigateToPage(_currentIndex);
                }
                else if (e.PropertyName == nameof(InstallingPageViewModel.InstallFailed) && installing.InstallFailed)
                {
                    var exitPage = new PrematureExitPageViewModel
                    {
                        Traceback = installing.ErrorMessage
                    };
                    CurrentPage = exitPage;
                    IsFinished = true;
                    ShowCancelButton = false;
                    CanNavigateBack = false;
                    CanNavigateNext = false;
                    ShowStepIndicator = false;
                }
                else if (e.PropertyName == nameof(InstallingPageViewModel.InstallCancelled) && installing.InstallCancelled)
                {
                    bool checkpointSaved = installing.CompletedFiles > 0;
                    var cancelPage = new CancelPageViewModel(wasInstalling: true, checkpointSaved: checkpointSaved);
                    CurrentPage = cancelPage;
                    IsFinished = true;
                    ShowCancelButton = false;
                    CanNavigateBack = false;
                    CanNavigateNext = false;
                    ShowStepIndicator = false;
                }
            };
            return;
        }

        // ── Uninstaller: trigger uninstallation when on UninstallingPage ────
        if (CurrentPage is UninstallingPageViewModel uninstalling && !uninstalling.UninstallComplete)
        {
            uninstalling.StartUninstallation();
            uninstalling.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(UninstallingPageViewModel.UninstallComplete) && uninstalling.UninstallComplete)
                {
                    _currentIndex = Pages.IndexOf(uninstalling) + 1;
                    NavigateToPage(_currentIndex);
                }
                else if (e.PropertyName == nameof(UninstallingPageViewModel.UninstallFailed) && uninstalling.UninstallFailed)
                {
                    var exitPage = new PrematureExitPageViewModel
                    {
                        Traceback = uninstalling.ErrorMessage
                    };
                    CurrentPage = exitPage;
                    IsFinished = true;
                    ShowCancelButton = false;
                    CanNavigateBack = false;
                    CanNavigateNext = false;
                    ShowStepIndicator = false;
                }
            };
            return;
        }

        // Block manual navigation if programs need to be closed
        if (CurrentPage is CloseProgramsPageViewModel closePage && closePage.HasRunningProcesses)
        {
            closePage.PageSubtitle = "Cannot proceed: Please close the running fModLoader instances listed below.";
            return;
        }

        if (_currentIndex < Pages.Count - 1)
        {
            CurrentPage?.OnNavigatedFrom();
            _currentIndex++;
            NavigateToPage(_currentIndex);
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        if (_currentIndex > 0 && !IsInstalling)
        {
            CurrentPage?.OnNavigatedFrom();
            _currentIndex--;
            NavigateToPage(_currentIndex);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        IsCancelConfirmationVisible = true;
    }

    [RelayCommand]
    private void ConfirmCancel()
    {
        IsCancelConfirmationVisible = false;

        if (CurrentPage is InstallingPageViewModel installing)
        {
            installing.CancelInstallation();
            return;
        }

        if (CurrentPage is UninstallingPageViewModel uninstalling)
        {
            uninstalling.CancelUninstallation();
            return;
        }

        var label = App.IsUninstallMode ? "Uninstall" : "Setup";
        var cancelPage = new CancelPageViewModel(wasInstalling: false, checkpointSaved: false, label: label);
        CurrentPage = cancelPage;
        IsFinished = true;
        ShowCancelButton = false;
        CanNavigateBack = false;
        CanNavigateNext = false;
        ShowStepIndicator = false;
    }

    [RelayCommand]
    private void CloseCancelModal()
    {
        IsCancelConfirmationVisible = false;
    }

    [RelayCommand]
    private void Finish()
    {
        bool shouldLaunch = !App.IsUninstallMode && _config.LaunchAfterInstall && CurrentPage is FinishPageViewModel;
        bool shouldRestart = CurrentPage is RestartPageViewModel restartPage && restartPage.RestartNow;

        var cleanupPage = new CleanupPageViewModel();
        cleanupPage.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CleanupPageViewModel.CleanupComplete) && cleanupPage.CleanupComplete)
            {
                if (shouldLaunch)
                {
                    try
                    {
                        var exePath = System.IO.Path.Combine(_config.TargetDirectory, _config.AppExeName);
                        if (System.IO.File.Exists(exePath))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = exePath,
                                UseShellExecute = true
                            });
                        }
                    }
                    catch { /* Best effort */ }
                }
                if (shouldRestart)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "shutdown.exe",
                            Arguments = "/r /t 0",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                    }
                    catch { /* Best effort */ }
                }
                Environment.Exit(0);
            }
        };

        CurrentPage = cleanupPage;
        cleanupPage.OnNavigatedTo();
        IsFinished = false;
        IsCleaningUp = true;
        ShowCancelButton = false;
        CanNavigateBack = false;
        CanNavigateNext = false;
        ShowStepIndicator = false;
    }

    // ── Page navigation logic ────────────────────────────────────────────────

    private void NavigateToPage(int index)
    {
        var page = Pages[index];
        CurrentPage = page;
        CurrentStep = index == 0 ? 0 : index - 1;
        page.OnNavigatedTo();

        bool isUninstallingPage = page is UninstallingPageViewModel;
        bool isInstallingPage = page is InstallingPageViewModel;

        // Update navigation state
        CanNavigateBack = index > 1 && index < Pages.Count - 2 && page.CanGoBack;
        IsInstalling = isInstallingPage || isUninstallingPage;
        IsFinished = page is FinishPageViewModel || page is RestartPageViewModel;
        ShowCancelButton = !IsInstalling && !IsFinished;
        ShowStepIndicator = page is not PreparePageViewModel
                          && page is not CancelPageViewModel
                          && page is not PrematureExitPageViewModel
                          && page is not RestartPageViewModel;

        // Update Next button text
        if (page is ReadyPageViewModel)
            NextButtonText = "Install";
        else if (isInstallingPage)
            NextButtonText = "Installing…";
        else if (isUninstallingPage)
            NextButtonText = "Uninstalling…";
        else if (page is FinishPageViewModel || page is RestartPageViewModel)
            NextButtonText = "Finish";
        else
            NextButtonText = "Next";

        CanNavigateNext = page.CanGoNext && !IsFinished;

        if (page is PreparePageViewModel)
        {
            CanNavigateBack = false;
            CanNavigateNext = false;
            ShowCancelButton = false;
        }

        // Subscribe to property changes from the page
        page.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(WizardPageBase.CanGoNext))
                CanNavigateNext = page.CanGoNext && !IsFinished;
            if (e.PropertyName == nameof(WizardPageBase.CanGoBack))
                CanNavigateBack = index > 1 && index < Pages.Count - 2 && page.CanGoBack;
        };
    }
}
