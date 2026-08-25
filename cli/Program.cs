using fModLoader.CLI;
using fModLoader.Services;
using fModLoader.Models;


if (args.Length == 0 || args[0] is "help" or "--help" or "-h" or "/?")
{
    Tui.Help();
    return 0;
}

string command = args[0].ToLowerInvariant();
string[] cmdArgs = args.Skip(1).ToArray();

try
{
    return command switch
    {
        "apply"          => RunApply(cmdArgs),
        "restore"        => RunRestore(cmdArgs),
        "info"           => RunInfo(cmdArgs),
        "mod-info"       => RunModInfo(cmdArgs),
        "mod-list"       => RunModList(cmdArgs),
        "make-modcompat" => RunMakeModcompat(cmdArgs),
        "scan-fonts"     => RunScanFonts(cmdArgs),
        "scan-mods"      => RunScanMods(cmdArgs),
        "create-demo"    => RunCreateDemo(cmdArgs),
        _                => UnknownCommand(command),
    };
}
catch (Exception ex)
{
    Tui.ErrorBlock("Unhandled error", ex.Message);
    return 1;
}

// ═════════════════════════════════════════════════════════════════════════════
// COMMANDS
// ═════════════════════════════════════════════════════════════════════════════

// ── apply <font> <mod> [mod2 ...] ───────────────────────────────────────────
static int RunApply(string[] args)
{
    if (args.Length < 2)
    {
        Tui.ErrorBlock("Usage", "fModLoader_CLI apply <font> <mod> [mod2 ...]");
        return 1;
    }

    string fontPath = Path.GetFullPath(args[0]);
    var modPaths = args.Skip(1).Select(Path.GetFullPath).ToArray();

    Tui.Banner();

    var fontHandler = new FontHandlerService();
    var modHandler = new ModHandlerService();
    var backupService = new BackupService();

    // Validate font
    if (!File.Exists(fontPath))
    {
        Tui.ErrorBlock("Font not found", fontPath);
        return 1;
    }

    if (!fontHandler.IsModcompatFont(fontPath))
    {
        Tui.ErrorBlock("Not a modcompat font",
            $"{Path.GetFileName(fontPath)} is not a .modcompat font.\n" +
            "Use 'make-modcompat' to convert it first.");
        return 1;
    }

    // Show font info
    Tui.Section("Target Font");
    var fontInfo = fontHandler.GetFontInfo(fontPath);
    Tui.Field("File", Path.GetFileName(fontPath));
    Tui.Field("Family", fontInfo.Family);
    Tui.Field("Style", fontInfo.Style);
    Tui.Field("Vendor", fontInfo.VendorId);
    Tui.SectionEnd();

    // Backup
    Tui.Section("Backup");
    if (backupService.HasBackup(fontPath))
    {
        Tui.Status("BACKUP", "Existing backup found — skipping", StatusKind.Skip);
    }
    else
    {
        var backupResult = backupService.BackupFont(fontPath);
        if (backupResult != null)
            Tui.Status("BACKUP", $"Created {Tui.Arrow} {Path.GetFileName(backupResult)}", StatusKind.Ok);
        else
        {
            Tui.Status("BACKUP", "Failed to create backup!", StatusKind.Fail);
            return 1;
        }
    }
    Tui.SectionEnd();

    // Apply each mod
    int successCount = 0;
    int failCount = 0;

    foreach (var modPath in modPaths)
    {
        Tui.Section($"Applying: {Path.GetFileName(modPath)}");

        if (!File.Exists(modPath))
        {
            Tui.Status("MOD", "File not found", StatusKind.Fail);
            Tui.SectionEnd();
            failCount++;
            continue;
        }

        if (!modHandler.IsValidModFile(modPath))
        {
            Tui.Status("MOD", "Not a valid mod file (.ttfm/.otfm)", StatusKind.Fail);
            Tui.SectionEnd();
            failCount++;
            continue;
        }

        // Load metadata
        var (meta, loadErr) = modHandler.LoadMod(modPath);
        if (meta == null)
        {
            Tui.Status("MOD", $"Load failed: {loadErr}", StatusKind.Fail);
            Tui.SectionEnd();
            failCount++;
            continue;
        }

        Tui.Field("Mod Name", meta.Name);
        Tui.Field("Version", meta.Version);
        Tui.Field("Author", meta.Author);
        Tui.Field("Glyphs", $"{meta.GlifMap.Count} glyph(s) to inject");
        Tui.Blank();

        // Extract glifs
        var glifData = modHandler.ExtractGlifs(modPath, meta.GlifMap);
        if (glifData.Count == 0)
        {
            Tui.Status("EXTRACT", "No glyph data extracted", StatusKind.Warn);
            Tui.SectionEnd();
            failCount++;
            continue;
        }

        Tui.Status("EXTRACT", $"Extracted {glifData.Count} glyph(s)", StatusKind.Ok);

        // Inject
        var (ok, injectMsg) = fontHandler.ApplyModGlyphs(fontPath, glifData);
        if (ok)
        {
            Tui.Status("INJECT", injectMsg, StatusKind.Ok);
            successCount++;
        }
        else
        {
            Tui.Status("INJECT", injectMsg, StatusKind.Fail);
            failCount++;
        }

        Tui.SectionEnd();
    }

    // Summary
    Console.WriteLine();
    if (failCount == 0)
        Tui.SuccessBlock($"All {successCount} mod(s) applied successfully");
    else
    {
        Tui.Status("DONE", $"{successCount} succeeded, {failCount} failed", StatusKind.Warn);
        Console.WriteLine();
    }

    return failCount > 0 ? 1 : 0;
}

