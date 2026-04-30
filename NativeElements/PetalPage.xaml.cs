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

    private async void OnCalculateClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!double.TryParse(DiameterEntry.Text, out double diameter) ||
                !int.TryParse(PetalsEntry.Text, out int petals) ||
                !double.TryParse(DpiEntry.Text, out double dpi) ||
                !double.TryParse(SeamEntry.Text, out double seam))
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
}
