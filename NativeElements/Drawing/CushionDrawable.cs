using NativeElements.Models;
using Microsoft.Maui.Graphics;

namespace NativeElements.Drawing;

public class CushionDrawable : IDrawable
{
    public CushionOutput? CushionData { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (CushionData == null)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);
            return;
        }

        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        // Draw outer rectangle (layout)
        double layoutWidth = CushionData.LayoutWidth > 0 ? CushionData.LayoutWidth : CushionData.Input.FinishedWidth;
        double layoutHeight = CushionData.LayoutHeight > 0 ? CushionData.LayoutHeight : CushionData.Input.FinishedDepth;

        float scale = (float)CushionData.PixelsPerCm;
        float canvasW = (float)layoutWidth * scale;
        float canvasH = (float)layoutHeight * scale;

        // Center the cushion on canvas
        float offsetX = (dirtyRect.Width - canvasW) / 2;
        float offsetY = (dirtyRect.Height - canvasH) / 2;

        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 2;

        // Outer boundary
        canvas.DrawRectangle(offsetX, offsetY, canvasW, canvasH);

        // Seam allowance line (inset)
        float seamPx = (float)CushionData.Input.SeamAllowance * scale;
        canvas.StrokeColor = Colors.Red;
        canvas.StrokeSize = 1;
        canvas.DrawRectangle(offsetX + seamPx, offsetY + seamPx, canvasW - 2 * seamPx, canvasH - 2 * seamPx);

        // Draw grid
        DrawGrid(canvas, dirtyRect, scale);
    }

    private void DrawGrid(ICanvas canvas, RectF dirtyRect, float pixelsPerCm)
    {
        canvas.StrokeColor = Color.FromArgb("#CCCCCC");
        canvas.StrokeSize = 0.5f;

        float gridSpacing = pixelsPerCm;

        for (float x = 0; x < dirtyRect.Width; x += gridSpacing)
            canvas.DrawLine(x, 0, x, dirtyRect.Height);

        for (float y = 0; y < dirtyRect.Height; y += gridSpacing)
            canvas.DrawLine(0, y, dirtyRect.Width, y);
    }
}
