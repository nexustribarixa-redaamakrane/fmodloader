namespace FModLoaderInstaller.Models;

/// <summary>
/// Shared state passed between uninstaller wizard pages.
/// </summary>
public class UninstallConfig
{
    /// <summary>
    /// When true the file associations (.ttfm, .otfm, .modcompat etc.) are
    /// preserved on the system. When false they are deleted during uninstall.
    /// </summary>
    public bool KeepFileAssociations { get; set; } = false;
}
