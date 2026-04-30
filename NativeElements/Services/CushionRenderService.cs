using System;
using SkiaSharp;
using NativeElements.Models;

namespace NativeElements.Services
{
    public static class CushionRenderService
    {
        public static void Draw(SKCanvas canvas, SKImageInfo info, CushionInput input, dynamic output)
        {
            canvas.Clear(SKColors.White);

            var paint = new SKPaint
            {
                Color = SKColors.Black,
                StrokeWidth = 2,
                IsStroke = true,
                IsAntialias = true
            };

            // Padding and available area
            float padding = 20f;
            float availW = info.Width - 2 * padding;
            float availH = info.Height - 2 * padding;

            // Input dimensions (cm)
            float inputW = (float)Math.Max(1.0, input.FinishedWidth);
            float inputH = (float)Math.Max(1.0, input.FinishedDepth);

            // DPI-aware pixels per cm (default from input.Dpi)
            float pixelsPerCm = 118.11f; // fallback for 300 DPI            
            try { pixelsPerCm = (float)((input.Dpi > 0 ? input.Dpi : 300) / 2.54); } catch {}

            // Compute scale to fit the real-world size into available area while respecting DPI
            float scaleFactor = Math.Min(availW / (inputW * pixelsPerCm), availH / (inputH * pixelsPerCm));
            if (float.IsInfinity(scaleFactor) || scaleFactor <= 0) scaleFactor = 1f;
            float scale = pixelsPerCm * scaleFactor;

            float left = padding + (availW - inputW * scale) / 2f;
            float top = padding + (availH - inputH * scale) / 2f;

            var rect = new SKRect(left, top, left + inputW * scale, top + inputH * scale);

            // Draw outer fabric outline
            canvas.DrawRect(rect, paint);

            // Draw seam allowance as inner dashed rect if seam provided
            if (input.SeamAllowance > 0)
            {
                float seamPx = (float)input.SeamAllowance * scale;
                var inner = new SKRect(rect.Left + seamPx, rect.Top + seamPx, rect.Right - seamPx, rect.Bottom - seamPx);
                var seamPaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1.5f, IsStroke = true, PathEffect = SKPathEffect.CreateDash(new float[] { 8, 6 }, 0) };
                canvas.DrawRect(inner, seamPaint);
            }

            // Draw piping if present (red dashed) with piping width offset
            if (input.HasPiping)
            {
                float pipingPx = (float)input.PipingWidth * scale;
                var pipingRect = new SKRect(rect.Left + pipingPx / 2f, rect.Top + pipingPx / 2f, rect.Right - pipingPx / 2f, rect.Bottom - pipingPx / 2f);
                var pipingPaint = new SKPaint { Color = SKColors.Red, StrokeWidth = 2f, IsStroke = true, PathEffect = SKPathEffect.CreateDash(new float[] { 12, 6 }, 0) };
                canvas.DrawRect(pipingRect, pipingPaint);
            }

            // If layout is larger than available, draw tiling guides (split into pages)
            if (inputW * scale > availW || inputH * scale > availH)
            {
                int cols = (int)Math.Ceiling((inputW * scale) / availW);
                int rows = (int)Math.Ceiling((inputH * scale) / availH);
                var guidePaint = new SKPaint { Color = SKColors.DarkGray, StrokeWidth = 1, IsStroke = true, PathEffect = SKPathEffect.CreateDash(new float[] { 6, 4 }, 0) };
                for (int c = 1; c < cols; c++)
                {
                    float x = left + c * (availW);
                    canvas.DrawLine(x, top, x, top + inputH * scale, guidePaint);
                }
                for (int r = 1; r < rows; r++)
                {
                    float y = top + r * (availH);
                    canvas.DrawLine(left, y, left + inputW * scale, y, guidePaint);
                }
            }

            // Labels (dimensions)
            var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 16f, IsAntialias = true };
            var widthText = $"{input.FinishedWidth:F1} cm";
            var widthTextWidth = textPaint.MeasureText(widthText);
            canvas.DrawText(widthText, rect.MidX - widthTextWidth / 2f, rect.Bottom + 20f, textPaint);

            // Depth label (rotated)
            canvas.Save();
            canvas.Translate(rect.Right + 30f, rect.MidY + 6f);
            canvas.RotateDegrees(90);
            canvas.DrawText($"{input.FinishedDepth:F1} cm", 0, 0, textPaint);
            canvas.Restore();

            // If pattern repeat is set, draw repeat grid lines
            if (input.PatternRepeat > 0)
            {
                float repeatPx = (float)(input.PatternRepeat * pixelsPerCm * scaleFactor);
                var repeatPaint = new SKPaint { Color = SKColors.LightGray, StrokeWidth = 1f, IsStroke = true };
                for (float x = rect.Left + repeatPx; x < rect.Right; x += repeatPx)
                {
                    canvas.DrawLine(x, rect.Top, x, rect.Bottom, repeatPaint);
                }
                for (float y = rect.Top + repeatPx; y < rect.Bottom; y += repeatPx)
                {
                    canvas.DrawLine(rect.Left, y, rect.Right, y, repeatPaint);
                }
            }

            // Small legend
            var legendPaint = new SKPaint { Color = SKColors.Black, TextSize = 12f };
            canvas.DrawText("Outer = fabric outline", padding, info.Height - 40f, legendPaint);
            canvas.DrawText("Gray dashed = seam allowance", padding, info.Height - 26f, legendPaint);
            canvas.DrawText("Red dashed = piping (if any)", padding, info.Height - 12f, legendPaint);
        }
    }
}
