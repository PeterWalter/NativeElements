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

    // ─── Ring ─────────────────────────────────────────────────────────────────

    public static async Task<string> ExportRingToPdfAsync(SegmentedRingOutput ringData, string fileName, double dpi = 300)
    {
        return await Task.Run(() =>
        {
            float segAngle = (float)ringData.SegmentAngle;
            if (segAngle <= 0f || segAngle >= 360f) throw new InvalidOperationException("Invalid segment angle.");

            double angleRad = Math.PI * segAngle / 180.0;
            double sinHalf  = Math.Sin(angleRad / 2.0);
            if (Math.Abs(sinHalf) < 1e-9) throw new InvalidOperationException("Degenerate angle.");

            double outerRCm = ringData.OuterEdgeLength / (2.0 * sinHalf);
            double innerRCm = ringData.InnerEdgeLength  / (2.0 * sinHalf);
            if (outerRCm <= 0) throw new InvalidOperationException("Invalid ring dimensions.");
            if (innerRCm <= 0 || innerRCm >= outerRCm)
                innerRCm = Math.Max(0.1, outerRCm - Math.Max(0.1, ringData.RadialEdgeLength));

            float outerRPt = (float)outerRCm * PtPerCm;
            float innerRPt = (float)innerRCm * PtPerCm;

            // ---- Compact page: only the annular sector, not the full fan from centre ----
            // Sector points downward; bounding box of the annular part only:
            //   Width  = 2 × outerR × sin(θ/2)   (outer chord)
            //   Height = outerR − innerR × cos(θ/2)  (outer mid-arc to topmost inner arc points)
            float cosHalf        = (float)Math.Cos(angleRad / 2.0);
            float sectorWidthCm  = 2f * (float)outerRCm * (float)sinHalf;
            float topFromCtrCm   = (float)innerRCm * cosHalf;   // innermost visible Y below ring centre
            float sectorHeightCm = (float)outerRCm - topFromCtrCm;

            const float titleSizePt = 8f;
            const float titleGapPt  = 3f;
            float topReservePt = titleSizePt + titleGapPt;

            float pageWidthPt  = (sectorWidthCm  + 2f * MarginCm) * PtPerCm;
            float pageHeightPt = (sectorHeightCm + 2f * MarginCm) * PtPerCm + topReservePt;

            // Ring centre in page coordinates — sits above the top of the page so that
            // only the annular strip is visible (compact layout).
            float centerXPt = pageWidthPt / 2f;
            float centerYPt = MarginPt + topReservePt - topFromCtrCm * PtPerCm;

            var filePath = PdfPath(fileName);
            using var stream = File.OpenWrite(filePath);
            using var doc    = SKDocument.CreatePdf(stream);
            using var canvas = doc.BeginPage(pageWidthPt, pageHeightPt);
            canvas.Clear(SKColors.White);

            using var sectorPaint = StrokePaint("#000000", 0.75f);
            DrawAnnularSector(canvas, centerXPt, centerYPt, outerRPt, innerRPt,
                              90f - segAngle / 2f, segAngle, sectorPaint);

            // Radial dimension label on the right edge, mid-radial position
            float rightEdgeRad = (90f + segAngle / 2f) * (float)Math.PI / 180f;
            float midRPt = (innerRPt + outerRPt) / 2f;
            using var dimFont = TextPaint("#CC0000", 7f);
            canvas.DrawText($"H = {ringData.RadialEdgeLength:F1} cm",
                centerXPt + midRPt * (float)Math.Cos(rightEdgeRad) + 3f,
                centerYPt + midRPt * (float)Math.Sin(rightEdgeRad), dimFont);

            // Title immediately above inner arc
            float titleY = MarginPt + topReservePt - titleGapPt;
            int segCount = Math.Max(3, (int)Math.Round(360.0 / segAngle));
            using var titleFont = TextPaint("#000000", titleSizePt);
            string title = $"Ring  ·  {segCount} segments  ·  Scale 1:1";
            canvas.DrawText(title, centerXPt - titleFont.MeasureText(title) / 2f, titleY, titleFont);

            DrawScaleBar(canvas, pageWidthPt, pageHeightPt);

            doc.EndPage();
            doc.Close();
            return filePath;
        });
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

    private static void DrawAnnularSector(SKCanvas canvas,
        float cx, float cy, float outerR, float innerR,
        float startAngle, float sweepAngle, SKPaint paint)
    {
        float startRad = startAngle * (float)Math.PI / 180f;
        float endRad   = (startAngle + sweepAngle) * (float)Math.PI / 180f;

        var path = new SKPath();
        path.MoveTo(cx + outerR * (float)Math.Cos(startRad), cy + outerR * (float)Math.Sin(startRad));
        path.ArcTo(new SKRect(cx - outerR, cy - outerR, cx + outerR, cy + outerR),
                   startAngle, sweepAngle, false);
        path.LineTo(cx + innerR * (float)Math.Cos(endRad), cy + innerR * (float)Math.Sin(endRad));
        path.ArcTo(new SKRect(cx - innerR, cy - innerR, cx + innerR, cy + innerR),
                   startAngle + sweepAngle, -sweepAngle, false);
        path.Close();
        canvas.DrawPath(path, paint);
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
