using NativeElements.Models;
using Microsoft.Maui.Graphics;

namespace NativeElements.Drawing;

public class PetalDrawable : IDrawable
{
    public PetalOutput? PetalData { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (PetalData == null || PetalData.CurvePoints.Count == 0)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);
            return;
        }

        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        float centerX = dirtyRect.Width / 2;
        float centerY = dirtyRect.Height / 2;
        float scale = (float)PetalData.PixelsPerCm;

        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 2;

        var path = new PathF();

        // Start at first point (top of petal)
        var firstPoint = PetalData.CurvePoints[0];
        path.MoveTo(
            centerX + (float)firstPoint.X * scale,
            centerY + (float)firstPoint.Y * scale
        );

        // Trace right side
        for (int i = 1; i < PetalData.CurvePoints.Count; i++)
        {
            var point = PetalData.CurvePoints[i];
            path.LineTo(
                centerX + (float)point.X * scale,
                centerY + (float)point.Y * scale
            );
        }

        // Trace left side (mirrored)
        for (int i = PetalData.CurvePoints.Count - 1; i >= 0; i--)
        {
            var point = PetalData.CurvePoints[i];
            path.LineTo(
                centerX - (float)point.X * scale,
                centerY + (float)point.Y * scale
            );
        }

        path.Close();
        canvas.DrawPath(path);

        // Draw grid
        DrawGrid(canvas, dirtyRect, scale);
    }

    private void DrawGrid(ICanvas canvas, RectF dirtyRect, float pixelsPerCm)
    {
        canvas.StrokeColor = Color.FromArgb("#CCCCCC");
        canvas.StrokeSize = 0.5f;

        float gridSpacing = pixelsPerCm;

        // Vertical lines
        for (float x = 0; x < dirtyRect.Width; x += gridSpacing)
        {
            canvas.DrawLine(x, 0, x, dirtyRect.Height);
        }

        // Horizontal lines
        for (float y = 0; y < dirtyRect.Height; y += gridSpacing)
        {
            canvas.DrawLine(0, y, dirtyRect.Width, y);
        }
    }
}
