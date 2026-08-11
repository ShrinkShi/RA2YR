using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Core.Formats.PackedMap
{
    internal enum PreviewDiagnosticCode
    {
        MissingSection,
        PresentEmptySection,
        AmbiguousSectionOccurrence,
        InvalidSelection,
        SectionNameMismatch,
        DuplicatePreviewSection,
        MissingSize,
        DuplicateSizeOccurrence,
        MalformedSize,
        SizeFieldParseFailure,
        SizeFieldOverflow,
        InvalidDimension,
        DimensionBudgetExceeded,
        PixelCountBudgetExceeded,
        ArithmeticOverflow,
        OccurrenceBudgetExceeded,
        FragmentBudgetExceeded,
        PackedStageFailure,
        WrongCodec,
        BackendUnavailable,
        Base64Failure,
        ChunkFailure,
        DecodeFailure,
        LengthUnderflow,
        LengthOverflow,
        DecodedOutputBudgetExceeded,
        UnknownMetadataProfile,
        UnknownChannelProfile,
        UnknownRowProfile,
        UnknownLengthPolicy,
        InvalidPixelCoordinate,
        Cancellation,
        NoProgress,
        PayloadMissing,
        MetadataMissing
    }

    internal enum PreviewCompletionStatus
    {
        NotRun,
        Succeeded,
        Failed
    }

    internal sealed class PreviewExecutionState
    {
        private bool hasExecuted;
        private bool hasFatalError;
        private BinaryDiagnosticSeverity highestSeverity = BinaryDiagnosticSeverity.Info;
        private int suppressedDiagnosticCount;

        public PreviewCompletionStatus CompletionStatus => !hasExecuted
            ? PreviewCompletionStatus.NotRun
            : hasFatalError ? PreviewCompletionStatus.Failed : PreviewCompletionStatus.Succeeded;

        public bool HasFatalError => hasFatalError;
        public BinaryDiagnosticSeverity HighestObservedSeverity => highestSeverity;
        public int SuppressedDiagnosticCount => suppressedDiagnosticCount;

        internal void MarkExecuted()
        {
            hasExecuted = true;
        }

        internal void Observe(BinaryDiagnosticSeverity severity)
        {
            hasExecuted = true;
            if ((int)severity > (int)highestSeverity)
                highestSeverity = severity;
            if (severity == BinaryDiagnosticSeverity.Error)
                hasFatalError = true;
        }

        internal void ObserveFailure()
        {
            hasExecuted = true;
            hasFatalError = true;
            if ((int)highestSeverity < (int)BinaryDiagnosticSeverity.Error)
                highestSeverity = BinaryDiagnosticSeverity.Error;
        }

        internal void Merge(PackedSectionDecodeResult packed, bool required)
        {
            if (packed == null)
            {
                if (required) ObserveFailure();
                return;
            }

            MarkExecuted();
            foreach (PackedMapDiagnostic diagnostic in packed.Diagnostics)
                Observe(diagnostic.Severity);
            if (!packed.IsSuccess && required)
                ObserveFailure();
        }

        internal void Merge(PreviewExecutionState child, bool required)
        {
            if (child == null || child.CompletionStatus == PreviewCompletionStatus.NotRun)
            {
                if (required) ObserveFailure();
                return;
            }

            MarkExecuted();
            Observe(child.HighestObservedSeverity);
            if (child.HasFatalError && required)
                ObserveFailure();
            AddSuppressed(child.SuppressedDiagnosticCount);
        }

        internal void SuppressOne()
        {
            MarkExecuted();
            AddSuppressed(1);
        }

        internal void Suppress(int count)
        {
            if (count <= 0) return;
            MarkExecuted();
            AddSuppressed(count);
        }

        private void AddSuppressed(int count)
        {
            if (count <= 0 || suppressedDiagnosticCount == int.MaxValue)
                return;
            long total = (long)suppressedDiagnosticCount + count;
            suppressedDiagnosticCount = total >= int.MaxValue ? int.MaxValue : (int)total;
        }
    }

    internal sealed class PreviewDiagnostic
    {
        internal PreviewDiagnostic(
            BinaryDiagnosticSeverity severity,
            PreviewDiagnosticCode code,
            BinarySourceContext source,
            IEnumerable<IniSourceProvenance> provenance,
            long offset,
            int occurrenceOrdinal,
            string stage,
            string message)
        {
            Severity = severity;
            Code = code;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            IniSourceProvenance[] chain = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null))
                throw new ArgumentException("Preview provenance is required.", nameof(provenance));
            Provenance = Array.AsReadOnly(chain);
            Offset = offset;
            OccurrenceOrdinal = occurrenceOrdinal;
            Stage = BinaryDiagnosticLabel.Validate(stage, nameof(stage));
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public BinaryDiagnosticSeverity Severity { get; }
        public PreviewDiagnosticCode Code { get; }
        public BinarySourceContext Source { get; }
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }
        public long Offset { get; }
        public int OccurrenceOrdinal { get; }
        public string Stage { get; }
        public string Message { get; }
    }

    internal enum PreviewSectionSelectionStatus
    {
        Missing,
        PresentEmpty,
        SelectedOccurrence,
        AmbiguousMultipleOccurrences,
        InvalidSelection
    }

    internal sealed class PreviewPackSectionInput
    {
        internal PreviewPackSectionInput(
            string sectionName,
            PreviewSectionSelectionStatus selectionStatus,
            int selectedSectionOccurrenceOrdinal,
            int candidateSectionOccurrenceCount,
            IEnumerable<PackedIniFragmentOccurrence> occurrences,
            BinarySourceContext source,
            IEnumerable<IniSourceProvenance> provenance)
        {
            SectionName = BinaryDiagnosticLabel.Validate(sectionName, nameof(sectionName));
            if (selectedSectionOccurrenceOrdinal < -1 || candidateSectionOccurrenceCount < 0)
                throw new ArgumentOutOfRangeException();
            SelectionStatus = selectionStatus;
            SelectedSectionOccurrenceOrdinal = selectedSectionOccurrenceOrdinal;
            CandidateSectionOccurrenceCount = candidateSectionOccurrenceCount;
            Occurrences = occurrences ?? throw new ArgumentNullException(nameof(occurrences));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            IniSourceProvenance[] chain = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null))
                throw new ArgumentException("Preview section provenance is required.", nameof(provenance));
            Provenance = Array.AsReadOnly(chain);
        }

        public string SectionName { get; }
        public PreviewSectionSelectionStatus SelectionStatus { get; }
        public int SelectedSectionOccurrenceOrdinal { get; }
        public int CandidateSectionOccurrenceCount { get; }
        public IEnumerable<PackedIniFragmentOccurrence> Occurrences { get; }
        public BinarySourceContext Source { get; }
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }
    }

    internal sealed class PreviewMetadataSectionOccurrence
    {
        internal PreviewMetadataSectionOccurrence(
            int sectionOccurrenceOrdinal,
            IEnumerable<PreviewSizeOccurrence> sizeOccurrences,
            BinarySourceContext source,
            IniSourceProvenance provenance)
        {
            if (sectionOccurrenceOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(sectionOccurrenceOrdinal));
            SectionOccurrenceOrdinal = sectionOccurrenceOrdinal;
            SizeOccurrences = sizeOccurrences ?? throw new ArgumentNullException(nameof(sizeOccurrences));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
        }

        public int SectionOccurrenceOrdinal { get; }
        public IEnumerable<PreviewSizeOccurrence> SizeOccurrences { get; }
        public BinarySourceContext Source { get; }
        public IniSourceProvenance Provenance { get; }
    }

    internal sealed class PreviewSizeOccurrence
    {
        internal PreviewSizeOccurrence(string rawValue, int physicalLineId, IniSourceProvenance provenance)
        {
            RawValue = rawValue ?? throw new ArgumentNullException(nameof(rawValue));
            if (physicalLineId < 0) throw new ArgumentOutOfRangeException(nameof(physicalLineId));
            PhysicalLineId = physicalLineId;
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
        }

        public string RawValue { get; }
        public int PhysicalLineId { get; }
        public IniSourceProvenance Provenance { get; }
    }

    internal enum PreviewMetadataInterpretationProfile
    {
        Fields23Dimensions
    }

    internal enum PreviewChannelProfile
    {
        RawUnknown,
        RGB,
        BGR
    }

    internal enum PreviewRowOrderProfile
    {
        Unknown,
        RowMajorTopDown,
        RowMajorBottomUp
    }

    internal enum PreviewLengthPolicy
    {
        ExactThreeComponents
    }

    internal sealed class PreviewReadLimits
    {
        public PreviewReadLimits(
            int maxMetadataSections = 64,
            int maxSizeOccurrences = 64,
            int maxPreviewPackSections = 64,
            int maxFragments = 4096,
            long maxFragmentCharacters = 4 * 1024 * 1024,
            long maxCompressedBytes = 64 * 1024 * 1024,
            int maxChunks = 4096,
            long maxDecodedBytes = 64 * 1024 * 1024,
            int maxWidth = 16384,
            int maxHeight = 16384,
            long maxPixels = 64L * 1024 * 1024,
            int maxDiagnostics = 4096)
        {
            if (maxMetadataSections < 0 || maxSizeOccurrences < 0 || maxPreviewPackSections < 0 ||
                maxFragments < 0 || maxFragmentCharacters < 0 || maxCompressedBytes < 0 ||
                maxChunks < 0 || maxDecodedBytes < 0 || maxWidth < 0 || maxHeight < 0 ||
                maxPixels < 0 || maxDiagnostics < 0)
                throw new ArgumentOutOfRangeException();
            MaxMetadataSections = maxMetadataSections;
            MaxSizeOccurrences = maxSizeOccurrences;
            MaxPreviewPackSections = maxPreviewPackSections;
            MaxFragments = maxFragments;
            MaxFragmentCharacters = maxFragmentCharacters;
            MaxCompressedBytes = maxCompressedBytes;
            MaxChunks = maxChunks;
            MaxDecodedBytes = maxDecodedBytes;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            MaxPixels = maxPixels;
            MaxDiagnostics = maxDiagnostics;
        }

        public int MaxMetadataSections { get; }
        public int MaxSizeOccurrences { get; }
        public int MaxPreviewPackSections { get; }
        public int MaxFragments { get; }
        public long MaxFragmentCharacters { get; }
        public long MaxCompressedBytes { get; }
        public int MaxChunks { get; }
        public long MaxDecodedBytes { get; }
        public int MaxWidth { get; }
        public int MaxHeight { get; }
        public long MaxPixels { get; }
        public int MaxDiagnostics { get; }
    }

    internal sealed class PreviewMetadataReadPolicy
    {
        public PreviewMetadataReadPolicy(
            PreviewSectionSelectionStatus selectionStatus = PreviewSectionSelectionStatus.SelectedOccurrence,
            int selectedSectionOccurrenceOrdinal = 0,
            PreviewMetadataInterpretationProfile profile = PreviewMetadataInterpretationProfile.Fields23Dimensions,
            PreviewReadLimits limits = null)
        {
            SelectionStatus = selectionStatus;
            SelectedSectionOccurrenceOrdinal = selectedSectionOccurrenceOrdinal;
            Profile = profile;
            Limits = limits ?? new PreviewReadLimits();
        }

        public PreviewSectionSelectionStatus SelectionStatus { get; }
        public int SelectedSectionOccurrenceOrdinal { get; }
        public PreviewMetadataInterpretationProfile Profile { get; }
        public PreviewReadLimits Limits { get; }
    }

    internal sealed class PreviewSizeRaw
    {
        internal PreviewSizeRaw(
            IEnumerable<string> rawTokens,
            int? field0,
            int? field1,
            int? field2,
            int? field3,
            int physicalLineId,
            IniSourceProvenance provenance)
        {
            RawTokens = Array.AsReadOnly((rawTokens ?? throw new ArgumentNullException(nameof(rawTokens))).ToArray());
            Field0Raw = field0;
            Field1Raw = field1;
            Field2Raw = field2;
            Field3Raw = field3;
            PhysicalLineId = physicalLineId;
            Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
        }

        public IReadOnlyList<string> RawTokens { get; }
        public int? Field0Raw { get; }
        public int? Field1Raw { get; }
        public int? Field2Raw { get; }
        public int? Field3Raw { get; }
        public int PhysicalLineId { get; }
        public IniSourceProvenance Provenance { get; }
        public bool IsFourFieldParse => RawTokens.Count == 4 && Field0Raw.HasValue && Field1Raw.HasValue && Field2Raw.HasValue && Field3Raw.HasValue;
    }

    internal sealed class PreviewMetadataReadResult
    {
        internal PreviewMetadataReadResult(
            PreviewMetadataSectionOccurrence selectedSection,
            PreviewSizeRaw size,
            IEnumerable<PreviewSizeRaw> allSizes,
            IEnumerable<PreviewDiagnostic> diagnostics,
            PreviewExecutionState execution)
        {
            SelectedSection = selectedSection;
            Size = size;
            AllSizes = Array.AsReadOnly((allSizes ?? throw new ArgumentNullException(nameof(allSizes))).ToArray());
            Diagnostics = Array.AsReadOnly((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
            Execution = execution ?? throw new ArgumentNullException(nameof(execution));
        }

        public PreviewMetadataSectionOccurrence SelectedSection { get; }
        public PreviewSizeRaw Size { get; }
        public IReadOnlyList<PreviewSizeRaw> AllSizes { get; }
        public IReadOnlyList<PreviewDiagnostic> Diagnostics { get; }
        public PreviewExecutionState Execution { get; }
        public PreviewCompletionStatus CompletionStatus => Execution.CompletionStatus;
        public bool HasFatalError => Execution.HasFatalError;
        public int SuppressedDiagnosticCount => Execution.SuppressedDiagnosticCount;
        public bool IsSuccess => CompletionStatus == PreviewCompletionStatus.Succeeded && Size != null;
    }

    internal sealed class PreviewMetadataRaw
    {
        internal PreviewMetadataRaw(
            PreviewMetadataSectionOccurrence section,
            IEnumerable<PreviewSizeOccurrence> sizeOccurrences)
        {
            Section = section ?? throw new ArgumentNullException(nameof(section));
            SizeOccurrences = Array.AsReadOnly((sizeOccurrences ?? throw new ArgumentNullException(nameof(sizeOccurrences))).ToArray());
        }

        public PreviewMetadataSectionOccurrence Section { get; }
        public IReadOnlyList<PreviewSizeOccurrence> SizeOccurrences { get; }
    }

    internal enum PreviewLengthStatus
    {
        Exact,
        Underflow,
        Overflow,
        MetadataInvalid,
        BudgetExceeded,
        ArithmeticOverflow,
        CodecFailure,
        MissingPayload
    }

    internal sealed class PreviewDecodedStream
    {
        private readonly byte[] bytes;

        internal PreviewDecodedStream(
            byte[] decodedBytes,
            long expectedLength,
            long actualLength,
            PreviewLengthStatus lengthStatus,
            PackedSectionDecodeResult packed,
            IEnumerable<IniSourceProvenance> provenance)
        {
            bytes = decodedBytes == null ? null : (byte[])decodedBytes.Clone();
            ExpectedLength = expectedLength;
            ActualLength = actualLength;
            LengthStatus = lengthStatus;
            Packed = packed;
            IniSourceProvenance[] chain = (provenance ?? throw new ArgumentNullException(nameof(provenance))).ToArray();
            if (chain.Length == 0 || chain.Any(item => item == null))
                throw new ArgumentException("Decoded stream provenance is required.", nameof(provenance));
            Provenance = Array.AsReadOnly(chain);
            Sha256 = bytes == null ? null : ComputeSha256(bytes);
        }

        public long ExpectedLength { get; }
        public long ActualLength { get; }
        public PreviewLengthStatus LengthStatus { get; }
        public PackedSectionDecodeResult Packed { get; }
        public IReadOnlyList<IniSourceProvenance> Provenance { get; }
        public string Sha256 { get; }
        public bool IsExact => LengthStatus == PreviewLengthStatus.Exact && bytes != null;

        public byte[] GetBytesCopy() => bytes == null ? null : (byte[])bytes.Clone();

        internal byte GetByteAt(long offset)
        {
            if (bytes == null || offset < 0 || offset >= bytes.LongLength)
                throw new ArgumentOutOfRangeException(nameof(offset));
            return bytes[(int)offset];
        }

        private static string ComputeSha256(byte[] value)
        {
            using (SHA256 sha256 = SHA256.Create())
                return BitConverter.ToString(sha256.ComputeHash(value)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }

    internal readonly struct PreviewPixelRaw
    {
        internal PreviewPixelRaw(byte component0Raw, byte component1Raw, byte component2Raw)
        {
            Component0Raw = component0Raw;
            Component1Raw = component1Raw;
            Component2Raw = component2Raw;
        }

        public byte Component0Raw { get; }
        public byte Component1Raw { get; }
        public byte Component2Raw { get; }
    }

    internal readonly struct PreviewPixelSemantic
    {
        internal PreviewPixelSemantic(byte first, byte second, byte third, PreviewChannelProfile profile)
        {
            First = first;
            Second = second;
            Third = third;
            Profile = profile;
        }

        public byte First { get; }
        public byte Second { get; }
        public byte Third { get; }
        public PreviewChannelProfile Profile { get; }
    }

    internal sealed class PreviewPixelLayoutView
    {
        private readonly PreviewDecodedStream decoded;

        internal PreviewPixelLayoutView(
            PreviewDecodedStream decoded,
            int width,
            int height,
            PreviewChannelProfile channelProfile,
            PreviewRowOrderProfile rowOrderProfile)
        {
            decoded = decoded ?? throw new ArgumentNullException(nameof(decoded));
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
            if (channelProfile != PreviewChannelProfile.RawUnknown && channelProfile != PreviewChannelProfile.RGB && channelProfile != PreviewChannelProfile.BGR)
                throw new ArgumentOutOfRangeException(nameof(channelProfile));
            if (rowOrderProfile != PreviewRowOrderProfile.Unknown && rowOrderProfile != PreviewRowOrderProfile.RowMajorTopDown && rowOrderProfile != PreviewRowOrderProfile.RowMajorBottomUp)
                throw new ArgumentOutOfRangeException(nameof(rowOrderProfile));
            Width = width;
            Height = height;
            ChannelProfile = channelProfile;
            RowOrderProfile = rowOrderProfile;
            this.decoded = decoded;
            _ = checked((long)width * height * 3L);
        }

        public int Width { get; }
        public int Height { get; }
        public PreviewChannelProfile ChannelProfile { get; }
        public PreviewRowOrderProfile RowOrderProfile { get; }

        public PreviewPixelRaw GetRawPixel(int x, int y)
        {
            long offset = GetOffset(x, y);
            return new PreviewPixelRaw(decoded.GetByteAt(offset), decoded.GetByteAt(checked(offset + 1)), decoded.GetByteAt(checked(offset + 2)));
        }

        public PreviewPixelSemantic GetSemanticPixel(int x, int y)
        {
            if (ChannelProfile == PreviewChannelProfile.RawUnknown)
                throw new InvalidOperationException("A channel profile must be selected before semantic access.");
            PreviewPixelRaw raw = GetRawPixel(x, y);
            return ChannelProfile == PreviewChannelProfile.RGB
                ? new PreviewPixelSemantic(raw.Component0Raw, raw.Component1Raw, raw.Component2Raw, ChannelProfile)
                : new PreviewPixelSemantic(raw.Component2Raw, raw.Component1Raw, raw.Component0Raw, ChannelProfile);
        }

        private long GetOffset(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException();
            if (RowOrderProfile == PreviewRowOrderProfile.Unknown)
                throw new InvalidOperationException("A row-order profile must be selected before coordinate access.");
            int sourceY = RowOrderProfile == PreviewRowOrderProfile.RowMajorTopDown ? y : checked(Height - 1 - y);
            long pixelIndex = checked((long)sourceY * Width + x);
            return checked(pixelIndex * 3L);
        }
    }

    internal sealed class PreviewPackReadPolicy
    {
        public PreviewPackReadPolicy(
            PackedSectionDecodePolicy packedPolicy,
            PreviewMetadataInterpretationProfile metadataProfile = PreviewMetadataInterpretationProfile.Fields23Dimensions,
            PreviewChannelProfile channelProfile = PreviewChannelProfile.RawUnknown,
            PreviewRowOrderProfile rowOrderProfile = PreviewRowOrderProfile.Unknown,
            PreviewLengthPolicy lengthPolicy = PreviewLengthPolicy.ExactThreeComponents,
            PreviewReadLimits limits = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            PackedPolicy = packedPolicy ?? throw new ArgumentNullException(nameof(packedPolicy));
            MetadataProfile = metadataProfile;
            ChannelProfile = channelProfile;
            RowOrderProfile = rowOrderProfile;
            LengthPolicy = lengthPolicy;
            Limits = limits ?? new PreviewReadLimits();
            CancellationToken = cancellationToken;
        }

        public PackedSectionDecodePolicy PackedPolicy { get; }
        public PreviewMetadataInterpretationProfile MetadataProfile { get; }
        public PreviewChannelProfile ChannelProfile { get; }
        public PreviewRowOrderProfile RowOrderProfile { get; }
        public PreviewLengthPolicy LengthPolicy { get; }
        public PreviewReadLimits Limits { get; }
        public CancellationToken CancellationToken { get; }

        internal bool TryValidate(out PreviewDiagnosticCode code, out string message)
        {
            code = PreviewDiagnosticCode.InvalidSelection;
            message = null;
            PackedMapDiagnostic packedDiagnostic;
            if (PackedPolicy == null || !PackedPolicy.TryValidate(out packedDiagnostic))
            {
                code = PreviewDiagnosticCode.PackedStageFailure;
                message = "The nested packed policy is invalid.";
                return false;
            }
            if (MetadataProfile != PreviewMetadataInterpretationProfile.Fields23Dimensions)
            {
                code = PreviewDiagnosticCode.UnknownMetadataProfile;
                message = "The Preview metadata interpretation profile is unknown.";
                return false;
            }
            if (ChannelProfile != PreviewChannelProfile.RawUnknown && ChannelProfile != PreviewChannelProfile.RGB && ChannelProfile != PreviewChannelProfile.BGR)
            {
                code = PreviewDiagnosticCode.UnknownChannelProfile;
                message = "The Preview channel profile is unknown.";
                return false;
            }
            if (RowOrderProfile != PreviewRowOrderProfile.Unknown && RowOrderProfile != PreviewRowOrderProfile.RowMajorTopDown && RowOrderProfile != PreviewRowOrderProfile.RowMajorBottomUp)
            {
                code = PreviewDiagnosticCode.UnknownRowProfile;
                message = "The Preview row-order profile is unknown.";
                return false;
            }
            if (LengthPolicy != PreviewLengthPolicy.ExactThreeComponents)
            {
                code = PreviewDiagnosticCode.UnknownLengthPolicy;
                message = "The Preview length policy is unknown.";
                return false;
            }
            return true;
        }
    }

    internal sealed class PreviewPackReadResult
    {
        internal PreviewPackReadResult(
            PreviewMetadataReadResult metadata,
            PreviewPackSectionInput input,
            PackedSectionDecodeResult packed,
            PreviewDecodedStream decoded,
            PreviewPixelLayoutView layout,
            IEnumerable<PreviewDiagnostic> diagnostics,
            PreviewExecutionState execution)
        {
            Metadata = metadata;
            Input = input;
            Packed = packed;
            Decoded = decoded;
            Layout = layout;
            Diagnostics = Array.AsReadOnly((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
            Execution = execution ?? throw new ArgumentNullException(nameof(execution));
        }

        public PreviewMetadataReadResult Metadata { get; }
        public PreviewPackSectionInput Input { get; }
        public PackedSectionDecodeResult Packed { get; }
        public PreviewDecodedStream Decoded { get; }
        public PreviewPixelLayoutView Layout { get; }
        public IReadOnlyList<PreviewDiagnostic> Diagnostics { get; }
        public PreviewExecutionState Execution { get; }
        public PreviewCompletionStatus CompletionStatus => Execution.CompletionStatus;
        public bool HasFatalError => Execution.HasFatalError;
        public BinaryDiagnosticSeverity HighestObservedSeverity => Execution.HighestObservedSeverity;
        public int SuppressedDiagnosticCount => Execution.SuppressedDiagnosticCount;
        public bool IsSuccess => CompletionStatus == PreviewCompletionStatus.Succeeded && Decoded != null && Decoded.IsExact;
    }
}
