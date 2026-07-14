using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace fModLoader.Models;

public class PathNode
{
    public double X { get; set; }
    public double Y { get; set; }
    public Tuple<double, double>? CpIn { get; set; }   // Incoming control point
    public Tuple<double, double>? CpOut { get; set; }  // Outgoing control point
    public bool Smooth { get; set; }

    public PathNode() { }

    public PathNode(double x, double y)
    {
        X = x;
        Y = y;
    }

    public PathNode Copy()
    {
        return new PathNode
        {
            X = X,
            Y = Y,
            CpIn = CpIn != null ? Tuple.Create(CpIn.Item1, CpIn.Item2) : null,
            CpOut = CpOut != null ? Tuple.Create(CpOut.Item1, CpOut.Item2) : null,
            Smooth = Smooth
        };
    }
}

public class GlyphContour
{
    public List<PathNode> Nodes { get; set; } = new();
    public bool Closed { get; set; }

    public string ToSvgCommands()
    {
        if (Nodes.Count == 0)
            return "";

        var cmds = new List<string>();
        var first = Nodes[0];
        cmds.Add($"M {first.X:F2} {first.Y:F2}");

        for (int i = 1; i < Nodes.Count; i++)
        {
            var prev = Nodes[i - 1];
            var curr = Nodes[i];
            var pOut = prev.CpOut;
            var cIn = curr.CpIn;

            if (pOut == null && cIn == null)
            {
                cmds.Add($"L {curr.X:F2} {curr.Y:F2}");
            }
            else
            {
                double ox = pOut != null ? pOut.Item1 : prev.X;
                double oy = pOut != null ? pOut.Item2 : prev.Y;
                double ix = cIn != null ? cIn.Item1 : curr.X;
                double iy = cIn != null ? cIn.Item2 : curr.Y;
                cmds.Add($"C {ox:F2} {oy:F2} {ix:F2} {iy:F2} {curr.X:F2} {curr.Y:F2}");
            }
        }

        if (Closed && Nodes.Count > 1)
        {
            var last = Nodes[^1];
            var firstNode = Nodes[0];
            var pOut = last.CpOut;
            var cIn = firstNode.CpIn;

            if (pOut == null && cIn == null)
            {
                cmds.Add("Z");
            }
            else
            {
                double ox = pOut != null ? pOut.Item1 : last.X;
                double oy = pOut != null ? pOut.Item2 : last.Y;
                double ix = cIn != null ? cIn.Item1 : firstNode.X;
                double iy = cIn != null ? cIn.Item2 : firstNode.Y;
                cmds.Add($"C {ox:F2} {oy:F2} {ix:F2} {iy:F2} {firstNode.X:F2} {firstNode.Y:F2} Z");
            }
        }

        return string.Join(" ", cmds);
    }

    public GlyphContour Copy()
    {
        return new GlyphContour
        {
            Nodes = Nodes.Select(n => n.Copy()).ToList(),
            Closed = Closed
        };
    }
}

public class GlyphData
{
    public List<GlyphContour> Contours { get; set; } = new();

    public string ToSvgPath()
    {
        return string.Join(" ", Contours.Where(c => c.Nodes.Count > 0).Select(c => c.ToSvgCommands()));
    }

    public string ToSvgString(int unitsPerEm = 1000, int ascender = 800, int descender = -200)
    {
        int vbH = ascender - descender; // Note: Python version uses: units_per_em for height, but let's check it.
        // Wait, Python project.save uses: viewBox="0 {descender} {units_per_em} {vb_h}" with vb_h = units_per_em or ascender - descender.
        // Let's match the fixed python code: viewBox="0 {descender} {units_per_em} {ascender - descender}"
        int vbHeight = ascender - descender;
        string pathD = ToSvgPath();
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 {descender} {unitsPerEm} {vbHeight}\">\n" +
               $"  <path d=\"{pathD}\"/>\n" +
               $"</svg>";
    }

    public bool IsEmpty()
    {
        return Contours.All(c => c.Nodes.Count == 0);
    }

    public GlyphData Copy()
    {
        return new GlyphData
        {
            Contours = Contours.Select(c => c.Copy()).ToList()
        };
    }
}

public class ModProject
{
    public string Name { get; set; } = "My Mod";
    public string Version { get; set; } = "1.0";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string TargetFamily { get; set; } = "Any";
    public int UnitsPerEm { get; set; } = 1000;
    public int Ascender { get; set; } = 800;
    public int Descender { get; set; } = -200;
    public int XHeight { get; set; } = 500;
    public int CapHeight { get; set; } = 700;

    public Dictionary<int, GlyphData> Glyphs { get; set; } = new();

