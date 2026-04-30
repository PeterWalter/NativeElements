using NativeElements.Models;
using NativeElements.Services;
using NativeElements.Data;
using System.Threading.Tasks;
using SkiaSharp;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace NativeElements;

public partial class PetalPage : ContentPage
{
    public PetalPage()
    {
        InitializeComponent();
    }

    private PetalOutput? _lastOutput;

    private async void OnCalculateClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!double.TryParse(DiameterEntry.Text, out double diameter) ||
                !int.TryParse(PetalsEntry.Text, out int petals))
            {
                ResultLabel.Text = "Please enter valid numbers";
                return;
            }

            double dpi;
            if (!double.TryParse(DpiEntry.Text, out dpi))
            {
                dpi = await Services.SettingsService.GetDpiAsync();
            }

            if (!double.TryParse(SeamEntry.Text, out double seam))
            {
                ResultLabel.Text = "Please enter valid numbers";
                return;
            }

            var input = new PetalInput
            {
                SphereDiameter = diameter,
                NumberOfPetals = petals,
                Dpi = dpi,
                SeamAllowance = seam
            };

            var output = PetalMathService.CalculatePetal(input);
            _lastOutput = output;
            // Canvas rendering disabled - SkiaSharp.Views.Maui.Controls not available for .NET 10
            // PetalCanvas?.InvalidateSurface();

            // Show results immediately
            ResultLabel.Text = $"Petal Width: {output.PetalWidth:F2} cm\n" +
                             $"Arc Length: {output.ArcLength:F2} cm\n" +
                             $"Height: {output.PetalHeight:F2} cm\n" +
                             $"Pixels/cm: {output.PixelsPerCm:F2}";

            // Save to history (do not block UI)
            var history = new CalculationHistory
            {
                Type = "Petal",
                InputParams = System.Text.Json.JsonSerializer.Serialize(input),
                OutputParams = System.Text.Json.JsonSerializer.Serialize(output),
                Timestamp = DateTime.UtcNow
            };

            _ = Task.Run(async () => await DatabaseService.InsertAsync(history));
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"Error: {ex.Message}";
        }
    }

    private async void OnExportDxfClicked(object? sender, EventArgs e)
    {
        if (_lastOutput == null)
        {
            ResultLabel.Text = "Please calculate first before exporting.";
            return;
        }

        try
        {
            string fileName = $"Petal_{DateTime.Now:yyyyMMdd_HHmmss}";
            var path = await DxfExportService.ExportPetalToDxfAsync(_lastOutput, fileName);
            ResultLabel.Text = $"DXF exported: {path}";
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"Export error: {ex.Message}";
        }
    }

    private async void OnExportPdfClicked(object? sender, EventArgs e)
    {
        if (_lastOutput == null)
        {
            ResultLabel.Text = "Please calculate first before exporting.";
            return;
        }

        try
        {
            string fileName = $"Petal_{DateTime.Now:yyyyMMdd_HHmmss}";
            var path = await PdfExportService.ExportPetalToPdfAsync(_lastOutput, fileName);
            ResultLabel.Text = $"PDF exported: {path}";
            await Launcher.OpenAsync(new OpenFileRequest("Open Export", new ReadOnlyFile(path)));
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"PDF export error: {ex.Message}";
        }
    }

    private async void OnPrintClicked(object? sender, EventArgs e)
    {
        if (_lastOutput == null)
        {
            ResultLabel.Text = "Please calculate first before printing.";
            return;
        }

        try
        {
            await PrintService.PrintPetalAsync(_lastOutput);
            ResultLabel.Text = "Print/share dialog opened.";
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"Print error: {ex.Message}";
        }
    }

    // Canvas rendering disabled - SkiaSharp.Views.Maui.Controls not available for .NET 10
    // private void OnPetalCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e) { }
}
