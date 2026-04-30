using NativeElements.Core.Models;
using System;

namespace NativeElements.Core.Services
{
    public static class FoamCalculatorService
    {
        // Calculate foam volume and weight
        public static FoamOutput CalculateFoam(FoamInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.LengthCm <= 0 || input.WidthCm <= 0 || input.ThicknessCm <= 0) throw new ArgumentException("Invalid foam dimensions");

            // Volume per piece in cubic centimeters
            double volumeCm3 = input.LengthCm * input.WidthCm * input.ThicknessCm;

            // Convert to cubic meters
            double volumeM3PerPiece = volumeCm3 / 1_000_000.0; // 1 m^3 = 1,000,000 cm^3

            // Weight per piece (kg) = volume_m3 * density_kg_per_m3
            double weightPerPieceKg = volumeM3PerPiece * input.DensityKgPerM3;

            double totalVolumeCm3 = volumeCm3 * Math.Max(1, input.Quantity);
            double totalVolumeM3 = volumeM3PerPiece * Math.Max(1, input.Quantity);
            double totalWeightKg = weightPerPieceKg * Math.Max(1, input.Quantity);

            return new FoamOutput
            {
                VolumeCm3 = totalVolumeCm3,
                VolumeM3 = totalVolumeM3,
                WeightKg = totalWeightKg,
                VolumePerPieceCm3 = volumeCm3,
                WeightPerPieceKg = weightPerPieceKg
            };
        }
    }
}