namespace FModLoaderInstaller.Models;

/// <summary>
/// Shared installer state passed between wizard pages.
/// </summary>
public class InstallerConfig
{
    // ── Identity ────────────────────────────────────────────────────────────
    public string AppName { get; } = "fModLoader BETA v1.0.65";
    public string AppShortName { get; } = "fModLoader";
    public string AppVersion { get; } = "1.0.65";
    public string AppPublisher { get; } = "Nexus Tribarixa";
    public string AppUrl { get; } = "https://github.com/nexustribarixa-redaamakrane/fmodloader";
    public string AppExeName { get; } = "fModLoader.exe";
    public string AppCLIName { get; } = "fModLoader_CLI.exe";
    public Guid AppId { get; } = Guid.Parse("D3A8F2C1-5B4E-4D7F-9E2A-1C6B8F3E7D9A");

    // ── User choices ────────────────────────────────────────────────────────
    public string Language { get; set; } = "English";
    public string TargetDirectory { get; set; } = @"C:\Program Files\fModLoader";
    public string StartMenuGroup { get; set; } = "fModLoader";
    public bool CreateStartMenu { get; set; } = true;

    // ── File associations ───────────────────────────────────────────────────
    public bool AssocModcompatTtf { get; set; } = true;
    public bool AssocModcompatOtf { get; set; } = true;
    public bool AssocModcompatTtc { get; set; } = true;
    public bool AssocTtfm { get; set; } = true;
    public bool AssocOtfm { get; set; } = true;

    // ── Shortcuts ───────────────────────────────────────────────────────────
    public bool CreateDesktopShortcut { get; set; } = false;
    public bool CreateStartupShortcut { get; set; } = false;

    // ── Post-install ────────────────────────────────────────────────────────
    public bool LaunchAfterInstall { get; set; } = true;

    // ── Font viewer association ──────────────────────────────────────────────
    /// <summary>0 = Windows Font Viewer, 1 = Adobe Font Manager, 2 = None</summary>
    public int FontViewerChoice { get; set; } = 0;
}
