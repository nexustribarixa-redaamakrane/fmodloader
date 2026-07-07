using CommunityToolkit.Mvvm.ComponentModel;
using FModLoaderInstaller.Models;

namespace FModLoaderInstaller.ViewModels;

public partial class TasksPageViewModel : WizardPageBase
{
    private readonly InstallerConfig _config;

    // ── File Associations ───────────────────────────────────────────────────
    [ObservableProperty] private bool _assocModcompatTtf;
    [ObservableProperty] private bool _assocModcompatOtf;
    [ObservableProperty] private bool _assocModcompatTtc;
    [ObservableProperty] private bool _assocTtfm;
    [ObservableProperty] private bool _assocOtfm;

    // ── Shortcuts ───────────────────────────────────────────────────────────
    [ObservableProperty] private bool _createDesktopShortcut;
    [ObservableProperty] private bool _createStartupShortcut;

    // ── Font viewer ─────────────────────────────────────────────────────────
    [ObservableProperty] private int _fontViewerChoice;

    public string[] FontViewerOptions { get; } = new[]
    {
        "Windows Font Viewer",
        "Adobe Font Manager",
        "None"
    };

    public TasksPageViewModel(InstallerConfig config)
    {
        _config = config;
        _assocModcompatTtf = config.AssocModcompatTtf;
        _assocModcompatOtf = config.AssocModcompatOtf;
        _assocModcompatTtc = config.AssocModcompatTtc;
        _assocTtfm = config.AssocTtfm;
        _assocOtfm = config.AssocOtfm;
        _createDesktopShortcut = config.CreateDesktopShortcut;
        _createStartupShortcut = config.CreateStartupShortcut;
        _fontViewerChoice = config.FontViewerChoice;
        PageTitle = "Additional Tasks";
        PageSubtitle = "Select the additional tasks you would like to perform.";
    }

    public override void OnNavigatedFrom()
    {
        _config.AssocModcompatTtf = AssocModcompatTtf;
        _config.AssocModcompatOtf = AssocModcompatOtf;
        _config.AssocModcompatTtc = AssocModcompatTtc;
        _config.AssocTtfm = AssocTtfm;
        _config.AssocOtfm = AssocOtfm;
        _config.CreateDesktopShortcut = CreateDesktopShortcut;
        _config.CreateStartupShortcut = CreateStartupShortcut;
        _config.FontViewerChoice = FontViewerChoice;
    }
}
