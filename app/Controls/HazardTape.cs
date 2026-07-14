using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace fModLoader.Controls;

public class HazardTape : Control
{
    private double _offset;
    private readonly DispatcherTimer _timer;

    public HazardTape()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _timer.Tick += (_, _) =>
        {
            _offset = (_offset + 1) % 120;
            InvalidateVisual();
        };
        Height = 32;
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

        double stripeW = 60;
        var yellowBrush = new SolidColorBrush(Color.Parse("#f5c518"));
        var blackBrush = new SolidColorBrush(Color.Parse("#1a1a1a"));

        // Draw striped tape
        for (int i = -2; i < (w / stripeW) + 4; i++)
        {
            double x = i * stripeW - _offset;
            var pathGeometry = new StreamGeometry();
            using (var ctx = pathGeometry.Open())
            {
                ctx.BeginFigure(new Point(x, 0), true);
                ctx.LineTo(new Point(x + stripeW, 0));
                ctx.LineTo(new Point(x + stripeW - 20, h));
                ctx.LineTo(new Point(x - 20, h));
                ctx.EndFigure(true);
            }

            var brush = (i % 2 == 0) ? yellowBrush : blackBrush;
            context.DrawGeometry(brush, null, pathGeometry);
        }

        // Overlay text
        var typeface = new Typeface("Google Sans", FontStyle.Normal, FontWeight.Bold);
        var textBrush = Brushes.White;
        var textBrushBg = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));

        var formattedText = new FormattedText(
            "BETA / UNDER CONSTRUCTION",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            11,
            textBrush);

        // Draw text background pill in the center
        double textW = formattedText.Width;
        double textH = formattedText.Height;
        context.DrawRectangle(textBrushBg, null, new Rect((w - textW - 24) / 2, (h - textH - 8) / 2, textW + 24, textH + 8), 10, 10);
        context.DrawText(formattedText, new Point((w - textW) / 2, (h - textH) / 2));
    }
}
