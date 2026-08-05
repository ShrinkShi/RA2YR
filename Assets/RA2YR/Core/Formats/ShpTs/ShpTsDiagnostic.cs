using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;

namespace RA2YR.Core.Formats.ShpTs
{
    internal enum ShpTsDiagnosticCode
    {
        UnexpectedEndOfInput,
        InvalidFamilyMarker,
        ZeroFrameCount,
        DirectorySizeOverflow,
        FrameCountBudgetExceeded,
        CanvasDimensionBudgetExceeded,
        CanvasAreaBudgetExceeded,
        LocalFrameAreaBudgetExceeded,
        InputBudgetExceeded,
        ReadBudgetExceeded,
        AllocationBudgetExceeded,
        RecordBudgetExceeded,
        DescriptorBudgetExceeded,
        SubwindowBudgetExceeded,
        DiagnosticBudgetExceeded,
        ArithmeticOverflow,
        InvalidDataOffset,
        DataOffsetInsideDirectory,
        DataOffsetOutsideInput,
        FrameRectangleOutsideCanvas,
        CoordinateSignednessUnresolved,
        PartialEmptyFrame,
        EmptyFrameCoordinatesNonZero,
        ReservedFieldNonZero,
        DataOffsetNotEightByteAligned,
        DuplicateDataOffset,
        DescendingDataOffset,
        FrameDataOverlap,
        SourceConflictingFlags2,
        UnknownFlags,
        RawPayloadTruncated,
        RleLineLengthTooSmall,
        RleLineLengthBudgetExceeded,
        RleLineTruncated,
        RleDanglingZeroCommand,
        ZeroOutputCommandSemanticsUnresolved,
        RleOutputUnderflow,
        RleOutputOverflow,
        RleLineTrailingData,
        RleRowCountIncomplete,
        CommandBudgetExceeded,
        CompressedFrameBudgetExceeded,
        TotalDecodedPixelBudgetExceeded,
        UnsupportedSeekOperation,
        ReadFailure,
        BinaryReadFailure
    }

    internal sealed class ShpTsSourceProvenance
    {
        private readonly IReadOnlyList<LogicalContentPath> logicalChain;

        public ShpTsSourceProvenance(
            string sourceId,
            IEnumerable<LogicalContentPath> logicalChain)
        {
            SourceId = BinaryDiagnosticLabel.Validate(sourceId, nameof(sourceId));
            LogicalContentPath[] chain =
                (logicalChain ?? throw new ArgumentNullException(nameof(logicalChain))).ToArray();
            if (chain.Length == 0 || chain.Any(path => path == null))
            {
                throw new ArgumentException(
                    "SHP provenance requires a nonempty logical chain.",
                    nameof(logicalChain));
            }

            this.logicalChain = Array.AsReadOnly(chain);
        }

        public string SourceId { get; }

        public IReadOnlyList<LogicalContentPath> LogicalChain => logicalChain;
    }

    internal sealed class ShpTsDiagnostic
    {
        internal ShpTsDiagnostic(
            BinaryDiagnosticSeverity severity,
            ShpTsDiagnosticCode code,
            BinarySourceContext source,
            ShpTsSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            int frameIndex,
            int rowIndex,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            if (frameIndex < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(frameIndex));
            }

            if (rowIndex < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(rowIndex));
            }

            Severity = severity;
            Code = code;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            AbsoluteOffset = absoluteOffset;
            RequestedLength = requestedLength;
            RemainingLength = remainingLength;
            FieldOrSection = BinaryDiagnosticLabel.Validate(
                fieldOrSection,
                nameof(fieldOrSection));
            FrameIndex = frameIndex;
            RowIndex = rowIndex;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            BinaryCode = binaryCode;
        }

        public BinaryDiagnosticSeverity Severity { get; }

        public ShpTsDiagnosticCode Code { get; }

        public BinarySourceContext Source { get; }

        public ShpTsSourceProvenance Provenance { get; }

        public long AbsoluteOffset { get; }

        public long RequestedLength { get; }

        public long RemainingLength { get; }

        public string FieldOrSection { get; }

        public int FrameIndex { get; }

        public int RowIndex { get; }

        public string Message { get; }

        public BinaryDiagnosticCode? BinaryCode { get; }
    }

    internal sealed class ShpTsReadException : Exception
    {
        public ShpTsReadException(ShpTsDiagnostic diagnostic)
            : base((diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))).Message)
        {
            Diagnostic = diagnostic;
        }

        public ShpTsDiagnostic Diagnostic { get; }
    }
}
