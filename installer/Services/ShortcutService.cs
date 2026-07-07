using System;
using System.IO;
using System.Runtime.InteropServices;
using FModLoaderInstaller.Models;

namespace FModLoaderInstaller.Services;

/// <summary>
/// Creates Start Menu, Desktop, and Startup shortcuts via COM IShellLink.
/// </summary>
public class ShortcutService
{
    public void CreateShortcuts(InstallerConfig config)
    {
        var appExe = Path.Combine(config.TargetDirectory, config.AppExeName);
        var cliExe = Path.Combine(config.TargetDirectory, config.AppCLIName);

        // ── Start Menu shortcuts ────────────────────────────────────────
        if (config.CreateStartMenu)
        {
            var startMenuBase = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
            var groupPath = Path.Combine(startMenuBase, "Programs", config.StartMenuGroup);
            Directory.CreateDirectory(groupPath);

            CreateShortcut(
                Path.Combine(groupPath, $"{config.AppShortName}.lnk"),
                appExe, config.TargetDirectory, "Launch fModLoader");

            CreateShortcut(
                Path.Combine(groupPath, $"{config.AppShortName} CLI.lnk"),
                cliExe, config.TargetDirectory, "fModLoader Command-Line Tool");

            // Uninstall shortcut pointing to the uninstaller
            var uninstallExe = Path.Combine(config.TargetDirectory, "uninstall.exe");
            if (File.Exists(uninstallExe))
            {
                CreateShortcut(
                    Path.Combine(groupPath, $"Uninstall {config.AppShortName}.lnk"),
                    uninstallExe, config.TargetDirectory, $"Uninstall {config.AppShortName}");
            }
        }

        // ── Desktop shortcut ────────────────────────────────────────────
        if (config.CreateDesktopShortcut)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
            CreateShortcut(
                Path.Combine(desktop, $"{config.AppShortName}.lnk"),
                appExe, config.TargetDirectory, "Launch fModLoader");
        }

        // ── Startup shortcut ────────────────────────────────────────────
        if (config.CreateStartupShortcut)
        {
            var startup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
            CreateShortcut(
                Path.Combine(startup, $"{config.AppShortName}.lnk"),
                appExe, config.TargetDirectory, "fModLoader Auto-Start");
        }
    }

    /// <summary>
    /// Creates a .lnk shortcut file using COM IShellLink.
    /// </summary>
    private void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string description)
    {
        try
        {
            // Use WScript.Shell COM object for shortcut creation
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = workingDir;
            shortcut.Description = description;
            shortcut.IconLocation = $"{targetPath},0";
            shortcut.Save();

            Marshal.ReleaseComObject(shortcut);
            Marshal.ReleaseComObject(shell);
        }
        catch
        {
            // Silently skip if COM is unavailable
        }
    }
}
