using NativeElements.Models;
using NativeElements.Services;
using NativeElements.Data;
using System.Threading.Tasks;

namespace NativeElements;

public partial class SegmentedRingPage : ContentPage
{
    public SegmentedRingPage()
    {
        InitializeComponent();
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
}
