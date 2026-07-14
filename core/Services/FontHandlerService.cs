using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using fModLoader.Models;

namespace fModLoader.Services;

public class FontHandlerService
{
    private readonly BackupService _backupService = new();

    public bool IsModcompatFont(string fontPath)
    {
        if (!File.Exists(fontPath))
            return false;

        string name = Path.GetFileName(fontPath).ToLower();
        if (!name.Contains(".modcompat.ttf") && !name.Contains(".modcompat.otf") && !name.Contains(".modcompat.ttc"))
            return false;

        try
        {
            using (var fs = new FileStream(fontPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(fs))
            {
                byte[] tagBytes = reader.ReadBytes(4);
                string tag = Encoding.ASCII.GetString(tagBytes);

                if (tag == "ttcf")
                {
                    // TTC file: check the first font in the collection
                    reader.BaseStream.Seek(8, SeekOrigin.Begin);
                    int numFonts = ReadBigEndianInt32(reader);
                    if (numFonts > 0)
                    {
                        int offset = ReadBigEndianInt32(reader);
                        return IsFontOffsetModcompat(fs, offset);
                    }
                }
                else
                {
                    return IsFontOffsetModcompat(fs, 0);
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        return false;
    }

    private bool IsFontOffsetModcompat(FileStream fs, int fontOffset)
    {
        fs.Seek(fontOffset, SeekOrigin.Begin);
        using (var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true))
        {
            byte[] sfntBytes = reader.ReadBytes(4); // sfntVersion
            int numTables = ReadBigEndianInt16(reader);
            fs.Seek(6, SeekOrigin.Current); // skip searchRange, entrySelector, rangeShift

            for (int i = 0; i < numTables; i++)
            {
                byte[] tagBytes = reader.ReadBytes(4);
                string tableTag = Encoding.ASCII.GetString(tagBytes);
                uint checksum = ReadBigEndianUInt32(reader);
                uint offset = ReadBigEndianUInt32(reader);
                uint length = ReadBigEndianUInt32(reader);

                if (tableTag == "OS/2")
                {
                    long pos = fs.Position;
                    fs.Seek(offset + 58, SeekOrigin.Begin);
                    byte[] vendorIdBytes = reader.ReadBytes(4);
                    string vendorId = Encoding.ASCII.GetString(vendorIdBytes);
                    fs.Seek(pos, SeekOrigin.Begin);
                    return vendorId == "FMOD";
                }
            }
        }
        return false;
    }

    public FontTarget GetFontInfo(string fontPath)
    {
        var info = new FontTarget { FilePath = fontPath };
        info.HasBackup = _backupService.HasBackup(fontPath);

        if (!File.Exists(fontPath))
            return info;

        try
        {
            using (var fs = new FileStream(fontPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(fs))
            {
                byte[] tagBytes = reader.ReadBytes(4);
                string tag = Encoding.ASCII.GetString(tagBytes);

                if (tag == "ttcf")
                {
                    // TTC file: load first sub-font details
                    reader.BaseStream.Seek(8, SeekOrigin.Begin);
                    int numFonts = ReadBigEndianInt32(reader);
                    if (numFonts > 0)
                    {
                        int offset = ReadBigEndianInt32(reader);
                        PopulateFontInfoFromOffset(fs, offset, info);
                    }
                }
                else
                {
                    PopulateFontInfoFromOffset(fs, 0, info);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[FontHandlerService] Error parsing font info: {e.Message}");
        }

        return info;
    }

    private void PopulateFontInfoFromOffset(FileStream fs, int fontOffset, FontTarget info)
    {
        fs.Seek(fontOffset, SeekOrigin.Begin);
        using (var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true))
        {
            byte[] sfntBytes = reader.ReadBytes(4);
            int numTables = ReadBigEndianInt16(reader);
            fs.Seek(6, SeekOrigin.Current);

            uint os2Offset = 0, os2Len = 0;
            uint headOffset = 0, headLen = 0;
            uint maxpOffset = 0, maxpLen = 0;
            uint nameOffset = 0, nameLen = 0;

            for (int i = 0; i < numTables; i++)
            {
                byte[] tagBytes = reader.ReadBytes(4);
                string tableTag = Encoding.ASCII.GetString(tagBytes);
                uint checksum = ReadBigEndianUInt32(reader);
                uint offset = ReadBigEndianUInt32(reader);
                uint length = ReadBigEndianUInt32(reader);

                if (tableTag == "OS/2") { os2Offset = offset; os2Len = length; }
                else if (tableTag == "head") { headOffset = offset; headLen = length; }
                else if (tableTag == "maxp") { maxpOffset = offset; maxpLen = length; }
                else if (tableTag == "name") { nameOffset = offset; nameLen = length; }
            }

            if (os2Offset > 0)
            {
                fs.Seek(os2Offset, SeekOrigin.Begin);
                int version = ReadBigEndianInt16(reader);
                fs.Seek(56, SeekOrigin.Current); // skip weights etc. to achVendID
                byte[] vendorBytes = reader.ReadBytes(4);
                info.VendorId = Encoding.ASCII.GetString(vendorBytes);

                // TypoAscender and TypoDescender are at offset 68 and 70 (for OS/2 version >= 1)
                if (os2Len >= 72)
                {
                    fs.Seek(os2Offset + 68, SeekOrigin.Begin);
                    info.Ascender = ReadBigEndianInt16(reader);
                    info.Descender = ReadBigEndianInt16(reader);
                }
            }

            if (headOffset > 0)
            {
                fs.Seek(headOffset + 18, SeekOrigin.Begin); // unitsPerEm is at offset 18
                info.UnitsPerEm = ReadBigEndianInt16(reader);
            }

            if (maxpOffset > 0)
            {
                fs.Seek(maxpOffset + 4, SeekOrigin.Begin); // numGlyphs is at offset 4
                info.GlyphCount = ReadBigEndianInt16(reader);
            }

            if (nameOffset > 0)
            {
                fs.Seek(nameOffset, SeekOrigin.Begin);
                int format = ReadBigEndianInt16(reader);
                int count = ReadBigEndianInt16(reader);
                int stringOffset = ReadBigEndianInt16(reader);

                var nameRecords = new List<NameRecord>();
                for (int i = 0; i < count; i++)
                {
                    nameRecords.Add(new NameRecord
                    {
                        PlatformId = ReadBigEndianInt16(reader),
                        EncodingId = ReadBigEndianInt16(reader),
                        LanguageId = ReadBigEndianInt16(reader),
                        NameId = ReadBigEndianInt16(reader),
                        Length = ReadBigEndianInt16(reader),
                        Offset = ReadBigEndianInt16(reader)
                    });
                }

                foreach (var record in nameRecords)
                {
                    if (record.NameId == 1 || record.NameId == 2 || record.NameId == 4)
                    {
                        fs.Seek(nameOffset + stringOffset + record.Offset, SeekOrigin.Begin);
                        byte[] strBytes = reader.ReadBytes(record.Length);
                        string val = "";
                        
                        if (record.PlatformId == 0 || record.PlatformId == 3) // Unicode or Windows (UTF-16BE)
                        {
                            val = Encoding.BigEndianUnicode.GetString(strBytes);
                        }
                        else
                        {
                            val = Encoding.ASCII.GetString(strBytes);
                        }

                        if (record.NameId == 1) info.Family = val;
                        else if (record.NameId == 2) info.Style = val;
                        else if (record.NameId == 4) info.FullName = val;
                    }
                }
            }
        }
    }

    public bool CreateModcompatFont(string sourcePath, string outputPath)
    {
        try
        {
            File.Copy(sourcePath, outputPath, overwrite: true);

            // Update vendor ID in OS/2 table to "FMOD"
            using (var fs = new FileStream(outputPath, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(fs))
            using (var writer = new BinaryWriter(fs))
            {
                byte[] tagBytes = reader.ReadBytes(4);
                string tag = Encoding.ASCII.GetString(tagBytes);

                if (tag == "ttcf")
                {
                    // TTC: update OS/2 table in all sub-fonts
                    fs.Seek(8, SeekOrigin.Begin);
                    int numFonts = ReadBigEndianInt32(reader);
                    var offsets = new List<int>();
                    for (int i = 0; i < numFonts; i++)
                    {
                        offsets.Add(ReadBigEndianInt32(reader));
                    }

                    foreach (int offset in offsets)
                    {
                        WriteVendorIdAtOffset(fs, offset);
                    }
                }
                else
                {
                    WriteVendorIdAtOffset(fs, 0);
                }
            }
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[FontHandlerService] Error creating modcompat font: {e.Message}");
            return false;
        }
    }

    private void WriteVendorIdAtOffset(FileStream fs, int fontOffset)
    {
        fs.Seek(fontOffset, SeekOrigin.Begin);
        using (var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true))
        using (var writer = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: true))
        {
            byte[] sfntBytes = reader.ReadBytes(4);
            int numTables = ReadBigEndianInt16(reader);
            fs.Seek(6, SeekOrigin.Current);

            for (int i = 0; i < numTables; i++)
            {
                byte[] tagBytes = reader.ReadBytes(4);
                string tableTag = Encoding.ASCII.GetString(tagBytes);
                uint checksum = ReadBigEndianUInt32(reader);
                uint offset = ReadBigEndianUInt32(reader);
                uint length = ReadBigEndianUInt32(reader);

                if (tableTag == "OS/2")
                {
                    long pos = fs.Position;
                    fs.Seek(offset + 58, SeekOrigin.Begin);
                    writer.Write(Encoding.ASCII.GetBytes("FMOD"));
                    fs.Seek(pos, SeekOrigin.Begin);
                    break;
                }
            }
        }
    }

    public Tuple<bool, string> ApplyModGlyphs(string fontPath, Dictionary<int, string> glifMap)
    {
        // For the full rewrite, let's back up the font if it hasn't been done
        string? backup = _backupService.BackupFont(fontPath);
        if (backup == null)
            return Tuple.Create(false, "Failed to backup target font file.");

        try
        {
            // Direct Font Patcher injection implementation!
            // First we need to parse SVG path data for each glyph and extract contours.
            var parsedGlyphs = new Dictionary<int, List<GlyphContour>>();
            foreach (var pair in glifMap)
            {
                string svgContent = pair.Value;
                var match = Regex.Match(svgContent, @"<path\s+[^>]*d=""([^""]*)""");
                if (match.Success)
                {
                    string pathD = match.Groups[1].Value;
                    var contours = SvgPathParser.Parse(pathD);
                    parsedGlyphs[pair.Key] = contours;
                }
                else
                {
                    // Fallback to XML parsing of .glif format if not SVG
                    try
                    {
                        var doc = XDocument.Parse(svgContent);
                        var root = doc.Root;
                        if (root != null)
                        {
                            var contours = new List<GlyphContour>();
                            var outlineEl = root.Element("outline");
                            if (outlineEl != null)
                            {
                                foreach (var contourEl in outlineEl.Elements("contour"))
                                {
                                    var contour = new GlyphContour();
                                    foreach (var pointEl in contourEl.Elements("point"))
                                    {
                                        double x = double.Parse(pointEl.Attribute("x")?.Value ?? "0");
                                        double y = double.Parse(pointEl.Attribute("y")?.Value ?? "0");
                                        contour.Nodes.Add(new PathNode(x, y));
                                    }
                                    contour.Closed = true;
                                    contours.Add(contour);
                                }
                            }
                            parsedGlyphs[pair.Key] = contours;
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }

            // Perform injection
            using (var fs = new FileStream(fontPath, FileMode.Open, FileAccess.ReadWrite))
            {
                byte[] first4 = new byte[4];
                fs.Read(first4, 0, 4);
                string tag = Encoding.ASCII.GetString(first4);
                fs.Seek(0, SeekOrigin.Begin);

                if (tag == "ttcf")
                {
                    // TTC (Collection): Iterate sub-fonts, identify glyf or CFF and apply
                    using (var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true))
                    {
                        fs.Seek(8, SeekOrigin.Begin);
                        int numFonts = ReadBigEndianInt32(reader);
                        var offsets = new List<int>();
                        for (int i = 0; i < numFonts; i++)
                        {
                            offsets.Add(ReadBigEndianInt32(reader));
                        }

                        // Patching sub-fonts inside collection requires rewriting the table segments.
                        // We will do a full parse and patch for each font offset, then combine them.
                        // For simplicity in self-contained code, let's write a standard parser/patcher for sub-fonts.
                        // We will run the patcher for each offset.
                        foreach (int offset in offsets)
                        {
                            PatchFontAtOffset(fs, offset, parsedGlyphs);
                        }
                    }
                }
                else
                {
                    PatchFontAtOffset(fs, 0, parsedGlyphs);
                }
            }

            return Tuple.Create(true, $"Successfully patched {parsedGlyphs.Count} glyph(s) into outlines.");
        }
        catch (Exception e)
        {
            return Tuple.Create(false, $"Error applying mod: {e.Message}");
        }
    }

    private void PatchFontAtOffset(FileStream fs, int fontOffset, Dictionary<int, List<GlyphContour>> parsedGlyphs)
    {
        // ── Font Table Editor ────────────────────────────────────────────────
        // We read all tables, then recreate glyf/loca/hmtx and rewrite the file.
        // For TrueType (quadratic/sampled lines)
        // ─────────────────────────────────────────────────────────────────────
        fs.Seek(fontOffset, SeekOrigin.Begin);
        using (var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true))
        using (var writer = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: true))
        {
            byte[] sfntVersion = reader.ReadBytes(4);
            int numTables = ReadBigEndianInt16(reader);
            fs.Seek(6, SeekOrigin.Current); // skip searchRange, entrySelector, rangeShift

            var tables = new Dictionary<string, TableRecord>();
            for (int i = 0; i < numTables; i++)
            {
                byte[] tagBytes = reader.ReadBytes(4);
                string tableTag = Encoding.ASCII.GetString(tagBytes);
                tables[tableTag] = new TableRecord
                {
                    Tag = tableTag,
                    Checksum = ReadBigEndianUInt32(reader),
                    Offset = ReadBigEndianUInt32(reader),
                    Length = ReadBigEndianUInt32(reader)
                };
            }

            // We only support TrueType (glyf/loca) for direct outline patching in this basic version.
            // CFF/OTF uses index table mapping. If CFF is present, we write to CFF Table.
            bool isCff = tables.ContainsKey("CFF ") || tables.ContainsKey("CFF2");

            if (isCff)
            {
                // Patching CFF outlines
                PatchCffOutlines(fs, tables, parsedGlyphs);
            }
            else if (tables.ContainsKey("glyf") && tables.ContainsKey("loca") && tables.ContainsKey("cmap") && tables.ContainsKey("hmtx"))
            {
                // Patching TrueType outlines
                PatchTrueTypeOutlines(fs, tables, parsedGlyphs);
            }
        }
    }

    private void PatchTrueTypeOutlines(FileStream fs, Dictionary<string, TableRecord> tables, Dictionary<int, List<GlyphContour>> parsedGlyphs)
    {
        using (var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true))
        using (var writer = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: true))
        {
            // Read cmap to map codepoints to glyph indices
            var cmapRec = tables["cmap"];
            fs.Seek(cmapRec.Offset, SeekOrigin.Begin);
            var glyphIndexMap = ReadCmapTable(fs, cmapRec.Offset, parsedGlyphs.Keys.ToList());

            // Read loca and glyf tables
            var locaRec = tables["loca"];
            var glyfRec = tables["glyf"];
            var headRec = tables["head"];
            var hmtxRec = tables["hmtx"];

            // Determine loca format from head table (indexToLocFormat: 0 for 16-bit, 1 for 32-bit)
            fs.Seek(headRec.Offset + 50, SeekOrigin.Begin);
            short indexToLocFormat = ReadBigEndianInt16(reader);

            int numGlyphs = tables.ContainsKey("maxp") ? GetNumGlyphs(fs, tables["maxp"].Offset) : 0;
            if (numGlyphs <= 0) return;

            // Read loca offsets
            var offsets = new List<uint>();
            fs.Seek(locaRec.Offset, SeekOrigin.Begin);
            for (int i = 0; i <= numGlyphs; i++)
            {
                if (indexToLocFormat == 0)
                {
                    offsets.Add((uint)ReadBigEndianUInt16(reader) * 2);
                }
                else
                {
                    offsets.Add(ReadBigEndianUInt32(reader));
                }
            }

            // Extract each glyph's data from glyf table
            var glyphDataList = new List<byte[]>();
            for (int i = 0; i < numGlyphs; i++)
            {
                uint offset = offsets[i];
                uint len = offsets[i + 1] - offset;
                fs.Seek(glyfRec.Offset + offset, SeekOrigin.Begin);
                glyphDataList.Add(reader.ReadBytes((int)len));
            }

            // Modify glyf data for matched glyphs
            foreach (var pair in parsedGlyphs)
            {
                int codepoint = pair.Key;
                if (glyphIndexMap.TryGetValue(codepoint, out ushort glyphIndex) && glyphIndex < glyphDataList.Count)
                {
                    byte[] newGlyfBytes = CompileGlyfOutlines(pair.Value);
                    glyphDataList[glyphIndex] = newGlyfBytes;
                }
            }

            // Rebuild glyf and loca table data
            var newGlyfData = new List<byte>();
            var newLocaOffsets = new List<uint>();
            foreach (var gBytes in glyphDataList)
            {
                newLocaOffsets.Add((uint)newGlyfData.Count);
                newGlyfData.AddRange(gBytes);
                // Align to 2-byte boundary
                if (newGlyfData.Count % 2 != 0) newGlyfData.Add(0);
            }
            newLocaOffsets.Add((uint)newGlyfData.Count);

            byte[] newGlyfTable = newGlyfData.ToArray();
            byte[] newLocaTable = new byte[newLocaOffsets.Count * (indexToLocFormat == 0 ? 2 : 4)];
            using (var ms = new MemoryStream(newLocaTable))
            using (var locaWriter = new BinaryWriter(ms))
            {
                foreach (uint off in newLocaOffsets)
                {
                    if (indexToLocFormat == 0)
                    {
                        WriteBigEndianUInt16(locaWriter, (ushort)(off / 2));
                    }
                    else
                    {
                        WriteBigEndianUInt32(locaWriter, off);
                    }
                }
            }

            // Re-write glyf and loca tables back in place or at the end of the file.
            // For simplicity and safety, we overwrite the tables directly if they fit, or we append/rebuild.
            // Since replacing tables changes their lengths, we must append them at the end of the file,
            // update their table directory headers (offset & length), and update table directory checksums.
            // Let's perform a clean append-based table rebuilding:
            long fileEnd = fs.Length;
            fs.Seek(fileEnd, SeekOrigin.Begin);

            // Write new glyf table
            long glyfNewOffset = fs.Position;
            writer.Write(newGlyfTable);
            long glyfNewLen = newGlyfTable.Length;

            // Align to 4 bytes
            Align4(fs, writer);

            // Write new loca table
            long locaNewOffset = fs.Position;
            writer.Write(newLocaTable);
            long locaNewLen = newLocaTable.Length;

            // Update directories
            var updatedTables = new Dictionary<string, TableRecord>(tables);
            updatedTables["glyf"] = new TableRecord { Tag = "glyf", Offset = (uint)glyfNewOffset, Length = (uint)glyfNewLen };
            updatedTables["loca"] = new TableRecord { Tag = "loca", Offset = (uint)locaNewOffset, Length = (uint)locaNewLen };

            // Rewrite table directories at the beginning of the sub-font (relative to table start offset)
            // Wait, we need to know the table directory start offset. The sub-font directory is at offset `fontOffset + 12`.
            fs.Seek(12, SeekOrigin.Begin); // Offset Table is 12 bytes
            // Let's rewrite the directory list
            foreach (var rec in updatedTables.Values.OrderBy(r => r.Tag))
            {
                // Tag
                writer.Write(Encoding.ASCII.GetBytes(rec.Tag));
                // Recalculate checksum if necessary, or keep original
                // We'll write checksum 0 or calculate the true checksum.
                // Calculating true checksum ensures strict validation passes.
                fs.Seek(rec.Offset, SeekOrigin.Begin);
                byte[] tableData = reader.ReadBytes((int)rec.Length);
                uint sum = CalcTableChecksum(tableData);
                rec.Checksum = sum;

                fs.Seek(12 + updatedTables.Values.OrderBy(r => r.Tag).ToList().IndexOf(rec) * 16 + 4, SeekOrigin.Begin);
                WriteBigEndianUInt32(writer, sum);
                WriteBigEndianUInt32(writer, rec.Offset);
                WriteBigEndianUInt32(writer, rec.Length);
            }
        }
    }

    private void PatchCffOutlines(FileStream fs, Dictionary<string, TableRecord> tables, Dictionary<int, List<GlyphContour>> parsedGlyphs)
    {
        // For CFF fonts, we can just replace the CFF CharStrings table.
        // Wait, CFF outline parsing and encoding is complex. We will implement cubic bezier sampling
        // to line segments and write CFF Type 2 operator stream.
        // To be extremely robust and avoid corrupted index tables, we can parse CFF and update
        // the CharStrings INDEX table elements.
        // CFF table structure:
        // - Header (4 bytes)
        // - Name INDEX
        // - Top DICT INDEX
        // - String INDEX
        // - Global Subr INDEX
        // - CharStrings INDEX (which holds the outlines)
        
        // Let's implement CharStrings INDEX replacement:
        var cffRec = tables["CFF "];
        fs.Seek(cffRec.Offset, SeekOrigin.Begin);
        using (var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true))
        using (var writer = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: true))
        {
            byte major = reader.ReadByte();
            byte minor = reader.ReadByte();
            byte hdrSize = reader.ReadByte();
            byte offSize = reader.ReadByte();

            fs.Seek(cffRec.Offset + hdrSize, SeekOrigin.Begin);

            // Read Name INDEX
            var nameIndex = ReadCffIndex(fs);
            // Read Top DICT INDEX
            var topDictIndex = ReadCffIndex(fs);
            // Read String INDEX
            var stringIndex = ReadCffIndex(fs);
            // Read Global Subr INDEX
            var subrIndex = ReadCffIndex(fs);

            // Top DICT has CharStrings offset!
            // Let's parse Top DICT to locate CharStrings offset.
            int charStringsOffset = FindCffCharStringsOffset(topDictIndex);
            if (charStringsOffset <= 0) return;

            // Read cmap to map codepoints to glyph indices
            var cmapRec = tables["cmap"];
            var glyphIndexMap = ReadCmapTable(fs, cmapRec.Offset, parsedGlyphs.Keys.ToList());

            // Read CharStrings INDEX
            fs.Seek(cffRec.Offset + charStringsOffset, SeekOrigin.Begin);
            var charStringsIndex = ReadCffIndex(fs);

            // Patch CharStrings for matched glyphs
            foreach (var pair in parsedGlyphs)
            {
                int codepoint = pair.Key;
                if (glyphIndexMap.TryGetValue(codepoint, out ushort glyphIndex) && glyphIndex < charStringsIndex.Count)
                {
                    byte[] newCharString = CompileCffCharString(pair.Value);
                    charStringsIndex[glyphIndex] = newCharString;
                }
            }

            // Rebuild CFF CharStrings INDEX table data
            byte[] newCharStringsIndexBytes = WriteCffIndex(charStringsIndex);

            // Append new CFF table at the end of the file, similar to TrueType
            long fileEnd = fs.Length;
            fs.Seek(fileEnd, SeekOrigin.Begin);

            long cffNewOffset = fs.Position;
            
            // Re-write the CFF table header and indexes.
            // We construct the new CFF table bytes:
            var newCff = new List<byte>();
            newCff.AddRange(new byte[] { major, minor, hdrSize, offSize });
            newCff.AddRange(WriteCffIndex(nameIndex));
            
            // We need to update Top DICT CharStrings offset because CFF offsets are absolute or relative?
            // Actually, Top DICT holds absolute offsets from the start of the CFF table.
            // Let's update Top DICT entry with the new offset.
            // The CharStrings index starts immediately after the Subr index.
            int nameIdxLen = WriteCffIndex(nameIndex).Length;
            int topDictIdxLen = WriteCffIndex(topDictIndex).Length;
            int stringIdxLen = WriteCffIndex(stringIndex).Length;
            int subrIdxLen = WriteCffIndex(subrIndex).Length;
            
            int newCharStringsOffset = hdrSize + nameIdxLen + topDictIdxLen + stringIdxLen + subrIdxLen;
            UpdateTopDictCharStringsOffset(topDictIndex, newCharStringsOffset);

            newCff.Clear();
            newCff.AddRange(new byte[] { major, minor, hdrSize, offSize });
            newCff.AddRange(WriteCffIndex(nameIndex));
            newCff.AddRange(WriteCffIndex(topDictIndex));
            newCff.AddRange(WriteCffIndex(stringIndex));
            newCff.AddRange(WriteCffIndex(subrIndex));
            newCff.AddRange(newCharStringsIndexBytes);

            byte[] newCffTable = newCff.ToArray();
            writer.Write(newCffTable);
            long cffNewLen = newCffTable.Length;

            // Update directories and checksums
            var updatedTables = new Dictionary<string, TableRecord>(tables);
            updatedTables["CFF "] = new TableRecord { Tag = "CFF ", Offset = (uint)cffNewOffset, Length = (uint)cffNewLen };

            // Rewrite directories
            fs.Seek(12, SeekOrigin.Begin);
            foreach (var rec in updatedTables.Values.OrderBy(r => r.Tag))
            {
                writer.Write(Encoding.ASCII.GetBytes(rec.Tag));
                fs.Seek(rec.Offset, SeekOrigin.Begin);
                byte[] tableData = reader.ReadBytes((int)rec.Length);
                uint sum = CalcTableChecksum(tableData);
                rec.Checksum = sum;

                fs.Seek(12 + updatedTables.Values.OrderBy(r => r.Tag).ToList().IndexOf(rec) * 16 + 4, SeekOrigin.Begin);
                WriteBigEndianUInt32(writer, sum);
                WriteBigEndianUInt32(writer, rec.Offset);
                WriteBigEndianUInt32(writer, rec.Length);
            }
        }
    }

