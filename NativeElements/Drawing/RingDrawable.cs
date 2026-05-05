using NativeElements.Models;
using Microsoft.Maui.Graphics;

namespace NativeElements.Drawing;

/// <summary>
/// Renders ONE ring segment as it would appear on the cutting template:
/// outer arc at top (wider), inner arc at bottom (narrower), straight angled sides.
/// The ring centre is placed below the visible canvas so the entire segment
/// is shown without the empty "pie centre" space.
/// </summary>
public class RingDrawable : IDrawable
{
    public SegmentedRingOutput? RingData { get; set; }

    // Arc approximation quality
    private const int ArcSteps = 80;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        if (RingData == null) return;

        double R_o = RingData.OuterRadius;   // cm
        double R_i = RingData.InnerRadius;   // cm
        double halfAngleRad = RingData.MiterAngle * Math.PI / 180.0;
        double sinHalf = Math.Sin(halfAngleRad);
        double cosHalf = Math.Cos(halfAngleRad);

        // Segment bounding box in cm
        float segWidthCm  = (float)(2.0 * R_o * sinHalf);
        float segHeightCm = (float)(R_o - R_i * cosHalf);

        // Padding: title at top, Li label below, W label right
        float padTop    = 44f;
        float padBottom = 34f;
        float padLeft   = 36f;
        float padRight  = 120f;

        float availW = dirtyRect.Width  - padLeft - padRight;
        float availH = dirtyRect.Height - padTop  - padBottom;
        float s = Math.Min(availW / segWidthCm, availH / segHeightCm); // px / cm

        // Ring centre in screen coords (below the piece)
        float cx       = padLeft + availW / 2f;
        float cyRing   = padTop + (float)R_o * s;   // ring centre at bottom of outer arc

