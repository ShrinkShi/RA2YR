using System;

namespace RA2YR.Core.Formats.Mix.Writing
{
    internal enum MixWriteOrder
    {
        DeterministicRebuild,
        PreserveEntryOrder
    }

    internal enum MixWriteHeaderKind
    {
        Classic,
        Extended
    }

    internal enum MixOutputPurpose
    {
        TestResults,
        Cache,
        TemporaryTestDirectory
    }

    internal sealed class MixWriteOptions
    {
        private readonly byte[] encryptionKeySource;

        public MixWriteOptions(
            MixWriteOrder order,
            MixWriteHeaderKind headerKind,
            bool includeChecksum,
            byte[] encryptionKeySource,
            int maxEntryCount,
            long maxArchiveBytes)
        {
            if (!Enum.IsDefined(typeof(MixWriteOrder), order))
            {
                throw new ArgumentOutOfRangeException(nameof(order));
            }

            if (!Enum.IsDefined(typeof(MixWriteHeaderKind), headerKind))
            {
                throw new ArgumentOutOfRangeException(nameof(headerKind));
            }

            if (maxEntryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEntryCount));
            }

            if (maxArchiveBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxArchiveBytes));
            }

            Order = order;
            HeaderKind = headerKind;
            IncludeChecksum = includeChecksum;
            this.encryptionKeySource = encryptionKeySource == null
                ? null
                : (byte[])encryptionKeySource.Clone();
            MaxEntryCount = maxEntryCount;
            MaxArchiveBytes = maxArchiveBytes;
        }

        public static MixWriteOptions ClassicDeterministic { get; } =
            new MixWriteOptions(
                MixWriteOrder.DeterministicRebuild,
                MixWriteHeaderKind.Classic,
                false,
                null,
                ushort.MaxValue,
                1024L * 1024 * 1024);

        public MixWriteOrder Order { get; }

        public MixWriteHeaderKind HeaderKind { get; }

        public bool IncludeChecksum { get; }

        public bool IsEncrypted => encryptionKeySource != null;

        public int MaxEntryCount { get; }

        public long MaxArchiveBytes { get; }

        internal byte[] GetEncryptionKeySource()
        {
            return encryptionKeySource == null
                ? null
                : (byte[])encryptionKeySource.Clone();
        }
    }
}
