using System;
using System.Collections.Generic;
using System.IO;

namespace fModLoader.Services;

public class FontDiscoveryService
{
    public List<string> ScanForModcompatFonts(List<string> directories)
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
                    string name = file.ToLower();
                    if (name.Contains(".modcompat.ttf") || name.Contains(".modcompat.otf") || name.Contains(".modcompat.ttc"))
                    {
                        found.Add(Path.GetFullPath(file));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[FontDiscoveryService] Error scanning directory {dir}: {e.Message}");
            }
        }
        found.Sort();
        return found;
    }
}
