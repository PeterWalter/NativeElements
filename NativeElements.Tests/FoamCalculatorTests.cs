using Microsoft.VisualStudio.TestTools.UnitTesting;
using NativeElements.Core.Models;
using NativeElements.Core.Services;

namespace NativeElements.Tests
{
    [TestClass]
    public class FoamCalculatorTests
    {
        [TestMethod]
        public void Calculate_DefaultDensity_CorrectVolumesAndWeight()
        {
            var input = new FoamInput
            {
                LengthCm = 50, // 0.5 m
                WidthCm = 50,  // 0.5 m
                ThicknessCm = 5, // 0.05 m
                // default density 30 kg/m3
                Quantity = 1
            };

            var outp = FoamCalculatorService.CalculateFoam(input);

            // volume cm3 = 50*50*5 = 12500 cm3
            Assert.AreEqual(12500, outp.VolumePerPieceCm3, 0.0001);
            Assert.AreEqual(12500, outp.VolumeCm3, 0.0001);

            // volume m3 = 12500 / 1e6 = 0.0125 m3
            Assert.AreEqual(0.0125, outp.VolumeM3, 1e-6);

            // weight = 0.0125 * 30 = 0.375 kg
            Assert.AreEqual(0.375, outp.WeightKg, 1e-6);
            Assert.AreEqual(0.375, outp.WeightPerPieceKg, 1e-6);
        }

        [TestMethod]
        public void Calculate_MultipleQuantity_MultipliesTotals()
        {
            var input = new FoamInput
            {
                LengthCm = 100,
                WidthCm = 50,
                ThicknessCm = 10,
                DensityKgPerM3 = 50,
                Quantity = 3
            };

            var outp = FoamCalculatorService.CalculateFoam(input);

            double perPieceCm3 = 100 * 50 * 10; // 50000 cm3
            Assert.AreEqual(perPieceCm3, outp.VolumePerPieceCm3, 0.0001);
            Assert.AreEqual(perPieceCm3 * 3, outp.VolumeCm3, 0.0001);

            double perPieceM3 = perPieceCm3 / 1_000_000.0;
            double perPieceKg = perPieceM3 * 50.0;
            Assert.AreEqual(perPieceKg * 3, outp.WeightKg, 1e-6);
        }
    }
}