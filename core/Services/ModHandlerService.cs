using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using fModLoader.Models;

namespace fModLoader.Services;

public class ModHandlerService
{
    private static readonly string[] SupportedExtensions = { ".ttfm", ".otfm" };
    private static readonly string[] MetadataFilenames = { "metadata.json", "mod.json", "info.json", "metadata.xml" };

    public bool IsValidModFile(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        if (Array.IndexOf(SupportedExtensions, ext) == -1)
            return false;

        try
        {
            using (var zip = ZipFile.OpenRead(path))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public Tuple<ModMetadata?, string> LoadMod(string path)
    {
        if (!IsValidModFile(path))
            return Tuple.Create<ModMetadata?, string>(null, $"'{Path.GetFileName(path)}' is not a valid mod file.");

        try
        {
            using (var zip = ZipFile.OpenRead(path))
            {
                ZipArchiveEntry? metaEntry = null;
                foreach (var candidate in MetadataFilenames)
                {
                    metaEntry = zip.GetEntry(candidate);
                    if (metaEntry != null)
                        break;
                }

                if (metaEntry == null)
                    return Tuple.Create<ModMetadata?, string>(null, "No metadata file found inside mod archive.");

                var meta = new ModMetadata { FilePath = path };

                if (metaEntry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    using (var reader = new StreamReader(metaEntry.Open()))
                    {
                        string jsonStr = reader.ReadToEnd();
                        var doc = JsonDocument.Parse(jsonStr);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("name", out var prop)) meta.Name = prop.GetString() ?? meta.Name;
                        if (root.TryGetProperty("version", out prop)) meta.Version = prop.GetString() ?? meta.Version;
                        if (root.TryGetProperty("author", out prop)) meta.Author = prop.GetString() ?? meta.Author;
                        if (root.TryGetProperty("description", out prop)) meta.Description = prop.GetString() ?? meta.Description;
                        if (root.TryGetProperty("target_family", out prop)) meta.TargetFamily = prop.GetString() ?? meta.TargetFamily;

                        if (root.TryGetProperty("em_box", out var emBox))
                        {
                            foreach (var item in emBox.EnumerateObject())
                            {
                                meta.EmBox[item.Name] = item.Value.ToString();
                            }
                        }

                        JsonElement glyphMap;
                        if (root.TryGetProperty("glyph_map", out glyphMap) || root.TryGetProperty("glif_map", out glyphMap))
                        {
                            foreach (var item in glyphMap.EnumerateObject())
                            {
                                meta.GlifMap[item.Name] = item.Value.GetString() ?? "";
                            }
                        }
                    }
                }
                else if (metaEntry.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = metaEntry.Open())
                    {
                        var doc = XDocument.Load(stream);
                        var root = doc.Root;
                        if (root != null)
                        {
                            meta.Name = root.Element("name")?.Value ?? meta.Name;
                            meta.Version = root.Element("version")?.Value ?? meta.Version;
                            meta.Author = root.Element("author")?.Value ?? meta.Author;
                            meta.Description = root.Element("description")?.Value ?? meta.Description;
                            meta.TargetFamily = root.Element("target_family")?.Value ?? meta.TargetFamily;

                            var emEl = root.Element("em_box");
                            if (emEl != null)
                            {
                                foreach (var child in emEl.Elements())
                                {
                                    meta.EmBox[child.Name.LocalName] = child.Value;
                                }
                            }

                            var glyphsEl = root.Element("glyph_map");
                            if (glyphsEl != null)
                            {
                                foreach (var glyph in glyphsEl.Elements("glyph"))
                                {
                                    string cp = glyph.Attribute("codepoint")?.Value ?? "";
                                    string glifFile = glyph.Attribute("glif")?.Value ?? glyph.Attribute("svg")?.Value ?? "";
                                    if (!string.IsNullOrEmpty(cp) && !string.IsNullOrEmpty(glifFile))
                                    {
                                        meta.GlifMap[cp] = glifFile;
                                    }
                                }
                            }
                        }
                    }
                }

                return Tuple.Create<ModMetadata?, string>(meta, "");
            }
        }
        catch (Exception e)
        {
            return Tuple.Create<ModMetadata?, string>(null, $"Failed to load mod: {e.Message}");
        }
    }

