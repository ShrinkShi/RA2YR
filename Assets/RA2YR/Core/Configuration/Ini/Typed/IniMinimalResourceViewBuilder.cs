using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Configuration.Ini.Resolution;

namespace RA2YR.Core.Configuration.Ini.Typed
{
    internal static class IniMinimalResourceViewBuilder
    {
        private static readonly IReadOnlyDictionary<IniRulesRegistryKind, string>
            RegistryNames = new Dictionary<IniRulesRegistryKind, string>
            {
                { IniRulesRegistryKind.AircraftTypes, "AircraftTypes" },
                { IniRulesRegistryKind.BuildingTypes, "BuildingTypes" },
                { IniRulesRegistryKind.InfantryTypes, "InfantryTypes" },
                { IniRulesRegistryKind.VehicleTypes, "VehicleTypes" },
                { IniRulesRegistryKind.Animations, "Animations" }
            };

        private static readonly IReadOnlyDictionary<IniArtFieldKind, string>
            ArtFieldNames = new Dictionary<IniArtFieldKind, string>
            {
                { IniArtFieldKind.Image, "Image" },
                { IniArtFieldKind.Cameo, "Cameo" },
                { IniArtFieldKind.AltCameo, "AltCameo" },
                { IniArtFieldKind.Voxel, "Voxel" },
                { IniArtFieldKind.Remapable, "Remapable" },
                { IniArtFieldKind.NewTheater, "NewTheater" },
                { IniArtFieldKind.Palette, "Palette" },
                { IniArtFieldKind.CustomPalette, "CustomPalette" },
                { IniArtFieldKind.Buildup, "Buildup" },
                { IniArtFieldKind.ShadowIndex, "ShadowIndex" }
            };

        public static IniTypedViewResult<IniRulesResourceDocument> BuildRules(
            IniResolutionResult input,
            IniTypedNameComparisonPolicy nameComparison,
            IniTypedViewLimits limits = null)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            ValidateNameComparison(nameComparison);
            if (!input.IsComplete)
            {
                return IniTypedViewResult<IniRulesResourceDocument>
                    .RejectIncompleteInput(input);
            }

            limits = limits ?? IniTypedViewLimits.Default;
            var diagnostics = new TypedDiagnosticCollector(limits.MaxDiagnostics);
            AddOpaqueDiagnostics(input, diagnostics);
            var registries = new List<IniRulesRegistry>();
            int totalEntries = 0;

            foreach (IniRulesRegistryKind kind in Enum.GetValues(
                         typeof(IniRulesRegistryKind)))
            {
                IniResolvedSection[] sections = input.Sections.Where(section =>
                        NamesEqual(section.Name, RegistryNames[kind], nameComparison))
                    .ToArray();
                var entries = new List<IniRulesRegistryEntry>();
                foreach (IniResolvedSection section in sections)
                {
                    foreach (IniResolvedValue value in section.Values
                                 .OrderBy(item => item.Winner.SectionLineId)
                                 .ThenBy(item => item.Winner.KeyLineId))
                    {
                        int ordinal;
                        bool overflow;
                        if (!TryParseOrdinal(value.KeyName, out ordinal, out overflow))
                        {
                            diagnostics.Add(new IniTypedDiagnostic(
                                overflow
                                    ? IniTypedDiagnosticCode.IntegerOverflow
                                    : IniTypedDiagnosticCode.InvalidRegistryOrdinal,
                                IniTypedDiagnosticSeverity.Error,
                                IniTypedTargetKind.RulesRegistry,
                                overflow
                                    ? "A Rules registry ordinal exceeded the supported range."
                                    : "A Rules registry ordinal is not a non-negative decimal integer.",
                                value.Winner.Document.LogicalName,
                                value.Winner.Document.CandidateId,
                                value.Winner.KeyLineId));
                            continue;
                        }

                        if (totalEntries == limits.MaxRegistryEntries)
                        {
                            return IniTypedViewResult<IniRulesResourceDocument>.FailCompleteInput(
                                input,
                                new IniTypedDiagnostic(
                                    IniTypedDiagnosticCode.RegistryEntryBudgetExceeded,
                                    IniTypedDiagnosticSeverity.Error,
                                    IniTypedTargetKind.RulesRegistry,
                                    "The minimal Rules registry entry budget was exceeded."));
                        }

                        totalEntries++;

                        IniTypedParseResult identifier =
                            IniTypedScalarParser.ParseAsciiIdentifier(value, limits);
                        diagnostics.AddRange(identifier.Diagnostics);
                        AddValueHazards(value, diagnostics);
                        entries.Add(new IniRulesRegistryEntry(
                            kind,
                            value.KeyName,
                            ordinal,
                            identifier));
                    }
                }

                AddDuplicateIdentifierDiagnostics(entries, nameComparison, diagnostics);
                registries.Add(new IniRulesRegistry(kind, entries));
            }

