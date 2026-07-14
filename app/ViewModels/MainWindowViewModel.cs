using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fModLoader.Models;
using fModLoader.Services;

namespace fModLoader.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly FontHandlerService _fontHandlerService = new();
    private readonly ModHandlerService _modHandlerService = new();
    private readonly BackupService _backupService = new();
    private readonly FontDiscoveryService _fontDiscoveryService = new();

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private ObservableCollection<FontTarget> _fonts = new();

    [ObservableProperty]
    private FontTarget? _selectedFont;

    [ObservableProperty]
    private ObservableCollection<ModMetadata> _mods = new();

    [ObservableProperty]
    private ModMetadata? _selectedMod;

    [ObservableProperty]
    private string _backupStatus = "No Backup";

    [ObservableProperty]
    private bool _canRevert;

    public MainWindowViewModel()
    {
        ReloadFontsAndMods();
    }

    partial void OnSelectedFontChanged(FontTarget? value)
    {
        if (value != null)
        {
            CanRevert = _backupService.HasBackup(value.FilePath);
            BackupStatus = CanRevert ? "Backup Active" : "No Backup";
        }
        else
        {
            CanRevert = false;
            BackupStatus = "No Font Selected";
        }
    }

    [RelayCommand]
    public void ReloadFontsAndMods()
    {
        StatusText = "Scanning directories...";
        
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string modsDir = Path.Combine(appDir, "mods");
        string fontsDir = Path.Combine(appDir, "fonts");

        Directory.CreateDirectory(modsDir);
        Directory.CreateDirectory(fontsDir);

        var searchDirs = new List<string> { modsDir, fontsDir, appDir };

        // Scan fonts
        var fontPaths = _fontDiscoveryService.ScanForModcompatFonts(searchDirs);
        Fonts.Clear();
        foreach (var p in fontPaths)
        {
            Fonts.Add(_fontHandlerService.GetFontInfo(p));
        }

        if (Fonts.Count > 0)
        {
            SelectedFont = Fonts[0];
        }

        // Scan mods
        var modPaths = _modHandlerService.ScanForMods(searchDirs);
        Mods.Clear();
        foreach (var p in modPaths)
        {
            var res = _modHandlerService.LoadMod(p);
            if (res.Item1 != null)
            {
                Mods.Add(res.Item1);
            }
        }

        if (Mods.Count > 0)
        {
            SelectedMod = Mods[0];
        }

        StatusText = $"Found {Fonts.Count} fonts and {Mods.Count} mods.";
    }

    [RelayCommand]
    public async Task BrowseFont(Window parentWindow)
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Select Font File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Modcompat Fonts") { Patterns = new[] { "*.modcompat.ttf", "*.modcompat.otf", "*.modcompat.ttc" } },
                new FilePickerFileType("All Fonts") { Patterns = new[] { "*.ttf", "*.otf", "*.ttc" } }
            }
        };

        var result = await parentWindow.StorageProvider.OpenFilePickerAsync(options);
        if (result.Count > 0)
        {
            string path = result[0].Path.LocalPath;
            if (Path.GetFileName(path).Contains(".modcompat"))
            {
                var info = _fontHandlerService.GetFontInfo(path);
                Fonts.Add(info);
                SelectedFont = info;
                StatusText = $"Loaded custom font: {Path.GetFileName(path)}";
            }
            else
            {
                StatusText = "Error: Please select a font with the '.modcompat' extension or convert it first.";
            }
        }
    }

    [RelayCommand]
    public async Task BrowseMod(Window parentWindow)
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Select Mod Package",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Mod Packages") { Patterns = new[] { "*.ttfm", "*.otfm" } }
            }
        };

        var result = await parentWindow.StorageProvider.OpenFilePickerAsync(options);
        if (result.Count > 0)
        {
            string path = result[0].Path.LocalPath;
            var res = _modHandlerService.LoadMod(path);
            if (res.Item1 != null)
            {
                Mods.Add(res.Item1);
                SelectedMod = res.Item1;
                StatusText = $"Loaded mod: {res.Item1.Name}";
            }
            else
            {
                StatusText = $"Error loading mod: {res.Item2}";
            }
        }
    }

    [RelayCommand]
    public void ApplyMod()
    {
        if (SelectedFont == null)
        {
            StatusText = "Error: No target font selected.";
            return;
        }

        if (SelectedMod == null)
        {
            StatusText = "Error: No mod package selected.";
            return;
        }

        StatusText = "Extracting glyphs...";
        var glifDataMap = _modHandlerService.ExtractGlifs(SelectedMod.FilePath, SelectedMod.GlifMap);
        if (glifDataMap.Count == 0)
        {
            StatusText = "Error: No glyphs found in mod package.";
            return;
        }

        StatusText = "Injecting outlines into font tables...";
        var res = _fontHandlerService.ApplyModGlyphs(SelectedFont.FilePath, glifDataMap);

        if (res.Item1)
        {
            StatusText = $"Success: {res.Item2}";
            // Update backup status
            CanRevert = _backupService.HasBackup(SelectedFont.FilePath);
            BackupStatus = "Backup Active";
        }
        else
        {
            StatusText = $"Error: {res.Item2}";
        }
    }

    [RelayCommand]
    public void RevertFont()
    {
        if (SelectedFont == null) return;

        StatusText = "Restoring backup...";
        bool ok = _backupService.RestoreFont(SelectedFont.FilePath);
        if (ok)
        {
            StatusText = "Successfully restored font from backup.";
            CanRevert = _backupService.HasBackup(SelectedFont.FilePath);
            BackupStatus = CanRevert ? "Backup Active" : "No Backup";
        }
        else
        {
            StatusText = "Failed to restore backup.";
        }
    }

    [RelayCommand]
    public async Task ConvertFontToModcompat(Window parentWindow)
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Select Source Font to Convert",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Fonts") { Patterns = new[] { "*.ttf", "*.otf", "*.ttc" } }
            }
        };

        var result = await parentWindow.StorageProvider.OpenFilePickerAsync(options);
        if (result.Count > 0)
        {
            string srcPath = result[0].Path.LocalPath;
            string dir = Path.GetDirectoryName(srcPath) ?? "";
            string name = Path.GetFileNameWithoutExtension(srcPath);
            string ext = Path.GetExtension(srcPath);
            string destPath = Path.Combine(dir, $"{name}.modcompat{ext}");

            bool success = _fontHandlerService.CreateModcompatFont(srcPath, destPath);
            if (success)
            {
                StatusText = $"Created modcompat font: {Path.GetFileName(destPath)}";
                ReloadFontsAndMods();
            }
            else
            {
                StatusText = "Error converting font.";
            }
        }
    }

    [RelayCommand]
    public void CreateDemoModFile()
    {
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string modsDir = Path.Combine(appDir, "mods");
        Directory.CreateDirectory(modsDir);
        string dest = Path.Combine(modsDir, "demo_mod.ttfm");

        bool success = _modHandlerService.CreateDemoMod(dest);
        if (success)
        {
            StatusText = "Created demo_mod.ttfm in mods folder.";
            ReloadFontsAndMods();
        }
        else
        {
            StatusText = "Error creating demo mod.";
        }
    }
}
