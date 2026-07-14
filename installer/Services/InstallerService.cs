using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using FModLoaderInstaller.Models;

namespace FModLoaderInstaller.Services;

/// <summary>
/// Core installation service — copies payload files to target directory.
/// </summary>
public class InstallerService
{
    /// <summary>
    /// Copies all files from the payload directory to the target install directory.
    /// </summary>
    public async Task InstallAsync(InstallerConfig config, IProgress<InstallerProgress> progress, CancellationToken cancellationToken = default)
    {
        var copiedFiles = new List<string>();
        var exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        var exeDir = !string.IsNullOrEmpty(exePath) ? Path.GetDirectoryName(exePath) : AppDomain.CurrentDomain.BaseDirectory;
        exeDir ??= AppDomain.CurrentDomain.BaseDirectory;
        
        var filesToInstall = GetFilesToInstall(exeDir, out string errorMessage);
        if (filesToInstall.Count == 0 && !string.IsNullOrEmpty(errorMessage))
        {
            throw new DirectoryNotFoundException(errorMessage);
        }

        var totalFiles = filesToInstall.Count;

        // Create target directory
        Directory.CreateDirectory(config.TargetDirectory);

        var report = new InstallerProgress { TotalFiles = totalFiles };

        for (int i = 0; i < filesToInstall.Count; i++)
        {
            var (sourceFile, relativePath) = filesToInstall[i];
            var targetPath = Path.Combine(config.TargetDirectory, relativePath);

            try
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Ensure subdirectory exists
                var targetDir = Path.GetDirectoryName(targetPath);
                if (targetDir != null)
                    Directory.CreateDirectory(targetDir);

                // Copy file
                report.CurrentFile = relativePath;
                report.StatusMessage = $"Copying: {relativePath}";
                report.CompletedFiles = i;
                progress.Report(report);

                await Task.Run(() => File.Copy(sourceFile, targetPath, overwrite: true), cancellationToken);
                copiedFiles.Add(targetPath);

                // Small delay for visual feedback on fast installs
                if (totalFiles < 50)
                    await Task.Delay(30, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // On cancel, if files were copied, try saving checkpoint. Otherwise rollback.
                if (copiedFiles.Count > 0)
                {
                    SaveCheckpoint(config, copiedFiles);
                }
                else
                {
                    Rollback(config);
                }
                throw;
            }
        }

        // --- Save Uninstall Information ---
        var uninstallDataPath = Path.Combine(config.TargetDirectory, "uninstall.dat");
        var uninstallData = new {
            Files = copiedFiles,
            Directories = new[] { config.TargetDirectory },
            Shortcuts = new[] {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "fModLoader.lnk"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "fModLoader.lnk")
            }
        };
        await File.WriteAllTextAsync(uninstallDataPath, JsonSerializer.Serialize(uninstallData), cancellationToken);

        // --- Copy Uninstaller ---
        var currentExe = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (currentExe != null)
        {
            var uninstallExePath = Path.Combine(config.TargetDirectory, "uninstall.exe");
            File.Copy(currentExe, uninstallExePath, overwrite: true);
        }

