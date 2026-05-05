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

        // Convert to radians (full segment angle and half = miter angle)
        double angleRad   = Math.PI * segmentAngle / 180.0;
        double halfAngle  = angleRad / 2.0;           // miter angle in radians
        double sinA       = Math.Sin(halfAngle);
        double cosA       = Math.Cos(halfAngle);

        // Outer edge length: chord length formula
        double outerEdgeLength = 2 * input.OuterRadius * sinA;

        // Inner edge length
        double innerEdgeLength = 2 * input.InnerRadius * sinA;

        // Radial edge (straight sides)
        double radialEdgeLength = input.OuterRadius - input.InnerRadius;

        // Calculate pixels per cm for rendering
        double pixelsPerCm = input.Dpi / 2.54;

        double outerArcLength = input.OuterRadius * angleRad;
        double innerArcLength = input.InnerRadius * angleRad;

        // Minimum board width (radial) to contain the full segment template
        // = distance from outer arc peak (board top) to inner arc endpoints
        double minBoardWidth   = input.OuterRadius - input.InnerRadius * cosA;
        bool   boardWidthFits  = input.BoardWidth <= 0 || input.BoardWidth >= minBoardWidth;

        int    segmentsPerBoard = 0;
        double boardOffcut      = 0;
        if (input.BoardLength > 0 && outerEdgeLength > 0)
        {
            segmentsPerBoard = (int)(input.BoardLength / outerEdgeLength);
            boardOffcut      = input.BoardLength - segmentsPerBoard * outerEdgeLength;
        }

        return new SegmentedRingOutput
        {
            SegmentAngle       = segmentAngle,
            MiterAngle         = segmentAngle / 2.0,
            OuterRadius        = input.OuterRadius,
            InnerRadius        = input.InnerRadius,
            OuterEdgeLength    = outerEdgeLength,
            InnerEdgeLength    = innerEdgeLength,
            OuterArcLength     = outerArcLength,
            InnerArcLength     = innerArcLength,
            RadialEdgeLength   = radialEdgeLength,
            PixelsPerCm        = pixelsPerCm,
            BoardLengthUsed    = input.BoardLength,
            SegmentsPerBoard   = segmentsPerBoard,
            BoardOffcut        = boardOffcut,
            MinBoardWidth      = minBoardWidth,
            BoardWidthFits     = boardWidthFits,
            UserBoardWidthUsed = input.BoardWidth,
        };
    }
}
