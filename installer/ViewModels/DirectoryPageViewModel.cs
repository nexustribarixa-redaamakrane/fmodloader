using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FModLoaderInstaller.Models;

namespace FModLoaderInstaller.ViewModels;

public partial class DirectoryPageViewModel : WizardPageBase
{
    private readonly InstallerConfig _config;
    private readonly bool _readOnly;

    [ObservableProperty] private string _targetDirectory;
    [ObservableProperty] private string _spaceInfo = "";

    public bool IsReadOnly => _readOnly;

    public DirectoryPageViewModel(InstallerConfig config, bool readOnly = false)
    {
        _config = config;
        _readOnly = readOnly;
        _targetDirectory = config.TargetDirectory;

        if (readOnly)
        {
            PageTitle = "Installation Location";
            PageSubtitle = "fModLoader will be removed from the following directory.";
            CanGoNext = true;
        }
        else
        {
            PageTitle = "Select Destination";
            PageSubtitle = "Choose the folder where fModLoader will be installed.";
        }

        UpdateSpaceInfo();
    }

    partial void OnTargetDirectoryChanged(string value)
    {
        if (_readOnly) return; // Don't allow editing in read-only mode
        _config.TargetDirectory = value;
        CanGoNext = !string.IsNullOrWhiteSpace(value);
        UpdateSpaceInfo();
    }

    private void UpdateSpaceInfo()
    {
        try
        {
            var root = System.IO.Path.GetPathRoot(TargetDirectory);
            if (!string.IsNullOrEmpty(root))
            {
                var drive = new System.IO.DriveInfo(root);
                if (drive.IsReady)
                {
                    var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                    SpaceInfo = $"Drive {drive.Name} — {freeGb:F1} GB free";
                    return;
                }
            }
        }
        catch { /* ignore */ }
        SpaceInfo = "";
    }

    [RelayCommand]
    private void BrowseDirectory()
    {
        // This is handled via code-behind event handler BrowseButton_Click in DirectoryPage.axaml.cs
    }

    public override void OnNavigatedFrom()
    {
        _config.TargetDirectory = TargetDirectory;
    }
}
