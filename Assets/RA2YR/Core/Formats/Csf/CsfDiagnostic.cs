using System;
using RA2YR.Core.Binary;

namespace RA2YR.Core.Formats.Csf
{
    internal enum CsfDiagnosticCode
    {
        UnexpectedEndOfInput,
        InvalidLength,
        InvalidSignature,
        UnsupportedVersion,
        InvalidLabelMarker,
        InvalidValueMarker,
        InvalidAsciiByte,
        DeclaredLabelCountMismatch,
        DeclaredValueCountMismatch,
        LabelBudgetExceeded,
        TotalValueBudgetExceeded,
        ValuesPerLabelBudgetExceeded,
        LabelNameBudgetExceeded,
        MainTextBudgetExceeded,
        ExtraTextBudgetExceeded,
        CumulativeCodeUnitBudgetExceeded,
        UnexpectedTrailingData,
        InputBudgetExceeded,
        ReadBudgetExceeded,
        AllocationBudgetExceeded,
        RecordBudgetExceeded,
        StringBudgetExceeded,
        ArithmeticOverflow,
        UnsupportedSeekOperation,
        ReadFailure,
        BinaryReadFailure
    }

    internal sealed class CsfDiagnostic
    {
        internal CsfDiagnostic(
            CsfDiagnosticCode code,
            BinarySourceContext source,
            CsfSourceProvenance provenance,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            int labelIndex,
            int valueIndex,
            uint? rawRecordMarker,
            string message,
            BinaryDiagnosticCode? binaryCode = null)
        {
            if (labelIndex < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(labelIndex));
            }

            if (valueIndex < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(valueIndex));
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
            LabelIndex = labelIndex;
            ValueIndex = valueIndex;
            RawRecordMarker = rawRecordMarker;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            BinaryCode = binaryCode;
        }

        public CsfDiagnosticCode Code { get; }

        public BinarySourceContext Source { get; }

        public CsfSourceProvenance Provenance { get; }

        public long AbsoluteOffset { get; }

        public long RequestedLength { get; }

        public long RemainingLength { get; }

        public string FieldOrSection { get; }

        public int LabelIndex { get; }

        public int ValueIndex { get; }

        public uint? RawRecordMarker { get; }

        public string Message { get; }

        public BinaryDiagnosticCode? BinaryCode { get; }
    }

    internal sealed class CsfReadException : Exception
    {
        public CsfReadException(CsfDiagnostic diagnostic)
            : base((diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))).Message)
        {
            Diagnostic = diagnostic;
        }

        public CsfDiagnostic Diagnostic { get; }
    }
}
