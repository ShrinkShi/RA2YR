using System;
using System.Collections.Generic;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Configuration.Ini.Typed
{
    internal static class IniTypedScalarParser
    {
        public static IniTypedParseResult ParseRaw(
            IniResolvedValue value,
            IniTypedViewLimits limits = null)
        {
            ScalarInput input;
            IniTypedParseResult failure;
            if (!TryPrepare(value, limits, out input, out failure))
            {
                return failure;
            }

            return IniTypedParseResult.Present(IniTypedValue.Raw(input.Bytes, input.Trace));
        }

        public static IniTypedParseResult ParseAsciiIdentifier(
            IniResolvedValue value,
            IniTypedViewLimits limits = null)
        {
            ScalarInput input;
            IniTypedParseResult failure;
            if (!TryPrepare(value, limits, out input, out failure))
            {
                return failure;
            }

            string text;
            if (!TryDecodeAscii(input.Bytes, input.Encoding, out text) ||
                !IsIdentifier(text))
            {
                return Invalid(
                    input,
                    IniTypedDiagnosticCode.InvalidAsciiIdentifier,
                    "The selected INI value is not an explicit bounded ASCII identifier.");
            }

            return IniTypedParseResult.Present(
                IniTypedValue.AsciiIdentifier(input.Bytes, input.Trace, text));
        }

        public static IniTypedParseResult ParseBoolean(
            IniResolvedValue value,
            IniBooleanCasePolicy casePolicy,
            IniTypedViewLimits limits = null)
        {
            if (!Enum.IsDefined(typeof(IniBooleanCasePolicy), casePolicy))
            {
                throw new ArgumentOutOfRangeException(nameof(casePolicy));
            }

            ScalarInput input;
            IniTypedParseResult failure;
            if (!TryPrepare(value, limits, out input, out failure))
            {
                return failure;
            }

            string text;
            if (!TryDecodeAscii(input.Bytes, input.Encoding, out text))
            {
                return Invalid(
                    input,
                    IniTypedDiagnosticCode.InvalidBoolean,
                    "The selected INI value is not an explicit yes or no token.");
            }

            StringComparison comparison = casePolicy == IniBooleanCasePolicy.OrdinalLowercase
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
            if (string.Equals(text, "yes", comparison))
            {
                return IniTypedParseResult.Present(
                    IniTypedValue.Boolean(input.Bytes, input.Trace, true));
            }

            if (string.Equals(text, "no", comparison))
            {
                return IniTypedParseResult.Present(
                    IniTypedValue.Boolean(input.Bytes, input.Trace, false));
            }

            return Invalid(
                input,
                IniTypedDiagnosticCode.InvalidBoolean,
                "The selected INI value is not an explicit yes or no token.");
        }

        public static IniTypedParseResult ParseNonNegativeInteger(
            IniResolvedValue value,
            IniTypedViewLimits limits = null)
        {
            ScalarInput input;
            IniTypedParseResult failure;
            if (!TryPrepare(value, limits, out input, out failure))
            {
                return failure;
            }

            string text;
            if (!TryDecodeAscii(input.Bytes, input.Encoding, out text) || text.Length == 0)
            {
                return Invalid(
                    input,
                    IniTypedDiagnosticCode.InvalidNonNegativeInteger,
                    "The selected INI value is not a non-negative decimal integer.");
            }

            int result = 0;
            for (int index = 0; index < text.Length; index++)
            {
                int digit = text[index] - '0';
                if (digit < 0 || digit > 9)
                {
                    return Invalid(
                        input,
                        IniTypedDiagnosticCode.InvalidNonNegativeInteger,
                        "The selected INI value is not a non-negative decimal integer.");
                }

                try
                {
                    result = checked((result * 10) + digit);
                }
                catch (OverflowException)
                {
                    return Invalid(
                        input,
                        IniTypedDiagnosticCode.IntegerOverflow,
                        "The selected INI integer exceeds the supported bounded range.");
                }
            }

            return IniTypedParseResult.Present(
                IniTypedValue.NonNegativeInteger(input.Bytes, input.Trace, result));
        }

        public static IniTypedParseResult ParseIdentifierList(
            IniResolvedValue value,
            IniTypedViewLimits limits = null)
        {
            ScalarInput input;
            IniTypedParseResult failure;
            if (!TryPrepare(value, limits, out input, out failure))
            {
                return failure;
            }

            string text;
            if (!TryDecodeAscii(input.Bytes, input.Encoding, out text))
            {
                return Invalid(
                    input,
                    IniTypedDiagnosticCode.InvalidAsciiIdentifier,
                    "The selected INI list is not an explicit ASCII identifier list.");
            }

            string[] items = text.Split(new[] { ',' }, StringSplitOptions.None);
            if (items.Length > input.Limits.MaxListItems)
            {
                return IniTypedParseResult.Failure(CreateDiagnostic(
                    input.Value,
                    IniTypedDiagnosticCode.ListItemBudgetExceeded,
                    "The typed identifier-list item budget was exceeded."));
            }

            var parsed = new List<string>(items.Length);
            foreach (string item in items)
            {
                if (item.Length == 0)
                {
                    return Invalid(
                        input,
                        IniTypedDiagnosticCode.EmptyIdentifierListItem,
                        "The selected INI identifier list contains an empty item.");
                }

                if (!IsIdentifier(item))
                {
                    return Invalid(
                        input,
                        IniTypedDiagnosticCode.InvalidAsciiIdentifier,
                        "The selected INI list contains an invalid ASCII identifier.");
                }

                parsed.Add(item);
            }

            return IniTypedParseResult.Present(
                IniTypedValue.IdentifierList(input.Bytes, input.Trace, parsed));
        }

        internal static bool TryDecodeAscii(
            byte[] bytes,
            IniPhysicalEncodingKind encoding,
            out string value)
        {
            int width = IniPhysicalAscii.GetUnitWidth(encoding);
            if (bytes == null || bytes.Length % width != 0)
            {
                value = null;
                return false;
            }

            var characters = new char[bytes.Length / width];
            for (int index = 0; index < characters.Length; index++)
            {
                int unit = IniPhysicalAscii.ReadUnit(bytes, index * width, encoding);
                if (unit <= 0 || unit > 0x7f)
                {
                    value = null;
                    return false;
                }

                characters[index] = (char)unit;
            }

            value = new string(characters);
            return true;
        }

        internal static bool IsIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (char character in value)
            {
                if (character < 0x21 || character > 0x7e ||
                    character == ',' || character == ';' || character == '=' ||
                    character == '[' || character == ']')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryPrepare(
            IniResolvedValue value,
            IniTypedViewLimits limits,
            out ScalarInput input,
            out IniTypedParseResult failure)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            limits = limits ?? IniTypedViewLimits.Default;
            byte[] bytes = value.Winner.CopyEffectiveValueBytes();
            if (bytes.Length > limits.MaxScalarBytes)
            {
                input = default(ScalarInput);
                failure = IniTypedParseResult.Failure(CreateDiagnostic(
                    value,
                    IniTypedDiagnosticCode.ScalarBudgetExceeded,
                    "The typed scalar byte budget was exceeded."));
                return false;
            }

            if (value.CandidateChain.Count > limits.MaxSourceCandidates)
            {
                input = default(ScalarInput);
                failure = IniTypedParseResult.Failure(CreateDiagnostic(
                    value,
                    IniTypedDiagnosticCode.IncompleteSourceTrace,
                    "The typed source candidate budget was exceeded."));
                return false;
            }

            IniValueSourceTrace trace = IniValueSourceTrace.FromResolvedValue(
                value,
                limits.MaxSourceCandidates);
            input = new ScalarInput(
                value,
                bytes,
                trace,
                value.Winner.Document.Document.PhysicalEncoding,
                limits);
            failure = null;
            return true;
        }

        private static IniTypedParseResult Invalid(
            ScalarInput input,
            IniTypedDiagnosticCode code,
            string message)
        {
            return IniTypedParseResult.Invalid(
                IniTypedValue.Raw(input.Bytes, input.Trace),
                CreateDiagnostic(input.Value, code, message));
        }

        private static IniTypedDiagnostic CreateDiagnostic(
            IniResolvedValue value,
            IniTypedDiagnosticCode code,
            string message)
        {
            IniResolvedValueCandidate winner = value.Winner;
            return new IniTypedDiagnostic(
                code,
                IniTypedDiagnosticSeverity.Error,
                IniTypedTargetKind.Scalar,
                message,
                winner.Document.LogicalName,
                winner.Document.CandidateId,
                winner.KeyLineId);
        }

        private readonly struct ScalarInput
        {
            public ScalarInput(
                IniResolvedValue value,
                byte[] bytes,
                IniValueSourceTrace trace,
                IniPhysicalEncodingKind encoding,
                IniTypedViewLimits limits)
            {
                Value = value;
                Bytes = bytes;
                Trace = trace;
                Encoding = encoding;
                Limits = limits;
            }

            public IniResolvedValue Value { get; }
            public byte[] Bytes { get; }
            public IniValueSourceTrace Trace { get; }
            public IniPhysicalEncodingKind Encoding { get; }
            public IniTypedViewLimits Limits { get; }
        }
    }
}