            if (diagnostics.Exceeded)
            {
                return IniTypedViewResult<IniRulesResourceDocument>.FailCompleteInput(
                    input,
                    DiagnosticBudgetFailure(IniTypedTargetKind.RulesRegistry));
            }

            return IniTypedViewResult<IniRulesResourceDocument>.Create(
                new IniRulesResourceDocument(registries),
                input,
                diagnostics.Values);
        }

        public static IniTypedViewResult<IniArtResourceDocument> BuildArt(
            IniResolutionResult input,
            IniTypedNameComparisonPolicy nameComparison,
            IniBooleanCasePolicy booleanCase,
            IniTypedViewLimits limits = null)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            ValidateNameComparison(nameComparison);
            if (!Enum.IsDefined(typeof(IniBooleanCasePolicy), booleanCase))
            {
                throw new ArgumentOutOfRangeException(nameof(booleanCase));
            }

            if (!input.IsComplete)
            {
                return IniTypedViewResult<IniArtResourceDocument>.RejectIncompleteInput(input);
            }

            limits = limits ?? IniTypedViewLimits.Default;
            var diagnostics = new TypedDiagnosticCollector(limits.MaxDiagnostics);
            AddOpaqueDiagnostics(input, diagnostics);
            var records = new List<IniArtResourceRecord>();

            foreach (IniResolvedSection section in input.Sections)
            {
                bool containsTargetField = section.Values.Any(value =>
                    ArtFieldNames.Values.Any(name =>
                        NamesEqual(value.KeyName, name, nameComparison)));
                if (!containsTargetField)
                {
                    continue;
                }

                if (records.Count == limits.MaxArtRecords)
                {
                    return IniTypedViewResult<IniArtResourceDocument>.FailCompleteInput(
                        input,
                        new IniTypedDiagnostic(
                            IniTypedDiagnosticCode.ArtRecordBudgetExceeded,
                            IniTypedDiagnosticSeverity.Error,
                            IniTypedTargetKind.ArtSection,
                            "The minimal Art resource record budget was exceeded."));
                }

                var fields = new List<IniArtResourceField>();
                foreach (IniArtFieldKind kind in Enum.GetValues(typeof(IniArtFieldKind)))
                {
                    IniResolvedValue[] matches = section.Values.Where(value =>
                            NamesEqual(value.KeyName, ArtFieldNames[kind], nameComparison))
                        .ToArray();
                    if (matches.Length == 0)
                    {
                        fields.Add(IniArtResourceField.Missing(kind));
                        continue;
                    }

                    if (matches.Length > 1)
                    {
                        diagnostics.Add(new IniTypedDiagnostic(
                            IniTypedDiagnosticCode.ArtSectionAmbiguous,
                            IniTypedDiagnosticSeverity.Error,
                            IniTypedTargetKind.ArtField,
                            "More than one resolved Art field matched the explicit name policy.",
                            matches[0].Winner.Document.LogicalName,
                            matches[0].Winner.Document.CandidateId,
                            matches[0].Winner.KeyLineId));
                    }

                    IniResolvedValue selected = matches[0];
                    IniTypedParseResult parsed = ParseArtField(
                        kind,
                        selected,
                        booleanCase,
                        limits);
                    diagnostics.AddRange(parsed.Diagnostics);
                    AddValueHazards(selected, diagnostics);
                    fields.Add(IniArtResourceField.FromParse(kind, parsed));
                }

                records.Add(new IniArtResourceRecord(section.Name, fields));
            }

