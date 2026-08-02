using System;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.Pal
{
    internal sealed class PaletteReadLimits
    {
        public PaletteReadLimits(
            long maxInputBytes,
            long maxSingleReadBytes,
            long maxAllocatedBytes,
            long maxRecords)
        {
            ValidateNonNegative(maxInputBytes, nameof(maxInputBytes));
            ValidateNonNegative(maxSingleReadBytes, nameof(maxSingleReadBytes));
            ValidateNonNegative(maxAllocatedBytes, nameof(maxAllocatedBytes));
            ValidateNonNegative(maxRecords, nameof(maxRecords));
            MaxInputBytes = maxInputBytes;
            MaxSingleReadBytes = maxSingleReadBytes;
            MaxAllocatedBytes = maxAllocatedBytes;
            MaxRecords = maxRecords;
        }

        public static PaletteReadLimits Default { get; } = new PaletteReadLimits(
            4096,
            4096,
            16 * 1024,
            WestwoodPalette.ColorCount);

        public long MaxInputBytes { get; }

        public long MaxSingleReadBytes { get; }

        public long MaxAllocatedBytes { get; }

        public long MaxRecords { get; }

        internal BinaryReadLimits ToBinaryLimits()
        {
            return new BinaryReadLimits(
                MaxInputBytes,
                MaxSingleReadBytes,
                MaxAllocatedBytes,
                MaxRecords,
                0,
                0,
                0);
        }

        private static void ValidateNonNegative(long value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
