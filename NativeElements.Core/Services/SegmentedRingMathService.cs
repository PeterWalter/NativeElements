using NativeElements.Models;
using System;

namespace NativeElements.Services;

public class SegmentedRingMathService
{
    public static SegmentedRingOutput CalculateSegment(SegmentedRingInput input)
    {
        if (input.OuterRadius <= 0 || input.InnerRadius >= input.OuterRadius || input.NumberOfSegments < 3)
            throw new ArgumentException("Invalid segmented ring parameters");

        // Central angle for each segment
        double segmentAngle = 360.0 / input.NumberOfSegments; // In degrees

        // Convert to radians
        double angleRad = Math.PI * segmentAngle / 180.0;

        // Outer edge length: Using chord length formula
        double outerEdgeLength = 2 * input.OuterRadius * Math.Sin(angleRad / 2);

        // Inner edge length
        double innerEdgeLength = 2 * input.InnerRadius * Math.Sin(angleRad / 2);

        // Radial edge (straight sides): Simply the difference
        double radialEdgeLength = input.OuterRadius - input.InnerRadius;

        // Calculate pixels per cm for rendering
        double pixelsPerCm = input.Dpi / 2.54;

        double outerArcLength = input.OuterRadius * angleRad;
        double innerArcLength = input.InnerRadius * angleRad;

        return new SegmentedRingOutput
        {
            SegmentAngle    = segmentAngle,
            MiterAngle      = segmentAngle / 2.0,
            OuterRadius     = input.OuterRadius,
            InnerRadius     = input.InnerRadius,
            OuterEdgeLength = outerEdgeLength,
            InnerEdgeLength = innerEdgeLength,
            OuterArcLength  = outerArcLength,
            InnerArcLength  = innerArcLength,
            RadialEdgeLength = radialEdgeLength,
            PixelsPerCm     = pixelsPerCm
        };
    }
}
