using System;
using System.IO;

namespace fModLoader.Services;

public class BackupService
{
    public const string BackupSuffix = ".fml_backup";

    public bool HasBackup(string fontPath)
    {
        return File.Exists(fontPath + BackupSuffix);
    }

    public string? BackupFont(string fontPath)
    {
        string backupPath = fontPath + BackupSuffix;
        if (File.Exists(backupPath))
            return backupPath;

        try
        {
            File.Copy(fontPath, backupPath, overwrite: true);
            return backupPath;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[BackupService] Backup failed: {e.Message}");
            return null;
        }
    }

    public bool RestoreFont(string fontPath)
    {
        string backupPath = fontPath + BackupSuffix;
        if (!File.Exists(backupPath))
            return false;

        try
        {
            File.Copy(backupPath, fontPath, overwrite: true);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[BackupService] Restore failed: {e.Message}");
            return false;
        }
    }
}
