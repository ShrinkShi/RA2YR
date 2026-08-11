using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal sealed class PreviewMetadataReader
    {
        public PreviewMetadataReadResult Read(
            IEnumerable<PreviewMetadataSectionOccurrence> source,
            PreviewMetadataReadPolicy policy)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var diagnostics = new List<PreviewDiagnostic>();
            var execution = new PreviewExecutionState();
            execution.MarkExecuted();
            if (policy.Profile != PreviewMetadataInterpretationProfile.Fields23Dimensions)
            {
                Add(diagnostics, policy.Limits, execution, Diagnostic(
                    BinaryDiagnosticSeverity.Error,
                    PreviewDiagnosticCode.UnknownMetadataProfile,
                    SyntheticSource(),
                    new[] { SyntheticProvenance() },
                    -1,
                    -1,
                    "metadata",
                    "The Preview metadata interpretation profile is unknown."));
                return new PreviewMetadataReadResult(null, null, Array.Empty<PreviewSizeRaw>(), diagnostics, execution);
            }

            PreviewMetadataSectionOccurrence[] sections;
            if (!TrySnapshotSections(source, policy, diagnostics, execution, out sections))
                return new PreviewMetadataReadResult(null, null, Array.Empty<PreviewSizeRaw>(), diagnostics, execution);

            PreviewMetadataSectionOccurrence selected = SelectSection(sections, policy, diagnostics, execution);
            if (selected == null)
                return new PreviewMetadataReadResult(null, null, Array.Empty<PreviewSizeRaw>(), diagnostics, execution);

            PreviewSizeOccurrence[] sizeOccurrences;
            if (!TrySnapshotSizes(selected, policy, diagnostics, execution, out sizeOccurrences))
                return new PreviewMetadataReadResult(selected, null, Array.Empty<PreviewSizeRaw>(), diagnostics, execution);
            if (sizeOccurrences.Length == 0)
            {
                Add(diagnostics, policy.Limits, execution, Diagnostic(
                    BinaryDiagnosticSeverity.Error,
                    PreviewDiagnosticCode.MissingSize,
                    selected.Source,
                    new[] { selected.Provenance },
                    -1,
                    selected.SectionOccurrenceOrdinal,
                    "metadata",
                    "The selected Preview section contains no Size occurrence."));
                return new PreviewMetadataReadResult(selected, null, Array.Empty<PreviewSizeRaw>(), diagnostics, execution);
            }

            var parsed = new List<PreviewSizeRaw>(sizeOccurrences.Length);
            foreach (PreviewSizeOccurrence occurrence in sizeOccurrences)
            {
                PreviewSizeRaw raw = ParseSize(occurrence, selected.Source, selected.SectionOccurrenceOrdinal, diagnostics, execution, policy.Limits);
                if (raw != null)
                    parsed.Add(raw);
            }

            if (sizeOccurrences.Length > 1)
            {
                Add(diagnostics, policy.Limits, execution, Diagnostic(
                    BinaryDiagnosticSeverity.Error,
                    PreviewDiagnosticCode.DuplicateSizeOccurrence,
                    selected.Source,
                    sizeOccurrences.Select(item => item.Provenance),
                    sizeOccurrences[1].PhysicalLineId,
                    selected.SectionOccurrenceOrdinal,
                    "metadata",
                    "Multiple Size occurrences require an explicit caller-side winner and are not merged."));
            }

            PreviewSizeRaw selectedSize = parsed.Count == 1 && sizeOccurrences.Length == 1 ? parsed[0] : null;
            return new PreviewMetadataReadResult(selected, selectedSize, parsed, diagnostics, execution);
        }

        private static bool TrySnapshotSections(
            IEnumerable<PreviewMetadataSectionOccurrence> source,
            PreviewMetadataReadPolicy policy,
            IList<PreviewDiagnostic> diagnostics,
            PreviewExecutionState execution,
            out PreviewMetadataSectionOccurrence[] result)
        {
            var values = new List<PreviewMetadataSectionOccurrence>();
            try
            {
                using (IEnumerator<PreviewMetadataSectionOccurrence> enumerator = source.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (values.Count >= policy.Limits.MaxMetadataSections)
                        {
                            Add(diagnostics, policy.Limits, execution, Diagnostic(
                                BinaryDiagnosticSeverity.Error,
                                PreviewDiagnosticCode.OccurrenceBudgetExceeded,
                                SyntheticSource(),
                                new[] { SyntheticProvenance() },
                                -1,
                                values.Count,
                                "metadata",
                                "Preview section occurrence budget exceeded."));
                            result = values.ToArray();
                            return false;
                        }

                        PreviewMetadataSectionOccurrence occurrence = enumerator.Current;
                        if (occurrence == null)
                        {
                            Add(diagnostics, policy.Limits, execution, Diagnostic(
                                BinaryDiagnosticSeverity.Error,
                                PreviewDiagnosticCode.InvalidSelection,
                                SyntheticSource(),
                                new[] { SyntheticProvenance() },
                                -1,
                                values.Count,
                                "metadata",
                                "Preview section occurrences cannot contain null values."));
                            result = values.ToArray();
                            return false;
                        }
                        values.Add(occurrence);
                    }
                }
            }
            catch (Exception exception)
            {
                Add(diagnostics, policy.Limits, execution, Diagnostic(
                    BinaryDiagnosticSeverity.Error,
                    PreviewDiagnosticCode.InvalidSelection,
                    SyntheticSource(),
                    new[] { SyntheticProvenance() },
                    -1,
                    values.Count,
                    "metadata",
                    "Preview section enumeration failed: " + exception.GetType().Name + "."));
                result = values.ToArray();
                return false;
            }

            result = values.ToArray();
            return true;
        }

        private static PreviewMetadataSectionOccurrence SelectSection(
            PreviewMetadataSectionOccurrence[] sections,
            PreviewMetadataReadPolicy policy,
            IList<PreviewDiagnostic> diagnostics,
            PreviewExecutionState execution)
        {
            switch (policy.SelectionStatus)
            {
                case PreviewSectionSelectionStatus.Missing:
                    Add(diagnostics, policy.Limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.MissingSection, SyntheticSource(), new[] { SyntheticProvenance() }, -1, -1, "selection", "The Preview section is missing."));
                    return null;
                case PreviewSectionSelectionStatus.PresentEmpty:
                    Add(diagnostics, policy.Limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.PresentEmptySection, SyntheticSource(), new[] { SyntheticProvenance() }, -1, -1, "selection", "The Preview section is present but empty."));
                    return null;
                case PreviewSectionSelectionStatus.AmbiguousMultipleOccurrences:
                    Add(diagnostics, policy.Limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.AmbiguousSectionOccurrence, SyntheticSource(), new[] { SyntheticProvenance() }, -1, -1, "selection", "Multiple Preview sections require explicit selection."));
                    return null;
                case PreviewSectionSelectionStatus.InvalidSelection:
                    Add(diagnostics, policy.Limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.InvalidSelection, SyntheticSource(), new[] { SyntheticProvenance() }, -1, -1, "selection", "The Preview section selection status is invalid."));
                    return null;
                case PreviewSectionSelectionStatus.SelectedOccurrence:
                    if (sections.Length == 0 || policy.SelectedSectionOccurrenceOrdinal < 0 ||
                        policy.SelectedSectionOccurrenceOrdinal >= sections.Length)
                    {
                        Add(diagnostics, policy.Limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.InvalidSelection, SyntheticSource(), new[] { SyntheticProvenance() }, -1, -1, "selection", "The selected Preview section occurrence is unavailable."));
                        return null;
                    }
                    if (policy.SelectedSectionOccurrenceOrdinal >= sections.Length)
                    {
                        Add(diagnostics, policy.Limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.InvalidSelection, sections[0].Source, sections.Select(item => item.Provenance), -1, policy.SelectedSectionOccurrenceOrdinal, "selection", "The selected Preview section occurrence is outside the candidate set."));
                        return null;
                    }
                    return sections[policy.SelectedSectionOccurrenceOrdinal];
                default:
                    Add(diagnostics, policy.Limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.InvalidSelection, SyntheticSource(), new[] { SyntheticProvenance() }, -1, -1, "selection", "The Preview section selection status is unknown."));
                    return null;
            }
        }

        private static bool TrySnapshotSizes(
            PreviewMetadataSectionOccurrence section,
            PreviewMetadataReadPolicy policy,
            IList<PreviewDiagnostic> diagnostics,
            PreviewExecutionState execution,
            out PreviewSizeOccurrence[] result)
        {
            var values = new List<PreviewSizeOccurrence>();
            try
            {
                using (IEnumerator<PreviewSizeOccurrence> enumerator = section.SizeOccurrences.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (values.Count >= policy.Limits.MaxSizeOccurrences)
                        {
                            Add(diagnostics, policy.Limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.OccurrenceBudgetExceeded, section.Source, new[] { section.Provenance }, -1, values.Count, "metadata", "Size occurrence budget exceeded."));
                            result = values.ToArray();
                            return false;
                        }
                        PreviewSizeOccurrence occurrence = enumerator.Current;
                        if (occurrence == null)
                        {
                            Add(diagnostics, policy.Limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.InvalidSelection, section.Source, new[] { section.Provenance }, -1, values.Count, "metadata", "Size occurrences cannot contain null values."));
                            result = values.ToArray();
                            return false;
                        }
                        values.Add(occurrence);
                    }
                }
            }
            catch (Exception exception)
            {
                Add(diagnostics, policy.Limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.InvalidSelection, section.Source, new[] { section.Provenance }, -1, values.Count, "metadata", "Size occurrence enumeration failed: " + exception.GetType().Name + "."));
                result = values.ToArray();
                return false;
            }
            result = values.ToArray();
            return true;
        }

        private static PreviewSizeRaw ParseSize(
            PreviewSizeOccurrence occurrence,
            BinarySourceContext source,
            int sectionOrdinal,
            IList<PreviewDiagnostic> diagnostics,
            PreviewExecutionState execution,
            PreviewReadLimits limits)
        {
            string[] tokens = occurrence.RawValue.Split(new[] { ',' }, StringSplitOptions.None);
            var values = new int?[tokens.Length];
            for (int index = 0; index < tokens.Length; index++)
            {
                int value;
                if (!int.TryParse(tokens[index].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    bool overflow = tokens[index].Trim().Length != 0 &&
                        tokens[index].Trim().TrimStart('+', '-').All(char.IsDigit);
                    Add(diagnostics, limits, execution, Diagnostic(
                        BinaryDiagnosticSeverity.Error,
                        overflow ? PreviewDiagnosticCode.SizeFieldOverflow : PreviewDiagnosticCode.SizeFieldParseFailure,
                        source,
                        new[] { occurrence.Provenance },
                        occurrence.PhysicalLineId,
                        sectionOrdinal,
                        "metadata",
                        "Preview Size contains a non-representable signed integer field."));
                    continue;
                }
                values[index] = value;
            }

            if (tokens.Length != 4)
            {
                Add(diagnostics, limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.MalformedSize, source, new[] { occurrence.Provenance }, occurrence.PhysicalLineId, sectionOrdinal, "metadata", "Preview Size must contain exactly four comma-separated fields."));
            }
            if (tokens.Length != 4 || values.Any(item => !item.HasValue))
                return new PreviewSizeRaw(tokens, Get(values, 0), Get(values, 1), Get(values, 2), Get(values, 3), occurrence.PhysicalLineId, occurrence.Provenance);

            int width = values[2].Value;
            int height = values[3].Value;
            if (width <= 0 || height <= 0)
                Add(diagnostics, limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.InvalidDimension, source, new[] { occurrence.Provenance }, occurrence.PhysicalLineId, sectionOrdinal, "metadata", "Preview width and height candidates must be positive."));
            if (width > limits.MaxWidth || height > limits.MaxHeight)
                Add(diagnostics, limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.DimensionBudgetExceeded, source, new[] { occurrence.Provenance }, occurrence.PhysicalLineId, sectionOrdinal, "metadata", "Preview dimensions exceed the configured budget."));
            try
            {
                long pixels = checked((long)width * height);
                if (pixels > limits.MaxPixels)
                    Add(diagnostics, limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.PixelCountBudgetExceeded, source, new[] { occurrence.Provenance }, occurrence.PhysicalLineId, sectionOrdinal, "metadata", "Preview pixel count exceeds the configured budget."));
            }
            catch (OverflowException)
            {
                Add(diagnostics, limits, execution, Diagnostic(BinaryDiagnosticSeverity.Error, PreviewDiagnosticCode.ArithmeticOverflow, source, new[] { occurrence.Provenance }, occurrence.PhysicalLineId, sectionOrdinal, "metadata", "Preview pixel count arithmetic overflowed."));
            }
            return new PreviewSizeRaw(tokens, values[0], values[1], values[2], values[3], occurrence.PhysicalLineId, occurrence.Provenance);
        }

        private static int? Get(int?[] values, int index)
        {
            return index < values.Length ? values[index] : (int?)null;
        }

        private static void Add(IList<PreviewDiagnostic> diagnostics, PreviewReadLimits limits, PreviewExecutionState execution, PreviewDiagnostic diagnostic)
        {
            execution.Observe(diagnostic.Severity);
            if (diagnostics.Count < limits.MaxDiagnostics)
                diagnostics.Add(diagnostic);
            else
                execution.SuppressOne();
        }

        private static PreviewDiagnostic Diagnostic(BinaryDiagnosticSeverity severity, PreviewDiagnosticCode code, BinarySourceContext source, IEnumerable<IniSourceProvenance> provenance, long offset, int ordinal, string stage, string message)
        {
            return new PreviewDiagnostic(severity, code, source, provenance, offset, ordinal, stage, message);
        }

        private static BinarySourceContext SyntheticSource()
        {
            return new BinarySourceContext("preview-metadata-reader", "preview-input", LogicalContentPath.Parse("preview-input"));
        }

        private static IniSourceProvenance SyntheticProvenance()
        {
            return new IniSourceProvenance("preview-input", new[] { LogicalContentPath.Parse("preview-input") });
        }
    }
}
