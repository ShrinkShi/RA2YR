using System;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.Ini
{
    internal sealed class IniReadLimits
    {
        public IniReadLimits(
            long maxInputBytes,
            long maxSingleReadBytes,
            long maxLineCount,
            long maxLineBytes,
            long maxSectionNodes,
            long maxKeyValueNodes,
            long maxCommentNodes,
            long maxOpaqueNodes,
            long maxTotalNodes,
            long maxCumulativeRawBytes,
            long maxAllocatedBytes)
        {
            ValidateNonNegative(maxInputBytes, nameof(maxInputBytes));
            ValidateNonNegative(maxSingleReadBytes, nameof(maxSingleReadBytes));
            ValidateNonNegative(maxLineCount, nameof(maxLineCount));
            ValidateNonNegative(maxLineBytes, nameof(maxLineBytes));
            ValidateNonNegative(maxSectionNodes, nameof(maxSectionNodes));
            ValidateNonNegative(maxKeyValueNodes, nameof(maxKeyValueNodes));
            ValidateNonNegative(maxCommentNodes, nameof(maxCommentNodes));
            ValidateNonNegative(maxOpaqueNodes, nameof(maxOpaqueNodes));
            ValidateNonNegative(maxTotalNodes, nameof(maxTotalNodes));
            ValidateNonNegative(maxCumulativeRawBytes, nameof(maxCumulativeRawBytes));
            ValidateNonNegative(maxAllocatedBytes, nameof(maxAllocatedBytes));

            MaxInputBytes = maxInputBytes;
            MaxSingleReadBytes = maxSingleReadBytes;
            MaxLineCount = maxLineCount;
            MaxLineBytes = maxLineBytes;
            MaxSectionNodes = maxSectionNodes;
            MaxKeyValueNodes = maxKeyValueNodes;
            MaxCommentNodes = maxCommentNodes;
            MaxOpaqueNodes = maxOpaqueNodes;
            MaxTotalNodes = maxTotalNodes;
            MaxCumulativeRawBytes = maxCumulativeRawBytes;
            MaxAllocatedBytes = maxAllocatedBytes;
        }

        public static IniReadLimits Default { get; } = new IniReadLimits(
            16L * 1024 * 1024,
            1024L * 1024,
            1_000_000,
            1024L * 1024,
            250_000,
            1_000_000,
            1_000_000,
            1_000_000,
            1_000_000,
            16L * 1024 * 1024,
            128L * 1024 * 1024);

        public long MaxInputBytes { get; }

        public long MaxSingleReadBytes { get; }

        public long MaxLineCount { get; }

        public long MaxLineBytes { get; }

        public long MaxSectionNodes { get; }

        public long MaxKeyValueNodes { get; }

        public long MaxCommentNodes { get; }

        public long MaxOpaqueNodes { get; }

        public long MaxTotalNodes { get; }

        public long MaxCumulativeRawBytes { get; }

        public long MaxAllocatedBytes { get; }

        internal BinaryReadLimits ToBinaryLimits()
        {
            return new BinaryReadLimits(
                MaxInputBytes,
                MaxSingleReadBytes,
                MaxAllocatedBytes,
                MaxTotalNodes,
                MaxLineBytes,
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
