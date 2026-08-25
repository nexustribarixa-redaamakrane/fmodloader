namespace fModLoader.CLI;

/// <summary>
/// OpenCode-style terminal UI renderer.
/// Provides box drawing, ANSI colors, spinners, panels, and structured output.
/// Designed for Google Sans Code font with proper weight rendering.
/// </summary>
public static class Tui
{
    // ── ANSI Color Codes ────────────────────────────────────────────────
    public const string Reset     = "\x1b[0m";
    public const string Bold      = "\x1b[1m";
    public const string Dim       = "\x1b[2m";
    public const string Italic    = "\x1b[3m";
    public const string Underline = "\x1b[4m";

    // Foreground
    public const string FgBlack   = "\x1b[30m";
    public const string FgRed     = "\x1b[31m";
    public const string FgGreen   = "\x1b[32m";
    public const string FgYellow  = "\x1b[33m";
    public const string FgBlue    = "\x1b[34m";
    public const string FgMagenta = "\x1b[35m";
    public const string FgCyan    = "\x1b[36m";
    public const string FgWhite   = "\x1b[37m";

    // Bright foreground
    public const string FgBrightRed     = "\x1b[91m";
    public const string FgBrightGreen   = "\x1b[92m";
    public const string FgBrightYellow  = "\x1b[93m";
    public const string FgBrightBlue    = "\x1b[94m";
    public const string FgBrightMagenta = "\x1b[95m";
    public const string FgBrightCyan    = "\x1b[96m";
    public const string FgBrightWhite   = "\x1b[97m";

    // Background
    public const string BgBlack   = "\x1b[40m";
    public const string BgRed     = "\x1b[41m";
    public const string BgWhite   = "\x1b[47m";

    // 256-color (surgical red theme)
    public static string Fg256(int code) => $"\x1b[38;5;{code}m";
    public static string Bg256(int code) => $"\x1b[48;5;{code}m";

    // RGB true-color
    public static string FgRgb(int r, int g, int b) => $"\x1b[38;2;{r};{g};{b}m";
    public static string BgRgb(int r, int g, int b) => $"\x1b[48;2;{r};{g};{b}m";

    // ── Theme Colors (surgical red/white/black) ─────────────────────────
    public static readonly string Accent     = FgRgb(220, 38, 38);   // Red-600
    public static readonly string AccentBold = Bold + FgRgb(239, 68, 68); // Red-500 bold
    public static readonly string Muted      = FgRgb(113, 113, 122); // Zinc-500
    public static readonly string Success    = FgRgb(34, 197, 94);   // Green-500
    public static readonly string Warning    = FgRgb(234, 179, 8);   // Yellow-500
    public static readonly string Error      = Bold + FgRgb(239, 68, 68);
    public static readonly string Info       = FgRgb(96, 165, 250);  // Blue-400
    public static readonly string Label      = FgRgb(161, 161, 170); // Zinc-400
    public static readonly string Value      = FgRgb(244, 244, 245); // Zinc-100
    public static readonly string Separator  = FgRgb(63, 63, 70);    // Zinc-700

    // ── Box Drawing Characters ──────────────────────────────────────────
    public const char BoxTL  = '╭'; // Top-left
    public const char BoxTR  = '╷'; // Top-right (using light)
    public const char BoxBL  = '╰'; // Bottom-left
    public const char BoxBR  = '╯'; // Bottom-right  (unused, we use open ends)
    public const char BoxH   = '─'; // Horizontal
    public const char BoxV   = '│'; // Vertical
    public const char Bullet = '●';
    public const char Arrow  = '→';
    public const char Check  = '✓';
    public const char Cross  = '✗';
    public const char Dot    = '·';

    private static int _width = 80;

    static Tui()
    {
        try { _width = Console.WindowWidth; } catch { _width = 80; }
        EnableAnsi();
    }

    public static int Width => Math.Min(_width, 120);

