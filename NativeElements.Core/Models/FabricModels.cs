namespace NativeElements.Core.Models
{
    public class FabricInput
    {
        // Panel dimensions in cm
        public double PanelLengthCm { get; set; }
        public double PanelWidthCm { get; set; }

        // Fabric properties in cm
        public double FabricWidthCm { get; set; }

        // Pattern repeat in cm (0 = no repeat)
        public double RepeatCm { get; set; }

        // Extra allowance for matching repeats in cm
        public double RepeatAllowanceCm { get; set; }

        // Shrinkage/safety factor (decimal, e.g., 0.05 => +5%)
        public double ShrinkageFactor { get; set; } = 0.05;

        // Number of panels (quantity)
        public int Quantity { get; set; } = 1;
    }

    public class FabricOutput
    {
        // Total linear meters of fabric required (m)
        public double TotalLinearMeters { get; set; }

        // Total square meters of fabric required (m^2)
        public double TotalSquareMeters { get; set; }

        // Waste percentage
        public double WastePercent { get; set; }
    }
}