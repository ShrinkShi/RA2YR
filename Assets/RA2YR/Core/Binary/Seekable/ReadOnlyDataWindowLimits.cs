using System;

namespace RA2YR.Core.Binary.Seekable
{
    internal sealed class ReadOnlyDataWindowLimits
    {
        public ReadOnlyDataWindowLimits(
            long maxRootLength,
            long maxSingleReadBytes,
            long maxTotalReadBytes,
            long maxWindows,
            int maxWindowDepth)
        {
            ValidateNonNegative(maxRootLength, nameof(maxRootLength));
            ValidateNonNegative(maxSingleReadBytes, nameof(maxSingleReadBytes));
            ValidateNonNegative(maxTotalReadBytes, nameof(maxTotalReadBytes));
            ValidateNonNegative(maxWindows, nameof(maxWindows));
            ValidateNonNegative(maxWindowDepth, nameof(maxWindowDepth));

            MaxRootLength = maxRootLength;
            MaxSingleReadBytes = maxSingleReadBytes;
            MaxTotalReadBytes = maxTotalReadBytes;
            MaxWindows = maxWindows;
            MaxWindowDepth = maxWindowDepth;
        }

        public static ReadOnlyDataWindowLimits Default { get; } =
            new ReadOnlyDataWindowLimits(
                1024L * 1024 * 1024,
                16L * 1024 * 1024,
                4L * 1024 * 1024 * 1024,
                1_000_000,
                64);

        public long MaxRootLength { get; }

        public long MaxSingleReadBytes { get; }

        public long MaxTotalReadBytes { get; }

        public long MaxWindows { get; }

        public int MaxWindowDepth { get; }

        private static void ValidateNonNegative(long value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    internal sealed class ReadOnlyDataWindowBudgetUsage
    {
        internal ReadOnlyDataWindowBudgetUsage(
            long bytesRead,
            long windowsCreated,
            int deepestWindow)
        {
            BytesRead = bytesRead;
            WindowsCreated = windowsCreated;
            DeepestWindow = deepestWindow;
        }

        public long BytesRead { get; }

        public long WindowsCreated { get; }

        public int DeepestWindow { get; }
    }
}
