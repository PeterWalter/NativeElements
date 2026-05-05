using NativeElements.Models;
using Microsoft.Maui.Graphics;

namespace NativeElements.Drawing;

/// <summary>
/// Renders the ring segment cutting guide as a flat isosceles trapezoid.
///
/// The segment shows the flat board piece that will be cut and assembled:
///   • Trapezoid shape (flat board piece) with straight edges
///   • Outer edge (longer): represents outer radius of the ring segment
///   • Inner edge (shorter): represents inner radius of the ring segment
///   • Miter cuts on left/right: the angles of the trapezoid sides (where to cut)
///   • Grain direction: runs radially (from outer to inner edge)
///
/// Inside the trapezoid:
///   • Outer arc (red dashed): shows the curvature of where outer edge sits in the ring
///   • Inner arc (blue dashed): shows the curvature of where inner edge sits in the ring
///   • These arcs help verify the segment geometry is correct
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
        double tanA  = Math.Tan(alpha);
        double R_o   = d.OuterRadius;
        double R_i   = d.InnerRadius;
        double Lo    = 2.0 * R_o * tanA;   // outer chord using tan formula

        double minBW       = d.MinBoardWidth > 0 ? d.MinBoardWidth : (R_o - R_i * cosA);
        double boardWidthCm = (d.UserBoardWidthUsed > 0 && d.UserBoardWidthUsed >= minBW)
                              ? d.UserBoardWidthUsed : minBW;

        const float padTop    = 56f;   // room for title above segment
        const float padBottom = 44f;
        const float padLeft   = 50f;
        const float padRight  = 50f;

        float availW = dirtyRect.Width  - padLeft  - padRight;
        float availH = dirtyRect.Height - padTop - padBottom;
        if (availW <= 10 || availH <= 10) return;

        // Scale: fit trapezoid (Lo × boardWidthCm) into the available area
        float s      = (float)Math.Min(availW / Lo, availH / boardWidthCm);
        float roPx   = (float)R_o * s;
        float riPx   = (float)R_i * s;
        
        // Center trapezoid horizontally and vertically
        float cx     = padLeft + availW / 2f;
        float ringCy = padTop + availH / 2f + (roPx + riPx * (float)cosA) / 2f;

        DrawGrid(canvas, dirtyRect, s);
        DrawSegmentFill(canvas, cx, ringCy, roPx, riPx, alpha);
        DrawSegmentOutline(canvas, cx, ringCy, roPx, riPx, alpha);
        DrawOuterArc(canvas, cx, ringCy, roPx, alpha);
        DrawInnerArc(canvas, cx, ringCy, riPx, alpha);
        DrawMiterAngleGuides(canvas, cx, ringCy, roPx, riPx, alpha, (float)sinA, (float)cosA);
        DrawAnnotations(canvas, cx, ringCy, s, alpha, sinA, cosA, roPx, riPx, 
                        padLeft, padTop, availW, availH, minBW);
    }

    // ── Segment path ──────────────────────────────────────────────────────────

    /// <summary>
    /// Closed path: isosceles trapezoid representing the flat board piece.
    /// Outer edge (longer): outer chord using tan formula
    /// Inner edge (shorter): inner chord using sin formula
    /// Two miter cuts at sides (straight lines at cutting angle)
    /// </summary>
    private static PathF BuildSegmentPath(float cx, float ringCy, float roPx, float riPx, double alpha)
    {
        var path = new PathF();

        // Trapezoid vertices (flat board view):
        // Outer edge is longer (at y = ringCy - roPx)
        // Inner edge is shorter (at y = ringCy - riPx·cos(α))
        // Miter cuts on left and right connect them
        
        float outerLeftX = cx - roPx * (float)Math.Sin(alpha);
        float outerRightX = cx + roPx * (float)Math.Sin(alpha);
        float outerY = ringCy - roPx * (float)Math.Cos(alpha);  // outer chord level
        
        float innerLeftX = cx - riPx * (float)Math.Sin(alpha);
        float innerRightX = cx + riPx * (float)Math.Sin(alpha);
        float innerY = ringCy - riPx * (float)Math.Cos(alpha);  // inner chord level

        // Build trapezoid: outer-left → outer-right → inner-right → inner-left → close
        path.MoveTo(outerLeftX, outerY);
        path.LineTo(outerRightX, outerY);      // outer edge (top)
        path.LineTo(innerRightX, innerY);      // right miter cut
        path.LineTo(innerLeftX, innerY);       // inner edge (bottom)
        path.Close();                          // left miter cut auto-added

        return path;
    }

    // ── Draw layers ───────────────────────────────────────────────────────────

    private static void DrawSegmentFill(ICanvas canvas, float cx, float ringCy, float roPx, float riPx, double alpha)
    {
        canvas.FillColor = Color.FromArgb("#D4A96A");
        canvas.FillPath(BuildSegmentPath(cx, ringCy, roPx, riPx, alpha));
    }

    /// <summary>
    /// Draw the outer arc inside the trapezoid (thin dashed line).
    /// Shows the curved path that the outer edge follows when assembled in the ring.
    /// Arc connects from left endpoint to right endpoint, bowed outward (above).
    /// </summary>
    private static void DrawOuterArc(ICanvas canvas, float cx, float ringCy, float roPx, double alpha)
    {
        canvas.StrokeColor = Color.FromArgb("#CC0000");  // red for outer arc
        canvas.StrokeSize = 0.8f;
        canvas.StrokeDashPattern = new float[] { 4, 3 };

        float outerLeftX = cx - roPx * (float)Math.Sin(alpha);
        float outerRightX = cx + roPx * (float)Math.Sin(alpha);
        float outerY = ringCy - roPx * (float)Math.Cos(alpha);

        // Draw arc from left endpoint to right endpoint (curved outward/upward)
        for (int i = 0; i <= ArcSteps; i++)
        {
            double t = -Math.PI / 2 - alpha + i * 2 * alpha / ArcSteps;
            float x = cx + roPx * (float)Math.Cos(t);
            float y = ringCy + roPx * (float)Math.Sin(t);
            
            if (i == 0) continue;
            
            double prevT = -Math.PI / 2 - alpha + (i - 1) * 2 * alpha / ArcSteps;
            float prevX = cx + roPx * (float)Math.Cos(prevT);
            float prevY = ringCy + roPx * (float)Math.Sin(prevT);
            canvas.DrawLine(prevX, prevY, x, y);
        }

        canvas.StrokeDashPattern = null;
    }

    /// <summary>
    /// Draw the inner arc inside the trapezoid (thin dashed line).
    /// Shows the curved path that the inner edge follows when assembled in the ring.
    /// Arc connects from left endpoint to right endpoint, bowed inward (below/upward toward center).
    /// </summary>
    private static void DrawInnerArc(ICanvas canvas, float cx, float ringCy, float riPx, double alpha)
    {
        canvas.StrokeColor = Color.FromArgb("#0066CC");  // blue for inner arc
        canvas.StrokeSize = 0.8f;
        canvas.StrokeDashPattern = new float[] { 4, 3 };

        float innerLeftX = cx - riPx * (float)Math.Sin(alpha);
        float innerRightX = cx + riPx * (float)Math.Sin(alpha);
        float innerY = ringCy - riPx * (float)Math.Cos(alpha);

        // Draw arc from left endpoint to right endpoint (curved inward/upward toward center)
        for (int i = 0; i <= ArcSteps; i++)
        {
            double t = -Math.PI / 2 - alpha + i * 2 * alpha / ArcSteps;
            float x = cx + riPx * (float)Math.Cos(t);
            float y = ringCy + riPx * (float)Math.Sin(t);
            
            if (i == 0) continue;
            
            double prevT = -Math.PI / 2 - alpha + (i - 1) * 2 * alpha / ArcSteps;
            float prevX = cx + riPx * (float)Math.Cos(prevT);
            float prevY = ringCy + riPx * (float)Math.Sin(prevT);
            canvas.DrawLine(prevX, prevY, x, y);
        }

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

    /// <summary>
    /// Draw angle guide lines on the left and right miter cuts showing the cutting angle.
    /// The angle runs from board top to board bottom, starting perpendicular and rotating by the miter angle.
    /// </summary>
    private static void DrawMiterAngleGuides(ICanvas canvas, float cx, float ringCy, 
        float roPx, float riPx, double alpha, float sinA, float cosA)
    {
        // Left miter cut endpoint coordinates
        float outerLeftX = cx - roPx * sinA;
        float outerLeftY = ringCy - roPx * cosA;
        float innerLeftX = cx - riPx * sinA;
        float innerLeftY = ringCy - riPx * cosA;

        // Right miter cut endpoint coordinates
        float outerRightX = cx + roPx * sinA;
        float outerRightY = ringCy - roPx * cosA;
        float innerRightX = cx + riPx * sinA;
        float innerRightY = ringCy - riPx * cosA;

        // Draw left miter angle guide: line from outer to inner arc endpoints
        canvas.StrokeColor = Color.FromArgb("#FF6B35");  // bright orange-red
        canvas.StrokeSize = 1.2f;
        canvas.StrokeDashPattern = new float[] { 5, 3 };
        canvas.DrawLine(outerLeftX, outerLeftY, innerLeftX, innerLeftY);

        // Draw right miter angle guide: line from outer to inner arc endpoints
        canvas.DrawLine(outerRightX, outerRightY, innerRightX, innerRightY);

        canvas.StrokeDashPattern = null;

        // Add small angle arcs and labels to show the cutting angle more clearly
        float arcRadius = 8f;
        
        // Left angle annotation
        double leftAngle = Math.Atan2(innerLeftY - outerLeftY, innerLeftX - outerLeftX);
        double perpAngle = -Math.PI / 2;  // perpendicular (straight down)
        
        canvas.StrokeColor = Color.FromArgb("#FF6B35");
        canvas.StrokeSize = 0.8f;
        
        // Draw small arc to show angle at left
        for (int i = 0; i <= 20; i++)
        {
            double t = perpAngle + i * (leftAngle - perpAngle) / 20.0;
            float x = outerLeftX + arcRadius * (float)Math.Cos(t);
            float y = outerLeftY + arcRadius * (float)Math.Sin(t);
            if (i == 0) continue;
            
            double prevT = perpAngle + (i - 1) * (leftAngle - perpAngle) / 20.0;
            float prevX = outerLeftX + arcRadius * (float)Math.Cos(prevT);
            float prevY = outerLeftY + arcRadius * (float)Math.Sin(prevT);
            canvas.DrawLine(prevX, prevY, x, y);
        }

        // Right angle annotation (mirrored)
        double rightAngle = Math.Atan2(innerRightY - outerRightY, innerRightX - outerRightX);
        double rightPerpAngle = Math.PI / 2;  // perpendicular (straight down, mirrored)
        
        for (int i = 0; i <= 20; i++)
        {
            double t = rightPerpAngle - i * (rightPerpAngle - rightAngle) / 20.0;
            float x = outerRightX + arcRadius * (float)Math.Cos(t);
            float y = outerRightY + arcRadius * (float)Math.Sin(t);
            if (i == 0) continue;
            
            double prevT = rightPerpAngle - (i - 1) * (rightPerpAngle - rightAngle) / 20.0;
            float prevX = outerRightX + arcRadius * (float)Math.Cos(prevT);
            float prevY = outerRightY + arcRadius * (float)Math.Sin(prevT);
            canvas.DrawLine(prevX, prevY, x, y);
        }
    }


    private void DrawAnnotations(ICanvas canvas, float cx, float ringCy, float s, double alpha, 
        double sinA, double cosA, float roPx, float riPx, float padLeft, float padTop, 
        float availW, float availH, double minBW)
    {
        var d    = RingData!;
        int n    = (int)Math.Round(360.0 / d.SegmentAngle);

        // Key segment coordinates
        float outerLeftX  = cx - roPx * (float)sinA;
        float outerRightX = cx + roPx * (float)sinA;
        float outerY      = ringCy - roPx * (float)cosA;
        
        float innerLeftX  = cx - riPx * (float)sinA;
        float innerRightX = cx + riPx * (float)sinA;
        float innerY      = ringCy - riPx * (float)cosA;
        
        float innerArcMidY = ringCy - riPx;  // Y of inner arc peak (bows up)
        float midSection = (outerY + innerY) / 2f;  // vertical centre of segment body

        // ── Title (above segment) ──────────────────────────────────────────────
        canvas.FontSize  = 12f;
        canvas.FontColor = Colors.Black;
        canvas.DrawString("SEGMENTED RING – ONE SEGMENT (CUTTING GUIDE)",
            cx, padTop - 38f, HorizontalAlignment.Center);
        canvas.FontSize  = 9f;
        canvas.FontColor = Color.FromArgb("#555555");
        canvas.DrawString("Trapezoid to cut. Red arc = outer edge curve. Blue arc = inner edge curve.",
            cx, padTop - 26f, HorizontalAlignment.Center);

        float legY = padTop - 11f;
        DrawSwatch(canvas, cx - 80f, legY, "#D4A96A", "Segment");

        // ── Outer edge dimension line ──────────────────────────────────────────
        canvas.StrokeColor = Color.FromArgb("#8B5E1E"); canvas.StrokeSize = 0.7f;
        canvas.StrokeDashPattern = new float[] { 3, 2 };
        canvas.DrawLine(outerLeftX, outerY, outerRightX, outerY);
        canvas.DrawLine(outerLeftX,  outerY - 5, outerLeftX,  outerY + 5);
        canvas.DrawLine(outerRightX, outerY - 5, outerRightX, outerY + 5);
        canvas.StrokeDashPattern = null;
        canvas.FontSize = 8f; canvas.FontColor = Color.FromArgb("#8B5E1E");
        canvas.DrawString($"OUTER EDGE = {d.OuterEdgeLength:F2} cm",
            cx, outerY + 10f, HorizontalAlignment.Center);

        // ── Inner edge dimension line ──────────────────────────────────────────
        float innerDimY = Math.Min(innerArcMidY - 14f, innerY - 12f);
        canvas.StrokeColor = Color.FromArgb("#8B5E1E"); canvas.StrokeSize = 0.7f;
        canvas.StrokeDashPattern = new float[] { 3, 2 };
        canvas.DrawLine(innerLeftX,  innerDimY, innerRightX, innerDimY);
        canvas.DrawLine(innerLeftX,  innerDimY, innerLeftX,  innerY);
        canvas.DrawLine(innerRightX, innerDimY, innerRightX, innerY);
        canvas.StrokeDashPattern = null;
        canvas.FontSize = 8f; canvas.FontColor = Color.FromArgb("#8B5E1E");
        canvas.DrawString($"INNER EDGE = {d.InnerEdgeLength:F2} cm",
            cx, innerDimY - 2f, HorizontalAlignment.Center);

        // ── Miter angle labels (left side) ────────────────────────────────────
        canvas.FontSize = 8f; canvas.FontColor = Color.FromArgb("#1A1A8C");
        float leftX = outerLeftX - 80f;
        canvas.DrawString("MITER ANGLE",        leftX, midSection - 14f, HorizontalAlignment.Right);
        canvas.DrawString("Set saw to",          leftX, midSection - 2f,  HorizontalAlignment.Right);
        canvas.FontSize = 10f;
        canvas.DrawString($"{d.MiterAngle:F0}°", leftX, midSection + 8f,  HorizontalAlignment.Right);
        
        // ── Miter angle labels (right side) ───────────────────────────────────
        float rightX = outerRightX + 80f;
        canvas.FontSize = 8f;
        canvas.DrawString("MITER ANGLE",        rightX, midSection - 14f, HorizontalAlignment.Left);
        canvas.DrawString("Set saw to",          rightX, midSection - 2f,  HorizontalAlignment.Left);
        canvas.FontSize = 10f;
        canvas.DrawString($"{d.MiterAngle:F0}°", rightX, midSection + 8f,  HorizontalAlignment.Left);

        // ── Footer ────────────────────────────────────────────────────────────
        string footer = $"{n} pieces total  ·  θ = {d.MiterAngle:F1}°  ·  Min board width: {minBW:F2} cm";
        if (d.SegmentsPerBoard > 0)
            footer += $"  ·  {d.SegmentsPerBoard} per board";
        canvas.FontSize = 8f; canvas.FontColor = Colors.Black;
        float footerY = padTop + availH + 12f;
        canvas.DrawString(footer, cx, footerY, HorizontalAlignment.Center);

        // ── Scale bar ─────────────────────────────────────────────────────────
        float sbY = footerY + 14f;
        canvas.StrokeColor = Colors.Gray; canvas.StrokeSize = 0.8f; canvas.StrokeDashPattern = null;
        canvas.DrawLine(padLeft, sbY, padLeft + s, sbY);
        canvas.DrawLine(padLeft,     sbY - 3, padLeft,     sbY + 3);
        canvas.DrawLine(padLeft + s, sbY - 3, padLeft + s, sbY + 3);
        canvas.FontSize = 7f; canvas.FontColor = Colors.Gray;
        canvas.DrawString("1 cm", padLeft + s / 2f, sbY + 8f, HorizontalAlignment.Center);
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