    // ── Enable ANSI on Windows ──────────────────────────────────────────
    private static void EnableAnsi()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            var handle = Win32.GetStdHandle(-11); // STD_OUTPUT_HANDLE
            Win32.GetConsoleMode(handle, out uint mode);
            Win32.SetConsoleMode(handle, mode | 0x0004); // ENABLE_VIRTUAL_TERMINAL_PROCESSING
        }
        catch { /* Best effort */ }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static class Win32
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern IntPtr GetStdHandle(int nStdHandle);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    }

    // ── High-Level Rendering ────────────────────────────────────────────

    /// <summary>Prints the fModLoader banner.</summary>
    public static void Banner()
    {
        var w = Width;
        Console.WriteLine();
        Console.Write($"  {Accent}");
        Console.Write(new string(BoxH, w - 4));
        Console.WriteLine(Reset);

        string title = "fModLoader CLI";
        string version = "v1.0.65 BETA";
        int pad = w - 4 - title.Length - version.Length - 3;
        Console.Write($"  {AccentBold}{title}{Reset}");
        Console.Write(new string(' ', Math.Max(pad, 1)));
        Console.WriteLine($"{Muted}{version}{Reset}");

        Console.Write($"  {Accent}");
        Console.Write(new string(BoxH, w - 4));
        Console.WriteLine(Reset);
        Console.WriteLine();
    }

    /// <summary>Prints a section header.</summary>
    public static void Section(string title)
    {
        Console.WriteLine();
        Console.Write($"  {Accent}{BoxV}{Reset} {Bold}{FgWhite}{title}{Reset}");
        Console.WriteLine();
        Console.Write($"  {Accent}{BoxV}{Reset}");
        Console.WriteLine();
    }

    /// <summary>Prints a key-value pair in a structured panel style.</summary>
    public static void Field(string label, string value, string? valueColor = null)
    {
        string vc = valueColor ?? Value;
        Console.WriteLine($"  {Accent}{BoxV}{Reset}  {Label}{label,-20}{Reset} {vc}{value}{Reset}");
    }

    /// <summary>Prints a list item with a bullet.</summary>
    public static void ListItem(string text, string? color = null)
    {
        string c = color ?? Value;
        Console.WriteLine($"  {Accent}{BoxV}{Reset}   {Muted}{Bullet}{Reset} {c}{text}{Reset}");
    }

    /// <summary>Closes a section.</summary>
    public static void SectionEnd()
    {
        Console.Write($"  {Accent}{BoxBL}");
        Console.Write(new string(BoxH, Width - 5));
        Console.WriteLine(Reset);
    }

    /// <summary>Prints a status message: [OK], [FAIL], [SKIP], [INFO].</summary>
    public static void Status(string tag, string message, StatusKind kind = StatusKind.Info)
    {
        var (color, icon) = kind switch
        {
            StatusKind.Ok   => (Success, $"{Check}"),
            StatusKind.Fail => (Error,   $"{Cross}"),
            StatusKind.Skip => (Warning, "⊘"),
            StatusKind.Warn => (Warning, "⚠"),
            _               => (Info,    "ℹ"),
        };
        Console.WriteLine($"  {color}{icon}{Reset} {Dim}[{tag}]{Reset} {message}");
    }

    /// <summary>Prints a simple blank line with the gutter.</summary>
    public static void Blank()
    {
        Console.WriteLine($"  {Accent}{BoxV}{Reset}");
    }

    /// <summary>Prints a horizontal rule.</summary>
    public static void Rule()
    {
        Console.Write($"  {Separator}");
        Console.Write(new string(Dot, Width - 4));
        Console.WriteLine(Reset);
    }

    /// <summary>Prints an error block.</summary>
    public static void ErrorBlock(string title, string detail)
    {
        Console.WriteLine();
        Console.WriteLine($"  {Error}{Cross} {title}{Reset}");
        if (!string.IsNullOrEmpty(detail))
        {
            foreach (var line in detail.Split('\n'))
                Console.WriteLine($"    {Muted}{line.TrimEnd()}{Reset}");
        }
        Console.WriteLine();
    }

    /// <summary>Prints a success block.</summary>
    public static void SuccessBlock(string message)
    {
        Console.WriteLine();
        Console.WriteLine($"  {Success}{Check} {Bold}{message}{Reset}");
        Console.WriteLine();
    }

    /// <summary>Shows a simple progress line (overwrites current line).</summary>
    public static void Progress(string message, int current, int total)
    {
        int barWidth = Width - 30;
        double pct = total > 0 ? (double)current / total : 0;
        int filled = (int)(pct * barWidth);
        string bar = new string('█', filled) + new string('░', barWidth - filled);
        string pctStr = $"{pct * 100:F0}%".PadLeft(4);
        Console.Write($"\r  {Accent}{bar}{Reset} {pctStr} {Dim}{message}{Reset}");
        if (current >= total)
            Console.WriteLine();
    }

    /// <summary>Prints the help/usage screen.</summary>
    public static void Help()
    {
        Banner();

        Section("Commands");
        Field("apply",         "<font> <mod> [mod2 ...]   Apply mods to a modcompat font");
        Field("restore",       "<font>                    Restore font from backup");
        Field("info",          "<font>                    Show font metadata");
        Field("mod-info",      "<mod>                     Show mod metadata & glyph map");
        Field("mod-list",      "<mod>                     List files inside a mod archive");
        Field("make-modcompat","<src> [--output <dir>]    Convert font to .modcompat");
        Field("scan-fonts",    "[dir ...]                 Scan for modcompat fonts");
        Field("scan-mods",     "[dir ...]                 Scan for mod files (.ttfm/.otfm)");
        Field("create-demo",   "<output.ttfm>             Create a demo mod for testing");
        Field("help",          "                          Show this help screen");
        SectionEnd();

        Section("Examples");
        ListItem("fModLoader_CLI apply <font>.modcompat.ttf mymod.ttfm");
        ListItem("fModLoader_CLI make-modcompat <font.ttf> --output ./fonts");
        ListItem("fModLoader_CLI info <font>.modcompat.ttf");
        ListItem("fModLoader_CLI scan-fonts <font-directory>");
        SectionEnd();

        Section("File Types");
        Field(".ttfm",           "TrueType mod package (for .modcompat.ttf fonts)");
        Field(".otfm",           "OpenType mod package (for .modcompat.otf fonts)");
        Field(".modcompat.ttf",  "TrueType font prepared for mod injection");
        Field(".modcompat.otf",  "OpenType font prepared for mod injection");
        Field(".modcompat.ttc",  "TrueType Collection (accepts both .ttfm and .otfm)");
        SectionEnd();

        Console.WriteLine($"  {Muted}Recommended font: Google Sans Code{Reset}");
        Console.WriteLine();
    }
}

public enum StatusKind
{
    Ok,
    Fail,
    Skip,
    Warn,
    Info
}