// ── restore <font> ──────────────────────────────────────────────────────────
static int RunRestore(string[] args)
{
    if (args.Length < 1)
    {
        Tui.ErrorBlock("Usage", "fModLoader_CLI restore <font>");
        return 1;
    }

    Tui.Banner();
    string fontPath = Path.GetFullPath(args[0]);
    var backupService = new BackupService();

    Tui.Section("Restore");
    Tui.Field("File", Path.GetFileName(fontPath));

    if (!backupService.HasBackup(fontPath))
    {
        Tui.Status("RESTORE", "No backup found for this font", StatusKind.Fail);
        Tui.SectionEnd();
        return 1;
    }

    if (backupService.RestoreFont(fontPath))
    {
        Tui.Status("RESTORE", "Font restored from backup", StatusKind.Ok);
        Tui.SectionEnd();
        Tui.SuccessBlock("Font restored successfully");
        return 0;
    }
    else
    {
        Tui.Status("RESTORE", "Failed to restore font", StatusKind.Fail);
        Tui.SectionEnd();
        return 1;
    }
}

// ── info <font> ─────────────────────────────────────────────────────────────
static int RunInfo(string[] args)
{
    if (args.Length < 1)
    {
        Tui.ErrorBlock("Usage", "fModLoader_CLI info <font>");
        return 1;
    }

    Tui.Banner();
    string fontPath = Path.GetFullPath(args[0]);

    if (!File.Exists(fontPath))
    {
        Tui.ErrorBlock("File not found", fontPath);
        return 1;
    }

    var fontHandler = new FontHandlerService();
    var backupService = new BackupService();

    Tui.Section("Font Information");
    Tui.Field("File", Path.GetFileName(fontPath));
    Tui.Field("Path", fontPath);
    Tui.Field("Size", FormatFileSize(new FileInfo(fontPath).Length));
    Tui.Blank();

    var info = fontHandler.GetFontInfo(fontPath);
    Tui.Field("Family", info.Family);
    Tui.Field("Style", info.Style);
    Tui.Field("Vendor ID", info.VendorId);
    Tui.Field("Units/EM", info.UnitsPerEm.ToString());
    Tui.Blank();

    bool isModcompat = fontHandler.IsModcompatFont(fontPath);
    Tui.Field("ModCompat", isModcompat ? "Yes" : "No",
        isModcompat ? Tui.Success : Tui.Warning);
    Tui.Field("Backup", backupService.HasBackup(fontPath) ? "Exists" : "None",
        backupService.HasBackup(fontPath) ? Tui.Success : Tui.Muted);

    Tui.SectionEnd();
    return 0;
}

