using NativeElements.Models;
using Microsoft.Maui.Graphics;

namespace NativeElements.Drawing;

/// <summary>
/// Renders the ring segment cutting guide:
///   • The board rectangle (red + hatching = waste)
///   • The inner segment body (wood colour) with straight outer chord at board top,
///     miter-cut sides, and the inner arc bowing upward
///   • The outer arc drawn *above* the board top (it arches away from the ring centre,
///     creating the convex outer surface of the ring)
///   • Full cutting annotations
/// Both arcs use the same ring-centre formula  y = ringCy − R·cos(t),
/// so both face the ring centre (below the board).
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
        double R_o   = d.OuterRadius;
        double R_i   = d.InnerRadius;
        double Lo    = 2.0 * R_o * sinA;   // board width  = outer chord
        double W     = R_o - R_i;           // board height = radial thickness
        double sagO  = R_o * (1.0 - cosA); // outer arc protrudes above board by this amount

        // Fixed padding areas (excluding outer-arc protrusion which is scale-dependent)
        const float padTopBase = 66f;   // title + legend + board-length arrow
        const float padBottom  = 44f;
        const float padLeft    = 96f;
        const float padRight   = 160f;

        float availW = dirtyRect.Width  - padLeft  - padRight;
        float availH = dirtyRect.Height - padTopBase - padBottom;
        if (availW <= 10 || availH <= 10) return;

        // Scale: total drawing height = board(W) + outer arc protrusion(sagO)
        float s      = (float)Math.Min(availW / Lo, availH / (W + sagO));
        float boardW = (float)Lo   * s;
        float boardH = (float)W    * s;
        float sagOPx = (float)sagO * s;   // pixels the outer arc rises above board top

        // Board top Y is below the outer arc peak
        float bx = padLeft;
        float by = padTopBase + sagOPx;       // board rect top
        float cx = bx + boardW / 2f;
        float ringCy = by + (float)R_o * s * (float)cosA;  // ring centre (below board bottom)

        DrawGrid(canvas, dirtyRect, s);
        DrawBoardWaste(canvas, bx, by, boardW, boardH);
        DrawInnerSegmentFill(canvas, cx, ringCy, by, (float)R_o * s, (float)R_i * s, alpha);
        DrawBoardOutline(canvas, bx, by, boardW, boardH);
        DrawInnerSegmentOutline(canvas, cx, ringCy, by, (float)R_o * s, (float)R_i * s, alpha);
        DrawOuterArc(canvas, cx, ringCy, by, (float)R_o * s, alpha);
        DrawAnnotations(canvas, bx, by, boardW, boardH, cx, s, alpha, sinA, cosA, sagOPx);
    }

    // ── Paths ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// The "inner segment" = the wood kept inside the board rectangle.
    /// Top edge = outer chord (straight line), sides = miter cuts,
    /// bottom = inner arc bowing upward (same ring-centre formula as outer arc).
    /// </summary>
    private static PathF BuildInnerSegmentPath(float cx, float ringCy, float boardTopY,
        float roPx, float riPx, double alpha)
    {
        double sinA = Math.Sin(alpha);
        float outerLeftX  = cx - roPx * (float)sinA;
        float outerRightX = cx + roPx * (float)sinA;

        var path = new PathF();
        // Outer chord = straight top edge of board
        path.MoveTo(outerLeftX,  boardTopY);
        path.LineTo(outerRightX, boardTopY);

        // Right miter line + inner arc (t from +α → −α)
        for (int i = 0; i <= ArcSteps; i++)
        {
            double t  = alpha - i * 2.0 * alpha / ArcSteps;
            float  px = cx + riPx * (float)Math.Sin(t);
            float  py = ringCy - riPx * (float)Math.Cos(t);
            path.LineTo(px, py);
        }

        // Left miter line is implicit via path.Close()
        path.Close();
        return path;
    }

    // ── Draw layers ───────────────────────────────────────────────────────────

    private static void DrawBoardWaste(ICanvas canvas, float bx, float by, float boardW, float boardH)
    {
        canvas.FillColor = Color.FromArgb("#E8624A");
        canvas.FillRectangle(bx, by, boardW, boardH);
        canvas.StrokeColor = Color.FromArgb("#C43620");
        canvas.StrokeSize  = 0.8f;
        canvas.StrokeDashPattern = null;
        for (float d = -boardH; d <= boardW + boardH; d += 10f)
        {
            float x1 = bx + d, y1 = by;
            float x2 = bx + d + boardH, y2 = by + boardH;
            if (x1 < bx)          { y1 += bx - x1;              x1 = bx;          }
            if (x2 > bx + boardW) { y2 -= x2 - (bx + boardW);   x2 = bx + boardW; }
            if (x1 <= x2 && y1 <= y2) canvas.DrawLine(x1, y1, x2, y2);
        }
    }

    private static void DrawInnerSegmentFill(ICanvas canvas, float cx, float ringCy, float boardTopY,
        float roPx, float riPx, double alpha)
    {
        canvas.FillColor = Color.FromArgb("#D4A96A");
        canvas.FillPath(BuildInnerSegmentPath(cx, ringCy, boardTopY, roPx, riPx, alpha));
    }

    private static void DrawBoardOutline(ICanvas canvas, float bx, float by, float boardW, float boardH)
    {
        canvas.StrokeColor       = Colors.Black;
        canvas.StrokeSize        = 1f;
        canvas.StrokeDashPattern = new float[] { 5, 3 };
        canvas.DrawRectangle(bx, by, boardW, boardH);
        canvas.StrokeDashPattern = null;
    }

    private static void DrawInnerSegmentOutline(ICanvas canvas, float cx, float ringCy, float boardTopY,
        float roPx, float riPx, double alpha)
    {
        canvas.StrokeColor       = Colors.Black;
        canvas.StrokeSize        = 1.8f;
        canvas.StrokeDashPattern = null;
        canvas.DrawPath(BuildInnerSegmentPath(cx, ringCy, boardTopY, roPx, riPx, alpha));
    }

    /// <summary>
    /// Outer arc: uses ring-centre formula y = ringCy − R_o·cos(t).
    /// At t=0 the midpoint is ABOVE the board top by sagOPx; endpoints are at board top.
    /// This arc represents the curved outer surface cut (both arcs face the same ring centre).
    /// </summary>
    private static void DrawOuterArc(ICanvas canvas, float cx, float ringCy, float boardTopY,
        float roPx, double alpha)
    {
        var path = new PathF();
        for (int i = 0; i <= ArcSteps; i++)
        {
            double t  = -alpha + i * 2.0 * alpha / ArcSteps;
            float  px = cx + roPx * (float)Math.Sin(t);
            float  py = ringCy - roPx * (float)Math.Cos(t);   // arches above boardTopY
            if (i == 0) path.MoveTo(px, py); else path.LineTo(px, py);
        }
        canvas.StrokeColor       = Colors.Black;
        canvas.StrokeSize        = 1.8f;
        canvas.StrokeDashPattern = null;
        canvas.DrawPath(path);
    }

    // ── Annotations ───────────────────────────────────────────────────────────

    private void DrawAnnotations(ICanvas canvas, float bx, float by, float boardW, float boardH,
        float cx, float s, double alpha, double sinA, double cosA, float sagOPx)
    {
        var d    = RingData!;
        float roPx = (float)d.OuterRadius * s;
        float riPx = (float)d.InnerRadius * s;
        int   n    = (int)Math.Round(360.0 / d.SegmentAngle);

        float boardRight  = bx + boardW;
        float boardBottom = by + boardH;

        // Inner arc geometry
        float ringCy       = by + roPx * (float)cosA;
        float innerLeftX   = cx - riPx * (float)sinA;
        float innerRightX  = cx + riPx * (float)sinA;
        float innerChordY  = ringCy - riPx * (float)cosA;    // Y of inner arc endpoints
        float innerArcMidY = ringCy - riPx;                  // Y of inner arc midpoint (bows up = smaller Y)

        // Outer arc peak (above board top)
        float outerArcPeakY = by - sagOPx;    // absolute Y of outer arc midpoint

        float midSection = (by + innerChordY) / 2f;  // vertical centre of segment body

        // ── Title (above outer arc peak) ─────────────────────────────────────
        canvas.FontSize  = 12f;
        canvas.FontColor = Colors.Black;
        canvas.DrawString("SEGMENTED RING – ONE SEGMENT (CUTTING GUIDE)",
            cx, outerArcPeakY - 36f, HorizontalAlignment.Center);

        canvas.FontSize  = 9f;
        canvas.FontColor = Color.FromArgb("#555555");
        canvas.DrawString("Both curved edges face the same ring centre. Board shown with waste areas.",
            cx, outerArcPeakY - 23f, HorizontalAlignment.Center);

        // Legend
        float legY = outerArcPeakY - 8f;
        DrawSwatch(canvas, cx - 100f, legY, "#D4A96A", "Wood to keep");
        DrawSwatch(canvas, cx + 10f,  legY, "#E8624A", "Waste (cut off)");

        // ── Outer arc annotation (at the arc peak, above board) ───────────────
        canvas.FontSize  = 8f;
        canvas.FontColor = Color.FromArgb("#AA0000");
        canvas.DrawString("OUTER ARC CUT (creates outer curved surface)",
            cx, outerArcPeakY + 3f, HorizontalAlignment.Center);

        // ── Board Length arrow above board top ────────────────────────────────
        float blY = by - 7f;
        canvas.StrokeColor = Colors.Black; canvas.StrokeSize = 0.8f; canvas.StrokeDashPattern = null;
        canvas.DrawLine(bx, blY, boardRight, blY);
        canvas.DrawLine(bx,         blY - 3, bx,         by);
        canvas.DrawLine(boardRight, blY - 3, boardRight, by);
        canvas.FontSize = 9f; canvas.FontColor = Colors.Black;
        canvas.DrawString($"BOARD LENGTH   {d.OuterEdgeLength:F2} cm",
            cx, blY - 1f, HorizontalAlignment.Center);

        // ── Board Width (right side) ─────────────────────────────────────────
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

        // ── Outer chord dim line (at board top, inside) ───────────────────────
        float teY = by + 18f;
        canvas.StrokeColor = Color.FromArgb("#8B5E1E"); canvas.StrokeSize = 0.7f;
        canvas.StrokeDashPattern = new float[] { 3, 2 };
        canvas.DrawLine(bx,         teY, boardRight, teY);
        canvas.DrawLine(bx,         by,  bx,         teY);
        canvas.DrawLine(boardRight, by,  boardRight, teY);
        canvas.StrokeDashPattern = null;
        canvas.FontSize = 9f; canvas.FontColor = Color.FromArgb("#8B5E1E");
        canvas.DrawString($"TOP EDGE (OUTER CHORD)   {d.OuterEdgeLength:F2} cm",
            cx, teY + 11f, HorizontalAlignment.Center);

        // ── Inner chord dim line ───────────────────────────────────────────────
        float beY = innerArcMidY - 14f;
        canvas.StrokeColor = Color.FromArgb("#8B5E1E"); canvas.StrokeSize = 0.7f;
        canvas.StrokeDashPattern = new float[] { 3, 2 };
        canvas.DrawLine(innerLeftX,  beY, innerRightX, beY);
        canvas.DrawLine(innerLeftX,  beY, innerLeftX,  innerArcMidY);
        canvas.DrawLine(innerRightX, beY, innerRightX, innerArcMidY);
        canvas.StrokeDashPattern = null;
        canvas.FontSize = 9f; canvas.FontColor = Color.FromArgb("#8B5E1E");
        canvas.DrawString($"BOTTOM EDGE (INNER CHORD)   {d.InnerEdgeLength:F2} cm",
            cx, beY - 2f, HorizontalAlignment.Center);

        // ── Angle labels (outside left) ───────────────────────────────────────
        canvas.FontSize = 8f; canvas.FontColor = Color.FromArgb("#1A1A8C");
        canvas.DrawString("ANGLE TO CUT",         bx - 92f, midSection - 16f, HorizontalAlignment.Left);
        canvas.DrawString("Set saw to",           bx - 92f, midSection - 5f,  HorizontalAlignment.Left);
        canvas.FontSize = 11f;
        canvas.DrawString($"{d.MiterAngle:F0}°",  bx - 92f, midSection + 6f,  HorizontalAlignment.Left);
        canvas.FontSize = 8f;
        canvas.DrawString("Cut along this angle", bx - 92f, midSection + 17f, HorizontalAlignment.Left);

        // Angle labels (outside right, after board-width bracket)
        float raX = boardRight + 70f;
        canvas.DrawString("ANGLE TO CUT",         raX, midSection - 16f, HorizontalAlignment.Left);
        canvas.DrawString("Set saw to",           raX, midSection - 5f,  HorizontalAlignment.Left);
        canvas.FontSize = 11f;
        canvas.DrawString($"{d.MiterAngle:F0}°",  raX, midSection + 6f,  HorizontalAlignment.Left);
        canvas.FontSize = 8f;
        canvas.DrawString("Cut along this angle", raX, midSection + 17f, HorizontalAlignment.Left);

        // Corner angle labels inside waste triangles
        canvas.FontSize = 9f; canvas.FontColor = Color.FromArgb("#1A1A8C");
        float lCornerX = (bx + innerLeftX) / 2f;
        float lCornerY = (by + innerChordY) / 2f;
        canvas.DrawString("ANGLE",                lCornerX, lCornerY - 12f, HorizontalAlignment.Center);
        canvas.DrawString($"{d.MiterAngle:F0}°",  lCornerX, lCornerY,       HorizontalAlignment.Center);
        float rCornerX = (boardRight + innerRightX) / 2f;
        canvas.DrawString("ANGLE",                rCornerX, lCornerY - 12f, HorizontalAlignment.Center);
        canvas.DrawString($"{d.MiterAngle:F0}°",  rCornerX, lCornerY,       HorizontalAlignment.Center);

        // ── Curve to cut labels (right side) ─────────────────────────────────
        float crvX = boardRight + 12f + 52f;
        canvas.FontSize = 8f; canvas.FontColor = Color.FromArgb("#AA0000");
        float outerLabelY = (outerArcPeakY + by) / 2f;
        canvas.DrawString("OUTER ARC", crvX, outerLabelY - 4f, HorizontalAlignment.Left);
        canvas.DrawString("(outer surface)", crvX, outerLabelY + 6f, HorizontalAlignment.Left);

        float innerMid = (innerArcMidY + boardBottom) / 2f;
        canvas.DrawString("INNER ARC",  crvX, innerMid - 4f, HorizontalAlignment.Left);
        canvas.DrawString("(inner surface)", crvX, innerMid + 6f, HorizontalAlignment.Left);

        // ── Footer ────────────────────────────────────────────────────────────
        string footer = $"{n} pieces total  ·  θ = {d.MiterAngle:F1}°";
        if (d.SegmentsPerBoard > 0)
            footer += $"  ·  {d.SegmentsPerBoard} per board (offcut {d.BoardOffcut:F1} cm)";
        canvas.FontSize = 9f; canvas.FontColor = Colors.Black;
        canvas.DrawString(footer, cx, boardBottom + 12f, HorizontalAlignment.Center);

        // ── Scale bar ─────────────────────────────────────────────────────────
        float sbY = boardBottom + 26f;
        canvas.StrokeColor = Colors.Gray; canvas.StrokeSize = 0.8f; canvas.StrokeDashPattern = null;
        canvas.DrawLine(bx, sbY, bx + s, sbY);
        canvas.DrawLine(bx,     sbY - 3, bx,     sbY + 3);
        canvas.DrawLine(bx + s, sbY - 3, bx + s, sbY + 3);
        canvas.FontSize = 8f; canvas.FontColor = Colors.Gray;
        canvas.DrawString("| 1 cm |", bx + s / 2f, sbY + 10f, HorizontalAlignment.Center);
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
        canvas.StrokeColor       = Color.FromArgb("#EBEBEB");
        canvas.StrokeSize        = 0.4f;
        canvas.StrokeDashPattern = null;
        for (float x = 0; x < dirtyRect.Width;  x += pixelsPerCm)
            canvas.DrawLine(x, 0, x, dirtyRect.Height);
        for (float y = 0; y < dirtyRect.Height; y += pixelsPerCm)
            canvas.DrawLine(0, y, dirtyRect.Width, y);
    }
}
