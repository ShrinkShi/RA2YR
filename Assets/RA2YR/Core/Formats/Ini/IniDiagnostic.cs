using System;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.Ini
{
    internal enum IniDiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    internal enum IniDiagnosticCode
    {
        InputBudgetExceeded,
        ReadBudgetExceeded,
        LineCountBudgetExceeded,
        LineLengthBudgetExceeded,
        SectionBudgetExceeded,
        KeyValueBudgetExceeded,
        CommentBudgetExceeded,
        OpaqueBudgetExceeded,
        TotalNodeBudgetExceeded,
        CumulativeRawByteBudgetExceeded,
        AllocationBudgetExceeded,
        RecordBudgetExceeded,
        InvalidByteOrderMark,
        ByteOrderMarkLengthConflict,
        InvalidEncoding,
        NulCharacter,
        ArithmeticOverflow,
        UnexpectedEndOfInput,
        UnsupportedSeekOperation,
        ReadFailure,
        OpaqueLinePreserved,
        AmbiguousInlineSemicolon,
        BinaryReadFailure
    }

    internal sealed class IniDiagnostic
    {
        internal IniDiagnostic(
            IniDiagnosticSeverity severity,
            IniDiagnosticCode code,
            BinarySourceContext source,
            IniSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            int lineIndex,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            if (!Enum.IsDefined(typeof(IniDiagnosticSeverity), severity))
            {
                throw new ArgumentOutOfRangeException(nameof(severity));
            }

            if (!Enum.IsDefined(typeof(IniDiagnosticCode), code))
            {
                throw new ArgumentOutOfRangeException(nameof(code));
            }

            if (lineIndex < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(lineIndex));
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
            LineIndex = lineIndex;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            BinaryCode = binaryCode;
        }

        public IniDiagnosticSeverity Severity { get; }

        public IniDiagnosticCode Code { get; }

        public BinarySourceContext Source { get; }

        public IniSourceProvenance Provenance { get; }

        public long AbsoluteOffset { get; }

        public long RequestedLength { get; }

        public long RemainingLength { get; }

        public string FieldOrSection { get; }

        public int LineIndex { get; }

        public string Message { get; }

        public BinaryDiagnosticCode? BinaryCode { get; }
    }

    internal sealed class IniReadException : Exception
    {
        public IniReadException(IniDiagnostic diagnostic)
            : base((diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))).Message)
        {
            Diagnostic = diagnostic;
        }

        public IniDiagnostic Diagnostic { get; }
    }
}
