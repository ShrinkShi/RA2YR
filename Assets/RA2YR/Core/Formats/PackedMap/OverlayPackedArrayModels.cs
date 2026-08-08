using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal enum OverlaySectionKind
    {
        OverlayPack,
        OverlayDataPack
    }

    internal enum OverlaySectionSelectionStatus
    {
        Missing,
        PresentEmpty,
        Selected,
        Ambiguous
    }

    internal enum OverlayStorageProfile
    {
        OrdinaryByte512
    }

    internal enum OverlayStorageCoordinateIndexProfile
    {
        ExternalRowMajorCandidate,
        OfficialEditorTransposedComparison
    }

    internal enum OverlayCompletionStatus
    {
        NotRun,
        Succeeded,
        Failed
    }

    internal enum OverlayDiagnosticCode
    {
        WrongSectionKind,
        MissingSection,
        PresentButEmptySection,
        AmbiguousSectionOccurrence,
        NoFragmentOccurrences,
        InvalidFragmentOccurrence,
        OccurrenceInputBudgetExceeded,
        UnsupportedStorageProfile,
        UnsupportedFormat80Profile,
        InvalidPackedPolicy,
        WrongCodec,
        PackedStageFailure,
        ZeroBlockPackedResult,
        DeclaredLengthArithmeticOverflow,
        ArrayLengthMismatch,
        RawArrayBudgetExceeded,
        InvalidCoordinateProfile,
        StorageCoordinateOutOfRange,
        IndexArithmeticOverflow,
        RequiredStageNotRun,
        PartnerUnavailableOrIncomplete
    }

    internal static class OverlayStorageProfiles
    {
        internal const int StorageSideLength = 512;
        internal const int OrdinaryByteLength = StorageSideLength * StorageSideLength;

        internal static void Validate(OverlayStorageProfile profile, string parameterName)
        {
            if (profile != OverlayStorageProfile.OrdinaryByte512)
                throw new ArgumentOutOfRangeException(parameterName, profile, "Unknown Overlay storage profile.");
        }

        internal static void Validate(OverlayStorageCoordinateIndexProfile profile, string parameterName)
        {
            switch (profile)
            {
                case OverlayStorageCoordinateIndexProfile.ExternalRowMajorCandidate:
                case OverlayStorageCoordinateIndexProfile.OfficialEditorTransposedComparison:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(parameterName, profile, "Unknown Overlay storage coordinate profile.");
            }
        }

        internal static void Validate(OverlaySectionKind kind, string parameterName)
        {
            switch (kind)
            {
                case OverlaySectionKind.OverlayPack:
                case OverlaySectionKind.OverlayDataPack:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(parameterName, kind, "Unknown Overlay section kind.");
            }
        }

        internal static void Validate(OverlaySectionSelectionStatus status, string parameterName)
        {
            switch (status)
            {
                case OverlaySectionSelectionStatus.Missing:
                case OverlaySectionSelectionStatus.PresentEmpty:
                case OverlaySectionSelectionStatus.Selected:
                case OverlaySectionSelectionStatus.Ambiguous:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(parameterName, status, "Unknown Overlay section selection status.");
            }
        }

        internal static int GetExpectedLength(OverlayStorageProfile profile, OverlaySectionKind kind)
        {
            Validate(profile, nameof(profile));
            Validate(kind, nameof(kind));
            return OrdinaryByteLength;
        }

        internal static string GetExpectedSectionName(OverlaySectionKind kind)
        {
            switch (kind)
            {
                case OverlaySectionKind.OverlayPack:
                    return "OverlayPack";
                case OverlaySectionKind.OverlayDataPack:
                    return "OverlayDataPack";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown Overlay section kind.");
            }
        }
    }

    internal sealed class OverlayExecutionState
    {
        private bool hasExecuted;
        private bool hasFatalError;
        private BinaryDiagnosticSeverity highestSeverity = BinaryDiagnosticSeverity.Info;
        private int suppressedDiagnosticCount;

        public OverlayCompletionStatus CompletionStatus
        {
            get
            {
                if (!hasExecuted) return OverlayCompletionStatus.NotRun;
                return hasFatalError ? OverlayCompletionStatus.Failed : OverlayCompletionStatus.Succeeded;
            }
        }

        public bool HasFatalError => hasFatalError;
        public BinaryDiagnosticSeverity HighestObservedSeverity => highestSeverity;
        public int SuppressedDiagnosticCount => suppressedDiagnosticCount;

        internal void MarkSucceeded()
        {
            hasExecuted = true;
        }

        internal void Observe(BinaryDiagnosticSeverity severity)
        {
            hasExecuted = true;
            if ((int)severity > (int)highestSeverity) highestSeverity = severity;
            if (severity == BinaryDiagnosticSeverity.Error) hasFatalError = true;
        }

        internal void ObserveFailure()
        {
            hasExecuted = true;
            hasFatalError = true;
            if ((int)highestSeverity < (int)BinaryDiagnosticSeverity.Error)
                highestSeverity = BinaryDiagnosticSeverity.Error;
        }

        internal void ObservePacked(PackedSectionDecodeResult packed)
        {
            if (packed == null)
            {
                ObserveFailure();
                return;
            }

            MarkSucceeded();
            foreach (PackedMapDiagnostic diagnostic in packed.Diagnostics)
                Observe(diagnostic.Severity);
            if (!packed.IsSuccess)
                ObserveFailure();
        }

        internal void Merge(OverlayExecutionState child, bool required)
        {
            if (child == null || child.CompletionStatus == OverlayCompletionStatus.NotRun)
            {
                if (required) ObserveFailure();
                return;
            }

            MarkSucceeded();
            Observe(child.HighestObservedSeverity);
            if (child.HasFatalError) ObserveFailure();
            AddSuppressed(child.SuppressedDiagnosticCount);
        }

        internal void SuppressOne()
        {
            MarkSucceeded();
            AddSuppressed(1);
        }

        internal void Suppress(int count)
        {
            MarkSucceeded();
            AddSuppressed(count);
        }

        private void AddSuppressed(int count)
        {
            if (count <= 0 || suppressedDiagnosticCount == int.MaxValue) return;
            long aggregate = (long)suppressedDiagnosticCount + count;
            suppressedDiagnosticCount = aggregate >= int.MaxValue ? int.MaxValue : (int)aggregate;
        }
    }

    internal sealed class OverlayDiagnostic
    {
        internal OverlayDiagnostic(
            BinaryDiagnosticSeverity severity,
            OverlayDiagnosticCode code,
            OverlaySectionKind? sectionKind,
            BinarySourceContext source,
            IEnumerable<IniSourceProvenance> provenance,
            long offset,
            string stage,
            string message)
        {
            Severity = severity;
            Code = code;
            SectionKind = sectionKind;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            IniSourceProvenance[] chain = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null))
                throw new ArgumentException("Overlay provenance is required.", nameof(provenance));
            Provenance = Array.AsReadOnly(chain);
            Offset = offset;
            Stage = BinaryDiagnosticLabel.Validate(stage, nameof(stage));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public BinaryDiagnosticSeverity Severity { get; }
        public OverlayDiagnosticCode Code { get; }
        public OverlaySectionKind? SectionKind { get; }
        public BinarySourceContext Source { get; }
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }
        public long Offset { get; }
        public string Stage { get; }
        public string Message { get; }
    }

    internal sealed class OverlayReadLimits
    {
        public OverlayReadLimits(int maxDiagnostics = 4096, long maxRawArrayBytes = OverlayStorageProfiles.OrdinaryByteLength)
        {
            if (maxDiagnostics < 0 || maxRawArrayBytes < 0) throw new ArgumentOutOfRangeException();
            MaxDiagnostics = maxDiagnostics;
            MaxRawArrayBytes = maxRawArrayBytes;
        }

        public int MaxDiagnostics { get; }
        public long MaxRawArrayBytes { get; }
    }

    internal sealed class OverlaySectionInput
    {
        internal OverlaySectionInput(
            OverlaySectionKind sectionKind,
            string sectionName,
            OverlaySectionSelectionStatus selectionStatus,
            int selectedSectionOccurrenceOrdinal,
            int candidateSectionOccurrenceCount,
            IEnumerable<PackedIniFragmentOccurrence> occurrences,
            BinarySourceContext source,
            IEnumerable<IniSourceProvenance> provenance)
        {
            OverlayStorageProfiles.Validate(sectionKind, nameof(sectionKind));
            OverlayStorageProfiles.Validate(selectionStatus, nameof(selectionStatus));
            if (string.IsNullOrWhiteSpace(sectionName)) throw new ArgumentException("A section name is required.", nameof(sectionName));
            if (candidateSectionOccurrenceCount < 0 || selectedSectionOccurrenceOrdinal < -1) throw new ArgumentOutOfRangeException();
            if (occurrences == null) throw new ArgumentNullException(nameof(occurrences));
            ValidateSelection(selectionStatus, selectedSectionOccurrenceOrdinal, candidateSectionOccurrenceCount);
            SectionKind = sectionKind;
            SectionName = sectionName;
            SelectionStatus = selectionStatus;
            SelectedSectionOccurrenceOrdinal = selectedSectionOccurrenceOrdinal;
            CandidateSectionOccurrenceCount = candidateSectionOccurrenceCount;
            Occurrences = occurrences;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            IniSourceProvenance[] chain = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null))
                throw new ArgumentException("Section provenance is required.", nameof(provenance));
            Provenance = Array.AsReadOnly(chain);
        }

        public OverlaySectionKind SectionKind { get; }
        public string SectionName { get; }
        public OverlaySectionSelectionStatus SelectionStatus { get; }
        public int SelectedSectionOccurrenceOrdinal { get; }
        public int CandidateSectionOccurrenceCount { get; }
        public IEnumerable<PackedIniFragmentOccurrence> Occurrences { get; }
        public BinarySourceContext Source { get; }
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }

        private static void ValidateSelection(OverlaySectionSelectionStatus status, int selectedOrdinal, int candidateCount)
        {
            switch (status)
            {
                case OverlaySectionSelectionStatus.Missing:
                    if (selectedOrdinal != -1 || candidateCount != 0)
                        throw new ArgumentException("A missing section cannot carry selected occurrences.");
                    return;
                case OverlaySectionSelectionStatus.PresentEmpty:
                    if (selectedOrdinal < 0 || candidateCount != 1)
                        throw new ArgumentException("A present-empty section must identify exactly one empty source occurrence.");
                    return;
                case OverlaySectionSelectionStatus.Selected:
                    if (selectedOrdinal < 0 || candidateCount != 1)
                        throw new ArgumentException("A selected section must identify exactly one section occurrence.");
                    return;
                case OverlaySectionSelectionStatus.Ambiguous:
                    if (selectedOrdinal != -1 || candidateCount < 2)
                        throw new ArgumentException("An ambiguous section cannot select or combine candidate occurrences.");
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status));
            }
        }
    }

    internal sealed class OverlayPackedReadPolicy
    {
        public OverlayPackedReadPolicy(
            PackedSectionDecodePolicy packedPolicy,
            OverlayStorageProfile storageProfile = OverlayStorageProfile.OrdinaryByte512,
            OverlayReadLimits limits = null)
        {
            PackedPolicy = packedPolicy ?? throw new ArgumentNullException(nameof(packedPolicy));
            OverlayStorageProfiles.Validate(storageProfile, nameof(storageProfile));
            StorageProfile = storageProfile;
            Limits = limits ?? new OverlayReadLimits();
        }

        public PackedSectionDecodePolicy PackedPolicy { get; }
        public OverlayStorageProfile StorageProfile { get; }
        public OverlayReadLimits Limits { get; }
    }

    internal sealed class OverlayArrayRaw
    {
        private readonly byte[] bytes;

        internal OverlayArrayRaw(
            OverlaySectionKind sectionKind,
            OverlayStorageProfile storageProfile,
            int expectedLength,
            long declaredAggregateOutputLength,
            byte[] rawBytes,
            PackedSectionDecodeResult packed,
            IEnumerable<IniSourceProvenance> provenance)
        {
            OverlayStorageProfiles.Validate(sectionKind, nameof(sectionKind));
            OverlayStorageProfiles.Validate(storageProfile, nameof(storageProfile));
            if (expectedLength < 0 || declaredAggregateOutputLength < 0) throw new ArgumentOutOfRangeException();
            bytes = (byte[])(rawBytes ?? throw new ArgumentNullException(nameof(rawBytes))).Clone();
            if (bytes.Length != expectedLength) throw new ArgumentException("Overlay raw bytes must satisfy the selected exact-length profile.", nameof(rawBytes));
            SectionKind = sectionKind;
            StorageProfile = storageProfile;
            ExpectedLength = expectedLength;
            DeclaredAggregateOutputLength = declaredAggregateOutputLength;
            ActualLength = bytes.Length;
            Packed = packed ?? throw new ArgumentNullException(nameof(packed));
            IniSourceProvenance[] chain = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null))
                throw new ArgumentException("Raw Overlay provenance is required.", nameof(provenance));
            Provenance = Array.AsReadOnly(chain);
        }

        public OverlaySectionKind SectionKind { get; }
        public OverlayStorageProfile StorageProfile { get; }
        public int ExpectedLength { get; }
        public long DeclaredAggregateOutputLength { get; }
        public int ActualLength { get; }
        public PackedSectionDecodeResult Packed { get; }
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }

        public byte[] GetBytesCopy()
        {
            return (byte[])bytes.Clone();
        }

        public byte GetByteAt(int index)
        {
            if (index < 0 || index >= bytes.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return bytes[index];
        }
    }

    internal sealed class OverlayArrayReadResult
    {
        internal OverlayArrayReadResult(
            OverlaySectionInput input,
            PackedSectionDecodeResult packed,
            OverlayArrayRaw raw,
            IEnumerable<OverlayDiagnostic> diagnostics,
            OverlayExecutionState execution = null)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Packed = packed;
            Raw = raw;
            OverlayDiagnostic[] diagnosticArray = (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            Diagnostics = Array.AsReadOnly(diagnosticArray);
            Execution = execution ?? new OverlayExecutionState();
            foreach (OverlayDiagnostic diagnostic in diagnosticArray) Execution.Observe(diagnostic.Severity);
        }

        public OverlaySectionInput Input { get; }
        public PackedSectionDecodeResult Packed { get; }
        public OverlayArrayRaw Raw { get; }
        public IReadOnlyList<OverlayDiagnostic> Diagnostics { get; }
        public OverlayExecutionState Execution { get; }
        public OverlayCompletionStatus CompletionStatus => Execution.CompletionStatus;
        public bool HasFatalError => Execution.HasFatalError;
        public BinaryDiagnosticSeverity HighestObservedSeverity => Execution.HighestObservedSeverity;
        public int SuppressedDiagnosticCount => Execution.SuppressedDiagnosticCount;
        public bool IsSuccess => CompletionStatus == OverlayCompletionStatus.Succeeded && Raw != null && Packed != null && Packed.IsSuccess;
    }

    internal sealed class OverlayPackedDocumentReadResult
    {
        internal OverlayPackedDocumentReadResult(
            OverlayArrayReadResult overlayPack,
            OverlayArrayReadResult overlayDataPack,
            IEnumerable<OverlayDiagnostic> diagnostics,
            OverlayExecutionState execution = null)
        {
            OverlayPack = overlayPack;
            OverlayDataPack = overlayDataPack;
            OverlayDiagnostic[] diagnosticArray = (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            Diagnostics = Array.AsReadOnly(diagnosticArray);
            Execution = execution ?? new OverlayExecutionState();
            foreach (OverlayDiagnostic diagnostic in diagnosticArray) Execution.Observe(diagnostic.Severity);
        }

        public OverlayArrayReadResult OverlayPack { get; }
        public OverlayArrayReadResult OverlayDataPack { get; }
        public IReadOnlyList<OverlayDiagnostic> Diagnostics { get; }
        public OverlayExecutionState Execution { get; }
        public OverlayCompletionStatus CompletionStatus => Execution.CompletionStatus;
        public bool HasFatalError => Execution.HasFatalError;
        public BinaryDiagnosticSeverity HighestObservedSeverity => Execution.HighestObservedSeverity;
        public int SuppressedDiagnosticCount => Execution.SuppressedDiagnosticCount;
        public bool IsSuccess => CompletionStatus == OverlayCompletionStatus.Succeeded &&
            OverlayPack != null && OverlayPack.IsSuccess && OverlayDataPack != null && OverlayDataPack.IsSuccess;

        public OverlayRawIndexedView CreateIndexedView()
        {
            return new OverlayRawIndexedView(OverlayPack == null ? null : OverlayPack.Raw, OverlayDataPack == null ? null : OverlayDataPack.Raw);
        }
    }

    internal readonly struct OverlayStorageCoordinate : IEquatable<OverlayStorageCoordinate>
    {
        internal OverlayStorageCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
        public bool Equals(OverlayStorageCoordinate other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is OverlayStorageCoordinate && Equals((OverlayStorageCoordinate)obj);
        public override int GetHashCode() => unchecked((X * 397) ^ Y);
    }

    internal sealed class OverlayStorageIndexResult
    {
        internal OverlayStorageIndexResult(
            OverlayStorageCoordinate coordinate,
            OverlayStorageCoordinateIndexProfile profile,
            int? elementIndex,
            IEnumerable<OverlayDiagnostic> diagnostics,
            OverlayExecutionState execution = null)
        {
            Coordinate = coordinate;
            Profile = profile;
            ElementIndex = elementIndex;
            OverlayDiagnostic[] diagnosticArray = (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
            Diagnostics = Array.AsReadOnly(diagnosticArray);
            Execution = execution ?? new OverlayExecutionState();
            foreach (OverlayDiagnostic diagnostic in diagnosticArray) Execution.Observe(diagnostic.Severity);
        }

        public OverlayStorageCoordinate Coordinate { get; }
        public OverlayStorageCoordinateIndexProfile Profile { get; }
        public int? ElementIndex { get; }
        public IReadOnlyList<OverlayDiagnostic> Diagnostics { get; }
        public OverlayExecutionState Execution { get; }
        public bool IsSuccess => Execution.CompletionStatus == OverlayCompletionStatus.Succeeded && ElementIndex.HasValue;
    }

    internal readonly struct OverlayRawCellPair
    {
        internal OverlayRawCellPair(byte typeRaw, byte dataRaw)
        {
            TypeRaw = typeRaw;
            DataRaw = dataRaw;
        }

        public byte TypeRaw { get; }
        public byte DataRaw { get; }
    }

    internal sealed class OverlayRawIndexedView
    {
        private readonly OverlayArrayRaw overlayPack;
        private readonly OverlayArrayRaw overlayDataPack;

        internal OverlayRawIndexedView(OverlayArrayRaw overlayPack, OverlayArrayRaw overlayDataPack)
        {
            this.overlayPack = overlayPack;
            this.overlayDataPack = overlayDataPack;
        }

        public bool TryGetTypeByteAtIndex(int index, out byte value)
        {
            value = 0;
            if (overlayPack == null || index < 0 || index >= overlayPack.ActualLength) return false;
            value = overlayPack.GetByteAt(index);
            return true;
        }

        public bool TryGetDataByteAtIndex(int index, out byte value)
        {
            value = 0;
            if (overlayDataPack == null || index < 0 || index >= overlayDataPack.ActualLength) return false;
            value = overlayDataPack.GetByteAt(index);
            return true;
        }

        public bool TryGetPairAtIndex(int index, out OverlayRawCellPair pair)
        {
            pair = default(OverlayRawCellPair);
            byte type;
            byte data;
            if (!TryGetTypeByteAtIndex(index, out type) || !TryGetDataByteAtIndex(index, out data)) return false;
            pair = new OverlayRawCellPair(type, data);
            return true;
        }
    }
}
