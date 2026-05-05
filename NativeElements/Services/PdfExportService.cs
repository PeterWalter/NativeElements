using NativeElements.Models;
using SkiaSharp;

namespace NativeElements.Services;

/// <summary>
/// Exports patterns to PDF at true 1:1 scale using vector drawing.
///
/// PDF coordinate system uses points: 1 pt = 1/72 inch.
/// Therefore 1 cm = 72/2.54 ≈ 28.346 pts.
/// All page dimensions and drawing coordinates are in PDF points so that
/// "1 cm on the PDF = 1 cm on paper" when printed at 100 % / no scaling.
/// </summary>
public class PdfExportService
{
    // 72 PDF points per inch; 1 cm = 72/2.54 ≈ 28.346 pts
    private const float PtPerCm = 72.0f / 2.54f;
    private const float MarginCm = 0.7f;            // tight margin – saves paper
    private const float MarginPt = MarginCm * PtPerCm;

    // ─── Petal ────────────────────────────────────────────────────────────────

    public static async Task<string> ExportPetalToPdfAsync(PetalOutput petalData, string fileName, double dpi = 300)
    {
        return await Task.Run(() =>
        {
            // Use seam curve as outer boundary; fall back to sewing curve
            var outerPts = petalData.SeamCurvePoints.Count > 0
                ? petalData.SeamCurvePoints : petalData.CurvePoints;
            if (outerPts.Count == 0) throw new InvalidOperationException("No curve points to render.");

            // Physical bounding box of the outermost edge
            float maxXcm   = (float)outerPts.Max(p => p.X);
            float maxYcm   = (float)outerPts.Max(p => p.Y);
            float minYcm   = (float)outerPts.Min(p => p.Y);
            float widthCm  = 2f * maxXcm;
            float heightCm = maxYcm - minYcm;

            // Reserve space for the title line placed immediately above the petal
            const float titleSizePt = 8f;
            const float titleGapPt  = 3f;          // gap between label and petal tip
            float topReservePt = titleSizePt + titleGapPt;

            float pageWidthPt  = (widthCm  + 2f * MarginCm) * PtPerCm;
            float pageHeightPt = (heightCm + 2f * MarginCm) * PtPerCm + topReservePt;

            // Coordinate mapping: petal minY maps to (MarginPt + topReservePt)
            float centerXPt = pageWidthPt / 2f;
            float yOriginPt = MarginPt + topReservePt - minYcm * PtPerCm;

            var filePath = PdfPath(fileName);
            using var stream = File.OpenWrite(filePath);
            using var doc    = SKDocument.CreatePdf(stream);
            using var canvas = doc.BeginPage(pageWidthPt, pageHeightPt);
            canvas.Clear(SKColors.White);

            // — Seam curve (cut line) — orange dashed
            if (petalData.SeamCurvePoints.Count > 0 && petalData.SeamAllowance > 0)
            {
                using var seamPaint = StrokePaint("#FFA500", 0.5f, new[] { 3.5f, 2.5f });
                canvas.DrawPath(ClosedPetalPath(petalData.SeamCurvePoints, centerXPt, yOriginPt, PtPerCm), seamPaint);

                // SA label on left side, mid-height
                int midIdx = petalData.SeamCurvePoints.Count / 2;
                float lx = centerXPt - (float)petalData.SeamCurvePoints[midIdx].X * PtPerCm;
                float ly = yOriginPt  + (float)petalData.SeamCurvePoints[midIdx].Y * PtPerCm;
                using var saFont = TextPaint("#FFA500", 6f);
                string saText = $"SA {petalData.SeamAllowance:F1} cm";
                canvas.DrawText(saText, lx - saFont.MeasureText(saText) - 2f, ly, saFont);
            }

            // — Sewing line — solid black
            using var sewPaint = StrokePaint("#000000", 0.75f);
            canvas.DrawPath(ClosedPetalPath(petalData.CurvePoints, centerXPt, yOriginPt, PtPerCm), sewPaint);

            // — Dimension lines (L and W)
            DrawPetalDimensions(canvas, petalData, centerXPt, yOriginPt, PtPerCm);

            // — Title placed immediately above the top tip of the petal
            float titleY = yOriginPt + minYcm * PtPerCm - titleGapPt;
            using var titleFont = TextPaint("#000000", titleSizePt);
            string title = $"Ø {petalData.SphereDiameter:F0} cm  ·  {petalData.NumberOfPetals} petals  ·  Scale 1:1";
            canvas.DrawText(title, centerXPt - titleFont.MeasureText(title) / 2f, titleY, titleFont);

            // — Scale bar (1 cm) bottom-right
            DrawScaleBar(canvas, pageWidthPt, pageHeightPt);

            doc.EndPage();
            doc.Close();
            return filePath;
        });
    }

