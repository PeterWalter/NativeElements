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
    public double PetalWidth { get; set; } // Width at widest point
    public double ArcLength { get; set; }
    public double PetalHeight { get; set; } // Total height
    public double SeamAllowance { get; set; } // Seam allowance in cm
    public List<(double X, double Y)> CurvePoints { get; set; } = new();
    public double PixelsPerCm { get; set; }
}
