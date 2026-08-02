using System;
using NUnit.Framework;
using RA2YR.Core.Formats.Pal;

namespace RA2YR.Tests.EditMode.Formats.Pal
{
    [TestFixture]
    public sealed class PaletteDisplayConversionTests
    {
        [TestCase((int)PaletteDisplayConversionStrategy.ShiftLeftTwo, 0, 0)]
        [TestCase((int)PaletteDisplayConversionStrategy.ShiftLeftTwo, 16, 64)]
        [TestCase((int)PaletteDisplayConversionStrategy.ShiftLeftTwo, 63, 252)]
        [TestCase((int)PaletteDisplayConversionStrategy.ReplicateHighBits, 0, 0)]
        [TestCase((int)PaletteDisplayConversionStrategy.ReplicateHighBits, 16, 65)]
        [TestCase((int)PaletteDisplayConversionStrategy.ReplicateHighBits, 63, 255)]
        [TestCase((int)PaletteDisplayConversionStrategy.ScaleToFullRangeRounded, 0, 0)]
        [TestCase((int)PaletteDisplayConversionStrategy.ScaleToFullRangeRounded, 11, 45)]
        [TestCase((int)PaletteDisplayConversionStrategy.ScaleToFullRangeRounded, 63, 255)]
        [TestCase((int)PaletteDisplayConversionStrategy.XccScaleToFullRangeFloor, 0, 0)]
        [TestCase((int)PaletteDisplayConversionStrategy.XccScaleToFullRangeFloor, 11, 44)]
        [TestCase((int)PaletteDisplayConversionStrategy.XccScaleToFullRangeFloor, 63, 255)]
        public void StrategiesHaveExplicitBoundaryAndMidpointResults(
            int strategyValue,
            byte raw,
            byte expected)
        {
            var strategy = (PaletteDisplayConversionStrategy)strategyValue;
            Assert.That(
                PaletteDisplayConversion.ConvertChannel(raw, strategy),
                Is.EqualTo(expected));
        }

        [TestCase((int)PaletteDisplayConversionStrategy.ShiftLeftTwo)]
        [TestCase((int)PaletteDisplayConversionStrategy.ReplicateHighBits)]
        [TestCase((int)PaletteDisplayConversionStrategy.ScaleToFullRangeRounded)]
        [TestCase((int)PaletteDisplayConversionStrategy.XccScaleToFullRangeFloor)]
        public void EveryStrategyIsMonotonic(int strategyValue)
        {
            var strategy = (PaletteDisplayConversionStrategy)strategyValue;
            byte previous = PaletteDisplayConversion.ConvertChannel(0, strategy);
            for (byte raw = 1; raw <= PaletteColorRaw.MaximumChannelValue; raw++)
            {
                byte current = PaletteDisplayConversion.ConvertChannel(raw, strategy);
                Assert.That(current, Is.GreaterThanOrEqualTo(previous));
                previous = current;
            }
        }

        [Test]
        public void FullRangePoliciesAreNotCollapsedIntoOneStrategy()
        {
            const byte raw = 11;

            Assert.That(
                PaletteDisplayConversion.ConvertChannel(
                    raw,
                    PaletteDisplayConversionStrategy.ScaleToFullRangeRounded),
                Is.EqualTo(45));
            Assert.That(
                PaletteDisplayConversion.ConvertChannel(
                    raw,
                    PaletteDisplayConversionStrategy.XccScaleToFullRangeFloor),
                Is.EqualTo(44));
        }

        [Test]
        public void ColorConversionDoesNotMutateRawChannels()
        {
            var raw = new PaletteColorRaw(11, 16, 63);

            PaletteColorDisplay display = PaletteDisplayConversion.ConvertColor(
                raw,
                PaletteDisplayConversionStrategy.ReplicateHighBits);

            Assert.That(display, Is.EqualTo(new PaletteColorDisplay(44, 65, 255)));
            Assert.That(raw, Is.EqualTo(new PaletteColorRaw(11, 16, 63)));
        }

        [Test]
        public void ConversionRejectsRawValuesOutsideSixBitRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PaletteDisplayConversion.ConvertChannel(
                    64,
                    PaletteDisplayConversionStrategy.ShiftLeftTwo));
        }

        [Test]
        public void ConversionRejectsUnknownStrategies()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PaletteDisplayConversion.ConvertChannel(
                    0,
                    (PaletteDisplayConversionStrategy)int.MaxValue));
        }

        [Test]
        public void RawColorConstructorRejectsOutOfRangeChannels()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PaletteColorRaw(0, 64, 0));
        }
    }
}
