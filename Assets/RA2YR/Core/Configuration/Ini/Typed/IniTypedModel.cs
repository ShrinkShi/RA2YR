using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Content;

namespace RA2YR.Core.Configuration.Ini.Typed
{
    internal enum IniTypedValueKind
    {
        RawBytes,
        AsciiIdentifier,
        Boolean,
        NonNegativeInteger,
        IdentifierList
    }

    internal enum IniTypedValueStatus
    {
        Present,
        Missing,
        Invalid,
        Ambiguous,
        Failed
    }

    internal enum IniTypedViewStatus
    {
        Complete,
        Incomplete,
        Ambiguous,
        Failed
    }

    internal enum IniTypedDiagnosticSeverity
    {
        Warning,
        Error
    }

    internal enum IniTypedDiagnosticCode
    {
        InputResolutionAmbiguous,
        InputResolutionFailed,
        InvalidAsciiIdentifier,
        InvalidBoolean,
        InvalidNonNegativeInteger,
        IntegerOverflow,
        EmptyIdentifierListItem,
        ScalarBudgetExceeded,
        ListItemBudgetExceeded,
        RegistryEntryBudgetExceeded,
        ArtRecordBudgetExceeded,
        DiagnosticBudgetExceeded,
        IncompleteSourceTrace,
        OpaqueMayAffectTarget,
        InlineSemicolonMayAffectTarget,
        DuplicateSectionMayAffectTarget,
        DuplicateKeyMayAffectTarget,
        DuplicateRegistryIdentifier,
        InvalidRegistryOrdinal,
        ArtSectionMissing,
        ArtSectionAmbiguous,
        InvalidArtField
    }

    internal enum IniTypedTargetKind
    {
        InputResolution,
        Scalar,
        RulesRegistry,
        ArtSection,
        ArtField,
        SourceTrace
    }

    internal enum IniBooleanCasePolicy
    {
        OrdinalLowercase,
        OrdinalIgnoreCaseAscii
    }

    internal enum IniTypedNameComparisonPolicy
    {
        Ordinal,
        OrdinalIgnoreCaseAscii
    }

    internal sealed class IniTypedViewLimits
    {
        public IniTypedViewLimits(
            int maxScalarBytes,
            int maxListItems,
            int maxRegistryEntries,
            int maxArtRecords,
            int maxSourceCandidates,
            int maxDiagnostics)
        {
            if (maxScalarBytes <= 0 || maxListItems <= 0 ||
                maxRegistryEntries <= 0 || maxArtRecords <= 0 ||
                maxSourceCandidates <= 0 || maxDiagnostics <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxScalarBytes));
            }

