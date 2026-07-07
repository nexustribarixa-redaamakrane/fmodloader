using CommunityToolkit.Mvvm.ComponentModel;
using FModLoaderInstaller.Models;

namespace FModLoaderInstaller.ViewModels;

/// <summary>
/// Lets the user choose whether to keep or delete file associations during uninstall.
/// </summary>
public partial class UninstallExtensionsPageViewModel : WizardPageBase
{
    private readonly UninstallConfig _uninstallConfig;

    [ObservableProperty] private bool _keepExtensions = false;
    [ObservableProperty] private bool _deleteExtensions = true;

    public UninstallExtensionsPageViewModel(UninstallConfig uninstallConfig)
    {
        _uninstallConfig = uninstallConfig;
        PageTitle = "File Associations";
        PageSubtitle = "Choose what to do with fModLoader file associations.";
        CanGoBack = true;
        CanGoNext = true;

        // Sync initial value
        _uninstallConfig.KeepFileAssociations = _keepExtensions;
    }

    partial void OnKeepExtensionsChanged(bool value)
    {
        _uninstallConfig.KeepFileAssociations = value;
        _deleteExtensions = !value;
        OnPropertyChanged(nameof(DeleteExtensions));
    }

    partial void OnDeleteExtensionsChanged(bool value)
    {
        _uninstallConfig.KeepFileAssociations = !value;
        _keepExtensions = !value;
        OnPropertyChanged(nameof(KeepExtensions));
    }
}
