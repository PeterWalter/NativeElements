using Microsoft.VisualStudio.TestTools.UnitTesting;
using NativeElements.Core.Models;
using NativeElements.Core.Services;

namespace NativeElements.Tests
{
    [TestClass]
    public class CuttingLayoutTests
    {
        [TestMethod]
        public void GenerateBoxCushionLayout_BasicValues()
        {
            var input = new CushionInput
            {
                FinishedWidth = 50.0,
                FinishedDepth = 30.0,
                BoxedHeight = 5.0,
                SeamAllowance = 1.0,
                Quantity = 1
            };

            var layout = CuttingLayoutService.GenerateBoxCushionLayout(input);

            Assert.AreEqual(3, layout.Pieces.Count);

            var top = layout.Pieces.Find(p => p.Name == "Top");
            Assert.IsNotNull(top);
            Assert.AreEqual(52.0, top.WidthCm, 0.0001); // 50 + 2*1
            Assert.AreEqual(32.0, top.HeightCm, 0.0001); // 30 + 2*1

            var boxing = layout.Pieces.Find(p => p.Name == "Boxing Band");
            Assert.IsNotNull(boxing);
            // perimeter = 2*(50+30)=160; +4*1 =>164
            Assert.AreEqual(7.0, boxing.WidthCm, 0.0001); // boxing width = 5 + 2*1
            Assert.AreEqual(164.0, boxing.HeightCm, 0.0001); // stored length
        }
    }
}