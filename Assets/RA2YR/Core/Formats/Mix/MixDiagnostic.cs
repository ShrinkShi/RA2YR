using System;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.Mix
{
    internal enum MixDiagnosticCode
    {
        TruncatedHeader,
        UnsupportedFlags,
        EntryCountBudgetExceeded,
        DirectorySizeOverflow,
        DirectoryBudgetExceeded,
        AllocationBudgetExceeded,
        TruncatedDirectory,
        TruncatedDataRegion,
        TruncatedChecksum,
        UnexpectedTrailingData,
        DecryptionFailed,
        ChecksumMismatch,
        DuplicateEntryId,
        EntryRangeOverflow,
        EntryOutsideDataRegion,
        OverlappingEntries,
        BinaryReadFailure,
        ArithmeticOverflow
    }

    internal sealed class MixDiagnostic
    {
        internal MixDiagnostic(
            MixDiagnosticCode code,
            BinarySourceContext source,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            int entryIndex,
            MixFileId? entryId,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            Code = code;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            AbsoluteOffset = absoluteOffset;
            RequestedLength = requestedLength;
            RemainingLength = remainingLength;
            FieldOrSection = BinaryDiagnosticLabel.Validate(
                fieldOrSection,
                nameof(fieldOrSection));
            EntryIndex = entryIndex;
            EntryId = entryId;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            BinaryCode = binaryCode;
        }

        public MixDiagnosticCode Code { get; }

        public BinarySourceContext Source { get; }

        public long AbsoluteOffset { get; }

        public long RequestedLength { get; }

        public long RemainingLength { get; }

        public string FieldOrSection { get; }

        public int EntryIndex { get; }

        public MixFileId? EntryId { get; }

        public string Message { get; }

        public BinaryDiagnosticCode? BinaryCode { get; }
    }

    internal sealed class MixReadException : Exception
    {
        internal MixReadException(MixDiagnostic diagnostic)
            : base((diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))).Message)
        {
            Diagnostic = diagnostic;
        }

        public MixDiagnostic Diagnostic { get; }
    }
}
