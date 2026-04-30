using NativeElements.Core.Models;
using System;

namespace NativeElements.Core.Services
{
    public static class FabricCalculatorService
    {
        // Calculates fabric requirement for rectangular panels with optional pattern repeat alignment
        // Units: input in cm; outputs in meters/m^2
        public static FabricOutput CalculateFabricRequirement(FabricInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.PanelLengthCm <= 0 || input.PanelWidthCm <= 0 || input.FabricWidthCm <= 0) throw new ArgumentException("Invalid dimensions");

            // Determine adjusted panel length if a repeat is specified
            double adjustedLengthCm = input.PanelLengthCm;
            if (input.RepeatCm > 0)
            {
                // Number of repeats needed to cover panel length
                double repeats = Math.Ceiling(adjustedLengthCm / input.RepeatCm);
                adjustedLengthCm = repeats * input.RepeatCm + input.RepeatAllowanceCm;
            }

            // Convert cm to meters
            double panelLengthM = adjustedLengthCm / 100.0;
            double panelWidthM = input.PanelWidthCm / 100.0;
            double fabricWidthM = input.FabricWidthCm / 100.0;

            // Linear meters needed per panel (length dimension occupies fabric length)
            // If panel width > fabric width, need multiple strips side-by-side.
            double stripsPerPanel = Math.Ceiling(panelWidthM / fabricWidthM);

            double linearMetersPerPanel = panelLengthM * stripsPerPanel;

            // Apply shrinkage/safety margin
            linearMetersPerPanel *= (1.0 + input.ShrinkageFactor);

            // Total for quantity
            double totalLinearMeters = linearMetersPerPanel * Math.Max(1, input.Quantity);

            double totalSquareMeters = totalLinearMeters * fabricWidthM;

            // Simple waste estimation (if repeat used, extra waste due to repeat)
            double wastePercent = input.RepeatCm > 0 ? 100.0 * ( (adjustedLengthCm - input.PanelLengthCm) / input.PanelLengthCm ) : 0.0;

            return new FabricOutput
            {
                TotalLinearMeters = totalLinearMeters,
                TotalSquareMeters = totalSquareMeters,
                WastePercent = wastePercent
            };
        }
    }
}