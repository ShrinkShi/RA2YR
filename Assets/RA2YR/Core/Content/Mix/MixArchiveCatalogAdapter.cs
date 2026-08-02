using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Core.Content.Mix
{
    /// <summary>
    /// Narrow boundary between the MIX format reader and content mounting.
    /// Offsets are relative to the beginning of the archive window.
    /// </summary>
    internal sealed class MixArchiveCatalogEntry
    {
        public MixArchiveCatalogEntry(
            MixFileId id,
            long payloadOffsetFromArchiveStart,
            long length,
            int observedOrder)
        {
            if (payloadOffsetFromArchiveStart < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(payloadOffsetFromArchiveStart));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (observedOrder < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(observedOrder));
            }

            Id = id;
            PayloadOffsetFromArchiveStart = payloadOffsetFromArchiveStart;
            Length = length;
            ObservedOrder = observedOrder;
        }

        public MixFileId Id { get; }

        public long PayloadOffsetFromArchiveStart { get; }

        public long Length { get; }

        public int ObservedOrder { get; }
    }

    internal sealed class MixArchiveCatalog
    {
        private MixArchiveCatalog(
            IEnumerable<MixArchiveCatalogEntry> entries,
            IEnumerable<MixDiagnostic> diagnostics,
            MixArchiveHeaderKind? headerKind,
            MixArchiveFlags flags,
            bool checksumVerified,
            bool isComplete)
        {
            MixArchiveCatalogEntry[] entryArray =
                (entries ?? throw new ArgumentNullException(nameof(entries))).ToArray();
            if (entryArray.Any(entry => entry == null))
            {
                throw new ArgumentException("Catalog entries may not contain null.", nameof(entries));
            }

            if (entryArray
                .GroupBy(entry => entry.ObservedOrder)
                .Any(group => group.Count() != 1))
            {
                throw new ArgumentException(
                    "Observed catalog order values must be unique.",
                    nameof(entries));
            }

            Entries = Array.AsReadOnly(entryArray
                .OrderBy(entry => entry.ObservedOrder)
                .ToArray());
            MixDiagnostic[] diagnosticArray =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            if (diagnosticArray.Any(diagnostic => diagnostic == null))
            {
                throw new ArgumentException(
                    "Catalog diagnostics may not contain null.",
                    nameof(diagnostics));
            }

            Diagnostics = Array.AsReadOnly(diagnosticArray);
            if (isComplete && (!headerKind.HasValue || diagnosticArray.Length != 0))
            {
                throw new ArgumentException(
                    "A complete catalog requires header metadata and no diagnostics.");
            }

            if (!isComplete && entryArray.Length != 0)
            {
                throw new ArgumentException(
                    "An incomplete catalog cannot expose partially trusted entries.");
            }

            HeaderKind = headerKind;
            Flags = flags;
            ChecksumVerified = checksumVerified;
            IsComplete = isComplete;
        }

        public IReadOnlyList<MixArchiveCatalogEntry> Entries { get; }

        public bool IsComplete { get; }

        public IReadOnlyList<MixDiagnostic> Diagnostics { get; }

        public MixArchiveHeaderKind? HeaderKind { get; }

        public MixArchiveFlags Flags { get; }

        public bool ChecksumVerified { get; }

        public static MixArchiveCatalog Complete(
            IEnumerable<MixArchiveCatalogEntry> entries,
            MixArchiveHeaderKind headerKind,
            MixArchiveFlags flags,
            bool checksumVerified)
        {
            return new MixArchiveCatalog(
                entries,
                Array.Empty<MixDiagnostic>(),
                headerKind,
                flags,
                checksumVerified,
                true);
        }

        public static MixArchiveCatalog Incomplete(
            IEnumerable<MixDiagnostic> diagnostics = null)
        {
            return new MixArchiveCatalog(
                Array.Empty<MixArchiveCatalogEntry>(),
                diagnostics ?? Array.Empty<MixDiagnostic>(),
                null,
                MixArchiveFlags.None,
                false,
                false);
        }
    }

    internal delegate MixArchiveCatalog MixArchiveCatalogReader(
        ReadOnlyDataWindow archiveWindow,
        BinarySourceContext sourceContext);

    internal static class MixArchiveCatalogAdapters
    {
        public static MixArchiveCatalog ReadWithCoreReader(
            ReadOnlyDataWindow archiveWindow,
            BinarySourceContext sourceContext)
        {
            MixArchiveReadResult result = MixArchiveReader.Read(
                archiveWindow,
                sourceContext,
                MixReadLimits.Default);
            if (!result.IsSuccess)
            {
                return MixArchiveCatalog.Incomplete(result.Diagnostics);
            }

            using (MixArchive archive = result.Archive)
            {
                return MixArchiveCatalog.Complete(
                    archive.Entries.Select(entry =>
                        new MixArchiveCatalogEntry(
                            entry.Id,
                            checked(archive.PayloadRelativeOffset + entry.RelativeOffset),
                            entry.Length,
                            entry.Index)),
                    archive.HeaderKind,
                    archive.Flags,
                    archive.ChecksumVerified);
            }
        }
    }
}
