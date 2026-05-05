using NativeElements.Models;
using NativeElements.Services;
using NativeElements.Data;
using NativeElements.Drawing;
using System.Threading.Tasks;
using SkiaSharp;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace NativeElements;

public partial class SegmentedRingPage : ContentPage
{
    private RingDrawable? _ringDrawable;
    private RingAssemblyDrawable? _assemblyDrawable;
    private bool _showAssembly = false;

    public SegmentedRingPage()
    {
        InitializeComponent();
        _ringDrawable = new RingDrawable();
        _assemblyDrawable = new RingAssemblyDrawable();
        RingPreview.Drawable = _ringDrawable;
    }

    private SegmentedRingOutput? _lastRingOutput;

    private async void OnCalculateRingClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!double.TryParse(OuterRadiusEntry.Text, out double outer) ||
                !double.TryParse(InnerRadiusEntry.Text, out double inner) ||
                !int.TryParse(SegmentsEntry.Text, out int segments))
            {
                RingResultLabel.Text = "Please enter valid numbers";
                return;
            }

            double dpi;
            if (!double.TryParse(RingDpiEntry.Text, out dpi))
            {
                dpi = await Services.SettingsService.GetDpiAsync();
            }

            if (inner >= outer)
            {
                RingResultLabel.Text = "Inner radius must be less than outer radius";
                return;
            }

            var input = new SegmentedRingInput
            {
                OuterRadius      = outer,
                InnerRadius      = inner,
                NumberOfSegments = segments,
                Dpi              = dpi,
                BoardLength      = double.TryParse(BoardLengthEntry.Text,  out double bl) && bl > 0 ? bl : 0,
                BoardWidth       = double.TryParse(BoardWidthEntry.Text,   out double bw) && bw > 0 ? bw : 0,
            };

            var output = SegmentedRingMathService.CalculateSegment(input);
            _lastRingOutput = output;

            // Update both drawables with new data
            if (_ringDrawable != null)
            {
                _ringDrawable.RingData = output;
            }
            if (_assemblyDrawable != null)
            {
                _assemblyDrawable.RingData = output;
            }

            // Show single segment view by default after calculation
            _showAssembly = false;
            if (_ringDrawable != null)
            {
                RingPreview.Drawable = _ringDrawable;
                RingPreview.Invalidate();
            }

            // Show results immediately
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Segment Angle:  {output.SegmentAngle:F2}°");
            sb.AppendLine($"Miter Angle (θ): {output.MiterAngle:F2}°");
            sb.AppendLine($"Outer Chord (Lo): {output.OuterEdgeLength:F3} cm  |  Outer Arc: {output.OuterArcLength:F3} cm");
            sb.AppendLine($"Inner Chord (Li): {output.InnerEdgeLength:F3} cm  |  Inner Arc: {output.InnerArcLength:F3} cm");
            sb.Append(    $"Radial Width (W): {output.RadialEdgeLength:F3} cm");
            sb.AppendLine();
            sb.AppendLine($"─────────────────────────────────────");
            sb.Append(    $"Min board width needed: {output.MinBoardWidth:F2} cm");
            if (!output.BoardWidthFits)
            {
                sb.AppendLine();
                sb.Append(    $"⚠ Board too narrow! Your board ({output.UserBoardWidthUsed:F1} cm) < min ({output.MinBoardWidth:F2} cm)");
            }
            if (output.SegmentsPerBoard > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"─────────────────────────────────────");
                sb.AppendLine($"Board Length: {output.BoardLengthUsed:F1} cm");
                sb.AppendLine($"Segments per board: {output.SegmentsPerBoard}");
                sb.Append(    $"Offcut: {output.BoardOffcut:F2} cm");
            }
            RingResultLabel.Text = sb.ToString();

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

    private async void OnExportRingDxfClicked(object? sender, EventArgs e)
    {
        if (_lastRingOutput == null)
        {
            RingResultLabel.Text = "Please calculate first before exporting.";
            return;
        }

        try
        {
            string fileName = $"Ring_{DateTime.Now:yyyyMMdd_HHmmss}";
            var path = await DxfExportService.ExportSegmentedRingToDxfAsync(_lastRingOutput, fileName);
            RingResultLabel.Text = $"DXF exported: {path}";
        }
        catch (Exception ex)
        {
            RingResultLabel.Text = $"Export error: {ex.Message}";
        }
    }

    private async void OnExportRingPdfClicked(object? sender, EventArgs e)
    {
        if (_lastRingOutput == null)
        {
            RingResultLabel.Text = "Please calculate first before exporting.";
            return;
        }

        try
        {
            string fileName = $"Ring_{DateTime.Now:yyyyMMdd_HHmmss}";
            var path = await PdfExportService.ExportRingToPdfAsync(_lastRingOutput, fileName);
            RingResultLabel.Text = $"PDF exported: {path}";
            await Launcher.OpenAsync(new OpenFileRequest("Open Export", new ReadOnlyFile(path)));
        }
        catch (Exception ex)
        {
            RingResultLabel.Text = $"PDF export error: {ex.Message}";
        }
    }

    private async void OnPrintRingClicked(object? sender, EventArgs e)
    {
        if (_lastRingOutput == null)
        {
            RingResultLabel.Text = "Please calculate first before printing.";
            return;
        }

        try
        {
            await PrintService.PrintRingAsync(_lastRingOutput);
            RingResultLabel.Text = "Print/share dialog opened.";
        }
        catch (Exception ex)
        {
            RingResultLabel.Text = $"Print error: {ex.Message}";
        }
    }

    private void OnToggleViewClicked(object? sender, EventArgs e)
    {
        if (_lastRingOutput == null)
        {
            RingResultLabel.Text = "Please calculate first before toggling view.";
            return;
        }

        _showAssembly = !_showAssembly;
        ToggleViewButton.Text = _showAssembly ? "Show Segment" : "Show Assembly";
        
        if (_showAssembly && _assemblyDrawable != null)
        {
            RingPreview.Drawable = _assemblyDrawable;
        }
        else if (_ringDrawable != null)
        {
            RingPreview.Drawable = _ringDrawable;
        }

        RingPreview.Invalidate();
    }
}


