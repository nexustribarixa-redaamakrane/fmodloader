using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace FModLoaderInstaller.Controls;

/// <summary>
/// Animated ECG/heartbeat line control — the signature surgical pattern of fModLoader.
/// Draws a continuously scrolling QRS-complex waveform.
/// </summary>
public class HeartbeatLine : Control
{
    private double _phase;
    private readonly DispatcherTimer _timer;

    public static readonly StyledProperty<IBrush?> StrokeBrushProperty =
        AvaloniaProperty.Register<HeartbeatLine, IBrush?>(nameof(StrokeBrush),
            new SolidColorBrush(Color.Parse("#CC1A1A")));

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<HeartbeatLine, double>(nameof(StrokeThickness), 2.0);

    public static readonly StyledProperty<double> AmplitudeProperty =
        AvaloniaProperty.Register<HeartbeatLine, double>(nameof(Amplitude), 0.35);

    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<HeartbeatLine, double>(nameof(Speed), 0.008);

    public IBrush? StrokeBrush
    {
        get => GetValue(StrokeBrushProperty);
        set => SetValue(StrokeBrushProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double Amplitude
    {
        get => GetValue(AmplitudeProperty);
        set => SetValue(AmplitudeProperty, value);
    }

    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public HeartbeatLine()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
        _timer.Tick += (_, _) =>
        {
            _phase = (_phase + Speed) % 1.0;
            InvalidateVisual();
        };
        IsHitTestVisible = false;
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

        var pen = new Pen(StrokeBrush, StrokeThickness, lineCap: PenLineCap.Round);
        var mid = h / 2.0;
        var amp = h * Amplitude;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            bool first = true;
            int steps = (int)w;

            for (int i = 0; i <= steps; i++)
            {
                double t = ((double)i / steps + _phase) % 1.0;
                double x = i;
                double y;

                // QRS complex waveform pattern
                if (t > 0.28 && t < 0.33)
                {
                    // P wave (small bump before spike)
                    double local = (t - 0.28) / 0.05;
                    y = mid - Math.Sin(local * Math.PI) * amp * 0.15;
                }
                else if (t >= 0.33 && t < 0.36)
                {
                    // Q dip
                    double local = (t - 0.33) / 0.03;
                    y = mid + Math.Sin(local * Math.PI) * amp * 0.12;
                }
                else if (t >= 0.36 && t < 0.42)
                {
                    // R spike (main peak)
                    double local = (t - 0.36) / 0.06;
                    y = mid - Math.Sin(local * Math.PI) * amp;
                }
                else if (t >= 0.42 && t < 0.47)
                {
                    // S dip
                    double local = (t - 0.42) / 0.05;
                    y = mid + Math.Sin(local * Math.PI) * amp * 0.3;
                }
                else if (t >= 0.47 && t < 0.54)
                {
                    // T wave (recovery bump)
                    double local = (t - 0.47) / 0.07;
                    y = mid - Math.Sin(local * Math.PI) * amp * 0.2;
                }
                else
                {
                    // Baseline with subtle noise
                    y = mid + Math.Sin(t * Math.PI * 8) * 0.8;
                }

                if (first)
                {
                    ctx.BeginFigure(new Point(x, y), false);
                    first = false;
                }
                else
                {
                    ctx.LineTo(new Point(x, y));
                }
            }
        }

        context.DrawGeometry(null, pen, geometry);
    }
}
