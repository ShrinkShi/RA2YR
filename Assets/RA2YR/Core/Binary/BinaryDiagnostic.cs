using System;
using RA2YR.Core.Content;

namespace RA2YR.Core.Binary
{
    public enum BinaryDiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum BinaryDiagnosticCode
    {
        UnexpectedEndOfInput,
        InvalidOffset,
        InvalidLength,
        ArithmeticOverflow,
        InputBudgetExceeded,
        ReadBudgetExceeded,
        AllocationBudgetExceeded,
        RecordBudgetExceeded,
        StringBudgetExceeded,
        NestingBudgetExceeded,
        SubrangeBudgetExceeded,
        InvalidSubrange,
        UnsupportedSeekOperation,
        TrailingData,
        ReadFailure
    }

    public sealed class BinarySourceContext
    {
        internal BinarySourceContext(
            string parserName,
            string logicalSourceId,
            LogicalContentPath logicalPath)
        {
            ParserName = BinaryDiagnosticLabel.Validate(parserName, nameof(parserName));
            LogicalSourceId = BinaryDiagnosticLabel.Validate(
                logicalSourceId,
                nameof(logicalSourceId));
            LogicalPath = logicalPath ?? throw new ArgumentNullException(nameof(logicalPath));
        }

        public string ParserName { get; }

        public string LogicalSourceId { get; }

        public LogicalContentPath LogicalPath { get; }
    }

    public sealed class BinaryDiagnostic
    {
        internal BinaryDiagnostic(
            BinaryDiagnosticSeverity severity,
            BinaryDiagnosticCode code,
            BinarySourceContext source,
            long absoluteOffset,
            long requestedLength,
            long remainingLength,
            string fieldOrSection,
            string message)
        {
            Severity = severity;
            Code = code;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            AbsoluteOffset = absoluteOffset;
            RequestedLength = requestedLength;
            RemainingLength = remainingLength;
            FieldOrSection = BinaryDiagnosticLabel.Validate(
                fieldOrSection,
                nameof(fieldOrSection));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public BinaryDiagnosticSeverity Severity { get; }

        public BinaryDiagnosticCode Code { get; }

        public BinarySourceContext Source { get; }

        public long AbsoluteOffset { get; }

        public long RequestedLength { get; }

        public long RemainingLength { get; }

        public string FieldOrSection { get; }

        public string Message { get; }
    }

    public sealed class BinaryReadException : Exception
    {
        internal BinaryReadException(BinaryDiagnostic diagnostic)
            : base((diagnostic ?? throw new ArgumentNullException(nameof(diagnostic))).Message)
        {
            Diagnostic = diagnostic;
        }

        public BinaryDiagnostic Diagnostic { get; }
    }

    internal static class BinaryDiagnosticLabel
    {
        private const int MaximumLength = 256;

        public static string Validate(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A diagnostic label is required.", parameterName);
            }

            if (value.Length > MaximumLength)
            {
                throw new ArgumentException("A diagnostic label is too long.", parameterName);
            }

            foreach (char character in value)
            {
                if (char.IsControl(character) || character == '/' ||
                    character == '\\' || character == ':')
                {
                    throw new ArgumentException(
                        "Diagnostic labels cannot contain paths or control characters.",
                        parameterName);
                }
            }

            return value;
        }
    }
}
