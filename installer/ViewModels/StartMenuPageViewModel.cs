using CommunityToolkit.Mvvm.ComponentModel;
using FModLoaderInstaller.Models;

namespace FModLoaderInstaller.ViewModels;

public partial class StartMenuPageViewModel : WizardPageBase
{
    private readonly InstallerConfig _config;

    [ObservableProperty] private string _groupName;
    [ObservableProperty] private bool _createStartMenu;

    public StartMenuPageViewModel(InstallerConfig config)
    {
        _config = config;
        _groupName = config.StartMenuGroup;
        _createStartMenu = config.CreateStartMenu;
        PageTitle = "Start Menu Folder";
        PageSubtitle = "Choose the Start Menu folder for the program's shortcuts.";
    }

    public override void OnNavigatedFrom()
    {
        _config.StartMenuGroup = GroupName;
        _config.CreateStartMenu = CreateStartMenu;
    }
}