// ── mod-info <mod> ──────────────────────────────────────────────────────────
static int RunModInfo(string[] args)
{
    if (args.Length < 1)
    {
        Tui.ErrorBlock("Usage", "fModLoader_CLI mod-info <mod>");
        return 1;
    }

    Tui.Banner();
    string modPath = Path.GetFullPath(args[0]);

    if (!File.Exists(modPath))
    {
        Tui.ErrorBlock("File not found", modPath);
        return 1;
    }

    var modHandler = new ModHandlerService();

    if (!modHandler.IsValidModFile(modPath))
    {
        Tui.ErrorBlock("Invalid mod file", "Expected .ttfm or .otfm file.");
        return 1;
    }

    var (meta, err) = modHandler.LoadMod(modPath);
    if (meta == null)
    {
        Tui.ErrorBlock("Failed to load mod", err);
        return 1;
    }

    Tui.Section("Mod Information");
    Tui.Field("File", Path.GetFileName(modPath));
    Tui.Field("Name", meta.Name);
    Tui.Field("Version", meta.Version);
    Tui.Field("Author", meta.Author);
    Tui.Field("Description", meta.Description);
    Tui.Field("Target Family", meta.TargetFamily);
    Tui.SectionEnd();

    if (meta.EmBox.Count > 0)
    {
        Tui.Section("Em Box");
        foreach (var kv in meta.EmBox)
            Tui.Field(kv.Key, kv.Value);
        Tui.SectionEnd();
    }

    if (meta.GlifMap.Count > 0)
    {
        Tui.Section($"Glyph Map ({meta.GlifMap.Count} glyphs)");
        foreach (var kv in meta.GlifMap)
        {
            string cpDisplay = kv.Key;
            if (kv.Key.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                int cp = Convert.ToInt32(kv.Key, 16);
                cpDisplay = $"U+{cp:X4} ({(char)cp})";
            }
            Tui.Field(cpDisplay, kv.Value);
        }
        Tui.SectionEnd();
    }

    return 0;
}

// ── mod-list <mod> ──────────────────────────────────────────────────────────
static int RunModList(string[] args)
{
    if (args.Length < 1)
    {
        Tui.ErrorBlock("Usage", "fModLoader_CLI mod-list <mod>");
        return 1;
    }

    Tui.Banner();
    string modPath = Path.GetFullPath(args[0]);
    var modHandler = new ModHandlerService();

    if (!File.Exists(modPath))
    {
        Tui.ErrorBlock("File not found", modPath);
        return 1;
    }

    var contents = modHandler.ListModContents(modPath);

    Tui.Section($"Contents of {Path.GetFileName(modPath)}");
    if (contents.Count == 0)
    {
        Tui.ListItem("(empty archive)", Tui.Muted);
    }
    else
    {
        foreach (var entry in contents)
            Tui.ListItem(entry);
    }
    Tui.Field("Total", $"{contents.Count} file(s)");
    Tui.SectionEnd();

    return 0;
}

// ── make-modcompat <src> [--output <dir>] ───────────────────────────────────
static int RunMakeModcompat(string[] args)
{
    if (args.Length < 1)
    {
        Tui.ErrorBlock("Usage", "fModLoader_CLI make-modcompat <font> [--output <dir>]");
        return 1;
    }

    Tui.Banner();

    // Parse --output
    string? outputDir = null;
    var sources = new List<string>();
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] is "--output" or "-o" && i + 1 < args.Length)
        {
            outputDir = args[++i];
        }
        else if (!args[i].StartsWith("-"))
        {
            sources.Add(args[i]);
        }
    }

    if (sources.Count == 0)
    {
        Tui.ErrorBlock("No source fonts specified", "Provide at least one font file path.");
        return 1;
    }

    outputDir ??= Directory.GetCurrentDirectory();
    Directory.CreateDirectory(outputDir);

    var fontHandler = new FontHandlerService();
    int okCount = 0;
    int failCount = 0;

    Tui.Section("Convert to ModCompat");
    Tui.Field("Output", outputDir);
    Tui.Blank();

    foreach (var src in sources)
    {
        string sourcePath = Path.GetFullPath(src);

        if (!File.Exists(sourcePath))
        {
            Tui.Status("SKIP", $"Not found: {Path.GetFileName(sourcePath)}", StatusKind.Skip);
            failCount++;
            continue;
        }

        string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        string suffix = ext == ".otf" ? ".modcompat.otf" : ".modcompat.ttf";
        string outPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(sourcePath) + suffix);

        if (File.Exists(outPath))
        {
            Tui.Status("EXISTS", Path.GetFileName(outPath), StatusKind.Skip);
            okCount++;
            continue;
        }

        Tui.Progress($"Converting {Path.GetFileName(sourcePath)}...", okCount + failCount, sources.Count);

        bool ok = fontHandler.CreateModcompatFont(sourcePath, outPath);
        if (ok)
        {
            Tui.Status("OK", $"{Path.GetFileName(sourcePath)} {Tui.Arrow} {Path.GetFileName(outPath)}", StatusKind.Ok);
            okCount++;
        }
        else
        {
            Tui.Status("FAIL", Path.GetFileName(sourcePath), StatusKind.Fail);
            failCount++;
        }
    }

    Tui.Blank();
    Tui.Field("Result", $"{okCount}/{sources.Count} converted");
    Tui.SectionEnd();

    if (failCount == 0)
        Tui.SuccessBlock($"All {okCount} font(s) converted");

    return failCount > 0 ? 1 : 0;
}

