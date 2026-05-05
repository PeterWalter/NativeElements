namespace NativeElements.Models;

public class SegmentedRingInput
{
    public double OuterRadius { get; set; }
    public double InnerRadius { get; set; }
    public int NumberOfSegments { get; set; }
    public double Dpi { get; set; } = 300;
}

public class SegmentedRingOutput
{
    /// <summary>Central angle subtended by each segment (degrees). = 360 / N</summary>
    public double SegmentAngle { get; set; }

    /// <summary>Half of segment angle = saw miter angle (degrees).</summary>
    public double MiterAngle { get; set; }

    /// <summary>Outer radius of the ring (cm).</summary>
    public double OuterRadius { get; set; }

    /// <summary>Inner radius of the ring (cm).</summary>
    public double InnerRadius { get; set; }

    /// <summary>Outer chord (straight-line) length Lo (cm).</summary>
    public double OuterEdgeLength { get; set; }

    /// <summary>Inner chord (straight-line) length Li (cm).</summary>
    public double InnerEdgeLength { get; set; }

    /// <summary>Outer arc length (cm) — what the saw actually cuts along the curve.</summary>
    public double OuterArcLength { get; set; }

    /// <summary>Inner arc length (cm).</summary>
    public double InnerArcLength { get; set; }

    /// <summary>Radial (straight side) edge length W = R_o − R_i (cm).</summary>
    public double RadialEdgeLength { get; set; }

    public double PixelsPerCm { get; set; }
}
