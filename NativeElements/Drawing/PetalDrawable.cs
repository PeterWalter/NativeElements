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

        float baseScale = (float)PetalData.PixelsPerCm;

        // Compute bounds using the outer (seam) curve so all lines fit
        var outerPts = PetalData.SeamCurvePoints.Count > 0
            ? PetalData.SeamCurvePoints
            : PetalData.CurvePoints;

        double maxX = 0, maxY = 0, minY = double.MaxValue;
        foreach (var p in outerPts)
        {
            maxX = Math.Max(maxX, p.X);
            maxY = Math.Max(maxY, p.Y);
            minY = Math.Min(minY, p.Y);
        }
        // Full width is mirrored, so totalWidth = 2 * maxX
        float outerWidthPx  = (float)(2 * maxX) * baseScale;
        float outerHeightPx = (float)(maxY - minY) * baseScale;

        float padding = 40f; // Extra room for labels
        float availableWidth  = dirtyRect.Width  - 2 * padding;
        float availableHeight = dirtyRect.Height - 2 * padding;

        float fitScale = Math.Min(availableWidth / outerWidthPx, availableHeight / outerHeightPx);
        float s = baseScale * fitScale; // combined scale: 1 cm → s pixels

        float centerX = dirtyRect.Width / 2;
        // Offset for negative y values from the seam curve tip extension
        float yOrigin = padding + (availableHeight - outerHeightPx * fitScale) / 2 - (float)minY * s;

        // ── Draw grid ───────────────────────────────────────────────────
        DrawGrid(canvas, dirtyRect, s);

        // ── Draw outer cut line (seam allowance boundary) — orange dashed ─
        if (PetalData.SeamCurvePoints.Count > 0 && PetalData.SeamAllowance > 0)
        {
            canvas.StrokeColor  = Color.FromArgb("#FFA500");
            canvas.StrokeSize   = 1.5f;
            canvas.StrokeDashPattern = new float[] { 5, 4 };
            DrawClosedPetal(canvas, PetalData.SeamCurvePoints, centerX, yOrigin, s);
            canvas.StrokeDashPattern = null;
        }

        // ── Draw inner sewing line — solid black ─────────────────────────
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize  = 2f;
        DrawClosedPetal(canvas, PetalData.CurvePoints, centerX, yOrigin, s);

        // ── Dimension lines ───────────────────────────────────────────────
        DrawDimensionLines(canvas, centerX, yOrigin, s);

        // ── Seam allowance label ──────────────────────────────────────────
        if (PetalData.SeamAllowance > 0)
        {
            canvas.FontSize  = 11;
            canvas.FontColor = Color.FromArgb("#FFA500");
            // Label beside the widest outer point
            float outerWidestX = (float)(outerPts[50].X) * s;
            float outerWidestY = yOrigin + (float)(outerPts[50].Y) * s;
            canvas.DrawString($"SA {PetalData.SeamAllowance:F1}cm",
                centerX + outerWidestX + 5, outerWidestY, HorizontalAlignment.Left);
        }
    }

    // Draws right side then mirrored left side and closes
    private static void DrawClosedPetal(
        ICanvas canvas, List<(double X, double Y)> pts, float centerX, float yOrigin, float s)
    {
        if (pts.Count == 0) return;
        var path = new PathF();

        path.MoveTo(centerX + (float)pts[0].X * s, yOrigin + (float)pts[0].Y * s);
        for (int i = 1; i < pts.Count; i++)
            path.LineTo(centerX + (float)pts[i].X * s, yOrigin + (float)pts[i].Y * s);

        for (int i = pts.Count - 1; i >= 0; i--)
            path.LineTo(centerX - (float)pts[i].X * s, yOrigin + (float)pts[i].Y * s);

        path.Close();
        canvas.DrawPath(path);
    }

    private void DrawDimensionLines(ICanvas canvas, float centerX, float yOrigin, float s)
    {
        var data = PetalData!;
        float topY    = yOrigin + 0;                         // top tip (t=0, y=0)
        float bottomY = yOrigin + (float)data.ArcLength * s; // bottom tip
        float midY    = (topY + bottomY) / 2;
        float halfW   = (float)(data.PetalWidth / 2) * s;

        canvas.StrokeColor    = Color.FromArgb("#CC0000");
        canvas.StrokeSize     = 1f;
        canvas.StrokeDashPattern = new float[] { 4, 3 };

        // Vertical centre line — shows L (Length = arc length)
        canvas.DrawLine(centerX, topY, centerX, bottomY);

        // Horizontal centre line — shows W (Width) at widest point (midY)
        canvas.DrawLine(centerX - halfW, midY, centerX + halfW, midY);

        canvas.StrokeDashPattern = null;
        canvas.FontSize  = 12;
        canvas.FontColor = Color.FromArgb("#CC0000");

        // "L = xx.x cm"  on the right of the vertical line, near centre
        canvas.DrawString($"L = {data.ArcLength:F1} cm",
            centerX + halfW + 8, midY - 18, HorizontalAlignment.Left);

        // "W = xx.x cm"  at the centre width line, just above it
        canvas.DrawString($"W = {data.PetalWidth:F1} cm",
            centerX, midY - 16, HorizontalAlignment.Center);
    }

    private static void DrawGrid(ICanvas canvas, RectF dirtyRect, float pixelsPerCm)
    {
        canvas.StrokeColor = Color.FromArgb("#DDDDDD");
        canvas.StrokeSize  = 0.5f;

        for (float x = 0; x < dirtyRect.Width; x += pixelsPerCm)
            canvas.DrawLine(x, 0, x, dirtyRect.Height);

        for (float y = 0; y < dirtyRect.Height; y += pixelsPerCm)
            canvas.DrawLine(0, y, dirtyRect.Width, y);
    }
}