            if (diagnostics.Exceeded)
            {
                return IniTypedViewResult<IniArtResourceDocument>.FailCompleteInput(
                    input,
                    DiagnosticBudgetFailure(IniTypedTargetKind.ArtSection));
            }

            return IniTypedViewResult<IniArtResourceDocument>.Create(
                new IniArtResourceDocument(records),
                input,
                diagnostics.Values);
        }

        private static IniTypedParseResult ParseArtField(
            IniArtFieldKind kind,
            IniResolvedValue value,
            IniBooleanCasePolicy booleanCase,
            IniTypedViewLimits limits)
        {
            switch (kind)
            {
                case IniArtFieldKind.Image:
                case IniArtFieldKind.Cameo:
                case IniArtFieldKind.AltCameo:
                case IniArtFieldKind.Palette:
                case IniArtFieldKind.Buildup:
                    return IniTypedScalarParser.ParseAsciiIdentifier(value, limits);
                case IniArtFieldKind.Voxel:
                case IniArtFieldKind.Remapable:
                case IniArtFieldKind.NewTheater:
                    return IniTypedScalarParser.ParseBoolean(value, booleanCase, limits);
                case IniArtFieldKind.ShadowIndex:
                    return IniTypedScalarParser.ParseNonNegativeInteger(value, limits);
                case IniArtFieldKind.CustomPalette:
                    return IniTypedScalarParser.ParseRaw(value, limits);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static void AddOpaqueDiagnostics(
            IniResolutionResult input,
            TypedDiagnosticCollector diagnostics)
        {
            foreach (IniResolutionDiagnostic diagnostic in input.Diagnostics.Where(value =>
                         value.Code == IniResolutionDiagnosticCode.OpaqueNodeNotExecuted))
            {
                diagnostics.Add(new IniTypedDiagnostic(
                    IniTypedDiagnosticCode.OpaqueMayAffectTarget,
                    IniTypedDiagnosticSeverity.Error,
                    IniTypedTargetKind.InputResolution,
                    "A preserved Opaque INI line may affect a minimal typed view.",
                    diagnostic.LogicalPath,
                    diagnostic.CandidateId,
                    diagnostic.PhysicalLineId));
            }
        }

        private static void AddValueHazards(
            IniResolvedValue value,
            TypedDiagnosticCollector diagnostics)
        {
            foreach (IniResolvedValueCandidate candidate in value.CandidateChain)
            {
                if (candidate.ContainsInlineSemicolon)
                {
                    diagnostics.Add(new IniTypedDiagnostic(
                        IniTypedDiagnosticCode.InlineSemicolonMayAffectTarget,
                        IniTypedDiagnosticSeverity.Error,
                        IniTypedTargetKind.Scalar,
                        "The selected field contains a semicolon whose stock runtime semantics remain unresolved.",
                        candidate.Document.LogicalName,
                        candidate.Document.CandidateId,
                        candidate.KeyLineId));
                }

                if (candidate.Disposition ==
                    IniValueCandidateDisposition.SuppressedByDuplicateSection)
                {
                    diagnostics.Add(new IniTypedDiagnostic(
                        IniTypedDiagnosticCode.DuplicateSectionMayAffectTarget,
                        IniTypedDiagnosticSeverity.Error,
                        IniTypedTargetKind.Scalar,
                        "A duplicate section policy affected the selected field.",
                        candidate.Document.LogicalName,
                        candidate.Document.CandidateId,
                        candidate.SectionLineId));
                }

                if (candidate.Disposition == IniValueCandidateDisposition.SuppressedByDuplicateKey)
                {
                    diagnostics.Add(new IniTypedDiagnostic(
                        IniTypedDiagnosticCode.DuplicateKeyMayAffectTarget,
                        IniTypedDiagnosticSeverity.Error,
                        IniTypedTargetKind.Scalar,
                        "A duplicate key policy affected the selected field.",
                        candidate.Document.LogicalName,
                        candidate.Document.CandidateId,
                        candidate.KeyLineId));
                }
            }
        }

        private static void AddDuplicateIdentifierDiagnostics(
            IEnumerable<IniRulesRegistryEntry> entries,
            IniTypedNameComparisonPolicy comparison,
            TypedDiagnosticCollector diagnostics)
        {
            StringComparer comparer = comparison == IniTypedNameComparisonPolicy.Ordinal
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase;
            foreach (IGrouping<string, IniRulesRegistryEntry> group in entries
                         .Where(entry => entry.Identifier.Status == IniTypedValueStatus.Present)
                         .GroupBy(entry => entry.Identifier.Value.Identifier, comparer)
                         .Where(group => group.Count() > 1))
            {
                foreach (IniRulesRegistryEntry entry in group)
                {
                    IniValueSourceCandidateTrace winner = entry.Identifier.Value.SourceTrace.Winner;
                    diagnostics.Add(new IniTypedDiagnostic(
                        IniTypedDiagnosticCode.DuplicateRegistryIdentifier,
                        IniTypedDiagnosticSeverity.Error,
                        IniTypedTargetKind.RulesRegistry,
                        "A Rules registry contains a repeated explicit object identifier.",
                        winner.LogicalName,
                        winner.CandidateId,
                        winner.KeyPhysicalLineId));
                }
            }
        }

        private static bool TryParseOrdinal(
            string value,
            out int result,
            out bool overflow)
        {
            result = 0;
            overflow = false;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            foreach (char character in value)
            {
                int digit = character - '0';
                if (digit < 0 || digit > 9)
                {
                    return false;
                }

                try
                {
                    result = checked((result * 10) + digit);
                }
                catch (OverflowException)
                {
                    overflow = true;
                    return false;
                }
            }

            return true;
        }

        private static bool NamesEqual(
            string left,
            string right,
            IniTypedNameComparisonPolicy policy)
        {
            if (policy == IniTypedNameComparisonPolicy.Ordinal)
            {
                return string.Equals(left, right, StringComparison.Ordinal);
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int index = 0; index < left.Length; index++)
            {
                char leftCharacter = FoldAscii(left[index]);
                char rightCharacter = FoldAscii(right[index]);
                if (leftCharacter != rightCharacter)
                {
                    return false;
                }
            }

            return true;
        }

        private static char FoldAscii(char value)
        {
            return value >= 'A' && value <= 'Z'
                ? (char)(value + ('a' - 'A'))
                : value;
        }

        private static void ValidateNameComparison(IniTypedNameComparisonPolicy policy)
        {
            if (!Enum.IsDefined(typeof(IniTypedNameComparisonPolicy), policy))
            {
                throw new ArgumentOutOfRangeException(nameof(policy));
            }
        }

        private static IniTypedDiagnostic DiagnosticBudgetFailure(IniTypedTargetKind target)
        {
            return new IniTypedDiagnostic(
                IniTypedDiagnosticCode.DiagnosticBudgetExceeded,
                IniTypedDiagnosticSeverity.Error,
                target,
                "The minimal INI typed-view diagnostic budget was exceeded.");
        }

        private sealed class TypedDiagnosticCollector
        {
            private readonly int maximum;
            private readonly List<IniTypedDiagnostic> values =
                new List<IniTypedDiagnostic>();

            public TypedDiagnosticCollector(int maximum)
            {
                maximum = maximum > 0
                    ? maximum
                    : throw new ArgumentOutOfRangeException(nameof(maximum));
                this.maximum = maximum;
            }

            public IReadOnlyList<IniTypedDiagnostic> Values => values;
            public bool Exceeded { get; private set; }

            public void Add(IniTypedDiagnostic diagnostic)
            {
                if (Exceeded)
                {
                    return;
                }

                if (values.Count == maximum)
                {
                    Exceeded = true;
                    return;
                }

                values.Add(diagnostic ?? throw new ArgumentNullException(nameof(diagnostic)));
            }

            public void AddRange(IEnumerable<IniTypedDiagnostic> diagnostics)
            {
                foreach (IniTypedDiagnostic diagnostic in diagnostics)
                {
                    Add(diagnostic);
                }
            }
        }
    }
}