// ── Default font directories per OS ─────────────────────────────────────────
static List<string> DefaultFontDirs()
{
    var dirs = new List<string>();

    if (OperatingSystem.IsWindows())
    {
        string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (windir.Length > 0) dirs.Add(Path.Combine(windir, "Fonts"));

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (localAppData.Length > 0)
            dirs.Add(Path.Combine(localAppData, "Microsoft", "Windows", "Fonts"));
    }
    else if (OperatingSystem.IsMacOS())
    {
        dirs.Add("/System/Library/Fonts");
        dirs.Add("/Library/Fonts");
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        dirs.Add(Path.Combine(home, "Library", "Fonts"));
    }
    else
    {
        // Linux, FreeBSD and other Unix-like systems
        string xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME") ?? "";
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (!string.IsNullOrEmpty(home))
            dirs.Add(Path.Combine(home, ".fonts"));

        if (!string.IsNullOrEmpty(xdgDataHome))
            dirs.Add(Path.Combine(xdgDataHome, "fonts"));
        else if (!string.IsNullOrEmpty(home))
            dirs.Add(Path.Combine(home, ".local", "share", "fonts"));

        dirs.Add("/usr/local/share/fonts");
        dirs.Add("/usr/share/fonts");
    }

    dirs.Add(Directory.GetCurrentDirectory());
    return dirs;
}

// ── scan-fonts [dir ...] ────────────────────────────────────────────────────
static int RunScanFonts(string[] args)
{
    Tui.Banner();

    var dirs = args.Length > 0
        ? args.Select(Path.GetFullPath).ToList()
        : DefaultFontDirs();

    var scanner = new FontDiscoveryService();
    var found = scanner.ScanForModcompatFonts(dirs);

    Tui.Section($"ModCompat Font Scan ({found.Count} found)");
    foreach (var dir in dirs)
        Tui.Field("Scanned", dir);
    Tui.Blank();

    if (found.Count == 0)
    {
        Tui.ListItem("No modcompat fonts found", Tui.Muted);
    }
    else
    {
        foreach (var f in found)
            Tui.ListItem(f);
    }
    Tui.SectionEnd();

    return 0;
}

// ── scan-mods [dir ...] ─────────────────────────────────────────────────────
static int RunScanMods(string[] args)
{
    Tui.Banner();

    var dirs = args.Length > 0
        ? args.Select(Path.GetFullPath).ToList()
        : new List<string> { Directory.GetCurrentDirectory() };

    var scanner = new ModHandlerService();
    var found = scanner.ScanForMods(dirs);

    Tui.Section($"Mod Scan ({found.Count} found)");
    foreach (var dir in dirs)
        Tui.Field("Scanned", dir);
    Tui.Blank();

    if (found.Count == 0)
    {
        Tui.ListItem("No mod files found", Tui.Muted);
    }
    else
    {
        foreach (var f in found)
        {
            string ext = Path.GetExtension(f).ToUpperInvariant();
            Tui.ListItem($"{ext}  {f}");
        }
    }
    Tui.SectionEnd();

    return 0;
}

// ── create-demo <output.ttfm> ───────────────────────────────────────────────
static int RunCreateDemo(string[] args)
{
    if (args.Length < 1)
    {
        Tui.ErrorBlock("Usage", "fModLoader_CLI create-demo <output.ttfm>");
        return 1;
    }

    Tui.Banner();
    string outPath = Path.GetFullPath(args[0]);
    var modHandler = new ModHandlerService();

    Tui.Section("Create Demo Mod");
    Tui.Field("Output", outPath);

    if (modHandler.CreateDemoMod(outPath))
    {
        Tui.Status("CREATE", "Demo mod created", StatusKind.Ok);
        Tui.SectionEnd();
        Tui.SuccessBlock("Demo mod ready — use 'apply' to test it");
        return 0;
    }
    else
    {
        Tui.Status("CREATE", "Failed to create demo mod", StatusKind.Fail);
        Tui.SectionEnd();
        return 1;
    }
}

// ── Unknown command ─────────────────────────────────────────────────────────
static int UnknownCommand(string cmd)
{
    Tui.Banner();
    Tui.ErrorBlock($"Unknown command: '{cmd}'", "Run 'fModLoader_CLI help' to see available commands.");
    return 1;
}

// ── Helpers ─────────────────────────────────────────────────────────────────
static string FormatFileSize(long bytes)
{
    if (bytes < 1024) return $"{bytes} B";
    if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
    return $"{bytes / (1024.0 * 1024.0):F2} MB";
}
