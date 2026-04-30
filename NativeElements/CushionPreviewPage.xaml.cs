using SkiaSharp;
using NativeElements.Models;
using NativeElements.Services;

namespace NativeElements;

public partial class CushionPreviewPage : ContentPage
{
    private readonly CushionInput _input;
    private readonly dynamic _output;

    public CushionPreviewPage(CushionInput input)
    {
        InitializeComponent();
        _input = input ?? throw new ArgumentNullException(nameof(input));

        // Calculate output for preview using math service
        switch ((_input.CushionType ?? "Throw").ToLowerInvariant())
        {
            case "back":
                _output = CushionMathService.CalculateBackCushion(_input);
                break;
            case "seat":
                _output = CushionMathService.CalculateSeatCushion(_input);
                break;
            default:
                _output = CushionMathService.CalculateThrownCushion(_input);
                break;
        }
    }

    private void OnCanvasViewPaintSurface(object sender, object e)
    {
        // Canvas rendering disabled - SkiaSharp.Views.Maui.Controls not available for .NET 10
    }
}

