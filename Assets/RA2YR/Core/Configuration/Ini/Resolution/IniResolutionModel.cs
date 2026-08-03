using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Configuration.Ini.Resolution
{
    internal sealed class IniCandidateDocument
    {
        public IniCandidateDocument(
            string candidateId,
            string layerId,
            LogicalContentPath logicalName,
            IniRawDocument document)
        {
            CandidateId = BinaryDiagnosticLabel.Validate(candidateId, nameof(candidateId));
            LayerId = BinaryDiagnosticLabel.Validate(layerId, nameof(layerId));
            LogicalName = logicalName ?? throw new ArgumentNullException(nameof(logicalName));
            Document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public string CandidateId { get; }
        public string LayerId { get; }
        public LogicalContentPath LogicalName { get; }
        public IniRawDocument Document { get; }
    }

    internal enum IniResolutionStatus
    {
        Complete,
        Ambiguous,
        Failed
    }

    internal enum IniResolutionDiagnosticSeverity
    {
        Warning,
        Error
    }

    internal enum IniResolutionDiagnosticCode
    {
        EmptyCandidateSet,
        DocumentBudgetExceeded,
        LayerBudgetExceeded,
        ValueCandidateBudgetExceeded,
        ResolvedValueBudgetExceeded,
        DiagnosticBudgetExceeded,
        DuplicateCandidateId,
        CandidateLayerMissing,
        IncompleteProvenance,
        LogicalNameMismatch,
        MultipleLogicalNames,
        MissingLayerPriority,
        EqualLayerPriority,
        UnresolvedFileComposition,
        UnresolvedNameComparison,
        UnresolvedDuplicateSection,
        UnresolvedDuplicateKey,
        UnresolvedInlineComment,
        UnresolvedWhitespace,
        UnresolvedEmptyValue,
        NonAsciiRuntimeName,
        InvalidPhysicalNameEncoding,
        OpaqueNodeNotExecuted
    }

    internal sealed class IniResolutionDiagnostic
    {
        public IniResolutionDiagnostic(
            IniResolutionDiagnosticCode code,
            IniResolutionDiagnosticSeverity severity,
            string message,
            LogicalContentPath logicalPath = null,
            string candidateId = null,
            int? physicalLineId = null)
        {
            if (!Enum.IsDefined(typeof(IniResolutionDiagnosticCode), code))
            {
                throw new ArgumentOutOfRangeException(nameof(code));
            }

            if (!Enum.IsDefined(typeof(IniResolutionDiagnosticSeverity), severity))
            {
                throw new ArgumentOutOfRangeException(nameof(severity));
            }

            if (physicalLineId.HasValue && physicalLineId.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalLineId));
            }

            Code = code;
            Severity = severity;
            Message = BinaryDiagnosticLabel.Validate(message, nameof(message));
            LogicalPath = logicalPath;
            CandidateId = candidateId;
            PhysicalLineId = physicalLineId;
        }

        public IniResolutionDiagnosticCode Code { get; }
        public IniResolutionDiagnosticSeverity Severity { get; }
        public string Message { get; }
        public LogicalContentPath LogicalPath { get; }
        public string CandidateId { get; }
        public int? PhysicalLineId { get; }
    }

    internal enum IniValueCandidateDisposition
    {
        Winner,
        Candidate,
        SuppressedByDuplicateSection,
        SuppressedByDuplicateKey,
        SuppressedByEmptyValuePolicy,
        OverriddenByFileComposition,
        Ambiguous
    }

    internal sealed class IniResolvedValueCandidate
    {
        private readonly byte[] effectiveValueBytes;

        internal IniResolvedValueCandidate(
            IniCandidateDocument document,
            int sectionLineId,
            int keyLineId,
            IniValueCandidateDisposition disposition,
            byte[] effectiveValueBytes,
            bool isEmpty,
            bool containsInlineSemicolon)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            if (sectionLineId < 0 || keyLineId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sectionLineId));
            }

            if (!Enum.IsDefined(typeof(IniValueCandidateDisposition), disposition))
            {
                throw new ArgumentOutOfRangeException(nameof(disposition));
            }

            this.effectiveValueBytes =
                (effectiveValueBytes ?? throw new ArgumentNullException(nameof(effectiveValueBytes)))
                .ToArray();
            SectionLineId = sectionLineId;
            KeyLineId = keyLineId;
            Disposition = disposition;
            IsEmpty = isEmpty;
            ContainsInlineSemicolon = containsInlineSemicolon;
        }

        public IniCandidateDocument Document { get; }
        public int SectionLineId { get; }
        public int KeyLineId { get; }
        public IniValueCandidateDisposition Disposition { get; }
        public bool IsEmpty { get; }
        public bool ContainsInlineSemicolon { get; }

        public byte[] CopyEffectiveValueBytes()
        {
            return effectiveValueBytes.ToArray();
        }
    }

    internal sealed class IniResolvedValue
    {
        private readonly IReadOnlyList<IniResolvedValueCandidate> candidateChain;

        internal IniResolvedValue(
            string sectionName,
            string keyName,
            IniResolvedValueCandidate winner,
            IEnumerable<IniResolvedValueCandidate> candidateChain)
        {
            SectionName = sectionName ?? throw new ArgumentNullException(nameof(sectionName));
            KeyName = keyName ?? throw new ArgumentNullException(nameof(keyName));
            Winner = winner ?? throw new ArgumentNullException(nameof(winner));
            IniResolvedValueCandidate[] values =
                (candidateChain ?? throw new ArgumentNullException(nameof(candidateChain))).ToArray();
            if (values.Length == 0 || values.Any(value => value == null) ||
                values.Count(value => value.Disposition == IniValueCandidateDisposition.Winner) != 1 ||
                !values.Contains(winner))
            {
                throw new ArgumentException(
                    "A resolved INI value requires one winner within its complete candidate chain.",
                    nameof(candidateChain));
            }

            this.candidateChain = Array.AsReadOnly(values);
        }

        public string SectionName { get; }
        public string KeyName { get; }
        public IniResolvedValueCandidate Winner { get; }
        public IReadOnlyList<IniResolvedValueCandidate> CandidateChain => candidateChain;
    }

    internal sealed class IniResolvedSection
    {
        private readonly IReadOnlyList<IniResolvedValue> values;

        internal IniResolvedSection(string name, IEnumerable<IniResolvedValue> values)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            IniResolvedValue[] result =
                (values ?? throw new ArgumentNullException(nameof(values))).ToArray();
            if (result.Any(value => value == null ||
                !string.Equals(value.SectionName, name, StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    "Resolved values must belong to their resolved section.",
                    nameof(values));
            }

            this.values = Array.AsReadOnly(result);
        }

        public string Name { get; }
        public IReadOnlyList<IniResolvedValue> Values => values;
    }

    internal sealed class IniResolutionTrace
    {
        private readonly IReadOnlyList<IniCandidateDocument> documentCandidates;
        private readonly IReadOnlyList<IniResolvedValueCandidate> valueCandidates;

        internal IniResolutionTrace(
            IEnumerable<IniCandidateDocument> documentCandidates,
            IEnumerable<IniResolvedValueCandidate> valueCandidates)
        {
            IniCandidateDocument[] documents =
                (documentCandidates ?? throw new ArgumentNullException(nameof(documentCandidates)))
                .ToArray();
            IniResolvedValueCandidate[] values =
                (valueCandidates ?? throw new ArgumentNullException(nameof(valueCandidates)))
                .ToArray();
            if (documents.Any(value => value == null) || values.Any(value => value == null))
            {
                throw new ArgumentException("Resolution traces cannot contain null values.");
            }

            this.documentCandidates = Array.AsReadOnly(documents);
            this.valueCandidates = Array.AsReadOnly(values);
        }

        public IReadOnlyList<IniCandidateDocument> DocumentCandidates => documentCandidates;
        public IReadOnlyList<IniResolvedValueCandidate> ValueCandidates => valueCandidates;
    }

    internal sealed class IniResolutionResult
    {
        private readonly IReadOnlyList<IniResolvedSection> sections;
        private readonly IReadOnlyList<IniResolutionDiagnostic> diagnostics;

        private IniResolutionResult(
            IniResolutionStatus status,
            IEnumerable<IniResolvedSection> sections,
            IniResolutionTrace trace,
            IEnumerable<IniResolutionDiagnostic> diagnostics)
        {
            if (!Enum.IsDefined(typeof(IniResolutionStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            IniResolvedSection[] sectionArray =
                (sections ?? throw new ArgumentNullException(nameof(sections))).ToArray();
            IniResolutionDiagnostic[] diagnosticArray =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            if (sectionArray.Any(value => value == null) ||
                diagnosticArray.Any(value => value == null))
            {
                throw new ArgumentException("Resolution results cannot contain null values.");
            }

            bool hasError = diagnosticArray.Any(value =>
                value.Severity == IniResolutionDiagnosticSeverity.Error);
            if ((status == IniResolutionStatus.Complete && hasError) ||
                (status != IniResolutionStatus.Complete && !hasError))
            {
                throw new ArgumentException(
                    "Complete resolution results have no errors; incomplete results require errors.");
            }

            Status = status;
            this.sections = Array.AsReadOnly(sectionArray);
            Trace = trace ?? throw new ArgumentNullException(nameof(trace));
            this.diagnostics = Array.AsReadOnly(diagnosticArray);
        }

        public IniResolutionStatus Status { get; }
        public bool IsComplete => Status == IniResolutionStatus.Complete;
        public IReadOnlyList<IniResolvedSection> Sections => sections;
        public IniResolutionTrace Trace { get; }
        public IReadOnlyList<IniResolutionDiagnostic> Diagnostics => diagnostics;

        internal static IniResolutionResult Create(
            IniResolutionStatus status,
            IEnumerable<IniResolvedSection> sections,
            IniResolutionTrace trace,
            IEnumerable<IniResolutionDiagnostic> diagnostics)
        {
            return new IniResolutionResult(status, sections, trace, diagnostics);
        }
    }
}
