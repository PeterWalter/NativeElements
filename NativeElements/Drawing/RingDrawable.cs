using NativeElements.Models;
using Microsoft.Maui.Graphics;

namespace NativeElements.Drawing;

/// <summary>
/// Renders the ring segment cutting guide.
///
/// Geometry: the board rectangle contains both arcs entirely.
///   • Board top edge  = outer arc PEAK (the outermost point of the arc)
///   • Ring centre     = (cx, by + Ro·s)  — below the board top by the outer radius
///   • Outer arc       = from left endpoint (inside board) up to peak (board top) back down to right endpoint
///   • Inner arc       = bows upward inside the board; its endpoints sit at the minimum board-depth line
///   • Top-corner waste = curved triangles at board top-left / top-right between board edge and outer arc
///   • Bottom waste    = area below inner arc endpoints (if board wider than minimum)
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
        double Lo    = 2.0 * R_o * sinA;   // outer chord = board width (cm)

        double minBW       = d.MinBoardWidth > 0 ? d.MinBoardWidth : (R_o - R_i * cosA);
        double boardWidthCm = (d.UserBoardWidthUsed > 0 && d.UserBoardWidthUsed >= minBW)
                              ? d.UserBoardWidthUsed : minBW;

        const float padTopBase = 56f;   // room for title + legend above board
        const float padBottom  = 44f;
        const float padLeft    = 96f;
        const float padRight   = 160f;

        float availW = dirtyRect.Width  - padLeft  - padRight;
        float availH = dirtyRect.Height - padTopBase - padBottom;
        if (availW <= 10 || availH <= 10) return;

        // Scale: fit board (Lo × boardWidthCm) into the available area
        float s      = (float)Math.Min(availW / Lo, availH / boardWidthCm);
        float boardW = (float)Lo * s;
        float boardH = (float)boardWidthCm * s;
        float roPx   = (float)R_o * s;
        float riPx   = (float)R_i * s;

        // Board top = outer arc peak
        float bx     = padLeft;
        float by     = padTopBase;
        float cx     = bx + boardW / 2f;
        float ringCy = by + roPx;    // ring centre is Ro·s below the board top

        DrawGrid(canvas, dirtyRect, s);
        DrawBoardWaste(canvas, bx, by, boardW, boardH);
        DrawSegmentFill(canvas, cx, ringCy, roPx, riPx, alpha);
        DrawBoardOutline(canvas, bx, by, boardW, boardH);
        DrawSegmentOutline(canvas, cx, ringCy, roPx, riPx, alpha);
        DrawAnnotations(canvas, bx, by, boardW, boardH, cx, ringCy, s, alpha,
                        sinA, cosA, roPx, riPx, boardWidthCm, minBW);
    }

    // ── Segment path ──────────────────────────────────────────────────────────

    /// <summary>
    /// Closed path: outer arc (peak at board top) → right miter → inner arc → left miter (close).
    /// </summary>
    private static PathF BuildSegmentPath(float cx, float ringCy, float roPx, float riPx, double alpha)
    {
        var path = new PathF();

        // Outer arc: t from −α (left endpoint) → 0 (peak, board top) → +α (right endpoint)
        for (int i = 0; i <= ArcSteps; i++)
        {
            double t  = -alpha + i * 2.0 * alpha / ArcSteps;
            float  px = cx + roPx * (float)Math.Sin(t);
            float  py = ringCy - roPx * (float)Math.Cos(t);
            if (i == 0) path.MoveTo(px, py); else path.LineTo(px, py);
        }

        // Inner arc: t from +α (right endpoint) → 0 (inner peak) → −α (left endpoint)
        // First LineTo is the right miter cut; Close() handles the left miter cut.
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

    private static void DrawSegmentFill(ICanvas canvas, float cx, float ringCy, float roPx, float riPx, double alpha)
    {
        canvas.FillColor = Color.FromArgb("#D4A96A");
        canvas.FillPath(BuildSegmentPath(cx, ringCy, roPx, riPx, alpha));
    }

    private static void DrawBoardOutline(ICanvas canvas, float bx, float by, float boardW, float boardH)
    {
        canvas.StrokeColor       = Colors.Black;
        canvas.StrokeSize        = 1f;
        canvas.StrokeDashPattern = new float[] { 5, 3 };
        canvas.DrawRectangle(bx, by, boardW, boardH);
        canvas.StrokeDashPattern = null;
    }

    private static void DrawSegmentOutline(ICanvas canvas, float cx, float ringCy, float roPx, float riPx, double alpha)
    {
        canvas.StrokeColor       = Colors.Black;
        canvas.StrokeSize        = 1.8f;
        canvas.StrokeDashPattern = null;
        canvas.DrawPath(BuildSegmentPath(cx, ringCy, roPx, riPx, alpha));
    }

    // ── Annotations ───────────────────────────────────────────────────────────

    private void DrawAnnotations(ICanvas canvas, float bx, float by, float boardW, float boardH,
        float cx, float ringCy, float s, double alpha, double sinA, double cosA,
        float roPx, float riPx, double boardWidthCm, double minBW)
    {
        var d    = RingData!;
        int n    = (int)Math.Round(360.0 / d.SegmentAngle);

        float boardRight  = bx + boardW;
        float boardBottom = by + boardH;

        // Key arc coordinates
        float outerEndY   = ringCy - roPx * (float)cosA;   // Y of outer arc endpoints (below board top)
        float outerLeftX  = cx - roPx * (float)sinA;       // same as bx
        float outerRightX = cx + roPx * (float)sinA;       // same as boardRight

        float innerChordY  = ringCy - riPx * (float)cosA;  // Y of inner arc endpoints
        float innerArcMidY = ringCy - riPx;                 // Y of inner arc peak (bows up)
        float innerLeftX   = cx - riPx * (float)sinA;
        float innerRightX  = cx + riPx * (float)sinA;

        float midSection = (outerEndY + innerChordY) / 2f;  // vertical centre of segment body

        // ── Title / legend (above board top) ─────────────────────────────────
        canvas.FontSize  = 12f;
        canvas.FontColor = Colors.Black;
        canvas.DrawString("SEGMENTED RING – ONE SEGMENT (CUTTING GUIDE)",
            cx, by - 38f, HorizontalAlignment.Center);
        canvas.FontSize  = 9f;
        canvas.FontColor = Color.FromArgb("#555555");
        canvas.DrawString("Outer arc peak is at board top edge. Shade = waste wood.",
            cx, by - 26f, HorizontalAlignment.Center);

        float legY = by - 11f;
        DrawSwatch(canvas, cx - 100f, legY, "#D4A96A", "Wood to keep");
        DrawSwatch(canvas, cx + 10f,  legY, "#E8624A", "Waste (cut off)");

        // ── Outer arc label (inside board top-corner waste areas) ─────────────
        canvas.FontSize  = 8f;
        canvas.FontColor = Color.FromArgb("#AA0000");
        canvas.DrawString("← OUTER ARC CUT", bx + 4f, (by + outerEndY) / 2f, HorizontalAlignment.Left);

        // ── Outer chord dim line (at outer arc endpoint level) ────────────────
        canvas.StrokeColor = Color.FromArgb("#8B5E1E"); canvas.StrokeSize = 0.7f;
        canvas.StrokeDashPattern = new float[] { 3, 2 };
        canvas.DrawLine(outerLeftX, outerEndY, outerRightX, outerEndY);
        canvas.DrawLine(outerLeftX,  outerEndY - 5, outerLeftX,  outerEndY + 5);
        canvas.DrawLine(outerRightX, outerEndY - 5, outerRightX, outerEndY + 5);
        canvas.StrokeDashPattern = null;
        canvas.FontSize = 9f; canvas.FontColor = Color.FromArgb("#8B5E1E");
        canvas.DrawString($"OUTER CHORD (Lo) = {d.OuterEdgeLength:F2} cm",
            cx, outerEndY + 9f, HorizontalAlignment.Center);

        // ── Inner chord dim line ───────────────────────────────────────────────
        float beY = Math.Max(by + 6f, innerArcMidY - 14f);
        canvas.StrokeColor = Color.FromArgb("#8B5E1E"); canvas.StrokeSize = 0.7f;
        canvas.StrokeDashPattern = new float[] { 3, 2 };
        canvas.DrawLine(innerLeftX,  beY, innerRightX, beY);
        canvas.DrawLine(innerLeftX,  beY, innerLeftX,  innerChordY);
        canvas.DrawLine(innerRightX, beY, innerRightX, innerChordY);
        canvas.StrokeDashPattern = null;
        canvas.FontSize = 9f; canvas.FontColor = Color.FromArgb("#8B5E1E");
        canvas.DrawString($"INNER CHORD (Li) = {d.InnerEdgeLength:F2} cm",
            cx, beY - 2f, HorizontalAlignment.Center);

        // Inner arc label
        canvas.FontSize  = 8f;
        canvas.FontColor = Color.FromArgb("#AA0000");
        float innerLblY = (innerArcMidY + boardBottom) / 2f;
        canvas.DrawString("INNER ARC CUT →", boardRight - 4f, innerLblY, HorizontalAlignment.Right);

        // ── Miter angle labels (outside left) ────────────────────────────────
        canvas.FontSize = 8f; canvas.FontColor = Color.FromArgb("#1A1A8C");
        canvas.DrawString("MITER ANGLE",        bx - 92f, midSection - 16f, HorizontalAlignment.Left);
        canvas.DrawString("Set saw to",          bx - 92f, midSection - 5f,  HorizontalAlignment.Left);
        canvas.FontSize = 11f;
        canvas.DrawString($"{d.MiterAngle:F0}°", bx - 92f, midSection + 6f,  HorizontalAlignment.Left);
        canvas.FontSize = 8f;
        canvas.DrawString("each end",            bx - 92f, midSection + 17f, HorizontalAlignment.Left);

        // Miter angle labels (outside right, after board-width bracket)
        float raX = boardRight + 70f;
        canvas.DrawString("MITER ANGLE",        raX, midSection - 16f, HorizontalAlignment.Left);
        canvas.DrawString("Set saw to",          raX, midSection - 5f,  HorizontalAlignment.Left);
        canvas.FontSize = 11f;
        canvas.DrawString($"{d.MiterAngle:F0}°", raX, midSection + 6f,  HorizontalAlignment.Left);
        canvas.FontSize = 8f;
        canvas.DrawString("each end",            raX, midSection + 17f, HorizontalAlignment.Left);

        // ── Board Width bracket (right side) ─────────────────────────────────
        float bwX = boardRight + 12f;
        canvas.StrokeColor = Color.FromArgb("#444444"); canvas.StrokeSize = 0.8f;
        canvas.DrawLine(bwX, by, bwX, boardBottom);
        canvas.DrawLine(boardRight, by,          bwX + 4, by);
        canvas.DrawLine(boardRight, boardBottom, bwX + 4, boardBottom);
        canvas.FontSize = 9f; canvas.FontColor = Color.FromArgb("#444444");
        float bwMid = (by + boardBottom) / 2f;
        canvas.DrawString("BOARD WIDTH",  bwX + 6f, bwMid - 12f, HorizontalAlignment.Left);
        string bwVal = d.UserBoardWidthUsed > 0
            ? $"{boardWidthCm:F2} cm"
            : $"{minBW:F2} cm (min)";
        canvas.DrawString(bwVal,          bwX + 6f, bwMid,       HorizontalAlignment.Left);

        // Dashed minimum-depth line when board is wider than minimum
        if (boardWidthCm > minBW + 0.01)
        {
            float minY = by + (float)minBW * s;
            canvas.StrokeColor = Color.FromArgb("#AA5500"); canvas.StrokeSize = 0.7f;
            canvas.StrokeDashPattern = new float[] { 4, 2 };
            canvas.DrawLine(bx, minY, boardRight, minY);
            canvas.StrokeDashPattern = null;
            canvas.FontSize = 8f; canvas.FontColor = Color.FromArgb("#AA5500");
            canvas.DrawString($"─ min {minBW:F2} cm", boardRight + 4f, minY + 4f, HorizontalAlignment.Left);
        }

        // ── Footer ────────────────────────────────────────────────────────────
        string footer = $"{n} pieces total  ·  θ = {d.MiterAngle:F1}°  ·  Min board width: {minBW:F2} cm";
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
