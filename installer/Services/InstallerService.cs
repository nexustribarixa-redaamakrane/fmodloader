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
        // Locate the payload directory (next to the installer executable)
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var payloadDir = Path.Combine(exeDir, "payload");

        // Fallback: check for dist/fModLoader relative to project root
        if (!Directory.Exists(payloadDir))
        {
            var projectRoot = Path.GetFullPath(Path.Combine(exeDir, ".."));
            payloadDir = Path.Combine(projectRoot, "dist", "fModLoader");
        }
        if (!Directory.Exists(payloadDir))
        {
            // Development fallback — try relative to the source
            var devPath = Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "dist", "fModLoader"));
            if (Directory.Exists(devPath))
                payloadDir = devPath;
        }

        if (!Directory.Exists(payloadDir))
            throw new DirectoryNotFoundException(
                $"Payload directory not found. Expected at:\n{Path.Combine(exeDir, "payload")}\n\n" +
                "Please ensure the payload folder is next to the installer executable.");

        // Enumerate all files
        var allFiles = Directory.GetFiles(payloadDir, "*", SearchOption.AllDirectories);
        var totalFiles = allFiles.Length;

        // Create target directory
        Directory.CreateDirectory(config.TargetDirectory);

        var report = new InstallerProgress { TotalFiles = totalFiles };

        for (int i = 0; i < allFiles.Length; i++)
        {
            var sourceFile = allFiles[i];
            var relativePath = Path.GetRelativePath(payloadDir, sourceFile);
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
        var currentExe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
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

    /// <summary>
    /// Removes all installed files (for rollback on failure).
    /// </summary>
    public void Rollback(InstallerConfig config)
    {
        try
        {
            if (Directory.Exists(config.TargetDirectory))
                Directory.Delete(config.TargetDirectory, recursive: true);
        }
        catch { /* Best effort rollback */ }
    }
}
