namespace FModLoaderInstaller.ViewModels;

public partial class WelcomePageViewModel : WizardPageBase
{
    private readonly bool _isUninstall;

    public WelcomePageViewModel(bool isUninstall = false)
    {
        _isUninstall = isUninstall;
        if (isUninstall)
        {
            PageTitle = "Welcome to fModLoader Uninstall";
            PageSubtitle = "BETA v1.0.6 — Remove fModLoader";
        }
        else
        {
            PageTitle = "Welcome to fModLoader Setup";
            PageSubtitle = "BETA v1.0.6 — \"Project Horde\"";
        }
        CanGoBack = false;
    }

    public string WelcomeMessage => _isUninstall
        ? "This wizard will remove fModLoader BETA v1.0.6 from your computer.\n\n" +
          "It is recommended that you close all other applications, including fModLoader itself, before continuing."
        : "This wizard will guide you through the installation of fModLoader BETA v1.0.6 on your computer.\n\n" +
          "It is recommended that you close all other applications before continuing.";

    public string Instructions => _isUninstall
        ? "Click Next to continue, or Cancel to exit Uninstall."
        : "Click Next to continue, or Cancel to exit Setup.";
}
