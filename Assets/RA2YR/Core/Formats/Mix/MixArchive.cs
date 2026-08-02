using System;
using System.Collections.Generic;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;

namespace RA2YR.Core.Formats.Mix
{
    internal sealed class MixArchive : IDisposable
    {
        private readonly IReadOnlyList<MixArchiveEntry> entries;
        private readonly byte[] keySource;
        private readonly IDisposable ownedInput;
        private bool disposed;

        internal MixArchive(
            BinarySourceContext source,
            MixArchiveHeaderKind headerKind,
            MixArchiveFlags flags,
            uint declaredDataSize,
            long payloadRelativeOffset,
            ReadOnlyDataWindow payloadWindow,
            IList<MixArchiveEntry> entries,
            byte[] keySource,
            bool checksumVerified,
            IDisposable ownedInput)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            HeaderKind = headerKind;
            Flags = flags;
            DeclaredDataSize = declaredDataSize;
            PayloadRelativeOffset = payloadRelativeOffset;
            PayloadWindow = payloadWindow ??
                throw new ArgumentNullException(nameof(payloadWindow));
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            this.entries = new List<MixArchiveEntry>(entries).AsReadOnly();
            this.keySource = keySource == null ? null : (byte[])keySource.Clone();
            ChecksumVerified = checksumVerified;
            this.ownedInput = ownedInput;
        }

        public BinarySourceContext Source { get; }

        public MixArchiveHeaderKind HeaderKind { get; }

        public MixArchiveFlags Flags { get; }

        public uint DeclaredDataSize { get; }

        public long PayloadRelativeOffset { get; }

        public ReadOnlyDataWindow PayloadWindow { get; }

        public IReadOnlyList<MixArchiveEntry> Entries => entries;

        public bool ChecksumVerified { get; }

        public bool IsEncrypted =>
            (Flags & MixArchiveFlags.EncryptedDirectory) != 0;

        public bool HasChecksum =>
            (Flags & MixArchiveFlags.Checksum) != 0;

        public byte[] GetKeySource()
        {
            return keySource == null ? Array.Empty<byte>() : (byte[])keySource.Clone();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            ownedInput?.Dispose();
        }
    }
}
