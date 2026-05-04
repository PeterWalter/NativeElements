using NativeElements.Models;
using SkiaSharp;

namespace NativeElements.Services;

public class RenderService
{
    private const float DPI_TO_PIXELS = 1.0f; // SkiaSharp handles scaling

    public static SKImage RenderPetal(PetalOutput petalData, int canvasWidth, int canvasHeight, bool showGrid = true)
    {
        using var surface = SKSurface.Create(new SKImageInfo(canvasWidth, canvasHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        if (showGrid)
        {
            DrawGrid(canvas, canvasWidth, canvasHeight, petalData.PixelsPerCm);
        }

        DrawPetalShape(canvas, petalData, canvasWidth, canvasHeight);

        return surface.Snapshot();
    }

    public static SKImage RenderSegmentedRing(SegmentedRingOutput ringData, int canvasWidth, int canvasHeight, bool showGrid = true)
    {
        using var surface = SKSurface.Create(new SKImageInfo(canvasWidth, canvasHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        if (showGrid)
        {
            DrawGrid(canvas, canvasWidth, canvasHeight, ringData.PixelsPerCm);
        }

        DrawSegmentedRing(canvas, ringData, canvasWidth, canvasHeight);

        return surface.Snapshot();
    }

    private static void DrawPetalShape(SKCanvas canvas, PetalOutput petalData, int canvasWidth, int canvasHeight, float lineWidth = 2)
    {
        var paint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = lineWidth,
            IsStroke = true,
            IsAntialias = true
        };

        var path = new SKPath();
        float centerX = canvasWidth / 2f;
        float centerY = canvasHeight / 2f;

        // Scale curve points to canvas - use pixels per cm for 1:1 scale
        float scaleX = (float)petalData.PixelsPerCm;
        float scaleY = (float)petalData.PixelsPerCm;

        if (petalData.CurvePoints.Count > 0)
        {
            // CurvePoints are Y-parameterized right-side seam:
            // X = half-width at that height, Y = height from 0 to arcLength
            // Create symmetric petal by:
            // 1. Trace top point (Y=0, X=0)
            // 2. Trace right side (X positive, Y increasing)
            // 3. Trace bottom point (Y=arcLength, X=0)
            // 4. Trace left side (X negative, Y decreasing)

            var firstPoint = petalData.CurvePoints[0];
            path.MoveTo(
                centerX + (float)firstPoint.X * scaleX,
                centerY + (float)firstPoint.Y * scaleY
            );

            // Trace right side (X=0 to X=max, Y from 0 to arcLength)
            for (int i = 1; i < petalData.CurvePoints.Count; i++)
            {
                var point = petalData.CurvePoints[i];
                path.LineTo(
                    centerX + (float)point.X * scaleX,
                    centerY + (float)point.Y * scaleY
                );
            }

            // Trace left side (mirror back: X=-max to X=0, Y from arcLength to 0)
            for (int i = petalData.CurvePoints.Count - 1; i >= 0; i--)
            {
                var point = petalData.CurvePoints[i];
                // Mirror X for left side
                path.LineTo(
                    centerX - (float)point.X * scaleX,
                    centerY + (float)point.Y * scaleY
                );
            }

            path.Close();
        }

        canvas.DrawPath(path, paint);
    }

    private static void DrawSegmentedRing(SKCanvas canvas, SegmentedRingOutput ringData, int canvasWidth, int canvasHeight, float lineWidth = 2)
    {
        var paint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = lineWidth,
            IsStroke = true,
            IsAntialias = true
        };

        float centerX = canvasWidth / 2f;
        float centerY = canvasHeight / 2f;

        // Recover radii in cm from edge chord lengths:
        // chord = 2 * R * sin(theta/2) => R = chord / (2 * sin(theta/2))
        float segmentAngle = (float)ringData.SegmentAngle;
        if (segmentAngle <= 0f || segmentAngle >= 360f)
        {
            return;
        }

        double angleRad = Math.PI * segmentAngle / 180.0;
        double sinHalf = Math.Sin(angleRad / 2.0);
        if (Math.Abs(sinHalf) < 1e-9)
        {
            return;
        }

        double outerRadiusCm = ringData.OuterEdgeLength / (2.0 * sinHalf);
        double innerRadiusCm = ringData.InnerEdgeLength / (2.0 * sinHalf);
        if (outerRadiusCm <= 0)
        {
            return;
        }
        if (innerRadiusCm <= 0 || innerRadiusCm >= outerRadiusCm)
        {
            innerRadiusCm = Math.Max(0.1, outerRadiusCm - Math.Max(0.1, ringData.RadialEdgeLength));
        }

        // Fit to canvas with padding
        float maxRadiusPx = Math.Max(10f, (Math.Min(canvasWidth, canvasHeight) * 0.42f));
        float outerRadius = maxRadiusPx;
        float innerRadius = Math.Max(1f, (float)(innerRadiusCm / outerRadiusCm) * outerRadius);

        // Draw each segment
        int segmentCount = Math.Max(3, (int)Math.Round(360.0 / segmentAngle));
        for (int i = 0; i < segmentCount; i++)
        {
            float startAngle = i * segmentAngle;
            DrawSegment(canvas, centerX, centerY, outerRadius, innerRadius, startAngle, segmentAngle, paint);
        }
    }

    private static void DrawSegment(SKCanvas canvas, float centerX, float centerY, float outerRadius, float innerRadius, float startAngle, float angleSpan, SKPaint paint)
    {
        var path = new SKPath();

        // Convert angles to radians
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
        path.ArcTo(new SKRect(centerX - outerRadius, centerY - outerRadius, centerX + outerRadius, centerY + outerRadius), startAngle, angleSpan, false);
        path.LineTo(x3, y3);
        path.ArcTo(new SKRect(centerX - innerRadius, centerY - innerRadius, centerX + innerRadius, centerY + innerRadius), startAngle + angleSpan, -angleSpan, false);
        path.Close();

        canvas.DrawPath(path, paint);
    }

    private static void DrawGrid(SKCanvas canvas, int canvasWidth, int canvasHeight, double pixelsPerCm)
    {
        var gridPaint = new SKPaint
        {
            Color = new SKColor(200, 200, 200, 100),
            StrokeWidth = 0.5f,
            IsStroke = true,
            IsAntialias = true
        };

        float gridSpacing = (float)pixelsPerCm;

        // Vertical lines
        for (float x = 0; x < canvasWidth; x += gridSpacing)
        {
            canvas.DrawLine(x, 0, x, canvasHeight, gridPaint);
        }

        // Horizontal lines
        for (float y = 0; y < canvasHeight; y += gridSpacing)
        {
            canvas.DrawLine(0, y, canvasWidth, y, gridPaint);
        }
    }
}
