using CommunityToolkit.Mvvm.ComponentModel;
using FModLoaderInstaller.Models;

namespace FModLoaderInstaller.ViewModels;

public partial class RestartPageViewModel : WizardPageBase
{
    private readonly InstallerConfig _config;

    [ObservableProperty] private bool _restartNow = true;

    public RestartPageViewModel(InstallerConfig config)
    {
        _config = config;
        PageTitle = "Restart System";
        PageSubtitle = "System restart is required to apply file associations.";
        CanGoBack = false;
        CanGoNext = false;
    }

    public string RestartMessage =>
        "To complete the installation of fModLoader, you must restart your computer.\n\n" +
        "Would you like to restart now?";
}
