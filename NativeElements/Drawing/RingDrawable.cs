using NativeElements.Models;
using Microsoft.Maui.Graphics;

namespace NativeElements.Drawing;

/// <summary>
/// Renders the ring segment as a full cutting guide:
/// raw board rectangle (red waste hatching) with the final segment
/// (wood fill) overlaid inside it, plus all cutting annotations.
/// Matches the "SEGMENTED RING – ONE SEGMENT (CUTTING GUIDE)" diagram style.
/// </summary>
public class RingDrawable : IDrawable
{
    public SegmentedRingOutput? RingData { get; set; }
    private const int ArcSteps = 80;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);
        if (RingData == null) return;

        var d    = RingData;
        double alpha = d.MiterAngle * Math.PI / 180.0;
        double sinA  = Math.Sin(alpha);
        double cosA  = Math.Cos(alpha);
        double R_o   = d.OuterRadius;          // cm
        double R_i   = d.InnerRadius;          // cm
        double Lo    = 2.0 * R_o * sinA;       // board width  = outer chord
        double W     = R_o - R_i;              // board height = radial thickness

        // Padding around the board for annotation labels
        const float padTop    = 72f;
        const float padBottom = 44f;
        const float padLeft   = 96f;
        const float padRight  = 160f;

        float availW = dirtyRect.Width  - padLeft - padRight;
        float availH = dirtyRect.Height - padTop  - padBottom;
        if (availW <= 10 || availH <= 10) return;

        float s      = (float)Math.Min(availW / Lo, availH / W);   // px / cm
        float bx     = padLeft;
        float by     = padTop;
        float boardW = (float)Lo * s;
        float boardH = (float)W  * s;
        float cx     = bx + boardW / 2f;
        // Ring centre below board top (used for inner arc)
        float ringCy = by + (float)R_o * s * (float)cosA;

        DrawGrid(canvas, dirtyRect, s);
        DrawBoardWaste(canvas, bx, by, boardW, boardH);
        DrawSegmentFill(canvas, cx, ringCy, by, (float)R_o * s, (float)R_i * s, alpha);
        DrawBoardOutline(canvas, bx, by, boardW, boardH);
        DrawSegmentOutline(canvas, cx, ringCy, by, (float)R_o * s, (float)R_i * s, alpha);
        DrawAnnotations(canvas, bx, by, boardW, boardH, cx, s, alpha, sinA, cosA);
    }

    // ── Segment path ──────────────────────────────────────────────────────────

    private static PathF BuildSegmentPath(float cx, float ringCy, float boardTopY,
        float roPx, float riPx, double alpha)
    {
        double cosA = Math.Cos(alpha);
        var path = new PathF();

        // Outer arc: concave DOWN (bows into board from top)
        // y = boardTopY + R_o*(cos(t) − cos(α))   →  0 at ends, sagitta_o at centre
        for (int i = 0; i <= ArcSteps; i++)
        {
            double t  = -alpha + i * 2.0 * alpha / ArcSteps;
            float  px = cx + roPx * (float)Math.Sin(t);
            float  py = boardTopY + roPx * (float)(Math.Cos(t) - cosA);
            if (i == 0) path.MoveTo(px, py); else path.LineTo(px, py);
        }

        // Right miter line is implicit (LineTo start of inner arc)
        // Inner arc: bows UP (natural ring formula)
        // y = ringCy − R_i*cos(t),  t sweeps +α → −α
        for (int i = 0; i <= ArcSteps; i++)
        {
            double t  = alpha - i * 2.0 * alpha / ArcSteps;
            float  px = cx + riPx * (float)Math.Sin(t);
            float  py = ringCy - riPx * (float)Math.Cos(t);
            path.LineTo(px, py);
        }

        path.Close();
        return path;
    }

    // ── Draw layers ───────────────────────────────────────────────────────────

    private static void DrawBoardWaste(ICanvas canvas, float bx, float by, float boardW, float boardH)
    {
        canvas.FillColor = Color.FromArgb("#E8624A");
        canvas.FillRectangle(bx, by, boardW, boardH);

        // Diagonal hatching (visible only in waste areas after segment covers centre)
        canvas.StrokeColor = Color.FromArgb("#C43620");
        canvas.StrokeSize  = 0.8f;
        canvas.StrokeDashPattern = null;
        float step = 10f;
        for (float d = -boardH; d <= boardW + boardH; d += step)
        {
            float x1 = bx + d, y1 = by;
            float x2 = bx + d + boardH, y2 = by + boardH;
            if (x1 < bx)          { y1 += bx - x1;              x1 = bx;          }
            if (x2 > bx + boardW) { y2 -= x2 - (bx + boardW);   x2 = bx + boardW; }
            if (x1 <= x2 && y1 <= y2) canvas.DrawLine(x1, y1, x2, y2);
        }
    }

    private static void DrawSegmentFill(ICanvas canvas, float cx, float ringCy, float boardTopY,
        float roPx, float riPx, double alpha)
    {
        canvas.FillColor = Color.FromArgb("#D4A96A");
        canvas.FillPath(BuildSegmentPath(cx, ringCy, boardTopY, roPx, riPx, alpha));
    }

    private static void DrawBoardOutline(ICanvas canvas, float bx, float by, float boardW, float boardH)
    {
        canvas.StrokeColor    = Colors.Black;
        canvas.StrokeSize     = 1f;
        canvas.StrokeDashPattern = new float[] { 5, 3 };
        canvas.DrawRectangle(bx, by, boardW, boardH);
        canvas.StrokeDashPattern = null;
    }

    private static void DrawSegmentOutline(ICanvas canvas, float cx, float ringCy, float boardTopY,
        float roPx, float riPx, double alpha)
    {
        canvas.StrokeColor    = Colors.Black;
        canvas.StrokeSize     = 1.8f;
        canvas.StrokeDashPattern = null;
        canvas.DrawPath(BuildSegmentPath(cx, ringCy, boardTopY, roPx, riPx, alpha));
    }

    // ── Annotations ───────────────────────────────────────────────────────────

    private void DrawAnnotations(ICanvas canvas, float bx, float by, float boardW, float boardH,
        float cx, float s, double alpha, double sinA, double cosA)
    {
        var d    = RingData!;
        float roPx = (float)d.OuterRadius * s;
        float riPx = (float)d.InnerRadius * s;
        int   n    = (int)Math.Round(360.0 / d.SegmentAngle);

        float boardRight  = bx + boardW;
        float boardBottom = by + boardH;
        float sagOPx      = roPx * (float)(1.0 - cosA);
        float sagIPx      = riPx * (float)(1.0 - cosA);
        float outerArcMidY = by + sagOPx;                       // deepest point of outer arc
        float innerLeftX  = cx - riPx * (float)sinA;
        float innerRightX = cx + riPx * (float)sinA;
        float innerY      = by + (float)d.RadialEdgeLength * s * (float)cosA;  // inner chord Y
        float innerArcMidY = innerY - sagIPx;                   // highest point of inner arc
        float ringCy      = by + roPx * (float)cosA;

        // ── Title ──────────────────────────────────────────────────────────────
        canvas.FontSize  = 12f;
        canvas.FontColor = Colors.Black;
        canvas.DrawString("SEGMENTED RING – ONE SEGMENT (CUTTING GUIDE)",
            cx, by - 56f, HorizontalAlignment.Center);

        canvas.FontSize  = 9f;
        canvas.FontColor = Color.FromArgb("#555555");
        canvas.DrawString("All measurements shown on one rectangular piece of wood",
            cx, by - 43f, HorizontalAlignment.Center);

        // Legend swatches
        float legY = by - 26f;
        DrawSwatch(canvas, cx - 90f, legY, "#D4A96A", "Wood to keep (final segment)");
        DrawSwatch(canvas, cx + 20f, legY, "#E8624A", "Wood to cut out (waste)");

        // ── Board Length above board ────────────────────────────────────────────
        float blY = by - 10f;
        canvas.StrokeColor = Colors.Black; canvas.StrokeSize = 0.8f; canvas.StrokeDashPattern = null;
        canvas.DrawLine(bx, blY, boardRight, blY);
        canvas.DrawLine(bx,         blY - 3, bx,         by);
        canvas.DrawLine(boardRight, blY - 3, boardRight, by);
        canvas.FontSize = 9f; canvas.FontColor = Colors.Black;
        canvas.DrawString($"BOARD LENGTH   {d.OuterEdgeLength:F2} cm",
            cx, blY - 2f, HorizontalAlignment.Center);

        // ── Board Width on right ────────────────────────────────────────────────
        float bwX = boardRight + 12f;
        canvas.StrokeColor = Color.FromArgb("#444444"); canvas.StrokeSize = 0.8f;
        canvas.DrawLine(bwX, by, bwX, boardBottom);
        canvas.DrawLine(boardRight, by,          bwX + 4, by);
        canvas.DrawLine(boardRight, boardBottom, bwX + 4, boardBottom);
        canvas.FontSize = 9f; canvas.FontColor = Color.FromArgb("#444444");
        float bwMid = (by + boardBottom) / 2f;
        canvas.DrawString("BOARD WIDTH",   bwX + 6f, bwMid - 10f, HorizontalAlignment.Left);
        canvas.DrawString("(THICKNESS)",   bwX + 6f, bwMid,       HorizontalAlignment.Left);
        canvas.DrawString($"{d.RadialEdgeLength:F2} cm", bwX + 6f, bwMid + 10f, HorizontalAlignment.Left);

        // ── Top Edge inside segment ────────────────────────────────────────────
        float teY = outerArcMidY + 16f;
        canvas.StrokeColor = Color.FromArgb("#8B5E1E"); canvas.StrokeSize = 0.7f;
        canvas.StrokeDashPattern = new float[] { 3, 2 };
        canvas.DrawLine(bx,         teY, boardRight, teY);
        canvas.DrawLine(bx,         outerArcMidY, bx,         teY);
        canvas.DrawLine(boardRight, outerArcMidY, boardRight, teY);
        canvas.StrokeDashPattern = null;
        canvas.FontSize = 9f; canvas.FontColor = Color.FromArgb("#8B5E1E");
        canvas.DrawString($"TOP EDGE (OUTER LENGTH)   {d.OuterEdgeLength:F2} cm",
            cx, teY + 12f, HorizontalAlignment.Center);

        // ── Bottom Edge inside segment ─────────────────────────────────────────
        float beY = innerArcMidY - 16f;
        canvas.StrokeColor = Color.FromArgb("#8B5E1E"); canvas.StrokeSize = 0.7f;
        canvas.StrokeDashPattern = new float[] { 3, 2 };
        canvas.DrawLine(innerLeftX,  beY, innerRightX, beY);
        canvas.DrawLine(innerLeftX,  beY, innerLeftX,  innerArcMidY);
        canvas.DrawLine(innerRightX, beY, innerRightX, innerArcMidY);
        canvas.StrokeDashPattern = null;
        canvas.FontSize = 9f; canvas.FontColor = Color.FromArgb("#8B5E1E");
        canvas.DrawString($"BOTTOM EDGE (INNER LENGTH)   {d.InnerEdgeLength:F2} cm",
            cx, beY - 3f, HorizontalAlignment.Center);

        // ── Angle labels ──────────────────────────────────────────────────────
        float midSection = (by + innerY) / 2f;

        // Left outside
        canvas.FontSize = 8f; canvas.FontColor = Color.FromArgb("#1A1A8C");
        canvas.DrawString("ANGLE TO CUT",                    bx - 92f, midSection - 16f, HorizontalAlignment.Left);
        canvas.DrawString("Set saw to",                      bx - 92f, midSection - 5f,  HorizontalAlignment.Left);
        canvas.FontSize = 11f;
        canvas.DrawString($"{d.MiterAngle:F0}°",             bx - 92f, midSection + 6f,  HorizontalAlignment.Left);
        canvas.FontSize = 8f;
        canvas.DrawString("Cut along this angle",            bx - 92f, midSection + 17f, HorizontalAlignment.Left);

        // Right outside
        float raX = boardRight + bwX - boardRight + 6f + 60f;  // after board width label
        canvas.DrawString("ANGLE TO CUT",                    boardRight + 70f, midSection - 16f, HorizontalAlignment.Left);
        canvas.DrawString("Set saw to",                      boardRight + 70f, midSection - 5f,  HorizontalAlignment.Left);
        canvas.FontSize = 11f;
        canvas.DrawString($"{d.MiterAngle:F0}°",             boardRight + 70f, midSection + 6f,  HorizontalAlignment.Left);
        canvas.FontSize = 8f;
        canvas.DrawString("Cut along this angle",            boardRight + 70f, midSection + 17f, HorizontalAlignment.Left);

        // Inside corners (in the waste triangles)
        canvas.FontSize = 9f; canvas.FontColor = Color.FromArgb("#1A1A8C");
        float lCornerX = (bx + innerLeftX) / 2f;
        float lCornerY = (by + innerY) / 2f;
        canvas.DrawString($"ANGLE TO CUT",  lCornerX, lCornerY - 10f, HorizontalAlignment.Center);
        canvas.DrawString($"{d.MiterAngle:F0}°",       lCornerX, lCornerY,      HorizontalAlignment.Center);
        float rCornerX = (boardRight + innerRightX) / 2f;
        canvas.DrawString($"ANGLE TO CUT",  rCornerX, lCornerY - 10f, HorizontalAlignment.Center);
        canvas.DrawString($"{d.MiterAngle:F0}°",       rCornerX, lCornerY,      HorizontalAlignment.Center);

        // ── Curve to cut (right side) ──────────────────────────────────────────
        float crvX = boardRight + 12f + 52f;
        canvas.FontSize = 8f; canvas.FontColor = Color.FromArgb("#AA0000");
        canvas.DrawString("CURVE TO CUT",   crvX, outerArcMidY - 6f, HorizontalAlignment.Left);
        canvas.DrawString("(OUTER ARC)",    crvX, outerArcMidY + 4f,  HorizontalAlignment.Left);
        canvas.DrawString("Waste to remove",crvX, outerArcMidY + 14f, HorizontalAlignment.Left);

        canvas.DrawString("CURVE TO CUT",   crvX, innerY + 2f,  HorizontalAlignment.Left);
        canvas.DrawString("(INNER ARC)",    crvX, innerY + 12f, HorizontalAlignment.Left);
        canvas.DrawString("Waste to remove",crvX, innerY + 22f, HorizontalAlignment.Left);

        // ── Scale bar ──────────────────────────────────────────────────────────
        float sbY = boardBottom + 24f;
        canvas.StrokeColor = Colors.Gray; canvas.StrokeSize = 0.8f; canvas.StrokeDashPattern = null;
        canvas.DrawLine(bx, sbY, bx + s, sbY);
        canvas.DrawLine(bx,     sbY - 3, bx,     sbY + 3);
        canvas.DrawLine(bx + s, sbY - 3, bx + s, sbY + 3);
        canvas.FontSize = 8f; canvas.FontColor = Colors.Gray;
        canvas.DrawString("| 1 cm |", bx + s / 2f, sbY + 10f, HorizontalAlignment.Center);

        // Piece count
        canvas.FontSize = 10f; canvas.FontColor = Colors.Black;
        canvas.DrawString($"{n} pieces total  ·  Miter angle θ = {d.MiterAngle:F1}°",
            cx, boardBottom + 12f, HorizontalAlignment.Center);
    }

    private static void DrawSwatch(ICanvas canvas, float x, float y, string hex, string label)
    {
        canvas.FillColor = Color.FromArgb(hex);
        canvas.FillRectangle(x, y - 7, 14, 10);
        canvas.StrokeColor = Colors.Black; canvas.StrokeSize = 0.5f;
        canvas.DrawRectangle(x, y - 7, 14, 10);
        canvas.FontSize  = 8f;
        canvas.FontColor = Colors.Black;
        canvas.DrawString(label, x + 18f, y, HorizontalAlignment.Left);
    }

    private static void DrawGrid(ICanvas canvas, RectF dirtyRect, float pixelsPerCm)
    {
        canvas.StrokeColor    = Color.FromArgb("#EBEBEB");
        canvas.StrokeSize     = 0.4f;
        canvas.StrokeDashPattern = null;
        for (float x = 0; x < dirtyRect.Width;  x += pixelsPerCm)
            canvas.DrawLine(x, 0, x, dirtyRect.Height);
        for (float y = 0; y < dirtyRect.Height; y += pixelsPerCm)
            canvas.DrawLine(0, y, dirtyRect.Width, y);
    }
}

