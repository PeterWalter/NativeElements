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

        // Title at top of page
        var titlePaint = new SKPaint { Color = SKColors.Black, TextSize = 32, IsAntialias = true };
        string title = $"Ø {petalData.SphereDiameter:F0} cm  ·  {petalData.NumberOfPetals} petals";
        canvas.DrawText(title, canvasWidth / 2f - titlePaint.MeasureText(title) / 2, 36, titlePaint);

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
        float baseScale = (float)petalData.PixelsPerCm;

        // Compute bounds from the outer (seam) curve so everything fits
        var outerPts = petalData.SeamCurvePoints.Count > 0
            ? petalData.SeamCurvePoints
            : petalData.CurvePoints;

        double maxX = 0, maxY = 0, minY = double.MaxValue;
        foreach (var p in outerPts)
        {
            maxX = Math.Max(maxX, p.X);
            maxY = Math.Max(maxY, p.Y);
            minY = Math.Min(minY, p.Y);
        }

        float outerWidthPx  = (float)(2 * maxX) * baseScale;
        float outerHeightPx = (float)(maxY - minY) * baseScale;

        float padding       = (float)(1.5 * baseScale); // 1.5 cm
        float availableW    = canvasWidth  - 2 * padding;
        float availableH    = canvasHeight - 2 * padding;
        float fitScale      = Math.Min(availableW / outerWidthPx, availableH / outerHeightPx);
        float s             = baseScale * fitScale;

        float centerX = canvasWidth  / 2f;
        float yOrigin = padding + (availableH - outerHeightPx * fitScale) / 2 - (float)minY * s;

        // Draw cut line (seam curve) — orange dashed
        if (petalData.SeamCurvePoints.Count > 0 && petalData.SeamAllowance > 0)
        {
            var seamPaint = new SKPaint
            {
                Color       = SKColor.Parse("#FFA500"),
                StrokeWidth = lineWidth * 0.8f,
                IsStroke    = true,
                IsAntialias = true,
                PathEffect  = SKPathEffect.CreateDash(new[] { 8f, 6f }, 0)
            };
            var seamPath = BuildClosedPetalPath(petalData.SeamCurvePoints, centerX, yOrigin, s);
            canvas.DrawPath(seamPath, seamPaint);

            var saTextPaint = new SKPaint { Color = SKColor.Parse("#FFA500"), TextSize = 22, IsAntialias = true };
            int mid = petalData.SeamCurvePoints.Count / 2;
            // Place SA label on LEFT side so it doesn't clash with L label on right
            float lx = centerX - (float)petalData.SeamCurvePoints[mid].X * s - 8;
            float ly = yOrigin  + (float)petalData.SeamCurvePoints[mid].Y * s;
            string saText = $"SA {petalData.SeamAllowance:F1}cm";
            canvas.DrawText(saText, lx - saTextPaint.MeasureText(saText), ly, saTextPaint);
        }

        // Draw sewing line — solid black
        var sewPaint = new SKPaint
        {
            Color       = SKColors.Black,
            StrokeWidth = lineWidth,
            IsStroke    = true,
            IsAntialias = true
        };
        var sewPath = BuildClosedPetalPath(petalData.CurvePoints, centerX, yOrigin, s);
        canvas.DrawPath(sewPath, sewPaint);

        // Draw dimension lines and labels
        DrawPetalDimensions(canvas, petalData, centerX, yOrigin, s);
    }

    private static SKPath BuildClosedPetalPath(
        List<(double X, double Y)> pts, float centerX, float yOrigin, float s)
    {
        var path = new SKPath();
        if (pts.Count == 0) return path;

        path.MoveTo(centerX + (float)pts[0].X * s, yOrigin + (float)pts[0].Y * s);
        for (int i = 1; i < pts.Count; i++)
            path.LineTo(centerX + (float)pts[i].X * s, yOrigin + (float)pts[i].Y * s);
        for (int i = pts.Count - 1; i >= 0; i--)
            path.LineTo(centerX - (float)pts[i].X * s, yOrigin + (float)pts[i].Y * s);
        path.Close();
        return path;
    }

    private static void DrawPetalDimensions(SKCanvas canvas, PetalOutput petalData, float centerX, float yOrigin, float s)
    {
        var redPaint = new SKPaint
        {
            Color       = SKColor.Parse("#CC0000"),
            StrokeWidth = 1.5f,
            IsStroke    = true,
            IsAntialias = true,
            PathEffect  = SKPathEffect.CreateDash(new[] { 6f, 4f }, 0)
        };
        var textPaint = new SKPaint
        {
            Color       = SKColor.Parse("#CC0000"),
            TextSize    = 24,
            IsAntialias = true
        };

        float topY    = yOrigin;
        float bottomY = yOrigin + (float)petalData.ArcLength * s;
        float midY    = (topY + bottomY) / 2;
        float halfW   = (float)(petalData.PetalWidth / 2) * s;

        // Vertical centre line (L)
        canvas.DrawLine(centerX, topY, centerX, bottomY, redPaint);

        // Horizontal line at widest point (W)
        canvas.DrawLine(centerX - halfW, midY, centerX + halfW, midY, redPaint);

        // "L = xx.x cm" — RIGHT side, above horizontal line
        canvas.DrawText($"L = {petalData.ArcLength:F1} cm", centerX + halfW + 10, midY - 8, textPaint);

        // "W = xx.x cm" — BELOW the horizontal line, centred (avoids clash with L)
        string wText = $"W = {petalData.PetalWidth:F1} cm";
        canvas.DrawText(wText, centerX - textPaint.MeasureText(wText) / 2, midY + 30, textPaint);
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
