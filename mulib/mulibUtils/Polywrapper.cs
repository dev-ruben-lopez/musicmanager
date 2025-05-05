using Avalonia.Controls;
using Avalonia.Skia;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

public class WrapAroundCanvas : Control
{
    private readonly List<SKPoint> originalPolygon = new()
    {
        new SKPoint(10, 90),
        new SKPoint(50, 90),
        new SKPoint(50, 200),
        new SKPoint(10, 200)
    };

    const float MinY = -180;
    const float MaxY = 180;
    const float WrapRange = MaxY - MinY;

    protected override void Render(DrawingContext context)
    {
        base.Render(context);

        var skia = context.GetSkiaSurface();
        if (skia is null)
            return;

        var canvas = skia.Surface.Canvas;
        canvas.Clear(SKColors.Black);

        // Set up paints
        var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.Yellow,
            StrokeWidth = 2,
            IsAntialias = true
        };

        var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.Yellow.WithAlpha(128), // 50% transparency
            IsAntialias = true
        };

        var polygons = SplitAndWrapPolygon(originalPolygon);

        foreach (var poly in polygons)
        {
            DrawPolygon(poly, canvas, fillPaint, strokePaint);
        }
    }

    private float WrapY(float y)
    {
        while (y > MaxY) y -= WrapRange;
        while (y < MinY) y += WrapRange;
        return y;
    }

    private List<List<SKPoint>> SplitAndWrapPolygon(List<SKPoint> points)
    {
        var visible = new List<SKPoint>();
        var wrapped = new List<SKPoint>();

        foreach (var pt in points)
        {
            if (pt.Y > MaxY)
            {
                visible.Add(new SKPoint(pt.X, MaxY));        // Clamp to max Y
                wrapped.Add(new SKPoint(pt.X, WrapY(pt.Y))); // Wrap to bottom
            }
            else if (pt.Y < MinY)
            {
                visible.Add(new SKPoint(pt.X, MinY));        // Clamp to min Y
                wrapped.Add(new SKPoint(pt.X, WrapY(pt.Y))); // Wrap to top
            }
            else
            {
                visible.Add(pt); // Within range
            }
        }

        var result = new List<List<SKPoint>> { visible };
        if (wrapped.Count > 0)
            result.Add(wrapped);
        return result;
    }

    private void DrawPolygon(List<SKPoint> points, SKCanvas canvas, SKPaint fill, SKPaint stroke)
    {
        if (points.Count < 3)
            return;

        var path = new SKPath();
        path.MoveTo(points[0]);
        foreach (var pt in points.Skip(1))
            path.LineTo(pt);
        path.Close();

        canvas.DrawPath(path, fill);
        canvas.DrawPath(path, stroke);
    }
}