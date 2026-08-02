using System;

namespace RA2YR.Core.Formats.Mix
{
    internal sealed class MixReadLimits
    {
        public MixReadLimits(
            long maxArchiveBytes,
            int maxEntries,
            long maxDirectoryBytes,
            long maxAllocatedBytes,
            long maxTotalReadBytes,
            long maxWindows,
            int maxWindowDepth)
        {
            if (maxArchiveBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxArchiveBytes));
            }

            if (maxEntries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEntries));
            }

            if (maxDirectoryBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDirectoryBytes));
            }

            if (maxAllocatedBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAllocatedBytes));
            }

            if (maxTotalReadBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTotalReadBytes));
            }

            if (maxWindows < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxWindows));
            }

            if (maxWindowDepth < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxWindowDepth));
            }

            MaxArchiveBytes = maxArchiveBytes;
            MaxEntries = maxEntries;
            MaxDirectoryBytes = maxDirectoryBytes;
            MaxAllocatedBytes = maxAllocatedBytes;
            MaxTotalReadBytes = maxTotalReadBytes;
            MaxWindows = maxWindows;
            MaxWindowDepth = maxWindowDepth;
        }

        public static MixReadLimits Default { get; } = new MixReadLimits(
            1024L * 1024 * 1024,
            ushort.MaxValue,
            1024L * 1024,
            64L * 1024 * 1024,
            2L * 1024 * 1024 * 1024,
            1_000_000,
            64);

        public long MaxArchiveBytes { get; }

        public int MaxEntries { get; }

        public long MaxDirectoryBytes { get; }

        public long MaxAllocatedBytes { get; }

        public long MaxTotalReadBytes { get; }

        public long MaxWindows { get; }

        public int MaxWindowDepth { get; }
    }
}
