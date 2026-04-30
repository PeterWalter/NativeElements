using NativeElements.Models;
using NativeElements.Services;
using NativeElements.Data;
using System.Threading.Tasks;

namespace NativeElements;

public partial class MainPage : TabbedPage
{
    public MainPage()
    {
        InitializeComponent();
    }

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

    private async void OnCalculateRingClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!double.TryParse(OuterRadiusEntry.Text, out double outer) ||
                !double.TryParse(InnerRadiusEntry.Text, out double inner) ||
                !int.TryParse(SegmentsEntry.Text, out int segments) ||
                !double.TryParse(RingDpiEntry.Text, out double dpi))
            {
                RingResultLabel.Text = "Please enter valid numbers";
                return;
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

    private async void OnRefreshHistoryClicked(object? sender, EventArgs e)
    {
        try
        {
            var history = await DatabaseService.GetAllAsync<CalculationHistory>();
            HistoryCollectionView.ItemsSource = history.OrderByDescending(h => h.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load history: {ex.Message}", "OK");
        }
    }

    private async void OnClearHistoryClicked(object? sender, EventArgs e)
    {
        bool confirmed = await DisplayAlertAsync("Clear History", "Delete all calculation history?", "Yes", "No");
        if (!confirmed) return;

        try
        {
            var history = await DatabaseService.GetAllAsync<CalculationHistory>();
            foreach (var item in history)
            {
                await DatabaseService.DeleteAsync(item);
            }
            HistoryCollectionView.ItemsSource = null;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to clear history: {ex.Message}", "OK");
        }
    }
}
