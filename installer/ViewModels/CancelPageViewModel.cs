using System;

namespace FModLoaderInstaller.ViewModels;

public partial class CancelPageViewModel : WizardPageBase
{
    private readonly string _cancelMessage;

    public CancelPageViewModel(bool wasInstalling = false, bool checkpointSaved = false, string label = "Setup")
    {
        PageTitle = $"{label} Canceled";
        PageSubtitle = $"fModLoader {label} was not completed.";
        CanGoBack = false;
        CanGoNext = false;

        if (checkpointSaved)
        {
            _cancelMessage = $"The fModLoader {label.ToLower()} has been canceled.\n\n" +
                             "A checkpoint was saved. We'll continue from where we left off once you restart the installer.\n\n" +
                             "Click Finish to exit the setup wizard.";
        }
        else if (wasInstalling)
        {
            _cancelMessage = $"The fModLoader {label.ToLower()} has been canceled.\n\n" +
                             "All changes were undone.\n\n" +
                             "Click Finish to exit the setup wizard.";
        }
        else
        {
            _cancelMessage = $"The fModLoader {label.ToLower()} has been canceled.\n\n" +
                             "No modifications have been made to your system.\n\n" +
                             "Click Finish to exit the setup wizard.";
        }
    }

    public string CancelMessage => _cancelMessage;
}