        report.CompletedFiles = totalFiles;
        report.StatusMessage = "All files copied successfully.";
        report.CurrentFile = "";
        progress.Report(report);
    }

    /// <summary>
    /// Saves a checkpoint of the current installation progress to allow resuming later.
    /// </summary>
    public void SaveCheckpoint(InstallerConfig config, List<string> installedFiles)
    {
        try
        {
            var checkpointData = new {
                InstalledFiles = installedFiles,
                TargetDirectory = config.TargetDirectory
            };
            var checkpointPath = Path.Combine(config.TargetDirectory, ".install_checkpoint");
            File.WriteAllText(checkpointPath, JsonSerializer.Serialize(checkpointData));
        }
        catch { /* Best effort */ }
    }

    public void Rollback(InstallerConfig config)
    {
        try
        {
            if (Directory.Exists(config.TargetDirectory))
                Directory.Delete(config.TargetDirectory, recursive: true);
        }
        catch { /* Best effort rollback */ }
    }

    private List<(string SourcePath, string RelativePath)> GetFilesToInstall(string exeDir, out string errorMessage)
    {
        errorMessage = "";
        var list = new List<(string SourcePath, string RelativePath)>();
        var payloadDir = Path.Combine(exeDir, "payload");

        if (Directory.Exists(payloadDir))
        {
            foreach (var file in Directory.GetFiles(payloadDir, "*", SearchOption.AllDirectories))
            {
                list.Add((file, Path.GetRelativePath(payloadDir, file)));
            }
            return list;
        }

        // Development fallback: look for solution root by walking up
        string? projectRoot = null;
        var dir = exeDir;
        for (int i = 0; i < 6; i++)
        {
            if (Directory.Exists(Path.Combine(dir, "app")) && Directory.Exists(Path.Combine(dir, "cli")))
            {
                projectRoot = dir;
                break;
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == null || parent == dir) break;
            dir = parent;
        }

        if (projectRoot == null)
        {
            errorMessage = $"Payload directory not found. Expected at:\n{payloadDir}\n\n" +
                           "Please ensure the payload folder is next to the installer executable.";
            return list;
        }

        // Let's resolve the App build directory
        string? appBuildDir = FindBestBuildDir(Path.Combine(projectRoot, "app"), true);
        if (appBuildDir != null)
        {
            foreach (var file in Directory.GetFiles(appBuildDir, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(appBuildDir, file);
                list.Add((file, rel));
            }
        }
        else
        {
            errorMessage = $"Could not locate built fModLoader GUI executable under {Path.Combine(projectRoot, "app")}. Please build the project first.";
            return list;
        }

        // Let's resolve the CLI build directory
        string? cliBuildDir = FindBestBuildDir(Path.Combine(projectRoot, "cli"), false);
        if (cliBuildDir != null)
        {
            foreach (var file in Directory.GetFiles(cliBuildDir, "*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(cliBuildDir, file);
                // In case of conflict, we overwrite, so just add
                list.Add((file, rel));
            }
        }

        // Copy fonts from project root
        var fontsSrcDir = Path.Combine(projectRoot, "fonts");
        if (Directory.Exists(fontsSrcDir))
        {
            foreach (var file in Directory.GetFiles(fontsSrcDir, "*", SearchOption.AllDirectories))
            {
                var rel = Path.Combine("fonts", Path.GetRelativePath(fontsSrcDir, file));
                list.Add((file, rel));
            }
        }

        // Copy mods from project root
        var modsSrcDir = Path.Combine(projectRoot, "mods");
        if (Directory.Exists(modsSrcDir))
        {
            foreach (var file in Directory.GetFiles(modsSrcDir, "*", SearchOption.AllDirectories))
            {
                var rel = Path.Combine("mods", Path.GetRelativePath(modsSrcDir, file));
                list.Add((file, rel));
            }
        }

        return list;
    }

    private string? FindBestBuildDir(string projectPath, bool isWindowsTarget)
    {
        string[] configurations = { "Release", "Debug" };
        string[] frameworks = { 
            "net9.0-windows", "net9.0", 
            "net8.0-windows", "net8.0", 
            "net7.0-windows", "net7.0", 
            "net6.0-windows", "net6.0" 
        };

        foreach (var config in configurations)
        {
            foreach (var fw in frameworks)
            {
                var basePath = Path.Combine(projectPath, "bin", config, fw);
                if (Directory.Exists(basePath))
                {
                    var exeName = isWindowsTarget ? "fModLoader.exe" : "fModLoader_CLI.exe";
                    if (File.Exists(Path.Combine(basePath, exeName)))
                    {
                        return basePath;
                    }

                    // Check subdirectories (e.g. win-x64)
                    try
                    {
                        foreach (var subDir in Directory.GetDirectories(basePath))
                        {
                            if (File.Exists(Path.Combine(subDir, exeName)))
                            {
                                return subDir;
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        return null;
    }
}
