using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

if (args.Length < 2)
{
    Console.WriteLine("Usage: png2ico <input.png> <output.ico>");
    return;
}

var inputPng = args[0];
var outputIco = args[1];
var sizes = new int[] { 16, 24, 32, 48, 64, 128, 256 };

using var original = new Bitmap(inputPng);
using var ms = new MemoryStream();
using var bw = new BinaryWriter(ms);

// ICO Header
bw.Write((short)0);       // Reserved
bw.Write((short)1);       // Type: ICO
bw.Write((short)sizes.Length);

var imageDataList = new List<byte[]>();

foreach (var size in sizes)
{
    using var resized = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(resized))
    {
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.DrawImage(original, 0, 0, size, size);
    }
    using var pngMs = new MemoryStream();
    resized.Save(pngMs, ImageFormat.Png);
    imageDataList.Add(pngMs.ToArray());
}

int offset = 6 + (sizes.Length * 16);

for (int i = 0; i < sizes.Length; i++)
{
    var size = sizes[i];
    var data = imageDataList[i];
    bw.Write((byte)(size >= 256 ? 0 : size));
    bw.Write((byte)(size >= 256 ? 0 : size));
    bw.Write((byte)0);
    bw.Write((byte)0);
    bw.Write((short)1);
    bw.Write((short)32);
    bw.Write(data.Length);
    bw.Write(offset);
    offset += data.Length;
}

foreach (var data in imageDataList)
    bw.Write(data);

bw.Flush();
File.WriteAllBytes(outputIco, ms.ToArray());
Console.WriteLine($"ICO written to {outputIco} with {sizes.Length} sizes");
