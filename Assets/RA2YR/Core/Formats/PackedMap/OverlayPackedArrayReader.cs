using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal sealed class OverlayPackedArrayReader
    {
        public OverlayArrayReadResult Read(
            OverlaySectionInput input,
            OverlaySectionKind expectedSectionKind,
            OverlayPackedReadPolicy policy)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            OverlayStorageProfiles.Validate(expectedSectionKind, nameof(expectedSectionKind));

            var diagnostics = new List<OverlayDiagnostic>();
            var execution = new OverlayExecutionState();
            PackedMapDiagnostic invalidPolicy;
            if (!policy.PackedPolicy.TryValidate(out invalidPolicy))
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.InvalidPackedPolicy, "packed-policy", invalidPolicy.Message));
                return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
            }
            string expectedSectionName = OverlayStorageProfiles.GetExpectedSectionName(expectedSectionKind);
            if (input.SectionKind != expectedSectionKind || !string.Equals(input.SectionName, expectedSectionName, StringComparison.Ordinal))
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.WrongSectionKind, "selection", "The selected source does not match the required Overlay section kind."));
                return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
            }

            switch (input.SelectionStatus)
            {
                case OverlaySectionSelectionStatus.Missing:
                    Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.MissingSection, "selection", "The Overlay section is missing."));
                    return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
                case OverlaySectionSelectionStatus.PresentEmpty:
                    Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.PresentButEmptySection, "selection", "The Overlay section is present but has no fragment occurrences."));
                    return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
                case OverlaySectionSelectionStatus.Ambiguous:
                    Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.AmbiguousSectionOccurrence, "selection", "Multiple Overlay section occurrences were supplied without an explicit selection."));
                    return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
                case OverlaySectionSelectionStatus.Selected:
                    break;
                default:
                    Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.WrongSectionKind, "selection", "The Overlay section selection status is unsupported."));
                    return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
            }

            List<PackedIniFragmentOccurrence> occurrences;
            OverlayDiagnosticCode occurrenceFailure;
            string occurrenceFailureMessage;
            if (!TrySnapshotOccurrences(input.Occurrences, policy.PackedPolicy.FragmentLimits.MaxFragments, out occurrences, out occurrenceFailure, out occurrenceFailureMessage))
            {
                Add(diagnostics, policy.Limits, execution, Error(input, occurrenceFailure, "selection", occurrenceFailureMessage));
                return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
            }
            if (occurrences.Any(item => !string.Equals(item.SectionName, expectedSectionName, StringComparison.Ordinal)))
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.WrongSectionKind, "selection", "The selected source does not match the required Overlay section kind."));
                return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
            }
            if (occurrences.Count == 0)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.NoFragmentOccurrences, "selection", "The selected Overlay section contains no fragment occurrences."));
                return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
            }
            if (policy.PackedPolicy.Codec != PackedCodecKind.Format80)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.WrongCodec, "packed", "Overlay packed arrays require the explicit Format80 codec policy."));
                return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
            }
            if (!IsRequiredFormat80Profile(policy.PackedPolicy.Format80Profile))
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.UnsupportedFormat80Profile, "packed", "Overlay packed arrays require the explicit absolute Format80 profile with a required terminator and exact input consumption."));
                return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
            }

            int expectedLength;
            try
            {
                expectedLength = OverlayStorageProfiles.GetExpectedLength(policy.StorageProfile, expectedSectionKind);
            }
            catch (ArgumentOutOfRangeException)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.UnsupportedStorageProfile, "array", "The selected Overlay storage profile is unsupported."));
                return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
            }
            if (expectedLength > policy.Limits.MaxRawArrayBytes)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.RawArrayBudgetExceeded, "array", "The selected Overlay profile exceeds the raw-array budget."));
                return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
            }

            PackedSectionDecodeResult packed;
            try
            {
                packed = new PackedSectionDecodePipeline().Decode(occurrences, policy.PackedPolicy);
            }
            catch (Exception exception)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.PackedStageFailure, "packed", "Packed section decoding threw " + exception.GetType().Name + "."));
                return new OverlayArrayReadResult(input, null, null, diagnostics, execution);
            }

            execution.ObservePacked(packed);
            byte[] decodedBytes = packed == null ? null : packed.DecodedBytes;
            if (packed == null || !packed.IsSuccess || decodedBytes == null)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.PackedStageFailure, "packed", "Packed section decoding failed; raw array construction was not attempted."));
                return new OverlayArrayReadResult(input, packed, null, diagnostics, execution);
            }
            if (packed.Envelope == null || packed.Envelope.Blocks.Count == 0)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.ZeroBlockPackedResult, "packed", "A successful Overlay packed result must contain at least one chunk block."));
                return new OverlayArrayReadResult(input, packed, null, diagnostics, execution);
            }

            long declaredLength;
            try
            {
                declaredLength = 0;
                foreach (WestwoodChunkEnvelope block in packed.Envelope.Blocks)
                    declaredLength = checked(declaredLength + block.UncompressedSize);
            }
            catch (OverflowException)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.DeclaredLengthArithmeticOverflow, "array", "Overlay declared aggregate output length overflowed."));
                return new OverlayArrayReadResult(input, packed, null, diagnostics, execution);
            }

            if (declaredLength != expectedLength || decodedBytes.Length != expectedLength || declaredLength != decodedBytes.LongLength)
            {
                Add(diagnostics, policy.Limits, execution, Error(input, OverlayDiagnosticCode.ArrayLengthMismatch, "array", "Overlay decoded bytes must exactly match the selected storage profile and chunk declarations."));
                return new OverlayArrayReadResult(input, packed, null, diagnostics, execution);
            }

            var raw = new OverlayArrayRaw(
                expectedSectionKind,
                policy.StorageProfile,
                expectedLength,
                declaredLength,
                decodedBytes,
                packed,
                input.Provenance);
            execution.MarkSucceeded();
            return new OverlayArrayReadResult(input, packed, raw, diagnostics, execution);
        }

        private static bool IsRequiredFormat80Profile(Format80Profile profile)
        {
            return profile != null &&
                profile.Variant == Format80Variant.Absolute &&
                profile.RequireTerminator &&
                !profile.AllowTrailingAfterTerminator &&
                !profile.AllowInitialMarker &&
                profile.RejectZeroFill;
        }

        private static bool TrySnapshotOccurrences(
            IEnumerable<PackedIniFragmentOccurrence> source,
            int maxFragments,
            out List<PackedIniFragmentOccurrence> occurrences,
            out OverlayDiagnosticCode failureCode,
            out string failureMessage)
        {
            occurrences = new List<PackedIniFragmentOccurrence>();
            failureCode = OverlayDiagnosticCode.PackedStageFailure;
            failureMessage = null;
            try
            {
                using (IEnumerator<PackedIniFragmentOccurrence> enumerator = source.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (occurrences.Count >= maxFragments)
                        {
                            failureCode = OverlayDiagnosticCode.OccurrenceInputBudgetExceeded;
                            failureMessage = "Overlay fragment occurrences exceed the configured packed fragment budget.";
                            return false;
                        }

                        PackedIniFragmentOccurrence occurrence = enumerator.Current;
                        if (occurrence == null)
                        {
                            failureCode = OverlayDiagnosticCode.InvalidFragmentOccurrence;
                            failureMessage = "Overlay fragment occurrences cannot contain null values.";
                            return false;
                        }
                        occurrences.Add(occurrence);
                    }
                }
            }
            catch (Exception exception)
            {
                failureMessage = "Overlay fragment enumeration threw " + exception.GetType().Name + ".";
                return false;
            }
            return true;
        }

        private static void Add(IList<OverlayDiagnostic> diagnostics, OverlayReadLimits limits, OverlayExecutionState execution, OverlayDiagnostic diagnostic)
        {
            execution.Observe(diagnostic.Severity);
            if (diagnostics.Count < limits.MaxDiagnostics)
                diagnostics.Add(diagnostic);
            else
                execution.SuppressOne();
        }

        private static OverlayDiagnostic Error(OverlaySectionInput input, OverlayDiagnosticCode code, string stage, string message)
        {
            return new OverlayDiagnostic(BinaryDiagnosticSeverity.Error, code, input.SectionKind, input.Source, input.Provenance, -1, stage, message);
        }
    }

    internal sealed class OverlayPackedDocumentReader
    {
        public OverlayPackedDocumentReadResult Read(
            OverlaySectionInput overlayPack,
            OverlaySectionInput overlayDataPack,
            OverlayPackedReadPolicy policy)
        {
            if (overlayPack == null) throw new ArgumentNullException(nameof(overlayPack));
            if (overlayDataPack == null) throw new ArgumentNullException(nameof(overlayDataPack));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var reader = new OverlayPackedArrayReader();
            OverlayArrayReadResult typeResult = reader.Read(overlayPack, OverlaySectionKind.OverlayPack, policy);
            OverlayArrayReadResult dataResult = reader.Read(overlayDataPack, OverlaySectionKind.OverlayDataPack, policy);
            var diagnostics = new List<OverlayDiagnostic>();
            var execution = new OverlayExecutionState();
            execution.Merge(typeResult == null ? null : typeResult.Execution, true);
            execution.Merge(dataResult == null ? null : dataResult.Execution, true);

            if (typeResult == null || typeResult.CompletionStatus == OverlayCompletionStatus.NotRun)
                Add(diagnostics, policy.Limits, execution, RequiredStageNotRun(overlayPack, "OverlayPack"));
            else if (!typeResult.IsSuccess)
                Add(diagnostics, policy.Limits, execution, PartnerUnavailable(overlayPack, "OverlayPack"));

            if (dataResult == null || dataResult.CompletionStatus == OverlayCompletionStatus.NotRun)
                Add(diagnostics, policy.Limits, execution, RequiredStageNotRun(overlayDataPack, "OverlayDataPack"));
            else if (!dataResult.IsSuccess)
                Add(diagnostics, policy.Limits, execution, PartnerUnavailable(overlayDataPack, "OverlayDataPack"));

            if (typeResult != null && dataResult != null && typeResult.IsSuccess && dataResult.IsSuccess)
                execution.MarkSucceeded();
            return new OverlayPackedDocumentReadResult(typeResult, dataResult, diagnostics, execution);
        }

        private static OverlayDiagnostic RequiredStageNotRun(OverlaySectionInput input, string name)
        {
            return new OverlayDiagnostic(BinaryDiagnosticSeverity.Error, OverlayDiagnosticCode.RequiredStageNotRun, input.SectionKind, input.Source, input.Provenance, -1, "pair", name + " did not run.");
        }

        private static OverlayDiagnostic PartnerUnavailable(OverlaySectionInput input, string name)
        {
            return new OverlayDiagnostic(BinaryDiagnosticSeverity.Error, OverlayDiagnosticCode.PartnerUnavailableOrIncomplete, input.SectionKind, input.Source, input.Provenance, -1, "pair", name + " did not produce a complete raw array; its partner result remains independent.");
        }

        private static void Add(IList<OverlayDiagnostic> diagnostics, OverlayReadLimits limits, OverlayExecutionState execution, OverlayDiagnostic diagnostic)
        {
            execution.Observe(diagnostic.Severity);
            if (diagnostics.Count < limits.MaxDiagnostics)
                diagnostics.Add(diagnostic);
            else
                execution.SuppressOne();
        }
    }

    internal sealed class OverlayStorageCoordinateIndexer
    {
        public OverlayStorageIndexResult GetIndex(
            OverlayStorageCoordinate coordinate,
            OverlayStorageCoordinateIndexProfile profile,
            BinarySourceContext source = null,
            IEnumerable<IniSourceProvenance> provenance = null,
            OverlayReadLimits limits = null)
        {
            source = source ?? SyntheticSource();
            limits = limits ?? new OverlayReadLimits();
            IReadOnlyList<IniSourceProvenance> chain = NormalizeProvenance(provenance, source);
            var diagnostics = new List<OverlayDiagnostic>();
            var execution = new OverlayExecutionState();
            if (!IsKnownProfile(profile))
            {
                Add(diagnostics, limits, execution, Error(OverlayDiagnosticCode.InvalidCoordinateProfile, source, chain, "The selected Overlay storage coordinate profile is unsupported."));
                return new OverlayStorageIndexResult(coordinate, profile, null, diagnostics, execution);
            }
            if (coordinate.X < 0 || coordinate.X >= OverlayStorageProfiles.StorageSideLength ||
                coordinate.Y < 0 || coordinate.Y >= OverlayStorageProfiles.StorageSideLength)
            {
                Add(diagnostics, limits, execution, Error(OverlayDiagnosticCode.StorageCoordinateOutOfRange, source, chain, "Overlay storage coordinates must be within 0..511."));
                return new OverlayStorageIndexResult(coordinate, profile, null, diagnostics, execution);
            }

            try
            {
                int index;
                switch (profile)
                {
                    case OverlayStorageCoordinateIndexProfile.ExternalRowMajorCandidate:
                        index = checked(coordinate.X + checked(OverlayStorageProfiles.StorageSideLength * coordinate.Y));
                        break;
                    case OverlayStorageCoordinateIndexProfile.OfficialEditorTransposedComparison:
                        index = checked(coordinate.Y + checked(OverlayStorageProfiles.StorageSideLength * coordinate.X));
                        break;
                    default:
                        Add(diagnostics, limits, execution, Error(OverlayDiagnosticCode.InvalidCoordinateProfile, source, chain, "The selected Overlay storage coordinate profile is unsupported."));
                        return new OverlayStorageIndexResult(coordinate, profile, null, diagnostics, execution);
                }
                if (index < 0 || index >= OverlayStorageProfiles.OrdinaryByteLength)
                {
                    Add(diagnostics, limits, execution, Error(OverlayDiagnosticCode.StorageCoordinateOutOfRange, source, chain, "Overlay storage coordinate resolves outside the ordinary storage profile."));
                    return new OverlayStorageIndexResult(coordinate, profile, null, diagnostics, execution);
                }
                execution.MarkSucceeded();
                return new OverlayStorageIndexResult(coordinate, profile, index, diagnostics, execution);
            }
            catch (OverflowException)
            {
                Add(diagnostics, limits, execution, Error(OverlayDiagnosticCode.IndexArithmeticOverflow, source, chain, "Overlay storage index arithmetic overflowed."));
                return new OverlayStorageIndexResult(coordinate, profile, null, diagnostics, execution);
            }
        }

        private static bool IsKnownProfile(OverlayStorageCoordinateIndexProfile profile)
        {
            return profile == OverlayStorageCoordinateIndexProfile.ExternalRowMajorCandidate ||
                profile == OverlayStorageCoordinateIndexProfile.OfficialEditorTransposedComparison;
        }

        private static void Add(IList<OverlayDiagnostic> diagnostics, OverlayReadLimits limits, OverlayExecutionState execution, OverlayDiagnostic diagnostic)
        {
            execution.Observe(diagnostic.Severity);
            if (diagnostics.Count < limits.MaxDiagnostics)
                diagnostics.Add(diagnostic);
            else
                execution.SuppressOne();
        }

        private static OverlayDiagnostic Error(OverlayDiagnosticCode code, BinarySourceContext source, IEnumerable<IniSourceProvenance> provenance, string message)
        {
            return new OverlayDiagnostic(BinaryDiagnosticSeverity.Error, code, null, source, provenance, -1, "coordinate", message);
        }

        private static BinarySourceContext SyntheticSource()
        {
            return new BinarySourceContext("overlay-storage-index", "overlay-storage", LogicalContentPath.Parse("overlay-storage"));
        }

        private static IReadOnlyList<IniSourceProvenance> NormalizeProvenance(IEnumerable<IniSourceProvenance> provenance, BinarySourceContext source)
        {
            if (provenance == null)
                return new[] { new IniSourceProvenance(source.LogicalSourceId, new[] { source.LogicalPath }) };
            IniSourceProvenance[] chain = provenance.ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null)) throw new ArgumentException("Provenance is required.", nameof(provenance));
            return chain;
        }
    }
}
