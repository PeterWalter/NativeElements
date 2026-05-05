namespace NativeElements.Models;

public class SegmentedRingInput
{
    public double OuterRadius { get; set; }
    public double InnerRadius { get; set; }
    public int NumberOfSegments { get; set; }
    public double Dpi { get; set; } = 300;
    /// <summary>Length of board material available (cm, chord direction). 0 = not specified.</summary>
    public double BoardLength { get; set; } = 0;

    /// <summary>Radial-direction thickness of available board plank (cm). 0 = not specified.</summary>
    public double BoardWidth { get; set; } = 0;
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

    /// <summary>User-specified available board length (cm, 0 = not given).</summary>
    public double BoardLengthUsed { get; set; }

    /// <summary>How many ring segments can be cut from one board of BoardLengthUsed.</summary>
    public int SegmentsPerBoard { get; set; }

    /// <summary>Leftover board length after cutting SegmentsPerBoard segments (cm).</summary>
    public double BoardOffcut { get; set; }

    /// <summary>Minimum board width (radial depth) needed to fit the segment: R_o − R_i·cos(α) (cm).</summary>
    public double MinBoardWidth { get; set; }

    /// <summary>Whether the user's board width fits (true when BoardWidth=0 or ≥ MinBoardWidth).</summary>
    public bool BoardWidthFits { get; set; }

    /// <summary>User-specified board width (cm, 0 = not given). Stored for drawing.</summary>
    public double UserBoardWidthUsed { get; set; }
}