        DrawGrid(canvas, dirtyRect, s);
        DrawSegmentFill(canvas, cx, cyRing, (float)R_o * s, (float)R_i * s, halfAngleRad);
        DrawSegmentOutline(canvas, cx, cyRing, (float)R_o * s, (float)R_i * s, halfAngleRad);
        DrawDimensions(canvas, cx, cyRing, (float)R_o * s, (float)R_i * s, halfAngleRad, sinHalf, cosHalf, s);
    }

    // ── Path helper ───────────────────────────────────────────────────────────

    private static PathF BuildSegmentPath(float cx, float cyRing, float roP, float riP, double halfRad)
    {
        var path = new PathF();

        // Outer arc: left → right (through topmost outer point)
        for (int i = 0; i <= ArcSteps; i++)
        {
            double t = i / (double)ArcSteps;
            double a = -halfRad + t * 2 * halfRad;  // -α … +α
            float px = cx      + roP * (float)Math.Sin(a);
            float py = cyRing  - roP * (float)Math.Cos(a);
            if (i == 0) path.MoveTo(px, py); else path.LineTo(px, py);
        }

        // Inner arc: right → left (through topmost inner point)
        for (int i = 0; i <= ArcSteps; i++)
        {
            double t = i / (double)ArcSteps;
            double a = halfRad - t * 2 * halfRad;   // +α … -α
            float px = cx     + riP * (float)Math.Sin(a);
            float py = cyRing - riP * (float)Math.Cos(a);
            path.LineTo(px, py);
        }

        path.Close();
        return path;
    }

    private static void DrawSegmentFill(ICanvas canvas, float cx, float cyRing, float roP, float riP, double halfRad)
    {
        var path = BuildSegmentPath(cx, cyRing, roP, riP, halfRad);
        canvas.FillColor = Color.FromArgb("#D4A96A"); // warm wood tone
        canvas.FillPath(path);
    }

    private static void DrawSegmentOutline(ICanvas canvas, float cx, float cyRing, float roP, float riP, double halfRad)
    {
        var path = BuildSegmentPath(cx, cyRing, roP, riP, halfRad);
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize  = 2f;
        canvas.StrokeDashPattern = null;
        canvas.DrawPath(path);
    }

    // ── Dimension annotations ─────────────────────────────────────────────────

    private void DrawDimensions(ICanvas canvas, float cx, float cyRing, float roP, float riP,
        double halfRad, double sinHalf, double cosHalf, float s)
    {
        var data = RingData!;

        // Key points in screen coords
        float outerLeftX  = cx - roP * (float)sinHalf;
        float outerY      = cyRing - roP * (float)cosHalf;  // Y of outer corners
        float outerTopY   = cyRing - roP;                   // topmost outer point
        float innerLeftX  = cx - riP * (float)sinHalf;
        float innerRightX = cx + riP * (float)sinHalf;
        float innerY      = cyRing - riP * (float)cosHalf;  // Y of inner corners (bottommost)
        float outerRightX = cx + roP * (float)sinHalf;

        canvas.FontSize  = 12f;

        // ─ Lo (outer chord): horizontal arrow above the outer arc ─
        float loArrowY = outerTopY - 18f;
        DrawHorizontalDim(canvas, outerLeftX, outerRightX, loArrowY,
            $"Lo = {data.OuterEdgeLength:F2} cm", Color.FromArgb("#CC0000"), tickBot: outerY);

        // ─ Li (inner chord): horizontal arrow below the inner arc ─
        float liArrowY = innerY + 18f;
        DrawHorizontalDim(canvas, innerLeftX, innerRightX, liArrowY,
            $"Li = {data.InnerEdgeLength:F2} cm", Color.FromArgb("#CC0000"), tickBot: innerY, labelsBelow: true);

        // ─ W (radial edge = wood width): vertical arrow on right side ─
        float wArrowX = outerRightX + 22f;
        DrawVerticalDim(canvas, wArrowX, outerY, innerY,
            $"W = {data.RadialEdgeLength:F2} cm", Color.FromArgb("#0055AA"),
            tickLeft: outerRightX);

        // ─ θ (miter angle) at the upper-left corner ─
        canvas.FontSize  = 13f;
        canvas.FontColor = Color.FromArgb("#336600");
        canvas.DrawString($"θ = {data.MiterAngle:F1}°",
            outerLeftX - 4f, outerY + 16f, HorizontalAlignment.Right);

        // ─ Title ─
        int n = (int)Math.Round(360.0 / data.SegmentAngle);
        canvas.FontSize  = 13f;
        canvas.FontColor = Colors.Black;
        canvas.DrawString($"Ring Segment  ·  {n} pieces  ·  θ = {data.MiterAngle:F1}°",
            cx, 14f, HorizontalAlignment.Center);
    }

    private static void DrawHorizontalDim(ICanvas canvas,
        float x1, float x2, float arrowY, string label, Color colour,
        float tickBot = 0, bool labelsBelow = false)
    {
        canvas.StrokeColor = colour;
        canvas.StrokeSize  = 1f;
        canvas.StrokeDashPattern = new float[] { 4, 3 };
        canvas.DrawLine(x1, arrowY, x2, arrowY);
        canvas.DrawLine(x1, arrowY - 4, x1, tickBot);
        canvas.DrawLine(x2, arrowY - 4, x2, tickBot);
        canvas.StrokeDashPattern = null;
        canvas.FontColor = colour;
        canvas.FontSize  = 11f;
        float cx = (x1 + x2) / 2f;
        if (labelsBelow) canvas.DrawString(label, cx, arrowY + 14f, HorizontalAlignment.Center);
        else             canvas.DrawString(label, cx, arrowY - 6f,  HorizontalAlignment.Center);
    }

    private static void DrawVerticalDim(ICanvas canvas,
        float arrowX, float y1, float y2, string label, Color colour, float tickLeft = 0)
    {
        canvas.StrokeColor = colour;
        canvas.StrokeSize  = 1f;
        canvas.StrokeDashPattern = new float[] { 4, 3 };
        canvas.DrawLine(arrowX, y1, arrowX, y2);
        canvas.DrawLine(tickLeft, y1, arrowX + 4, y1);
        canvas.DrawLine(tickLeft, y2, arrowX + 4, y2);
        canvas.StrokeDashPattern = null;
        canvas.FontColor = colour;
        canvas.FontSize  = 11f;
        canvas.DrawString(label, arrowX + 6f, (y1 + y2) / 2f, HorizontalAlignment.Left);
    }

    private static void DrawGrid(ICanvas canvas, RectF dirtyRect, float pixelsPerCm)
    {
        canvas.StrokeColor = Color.FromArgb("#E0E0E0");
        canvas.StrokeSize  = 0.5f;
        for (float x = 0; x < dirtyRect.Width;  x += pixelsPerCm)
            canvas.DrawLine(x, 0, x, dirtyRect.Height);
        for (float y = 0; y < dirtyRect.Height; y += pixelsPerCm)
            canvas.DrawLine(0, y, dirtyRect.Width, y);
    }
}

