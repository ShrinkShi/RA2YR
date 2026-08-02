using System;

namespace RA2YR.Core.Binary
{
    public sealed class BinaryReadLimits
    {
        public BinaryReadLimits(
            long maxInputBytes,
            long maxSingleReadBytes,
            long maxAllocatedBytes,
            long maxRecords,
            long maxStringLength,
            int maxNestingDepth,
            long maxSubranges)
        {
            ValidateNonNegative(maxInputBytes, nameof(maxInputBytes));
            ValidateNonNegative(maxSingleReadBytes, nameof(maxSingleReadBytes));
            ValidateNonNegative(maxAllocatedBytes, nameof(maxAllocatedBytes));
            ValidateNonNegative(maxRecords, nameof(maxRecords));
            ValidateNonNegative(maxStringLength, nameof(maxStringLength));
            ValidateNonNegative(maxNestingDepth, nameof(maxNestingDepth));
            ValidateNonNegative(maxSubranges, nameof(maxSubranges));

            MaxInputBytes = maxInputBytes;
            MaxSingleReadBytes = maxSingleReadBytes;
            MaxAllocatedBytes = maxAllocatedBytes;
            MaxRecords = maxRecords;
            MaxStringLength = maxStringLength;
            MaxNestingDepth = maxNestingDepth;
            MaxSubranges = maxSubranges;
        }

        public static BinaryReadLimits Default { get; } = new BinaryReadLimits(
            256L * 1024 * 1024,
            16L * 1024 * 1024,
            512L * 1024 * 1024,
            1_000_000,
            1_000_000,
            64,
            1_000_000);

        public long MaxInputBytes { get; }

        public long MaxSingleReadBytes { get; }

        public long MaxAllocatedBytes { get; }

        public long MaxRecords { get; }

        public long MaxStringLength { get; }

        public int MaxNestingDepth { get; }

        public long MaxSubranges { get; }

        private static void ValidateNonNegative(long value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    public sealed class BinaryBudgetUsage
    {
        internal BinaryBudgetUsage(
            long inputBytes,
            long allocatedBytes,
            long records,
            long subranges,
            long longestStringLength,
            int deepestNesting)
        {
            InputBytes = inputBytes;
            AllocatedBytes = allocatedBytes;
            Records = records;
            Subranges = subranges;
            LongestStringLength = longestStringLength;
            DeepestNesting = deepestNesting;
        }

        public long InputBytes { get; }

        public long AllocatedBytes { get; }

        public long Records { get; }

        public long Subranges { get; }

        public long LongestStringLength { get; }

        public int DeepestNesting { get; }
    }
}
