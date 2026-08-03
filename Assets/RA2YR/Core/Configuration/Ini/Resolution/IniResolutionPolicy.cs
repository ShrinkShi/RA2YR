using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;

namespace RA2YR.Core.Configuration.Ini.Resolution
{
    internal enum IniResolutionEvidenceLevel
    {
        ConfirmedByOriginalRuntime,
        ConfirmedByProjectBaselineRuntime,
        ConfirmedByOfficialEditorSource,
        CrossCheckedByIndependentImplementation,
        CommunityDocumented,
        ConfiguredForTesting,
        Unresolved
    }

    internal sealed class IniResolutionEvidence
    {
        public IniResolutionEvidence(
            IniResolutionEvidenceLevel level,
            string referenceId)
        {
            if (!Enum.IsDefined(typeof(IniResolutionEvidenceLevel), level))
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            Level = level;
            ReferenceId = BinaryDiagnosticLabel.Validate(referenceId, nameof(referenceId));
        }

        public IniResolutionEvidenceLevel Level { get; }

        public string ReferenceId { get; }

        public bool ConfirmsRuntime =>
            Level == IniResolutionEvidenceLevel.ConfirmedByOriginalRuntime ||
            Level == IniResolutionEvidenceLevel.ConfirmedByProjectBaselineRuntime;
    }

    internal enum IniLoadLayerKind
    {
        LooseDirectory,
        ExpandMix,
        EcacheMix,
        ElocalMix,
        BaseMix,
        NestedMix,
        TestSource,
        Other
    }

    internal sealed class IniLoadLayer
    {
        private readonly IReadOnlyList<LogicalContentPath> logicalChain;