    public GlyphData AddGlyph(int codepoint)
    {
        if (!Glyphs.TryGetValue(codepoint, out var glyph))
        {
            glyph = new GlyphData();
            Glyphs[codepoint] = glyph;
        }
        return glyph;
    }

    public void RemoveGlyph(int codepoint)
    {
        Glyphs.Remove(codepoint);
    }

    public GlyphData? GetGlyph(int codepoint)
    {
        return Glyphs.TryGetValue(codepoint, out var glyph) ? glyph : null;
    }

    public string BuildMetadataJson()
    {
        var glyphMap = new Dictionary<string, string>();
        foreach (var pair in Glyphs)
        {
            glyphMap[$"0x{pair.Key:X4}"] = $"glyphs/U{pair.Key:X4}.svg";
        }

        var emBox = new Dictionary<string, int>
        {
            { "units_per_em", UnitsPerEm },
            { "ascender", Ascender },
            { "descender", Descender },
            { "x_height", XHeight },
            { "cap_height", CapHeight }
        };

        var metadata = new Dictionary<string, object>
        {
            { "name", Name },
            { "version", Version },
            { "author", Author },
            { "description", Description },
            { "target_family", TargetFamily },
            { "em_box", emBox },
            { "glyph_map", glyphMap }
        };

        return JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
    }

    public Tuple<bool, string> Save(string path)
    {
        try
        {
            string metaJson = BuildMetadataJson();
            using (var fs = new FileStream(path, FileMode.Create))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var metaEntry = zip.CreateEntry("metadata.json");
                using (var writer = new StreamWriter(metaEntry.Open()))
                {
                    writer.Write(metaJson);
                }

                foreach (var pair in Glyphs)
                {
                    string svg = pair.Value.ToSvgString(UnitsPerEm, Ascender, Descender);
                    var glyphEntry = zip.CreateEntry($"glyphs/U{pair.Key:X4}.svg");
                    using (var writer = new StreamWriter(glyphEntry.Open()))
                    {
                        writer.Write(svg);
                    }
                }
            }
            return Tuple.Create(true, $"Saved {Glyphs.Count} glyph(s) to {Path.GetFileName(path)}");
        }
        catch (Exception e)
        {
            return Tuple.Create(false, e.Message);
        }
    }

    public Tuple<bool, string> Load(string path)
    {
        try
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                var metaEntry = zip.GetEntry("metadata.json");
                if (metaEntry == null)
                    return Tuple.Create(false, "No metadata.json in archive.");

                using (var reader = new StreamReader(metaEntry.Open()))
                {
                    string metaJson = reader.ReadToEnd();
                    var doc = JsonDocument.Parse(metaJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("name", out var prop)) Name = prop.GetString() ?? Name;
                    if (root.TryGetProperty("version", out prop)) Version = prop.GetString() ?? Version;
                    if (root.TryGetProperty("author", out prop)) Author = prop.GetString() ?? Author;
                    if (root.TryGetProperty("description", out prop)) Description = prop.GetString() ?? Description;
                    if (root.TryGetProperty("target_family", out prop)) TargetFamily = prop.GetString() ?? TargetFamily;

                    if (root.TryGetProperty("em_box", out var emBox))
                    {
                        if (emBox.TryGetProperty("units_per_em", out prop)) UnitsPerEm = prop.GetInt32();
                        if (emBox.TryGetProperty("ascender", out prop)) Ascender = prop.GetInt32();
                        if (emBox.TryGetProperty("descender", out prop)) Descender = prop.GetInt32();
                        if (emBox.TryGetProperty("x_height", out prop)) XHeight = prop.GetInt32();
                        if (emBox.TryGetProperty("cap_height", out prop)) CapHeight = prop.GetInt32();
                    }

                    Glyphs.Clear();

                    // Parse glyph_map
                    JsonElement glyphMap;
                    if (root.TryGetProperty("glyph_map", out glyphMap) || root.TryGetProperty("glif_map", out glyphMap))
                    {
                        foreach (var item in glyphMap.EnumerateObject())
                        {
                            string cpStr = item.Name;
                            int cp = cpStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase) 
                                ? Convert.ToInt32(cpStr, 16) 
                                : Convert.ToInt32(cpStr);

                            Glyphs[cp] = new GlyphData();
                            // Note: Python does not re-parse the SVG contents, it creates empty slots.
                            // We can also try to parse SVG path data. Let's add simple SVG path parser if needed.
                        }
                    }
                }
            }
            return Tuple.Create(true, $"Loaded {Glyphs.Count} glyph slot(s).");
        }
        catch (Exception e)
        {
            return Tuple.Create(false, e.Message);
        }
    }
}