    // ─── Ring (single segment, 1:1 scale) ────────────────────────────────────

    public static async Task<string> ExportRingToPdfAsync(SegmentedRingOutput ringData, string fileName, double dpi = 300)
    {
        return await Task.Run(() =>
        {
            double R_o = ringData.OuterRadius;
            double R_i = ringData.InnerRadius;
            if (R_o <= 0 || R_i <= 0 || R_i >= R_o)
                throw new InvalidOperationException("Invalid ring radii.");

            double halfAngleRad = ringData.MiterAngle * Math.PI / 180.0;
            double sinA = Math.Sin(halfAngleRad);
            double cosA = Math.Cos(halfAngleRad);
            double tanA = Math.Tan(halfAngleRad);

            // Board physical dimensions at 1:1 scale (cm → pts)
            float Lo   = (float)(2.0 * R_o * tanA);    // outer chord using tan formula
            float minBW       = ringData.MinBoardWidth > 0
                                ? (float)ringData.MinBoardWidth
                                : (float)(R_o - R_i * cosA);
            float boardWidthCm = (ringData.UserBoardWidthUsed > 0 && ringData.UserBoardWidthUsed >= minBW)
                                 ? (float)ringData.UserBoardWidthUsed : minBW;

            float boardWPt = Lo           * PtPerCm;
            float boardHPt = boardWidthCm * PtPerCm;

            // Page layout: board top = outer arc peak (no sagOPt offset above board)
            const float titleAreaPt  = 60f;
            const float botReservePt = 48f;
            const float leftResvPt   = 68f;
            const float rightResvPt  = 130f;

            float pageWidthPt  = boardWPt + leftResvPt + rightResvPt;
            float pageHeightPt = boardHPt + MarginPt + titleAreaPt + botReservePt;

            float bLeft  = leftResvPt;
            float bTop   = MarginPt + titleAreaPt;
            float cx     = bLeft + boardWPt / 2f;
            // ring centre: segment is centered vertically on the board
            float ringCy = bTop + boardHPt / 2f + ((float)R_o * PtPerCm + (float)R_i * (float)cosA * PtPerCm) / 2f;

            var filePath = PdfPath(fileName);
            using var stream = File.OpenWrite(filePath);
            using var doc    = SKDocument.CreatePdf(stream);
            using var canvas = doc.BeginPage(pageWidthPt, pageHeightPt);
            canvas.Clear(SKColors.White);

            // 1. Board waste fill + hatching
            using var wasteFill = new SKPaint { Color = SKColor.Parse("#E8624A"), IsAntialias = true };
            canvas.DrawRect(SKRect.Create(bLeft, bTop, boardWPt, boardHPt), wasteFill);
            using var hatchPaint = StrokePaint("#C43620", 0.5f);
            for (float d = -boardHPt; d <= boardWPt + boardHPt; d += 8f)
            {
                float x1 = bLeft + d, y1 = bTop;
                float x2 = bLeft + d + boardHPt, y2 = bTop + boardHPt;
                if (x1 < bLeft)            { y1 += bLeft - x1; x1 = bLeft; }
                if (x2 > bLeft + boardWPt) { y2 -= x2 - (bLeft + boardWPt); x2 = bLeft + boardWPt; }
                if (x1 <= x2 && y1 <= y2) canvas.DrawLine(x1, y1, x2, y2, hatchPaint);
            }

            // 2. Segment fill (outer arc + miter sides + inner arc — fully contained in board)
            var segPath = BuildBoardViewSegmentPath(cx, ringCy,
                (float)R_o * PtPerCm, (float)R_i * PtPerCm, halfAngleRad);
            using var woodFill = new SKPaint { Color = SKColor.Parse("#D4A96A"), IsAntialias = true };
            canvas.DrawPath(segPath, woodFill);

            // 3. Board outline (dashed)
            using var boardOutline = StrokePaint("#000000", 0.6f, new[] { 4f, 2.5f });
            canvas.DrawRect(SKRect.Create(bLeft, bTop, boardWPt, boardHPt), boardOutline);

            // 4. Segment outline (solid — includes outer arc as top boundary)
            using var segOutline = StrokePaint("#000000", 0.85f);
            canvas.DrawPath(segPath, segOutline);

            // 5. Annotations
            DrawRingBoardAnnotations(canvas, ringData, cx, bLeft, bTop, boardWPt, boardHPt,
                ringCy, halfAngleRad, sinA, cosA, boardWidthCm, minBW);

            // 7. Scale bar
            DrawScaleBar(canvas, pageWidthPt, pageHeightPt);

            doc.EndPage();
            doc.Close();
            return filePath;
        });
    }

