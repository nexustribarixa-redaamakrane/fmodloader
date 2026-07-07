using FModLoaderInstaller.Models;

namespace FModLoaderInstaller.ViewModels;

public partial class ReadyPageViewModel : WizardPageBase
{
    private readonly InstallerConfig _config;

    public ReadyPageViewModel(InstallerConfig config)
    {
        _config = config;
        PageTitle = "Ready to Install";
        PageSubtitle = "Setup is ready to begin installation.";
    }

    public string Summary
    {
        get
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"Application:  {_config.AppName}");
            lines.AppendLine($"Destination:  {_config.TargetDirectory}");
            lines.AppendLine($"Start Menu:   {(_config.CreateStartMenu ? _config.StartMenuGroup : "Not created")}");
            lines.AppendLine();
            lines.AppendLine("File Associations:");
            if (_config.AssocModcompatTtf) lines.AppendLine("  ✓ .MODCOMPAT.TTF");
            if (_config.AssocModcompatOtf) lines.AppendLine("  ✓ .MODCOMPAT.OTF");
            if (_config.AssocModcompatTtc) lines.AppendLine("  ✓ .MODCOMPAT.TTC");
            if (_config.AssocTtfm) lines.AppendLine("  ✓ .TTFM");
            if (_config.AssocOtfm) lines.AppendLine("  ✓ .OTFM");
            if (!_config.AssocModcompatTtf && !_config.AssocModcompatOtf &&
                !_config.AssocModcompatTtc && !_config.AssocTtfm && !_config.AssocOtfm)
                lines.AppendLine("  None selected");
            lines.AppendLine();
            lines.AppendLine("Shortcuts:");
            if (_config.CreateDesktopShortcut) lines.AppendLine("  ✓ Desktop shortcut");
            if (_config.CreateStartupShortcut) lines.AppendLine("  ✓ Launch on startup");
            if (!_config.CreateDesktopShortcut && !_config.CreateStartupShortcut)
                lines.AppendLine("  None selected");
            return lines.ToString();
        }
    }

    public override void OnNavigatedTo()
    {
        OnPropertyChanged(nameof(Summary));
    }
}
