using NativeElements.Models;
using Microsoft.Maui.Graphics;

namespace NativeElements.Drawing;

/// <summary>
/// Renders the complete ring assembly: all N segments arranged in a circle around the ring center.
/// Shows how individual segments fit together when glued miter-to-miter.
/// </summary>
public class RingAssemblyDrawable : IDrawable
{
    public SegmentedRingOutput? RingData { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);
        if (RingData == null) return;

        var d = RingData;
        int n = (int)Math.Round(360.0 / d.SegmentAngle);
        
        // Center of canvas
        float cx = dirtyRect.Center.X;
        float cy = dirtyRect.Center.Y;

        // Determine scaling to fit all segments on screen
        float availW = dirtyRect.Width * 0.8f;
        float availH = dirtyRect.Height * 0.7f;
        
        // Ring outer diameter determines scale
        float outerDiameterPx = (float)Math.Min(availW / (d.OuterRadius * 2),
                                               availH / (d.OuterRadius * 2));
        
        float roScale = (float)d.OuterRadius * outerDiameterPx;
        float riScale = (float)d.InnerRadius * outerDiameterPx;

        // Draw title
        canvas.FontSize = 14f;
        canvas.FontColor = Colors.Black;
        canvas.DrawString($"RING ASSEMBLY: {n} Segments Arranged in Circle",
            cx, dirtyRect.Top + 20f, HorizontalAlignment.Center);

        // Draw complete ring with all segments
        DrawRingAssembly(canvas, cx, cy, roScale, riScale, n, d.SegmentAngle);

        // Draw center point reference
        canvas.StrokeColor = Colors.Gray;
        canvas.StrokeSize = 0.5f;
        canvas.DrawLine(cx - 8f, cy, cx + 8f, cy);
        canvas.DrawLine(cx, cy - 8f, cx, cy + 8f);

        // Add dimension information at bottom
        DrawAssemblyInfo(canvas, cx, dirtyRect, d, n);
    }

    private static void DrawRingAssembly(ICanvas canvas, float cx, float cy, 
        float roScale, float riScale, int n, double segmentAngle)
    {
        double angleStep = segmentAngle * Math.PI / 180.0;  // radians between segment centers
        double halfAngle = angleStep / 2.0;                  // half-angle of miter

        // Draw all N segments
        for (int i = 0; i < n; i++)
        {
            double segmentCenterAngle = i * angleStep;  // angle from vertical (0°) going clockwise
            
            DrawSegmentInRing(canvas, cx, cy, roScale, riScale, 
                             segmentCenterAngle, halfAngle, i);
        }

        // Draw outer and inner circles for reference
        canvas.StrokeColor = Color.FromArgb("#CCCCCC");
        canvas.StrokeSize = 0.5f;
        canvas.DrawCircle(cx, cy, roScale);
        canvas.DrawCircle(cx, cy, riScale);
    }

    private static void DrawSegmentInRing(ICanvas canvas, float cx, float cy,
        float roScale, float riScale, double segmentCenterAngle, double halfAngle, int segmentIndex)
    {
        // Outer arc endpoints (at full outer radius)
        float outerLeftX = cx + roScale * (float)Math.Sin(segmentCenterAngle - halfAngle);
        float outerLeftY = cy - roScale * (float)Math.Cos(segmentCenterAngle - halfAngle);

        float outerRightX = cx + roScale * (float)Math.Sin(segmentCenterAngle + halfAngle);
        float outerRightY = cy - roScale * (float)Math.Cos(segmentCenterAngle + halfAngle);

        // Inner arc endpoints (at full inner radius)
        float innerLeftX = cx + riScale * (float)Math.Sin(segmentCenterAngle - halfAngle);
        float innerLeftY = cy - riScale * (float)Math.Cos(segmentCenterAngle - halfAngle);

        float innerRightX = cx + riScale * (float)Math.Sin(segmentCenterAngle + halfAngle);
        float innerRightY = cy - riScale * (float)Math.Cos(segmentCenterAngle + halfAngle);

        // For assembly view, show simplified trapezoid for each segment
        // (not the full arc detail, just the flat board piece shape)
        
        var path = new PathF();
        
        // Trapezoid vertices
        path.MoveTo(outerLeftX, outerLeftY);
        path.LineTo(outerRightX, outerRightY);   // outer edge (longer)
        path.LineTo(innerRightX, innerRightY);   // right miter cut
        path.LineTo(innerLeftX, innerLeftY);     // inner edge (shorter)
        path.Close();                            // left miter cut auto-added

        // Fill segment with alternating colors for clarity
        bool isEven = (segmentIndex % 2) == 0;
        canvas.FillColor = isEven 
            ? Color.FromArgb("#D4A96A")    // wood color (light tan)
            : Color.FromArgb("#C9934C");   // darker tan for contrast

        canvas.FillPath(path);

        // Draw segment outline
        canvas.StrokeColor = Color.FromArgb("#8B5E1E");
        canvas.StrokeSize = 0.6f;
        canvas.DrawPath(path);

        // Draw miter angle guides (dashed lines on edges)
        canvas.StrokeColor = Color.FromArgb("#FF6B35");
        canvas.StrokeSize = 0.4f;
        canvas.StrokeDashPattern = new float[] { 3, 2 };
        canvas.DrawLine(outerLeftX, outerLeftY, innerLeftX, innerLeftY);
        canvas.DrawLine(outerRightX, outerRightY, innerRightX, innerRightY);
        canvas.StrokeDashPattern = null;

        // Optional: Label segments for clarity (only if N is small)
        if (segmentIndex < 24)  // Only label if 24 segments or fewer
        {
            float midRadiusScale = (roScale + riScale) / 2f;
            float labelX = cx + midRadiusScale * (float)Math.Sin(segmentCenterAngle);
            float labelY = cy - midRadiusScale * (float)Math.Cos(segmentCenterAngle);

            canvas.FontSize = 7f;
            canvas.FontColor = Color.FromArgb("#333333");
            canvas.DrawString((segmentIndex + 1).ToString(), labelX, labelY, HorizontalAlignment.Center);
        }
    }

    private static void DrawAssemblyInfo(ICanvas canvas, float cx, RectF dirtyRect,
        SegmentedRingOutput data, int n)
    {
        float infoY = dirtyRect.Bottom - 50f;
        
        canvas.FontSize = 9f;
        canvas.FontColor = Colors.Black;
        
        string info = $"Ring: {n} segments × {data.SegmentAngle:F1}° = 360° complete circle";
        canvas.DrawString(info, cx, infoY, HorizontalAlignment.Center);

        canvas.FontSize = 8f;
        canvas.FontColor = Color.FromArgb("#666666");
        string details = $"Outer: {data.OuterEdgeLength:F2}cm  ·  Inner: {data.InnerEdgeLength:F2}cm  ·  Miter: {data.MiterAngle:F1}°";
        canvas.DrawString(details, cx, infoY + 12f, HorizontalAlignment.Center);

        string assembly = $"Glue miter-to-miter around ring center. Segments shown in alternating shades.";
        canvas.DrawString(assembly, cx, infoY + 22f, HorizontalAlignment.Center);
    }
}