    /// <summary>
    /// Closed segment path: outer arc → right miter → inner arc → left miter (close).
    /// Both outer and inner edges are circular arcs centered at ring centre.
    /// </summary>
    private static SKPath BuildBoardViewSegmentPath(float cx, float ringCy,
        float roPt, float riPt, double halfRad, int steps = 80)
    {
        var path = new SKPath();

        // Outer arc: t from −α (left endpoint) → 0 (peak) → +α (right endpoint)
        for (int i = 0; i <= steps; i++)
        {
            double t  = -halfRad + i * 2.0 * halfRad / steps;
            float  px = cx + roPt * (float)Math.Sin(t);
            float  py = ringCy - roPt * (float)Math.Cos(t);
            if (i == 0) path.MoveTo(px, py); else path.LineTo(px, py);
        }

        // Inner arc: t from +α (right endpoint) → 0 (peak) → −α (left endpoint)
        // First LineTo becomes the right miter cut; Close = left miter cut
        for (int i = 0; i <= steps; i++)
        {
            double t  = halfRad - i * 2.0 * halfRad / steps;
            float  px = cx + riPt * (float)Math.Sin(t);
            float  py = ringCy - riPt * (float)Math.Cos(t);
            path.LineTo(px, py);
        }

        path.Close();
        return path;
    }

