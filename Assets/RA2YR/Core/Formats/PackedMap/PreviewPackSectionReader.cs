using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal sealed class PreviewPackSectionReader
    {
        public PreviewPackReadResult Read(
            PreviewMetadataReadResult metadata,
            PreviewPackSectionInput input,
            PreviewPackReadPolicy policy,
            ILzoDecodeBackend lzoBackend = null)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var diagnostics = new List<PreviewDiagnostic>();
            var execution = new PreviewExecutionState();
            PreviewDiagnosticCode invalidCode;
            string invalidMessage;
            if (!policy.TryValidate(out invalidCode, out invalidMessage))
            {
                Add(diagnostics, policy.Limits, execution, Error(input, invalidCode, "policy", invalidMessage));
                return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
            }

            if (!string.Equals(input.SectionName, "PreviewPack", StringComparison.Ordinal))
            {
                Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.SectionNameMismatch, "selection", "The selected section is not PreviewPack."));
                return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
            }
            switch (input.SelectionStatus)
            {
                case PreviewSectionSelectionStatus.Missing:
                    Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.MissingSection, "selection", "The PreviewPack section is missing."));
                    return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
                case PreviewSectionSelectionStatus.PresentEmpty:
                    Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.PresentEmptySection, "selection", "The PreviewPack section is present but empty."));
                    return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
                case PreviewSectionSelectionStatus.AmbiguousMultipleOccurrences:
                    Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.AmbiguousSectionOccurrence, "selection", "Multiple PreviewPack sections require explicit selection."));
                    return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
                case PreviewSectionSelectionStatus.InvalidSelection:
                    Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.InvalidSelection, "selection", "The PreviewPack section selection status is invalid."));
                    return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
                case PreviewSectionSelectionStatus.SelectedOccurrence:
                    break;
                default:
                    Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.InvalidSelection, "selection", "The PreviewPack section selection status is unknown."));
                    return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
            }

            if (policy.PackedPolicy.Codec != PackedCodecKind.RawLzo1X)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.WrongCodec, "packed", "PreviewPack requires the explicit RawLzo1X codec policy."));
                return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
            }
            if (lzoBackend == null)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.BackendUnavailable, "packed", "PreviewPack RawLzo1X decoding requires an injected backend."));
                return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
            }
            if (policy.CancellationToken.IsCancellationRequested || policy.PackedPolicy.CancellationToken.IsCancellationRequested)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.Cancellation, "packed", "PreviewPack decoding was cancelled before input consumption."));
                return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
            }

            List<PackedIniFragmentOccurrence> occurrences;
            if (!TrySnapshotOccurrences(input, policy, diagnostics, execution, out occurrences))
                return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
            if (occurrences.Count == 0)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.PresentEmptySection, "selection", "The selected PreviewPack section has no fragment occurrences."));
                return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
            }

            PackedSectionDecodeResult packed;
            try
            {
                packed = new PackedSectionDecodePipeline().Decode(occurrences, policy.PackedPolicy, lzoBackend);
            }
            catch (Exception exception)
            {
                execution.ObserveFailure();
                Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.PackedStageFailure, "packed", "PreviewPack packed decoding threw " + exception.GetType().Name + "."));
                return new PreviewPackReadResult(metadata, input, null, null, null, diagnostics, execution);
            }
            execution.Merge(packed, true);
            if (packed == null || !packed.IsSuccess || packed.DecodedBytes == null)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.PackedStageFailure, "packed", "Packed decoding failed; decoded preview interpretation was not attempted."));
                return new PreviewPackReadResult(metadata, input, packed, null, null, diagnostics, execution);
            }

            byte[] decodedBytes = packed.DecodedBytes;
            if (decodedBytes.LongLength > policy.Limits.MaxDecodedBytes)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.DecodedOutputBudgetExceeded, "length", "Decoded PreviewPack output exceeds the configured budget."));
                return new PreviewPackReadResult(metadata, input, packed, null, null, diagnostics, execution);
            }

            PreviewSizeRaw size = metadata == null ? null : metadata.Size;
            long expectedLength = -1;
            PreviewLengthStatus lengthStatus = PreviewLengthStatus.MetadataInvalid;
            if (metadata == null || !metadata.IsSuccess || size == null)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.MetadataMissing, "length", "Preview metadata was not successfully selected and parsed."));
            }
            else
            {
                try
                {
                    if (!size.Field2Raw.HasValue || !size.Field3Raw.HasValue || size.Field2Raw.Value <= 0 || size.Field3Raw.Value <= 0)
                    {
                        Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.InvalidDimension, "length", "Preview width and height candidates are invalid."));
                    }
                    else
                    {
                        long pixels = checked((long)size.Field2Raw.Value * size.Field3Raw.Value);
                        if (pixels > policy.Limits.MaxPixels)
                        {
                            Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.PixelCountBudgetExceeded, "length", "Preview pixel count exceeds the configured budget."));
                        }
                        expectedLength = checked(pixels * 3L);
                        if (expectedLength > policy.Limits.MaxDecodedBytes)
                        {
                            Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.DecodedOutputBudgetExceeded, "length", "Expected Preview decoded length exceeds the configured budget."));
                        }
                        else if (decodedBytes.LongLength < expectedLength)
                        {
                            lengthStatus = PreviewLengthStatus.Underflow;
                            Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.LengthUnderflow, "length", "Decoded Preview bytes are shorter than the exact three-component length."));
                        }
                        else if (decodedBytes.LongLength > expectedLength)
                        {
                            lengthStatus = PreviewLengthStatus.Overflow;
                            Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.LengthOverflow, "length", "Decoded Preview bytes contain trailing data beyond the exact three-component length."));
                        }
                        else
                        {
                            lengthStatus = PreviewLengthStatus.Exact;
                        }
                    }
                }
                catch (OverflowException)
                {
                    lengthStatus = PreviewLengthStatus.ArithmeticOverflow;
                    Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.ArithmeticOverflow, "length", "Preview expected decoded length arithmetic overflowed."));
                }
            }

            IniSourceProvenance[] provenance = input.Provenance.ToArray();
            var decoded = new PreviewDecodedStream(decodedBytes, expectedLength, decodedBytes.LongLength, lengthStatus, packed, provenance);
            PreviewPixelLayoutView layout = null;
            if (size != null && size.Field2Raw.HasValue && size.Field3Raw.HasValue && size.Field2Raw.Value > 0 && size.Field3Raw.Value > 0)
            {
                try
                {
                    layout = new PreviewPixelLayoutView(decoded, size.Field2Raw.Value, size.Field3Raw.Value, policy.ChannelProfile, policy.RowOrderProfile);
                }
                catch (OverflowException)
                {
                    Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.ArithmeticOverflow, "layout", "Preview pixel layout arithmetic overflowed."));
                }
            }

            if (decoded.IsExact && metadata != null && metadata.IsSuccess && layout != null)
                execution.MarkExecuted();
            return new PreviewPackReadResult(metadata, input, packed, decoded, layout, diagnostics, execution);
        }

        private static bool TrySnapshotOccurrences(
            PreviewPackSectionInput input,
            PreviewPackReadPolicy policy,
            IList<PreviewDiagnostic> diagnostics,
            PreviewExecutionState execution,
            out List<PackedIniFragmentOccurrence> result)
        {
            result = new List<PackedIniFragmentOccurrence>();
            try
            {
                using (IEnumerator<PackedIniFragmentOccurrence> enumerator = input.Occurrences.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (result.Count >= policy.Limits.MaxFragments)
                        {
                            Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.OccurrenceBudgetExceeded, "selection", "PreviewPack fragment occurrence budget exceeded."));
                            return false;
                        }
                        PackedIniFragmentOccurrence occurrence = enumerator.Current;
                        if (occurrence == null)
                        {
                            Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.NoProgress, "selection", "PreviewPack fragment occurrences cannot contain null values."));
                            return false;
                        }
                        if (!string.Equals(occurrence.SectionName, "PreviewPack", StringComparison.Ordinal))
                        {
                            Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.SectionNameMismatch, "selection", "A selected PreviewPack fragment has a different section name."));
                            return false;
                        }
                        result.Add(occurrence);
                    }
                }
            }
            catch (Exception exception)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, PreviewDiagnosticCode.PackedStageFailure, "selection", "PreviewPack occurrence enumeration failed: " + exception.GetType().Name + "."));
                return false;
            }
            return true;
        }

        private static void Add(IList<PreviewDiagnostic> diagnostics, PreviewReadLimits limits, PreviewExecutionState execution, PreviewDiagnostic diagnostic)
        {
            execution.Observe(diagnostic.Severity);
            if (diagnostics.Count < limits.MaxDiagnostics)
                diagnostics.Add(diagnostic);
            else
                execution.SuppressOne();
        }

        private static PreviewDiagnostic Error(PreviewPackSectionInput input, PreviewDiagnosticCode code, string stage, string message)
        {
            return new PreviewDiagnostic(BinaryDiagnosticSeverity.Error, code, input.Source, input.Provenance, -1, input.SelectedSectionOccurrenceOrdinal, stage, message);
        }
    }
}
