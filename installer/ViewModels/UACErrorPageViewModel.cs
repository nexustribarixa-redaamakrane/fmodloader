using System;
using CommunityToolkit.Mvvm.Input;
using FModLoaderInstaller.Services;

namespace FModLoaderInstaller.ViewModels;

public partial class UACErrorPageViewModel : WizardPageBase
{
    public UACErrorPageViewModel()
    {
        PageTitle = "UAC Privileges Required";
        PageSubtitle = "The installer requires administrator privileges to continue.";
        CanGoNext = false;
        CanGoBack = false;
    }

    public string ErrorMessage =>
        "The setup cannot proceed because it does not have the necessary " +
        "system-level access to install and configure required components.\n\n" +
        "To resolve this, you must run the installer with elevated administrative privileges.";

    public string Instructions =>
        "Please restart the application, choosing 'Run as administrator'.";

    [RelayCommand]
    private void RestartAsAdmin()
    {
        if (ElevationService.RelaunchAsAdmin())
        {
            Environment.Exit(0);
        }
    }
}
