using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fModLoader.Models;
using fModLoader.Services;

namespace fModLoader.ViewModels;

public class GlyphGridCell
{
    public int Codepoint { get; }
    public string HexStr => $"U+{Codepoint:X4}";
    public string CharStr => char.ConvertFromUtf32(Codepoint);

    public GlyphGridCell(int codepoint)
    {
        Codepoint = codepoint;
    }
}

public partial class FontEditorViewModel : ObservableObject
{
    private readonly FontHandlerService _fontHandlerService = new();

    [ObservableProperty]
    private ModProject _project = new();

    [ObservableProperty]
    private ObservableCollection<GlyphGridCell> _gridCells = new();

    [ObservableProperty]
    private GlyphGridCell? _selectedCell;

    [ObservableProperty]
    private string _svgPathText = "";

    [ObservableProperty]
    private string _editorStatus = "Create or Load a Project to edit glyphs.";

    public FontEditorViewModel()
    {
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        GridCells.Clear();
        // Load basic alphanumeric and punctuation range (U+0020 to U+007E)
        for (int cp = 0x0020; cp <= 0x007E; cp++)
        {
            GridCells.Add(new GlyphGridCell(cp));
        }
        SelectedCell = GridCells.FirstOrDefault(c => c.Codepoint == 0x0041); // Default to 'A'
    }

    partial void OnSelectedCellChanged(GlyphGridCell? value)
    {
        if (value == null) return;
        LoadGlyph(value.Codepoint);
    }

    private void LoadGlyph(int cp)
    {
        var glyph = Project.GetGlyph(cp);
        if (glyph != null)
        {
            SvgPathText = glyph.ToSvgPath();
            EditorStatus = $"Editing glyph {SelectedCell?.HexStr} ('{SelectedCell?.CharStr}').";
        }
        else
        {
            SvgPathText = "";
            EditorStatus = $"Glyph {SelectedCell?.HexStr} ('{SelectedCell?.CharStr}') is currently empty.";
        }
    }

    [RelayCommand]
    public void SaveGlyph()
    {
        if (SelectedCell == null) return;

        int cp = SelectedCell.Codepoint;
        var glyph = Project.AddGlyph(cp);
        glyph.Contours = SvgPathParser.Parse(SvgPathText);

        EditorStatus = $"Saved glyph {SelectedCell.HexStr} ('{SelectedCell.CharStr}') vector data.";
    }

    [RelayCommand]
    public void ClearGlyph()
    {
        if (SelectedCell == null) return;
        SvgPathText = "";
        Project.RemoveGlyph(SelectedCell.Codepoint);
        EditorStatus = $"Cleared glyph {SelectedCell.HexStr} ('{SelectedCell.CharStr}').";
    }

    [RelayCommand]
    public async Task LoadProject(Window parentWindow)
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Open Font Mod Project",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Font Mods") { Patterns = new[] { "*.ttfm", "*.otfm" } }
            }
        };

        var result = await parentWindow.StorageProvider.OpenFilePickerAsync(options);
        if (result.Count > 0)
        {
            string path = result[0].Path.LocalPath;
            var newProj = new ModProject();
            var res = newProj.Load(path);
            if (res.Item1)
            {
                Project = newProj;
                EditorStatus = $"Loaded project: {Project.Name}. {res.Item2}";
                if (SelectedCell != null)
                {
                    LoadGlyph(SelectedCell.Codepoint);
                }
            }
            else
            {
                EditorStatus = $"Error loading project: {res.Item2}";
            }
        }
    }

    [RelayCommand]
    public async Task SaveProject(Window parentWindow)
    {
        // Auto-save current glyph first
        SaveGlyph();

        var options = new FilePickerSaveOptions
        {
            Title = "Save Font Mod Project",
            DefaultExtension = "ttfm",
            ShowOverwritePrompt = true,
            SuggestedFileName = string.IsNullOrEmpty(Project.Name) ? "my_font_mod" : Project.Name.ToLower().Replace(" ", "_")
        };

        var result = await parentWindow.StorageProvider.SaveFilePickerAsync(options);
        if (result != null)
        {
            string path = result.Path.LocalPath;
            var res = Project.Save(path);
            if (res.Item1)
            {
                EditorStatus = $"Successfully exported mod project: {Path.GetFileName(path)}";
            }
            else
            {
                EditorStatus = $"Error saving project: {res.Item2}";
            }
        }
    }
}
