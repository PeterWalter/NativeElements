namespace NativeElements.Models;

public class PetalInput
{
    public double SphereDiameter { get; set; }
    public int NumberOfPetals { get; set; }
    public double Dpi { get; set; } = 300;
    public double SeamAllowance { get; set; } = 0.5; // cm
}

public class PetalOutput
{
    public double SphereDiameter { get; set; }  // Input diameter — used as title
    public double PetalWidth { get; set; }      // W = 2R·sin(π/n) — widest horizontal point
    public double ArcLength { get; set; }       // L = π·R — sewing line length (pole to pole)
    public double PetalHeight { get; set; }     // Same as ArcLength — used for rendering
    public double SeamAllowance { get; set; }   // Seam allowance in cm (added outward on curved edges)
    public int NumberOfPetals { get; set; }     // Number of petals
    public List<(double X, double Y)> CurvePoints { get; set; } = new();      // Right-side sewing line
    public List<(double X, double Y)> SeamCurvePoints { get; set; } = new();  // Right-side cut line (offset outward by SA)
    public double PixelsPerCm { get; set; }
}
