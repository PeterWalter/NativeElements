using NativeElements.Models;
using NativeElements.Services;
using NativeElements.Data;
using System.Threading.Tasks;

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
}
