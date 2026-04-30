using NativeElements.Models;
using SkiaSharp;

namespace NativeElements.Services;

public class PdfExportService
{
    // Minimal, stable PDF exporter wrapper. Renders to PNG for now.

    public static async Task<string> ExportPetalToPdfAsync(PetalOutput petalData, string fileName, double dpi = 300)
    {
        return await ExportPetalToPngAsync(petalData, fileName, dpi);
    }

    public static async Task<string> ExportRingToPdfAsync(SegmentedRingOutput ringData, string fileName, double dpi = 300)
    {
        return await ExportRingToPngAsync(ringData, fileName, dpi);
    }

    public static async Task<string> ExportCushionToPdfAsync(CushionOutput cushionData, string fileName, double dpi = 300)
    {
        // Prefer PDF with BOM embedded. Fallback to PNG-only if PDF creation fails.
        try
        {
            return await ExportCushionPdfWithBomAsync(cushionData, fileName, dpi);
        }
        catch
        {
            return await ExportCushionToPngAsync(cushionData, fileName, dpi);
        }
    }

    private static async Task<string> ExportCushionPdfWithBomAsync(CushionOutput cushionData, string fileName, double dpi)
    {
        return await Task.Run(() =>
        {
            // Determine full layout size in cm (fallback to input dims)
            double layoutWcm = cushionData.LayoutWidth > 0 ? cushionData.LayoutWidth : cushionData.Input.FinishedWidth;
            double layoutHcm = cushionData.LayoutHeight > 0 ? cushionData.LayoutHeight : cushionData.Input.FinishedDepth;

            int layoutPxW = Math.Max(100, (int)(cushionData.PixelsPerCm * layoutWcm));
            int layoutPxH = Math.Max(100, (int)(cushionData.PixelsPerCm * layoutHcm));

            // Define a printable page size (A4 portrait) in cm and convert to pixels
            const double pageCmW = 21.0; // A4 width
            const double pageCmH = 29.7; // A4 height

            int pagePxW = Math.Max(600, (int)(cushionData.PixelsPerCm * pageCmW));
            int pagePxH = Math.Max(600, (int)(cushionData.PixelsPerCm * pageCmH));

            // Overlap between tiles (in cm) for later joining — read from settings (default 1.0 cm)
            double overlapCm = Services.SettingsService.GetOverlapCmAsync().GetAwaiter().GetResult();
            int overlapPx = (int)Math.Round(overlapCm * cushionData.PixelsPerCm);

            int cols = (int)Math.Ceiling((double)layoutPxW / (pagePxW - overlapPx));
            int rows = (int)Math.Ceiling((double)layoutPxH / (pagePxH - overlapPx));

            var docsPath = FileSystem.AppDataDirectory;
            var filePath = Path.Combine(docsPath, $"{fileName}.pdf");

            // Render full layout to a large surface once, then tile into pages to avoid re-calculating scaling per page.
            var fullInfo = new SKImageInfo(layoutPxW, layoutPxH);
            using (var fullSurface = SKSurface.Create(fullInfo))
            {
                var fullCanvas = fullSurface.Canvas;
                // Draw full layout at 1:1 pixel scale
                CushionRenderService.Draw(fullCanvas, fullInfo, cushionData.Input, cushionData);
                using (var fullImage = fullSurface.Snapshot())
                using (var fs = File.OpenWrite(filePath))
                using (var doc = SKDocument.CreatePdf(fs))
                {
                    // Create tiled pages
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            using (var canvas = doc.BeginPage(pagePxW, pagePxH))
                            {
                                // Calculate source rectangle with overlap margins
                                int srcX = c * (pagePxW - overlapPx);
                                int srcY = r * (pagePxH - overlapPx);
                                srcX = Math.Max(0, srcX);
                                srcY = Math.Max(0, srcY);

                                int srcW = Math.Min(pagePxW, layoutPxW - srcX);
                                int srcH = Math.Min(pagePxH, layoutPxH - srcY);

                                var srcRect = new SKRectI(srcX, srcY, srcX + srcW, srcY + srcH);
                                var destRect = new SKRect(0, 0, srcW, srcH);

                                canvas.Clear(SKColors.White);
                                // Draw the tile image
                                canvas.DrawImage(fullImage, srcRect, destRect);

                                // Draw crop/trim marks at edges (5mm length)
                                var markPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 1, IsAntialias = true };
                                int markLenPx = (int)Math.Max(3, 0.5 * cushionData.PixelsPerCm); // ~5mm

                                // Top-left
                                canvas.DrawLine(-markLenPx, 0, 0, 0, markPaint);
                                canvas.DrawLine(0, -markLenPx, 0, 0, markPaint);
                                // Top-right
                                canvas.DrawLine(srcW, -markLenPx, srcW + markLenPx, -markLenPx, markPaint);
                                canvas.DrawLine(srcW, -markLenPx, srcW, 0, markPaint);
                                // Bottom-left
                                canvas.DrawLine(-markLenPx, srcH, 0, srcH, markPaint);
                                canvas.DrawLine(0, srcH, 0, srcH + markLenPx, markPaint);
                                // Bottom-right
                                canvas.DrawLine(srcW, srcH, srcW + markLenPx, srcH, markPaint);
                                canvas.DrawLine(srcW, srcH, srcW, srcH + markLenPx, markPaint);

                                // Draw registration marks (small circles) near centres of edges
                                var regPaint = new SKPaint { Color = SKColors.Red, IsAntialias = true };
                                float cxTop = srcW / 2f;
                                float cyTop = 10;
                                canvas.DrawCircle(cxTop, cyTop, 4, regPaint);

                                float cxBottom = srcW / 2f;
                                float cyBottom = srcH - 10;
                                canvas.DrawCircle(cxBottom, cyBottom, 4, regPaint);

                                float cyLeft = srcH / 2f;
                                float cxLeft = 10;
                                canvas.DrawCircle(cxLeft, cyLeft, 4, regPaint);

                                float cyRight = srcH / 2f;
                                float cxRight = srcW - 10;
                                canvas.DrawCircle(cxRight, cyRight, 4, regPaint);

                                // Draw page index and tile info
                                var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 12, IsAntialias = true };
                                string pageLabel = $"Page {r + 1},{c + 1} ({rows}x{cols})";
                                canvas.DrawText(pageLabel, 10, srcH - 10, textPaint);

                                // Draw scale verification bar (100 mm) in bottom-right corner
                                int barLenPx = (int)Math.Round(10.0 * cushionData.PixelsPerCm); // 100 mm = 10 cm
                                int barX = srcW - barLenPx - 20;
                                int barY = srcH - 30;
                                canvas.DrawRect(barX, barY, barLenPx, 6, new SKPaint { Color = SKColors.Black, StrokeWidth = 1 });
                                canvas.DrawText("100 mm", barX, barY - 6, textPaint);

                                doc.EndPage();
                            }
                        }
                    }

                    // Final page: BOM text
                    using (var canvas = doc.BeginPage(pagePxW, pagePxH))
                    {
                        var paint = new SKPaint { Color = SKColors.Black, TextSize = 14, IsAntialias = true };
                        float x = 40, y = 60, lineHeight = 20;

                        canvas.DrawText("Bill of Materials", x, y, new SKPaint { Color = SKColors.Black, TextSize = 20, IsAntialias = true });
                        y += lineHeight * 2;

                        void DrawLine(string label, string value)
                        {
                            canvas.DrawText(label, x, y, paint);
                            canvas.DrawText(value, x + 300, y, paint);
                            y += lineHeight;
                        }

                        var o = cushionData.OuterFabric;
                        var i = cushionData.InnerFabric;
                        var p = cushionData.Piping;
                        var f = cushionData.FillMaterial;

                        DrawLine("Outer Fabric (sq in)", $"{o.SquareInches:F1}");
                        DrawLine("Outer Fabric (yd)", $"{o.LinearYards:F2}");
                        DrawLine("Inner Fabric (sq in)", $"{i.SquareInches:F1}");
                        DrawLine("Inner Fabric (yd)", $"{i.LinearYards:F2}");
                        DrawLine("Piping (yards)", $"{p.PreMadePipingYards:F2}");
                        DrawLine("Fill Quantity", $"{f.Quantity} {f.Unit}");
                        DrawLine("Total Material Cost", $"${cushionData.TotalMaterialCost:F2}");
                        DrawLine("Material Efficiency", $"{cushionData.MaterialEfficiency:F1}%");

                        doc.EndPage();
                    }

                    doc.Close();
                }
            }

            return filePath;
        });
    }

    private static async Task<string> ExportPetalToPngAsync(PetalOutput petalData, string fileName, double dpi)
    {
        return await Task.Run(() =>
        {
            int canvasWidth = Math.Max(200, (int)(petalData.PetalWidth * petalData.PixelsPerCm * 1.5));
            int canvasHeight = Math.Max(200, (int)(petalData.PetalHeight * petalData.PixelsPerCm * 1.5));

            var image = RenderService.RenderPetal(petalData, canvasWidth, canvasHeight, true);
            var docsPath = FileSystem.AppDataDirectory;
            var filePath = Path.Combine(docsPath, $"{fileName}.png");

            using (var stream = File.OpenWrite(filePath))
            {
                image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);
            }

            image.Dispose();
            return filePath;
        });
    }

    private static async Task<string> ExportRingToPngAsync(SegmentedRingOutput ringData, string fileName, double dpi)
    {
        return await Task.Run(() =>
        {
            int size = Math.Max(200, (int)(ringData.PixelsPerCm * (ringData.RadialEdgeLength * 2) * 1.5));
            var image = RenderService.RenderSegmentedRing(ringData, size, size, true);
            var docsPath = FileSystem.AppDataDirectory;
            var filePath = Path.Combine(docsPath, $"{fileName}.png");

            using (var stream = File.OpenWrite(filePath))
            {
                image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);
            }

            image.Dispose();
            return filePath;
        });
    }

    private static async Task<string> ExportCushionToPngAsync(CushionOutput cushionData, string fileName, double dpi)
    {
        return await Task.Run(() =>
        {
            double layoutW = cushionData.LayoutWidth > 0 ? cushionData.LayoutWidth : cushionData.Input.FinishedWidth;
            double layoutH = cushionData.LayoutHeight > 0 ? cushionData.LayoutHeight : cushionData.Input.FinishedDepth;

            int width = Math.Max(200, (int)(cushionData.PixelsPerCm * layoutW * 1.5));
            int height = Math.Max(200, (int)(cushionData.PixelsPerCm * layoutH * 1.5));

            var info = new SKImageInfo(width, height);
            using (var surface = SKSurface.Create(info))
            {
                var canvas = surface.Canvas;
                CushionRenderService.Draw(canvas, info, cushionData.Input, cushionData);

                using (var image = surface.Snapshot())
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                {
                    var docsPath = FileSystem.AppDataDirectory;
                    var filePath = Path.Combine(docsPath, $"{fileName}.png");
                    using (var stream = File.OpenWrite(filePath))
                    {
                        data.SaveTo(stream);
                    }
                    return filePath;
                }
            }
        });
    }
}
