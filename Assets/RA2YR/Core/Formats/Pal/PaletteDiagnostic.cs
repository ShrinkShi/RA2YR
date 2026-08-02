using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;

namespace RA2YR.Core.Formats.Pal
{
    internal enum PaletteChannel
    {
        Red,
        Green,
        Blue
    }

    internal enum PaletteDiagnosticCode
    {
        UnexpectedEndOfInput,
        InvalidLength,
        InvalidChannelValue,
        UnexpectedTrailingData,
        InputBudgetExceeded,
        ReadBudgetExceeded,
        AllocationBudgetExceeded,
        RecordBudgetExceeded,
        ArithmeticOverflow,
        UnsupportedSeekOperation,
        ReadFailure,
        BinaryReadFailure
    }

    internal sealed class PaletteSourceProvenance
    {
        private readonly IReadOnlyList<LogicalContentPath> logicalChain;

        public PaletteSourceProvenance(
            string sourceId,
            IEnumerable<LogicalContentPath> logicalChain)
        {
            SourceId = BinaryDiagnosticLabel.Validate(sourceId, nameof(sourceId));
            LogicalContentPath[] chain =
                (logicalChain ?? throw new ArgumentNullException(nameof(logicalChain))).ToArray();
            if (chain.Length == 0 || chain.Any(path => path == null))
            {
                throw new ArgumentException(
                    "Palette provenance requires a nonempty logical chain.",
                    nameof(logicalChain));
            }

            this.logicalChain = Array.AsReadOnly(chain);
        }

        public string SourceId { get; }

        public IReadOnlyList<LogicalContentPath> LogicalChain => logicalChain;
    }

    internal sealed class PaletteDiagnostic
    {
        internal PaletteDiagnostic(
            PaletteDiagnosticCode code,
            BinarySourceContext source,
            PaletteSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            int colorIndex,
            PaletteChannel? channel,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            if (colorIndex < -1 || colorIndex >= WestwoodPalette.ColorCount)
            {
                throw new ArgumentOutOfRangeException(nameof(colorIndex));
            }

            Code = code;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
            AbsoluteOffset = absoluteOffset;
            RequestedLength = requestedLength;
            RemainingLength = remainingLength;
            FieldOrSection = BinaryDiagnosticLabel.Validate(
                fieldOrSection,
                nameof(fieldOrSection));
            ColorIndex = colorIndex;
            Channel = channel;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            BinaryCode = binaryCode;
        }

        public PaletteDiagnosticCode Code { get; }

        public BinarySourceContext Source { get; }

        public PaletteSourceProvenance Provenance { get; }

        public long AbsoluteOffset { get; }

        public long RequestedLength { get; }

        public long RemainingLength { get; }

        public string FieldOrSection { get; }

        public int ColorIndex { get; }

        public PaletteChannel? Channel { get; }

        public string Message { get; }

        public BinaryDiagnosticCode? BinaryCode { get; }
    }

    internal sealed class PaletteReadException : Exception
    {
        public PaletteReadException(PaletteDiagnostic diagnostic)
            : base((diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))).Message)
        {
            Diagnostic = diagnostic;
        }

        public PaletteDiagnostic Diagnostic { get; }
    }
}
