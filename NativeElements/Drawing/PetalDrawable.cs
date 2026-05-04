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

        // Draw center lines with dimensions
        DrawDimensionLines(canvas, centerX, startY, finalPetalHeightPx, finalPetalWidthPx);

        // Draw grid
        DrawGrid(canvas, dirtyRect, scale * fitScale);
    }

    private void DrawDimensionLines(ICanvas canvas, float centerX, float startY, float heightPx, float widthPx)
    {
        canvas.StrokeColor = Color.FromArgb("#FF0000");
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = new float[] { 2, 2 };

        float endY = startY + heightPx;

        // Vertical center line (height)
        canvas.DrawLine(centerX, startY, centerX, endY);

        // Horizontal center line (width)
        canvas.DrawLine(centerX - widthPx / 2, startY + heightPx / 2, centerX + widthPx / 2, startY + heightPx / 2);

        canvas.StrokeDashPattern = null;

        // Draw dimension text
        canvas.FontSize = 12;
        canvas.FontColor = Colors.Red;

        // Height dimension
        string heightText = $"H:{(heightPx / (float)118.11):F1}cm";
        canvas.DrawString(heightText, centerX + 10, startY + heightPx / 2, HorizontalAlignment.Left);

        // Width dimension
        string widthText = $"W:{(widthPx / (float)118.11):F1}cm";
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

