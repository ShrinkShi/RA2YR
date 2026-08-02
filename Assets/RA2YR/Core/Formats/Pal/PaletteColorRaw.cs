using System;

namespace RA2YR.Core.Formats.Pal
{
    internal readonly struct PaletteColorRaw : IEquatable<PaletteColorRaw>
    {
        public const byte MaximumChannelValue = 63;

        public PaletteColorRaw(byte red, byte green, byte blue)
        {
            ValidateChannel(red, nameof(red));
            ValidateChannel(green, nameof(green));
            ValidateChannel(blue, nameof(blue));
            Red = red;
            Green = green;
            Blue = blue;
        }

        public byte Red { get; }

        public byte Green { get; }

        public byte Blue { get; }

        public bool Equals(PaletteColorRaw other)
        {
            return Red == other.Red && Green == other.Green && Blue == other.Blue;
        }

        public override bool Equals(object obj)
        {
            return obj is PaletteColorRaw other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Red | (Green << 8) | (Blue << 16);
        }

        public static bool operator ==(PaletteColorRaw left, PaletteColorRaw right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PaletteColorRaw left, PaletteColorRaw right)
        {
            return !left.Equals(right);
        }

        private static void ValidateChannel(byte value, string parameterName)
        {
            if (value > MaximumChannelValue)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
