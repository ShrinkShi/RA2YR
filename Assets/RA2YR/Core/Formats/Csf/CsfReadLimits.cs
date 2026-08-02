using System;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.Csf
{
    internal sealed class CsfReadLimits
    {
        public CsfReadLimits(
            long maxInputBytes,
            long maxSingleReadBytes,
            long maxAllocatedBytes,
            long maxLabels,
            long maxTotalValues,
            long maxValuesPerLabel,
            long maxLabelNameBytes,
            long maxMainTextCodeUnits,
            long maxExtraTextBytes,
            long maxCumulativeUtf16CodeUnits)
        {
            ValidateNonNegative(maxInputBytes, nameof(maxInputBytes));
            ValidateNonNegative(maxSingleReadBytes, nameof(maxSingleReadBytes));
            ValidateNonNegative(maxAllocatedBytes, nameof(maxAllocatedBytes));
            ValidateNonNegative(maxLabels, nameof(maxLabels));
            ValidateNonNegative(maxTotalValues, nameof(maxTotalValues));
            ValidateNonNegative(maxValuesPerLabel, nameof(maxValuesPerLabel));
            ValidateNonNegative(maxLabelNameBytes, nameof(maxLabelNameBytes));
            ValidateNonNegative(maxMainTextCodeUnits, nameof(maxMainTextCodeUnits));
            ValidateNonNegative(maxExtraTextBytes, nameof(maxExtraTextBytes));
            ValidateNonNegative(
                maxCumulativeUtf16CodeUnits,
                nameof(maxCumulativeUtf16CodeUnits));

            try
            {
                checked
                {
                    _ = maxLabels + maxTotalValues;
                }
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxTotalValues),
                    "The combined CSF record budget must fit in Int64.");
            }

            MaxInputBytes = maxInputBytes;
            MaxSingleReadBytes = maxSingleReadBytes;
            MaxAllocatedBytes = maxAllocatedBytes;
            MaxLabels = maxLabels;
            MaxTotalValues = maxTotalValues;
            MaxValuesPerLabel = maxValuesPerLabel;
            MaxLabelNameBytes = maxLabelNameBytes;
            MaxMainTextCodeUnits = maxMainTextCodeUnits;
            MaxExtraTextBytes = maxExtraTextBytes;
            MaxCumulativeUtf16CodeUnits = maxCumulativeUtf16CodeUnits;
        }

        public static CsfReadLimits Default { get; } = new CsfReadLimits(
            16L * 1024 * 1024,
            1024L * 1024,
            64L * 1024 * 1024,
            100_000,
            200_000,
            4096,
            4096,
            1024 * 1024,
            1024 * 1024,
            16L * 1024 * 1024);

        public long MaxInputBytes { get; }

        public long MaxSingleReadBytes { get; }

        public long MaxAllocatedBytes { get; }

        public long MaxLabels { get; }

        public long MaxTotalValues { get; }

        public long MaxValuesPerLabel { get; }

        public long MaxLabelNameBytes { get; }

        public long MaxMainTextCodeUnits { get; }

        public long MaxExtraTextBytes { get; }

        public long MaxCumulativeUtf16CodeUnits { get; }

        internal BinaryReadLimits ToBinaryLimits()
        {
            long maximumStringLength = Math.Max(
                MaxLabelNameBytes,
                Math.Max(MaxMainTextCodeUnits, MaxExtraTextBytes));
            return new BinaryReadLimits(
                MaxInputBytes,
                MaxSingleReadBytes,
                MaxAllocatedBytes,
                checked(MaxLabels + MaxTotalValues),
                maximumStringLength,
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
