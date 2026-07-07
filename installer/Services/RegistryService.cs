using System;
using FModLoaderInstaller.Models;
using Microsoft.Win32;

namespace FModLoaderInstaller.Services;

/// <summary>
/// Handles all Windows Registry operations for the installer.
/// </summary>
public class RegistryService
{
    /// <summary>
    /// Writes file association registry entries matching the original ISS installer.
    /// </summary>
    public void WriteFileAssociations(InstallerConfig config)
    {
        var appPath = System.IO.Path.Combine(config.TargetDirectory, config.AppExeName);

        // ── .TTFM → fModLoader ──────────────────────────────────────────
        if (config.AssocTtfm)
        {
            WriteAssociation(".ttfm", "fModLoader.ttfm", "fModLoader Mod Package", appPath);
        }

        // ── .OTFM → fModLoader ──────────────────────────────────────────
        if (config.AssocOtfm)
        {
            WriteAssociation(".otfm", "fModLoader.otfm", "fModLoader OTF Mod Package", appPath);
        }

        // ── .MODCOMPAT extensions → OpenWithProgIds ─────────────────────
        if (config.AssocModcompatTtf)
            WriteOpenWith(".ttf", "fModLoader.modcompat");
        if (config.AssocModcompatOtf)
            WriteOpenWith(".otf", "fModLoader.modcompat");
        if (config.AssocModcompatTtc)
            WriteOpenWith(".ttc", "fModLoader.modcompat");

        // Write the modcompat ProgId if any modcompat association is enabled
        if (config.AssocModcompatTtf || config.AssocModcompatOtf || config.AssocModcompatTtc)
        {
            using var key = Registry.ClassesRoot.CreateSubKey("fModLoader.modcompat");
            key?.SetValue("", "fModLoader ModCompat Font");

            using var iconKey = Registry.ClassesRoot.CreateSubKey(@"fModLoader.modcompat\DefaultIcon");
            iconKey?.SetValue("", $"{appPath},0");

            using var cmdKey = Registry.ClassesRoot.CreateSubKey(@"fModLoader.modcompat\shell\open\command");
            cmdKey?.SetValue("", $"\"{appPath}\" \"%1\"");

            // Font viewer association
            WriteFontViewer(config);
        }
    }

    /// <summary>
    /// Writes app path and version to HKLM.
    /// </summary>
    public void WriteAppPath(InstallerConfig config)
    {
        var subkey = $@"Software\{config.AppPublisher}\{config.AppShortName}";
        using var key = Registry.LocalMachine.CreateSubKey(subkey);
        key?.SetValue("InstallPath", config.TargetDirectory);
        key?.SetValue("Version", config.AppVersion);
    }

    /// <summary>
    /// Registers in Add/Remove Programs for clean uninstallation.
    /// </summary>
    public void WriteUninstallEntry(InstallerConfig config)
    {
        var subkey = $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{config.AppShortName}";
        var uninstallExe = System.IO.Path.Combine(config.TargetDirectory, "uninstall.exe");

        using var key = Registry.LocalMachine.CreateSubKey(subkey);
        if (key == null) return;

        key.SetValue("DisplayName", config.AppName);
        key.SetValue("DisplayVersion", config.AppVersion);
        key.SetValue("Publisher", config.AppPublisher);
        key.SetValue("InstallLocation", config.TargetDirectory);
        key.SetValue("UninstallString", $"\"{uninstallExe}\"");
        key.SetValue("URLInfoAbout", config.AppUrl);
        key.SetValue("URLUpdateInfo", $"{config.AppUrl}/releases");
        key.SetValue("HelpLink", $"{config.AppUrl}/issues");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    public void RemoveUninstallEntry(InstallerConfig config)
    {
        var subkey = $@"Software\Microsoft\Windows\CurrentVersion\Uninstall";
        using var key = Registry.LocalMachine.OpenSubKey(subkey, writable: true);
        key?.DeleteSubKeyTree(config.AppShortName, throwOnMissingSubKey: false);
    }

    public void RemoveAppPath(InstallerConfig config)
    {
        var subkey = $@"Software\{config.AppPublisher}";
        using var key = Registry.LocalMachine.OpenSubKey(subkey, writable: true);
        key?.DeleteSubKeyTree(config.AppShortName, throwOnMissingSubKey: false);
    }

    public void RemoveFileAssociations(InstallerConfig config)
    {
        try
        {
            Registry.ClassesRoot.DeleteSubKeyTree(".ttfm", throwOnMissingSubKey: false);
            Registry.ClassesRoot.DeleteSubKeyTree("fModLoader.ttfm", throwOnMissingSubKey: false);
            Registry.ClassesRoot.DeleteSubKeyTree(".otfm", throwOnMissingSubKey: false);
            Registry.ClassesRoot.DeleteSubKeyTree("fModLoader.otfm", throwOnMissingSubKey: false);
            Registry.ClassesRoot.DeleteSubKeyTree("fModLoader.modcompat", throwOnMissingSubKey: false);

            RemoveOpenWith(".ttf", "fModLoader.modcompat");
            RemoveOpenWith(".otf", "fModLoader.modcompat");
            RemoveOpenWith(".ttc", "fModLoader.modcompat");
        }
        catch { /* Best effort */ }
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private void WriteAssociation(string ext, string progId, string description, string appPath)
    {
        using var extKey = Registry.ClassesRoot.CreateSubKey(ext);
        extKey?.SetValue("", progId);

        using var progKey = Registry.ClassesRoot.CreateSubKey(progId);
        progKey?.SetValue("", description);

        using var iconKey = Registry.ClassesRoot.CreateSubKey($@"{progId}\DefaultIcon");
        iconKey?.SetValue("", $"{appPath},0");

        using var cmdKey = Registry.ClassesRoot.CreateSubKey($@"{progId}\shell\open\command");
        cmdKey?.SetValue("", $"\"{appPath}\" \"%1\"");
    }

    private void WriteOpenWith(string ext, string progId)
    {
        using var key = Registry.ClassesRoot.CreateSubKey($@"{ext}\OpenWithProgids");
        key?.SetValue(progId, "");
    }

    private void RemoveOpenWith(string ext, string progId)
    {
        using var key = Registry.ClassesRoot.OpenSubKey($@"{ext}\OpenWithProgids", writable: true);
        key?.DeleteValue(progId, throwOnMissingValue: false);
    }

    private void WriteFontViewer(InstallerConfig config)
    {
        if (config.FontViewerChoice == 2) return; // "None"

        string viewerExe;
        if (config.FontViewerChoice == 0)
            viewerExe = @"%SystemRoot%\System32\fontview.exe ""%1""";
        else
            viewerExe = @"""C:\Program Files (x86)\Adobe\Adobe Font Manager\AFM.exe"" ""%1""";

        using var cmdKey = Registry.ClassesRoot.CreateSubKey(@"fModLoader.modcompat\shell\preview\command");
        cmdKey?.SetValue("", viewerExe);
    }
}