        public IniLoadLayer(
            string layerId,
            string sourceId,
            IniLoadLayerKind kind,
            IEnumerable<LogicalContentPath> logicalChain,
            int? priority,
            IniResolutionEvidence priorityEvidence)
        {
            LayerId = BinaryDiagnosticLabel.Validate(layerId, nameof(layerId));
            if (!ContentConfigurationValueRules.IsValidSourceId(sourceId))
            {
                throw new ArgumentException("A valid source id is required.", nameof(sourceId));
            }

            if (!Enum.IsDefined(typeof(IniLoadLayerKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            LogicalContentPath[] chain =
                (logicalChain ?? throw new ArgumentNullException(nameof(logicalChain))).ToArray();
            if (chain.Length == 0 || chain.Any(path => path == null))
            {
                throw new ArgumentException(
                    "An INI load layer requires a complete logical provenance chain.",
                    nameof(logicalChain));
            }

            SourceId = sourceId;
            Kind = kind;
            this.logicalChain = Array.AsReadOnly(chain);
            Priority = priority;
            PriorityEvidence = priorityEvidence ??
                throw new ArgumentNullException(nameof(priorityEvidence));
        }

        public string LayerId { get; }

        public string SourceId { get; }

        public IniLoadLayerKind Kind { get; }

        public IReadOnlyList<LogicalContentPath> LogicalChain => logicalChain;

        public int? Priority { get; }

        public IniResolutionEvidence PriorityEvidence { get; }
    }

    internal sealed class IniLoadPlan
    {
        private readonly IReadOnlyList<IniLoadLayer> layers;

        public IniLoadPlan(string planId, IReadOnlyList<IniLoadLayer> layers)
        {
            PlanId = BinaryDiagnosticLabel.Validate(planId, nameof(planId));
            if (layers == null)
            {
                throw new ArgumentNullException(nameof(layers));
            }

            if (layers.Count == 0)
            {
                throw new ArgumentException(
                    "An INI load plan requires at least one non-null layer.",
                    nameof(layers));
            }

            var values = new IniLoadLayer[layers.Count];
            for (int index = 0; index < values.Length; index++)
            {
                values[index] = layers[index] ?? throw new ArgumentException(
                    "An INI load plan requires at least one non-null layer.",
                    nameof(layers));
            }

            if (values.Select(layer => layer.LayerId)
                .Distinct(StringComparer.Ordinal).Count() != values.Length)
            {
                throw new ArgumentException(
                    "INI load layer ids must be unique.",
                    nameof(layers));
            }

            this.layers = Array.AsReadOnly(values);
        }

        public string PlanId { get; }

        public IReadOnlyList<IniLoadLayer> Layers => layers;
    }

    internal enum IniFileCompositionPolicy
    {
        SelectHighestPriorityDocument,
        OverlayDocumentsLowToHigh,
        Unresolved
    }

    internal enum IniNameComparisonPolicy
    {
        OrdinalRawAscii,
        OrdinalIgnoreCaseAscii,
        Unresolved
    }

    internal enum IniDuplicateSectionPolicy
    {
        FirstSectionWins,
        LastSectionWins,
        MergeSectionsInFileOrder,
        Unresolved
    }

    internal enum IniDuplicateKeyPolicy
    {
        FirstKeyWins,
        LastKeyWins,
        Unresolved
    }

    internal enum IniInlineCommentPolicy
    {
        PreserveSemicolonInValue,
        SemicolonStartsComment,
        Unresolved
    }

    internal enum IniWhitespaceReadPolicy
    {
        Preserve,
        TrimAsciiSpaceAndTab,
        Unresolved
    }

    internal enum IniEmptyValuePolicy
    {
        OverridesEarlierValue,
        DoesNotOverrideEarlierValue,
        Unresolved
    }

    internal sealed class IniResolutionPolicy
    {
        public IniResolutionPolicy(
            IniFileCompositionPolicy fileComposition,
            IniResolutionEvidence fileCompositionEvidence,
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
            ValidateEnum(fileComposition, nameof(fileComposition));
            ValidateEnum(nameComparison, nameof(nameComparison));
            ValidateEnum(duplicateSections, nameof(duplicateSections));
            ValidateEnum(duplicateKeys, nameof(duplicateKeys));
            ValidateEnum(inlineComments, nameof(inlineComments));
            ValidateEnum(whitespace, nameof(whitespace));
            ValidateEnum(emptyValues, nameof(emptyValues));

            FileComposition = fileComposition;
            FileCompositionEvidence = fileCompositionEvidence ??
                throw new ArgumentNullException(nameof(fileCompositionEvidence));
            NameComparison = nameComparison;
            NameComparisonEvidence = nameComparisonEvidence ??
                throw new ArgumentNullException(nameof(nameComparisonEvidence));
            DuplicateSections = duplicateSections;
            DuplicateSectionEvidence = duplicateSectionEvidence ??
                throw new ArgumentNullException(nameof(duplicateSectionEvidence));
            DuplicateKeys = duplicateKeys;
            DuplicateKeyEvidence = duplicateKeyEvidence ??
                throw new ArgumentNullException(nameof(duplicateKeyEvidence));
            InlineComments = inlineComments;
            InlineCommentEvidence = inlineCommentEvidence ??
                throw new ArgumentNullException(nameof(inlineCommentEvidence));
            Whitespace = whitespace;
            WhitespaceEvidence = whitespaceEvidence ??
                throw new ArgumentNullException(nameof(whitespaceEvidence));
            EmptyValues = emptyValues;
            EmptyValueEvidence = emptyValueEvidence ??
                throw new ArgumentNullException(nameof(emptyValueEvidence));
        }

        public IniFileCompositionPolicy FileComposition { get; }
        public IniResolutionEvidence FileCompositionEvidence { get; }
        public IniNameComparisonPolicy NameComparison { get; }
        public IniResolutionEvidence NameComparisonEvidence { get; }
        public IniDuplicateSectionPolicy DuplicateSections { get; }
        public IniResolutionEvidence DuplicateSectionEvidence { get; }
        public IniDuplicateKeyPolicy DuplicateKeys { get; }
        public IniResolutionEvidence DuplicateKeyEvidence { get; }
        public IniInlineCommentPolicy InlineComments { get; }
        public IniResolutionEvidence InlineCommentEvidence { get; }
        public IniWhitespaceReadPolicy Whitespace { get; }
        public IniResolutionEvidence WhitespaceEvidence { get; }
        public IniEmptyValuePolicy EmptyValues { get; }
        public IniResolutionEvidence EmptyValueEvidence { get; }

        private static void ValidateEnum<T>(T value, string parameterName)
            where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    internal sealed class IniResolutionLimits
    {
        public IniResolutionLimits(
            int maxDocuments,
            int maxLayers,
            int maxValueCandidates,
            int maxResolvedValues,
            int maxDiagnostics)
        {
            if (maxDocuments <= 0 || maxLayers <= 0 || maxValueCandidates <= 0 ||
                maxResolvedValues <= 0 || maxDiagnostics <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDocuments));
            }

            MaxDocuments = maxDocuments;
            MaxLayers = maxLayers;
            MaxValueCandidates = maxValueCandidates;
            MaxResolvedValues = maxResolvedValues;
            MaxDiagnostics = maxDiagnostics;
        }

        public static IniResolutionLimits Default { get; } =
            new IniResolutionLimits(64, 128, 2_000_000, 1_000_000, 100_000);

        public int MaxDocuments { get; }
        public int MaxLayers { get; }
        public int MaxValueCandidates { get; }
        public int MaxResolvedValues { get; }
        public int MaxDiagnostics { get; }
    }
}
