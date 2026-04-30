using NativeElements.Core.Models;
using NativeElements.Models;
using System;

namespace NativeElements.Core.Services
{
    public static class CuttingLayoutService
    {
        // Generate cutting pieces for a boxed cushion (top, bottom, boxing band)
        public static CuttingLayoutOutput GenerateBoxCushionLayout(CushionInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var outp = new CuttingLayoutOutput();

            // Top and Bottom pieces: include seam allowance on all sides
            double topWidth = input.FinishedWidth + 2.0 * input.SeamAllowance; // cm
            double topHeight = input.FinishedDepth + 2.0 * input.SeamAllowance; // cm

            var top = new CuttingPiece
            {
                Name = "Top",
                WidthCm = Math.Round(topWidth, 4),
                HeightCm = Math.Round(topHeight, 4),
                Quantity = input.Quantity
            };

            var bottom = new CuttingPiece
            {
                Name = "Bottom",
                WidthCm = Math.Round(topWidth, 4),
                HeightCm = Math.Round(topHeight, 4),
                Quantity = input.Quantity
            };

            // Boxing band: length = perimeter of finished top + seam allowances for joins
            // Perimeter of finished top (without seam allowances) = 2*(W + D)
            double perimeter = 2.0 * (input.FinishedWidth + input.FinishedDepth);
            // Add seam allowances: we'll add 4 * seamAllowance (one per corner) as safe allowance
            double boxingLength = perimeter + 4.0 * input.SeamAllowance;
            double boxingWidth = input.BoxedHeight + 2.0 * input.SeamAllowance; // include seam allowances on top/bottom edges

            var boxing = new CuttingPiece
            {
                Name = "Boxing Band",
                WidthCm = Math.Round(boxingWidth, 4),
                HeightCm = Math.Round(boxingLength, 4), // store length in HeightCm for strip pieces
                Quantity = input.Quantity
            };

            // For DXF/polygon export, add rectangle points for top and bottom
            top.Points = RectanglePoints(top.WidthCm, top.HeightCm);
            bottom.Points = RectanglePoints(bottom.WidthCm, bottom.HeightCm);

            // For boxing, represent as a long rectangle (length x width)
            boxing.Points = RectanglePoints(boxing.HeightCm, boxing.WidthCm);

            outp.Pieces.Add(top);
            outp.Pieces.Add(bottom);
            outp.Pieces.Add(boxing);

            return outp;
        }

        private static System.Collections.Generic.List<(double X, double Y)> RectanglePoints(double wCm, double hCm)
        {
            var pts = new System.Collections.Generic.List<(double X, double Y)>
            {
                (0,0), (wCm,0), (wCm,hCm), (0,hCm)
            };
            return pts;
        }
    }
}
