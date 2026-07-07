using CommunityToolkit.Mvvm.ComponentModel;
using FModLoaderInstaller.Models;

namespace FModLoaderInstaller.ViewModels;

public partial class FinishPageViewModel : WizardPageBase
{
    private readonly InstallerConfig _config;
    private readonly bool _isUninstall;

    [ObservableProperty] private bool _launchAfterInstall;

    public FinishPageViewModel(InstallerConfig config, bool isUninstall = false)
    {
        _config = config;
        _isUninstall = isUninstall;

        if (isUninstall)
        {
            PageTitle = "Uninstallation Complete";
            PageSubtitle = "fModLoader BETA v1.0.6 has been removed successfully.";
            _launchAfterInstall = false;
        }
        else
        {
            _launchAfterInstall = config.LaunchAfterInstall;
            PageTitle = "Installation Complete";
            PageSubtitle = "fModLoader BETA v1.0.6 has been installed successfully.";
        }

        CanGoBack = false;
        CanGoNext = false;
    }

    partial void OnLaunchAfterInstallChanged(bool value)
    {
        if (!_isUninstall)
            _config.LaunchAfterInstall = value;
    }

    public bool ShowLaunchOption => !_isUninstall;

    public string FinishMessage => _isUninstall
        ? "fModLoader BETA v1.0.6 has been successfully removed from your computer.\n\n" +
          "Thank you for using fModLoader."
        : "fModLoader BETA v1.0.6 has been successfully installed on your computer.\n\n" +
          "The application may be launched by selecting the installed shortcuts.";
}

