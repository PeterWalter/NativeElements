using NativeElements.Models;
using NativeElements.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace NativeElements;

public partial class CushionCalculatorPage : ContentPage
{
    private async void OnExportClicked(object? sender, EventArgs e)
    {
        if (BindingContext is ViewModels.CushionViewModel vm)
        {
            // Build input from ViewModel
            var input = new CushionInput
            {
                CushionType = vm.CushionType,
                FinishedWidth = vm.WidthCm,
                FinishedDepth = vm.DepthCm,
                BoxedHeight = vm.BoxedHeightCm,
                SeamAllowance = vm.SeamAllowanceCm,
                ShrinkageFactor = vm.ShrinkageFactor,
                HasPiping = vm.HasPiping,
                HasInnerLining = vm.HasInnerLining,
                Quantity = vm.Quantity,
                Dpi = 300
            };

            CushionOutput output;
            switch ((input.CushionType ?? "").ToLowerInvariant())
            {
                case "back":
                    output = CushionMathService.CalculateBackCushion(input);
                    break;
                case "seat":
                    output = CushionMathService.CalculateSeatCushion(input);
                    break;
                default:
                    output = CushionMathService.CalculateThrownCushion(input);
                    break;
            }

            _lastCushionInput = input;
            _lastCushionOutput = output;
            // Canvas rendering disabled - SkiaSharp.Views.Maui.Controls not available for .NET 10
            // CushionCanvas?.InvalidateSurface();

            string fileName = $"cushion_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            try
            {
                var path = await PdfExportService.ExportCushionToPdfAsync(output, fileName, vm.ShrinkageFactor > 0 ? 300 : 300);
                await DisplayAlert("Export Complete", $"Saved to: {path}", "OK");
                // Attempt to open the file if platform supports it
                try
                {
                    await Launcher.OpenAsync(new OpenFileRequest("Open Export", new ReadOnlyFile(path)));
                }
                catch { /* ignore open errors */ }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Export Failed", ex.Message, "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", "Unable to access CushionViewModel.", "OK");
        }
    }
}
