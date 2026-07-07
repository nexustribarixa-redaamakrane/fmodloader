namespace FModLoaderInstaller.Models;

/// <summary>
/// Progress event data during installation.
/// </summary>
public class InstallerProgress
{
    public int TotalFiles { get; set; }
    public int CompletedFiles { get; set; }
    public string CurrentFile { get; set; } = "";
    public string StatusMessage { get; set; } = "";
    public double Percentage => TotalFiles > 0 ? (double)CompletedFiles / TotalFiles * 100.0 : 0;
    public bool IsComplete => CompletedFiles >= TotalFiles && TotalFiles > 0;
}
