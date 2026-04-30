using System.Collections.Generic;

namespace NativeElements.Core.Models
{
    public class CuttingPiece
    {
        public string Name { get; set; } = string.Empty;
        // Width and Height in cm for rectangular pieces
        public double WidthCm { get; set; }
        public double HeightCm { get; set; }

        // Optional polygon points (cm) for DXF export or drawing (list of (x,y) pairs)
        public List<(double X, double Y)> Points { get; set; } = new List<(double X, double Y)>();

        // Quantity of this piece
        public int Quantity { get; set; } = 1;
    }

    public class CuttingLayoutOutput
    {
        public List<CuttingPiece> Pieces { get; set; } = new List<CuttingPiece>();
    }

    public class CuttingLayoutInput
    {
        // Generic input wrapper to allow different calculators to reuse
        // For cushions, use CushionInput (from CushionModels)
    }
}
