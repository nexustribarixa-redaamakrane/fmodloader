using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace FModLoaderInstaller.Controls;

/// <summary>
/// Wizard step indicator with dots that morph between circle (inactive) and
/// rounded rectangle (active), connected by an animated ECG heartbeat line.
/// MD3 Expressive shape morphing in action.
/// </summary>
public class StepIndicator : Control
{
    private double _heartbeatPhase;
    private readonly DispatcherTimer _timer;

    public static readonly StyledProperty<int> CurrentStepProperty =
        AvaloniaProperty.Register<StepIndicator, int>(nameof(CurrentStep), 0);

    public static readonly StyledProperty<int> TotalStepsProperty =
        AvaloniaProperty.Register<StepIndicator, int>(nameof(TotalSteps), 8);

    public static readonly StyledProperty<IList<string>?> StepNamesProperty =
        AvaloniaProperty.Register<StepIndicator, IList<string>?>(nameof(StepNames));

    public int CurrentStep
    {
        get => GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    public int TotalSteps
    {
        get => GetValue(TotalStepsProperty);
        set => SetValue(TotalStepsProperty, value);
    }

    public IList<string>? StepNames
    {
        get => GetValue(StepNamesProperty);
        set => SetValue(StepNamesProperty, value);
    }

    public StepIndicator()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _timer.Tick += (_, _) =>
        {
            _heartbeatPhase = (_heartbeatPhase + 0.006) % 1.0;
            InvalidateVisual();
        };
        Height = 80;
    }

    static StepIndicator()
    {
        CurrentStepProperty.Changed.AddClassHandler<StepIndicator>((s, _) => s.InvalidateVisual());
        TotalStepsProperty.Changed.AddClassHandler<StepIndicator>((s, _) => s.InvalidateVisual());
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
        if (w <= 0 || h <= 0 || TotalSteps <= 0) return;

        var primaryColor = Color.Parse("#CC1A1A");
        var completedColor = Color.Parse("#8B0000");
        var inactiveColor = Color.Parse("#D8C2C2");
        var textColor = Color.Parse("#1C1B1B");
        var lightTextColor = Color.Parse("#857373");

        var dotY = h * 0.35;
        var padding = 48.0;
        var usableWidth = w - padding * 2;
        var stepSpacing = TotalSteps > 1 ? usableWidth / (TotalSteps - 1) : 0;

        // ── Draw connecting lines with heartbeat ────────────────────────
        for (int i = 0; i < TotalSteps - 1; i++)
        {
            var x1 = padding + i * stepSpacing;
            var x2 = padding + (i + 1) * stepSpacing;
            var isCompleted = i < CurrentStep;

            var lineColor = isCompleted ? completedColor : inactiveColor;
            var pen = new Pen(new SolidColorBrush(lineColor), isCompleted ? 2.5 : 1.5, lineCap: PenLineCap.Round);

            if (isCompleted && i == CurrentStep - 1)
            {
                // Animated heartbeat on the last completed segment
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    int steps = (int)(x2 - x1);
                    bool first = true;
                    for (int s = 0; s <= steps; s++)
                    {
                        double t = ((double)s / steps + _heartbeatPhase * 3) % 1.0;
                        double x = x1 + s;
                        double y = dotY;

                        if (t > 0.35 && t < 0.42)
                        {
                            double local = (t - 0.35) / 0.07;
                            y = dotY - Math.Sin(local * Math.PI) * 8;
                        }
                        else if (t >= 0.42 && t < 0.48)
                        {
                            double local = (t - 0.42) / 0.06;
                            y = dotY + Math.Sin(local * Math.PI) * 4;
                        }

                        var pt = new Point(x, y);
                        if (first) { ctx.BeginFigure(pt, false); first = false; }
                        else ctx.LineTo(pt);
                    }
                }
                var hbPen = new Pen(new SolidColorBrush(primaryColor), 2.5, lineCap: PenLineCap.Round);
                context.DrawGeometry(null, hbPen, geometry);
            }
            else
            {
                context.DrawLine(pen, new Point(x1, dotY), new Point(x2, dotY));
            }
        }

        // ── Draw step dots (shape morphing: circle → rounded rect for active) ──
        for (int i = 0; i < TotalSteps; i++)
        {
            var cx = padding + i * stepSpacing;
            var isCurrent = i == CurrentStep;
            var isCompleted = i < CurrentStep;

            if (isCurrent)
            {
                // MD3E shape morph: active step is a rounded rectangle (pill)
                var pillW = 28.0;
                var pillH = 14.0;
                var rect = new Rect(cx - pillW / 2, dotY - pillH / 2, pillW, pillH);

                // Glow effect
                var glowBrush = new SolidColorBrush(Color.FromArgb(40, 0xCC, 0x1A, 0x1A));
                context.DrawRectangle(glowBrush, null, rect.Inflate(3), 10, 10);

                context.DrawRectangle(
                    new SolidColorBrush(primaryColor),
                    new Pen(new SolidColorBrush(Color.Parse("#FF6B6B")), 1.5),
                    rect, 7, 7);

                // Step number inside
                var numText = new FormattedText(
                    (i + 1).ToString(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Google Sans", FontStyle.Normal, FontWeight.Bold),
                    10, new SolidColorBrush(Colors.White));
                context.DrawText(numText, new Point(cx - numText.Width / 2, dotY - numText.Height / 2));
            }
            else
            {
                // Circle dot
                var radius = isCompleted ? 6.0 : 5.0;
                var dotBrush = new SolidColorBrush(isCompleted ? completedColor : inactiveColor);
                var dotPen = isCompleted ? null : new Pen(new SolidColorBrush(inactiveColor), 1.5);
                var fillBrush = isCompleted ? dotBrush : new SolidColorBrush(Colors.White);

                context.DrawEllipse(fillBrush, dotPen, new Point(cx, dotY), radius, radius);

                // Checkmark for completed steps
                if (isCompleted)
                {
                    var checkPen = new Pen(new SolidColorBrush(Colors.White), 1.5, lineCap: PenLineCap.Round);
                    context.DrawLine(checkPen, new Point(cx - 3, dotY), new Point(cx - 0.5, dotY + 2.5));
                    context.DrawLine(checkPen, new Point(cx - 0.5, dotY + 2.5), new Point(cx + 3.5, dotY - 2.5));
                }
            }

            // ── Step label below dot ────────────────────────────────────
            if (StepNames != null && i < StepNames.Count)
            {
                var labelColor = isCurrent ? textColor : (isCompleted ? completedColor : lightTextColor);
                var weight = isCurrent ? FontWeight.Bold : FontWeight.Regular;
                var labelText = new FormattedText(
                    StepNames[i],
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Google Sans Flex", FontStyle.Normal, weight),
                    isCurrent ? 10.5 : 9.5,
                    new SolidColorBrush(labelColor));

                context.DrawText(labelText, new Point(cx - labelText.Width / 2, dotY + 14));
            }
        }
    }
}
