using NativeElements.ViewModels;
using NativeElements.Models;
using NativeElements.Services;
using NativeElements.Drawing;
using CommunityToolkit.Mvvm.Input;
using SkiaSharp;
using Microsoft.Maui.ApplicationModel;

namespace NativeElements;

public partial class CushionCalculatorPage : ContentPage
{
    private CushionDrawable? _cushionDrawable;

    public CushionCalculatorPage()
    {
        InitializeComponent();
        _cushionDrawable = new CushionDrawable();
        CushionPreview.Drawable = _cushionDrawable;
    }

    private CushionInput? _lastCushionInput;
    private CushionOutput? _lastCushionOutput;

    private async void OnCalculateAndPreviewClicked(object? sender, EventArgs e)
    {
        try
        {
            if (BindingContext is not CushionViewModel vm)
            {
                await DisplayAlertAsync("Error", "Unable to access CushionViewModel.", "OK");
                return;
            }

            if (vm.CalculateCommand is IAsyncRelayCommand asyncCommand)
            {
                await asyncCommand.ExecuteAsync(null);
            }
            else if (vm.CalculateCommand.CanExecute(null))
            {
                vm.CalculateCommand.Execute(null);
            }

            OnPreviewClicked(sender, e);
        }
        catch (Exception ex)
        {
            if (BindingContext is CushionViewModel vm)
            {
                vm.ResultText = $"Error: {ex.Message}";
            }
            await DisplayAlertAsync("Calculation Error", ex.Message, "OK");
        }
    }

    private void OnPreviewClicked(object? sender, EventArgs e)
    {
        try
        {
            if (BindingContext is not ViewModels.CushionViewModel vm)
            {
                return;
            }

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
            _lastCushionInput = input;

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

            // Update drawable and refresh preview
            if (_cushionDrawable != null && _lastCushionOutput != null)
            {
                _cushionDrawable.CushionData = _lastCushionOutput;
                CushionPreview.Invalidate();
            }
        }
        catch (Exception ex)
        {
            if (BindingContext is CushionViewModel vm)
            {
                vm.ResultText = $"Preview error: {ex.Message}";
            }
        }
    }

    private async void OnPrintClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not CushionViewModel vm)
        {
            await DisplayAlertAsync("Error", "Unable to access CushionViewModel.", "OK");
            return;
        }

        if (_lastCushionOutput == null)
        {
            OnPreviewClicked(sender, e);
        }

        if (_lastCushionOutput == null)
        {
            await DisplayAlertAsync("Error", "Please calculate/preview before printing.", "OK");
            return;
        }

        try
        {
            string fileName = $"cushion_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var path = await PdfExportService.ExportCushionToPdfAsync(_lastCushionOutput, fileName, 300);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Print Cushion Design",
                File = new ShareFile(path)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Print Failed", ex.Message, "OK");
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
            _lastCushionInput = input;

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


