using Microsoft.VisualStudio.TestTools.UnitTesting;
using NativeElements.Core.Models;
using NativeElements.Core.Services;

namespace NativeElements.Tests
{
    [TestClass]
    public class FabricCalculatorTests
    {
        [TestMethod]
        public void Calculate_NoRepeat_ReturnsExpected()
        {
            var input = new FabricInput
            {
                PanelLengthCm = 100, // 1.0 m
                PanelWidthCm = 50,   // 0.5 m
                FabricWidthCm = 150, // 1.5 m
                RepeatCm = 0,
                RepeatAllowanceCm = 0,
                ShrinkageFactor = 0.0,
                Quantity = 1
            };

            var output = FabricCalculatorService.CalculateFabricRequirement(input);

            // Expect linear meters per panel = length * strips (0.5/1.5 => 1 strip) => 1.0 m
            Assert.AreEqual(1.0, output.TotalLinearMeters, 0.0001);

            // Square meters = linear meters * fabric width (1.0 * 1.5 = 1.5 m^2)
            Assert.AreEqual(1.5, output.TotalSquareMeters, 0.0001);

            // No repeat => zero waste
            Assert.AreEqual(0.0, output.WastePercent, 0.0001);
        }

        [TestMethod]
        public void Calculate_WithRepeat_AddsAllowance()
        {
            var input = new FabricInput
            {
                PanelLengthCm = 95, // 0.95 m
                PanelWidthCm = 50,
                FabricWidthCm = 150,
                RepeatCm = 20, // repeat every 20 cm
                RepeatAllowanceCm = 2, // add 2 cm for matching
                ShrinkageFactor = 0.0,
                Quantity = 2
            };

            var output = FabricCalculatorService.CalculateFabricRequirement(input);

            // adjusted length: ceil(95/20)=5 -> 5*20 +2 = 102 cm => 1.02 m
            // strips=1 => linear per panel=1.02 m, quantity 2 => total 2.04 m
            Assert.AreEqual(2.04, output.TotalLinearMeters, 0.0001);

            // square meters = totalLinear * fabricWidthM (2.04 * 1.5 = 3.06)
            Assert.AreEqual(3.06, output.TotalSquareMeters, 0.0001);

            // waste percent = (102-95)/95 *100 = ~7.368421
            Assert.AreEqual((102.0-95.0)/95.0*100.0, output.WastePercent, 0.0001);
        }
    }
}