    private static void DrawRingBoardAnnotations(SKCanvas canvas, SegmentedRingOutput data,
        float cx, float bLeft, float bTop, float boardWPt, float boardHPt, float ringCy,
        double halfRad, double sinA, double cosA, float boardWidthCm, float minBW)
    {
        float roPt = (float)data.OuterRadius * PtPerCm;
        float riPt = (float)data.InnerRadius * PtPerCm;
        int   n    = (int)Math.Round(360.0 / data.SegmentAngle);

        float boardRight   = bLeft + boardWPt;
        float boardBottom  = bTop  + boardHPt;

        // Key arc coordinates (outer arc peak = bTop)
        float outerEndY   = ringCy - roPt * (float)cosA;   // Y of outer arc endpoints (below board top)
        float outerLeftX  = cx - roPt * (float)sinA;
        float outerRightX = cx + roPt * (float)sinA;
        float innerChordY  = ringCy - riPt * (float)cosA;
        float innerArcMidY = ringCy - riPt;
        float innerLeftX   = cx - riPt * (float)sinA;
        float innerRightX  = cx + riPt * (float)sinA;
        float midSection   = (outerEndY + innerChordY) / 2f;

        using var dimLine = StrokePaint("#555555", 0.5f);
        using var dimDash = StrokePaint("#8B5E1E", 0.5f, new[] { 3f, 2f });
        using var blk10  = TextPaint("#000000", 10f);
        using var blk8   = TextPaint("#000000", 8f);
        using var blk7   = TextPaint("#000000", 7f);
        using var gry7   = TextPaint("#555555", 7f);
        using var brn7   = TextPaint("#8B5E1E", 7f);
        using var nvy7   = TextPaint("#1A1A8C", 7f);
        using var nvy9   = TextPaint("#1A1A8C", 9f);
        using var red7   = TextPaint("#AA0000", 7f);

        // ── Title (above board top = outer arc peak) ───────────────────────────
        string titleStr = "SEGMENTED RING – ONE SEGMENT (CUTTING GUIDE)";
        canvas.DrawText(titleStr, cx - blk10.MeasureText(titleStr) / 2f, bTop - 30f, blk10);
        string subStr = "Outer arc peak at board top edge. Board shows waste areas.";
        canvas.DrawText(subStr, cx - gry7.MeasureText(subStr) / 2f, bTop - 18f, gry7);

        // Legend
        float legY = bTop - 5f;
        DrawPdfSwatch(canvas, cx - 90f, legY, "#D4A96A", "Wood to keep (final segment)", blk7);
        DrawPdfSwatch(canvas, cx + 20f, legY, "#E8624A", "Wood to cut out (waste)", blk7);

        // Outer arc label (inside top-corner waste area)
        string outerArcStr = "← OUTER ARC CUT (outer surface)";
        canvas.DrawText(outerArcStr, bLeft + 4f, (bTop + outerEndY) / 2f, red7);

        // ── Outer chord dim line (at outer arc endpoint level) ─────────────────
        canvas.DrawLine(outerLeftX,  outerEndY, outerRightX, outerEndY, dimDash);
        canvas.DrawLine(outerLeftX,  outerEndY - 4f, outerLeftX,  outerEndY + 4f, dimDash);
        canvas.DrawLine(outerRightX, outerEndY - 4f, outerRightX, outerEndY + 4f, dimDash);
        string ocStr = $"OUTER CHORD (Lo) = {data.OuterEdgeLength:F2} cm";
        canvas.DrawText(ocStr, cx - brn7.MeasureText(ocStr) / 2f, outerEndY + 8f, brn7);

        // ── Inner chord dim line ────────────────────────────────────────────────
        float beY = Math.Max(bTop + 5f, innerArcMidY - 12f);
        canvas.DrawLine(innerLeftX,  beY, innerRightX, beY, dimDash);
        canvas.DrawLine(innerLeftX,  beY, innerLeftX,  innerChordY, dimDash);
        canvas.DrawLine(innerRightX, beY, innerRightX, innerChordY, dimDash);
        string beStr = $"INNER CHORD (Li) = {data.InnerEdgeLength:F2} cm";
        canvas.DrawText(beStr, cx - brn7.MeasureText(beStr) / 2f, beY - 2f, brn7);

        // Inner arc label
        float innerLblY = (innerArcMidY + boardBottom) / 2f;
        canvas.DrawText("INNER ARC CUT →", boardRight - red7.MeasureText("INNER ARC CUT →") - 4f, innerLblY, red7);

        // ── Angle labels (outside left) ───────────────────────────────────────
        canvas.DrawText("MITER ANGLE",                   bLeft - 64f, midSection - 14f, nvy7);
        canvas.DrawText("Set saw to",                    bLeft - 64f, midSection - 4f,  nvy7);
        canvas.DrawText($"{data.MiterAngle:F0}°",        bLeft - 64f, midSection + 6f,  nvy9);
        canvas.DrawText("each end",                      bLeft - 64f, midSection + 16f, nvy7);

        float raX = boardRight + 70f;
        canvas.DrawText("MITER ANGLE",                   raX, midSection - 14f, nvy7);
        canvas.DrawText("Set saw to",                    raX, midSection - 4f,  nvy7);
        canvas.DrawText($"{data.MiterAngle:F0}°",        raX, midSection + 6f,  nvy9);
        canvas.DrawText("each end",                      raX, midSection + 16f, nvy7);

        // ── Board Width bracket (right side) ──────────────────────────────────
        float bwX = boardRight + 12f;
        using var bwLine = StrokePaint("#444444", 0.5f);
        canvas.DrawLine(bwX, bTop, bwX, boardBottom, bwLine);
        canvas.DrawLine(boardRight, bTop,        bwX + 4f, bTop, bwLine);
        canvas.DrawLine(boardRight, boardBottom, bwX + 4f, boardBottom, bwLine);
        float bwMidY = (bTop + boardBottom) / 2f;
        canvas.DrawText("BOARD WIDTH",  bwX + 6f, bwMidY - 8f, gry7);
        string bwVal = data.UserBoardWidthUsed > 0
            ? $"{boardWidthCm:F2} cm"
            : $"{minBW:F2} cm (min)";
        canvas.DrawText(bwVal,          bwX + 6f, bwMidY + 2f, gry7);

        // Minimum board depth dashed line (if board wider than min)
        if (boardWidthCm > minBW + 0.01f)
        {
            float minY = bTop + minBW * PtPerCm;
            using var minDash = StrokePaint("#AA5500", 0.5f, new[] { 4f, 2f });
            canvas.DrawLine(bLeft, minY, boardRight, minY, minDash);
            string minStr = $"─ min {minBW:F2} cm";
            canvas.DrawText(minStr, boardRight + 4f, minY + 4f, gry7);
        }

        // ── Footer ────────────────────────────────────────────────────────────
        string footer = $"Ring: {n} segments  ·  θ = {data.MiterAngle:F1}°  ·  Min board width: {minBW:F2} cm  ·  Scale 1:1";
        if (data.SegmentsPerBoard > 0)
            footer += $"  ·  {data.SegmentsPerBoard} per board  ·  offcut {data.BoardOffcut:F1} cm";
        canvas.DrawText(footer, cx - gry7.MeasureText(footer) / 2f, boardBottom + 16f, gry7);
    }

