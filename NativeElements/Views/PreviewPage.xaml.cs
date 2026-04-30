using NativeElements.Models;
using NativeElements.Services;
using SkiaSharp;

namespace NativeElements.Views;

public partial class PreviewPage : ContentPage
{
    private PetalOutput? _currentPetalData;
    private SegmentedRingOutput? _currentRingData;
    private bool _isPetalMode = true;

    public PreviewPage()
    {
        InitializeComponent();
    }

    public void SetPetalData(PetalOutput petalData)
    {
        _currentPetalData = petalData;
        _isPetalMode = true;
        // Canvas rendering disabled - SkiaSharp.Views.Maui.Controls not available for .NET 10
        // CanvasView?.InvalidateSurface();
    }

    public void SetRingData(SegmentedRingOutput ringData)
    {
        _currentRingData = ringData;
        _isPetalMode = false;
        // Canvas rendering disabled - SkiaSharp.Views.Maui.Controls not available for .NET 10
        // CanvasView?.InvalidateSurface();
    }

    // Canvas rendering disabled - SkiaSharp.Views.Maui.Controls not available for .NET 10
    private void OnCanvasPaintSurface(object? sender, object e) { }

    private void DrawSegmentOnCanvas(SKCanvas canvas, float centerX, float centerY, float outerRadius, float innerRadius, float startAngle, float angleSpan, SKPaint paint)
    {
        var path = new SKPath();
        float startRad = (startAngle - 90) * (float)Math.PI / 180;
        float endRad = (startAngle + angleSpan - 90) * (float)Math.PI / 180;

        float x1 = centerX + outerRadius * (float)Math.Cos(startRad);
        float y1 = centerY + outerRadius * (float)Math.Sin(startRad);
        float x2 = centerX + outerRadius * (float)Math.Cos(endRad);
        float y2 = centerY + outerRadius * (float)Math.Sin(endRad);
        float x3 = centerX + innerRadius * (float)Math.Cos(endRad);
        float y3 = centerY + innerRadius * (float)Math.Sin(endRad);
        float x4 = centerX + innerRadius * (float)Math.Cos(startRad);
        float y4 = centerY + innerRadius * (float)Math.Sin(startRad);

        path.MoveTo(x1, y1);
        path.ArcTo(new SKRect(centerX - outerRadius, centerY - outerRadius, centerX + outerRadius, centerY + outerRadius), startAngle, angleSpan, false);
        path.LineTo(x3, y3);
        path.ArcTo(new SKRect(centerX - innerRadius, centerY - innerRadius, centerX + innerRadius, centerY + innerRadius), startAngle + angleSpan, -angleSpan, false);
        path.Close();

        canvas.DrawPath(path, paint);
    }

    private void OnRefreshClicked(object? sender, EventArgs e)
    {
        // Canvas rendering disabled - SkiaSharp.Views.Maui.Controls not available for .NET 10
        // CanvasView?.InvalidateSurface();
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Export", "Export functionality coming soon", "OK");
    }
}

