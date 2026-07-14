namespace fModLoader.Models;

public class FontTarget
{
    public string FilePath { get; set; } = "";
    public string Family { get; set; } = "";
    public string Style { get; set; } = "";
    public string FullName { get; set; } = "";
    public string VendorId { get; set; } = "";
    public int GlyphCount { get; set; }
    public int UnitsPerEm { get; set; } = 1000;
    public int Ascender { get; set; } = 800;
    public int Descender { get; set; } = -200;
    public bool HasBackup { get; set; }

    public string DisplayName => string.IsNullOrEmpty(FullName) ? System.IO.Path.GetFileName(FilePath) : FullName;
}
