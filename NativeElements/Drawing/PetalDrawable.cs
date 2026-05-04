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

        float scale = (float)PetalData.PixelsPerCm;
        
        // Calculate petal bounds
        double maxWidth = PetalData.PetalWidth / 2;
        double maxHeight = PetalData.PetalHeight;

        float petalWidthPx = (float)maxWidth * scale;
        float petalHeightPx = (float)maxHeight * scale;

        // Calculate padding (10% on each side)
        float padding = 20;
        float availableWidth = dirtyRect.Width - 2 * padding;
        float availableHeight = dirtyRect.Height - 2 * padding;

        // Fit petal to available space
        float scaleX = availableWidth / petalWidthPx;
        float scaleY = availableHeight / petalHeightPx;
        float fitScale = Math.Min(scaleX, scaleY);

        // Adjust final dimensions
        float finalPetalWidthPx = petalWidthPx * fitScale;
        float finalPetalHeightPx = petalHeightPx * fitScale;

        // Center the petal
        float centerX = dirtyRect.Width / 2;
        float startY = padding + (availableHeight - finalPetalHeightPx) / 2;

        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 2;

        var path = new PathF();

        // Start at first point (top of petal)
        var firstPoint = PetalData.CurvePoints[0];
        path.MoveTo(
            centerX + (float)firstPoint.X * scale * fitScale,
            startY + (float)firstPoint.Y * scale * fitScale
        );

        // Trace right side
        for (int i = 1; i < PetalData.CurvePoints.Count; i++)
        {
            var point = PetalData.CurvePoints[i];
            path.LineTo(
                centerX + (float)point.X * scale * fitScale,
                startY + (float)point.Y * scale * fitScale
            );
        }

        // Trace left side (mirrored)
        for (int i = PetalData.CurvePoints.Count - 1; i >= 0; i--)
        {
            var point = PetalData.CurvePoints[i];
            path.LineTo(
                centerX - (float)point.X * scale * fitScale,
                startY + (float)point.Y * scale * fitScale
            );
        }

        path.Close();
        canvas.DrawPath(path);

        // Draw seam allowance lines
        DrawSeamAllowance(canvas, centerX, startY, finalPetalWidthPx, finalPetalHeightPx);

        // Draw center lines with dimensions
        DrawDimensionLines(canvas, centerX, startY, finalPetalHeightPx, finalPetalWidthPx);

        // Draw grid
        DrawGrid(canvas, dirtyRect, scale * fitScale);
    }

    private void DrawSeamAllowance(ICanvas canvas, float centerX, float startY, float widthPx, float heightPx)
    {
        if (PetalData?.SeamAllowance <= 0)
            return;

        float scale = (float)PetalData.PixelsPerCm;
        float arcLengthPx = (float)PetalData.ArcLength * scale;
        
        // Calculate how much of the height is seam allowance
        float seamAllowancePx = (float)PetalData.SeamAllowance * scale;

        canvas.StrokeColor = Color.FromArgb("#FFA500");  // Orange
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = new float[] { 4, 4 };

        // Top seam allowance line (horizontal dashed line)
        float topSeamY = startY + seamAllowancePx;
        canvas.DrawLine(centerX - widthPx / 2, topSeamY, centerX + widthPx / 2, topSeamY);

        // Bottom seam allowance line (horizontal dashed line)
        float bottomSeamY = startY + heightPx - seamAllowancePx;
        canvas.DrawLine(centerX - widthPx / 2, bottomSeamY, centerX + widthPx / 2, bottomSeamY);

        canvas.StrokeDashPattern = null;

        // Add seam allowance labels
        canvas.FontSize = 10;
        canvas.FontColor = Color.FromArgb("#FFA500");
        string seamText = $"SA: {PetalData.SeamAllowance:F1}cm";
        canvas.DrawString(seamText, centerX - widthPx / 2 - 40, topSeamY, HorizontalAlignment.Right);
    }

    private void DrawDimensionLines(ICanvas canvas, float centerX, float startY, float heightPx, float widthPx)
    {
        canvas.StrokeColor = Color.FromArgb("#FF0000");
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = new float[] { 2, 2 };

        float endY = startY + heightPx;
        float midY = startY + heightPx / 2;

        // Vertical center line (height) - at the center horizontally
        canvas.DrawLine(centerX, startY, centerX, endY);

        // Horizontal center line (width) - at the vertical middle of petal
        float halfWidth = widthPx / 2;
        canvas.DrawLine(centerX - halfWidth, midY, centerX + halfWidth, midY);

        canvas.StrokeDashPattern = null;

        // Draw dimension text
        canvas.FontSize = 12;
        canvas.FontColor = Colors.Red;

        // Height dimension - actual height value (from heightPx)
        double heightCm = PetalData?.PetalHeight ?? 0;
        string heightText = $"H: {heightCm:F1}cm";
        canvas.DrawString(heightText, centerX + 15, midY, HorizontalAlignment.Left);

        // Width dimension - actual width value (from widthPx, which represents full width)
        double widthCm = PetalData?.PetalWidth ?? 0;
        string widthText = $"W: {widthCm:F1}cm";
        canvas.DrawString(widthText, centerX, startY - 15, HorizontalAlignment.Center);
    }

    private void DrawGrid(ICanvas canvas, RectF dirtyRect, float pixelsPerCm)
    {
        canvas.StrokeColor = Color.FromArgb("#CCCCCC");
        canvas.StrokeSize = 0.5f;

        float gridSpacing = pixelsPerCm;

        // Vertical lines
        for (float x = 0; x < dirtyRect.Width; x += gridSpacing)
        {
            canvas.DrawLine(x, 0, x, dirtyRect.Height);
        }

        // Horizontal lines
        for (float y = 0; y < dirtyRect.Height; y += gridSpacing)
        {
            canvas.DrawLine(0, y, dirtyRect.Width, y);
        }
    }
}

