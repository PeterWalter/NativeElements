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

        // Calculate petal bounds
        double maxWidth = petalData.PetalWidth / 2;
        double maxHeight = petalData.PetalHeight;

        float petalWidthPx = (float)maxWidth * (float)petalData.PixelsPerCm;
        float petalHeightPx = (float)maxHeight * (float)petalData.PixelsPerCm;

        // Calculate padding (2cm on each side)
        float padding = (float)(2 * petalData.PixelsPerCm);
        float availableWidth = canvasWidth - 2 * padding;
        float availableHeight = canvasHeight - 2 * padding;

        // Fit petal to available space
        float scaleX = availableWidth / petalWidthPx;
        float scaleY = availableHeight / petalHeightPx;
        float fitScale = Math.Min(scaleX, scaleY);

        // Center the petal
        float centerX = canvasWidth / 2f;
        float startY = padding + (availableHeight - petalHeightPx * fitScale) / 2;

        if (petalData.CurvePoints.Count > 0)
        {
            var firstPoint = petalData.CurvePoints[0];
            path.MoveTo(
                centerX + (float)firstPoint.X * (float)petalData.PixelsPerCm * fitScale,
                startY + (float)firstPoint.Y * (float)petalData.PixelsPerCm * fitScale
            );

            // Trace right side
            for (int i = 1; i < petalData.CurvePoints.Count; i++)
            {
                var point = petalData.CurvePoints[i];
                path.LineTo(
                    centerX + (float)point.X * (float)petalData.PixelsPerCm * fitScale,
                    startY + (float)point.Y * (float)petalData.PixelsPerCm * fitScale
                );
            }

            // Trace left side (mirror)
            for (int i = petalData.CurvePoints.Count - 1; i >= 0; i--)
            {
                var point = petalData.CurvePoints[i];
                path.LineTo(
                    centerX - (float)point.X * (float)petalData.PixelsPerCm * fitScale,
                    startY + (float)point.Y * (float)petalData.PixelsPerCm * fitScale
                );
            }

            path.Close();
        }

        canvas.DrawPath(path, paint);

        // Draw seam allowance lines
        DrawSeamAllowance(canvas, petalData, centerX, startY, fitScale);

        // Draw center lines with dimensions
        DrawPetalDimensions(canvas, petalData, centerX, startY, fitScale);
    }

    private static void DrawSeamAllowance(SKCanvas canvas, PetalOutput petalData, float centerX, float startY, float fitScale)
    {
        if (petalData.SeamAllowance <= 0)
            return;

        var seamPaint = new SKPaint
        {
            Color = SKColor.Parse("#FFA500"),  // Orange
            StrokeWidth = 1,
            IsStroke = true,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new[] { 4f, 4f }, 0)
        };

        float scale = (float)petalData.PixelsPerCm * fitScale;
        float petalWidthPx = (float)petalData.PetalWidth * scale;
        float petalHeightPx = (float)petalData.PetalHeight * scale;
        float halfWidthPx = petalWidthPx / 2;
        float seamAllowancePx = (float)petalData.SeamAllowance * scale;

        // Top seam allowance line
        float topSeamY = startY + seamAllowancePx;
        canvas.DrawLine(centerX - halfWidthPx, topSeamY, centerX + halfWidthPx, topSeamY, seamPaint);

        // Bottom seam allowance line
        float bottomSeamY = startY + petalHeightPx - seamAllowancePx;
        canvas.DrawLine(centerX - halfWidthPx, bottomSeamY, centerX + halfWidthPx, bottomSeamY, seamPaint);

        // Seam allowance text label
        var textPaint = new SKPaint
        {
            Color = SKColor.Parse("#FFA500"),
            TextSize = 20,
            IsAntialias = true
        };

        string seamText = $"SA: {petalData.SeamAllowance:F1}cm";
        canvas.DrawText(seamText, centerX - halfWidthPx - 60, topSeamY + 5, textPaint);
    }

    private static void DrawPetalDimensions(SKCanvas canvas, PetalOutput petalData, float centerX, float startY, float fitScale)
    {
        var redPaint = new SKPaint
        {
            Color = SKColors.Red,
            StrokeWidth = 1,
            IsStroke = true,
            IsAntialias = true
        };

        var textPaint = new SKPaint
        {
            Color = SKColors.Red,
            TextSize = 24,
            IsAntialias = true
        };

        float scale = (float)petalData.PixelsPerCm * fitScale;
        float petalWidthPx = (float)petalData.PetalWidth * scale;  // Full width
        float petalHeightPx = (float)petalData.PetalHeight * scale;
        float halfWidthPx = petalWidthPx / 2;  // Half width for positioning

        // Vertical center line
        canvas.DrawLine(centerX, startY, centerX, startY + petalHeightPx, redPaint);

        // Horizontal center line (at vertical midpoint)
        float midY = startY + petalHeightPx / 2;
        canvas.DrawLine(centerX - halfWidthPx, midY, centerX + halfWidthPx, midY, redPaint);

        // Dimension labels
        string heightText = $"H: {petalData.PetalHeight:F1}cm";
        string widthText = $"W: {petalData.PetalWidth:F1}cm";

        canvas.DrawText(heightText, centerX + 20, midY + 10, textPaint);
        canvas.DrawText(widthText, centerX - 40, startY - 10, textPaint);
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
