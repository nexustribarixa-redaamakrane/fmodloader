using System;
using System.IO;
using System.Reflection;
using FModLoaderInstaller.Services;

namespace installer_smoke;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== FModLoader Installer Smoke Test ===");

        var service = new InstallerService();
        var method = typeof(InstallerService).GetMethod("GetFilesToInstall", BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            Console.WriteLine("FAIL: Could not find GetFilesToInstall method via reflection.");
            return;
        }

        // Test Case 1: Source-build mode (walking up directory)
        // We simulate running from a nested bin directory inside installer.
        var tempRunDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_nested_run");
        Directory.CreateDirectory(tempRunDir);

        Console.WriteLine($"\nSimulating source-build mode (running from: {tempRunDir})");
        var parameters = new object?[] { tempRunDir, null };
        var files = (dynamic?)method.Invoke(service, parameters);
        string? errorMessage = (string?)parameters[1];

        if (files != null && files.Count > 0)
        {
            Console.WriteLine($"SUCCESS: Found {files.Count} files via dev fallback!");
            bool hasGui = false;
            bool hasCli = false;
            bool hasFonts = false;
            bool hasMods = false;

            foreach (var file in files)
            {
                string rel = file.Item2; // (SourcePath, RelativePath) tuple
                if (rel == "fModLoader.exe") hasGui = true;
                if (rel == "fModLoader_CLI.exe") hasCli = true;
                if (rel.StartsWith("fonts")) hasFonts = true;
                if (rel.StartsWith("mods")) hasMods = true;
            }

            Console.WriteLine($"- Has fModLoader.exe: {hasGui}");
            Console.WriteLine($"- Has fModLoader_CLI.exe: {hasCli}");
            Console.WriteLine($"- Has fonts: {hasFonts}");
            Console.WriteLine($"- Has mods: {hasMods}");
            
            if (hasGui && hasCli && hasFonts && hasMods)
            {
                Console.WriteLine("ALL REQUIRED FILES PRESENT IN SOURCE-BUILD FALLBACK");
            }
            else
            {
                Console.WriteLine("FAIL: Some required files are missing from source-build fallback");
            }
        }
        else
        {
            Console.WriteLine($"FAIL: No files found. Error: {errorMessage}");
        }

        // Clean up temp dir
        try { Directory.Delete(tempRunDir, true); } catch {}
    }
}
