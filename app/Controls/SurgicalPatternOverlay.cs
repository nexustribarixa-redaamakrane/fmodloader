using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace fModLoader.Controls;

/// <summary>
/// Draws subtle surgical background patterns — cross-hatch lines and faint 'f' watermarks.
/// </summary>
public class SurgicalPatternOverlay : Control
{
    public static readonly StyledProperty<bool> ShowCrossHatchProperty =
        AvaloniaProperty.Register<SurgicalPatternOverlay, bool>(nameof(ShowCrossHatch), true);

    public static readonly StyledProperty<bool> ShowWatermarksProperty =
        AvaloniaProperty.Register<SurgicalPatternOverlay, bool>(nameof(ShowWatermarks), true);

    public static readonly StyledProperty<bool> ShowDotGridProperty =
        AvaloniaProperty.Register<SurgicalPatternOverlay, bool>(nameof(ShowDotGrid), false);

    public static readonly StyledProperty<double> PatternOpacityProperty =
        AvaloniaProperty.Register<SurgicalPatternOverlay, double>(nameof(PatternOpacity), 0.04);

    public bool ShowCrossHatch
    {
        get => GetValue(ShowCrossHatchProperty);
        set => SetValue(ShowCrossHatchProperty, value);
    }

    public bool ShowWatermarks
    {
        get => GetValue(ShowWatermarksProperty);
        set => SetValue(ShowWatermarksProperty, value);
    }

    public bool ShowDotGrid
    {
        get => GetValue(ShowDotGridProperty);
        set => SetValue(ShowDotGridProperty, value);
    }

    public double PatternOpacity
    {
        get => GetValue(PatternOpacityProperty);
        set => SetValue(PatternOpacityProperty, value);
    }

    public SurgicalPatternOverlay()
    {
        IsHitTestVisible = false;
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var alpha = (byte)(255 * PatternOpacity);
        var patternColor = Color.FromArgb(alpha, 0xCC, 0x1A, 0x1A);

        // ── Cross-hatch diagonal lines ──────────────────────────────────
        if (ShowCrossHatch)
        {
            var pen = new Pen(new SolidColorBrush(patternColor), 0.5);
            double spacing = 32;

            // 45° lines
            for (double i = -h; i < w + h; i += spacing)
            {
                context.DrawLine(pen, new Point(i, 0), new Point(i + h, h));
            }
            // 135° lines
            for (double i = -h; i < w + h; i += spacing)
            {
                context.DrawLine(pen, new Point(i + h, 0), new Point(i, h));
            }
        }

        // ── Dot grid ────────────────────────────────────────────────────
        if (ShowDotGrid)
        {
            var dotBrush = new SolidColorBrush(Color.FromArgb((byte)(alpha * 0.7), 0xCC, 0x1A, 0x1A));
            double spacing = 24;
            double dotRadius = 1;

            for (double x = spacing; x < w; x += spacing)
            {
                for (double y = spacing; y < h; y += spacing)
                {
                    context.DrawEllipse(dotBrush, null, new Point(x, y), dotRadius, dotRadius);
                }
            }
        }

        // ── Faint 'f' watermarks ────────────────────────────────────────
        if (ShowWatermarks)
        {
            var wmColor = Color.FromArgb((byte)(255 * 0.035), 0xCC, 0x1A, 0x1A);
            var typeface = new Typeface("Georgia", FontStyle.Italic, FontWeight.Bold);

            // Scatter watermarks at pseudo-random positions
            var positions = new (double xFrac, double yFrac, double size)[]
            {
                (0.05, 0.15, 80), (0.25, 0.45, 100), (0.55, 0.12, 70),
                (0.75, 0.55, 90), (0.90, 0.25, 75), (0.15, 0.75, 85),
                (0.60, 0.80, 95), (0.40, 0.30, 65),
            };

            foreach (var (xFrac, yFrac, size) in positions)
            {
                var formattedText = new FormattedText(
                    "f",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    size,
                    new SolidColorBrush(wmColor));

                context.DrawText(formattedText, new Point(w * xFrac, h * yFrac));
            }
        }
    }
}