    public Dictionary<int, string> ExtractGlifs(string modPath, Dictionary<string, string> glifMap)
    {
        var result = new Dictionary<int, string>();
        if (!IsValidModFile(modPath))
            return result;

        try
        {
            using (var zip = ZipFile.OpenRead(modPath))
            {
                foreach (var pair in glifMap)
                {
                    var entry = zip.GetEntry(pair.Value);
                    if (entry != null)
                    {
                        using (var reader = new StreamReader(entry.Open()))
                        {
                            string data = reader.ReadToEnd();
                            int cpInt;
                            if (pair.Key.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                            {
                                cpInt = Convert.ToInt32(pair.Key, 16);
                            }
                            else
                            {
                                cpInt = Convert.ToInt32(pair.Key);
                            }
                            result[cpInt] = data;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ModHandlerService] GLIF extraction error: {e.Message}");
        }

        return result;
    }

    public List<string> ListModContents(string modPath)
    {
        var list = new List<string>();
        try
        {
            using (var zip = ZipFile.OpenRead(modPath))
            {
                foreach (var entry in zip.Entries)
                {
                    list.Add(entry.FullName);
                }
            }
        }
        catch { }
        return list;
    }

    public List<string> ScanForMods(List<string> directories)
    {
        var found = new List<string>();
        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
                continue;

            try
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(file).ToLower();
                    if (Array.IndexOf(SupportedExtensions, ext) != -1)
                    {
                        found.Add(Path.GetFullPath(file));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ModHandlerService] Error scanning mods in {dir}: {e.Message}");
            }
        }
        found.Sort();
        return found;
    }

    public bool CreateDemoMod(string outputPath)
    {
        try
        {
            var metadata = new Dictionary<string, object>
            {
                { "name", "Demo Mod (GLIF)" },
                { "version", "1.0" },
                { "author", "fModLoader Dev Team" },
                { "description", "A minimal demo mod for testing purposes." },
                { "target_family", "Any" },
                { "glif_map", new Dictionary<string, string>
                    {
                        { "0x0041", "glyphs/A.glif" },
                        { "0x0042", "glyphs/B.glif" }
                    }
                }
            };

            string glifA = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<glyph name=""A"" format=""2"">
  <advance width=""600""/>
  <unicode hex=""0041""/>
  <outline>
    <contour>
      <point x=""200"" y=""0"" type=""line""/>
      <point x=""300"" y=""700"" type=""line""/>
      <point x=""400"" y=""0"" type=""line""/>
    </contour>
  </outline>
</glyph>";

            string glifB = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<glyph name=""B"" format=""2"">
  <advance width=""600""/>
  <unicode hex=""0042""/>
  <outline>
    <contour>
      <point x=""200"" y=""0"" type=""line""/>
      <point x=""500"" y=""350"" type=""curve""/>
      <point x=""200"" y=""700"" type=""line""/>
    </contour>
  </outline>
</glyph>";

            using (var fs = new FileStream(outputPath, FileMode.Create))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var metaEntry = zip.CreateEntry("metadata.json");
                using (var writer = new StreamWriter(metaEntry.Open()))
                {
                    writer.Write(JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
                }

                var glyphAEntry = zip.CreateEntry("glyphs/A.glif");
                using (var writer = new StreamWriter(glyphAEntry.Open()))
                {
                    writer.Write(glifA);
                }

                var glyphBEntry = zip.CreateEntry("glyphs/B.glif");
                using (var writer = new StreamWriter(glyphBEntry.Open()))
                {
                    writer.Write(glifB);
                }
            }
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ModHandlerService] Failed to create demo mod: {e.Message}");
            return false;
        }
    }
}
