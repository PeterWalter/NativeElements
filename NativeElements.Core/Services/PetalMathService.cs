using NativeElements.Models;
using System;
using System.Collections.Generic;

namespace NativeElements.Services;

public class PetalMathService
{
    public static PetalOutput CalculatePetal(PetalInput input)
    {
        if (input.SphereDiameter <= 0 || input.NumberOfPetals < 3)
            throw new ArgumentException("Invalid petal parameters");

        double radius = input.SphereDiameter / 2;

        // Width of petal at widest point: W = 2R * sin(π/n)
        double petalWidth = 2 * radius * Math.Sin(Math.PI / input.NumberOfPetals);

        // Arc length along sphere: L = π * R
        double arcLength = Math.PI * radius;

        // Petal height (including seam allowance)
        double petalHeight = arcLength + (2 * input.SeamAllowance);

        // Generate curve points using sine function
        // For a boat-shaped petal: y = R * sin(n/n) * sin(x/R)
        var curvePoints = GenerateCurvePoints(radius, input.NumberOfPetals, petalWidth);

        // Calculate pixels per cm for rendering
        double pixelsPerCm = input.Dpi / 2.54; // DPI conversion

        return new PetalOutput
        {
            PetalWidth = petalWidth,
            ArcLength = arcLength,
            PetalHeight = petalHeight,
            CurvePoints = curvePoints,
            PixelsPerCm = pixelsPerCm
        };
    }

    private static List<(double X, double Y)> GenerateCurvePoints(double radius, int numPetals, double petalWidth)
    {
        var points = new List<(double, double)>();
        const int stepCount = 100;

        double arcLength = Math.PI * radius;
        double halfWidth = petalWidth / 2;

        // Y-parameterized seam line: x = (W/2) * sin(π * y / L)
        // where W = petalWidth, L = arcLength, y ranges from 0 to arcLength
        // This creates a symmetric petal shape that tapers to points at both ends
        for (int i = 0; i <= stepCount; i++)
        {
            double t = i / (double)stepCount; // 0 to 1
            double y = t * arcLength; // Height from 0 to arcLength

            // Right-side seam: sine curve
            double x = halfWidth * Math.Sin(Math.PI * t);

            points.Add((x, y));
        }

        return points;
    }
}
