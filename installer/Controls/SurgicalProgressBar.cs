using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace FModLoaderInstaller.Controls;

/// <summary>
/// Progress bar with an animated ECG waveform overlay — fills left-to-right with a
/// heartbeat pulse effect running along the leading edge.
/// </summary>
public class SurgicalProgressBar : Control
{
    private double _heartbeatPhase;
    private readonly DispatcherTimer _timer;

    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<SurgicalProgressBar, double>(nameof(Value), 0.0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<SurgicalProgressBar, double>(nameof(Maximum), 100.0);

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public SurgicalProgressBar()
    {
        Height = 12;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _timer.Tick += (_, _) =>
        {
            _heartbeatPhase = (_heartbeatPhase + 0.015) % 1.0;
            InvalidateVisual();
        };
    }

    static SurgicalProgressBar()
    {
        ValueProperty.Changed.AddClassHandler<SurgicalProgressBar>((s, _) => s.InvalidateVisual());
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        var radius = h / 2;
        var fraction = Maximum > 0 ? Math.Clamp(Value / Maximum, 0, 1) : 0;
        var fillW = w * fraction;

        // ── Track (background) ──────────────────────────────────────────
        var trackRect = new Rect(0, 0, w, h);
        context.DrawRectangle(
            new SolidColorBrush(Color.Parse("#F4E4E4")),
            null, trackRect, radius, radius);

        // ── Fill ────────────────────────────────────────────────────────
        if (fillW > 0)
        {
            var fillGradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.Parse("#CC1A1A"), 0),
                    new GradientStop(Color.Parse("#8B0000"), 0.7),
                    new GradientStop(Color.Parse("#CC1A1A"), 1),
                }
            };

            // Clip to rounded rect
            var fillRect = new Rect(0, 0, fillW, h);
            using (context.PushClip(new RoundedRect(trackRect, radius)))
            {
                context.DrawRectangle(fillGradient, null, fillRect);
            }

            // ── ECG overlay on filled portion ───────────────────────────
            if (fillW > 20)
            {
                var ecgPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 1.5,
                    lineCap: PenLineCap.Round);
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    bool first = true;
                    int steps = (int)fillW;
                    var mid = h / 2.0;
                    for (int i = 0; i <= steps; i++)
                    {
                        double t = ((double)i / Math.Max(steps, 1) + _heartbeatPhase * 2) % 1.0;
                        double x = i;
                        double y = mid;

                        if (t > 0.35 && t < 0.42)
                        {
                            double local = (t - 0.35) / 0.07;
                            y = mid - Math.Sin(local * Math.PI) * (h * 0.3);
                        }
                        else if (t >= 0.42 && t < 0.48)
                        {
                            double local = (t - 0.42) / 0.06;
                            y = mid + Math.Sin(local * Math.PI) * (h * 0.15);
                        }

                        var pt = new Point(x, y);
                        if (first) { ctx.BeginFigure(pt, false); first = false; }
                        else ctx.LineTo(pt);
                    }
                }
                using (context.PushClip(new RoundedRect(trackRect, radius)))
                {
                    context.DrawGeometry(null, ecgPen, geometry);
                }
            }

            // ── Leading edge pulse glow ─────────────────────────────────
            if (fillW > 4 && fraction < 1)
            {
                var pulseAlpha = (byte)(100 + 60 * Math.Sin(_heartbeatPhase * Math.PI * 2));
                var glowBrush = new RadialGradientBrush
                {
                    Center = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    GradientOrigin = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    RadiusX = new RelativeScalar(1.0, RelativeUnit.Relative),
                    RadiusY = new RelativeScalar(1.0, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(pulseAlpha, 0xFF, 0x6B, 0x6B), 0),
                        new GradientStop(Color.FromArgb(0, 0xFF, 0x6B, 0x6B), 1),
                    }
                };
                var glowRect = new Rect(fillW - 20, -4, 24, h + 8);
                context.DrawRectangle(glowBrush, null, glowRect);
            }
        }
    }
}