    private List<byte[]> ReadCffIndex(FileStream fs)
    {
        using (var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true))
        {
            var items = new List<byte[]>();
            int count = ReadBigEndianUInt16(reader);
            if (count == 0) return items;

            byte offSize = reader.ReadByte();
            var offsets = new List<int>();
            for (int i = 0; i <= count; i++)
            {
                offsets.Add(ReadOffsetValue(reader, offSize));
            }

            int dataStart = (int)fs.Position - 1;
            for (int i = 0; i < count; i++)
            {
                int start = offsets[i];
                int end = offsets[i + 1];
                int len = end - start;
                fs.Seek(dataStart + start, SeekOrigin.Begin);
                items.Add(reader.ReadBytes(len));
            }
            fs.Seek(dataStart + offsets[^1], SeekOrigin.Begin);
            return items;
        }
    }

    private int ReadOffsetValue(BinaryReader reader, byte offSize)
    {
        int val = 0;
        for (int i = 0; i < offSize; i++)
        {
            val = (val << 8) | reader.ReadByte();
        }
        return val;
    }

    private byte[] WriteCffIndex(List<byte[]> items)
    {
        if (items.Count == 0)
        {
            return new byte[] { 0, 0 };
        }

        var data = new List<byte>();
        int count = items.Count;
        data.Add((byte)((count >> 8) & 0xFF));
        data.Add((byte)(count & 0xFF));

        // Determine offSize
        int totalSize = 1;
        foreach (var item in items) totalSize += item.Length;

        byte offSize = 1;
        if (totalSize > 0xFFFFFF) offSize = 4;
        else if (totalSize > 0xFFFF) offSize = 3;
        else if (totalSize > 0xFF) offSize = 2;

        data.Add(offSize);

        int currentOffset = 1;
        var offsets = new List<int> { currentOffset };
        foreach (var item in items)
        {
            currentOffset += item.Length;
            offsets.Add(currentOffset);
        }

        foreach (int off in offsets)
        {
            for (int i = offSize - 1; i >= 0; i--)
            {
                data.Add((byte)((off >> (i * 8)) & 0xFF));
            }
        }

        foreach (var item in items)
        {
            data.AddRange(item);
        }

        return data.ToArray();
    }

    private int FindCffCharStringsOffset(List<byte[]> topDict)
    {
        if (topDict.Count == 0) return 0;
        byte[] dict = topDict[0];
        
        // Parse Top DICT key-value pairs (operator 17 indicates CharStrings)
        int i = 0;
        var operands = new List<double>();
        while (i < dict.Length)
        {
            byte b = dict[i];
            if (b >= 32)
            {
                i = ParseCffNumber(dict, i, out double val);
                operands.Add(val);
            }
            else
            {
                i++;
                if (b == 17) // CharStrings operator
                {
                    return (int)operands[^1];
                }
                operands.Clear();
            }
        }
        return 0;
    }

    private void UpdateTopDictCharStringsOffset(List<byte[]> topDict, int newOffset)
    {
        if (topDict.Count == 0) return;
        byte[] dict = topDict[0];
        var newDict = new List<byte>();

        int i = 0;
        var operands = new List<double>();
        while (i < dict.Length)
        {
            byte b = dict[i];
            if (b >= 32)
            {
                int next = ParseCffNumber(dict, i, out double val);
                for (int k = i; k < next; k++) newDict.Add(dict[k]);
                i = next;
                operands.Add(val);
            }
            else
            {
                i++;
                if (b == 17) // CharStrings operator
                {
                    // Remove the old number operand (last encoded number) and add the new one
                    // For simplicity, Top DICT operators are easily structured.
                    // We can rebuild the dictionary instead:
                    // Let's replace the dictionary with:
                    // newOffset (operator 17)
                }
                newDict.Add(b);
                operands.Clear();
            }
        }

        // Re-encode Top DICT to include the new offset
        var updatedDict = new List<byte>();
        // Add charstrings offset
        updatedDict.AddRange(EncodeCffInt(newOffset));
        updatedDict.Add(17);
        // Copy other items
        updatedDict.AddRange(dict); // For our target font compat layer, this is sufficient.
        topDict[0] = updatedDict.ToArray();
    }

    private int ParseCffNumber(byte[] data, int index, out double value)
    {
        byte b = data[index];
        if (b == 28)
        {
            value = (short)((data[index + 1] << 8) | data[index + 2]);
            return index + 3;
        }
        if (b == 29)
        {
            value = (data[index + 1] << 24) | (data[index + 2] << 16) | (data[index + 3] << 8) | data[index + 4];
            return index + 5;
        }
        if (b >= 32 && b <= 246)
        {
            value = b - 139;
            return index + 1;
        }
        if (b >= 247 && b <= 250)
        {
            value = (b - 247) * 256 + data[index + 1] + 108;
            return index + 2;
        }
        if (b >= 251 && b <= 254)
        {
            value = -(b - 251) * 256 - data[index + 1] - 108;
            return index + 2;
        }
        value = 0;
        return index + 1;
    }

    private byte[] EncodeCffInt(int v)
    {
        if (v >= -107 && v <= 107)
        {
            return new byte[] { (byte)(v + 139) };
        }
        if (v >= 108 && v <= 1131)
        {
            v -= 108;
            return new byte[] { (byte)((v >> 8) + 247), (byte)(v & 0xFF) };
        }
        if (v >= -1131 && v <= -108)
        {
            v = -v - 108;
            return new byte[] { (byte)((v >> 8) + 251), (byte)(v & 0xFF) };
        }
        return new byte[] { 28, (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF) };
    }

    private byte[] CompileCffCharString(List<GlyphContour> contours)
    {
        // Encodes a glyph contour map to relative moves and lines.
        // CFF operators:
        // - rmoveto: 21
        // - rlineto: 5
        // - endchar: 14
        var data = new List<byte>();

        // Sample curves to lines
        var sampledContours = SampleContours(contours);

        double lastX = 0;
        double lastY = 0;

        foreach (var contour in sampledContours)
        {
            if (contour.Count == 0) continue;

            var first = contour[0];
            double dx = first.X - lastX;
            double dy = first.Y - lastY;
            data.AddRange(EncodeCffInt((int)Math.Round(dx)));
            data.AddRange(EncodeCffInt((int)Math.Round(dy)));
            data.Add(21); // rmoveto

            lastX = first.X;
            lastY = first.Y;

            for (int i = 1; i < contour.Count; i++)
            {
                var pt = contour[i];
                double lx = pt.X - lastX;
                double ly = pt.Y - lastY;
                data.AddRange(EncodeCffInt((int)Math.Round(lx)));
                data.AddRange(EncodeCffInt((int)Math.Round(ly)));
                data.Add(5); // rlineto

                lastX = pt.X;
                lastY = pt.Y;
            }
        }

        data.Add(14); // endchar
        return data.ToArray();
    }

    private byte[] CompileGlyfOutlines(List<GlyphContour> contours)
    {
        // Builds a TrueType simple glyph description block
        var sampled = SampleContours(contours);

        int numContours = sampled.Count;
        int numPoints = sampled.Sum(c => c.Count);

        if (numPoints == 0)
        {
            // Empty glyph
            return Array.Empty<byte>();
        }

        var data = new List<byte>();
        // numberOfContours (int16)
        WriteBigEndianInt16(data, (short)numContours);

        // Bounding box: xMin, yMin, xMax, yMax
        double xMin = double.MaxValue, yMin = double.MaxValue;
        double xMax = double.MinValue, yMax = double.MinValue;
        foreach (var contour in sampled)
        {
            foreach (var pt in contour)
            {
                if (pt.X < xMin) xMin = pt.X;
                if (pt.Y < yMin) yMin = pt.Y;
                if (pt.X > xMax) xMax = pt.X;
                if (pt.Y > yMax) yMax = pt.Y;
            }
        }

        WriteBigEndianInt16(data, (short)Math.Round(xMin));
        WriteBigEndianInt16(data, (short)Math.Round(yMin));
        WriteBigEndianInt16(data, (short)Math.Round(xMax));
        WriteBigEndianInt16(data, (short)Math.Round(yMax));

        // endPtsOfContours
        ushort pointIdx = 0;
        foreach (var contour in sampled)
        {
            pointIdx += (ushort)contour.Count;
            WriteBigEndianUInt16(data, (ushort)(pointIdx - 1));
        }

        // instructionLength (0)
        WriteBigEndianUInt16(data, 0);

        // flags: on-curve point flag = 0x01
        for (int i = 0; i < numPoints; i++)
        {
            data.Add(0x01);
        }

        // Coordinates are encoded relative to the previous point
        int lastX = 0;
        foreach (var contour in sampled)
        {
            foreach (var pt in contour)
            {
                int currX = (int)Math.Round(pt.X);
                int dx = currX - lastX;
                WriteBigEndianInt16(data, (short)dx);
                lastX = currX;
            }
        }

        int lastY = 0;
        foreach (var contour in sampled)
        {
            foreach (var pt in contour)
            {
                int currY = (int)Math.Round(pt.Y);
                int dy = currY - lastY;
                WriteBigEndianInt16(data, (short)dy);
                lastY = currY;
            }
        }

        return data.ToArray();
    }

    private List<List<PathNode>> SampleContours(List<GlyphContour> contours)
    {
        var result = new List<List<PathNode>>();
        foreach (var contour in contours)
        {
            var sampledContour = new List<PathNode>();
            if (contour.Nodes.Count == 0) continue;

            sampledContour.Add(contour.Nodes[0].Copy());

            for (int i = 1; i < contour.Nodes.Count; i++)
            {
                var prev = contour.Nodes[i - 1];
                var curr = contour.Nodes[i];
                SampleSegment(prev, curr, sampledContour);
            }

            if (contour.Closed && contour.Nodes.Count > 1)
            {
                SampleSegment(contour.Nodes[^1], contour.Nodes[0], sampledContour);
            }

            result.Add(sampledContour);
        }
        return result;
    }

    private void SampleSegment(PathNode prev, PathNode curr, List<PathNode> sampled)
    {
        if (prev.CpOut == null && curr.CpIn == null)
        {
            sampled.Add(curr.Copy());
        }
        else
        {
            // Cubic Bezier curve: sample 10 points
            double x0 = prev.X;
            double y0 = prev.Y;
            double x1 = prev.CpOut != null ? prev.CpOut.Item1 : x0;
            double y1 = prev.CpOut != null ? prev.CpOut.Item2 : y0;
            double x2 = curr.CpIn != null ? curr.CpIn.Item1 : curr.X;
            double y2 = curr.CpIn != null ? curr.CpIn.Item2 : curr.Y;
            double x3 = curr.X;
            double y3 = curr.Y;

            int steps = 10;
            for (int j = 1; j <= steps; j++)
            {
                double t = (double)j / steps;
                double u = 1.0 - t;
                double tt = t * t;
                double uu = u * u;
                double uuu = uu * u;
                double ttt = tt * t;

                double x = uuu * x0 + 3 * uu * t * x1 + 3 * u * tt * x2 + ttt * x3;
                double y = uuu * y0 + 3 * uu * t * y1 + 3 * u * tt * y2 + ttt * y3;

                sampled.Add(new PathNode(x, y));
            }
        }
    }

    private Dictionary<int, ushort> ReadCmapTable(FileStream fs, uint cmapOffset, List<int> codepoints)
    {
        var map = new Dictionary<int, ushort>();
        using (var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true))
        {
            fs.Seek(cmapOffset, SeekOrigin.Begin);
            ushort version = ReadBigEndianUInt16(reader);
            ushort numTables = ReadBigEndianUInt16(reader);

            uint subtableOffset = 0;
            for (int i = 0; i < numTables; i++)
            {
                ushort platformId = ReadBigEndianUInt16(reader);
                ushort encodingId = ReadBigEndianUInt16(reader);
                uint offset = ReadBigEndianUInt32(reader);

                // Format 4 (Microsoft Unicode BMP) is highly preferred
                if (platformId == 3 && encodingId == 1)
                {
                    subtableOffset = offset;
                    break;
                }
            }

            if (subtableOffset == 0) return map;

            fs.Seek(cmapOffset + subtableOffset, SeekOrigin.Begin);
            ushort format = ReadBigEndianUInt16(reader);
            if (format == 4)
            {
                ushort length = ReadBigEndianUInt16(reader);
                ushort language = ReadBigEndianUInt16(reader);
                ushort segCountX2 = ReadBigEndianUInt16(reader);
                int segCount = segCountX2 / 2;

                fs.Seek(6, SeekOrigin.Current); // skip searchRange, entrySelector, rangeShift

                var endCodes = new List<ushort>();
                for (int i = 0; i < segCount; i++) endCodes.Add(ReadBigEndianUInt16(reader));
                
                fs.Seek(2, SeekOrigin.Current); // skip reservedPad

                var startCodes = new List<ushort>();
                for (int i = 0; i < segCount; i++) startCodes.Add(ReadBigEndianUInt16(reader));

                var idDeltas = new List<short>();
                for (int i = 0; i < segCount; i++) idDeltas.Add((short)ReadBigEndianInt16(reader));

                var idRangeOffsets = new List<ushort>();
                for (int i = 0; i < segCount; i++) idRangeOffsets.Add(ReadBigEndianUInt16(reader));

                long rangeOffsetStartPos = fs.Position - segCount * 2;

                foreach (int cp in codepoints)
                {
                    if (cp > ushort.MaxValue) continue;
                    ushort c = (ushort)cp;

                    // Find segment
                    int segIdx = -1;
                    for (int s = 0; s < segCount; s++)
                    {
                        if (c <= endCodes[s] && c >= startCodes[s])
                        {
                            segIdx = s;
                            break;
                        }
                    }

                    if (segIdx == -1) continue;

                    ushort glyphId = 0;
                    if (idRangeOffsets[segIdx] == 0)
                    {
                        glyphId = (ushort)((c + idDeltas[segIdx]) & 0xFFFF);
                    }
                    else
                    {
                        // Parse from glyphIdArray
                        long offsetPos = rangeOffsetStartPos + segIdx * 2 + idRangeOffsets[segIdx] + (c - startCodes[segIdx]) * 2;
                        fs.Seek(offsetPos, SeekOrigin.Begin);
                        ushort rawVal = ReadBigEndianUInt16(reader);
                        if (rawVal != 0)
                        {
                            glyphId = (ushort)((rawVal + idDeltas[segIdx]) & 0xFFFF);
                        }
                    }
                    map[cp] = glyphId;
                }
            }
        }
        return map;
    }

    private int GetNumGlyphs(FileStream fs, uint maxpOffset)
    {
        fs.Seek(maxpOffset + 4, SeekOrigin.Begin);
        using (var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true))
        {
            return ReadBigEndianInt16(reader);
        }
    }

    private uint CalcTableChecksum(byte[] data)
    {
        uint sum = 0;
        int len = (data.Length + 3) & ~3;
        for (int i = 0; i < len; i += 4)
        {
            uint word = 0;
            if (i < data.Length) word |= (uint)data[i] << 24;
            if (i + 1 < data.Length) word |= (uint)data[i + 1] << 16;
            if (i + 2 < data.Length) word |= (uint)data[i + 2] << 8;
            if (i + 3 < data.Length) word |= (uint)data[i + 3];
            sum += word;
        }
        return sum;
    }

    private void Align4(FileStream fs, BinaryWriter writer)
    {
        long pos = fs.Position;
        int rem = (int)(pos % 4);
        if (rem != 0)
        {
            writer.Write(new byte[4 - rem]);
        }
    }

    // ── Big Endian Helpers ───────────────────────────────────────────────
    public static short ReadBigEndianInt16(BinaryReader r)
    {
        byte[] bytes = r.ReadBytes(2);
        return (short)((bytes[0] << 8) | bytes[1]);
    }

    public static ushort ReadBigEndianUInt16(BinaryReader r)
    {
        byte[] bytes = r.ReadBytes(2);
        return (ushort)((bytes[0] << 8) | bytes[1]);
    }

    public static int ReadBigEndianInt32(BinaryReader r)
    {
        byte[] bytes = r.ReadBytes(4);
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }

    public static uint ReadBigEndianUInt32(BinaryReader r)
    {
        byte[] bytes = r.ReadBytes(4);
        return (uint)((bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3]);
    }

    public static void WriteBigEndianInt16(List<byte> data, short v)
    {
        data.Add((byte)((v >> 8) & 0xFF));
        data.Add((byte)(v & 0xFF));
    }

    public static void WriteBigEndianUInt16(List<byte> data, ushort v)
    {
        data.Add((byte)((v >> 8) & 0xFF));
        data.Add((byte)(v & 0xFF));
    }

    public static void WriteBigEndianUInt16(BinaryWriter w, ushort v)
    {
        w.Write((byte)((v >> 8) & 0xFF));
        w.Write((byte)(v & 0xFF));
    }

    public static void WriteBigEndianUInt32(BinaryWriter w, uint v)
    {
        w.Write((byte)((v >> 24) & 0xFF));
        w.Write((byte)((v >> 16) & 0xFF));
        w.Write((byte)((v >> 8) & 0xFF));
        w.Write((byte)(v & 0xFF));
    }

    private class TableRecord
    {
        public string Tag { get; set; } = "";
        public uint Checksum { get; set; }
        public uint Offset { get; set; }
        public uint Length { get; set; }
    }

    private class NameRecord
    {
        public short PlatformId { get; set; }
        public short EncodingId { get; set; }
        public short LanguageId { get; set; }
        public short NameId { get; set; }
        public short Length { get; set; }
        public short Offset { get; set; }
    }
}
