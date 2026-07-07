using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FModLoaderInstaller.ViewModels;

public partial class PrematureExitPageViewModel : WizardPageBase
{
    [ObservableProperty] private string _traceback = "";

    public PrematureExitPageViewModel()
    {
        PageTitle = "Setup Ended Prematurely";
        PageSubtitle = "The setup wizard was interrupted.";
        CanGoBack = false;
        CanGoNext = false;
    }

    public string FailureMessage =>
        "The setup was interrupted before fModLoader could be fully installed.\n\n" +
        "Your system has not been modified. To install this program at a later time, please run setup again.\n\n" +
        "See the details below for the error traceback:";
}