    private static void DrawPdfSwatch(SKCanvas canvas, float x, float y, string hex, string label, SKPaint textPaint)
    {
        using var fill    = new SKPaint { Color = SKColor.Parse(hex), IsAntialias = true };
        using var outline = StrokePaint("#000000", 0.3f);
        canvas.DrawRect(SKRect.Create(x, y - 7f, 12f, 8f), fill);
        canvas.DrawRect(SKRect.Create(x, y - 7f, 12f, 8f), outline);
        canvas.DrawText(label, x + 14f, y, textPaint);
    }

    // ─── Cushion (tiled A4 pages at 1:1 scale) ────────────────────────────────

    public static async Task<string> ExportCushionToPdfAsync(CushionOutput cushionData, string fileName, double dpi = 300)
    {
        try   { return await ExportCushionPdfWithBomAsync(cushionData, fileName); }
        catch { return await ExportCushionToPngAsync(cushionData, fileName); }
    }

    private static async Task<string> ExportCushionPdfWithBomAsync(CushionOutput cushionData, string fileName)
    {
        return await Task.Run(() =>
        {
            // A4 page in PDF points: 21 cm × 29.7 cm
            const float a4WPt = 21.0f * PtPerCm;   // ≈ 595.3 pts
            const float a4HPt = 29.7f * PtPerCm;   // ≈ 841.9 pts

            double overlapCm = Services.SettingsService.GetOverlapCmAsync().GetAwaiter().GetResult();
            float overlapPt  = (float)overlapCm * PtPerCm;

            float layoutWcm = (float)(cushionData.LayoutWidth  > 0 ? cushionData.LayoutWidth  : cushionData.Input.FinishedWidth);
            float layoutHcm = (float)(cushionData.LayoutHeight > 0 ? cushionData.LayoutHeight : cushionData.Input.FinishedDepth);
            float layoutWPt = layoutWcm * PtPerCm;
            float layoutHPt = layoutHcm * PtPerCm;

            int cols = (int)Math.Ceiling(layoutWPt / (a4WPt - overlapPt));
            int rows = (int)Math.Ceiling(layoutHPt / (a4HPt - overlapPt));

            // Render full layout as raster at 150 DPI (adequate for fabric cutting patterns)
            const float renderDpi  = 150f;
            float pxPerCm  = renderDpi / 2.54f;
            int layoutPxW = Math.Max(100, (int)(pxPerCm * layoutWcm));
            int layoutPxH = Math.Max(100, (int)(pxPerCm * layoutHcm));
            float ptToPx  = pxPerCm / PtPerCm;     // converts PDF pts → raster pixels

            var filePath = PdfPath(fileName);

            var fullInfo = new SKImageInfo(layoutPxW, layoutPxH);
            using var fullSurface = SKSurface.Create(fullInfo);
            CushionRenderService.Draw(fullSurface.Canvas, fullInfo, cushionData.Input, cushionData);
            using var fullImage = fullSurface.Snapshot();

            using var stream = File.OpenWrite(filePath);
            using var doc    = SKDocument.CreatePdf(stream);

            float cropMarkLen = 0.4f * PtPerCm;    // 4 mm crop marks

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    using var canvas = doc.BeginPage(a4WPt, a4HPt);
                    canvas.Clear(SKColors.White);

                    // Tile extent in PDF pts
                    float srcXPt = c * (a4WPt - overlapPt);
                    float srcYPt = r * (a4HPt - overlapPt);
                    float srcWPt = Math.Min(a4WPt, layoutWPt - srcXPt);
                    float srcHPt = Math.Min(a4HPt, layoutHPt - srcYPt);
                    if (srcWPt <= 0 || srcHPt <= 0) { doc.EndPage(); continue; }

                    // Convert to raster pixel source rect
                    var srcRect  = SKRectI.Round(new SKRect(
                        srcXPt * ptToPx, srcYPt * ptToPx,
                        (srcXPt + srcWPt) * ptToPx, (srcYPt + srcHPt) * ptToPx));
                    var destRect = new SKRect(0, 0, srcWPt, srcHPt);
                    canvas.DrawImage(fullImage, srcRect, destRect);

                    // Crop marks at tile corners
                    using var markPaint = StrokePaint("#000000", 0.4f);
                    float mL = cropMarkLen;
                    canvas.DrawLine(-mL, 0,     0,      0,     markPaint);
                    canvas.DrawLine(0,   -mL,   0,      0,     markPaint);
                    canvas.DrawLine(srcWPt, -mL, srcWPt, 0, markPaint);
                    canvas.DrawLine(srcWPt,  0, srcWPt + mL, 0, markPaint);
                    canvas.DrawLine(0,  srcHPt, 0, srcHPt + mL, markPaint);
                    canvas.DrawLine(-mL, srcHPt, 0, srcHPt,  markPaint);
                    canvas.DrawLine(srcWPt, srcHPt, srcWPt + mL, srcHPt, markPaint);
                    canvas.DrawLine(srcWPt, srcHPt, srcWPt, srcHPt + mL, markPaint);

                    // Page label
                    using var lblFont = TextPaint("#000000", 7f);
                    canvas.DrawText($"Page {r + 1},{c + 1}  of  {rows}×{cols}", 4, srcHPt - 4, lblFont);

                    DrawScaleBar(canvas, srcWPt, srcHPt);
                    doc.EndPage();
                }
            }

            AppendBomPage(doc, a4WPt, a4HPt, cushionData);
            doc.Close();
            return filePath;
        });
    }

    // ─── Shared drawing helpers ───────────────────────────────────────────────

    private static SKPath ClosedPetalPath(
        List<(double X, double Y)> pts, float centerXPt, float yOriginPt, float scale)
    {
        var path = new SKPath();
        if (pts.Count == 0) return path;
        path.MoveTo(centerXPt + (float)pts[0].X * scale, yOriginPt + (float)pts[0].Y * scale);
        for (int i = 1; i < pts.Count; i++)
            path.LineTo(centerXPt + (float)pts[i].X * scale, yOriginPt + (float)pts[i].Y * scale);
        for (int i = pts.Count - 1; i >= 0; i--)
            path.LineTo(centerXPt - (float)pts[i].X * scale, yOriginPt + (float)pts[i].Y * scale);
        path.Close();
        return path;
    }

    private static void DrawPetalDimensions(SKCanvas canvas, PetalOutput petalData,
                                            float centerXPt, float yOriginPt, float scale)
    {
        using var dashPaint = StrokePaint("#CC0000", 0.5f, new[] { 3.5f, 2.5f });
        using var textPaint = TextPaint("#CC0000", 7f);

        float topY  = yOriginPt;
        float botY  = yOriginPt + (float)petalData.ArcLength * scale;
        float midY  = (topY + botY) / 2f;
        float halfW = (float)(petalData.PetalWidth / 2.0) * scale;

        // Vertical centre line (L dimension)
        canvas.DrawLine(centerXPt, topY, centerXPt, botY, dashPaint);
        // Horizontal line at widest point (W dimension)
        canvas.DrawLine(centerXPt - halfW, midY, centerXPt + halfW, midY, dashPaint);

        // "L = x.x cm" — right of petal, above horizontal line
        canvas.DrawText($"L = {petalData.ArcLength:F1} cm", centerXPt + halfW + 3f, midY - 3f, textPaint);
        // "W = x.x cm" — below horizontal line, centred
        string wText = $"W = {petalData.PetalWidth:F1} cm";
        canvas.DrawText(wText, centerXPt - textPaint.MeasureText(wText) / 2f, midY + 9f, textPaint);
    }

    /// <summary>Draws a "| 1 cm |" scale bar in the bottom-right corner.</summary>
    private static void DrawScaleBar(SKCanvas canvas, float pageWidthPt, float pageHeightPt)
    {
        float barLen = PtPerCm;                              // exactly 1 cm
        float x1 = pageWidthPt - MarginPt - barLen;
        float x2 = pageWidthPt - MarginPt;
        float y  = pageHeightPt - MarginPt;

        using var barPaint = StrokePaint("#000000", 0.7f);
        canvas.DrawLine(x1, y,     x2, y,     barPaint);   // horizontal bar
        canvas.DrawLine(x1, y - 3, x1, y + 3, barPaint);   // left tick
        canvas.DrawLine(x2, y - 3, x2, y + 3, barPaint);   // right tick

        using var lbl = TextPaint("#000000", 5.5f);
        canvas.DrawText("1 cm", x1, y - 4f, lbl);
    }

    private static void AppendBomPage(SKDocument doc, float pageWidthPt, float pageHeightPt,
                                      CushionOutput cushionData)
    {
        using var canvas  = doc.BeginPage(pageWidthPt, pageHeightPt);
        using var heading = TextPaint("#000000", 14f);
        using var body    = TextPaint("#000000", 9f);

        float x = MarginPt, y = MarginPt + 16f;
        canvas.DrawText("Bill of Materials", x, y, heading);
        y += 18f;

        void Row(string label, string value)
        {
            canvas.DrawText(label, x,        y, body);
            canvas.DrawText(value, x + 200f, y, body);
            y += 13f;
        }

        var o = cushionData.OuterFabric;
        var i = cushionData.InnerFabric;
        var p = cushionData.Piping;
        var f = cushionData.FillMaterial;

        Row("Outer Fabric (sq in)",  $"{o.SquareInches:F1}");
        Row("Outer Fabric (yd)",     $"{o.LinearYards:F2}");
        Row("Inner Fabric (sq in)",  $"{i.SquareInches:F1}");
        Row("Inner Fabric (yd)",     $"{i.LinearYards:F2}");
        Row("Piping (yards)",        $"{p.PreMadePipingYards:F2}");
        Row("Fill Quantity",         $"{f.Quantity} {f.Unit}");
        Row("Total Material Cost",   $"${cushionData.TotalMaterialCost:F2}");
        Row("Material Efficiency",   $"{cushionData.MaterialEfficiency:F1}%");

        doc.EndPage();
    }

    // ─── Paint factories ──────────────────────────────────────────────────────

    private static SKPaint StrokePaint(string hex, float width, float[]? dash = null)
    {
        var p = new SKPaint
        {
            Color       = SKColor.Parse(hex),
            StrokeWidth = width,
            IsStroke    = true,
            IsAntialias = true,
        };
        if (dash != null) p.PathEffect = SKPathEffect.CreateDash(dash, 0);
        return p;
    }

    private static SKPaint TextPaint(string hex, float size) => new SKPaint
    {
        Color       = SKColor.Parse(hex),
        TextSize    = size,
        IsAntialias = true,
    };

    private static string PdfPath(string fileName) =>
        Path.Combine(FileSystem.AppDataDirectory, $"{fileName}.pdf");

    // ─── PNG fallback exports ─────────────────────────────────────────────────

    private static async Task<string> ExportCushionToPngAsync(CushionOutput cushionData, string fileName)
    {
        return await Task.Run(() =>
        {
            double layoutW = cushionData.LayoutWidth  > 0 ? cushionData.LayoutWidth  : cushionData.Input.FinishedWidth;
            double layoutH = cushionData.LayoutHeight > 0 ? cushionData.LayoutHeight : cushionData.Input.FinishedDepth;

            // Render at 150 DPI for PNG fallback
            const float renderDpi = 150f;
            float pxPerCm = renderDpi / 2.54f;
            int width  = Math.Max(200, (int)(pxPerCm * layoutW));
            int height = Math.Max(200, (int)(pxPerCm * layoutH));

            var info = new SKImageInfo(width, height);
            using var surface = SKSurface.Create(info);
            CushionRenderService.Draw(surface.Canvas, info, cushionData.Input, cushionData);

            using var image = surface.Snapshot();
            using var data  = image.Encode(SKEncodedImageFormat.Png, 100);

            var filePath = Path.Combine(FileSystem.AppDataDirectory, $"{fileName}.png");
            using var stream = File.OpenWrite(filePath);
            data.SaveTo(stream);
            return filePath;
        });
    }
}
