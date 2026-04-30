using NativeElements.ViewModels;
using NativeElements.Models;

namespace NativeElements;

public partial class CushionCalculatorPage : ContentPage
{
    public CushionCalculatorPage()
    {
        InitializeComponent();
    }

    private CushionOutput? _lastCushionOutput;

    private async void OnPreviewClicked(object? sender, EventArgs e)
    {
        if (BindingContext is ViewModels.CushionViewModel vm)
        {
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
                Quantity = vm.Quantity
            };

            // Calculate output for preview
            switch (input.CushionType?.ToLowerInvariant())
            {
                case "throw":
                    _lastCushionOutput = CushionMathService.CalculateThrownCushion(input);
                    break;
                case "back":
                    _lastCushionOutput = CushionMathService.CalculateBackCushion(input);
                    break;
                case "seat":
                    _lastCushionOutput = CushionMathService.CalculateSeatCushion(input);
                    break;
                default:
                    _lastCushionOutput = CushionMathService.CalculateThrownCushion(input);
                    break;
            }

            var page = new CushionPreviewPage(input);
            var nav = Application.Current?.MainPage?.Navigation;
            if (nav != null)
            {
                await nav.PushAsync(page);
            }
            else
            {
                await Shell.Current.GoToAsync("/" );
            }
        }
    }

    private async void OnExportDxfClicked(object? sender, EventArgs e)
    {
        if (BindingContext is ViewModels.CushionViewModel vm)
        {
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
                Quantity = vm.Quantity
            };

            CushionOutput output = null;
            switch (input.CushionType?.ToLowerInvariant())
            {
                case "throw":
                    output = CushionMathService.CalculateThrownCushion(input);
                    break;
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

            _lastCushionOutput = output;

            try
            {
                // Generate cutting pieces (top/bottom/boxing) and export them as DXF
                var layout = NativeElements.Core.Services.CuttingLayoutService.GenerateBoxCushionLayout(input);
                string fileName = $"Cushion_Pieces_{DateTime.Now:yyyyMMdd_HHmmss}";
                var path = await DxfExportService.ExportCuttingLayoutToDxfAsync(layout, fileName);
                await DisplayAlertAsync("Export", $"DXF exported: {path}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Export error", ex.Message, "OK");
            }
        }
    }
}

