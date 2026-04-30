namespace NativeElements.Core.Models
{
    public class FoamInput
    {
        // Dimensions in cm
        public double LengthCm { get; set; }
        public double WidthCm { get; set; }
        public double ThicknessCm { get; set; }

        // Density in kg/m^3 (typical foam ~20-60 kg/m^3)
        public double DensityKgPerM3 { get; set; } = 30.0;

        public int Quantity { get; set; } = 1;
    }

    public class FoamOutput
    {
        // Volume in cubic centimeters
        public double VolumeCm3 { get; set; }

        // Volume in cubic meters
        public double VolumeM3 { get; set; }

        // Weight in kilograms
        public double WeightKg { get; set; }

        // Per-piece metrics
        public double VolumePerPieceCm3 { get; set; }
        public double WeightPerPieceKg { get; set; }
    }
}