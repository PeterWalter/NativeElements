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

        // W = 2R · sin(π/n)  — width at widest point
        double petalWidth = 2 * radius * Math.Sin(Math.PI / input.NumberOfPetals);

        // L = π · R  — arc length (sewing line, pole to pole)
        double arcLength = Math.PI * radius;

        // PetalHeight = arcLength (seam allowance is on the CURVED EDGES, not the tips)
        double petalHeight = arcLength;

        double pixelsPerCm = input.Dpi / 2.54;

        var curvePoints = GenerateCurvePoints(radius, input.NumberOfPetals, petalWidth);
        var seamCurvePoints = GenerateSeamCurvePoints(radius, petalWidth, arcLength, input.SeamAllowance);

        return new PetalOutput
        {
            PetalWidth = petalWidth,
            ArcLength = arcLength,
            PetalHeight = petalHeight,
            SeamAllowance = input.SeamAllowance,
            CurvePoints = curvePoints,
            SeamCurvePoints = seamCurvePoints,
            PixelsPerCm = pixelsPerCm
        };
    }

    // Right-side sewing line: x = (W/2)·sin(π·t),  y = t·L
    private static List<(double X, double Y)> GenerateCurvePoints(double radius, int numPetals, double petalWidth)
    {
        var points = new List<(double, double)>();
        const int stepCount = 100;

        double arcLength = Math.PI * radius;
        double halfWidth = petalWidth / 2;

        for (int i = 0; i <= stepCount; i++)
        {
            double t = i / (double)stepCount;
            double y = t * arcLength;
            double x = halfWidth * Math.Sin(Math.PI * t);
            points.Add((x, y));
        }

        return points;
    }

    // Right-side cut line: offset the sewing curve outward by seamAllowance along the curve normal
    private static List<(double X, double Y)> GenerateSeamCurvePoints(
        double radius, double petalWidth, double arcLength, double seamAllowance)
    {
        var points = new List<(double, double)>();
        const int stepCount = 100;
        double halfWidth = petalWidth / 2;

        for (int i = 0; i <= stepCount; i++)
        {
            double t = i / (double)stepCount;
            double x = halfWidth * Math.Sin(Math.PI * t);
            double y = t * arcLength;

            // Tangent: d/dt of (halfWidth·sin(πt), t·arcLength)
            double dxdt = halfWidth * Math.PI * Math.Cos(Math.PI * t);
            double dydt = arcLength;

            // Outward normal for right side = rotate tangent 90° clockwise = (dydt, -dxdt) normalised
            double mag = Math.Sqrt(dxdt * dxdt + dydt * dydt);
            double nx = dydt / mag;
            double ny = -dxdt / mag;

            points.Add((x + seamAllowance * nx, y + seamAllowance * ny));
        }

        return points;
    }
}