            MaxScalarBytes = maxScalarBytes;
            MaxListItems = maxListItems;
            MaxRegistryEntries = maxRegistryEntries;
            MaxArtRecords = maxArtRecords;
            MaxSourceCandidates = maxSourceCandidates;
            MaxDiagnostics = maxDiagnostics;
        }

        public static IniTypedViewLimits Default { get; } =
            new IniTypedViewLimits(16 * 1024, 4096, 100_000, 250_000, 4096, 100_000);

        public int MaxScalarBytes { get; }
        public int MaxListItems { get; }
        public int MaxRegistryEntries { get; }
        public int MaxArtRecords { get; }
        public int MaxSourceCandidates { get; }
        public int MaxDiagnostics { get; }
    }

    internal sealed class IniTypedDiagnostic
    {
        public IniTypedDiagnostic(
            IniTypedDiagnosticCode code,
            IniTypedDiagnosticSeverity severity,
            IniTypedTargetKind target,
            string message,
            LogicalContentPath logicalPath = null,
            string candidateId = null,
            int? physicalLineId = null)
        {
            if (!Enum.IsDefined(typeof(IniTypedDiagnosticCode), code) ||
                !Enum.IsDefined(typeof(IniTypedDiagnosticSeverity), severity) ||
                !Enum.IsDefined(typeof(IniTypedTargetKind), target))
            {
                throw new ArgumentOutOfRangeException(nameof(code));
            }

            if (physicalLineId.HasValue && physicalLineId.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalLineId));
            }

            Code = code;
            Severity = severity;
            Target = target;
            Message = BinaryDiagnosticLabel.Validate(message, nameof(message));
            LogicalPath = logicalPath;
            CandidateId = candidateId;
            PhysicalLineId = physicalLineId;
        }

        public IniTypedDiagnosticCode Code { get; }
        public IniTypedDiagnosticSeverity Severity { get; }
        public IniTypedTargetKind Target { get; }
        public string Message { get; }
        public LogicalContentPath LogicalPath { get; }
        public string CandidateId { get; }
        public int? PhysicalLineId { get; }
    }

    internal sealed class IniValueSourceCandidateTrace
    {
        private readonly IReadOnlyList<LogicalContentPath> logicalChain;

        internal IniValueSourceCandidateTrace(IniResolvedValueCandidate candidate)
        {
            if (candidate == null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            IniCandidateDocument document = candidate.Document;
            CandidateId = document.CandidateId;
            LogicalName = document.LogicalName;
            SourceId = document.Document.Provenance.SourceId;
            logicalChain = Array.AsReadOnly(document.Document.Provenance.LogicalChain.ToArray());
            SectionPhysicalLineId = candidate.SectionLineId;
            KeyPhysicalLineId = candidate.KeyLineId;
            Disposition = candidate.Disposition;
            ContainsInlineSemicolon = candidate.ContainsInlineSemicolon;
        }

        public string CandidateId { get; }
        public LogicalContentPath LogicalName { get; }
        public string SourceId { get; }
        public IReadOnlyList<LogicalContentPath> LogicalChain => logicalChain;
        public int SectionPhysicalLineId { get; }
        public int KeyPhysicalLineId { get; }
        public IniValueCandidateDisposition Disposition { get; }
        public bool ContainsInlineSemicolon { get; }
    }

    internal sealed class IniValueSourceTrace
    {
        private readonly IReadOnlyList<IniValueSourceCandidateTrace> candidates;

        private IniValueSourceTrace(
            IniValueSourceCandidateTrace winner,
            IEnumerable<IniValueSourceCandidateTrace> candidates)
        {
            Winner = winner ?? throw new ArgumentNullException(nameof(winner));
            IniValueSourceCandidateTrace[] values =
                (candidates ?? throw new ArgumentNullException(nameof(candidates))).ToArray();
            if (values.Length == 0 || values.Any(value => value == null) ||
                values.Count(value => value.Disposition == IniValueCandidateDisposition.Winner) != 1 ||
                !values.Contains(winner))
            {
                throw new ArgumentException("A typed source trace requires one winner.");
            }

            this.candidates = Array.AsReadOnly(values);
        }

        public IniValueSourceCandidateTrace Winner { get; }
        public IReadOnlyList<IniValueSourceCandidateTrace> Candidates => candidates;

        internal static IniValueSourceTrace FromResolvedValue(
            IniResolvedValue value,
            int maxCandidates)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (maxCandidates <= 0 || value.CandidateChain.Count > maxCandidates)
            {
                throw new InvalidOperationException(
                    "The typed source candidate budget was exceeded.");
            }

            IniValueSourceCandidateTrace[] candidates = value.CandidateChain
                .Select(candidate => new IniValueSourceCandidateTrace(candidate))
                .ToArray();
            int winnerIndex = Array.FindIndex(
                candidates,
                candidate => candidate.Disposition == IniValueCandidateDisposition.Winner);
            return new IniValueSourceTrace(candidates[winnerIndex], candidates);
        }
    }

    internal sealed class IniTypedValue
    {
        private readonly byte[] rawBytes;
        private readonly IReadOnlyList<string> identifiers;

        private IniTypedValue(
            IniTypedValueKind kind,
            byte[] rawBytes,
            IniValueSourceTrace sourceTrace,
            string identifier,
            bool? booleanValue,
            int? integerValue,
            IEnumerable<string> identifiers)
        {
            if (!Enum.IsDefined(typeof(IniTypedValueKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            Kind = kind;
            this.rawBytes = (rawBytes ?? throw new ArgumentNullException(nameof(rawBytes))).ToArray();
            SourceTrace = sourceTrace ?? throw new ArgumentNullException(nameof(sourceTrace));
            Identifier = identifier;
            BooleanValue = booleanValue;
            IntegerValue = integerValue;
            this.identifiers = Array.AsReadOnly((identifiers ?? Array.Empty<string>()).ToArray());
        }

        public IniTypedValueKind Kind { get; }
        public IniValueSourceTrace SourceTrace { get; }
        public string Identifier { get; }
        public bool? BooleanValue { get; }
        public int? IntegerValue { get; }
        public IReadOnlyList<string> Identifiers => identifiers;

        public byte[] CopyRawBytes()
        {
            return rawBytes.ToArray();
        }

        internal static IniTypedValue Raw(byte[] bytes, IniValueSourceTrace trace)
        {
            return new IniTypedValue(
                IniTypedValueKind.RawBytes, bytes, trace, null, null, null, null);
        }

        internal static IniTypedValue AsciiIdentifier(
            byte[] bytes,
            IniValueSourceTrace trace,
            string value)
        {
            return new IniTypedValue(
                IniTypedValueKind.AsciiIdentifier,
                bytes,
                trace,
                value ?? throw new ArgumentNullException(nameof(value)),
                null,
                null,
                null);
        }

        internal static IniTypedValue Boolean(
            byte[] bytes,
            IniValueSourceTrace trace,
            bool value)
        {
            return new IniTypedValue(
                IniTypedValueKind.Boolean, bytes, trace, null, value, null, null);
        }

        internal static IniTypedValue NonNegativeInteger(
            byte[] bytes,
            IniValueSourceTrace trace,
            int value)
        {
            return new IniTypedValue(
                IniTypedValueKind.NonNegativeInteger,
                bytes,
                trace,
                null,
                null,
                value,
                null);
        }

        internal static IniTypedValue IdentifierList(
            byte[] bytes,
            IniValueSourceTrace trace,
            IEnumerable<string> values)
        {
            return new IniTypedValue(
                IniTypedValueKind.IdentifierList,
                bytes,
                trace,
                null,
                null,
                null,
                values ?? throw new ArgumentNullException(nameof(values)));
        }
    }

    internal sealed class IniTypedParseResult
    {
        private readonly IReadOnlyList<IniTypedDiagnostic> diagnostics;

        private IniTypedParseResult(
            IniTypedValueStatus status,
            IniTypedValue value,
            IEnumerable<IniTypedDiagnostic> diagnostics)
        {
            if (status != IniTypedValueStatus.Present &&
                status != IniTypedValueStatus.Invalid &&
                status != IniTypedValueStatus.Failed)
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            IniTypedDiagnostic[] values =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            bool hasError = values.Any(diagnostic =>
                diagnostic.Severity == IniTypedDiagnosticSeverity.Error);
            if (values.Any(diagnostic => diagnostic == null) ||
                (status != IniTypedValueStatus.Failed && value == null) ||
                (status == IniTypedValueStatus.Failed && value != null) ||
                (status == IniTypedValueStatus.Present && hasError) ||
                (status != IniTypedValueStatus.Present && !hasError))
            {
                throw new ArgumentException("A typed scalar result is inconsistent.");
            }

            Status = status;
            Value = value;
            this.diagnostics = Array.AsReadOnly(values);
        }

        public IniTypedValueStatus Status { get; }
        public IniTypedValue Value { get; }
        public IReadOnlyList<IniTypedDiagnostic> Diagnostics => diagnostics;

        internal static IniTypedParseResult Present(IniTypedValue value)
        {
            return new IniTypedParseResult(
                IniTypedValueStatus.Present,
                value,
                Array.Empty<IniTypedDiagnostic>());
        }

        internal static IniTypedParseResult Invalid(
            IniTypedValue rawValue,
            IniTypedDiagnostic diagnostic)
        {
            return new IniTypedParseResult(
                IniTypedValueStatus.Invalid,
                rawValue,
                new[] { diagnostic ?? throw new ArgumentNullException(nameof(diagnostic)) });
        }

        internal static IniTypedParseResult Failure(IniTypedDiagnostic diagnostic)
        {
            return new IniTypedParseResult(
                IniTypedValueStatus.Failed,
                null,
                new[] { diagnostic ?? throw new ArgumentNullException(nameof(diagnostic)) });
        }
    }

    internal sealed class IniTypedViewResult<TDocument>
        where TDocument : class
    {
        private readonly IReadOnlyList<IniTypedDiagnostic> diagnostics;
        private readonly IReadOnlyList<IniResolutionDiagnostic> inputDiagnostics;

        private IniTypedViewResult(
            IniTypedViewStatus status,
            TDocument document,
            IniResolutionResult input,
            IEnumerable<IniTypedDiagnostic> diagnostics)
        {
            if (!Enum.IsDefined(typeof(IniTypedViewStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            InputStatus = (input ?? throw new ArgumentNullException(nameof(input))).Status;
            InputTrace = input.Trace;
            inputDiagnostics = Array.AsReadOnly(input.Diagnostics.ToArray());
            IniTypedDiagnostic[] values =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            if (values.Any(value => value == null) ||
                ((status == IniTypedViewStatus.Ambiguous || status == IniTypedViewStatus.Failed) &&
                 document != null) ||
                ((status == IniTypedViewStatus.Complete || status == IniTypedViewStatus.Incomplete) &&
                 document == null))
            {
                throw new ArgumentException("The typed view result is inconsistent.");
            }

            Status = status;
            Document = document;
            this.diagnostics = Array.AsReadOnly(values);
        }

        public IniTypedViewStatus Status { get; }
        public TDocument Document { get; }
        public IniResolutionStatus InputStatus { get; }
        public IniResolutionTrace InputTrace { get; }
        public IReadOnlyList<IniResolutionDiagnostic> InputDiagnostics => inputDiagnostics;
        public IReadOnlyList<IniTypedDiagnostic> Diagnostics => diagnostics;

        internal static IniTypedViewResult<TDocument> RejectIncompleteInput(
            IniResolutionResult input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            IniTypedViewStatus status = input.Status == IniResolutionStatus.Ambiguous
                ? IniTypedViewStatus.Ambiguous
                : IniTypedViewStatus.Failed;
            IniTypedDiagnosticCode code = input.Status == IniResolutionStatus.Ambiguous
                ? IniTypedDiagnosticCode.InputResolutionAmbiguous
                : IniTypedDiagnosticCode.InputResolutionFailed;
            return new IniTypedViewResult<TDocument>(
                status,
                null,
                input,
                new[]
                {
                    new IniTypedDiagnostic(
                        code,
                        IniTypedDiagnosticSeverity.Error,
                        IniTypedTargetKind.InputResolution,
                        "Typed views require a complete explicit INI resolution result.")
                });
        }

        internal static IniTypedViewResult<TDocument> Create(
            TDocument document,
            IniResolutionResult input,
            IEnumerable<IniTypedDiagnostic> diagnostics)
        {
            IniTypedDiagnostic[] values =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            IniTypedViewStatus status = values.Count(value =>
                    value.Severity == IniTypedDiagnosticSeverity.Error) == 0
                ? IniTypedViewStatus.Complete
                : IniTypedViewStatus.Incomplete;
            return new IniTypedViewResult<TDocument>(status, document, input, values);
        }

        internal static IniTypedViewResult<TDocument> FailCompleteInput(
            IniResolutionResult input,
            IniTypedDiagnostic diagnostic)
        {
            if (input == null || !input.IsComplete)
            {
                throw new ArgumentException(
                    "This failure factory requires a complete resolution input.",
                    nameof(input));
            }

            return new IniTypedViewResult<TDocument>(
                IniTypedViewStatus.Failed,
                null,
                input,
                new[] { diagnostic ?? throw new ArgumentNullException(nameof(diagnostic)) });
        }
    }
}
