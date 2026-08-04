using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Configuration.Ini.Resolution
{
    internal sealed class IniProjectBaselineDocumentInput
    {
        public IniProjectBaselineDocumentInput(
            string candidateId,
            LogicalContentPath logicalName,
            IniRawDocument document)
        {
            CandidateId = BinaryDiagnosticLabel.Validate(candidateId, nameof(candidateId));
            LogicalName = logicalName ?? throw new ArgumentNullException(nameof(logicalName));
            Document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public string CandidateId { get; }
        public LogicalContentPath LogicalName { get; }
        public IniRawDocument Document { get; }
    }

    internal sealed class IniProjectBaselineLoadPlanBuildResult
    {
        private readonly IReadOnlyList<IniCandidateDocument> candidates;
        private readonly IReadOnlyList<IniResolutionDiagnostic> diagnostics;

        private IniProjectBaselineLoadPlanBuildResult(
            IniLoadPlan plan,
            IEnumerable<IniCandidateDocument> candidates,
            IEnumerable<IniResolutionDiagnostic> diagnostics)
        {
            Plan = plan;
            this.candidates = Array.AsReadOnly(
                (candidates ?? throw new ArgumentNullException(nameof(candidates))).ToArray());
            this.diagnostics = Array.AsReadOnly(
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
        }

        public bool IsComplete => Plan != null && diagnostics.Count == 0;
        public IniLoadPlan Plan { get; }
        public IReadOnlyList<IniCandidateDocument> Candidates => candidates;
        public IReadOnlyList<IniResolutionDiagnostic> Diagnostics => diagnostics;

        internal static IniProjectBaselineLoadPlanBuildResult Complete(
            IniLoadPlan plan,
            IEnumerable<IniCandidateDocument> candidates)
        {
            return new IniProjectBaselineLoadPlanBuildResult(
                plan ?? throw new ArgumentNullException(nameof(plan)),
                candidates,
                Array.Empty<IniResolutionDiagnostic>());
        }

        internal static IniProjectBaselineLoadPlanBuildResult Failed(
            IEnumerable<IniResolutionDiagnostic> diagnostics)
        {
            IniResolutionDiagnostic[] values =
                (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            if (values.Length == 0 || values.Any(value => value == null ||
                value.Severity != IniResolutionDiagnosticSeverity.Error))
            {
                throw new ArgumentException(
                    "A failed ProjectBaseline load-plan build requires error diagnostics.",
                    nameof(diagnostics));
            }

            return new IniProjectBaselineLoadPlanBuildResult(
                null,
                Array.Empty<IniCandidateDocument>(),
                values);
        }
    }

    internal sealed class IniProjectBaselineLayerDescriptor
    {
        internal IniProjectBaselineLayerDescriptor(
            string layerId,
            IniLoadLayerKind kind,
            int priority,
            int? expandNumber)
        {
            LayerId = layerId;
            Kind = kind;
            Priority = priority;
            ExpandNumber = expandNumber;
        }

        public string LayerId { get; }
        public IniLoadLayerKind Kind { get; }
        public int Priority { get; }
        public int? ExpandNumber { get; }
    }

    internal static class IniProjectBaselineLoadPlanBuilder
    {
        private const int Ra2Priority = 100;
        private const int Ra2MdPriority = 200;
        private const int ExpandPriorityBase = 300;
        private const int LoosePriority = 1000;
        private const int MinimumExpandNumber = 1;
        private const int MaximumExpandNumber = 99;
        private const int ExpandArchiveNameLength = 14;
        private const int ExpandDigitOffset = 8;

        public static IniProjectBaselineLoadPlanBuildResult Build(
            string planId,
            string expectedSourceId,
            IReadOnlyList<IniProjectBaselineDocumentInput> inputs,
            int maxLayers = 102)
        {
            planId = BinaryDiagnosticLabel.Validate(planId, nameof(planId));
            if (!ContentConfigurationValueRules.IsValidSourceId(expectedSourceId))
            {
                throw new ArgumentException("A valid expected source id is required.",
                    nameof(expectedSourceId));
            }

            if (inputs == null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            if (maxLayers <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLayers));
            }

            var diagnostics = new List<IniResolutionDiagnostic>();
            if (inputs.Count == 0)
            {
                diagnostics.Add(Error(
                    IniResolutionDiagnosticCode.EmptyCandidateSet,
                    "ProjectBaseline INI composition requires at least one document."));
                return IniProjectBaselineLoadPlanBuildResult.Failed(diagnostics);
            }

            if (inputs.Count > maxLayers)
            {
                diagnostics.Add(Error(
                    IniResolutionDiagnosticCode.LayerBudgetExceeded,
                    "The ProjectBaseline INI composition layer budget was exceeded."));
                return IniProjectBaselineLoadPlanBuildResult.Failed(diagnostics);
            }

            var values = new IniProjectBaselineDocumentInput[inputs.Count];
            for (int index = 0; index < values.Length; index++)
            {
                values[index] = inputs[index] ?? throw new ArgumentException(
                    "ProjectBaseline composition inputs cannot contain null.",
                    nameof(inputs));
            }

            if (values.Select(value => value.CandidateId)
                .Distinct(StringComparer.Ordinal).Count() != values.Length)
            {
                diagnostics.Add(Error(
                    IniResolutionDiagnosticCode.DuplicateCandidateId,
                    "ProjectBaseline INI composition candidate ids must be unique."));
            }

            LogicalContentPath logicalName = values[0].LogicalName;
            if (values.Any(value => !value.LogicalName.Equals(logicalName)))
            {
                diagnostics.Add(Error(
                    IniResolutionDiagnosticCode.MultipleLogicalNames,
                    "One ProjectBaseline composition plan may contain only one logical INI name."));
            }

            var planned = new List<PlannedInput>(values.Length);
            var layerIds = new HashSet<string>(StringComparer.Ordinal);
            var expandNumbers = new HashSet<int>();
            foreach (IniProjectBaselineDocumentInput input in values)
            {
                if (!string.Equals(
                    input.Document.Provenance.SourceId,
                    expectedSourceId,
                    StringComparison.Ordinal))
                {
                    diagnostics.Add(Error(
                        IniResolutionDiagnosticCode.ProjectBaselineSourceRejected,
                        "An INI document is outside the configured ProjectBaseline source.",
                        input.LogicalName,
                        input.CandidateId));
                    continue;
                }

                if (!input.Document.Source.LogicalPath.Equals(input.LogicalName) ||
                    input.Document.Provenance.LogicalChain.Count == 0 ||
                    !input.Document.Provenance.LogicalChain[
                        input.Document.Provenance.LogicalChain.Count - 1].Equals(input.LogicalName))
                {
                    diagnostics.Add(Error(
                        IniResolutionDiagnosticCode.IncompleteProvenance,
                        "A ProjectBaseline INI document has incomplete logical provenance.",
                        input.LogicalName,
                        input.CandidateId));
                    continue;
                }

                IniProjectBaselineLayerDescriptor classification;
                IniResolutionDiagnosticCode failureCode;
                if (!TryClassifyLayer(
                    input.Document.Provenance.LogicalChain,
                    input.LogicalName,
                    out classification,
                    out failureCode))
                {
                    diagnostics.Add(Error(
                        failureCode,
                        failureCode == IniResolutionDiagnosticCode.InvalidExpandArchiveNumber
                            ? "An expandmd archive name does not contain an exact number from 01 through 99."
                            : "An INI document uses an archive outside the configured ProjectBaseline layer policy.",
                        input.LogicalName,
                        input.CandidateId));
                    continue;
                }

                if (!layerIds.Add(classification.LayerId))
                {
                    IniResolutionDiagnosticCode code = classification.ExpandNumber.HasValue
                        ? IniResolutionDiagnosticCode.DuplicateExpandArchiveNumber
                        : IniResolutionDiagnosticCode.DuplicateProjectBaselineLayer;
                    diagnostics.Add(Error(
                        code,
                        classification.ExpandNumber.HasValue
                            ? "More than one ProjectBaseline layer uses the same expandmd number."
                            : "More than one ProjectBaseline document occupies the same configured layer.",
                        input.LogicalName,
                        input.CandidateId));
                    continue;
                }

                if (classification.ExpandNumber.HasValue &&
                    !expandNumbers.Add(classification.ExpandNumber.Value))
                {
                    diagnostics.Add(Error(
                        IniResolutionDiagnosticCode.DuplicateExpandArchiveNumber,
                        "More than one ProjectBaseline layer uses the same expandmd number.",
                        input.LogicalName,
                        input.CandidateId));
                    continue;
                }

                planned.Add(new PlannedInput(input, classification));
            }

            if (diagnostics.Count != 0)
            {
                return IniProjectBaselineLoadPlanBuildResult.Failed(diagnostics);
            }

            IniResolutionEvidence evidence = CreateProjectBaselineEvidence();
            PlannedInput[] ordered = planned
                .OrderBy(value => value.Classification.Priority)
                .ThenBy(value => value.Input.CandidateId, StringComparer.Ordinal)
                .ToArray();
            var layers = new IniLoadLayer[ordered.Length];
            var candidates = new IniCandidateDocument[ordered.Length];
            for (int index = 0; index < ordered.Length; index++)
            {
                PlannedInput item = ordered[index];
                layers[index] = new IniLoadLayer(
                    item.Classification.LayerId,
                    expectedSourceId,
                    item.Classification.Kind,
                    item.Input.Document.Provenance.LogicalChain,
                    item.Classification.Priority,
                    evidence);
                candidates[index] = new IniCandidateDocument(
                    item.Input.CandidateId,
                    item.Classification.LayerId,
                    item.Input.LogicalName,
                    item.Input.Document);
            }

            return IniProjectBaselineLoadPlanBuildResult.Complete(
                new IniLoadPlan(planId, layers),
                candidates);
        }

        public static IniResolutionPolicy CreateResolutionPolicy(
            IniNameComparisonPolicy nameComparison,
            IniResolutionEvidence nameComparisonEvidence,
            IniDuplicateSectionPolicy duplicateSections,
            IniResolutionEvidence duplicateSectionEvidence,
            IniDuplicateKeyPolicy duplicateKeys,
            IniResolutionEvidence duplicateKeyEvidence,
            IniInlineCommentPolicy inlineComments,
            IniResolutionEvidence inlineCommentEvidence,
            IniWhitespaceReadPolicy whitespace,
            IniResolutionEvidence whitespaceEvidence,
            IniEmptyValuePolicy emptyValues,
            IniResolutionEvidence emptyValueEvidence)
        {
            return new IniResolutionPolicy(
                IniFileCompositionPolicy.OverlayDocumentsLowToHigh,
                CreateProjectBaselineEvidence(),
                nameComparison,
                nameComparisonEvidence,
                duplicateSections,
                duplicateSectionEvidence,
                duplicateKeys,
                duplicateKeyEvidence,
                inlineComments,
                inlineCommentEvidence,
                whitespace,
                whitespaceEvidence,
                emptyValues,
                emptyValueEvidence);
        }

        public static IniResolutionEvidence CreateProjectBaselineEvidence()
        {
            return new IniResolutionEvidence(
                IniResolutionEvidenceLevel.ConfiguredForProjectBaseline,
                "project-baseline-ordered-ini-semantic-composition");
        }

        internal static bool TryClassifyLayer(
            IReadOnlyList<LogicalContentPath> chain,
            LogicalContentPath logicalName,
            out IniProjectBaselineLayerDescriptor classification,
            out IniResolutionDiagnosticCode failureCode)
        {
            if (chain.Count == 1 && chain[0].Equals(logicalName))
            {
                classification = new IniProjectBaselineLayerDescriptor(
                    "projectbaseline-loose",
                    IniLoadLayerKind.LooseDirectory,
                    LoosePriority,
                    null);
                failureCode = default(IniResolutionDiagnosticCode);
                return true;
            }

            string root = chain[0].Value;
            if (string.Equals(root, "ra2.mix", StringComparison.OrdinalIgnoreCase))
            {
                classification = new IniProjectBaselineLayerDescriptor(
                    "projectbaseline-ra2",
                    chain.Count > 2 ? IniLoadLayerKind.NestedMix : IniLoadLayerKind.BaseMix,
                    Ra2Priority,
                    null);
                failureCode = default(IniResolutionDiagnosticCode);
                return true;
            }

            if (string.Equals(root, "ra2md.mix", StringComparison.OrdinalIgnoreCase))
            {
                classification = new IniProjectBaselineLayerDescriptor(
                    "projectbaseline-ra2md",
                    chain.Count > 2 ? IniLoadLayerKind.NestedMix : IniLoadLayerKind.BaseMix,
                    Ra2MdPriority,
                    null);
                failureCode = default(IniResolutionDiagnosticCode);
                return true;
            }

            if (root.StartsWith("expandmd", StringComparison.OrdinalIgnoreCase))
            {
                int number;
                if (!TryParseExpandNumber(root, out number))
                {
                    classification = null;
                    failureCode = IniResolutionDiagnosticCode.InvalidExpandArchiveNumber;
                    return false;
                }

                classification = new IniProjectBaselineLayerDescriptor(
                    "projectbaseline-expandmd" + number.ToString("00"),
                    IniLoadLayerKind.ExpandMix,
                    checked(ExpandPriorityBase + number),
                    number);
                failureCode = default(IniResolutionDiagnosticCode);
                return true;
            }

            classification = null;
            failureCode = IniResolutionDiagnosticCode.UnsupportedProjectBaselineLayer;
            return false;
        }

        private static bool TryParseExpandNumber(string archiveName, out int number)
        {
            number = 0;
            if (archiveName == null || archiveName.Length != ExpandArchiveNameLength ||
                !archiveName.EndsWith(".mix", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            char tens = archiveName[ExpandDigitOffset];
            char ones = archiveName[ExpandDigitOffset + 1];
            if (tens < '0' || tens > '9' || ones < '0' || ones > '9')
            {
                return false;
            }

            number = ((tens - '0') * 10) + (ones - '0');
            return number >= MinimumExpandNumber && number <= MaximumExpandNumber;
        }

        private static IniResolutionDiagnostic Error(
            IniResolutionDiagnosticCode code,
            string message,
            LogicalContentPath logicalPath = null,
            string candidateId = null)
        {
            return new IniResolutionDiagnostic(
                code,
                IniResolutionDiagnosticSeverity.Error,
                message,
                logicalPath,
                candidateId);
        }

        private sealed class PlannedInput
        {
            public PlannedInput(
                IniProjectBaselineDocumentInput input,
                IniProjectBaselineLayerDescriptor classification)
            {
                Input = input;
                Classification = classification;
            }

            public IniProjectBaselineDocumentInput Input { get; }
            public IniProjectBaselineLayerDescriptor Classification { get; }
        }
    }
}
