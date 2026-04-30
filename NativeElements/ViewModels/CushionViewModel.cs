using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NativeElements.Models;
using NativeElements.Services;
using NativeElements.Data;
using System.Threading.Tasks;

namespace NativeElements.ViewModels;

public partial class CushionViewModel : ObservableObject
{
    [ObservableProperty]
    private string cushionType = "Throw";

    [ObservableProperty]
    private double widthCm = 45.72; // default 18"

    [ObservableProperty]
    private double depthCm = 45.72;

    [ObservableProperty]
    private double boxedHeightCm = 5.08; // default 2"

    [ObservableProperty]
    private double seamAllowanceCm = 1.27; // default 0.5"

    [ObservableProperty]
    private double shrinkageFactor = 0.05; // 5%

    [ObservableProperty]
    private bool hasPiping = false;

    [ObservableProperty]
    private bool hasInnerLining = false;

    [ObservableProperty]
    private int quantity = 1;

    [ObservableProperty]
    private string resultText = string.Empty;

    public CushionViewModel()
    {
    }

    [RelayCommand]
    public async Task Calculate()
    {
        try
        {
            var input = new CushionInput
            {
                CushionType = CushionType,
                FinishedWidth = WidthCm,
                FinishedDepth = DepthCm,
                BoxedHeight = BoxedHeightCm,
                SeamAllowance = SeamAllowanceCm,
                ShrinkageFactor = ShrinkageFactor,
                HasPiping = HasPiping,
                HasInnerLining = HasInnerLining,
                Quantity = Quantity
            };

            // Run heavy math on background thread
            CushionOutput output = await Task.Run(() =>
            {
                switch (CushionType?.ToLowerInvariant())
                {
                    case "throw":
                        return CushionMathService.CalculateThrownCushion(input);
                    case "back":
                        return CushionMathService.CalculateBackCushion(input);
                    case "seat":
                        return CushionMathService.CalculateSeatCushion(input);
                    default:
                        return CushionMathService.CalculateThrownCushion(input);
                }
            });

            // Update UI-bound property on main thread (continuation runs on UI thread)
            ResultText = $"Outer Fabric: {output.TotalOuterFabricYards:F2} yd\n" +
                         $"Inner Fabric: {output.TotalInnerFabricYards:F2} yd\n" +
                         $"Piping: {output.TotalPipingYards:F2} yd\n" +
                         $"Efficiency: {output.MaterialEfficiency:F0}%";

            // Save to DB (await so any errors surface)
            var history = new CalculationHistory
            {
                Type = "Cushion",
                InputParams = System.Text.Json.JsonSerializer.Serialize(input),
                OutputParams = System.Text.Json.JsonSerializer.Serialize(output),
                Timestamp = DateTime.UtcNow
            };

            await DatabaseService.InsertAsync(history);
        }
        catch (System.Exception ex)
        {
            // Ensure UI update happens on main thread
            ResultText = "Error: " + ex.Message;
        }
    }
}
