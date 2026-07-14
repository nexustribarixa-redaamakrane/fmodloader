// Converts a PNG to a multi-size ICO file (16, 32, 48, 64, 128, 256)
// Usage: dotnet script png_to_ico.csx <input.png> <output.ico>
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

var inputPng = args[0];
var outputIco = args[1];

var sizes = new int[] { 16, 24, 32, 48, 64, 128, 256 };

using var original = new Bitmap(inputPng);

using var ms = new MemoryStream();
using var bw = new BinaryWriter(ms);

// ICO Header
bw.Write((short)0);       // Reserved
bw.Write((short)1);       // Type: ICO
bw.Write((short)sizes.Length); // Number of images

// We'll write the directory entries first, then the image data
var imageDataList = new List<byte[]>();

foreach (var size in sizes)
{
    using var resized = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(resized))
    {
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelFormat = PixelFormat.Format32bppArgb;
        g.DrawImage(original, 0, 0, size, size);
    }
    using var pngMs = new MemoryStream();
    resized.Save(pngMs, ImageFormat.Png);
    imageDataList.Add(pngMs.ToArray());
}

// Calculate offset: header (6 bytes) + directory entries (16 bytes each)
int offset = 6 + (sizes.Length * 16);

for (int i = 0; i < sizes.Length; i++)
{
    var size = sizes[i];
    var data = imageDataList[i];
    
    bw.Write((byte)(size >= 256 ? 0 : size)); // Width (0 = 256)
    bw.Write((byte)(size >= 256 ? 0 : size)); // Height (0 = 256)
    bw.Write((byte)0);                         // Color palette
    bw.Write((byte)0);                         // Reserved
    bw.Write((short)1);                         // Color planes
    bw.Write((short)32);                        // Bits per pixel
    bw.Write(data.Length);                      // Size of image data
    bw.Write(offset);                           // Offset to image data
    
    offset += data.Length;
}

// Write image data
foreach (var data in imageDataList)
{
    bw.Write(data);
}

bw.Flush();
File.WriteAllBytes(outputIco, ms.ToArray());
Console.WriteLine($"ICO written to {outputIco} with {sizes.Length} sizes");
