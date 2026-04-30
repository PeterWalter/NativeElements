using NativeElements.Models;
using NativeElements.Services;
using NativeElements.Data;
using System.Threading.Tasks;
using SkiaSharp;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace NativeElements;

public partial class SegmentedRingPage : ContentPage
{
    public SegmentedRingPage()
    {
        InitializeComponent();
    }

    private SegmentedRingOutput? _lastRingOutput;

    private async void OnCalculateRingClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!double.TryParse(OuterRadiusEntry.Text, out double outer) ||
                !double.TryParse(InnerRadiusEntry.Text, out double inner) ||
                !int.TryParse(SegmentsEntry.Text, out int segments))
            {
                RingResultLabel.Text = "Please enter valid numbers";
                return;
            }

            double dpi;
            if (!double.TryParse(RingDpiEntry.Text, out dpi))
            {
                dpi = await Services.SettingsService.GetDpiAsync();
            }

            if (inner >= outer)
            {
                RingResultLabel.Text = "Inner radius must be less than outer radius";
                return;
            }

            var input = new SegmentedRingInput
            {
                OuterRadius = outer,
                InnerRadius = inner,
                NumberOfSegments = segments,
                Dpi = dpi
            };

            var output = SegmentedRingMathService.CalculateSegment(input);
            _lastRingOutput = output;
            // Canvas rendering disabled - SkiaSharp.Views.Maui.Controls not available for .NET 10
            // RingCanvas?.InvalidateSurface();

            // Show results immediately
            RingResultLabel.Text = $"Segment Angle: {output.SegmentAngle:F2}°\n" +
                                  $"Outer Edge: {output.OuterEdgeLength:F2} cm\n" +
                                  $"Inner Edge: {output.InnerEdgeLength:F2} cm\n" +
                                  $"Radial Edge: {output.RadialEdgeLength:F2} cm\n" +
                                  $"Pixels/cm: {output.PixelsPerCm:F2}";

            // Save to history (do not block UI)
            var history = new CalculationHistory
            {
                Type = "SegmentedRing",
                InputParams = System.Text.Json.JsonSerializer.Serialize(input),
                OutputParams = System.Text.Json.JsonSerializer.Serialize(output),
                Timestamp = DateTime.UtcNow
            };

            _ = Task.Run(async () => await DatabaseService.InsertAsync(history));
        }
        catch (Exception ex)
        {
            RingResultLabel.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnExportRingDxfClicked(object? sender, EventArgs e)
    {
        if (_lastRingOutput == null)
        {
            RingResultLabel.Text = "Please calculate first before exporting.";
            return;
        }

        try
        {
            string fileName = $"Ring_{DateTime.Now:yyyyMMdd_HHmmss}";
            var path = await DxfExportService.ExportSegmentedRingToDxfAsync(_lastRingOutput, fileName);
            RingResultLabel.Text = $"DXF exported: {path}";
        }
        catch (Exception ex)
        {
            RingResultLabel.Text = $"Export error: {ex.Message}";
        }
    }

    private async void OnExportRingPdfClicked(object? sender, EventArgs e)
    {
        if (_lastRingOutput == null)
        {
            RingResultLabel.Text = "Please calculate first before exporting.";
            return;
        }

        try
        {
            string fileName = $"Ring_{DateTime.Now:yyyyMMdd_HHmmss}";
            var path = await PdfExportService.ExportRingToPdfAsync(_lastRingOutput, fileName);
            RingResultLabel.Text = $"PDF exported: {path}";
            await Launcher.OpenAsync(new OpenFileRequest("Open Export", new ReadOnlyFile(path)));
        }
        catch (Exception ex)
        {
            RingResultLabel.Text = $"PDF export error: {ex.Message}";
        }
    }

    private async void OnPrintRingClicked(object? sender, EventArgs e)
    {
        if (_lastRingOutput == null)
        {
            RingResultLabel.Text = "Please calculate first before printing.";
            return;
        }

        try
        {
            await PrintService.PrintRingAsync(_lastRingOutput);
            RingResultLabel.Text = "Print/share dialog opened.";
        }
        catch (Exception ex)
        {
            RingResultLabel.Text = $"Print error: {ex.Message}";
        }
    }

    // Canvas rendering disabled - SkiaSharp.Views.Maui.Controls not available for .NET 10
    private void OnRingCanvasPaintSurface(object? sender, object e) { }
}

