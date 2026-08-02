using System;

namespace RA2YR.Core.Formats.Pal
{
    internal enum PaletteDisplayConversionStrategy
    {
        ShiftLeftTwo,
        ReplicateHighBits,
        ScaleToFullRangeRounded,
        XccScaleToFullRangeFloor
    }

    internal readonly struct PaletteColorDisplay : IEquatable<PaletteColorDisplay>
    {
        public PaletteColorDisplay(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        public byte Red { get; }

        public byte Green { get; }

        public byte Blue { get; }

        public bool Equals(PaletteColorDisplay other)
        {
            return Red == other.Red && Green == other.Green && Blue == other.Blue;
        }

        public override bool Equals(object obj)
        {
            return obj is PaletteColorDisplay other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Red | (Green << 8) | (Blue << 16);
        }
    }

    internal static class PaletteDisplayConversion
    {
        public static byte ConvertChannel(
            byte rawValue,
            PaletteDisplayConversionStrategy strategy)
        {
            if (rawValue > PaletteColorRaw.MaximumChannelValue)
            {
                throw new ArgumentOutOfRangeException(nameof(rawValue));
            }

            switch (strategy)
            {
                case PaletteDisplayConversionStrategy.ShiftLeftTwo:
                    return checked((byte)(rawValue << 2));

                case PaletteDisplayConversionStrategy.ReplicateHighBits:
                    return checked((byte)((rawValue << 2) | (rawValue >> 4)));

                case PaletteDisplayConversionStrategy.ScaleToFullRangeRounded:
                    return checked((byte)((rawValue * 255 + 31) / 63));

                case PaletteDisplayConversionStrategy.XccScaleToFullRangeFloor:
                    return checked((byte)(rawValue * 255 / 63));

                default:
                    throw new ArgumentOutOfRangeException(nameof(strategy));
            }
        }

        public static PaletteColorDisplay ConvertColor(
            PaletteColorRaw rawColor,
            PaletteDisplayConversionStrategy strategy)
        {
            return new PaletteColorDisplay(
                ConvertChannel(rawColor.Red, strategy),
                ConvertChannel(rawColor.Green, strategy),
                ConvertChannel(rawColor.Blue, strategy));
        }
    }
}
