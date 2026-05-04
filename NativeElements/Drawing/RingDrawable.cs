using NativeElements.Models;
using Microsoft.Maui.Graphics;

namespace NativeElements.Drawing;

public class RingDrawable : IDrawable
{
    public SegmentedRingOutput? RingData { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (RingData == null)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);
            return;
        }

        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        float centerX = dirtyRect.Width / 2;
        float centerY = dirtyRect.Height / 2;

        // Recover radii from chord lengths
        float segmentAngle = (float)RingData.SegmentAngle;
        double angleRad = Math.PI * segmentAngle / 180.0;
        double sinHalf = Math.Sin(angleRad / 2.0);

        double outerRadiusCm = RingData.OuterEdgeLength / (2.0 * sinHalf);
        double innerRadiusCm = RingData.InnerEdgeLength / (2.0 * sinHalf);

        // Fit to canvas
        float maxRadiusPx = Math.Min(dirtyRect.Width, dirtyRect.Height) * 0.42f;
        float outerRadius = maxRadiusPx;
        float innerRadius = (float)(innerRadiusCm / outerRadiusCm) * outerRadius;

        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 2;

        int segmentCount = Math.Max(3, (int)Math.Round(360.0 / segmentAngle));
        for (int i = 0; i < segmentCount; i++)
        {
            float startAngle = i * segmentAngle;
            DrawSegment(canvas, centerX, centerY, outerRadius, innerRadius, startAngle, segmentAngle);
        }

        // Draw grid
        DrawGrid(canvas, dirtyRect, (float)RingData.PixelsPerCm);
    }

    private void DrawSegment(ICanvas canvas, float centerX, float centerY, float outerRadius, float innerRadius, float startAngle, float angleSpan)
    {
        var path = new PathF();

        float startRad = (startAngle - 90) * (float)Math.PI / 180;
        float endRad = (startAngle + angleSpan - 90) * (float)Math.PI / 180;

        // Outer arc start
        float x1 = centerX + outerRadius * (float)Math.Cos(startRad);
        float y1 = centerY + outerRadius * (float)Math.Sin(startRad);

        // Outer arc end
        float x2 = centerX + outerRadius * (float)Math.Cos(endRad);
        float y2 = centerY + outerRadius * (float)Math.Sin(endRad);

        // Inner arc start
        float x3 = centerX + innerRadius * (float)Math.Cos(endRad);
        float y3 = centerY + innerRadius * (float)Math.Sin(endRad);

        // Inner arc end
        float x4 = centerX + innerRadius * (float)Math.Cos(startRad);
        float y4 = centerY + innerRadius * (float)Math.Sin(startRad);

        path.MoveTo(x1, y1);
        
        // Draw outer arc by approximating with line segments
        int steps = 30;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float angle = startRad + t * (endRad - startRad);
            float x = centerX + outerRadius * (float)Math.Cos(angle);
            float y = centerY + outerRadius * (float)Math.Sin(angle);
            path.LineTo(x, y);
        }
        
        path.LineTo(x3, y3);
        
        // Draw inner arc back
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float angle = endRad - t * (endRad - startRad);
            float x = centerX + innerRadius * (float)Math.Cos(angle);
            float y = centerY + innerRadius * (float)Math.Sin(angle);
            path.LineTo(x, y);
        }
        
        path.Close();
        canvas.DrawPath(path);
    }

    private void DrawGrid(ICanvas canvas, RectF dirtyRect, float pixelsPerCm)
    {
        canvas.StrokeColor = Color.FromArgb("#CCCCCC");
        canvas.StrokeSize = 0.5f;

        float gridSpacing = pixelsPerCm;

        for (float x = 0; x < dirtyRect.Width; x += gridSpacing)
            canvas.DrawLine(x, 0, x, dirtyRect.Height);

        for (float y = 0; y < dirtyRect.Height; y += gridSpacing)
            canvas.DrawLine(0, y, dirtyRect.Width, y);
    }
}
