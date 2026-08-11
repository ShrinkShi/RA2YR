using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.PackedMap;

namespace RA2YR.Tests.EditMode.Formats.PackedMap
{
    public sealed class PreviewPackTests
    {
        [Test]
        public void MetadataPreservesFourRawFieldsAndUsesFields23Dimensions()
        {
            PreviewMetadataReadResult result = ReadMetadata("7,-3,2,1");
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Size.Field0Raw, Is.EqualTo(7));
            Assert.That(result.Size.Field1Raw, Is.EqualTo(-3));
            Assert.That(result.Size.Field2Raw, Is.EqualTo(2));
            Assert.That(result.Size.Field3Raw, Is.EqualTo(1));
        }

        [Test]
        public void MetadataRejectsMalformedFieldCount()
        {
            PreviewMetadataReadResult result = ReadMetadata("0,0,2");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.MalformedSize), Is.True);
        }

        [Test]
        public void MetadataRejectsNegativeWidthWithoutAbsoluteValueRepair()
        {
            PreviewMetadataReadResult result = ReadMetadata("0,0,-2,1");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Size.Field2Raw, Is.EqualTo(-2));
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.InvalidDimension), Is.True);
        }

        [Test]
        public void MetadataRejectsIntegerOverflowStructurally()
        {
            PreviewMetadataReadResult result = ReadMetadata("0,0,999999999999999999999,1");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.SizeFieldOverflow), Is.True);
        }

        [Test]
        public void MetadataRejectsDuplicateSizeWithoutWinner()
        {
            PreviewMetadataSectionOccurrence section = MetadataSection(
                new[] { new PreviewSizeOccurrence("0,0,1,1", 2, Provenance()), new PreviewSizeOccurrence("0,0,2,1", 3, Provenance()) });
            PreviewMetadataReadResult result = new PreviewMetadataReader().Read(new[] { section }, new PreviewMetadataReadPolicy());
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.DuplicateSizeOccurrence), Is.True);
        }

        [Test]
        public void MetadataRequiresExplicitSelectionForAmbiguousPreviewSections()
        {
            PreviewMetadataSectionOccurrence[] sections = new[] { MetadataSection("0,0,1,1", 1), MetadataSection("0,0,1,1", 2) };
            PreviewMetadataReadResult ambiguous = new PreviewMetadataReader().Read(sections, new PreviewMetadataReadPolicy(PreviewSectionSelectionStatus.AmbiguousMultipleOccurrences));
            Assert.That(ambiguous.IsSuccess, Is.False);
            PreviewMetadataReadResult selected = new PreviewMetadataReader().Read(sections, new PreviewMetadataReadPolicy(PreviewSectionSelectionStatus.SelectedOccurrence, 1));
            Assert.That(selected.IsSuccess, Is.True);
            Assert.That(selected.SelectedSection.SectionOccurrenceOrdinal, Is.EqualTo(1));
        }

        [Test]
        public void MetadataInvalidPolicyDoesNotConsumeLazySource()
        {
            int moves = 0;
            IEnumerable<PreviewMetadataSectionOccurrence> Lazy()
            {
                moves++;
                throw new InvalidOperationException("must not enumerate");
#pragma warning disable 162
                yield return MetadataSection("0,0,1,1", 0);
#pragma warning restore 162
            }

            PreviewMetadataReadResult result = new PreviewMetadataReader().Read(
                Lazy(),
                new PreviewMetadataReadPolicy(PreviewSectionSelectionStatus.SelectedOccurrence, 0, (PreviewMetadataInterpretationProfile)99));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(moves, Is.EqualTo(0));
        }

        [Test]
        public void PreviewPackMissingSectionFailsClosed()
        {
            PreviewPackReadResult result = ReadPack(null, new PreviewPackSectionInput("PreviewPack", PreviewSectionSelectionStatus.Missing, -1, 0, Array.Empty<PackedIniFragmentOccurrence>(), Source(), new[] { Provenance() }), new FakeBackend(new byte[] { 1, 2, 3 }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.MissingSection), Is.True);
        }

        [Test]
        public void PreviewPackRejectsAmbiguousSectionWithoutConcatenation()
        {
            PreviewPackSectionInput input = new PreviewPackSectionInput("PreviewPack", PreviewSectionSelectionStatus.AmbiguousMultipleOccurrences, -1, 2, Array.Empty<PackedIniFragmentOccurrence>(), Source(), new[] { Provenance() });
            PreviewPackReadResult result = ReadPack(ReadMetadata("0,0,1,1"), input, new FakeBackend(new byte[] { 1, 2, 3 }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Packed, Is.Null);
        }

        [Test]
        public void PreviewPackExactDecodedStreamCreatesRawLayout()
        {
            PreviewPackReadResult result = ReadPack(ReadMetadata("12,9,1,1"), SelectedInput(3), new FakeBackend(new byte[] { 1, 2, 3 }), PreviewChannelProfile.RawUnknown, PreviewRowOrderProfile.RowMajorTopDown);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Decoded.GetBytesCopy(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            PreviewPixelRaw raw = result.Layout.GetRawPixel(0, 0);
            Assert.That(raw.Component0Raw, Is.EqualTo(1));
            Assert.That(raw.Component1Raw, Is.EqualTo(2));
            Assert.That(raw.Component2Raw, Is.EqualTo(3));
            Assert.That(result.Metadata.Size.Field0Raw, Is.EqualTo(12));
        }

        [Test]
        public void PreviewPackExplicitRgbAndBgrProfilesOnlyAffectDerivedView()
        {
            PreviewPackReadResult rgb = ReadPack(ReadMetadata("0,0,1,1"), SelectedInput(3), new FakeBackend(new byte[] { 1, 2, 3 }), PreviewChannelProfile.RGB, PreviewRowOrderProfile.RowMajorTopDown);
            PreviewPackReadResult bgr = ReadPack(ReadMetadata("0,0,1,1"), SelectedInput(3), new FakeBackend(new byte[] { 1, 2, 3 }), PreviewChannelProfile.BGR, PreviewRowOrderProfile.RowMajorTopDown);
            Assert.That(rgb.Layout.GetSemanticPixel(0, 0).First, Is.EqualTo(1));
            Assert.That(bgr.Layout.GetSemanticPixel(0, 0).First, Is.EqualTo(3));
            Assert.That(rgb.Decoded.GetBytesCopy(), Is.EqualTo(bgr.Decoded.GetBytesCopy()));
        }

        [Test]
        public void PreviewPackUnknownChannelProfileDoesNotGuess()
        {
            PreviewPackReadResult result = ReadPack(ReadMetadata("0,0,1,1"), SelectedInput(3), new FakeBackend(new byte[] { 1, 2, 3 }), PreviewChannelProfile.RawUnknown, PreviewRowOrderProfile.RowMajorTopDown);
            Assert.That(() => result.Layout.GetSemanticPixel(0, 0), Throws.InvalidOperationException);
        }

        [Test]
        public void PreviewPackExplicitRowProfilesMapRowsWithoutChangingBytes()
        {
            PreviewMetadataReadResult metadata = ReadMetadata("0,0,2,2");
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            PreviewPackReadResult top = ReadPack(metadata, SelectedInput(bytes.Length), new FakeBackend(bytes), PreviewChannelProfile.RawUnknown, PreviewRowOrderProfile.RowMajorTopDown);
            PreviewPackReadResult bottom = ReadPack(metadata, SelectedInput(bytes.Length), new FakeBackend(bytes), PreviewChannelProfile.RawUnknown, PreviewRowOrderProfile.RowMajorBottomUp);
            Assert.That(top.Layout.GetRawPixel(0, 0).Component0Raw, Is.EqualTo(1));
            Assert.That(bottom.Layout.GetRawPixel(0, 0).Component0Raw, Is.EqualTo(7));
            Assert.That(top.Decoded.Sha256, Is.EqualTo(bottom.Decoded.Sha256));
        }

        [Test]
        public void PreviewPackRejectsShortDecodedOutput()
        {
            PreviewPackReadResult result = ReadPack(ReadMetadata("0,0,1,1"), SelectedInput(3), new FakeBackend(new byte[] { 1, 2 }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.LengthUnderflow), Is.True);
        }

        [Test]
        public void PreviewPackRejectsLongDecodedOutput()
        {
            PreviewPackReadResult result = ReadPack(ReadMetadata("0,0,1,1"), SelectedInput(3), new FakeBackend(new byte[] { 1, 2, 3, 4 }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.LengthOverflow), Is.True);
        }

        [Test]
        public void PreviewPackRejectsMissingBackendBeforeDecode()
        {
            PreviewPackReadResult result = ReadPack(ReadMetadata("0,0,1,1"), SelectedInput(3), null);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.BackendUnavailable), Is.True);
        }

        [Test]
        public void PreviewPackRejectsWrongCodecWithoutConsumingFragments()
        {
            int moves = 0;
            IEnumerable<PackedIniFragmentOccurrence> Lazy()
            {
                moves++;
                yield return Occurrence("1", EnvelopeBase64(3));
            }

            PreviewPackReadPolicy policy = new PreviewPackReadPolicy(
                new PackedSectionDecodePolicy(PackedIniFragmentOrderingPolicy.StrictSequentialFromOne, StrictBase64Policy.StandardAlphabetNoWhitespace, ChunkSentinelPolicy.RejectAllZero, PackedCodecKind.Format80));
            PreviewPackReadResult result = new PreviewPackSectionReader().Read(ReadMetadata("0,0,1,1"), new PreviewPackSectionInput("PreviewPack", PreviewSectionSelectionStatus.SelectedOccurrence, 0, 1, Lazy(), Source(), new[] { Provenance() }), policy, new FakeBackend(new byte[] { 1, 2, 3 }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(moves, Is.EqualTo(0));
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.WrongCodec), Is.True);
        }

        [Test]
        public void PreviewPackStopsAtFragmentBudgetWithoutUnboundedEnumeration()
        {
            int moves = 0;
            IEnumerable<PackedIniFragmentOccurrence> Lazy()
            {
                for (int index = 0; index < 10; index++)
                {
                    moves++;
                    yield return Occurrence((index + 1).ToString(), EnvelopeBase64(3));
                }
            }

            PreviewReadLimits limits = new PreviewReadLimits(maxFragments: 2);
            PreviewPackReadPolicy policy = new PreviewPackReadPolicy(
                new PackedSectionDecodePolicy(PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder, StrictBase64Policy.StandardAlphabetNoWhitespace, ChunkSentinelPolicy.RejectAllZero, PackedCodecKind.RawLzo1X), limits: limits);
            PreviewPackReadResult result = new PreviewPackSectionReader().Read(ReadMetadata("0,0,1,1"), new PreviewPackSectionInput("PreviewPack", PreviewSectionSelectionStatus.SelectedOccurrence, 0, 1, Lazy(), Source(), new[] { Provenance() }), policy, new FakeBackend(new byte[] { 1, 2, 3 }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(moves, Is.EqualTo(3));
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.OccurrenceBudgetExceeded), Is.True);
        }

        [Test]
        public void PreviewPackReturnedDecodedBytesAreImmutable()
        {
            PreviewPackReadResult result = ReadPack(ReadMetadata("0,0,1,1"), SelectedInput(3), new FakeBackend(new byte[] { 1, 2, 3 }), PreviewChannelProfile.RawUnknown, PreviewRowOrderProfile.RowMajorTopDown);
            byte[] bytes = result.Decoded.GetBytesCopy();
            bytes[0] = 99;
            Assert.That(result.Decoded.GetBytesCopy(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.Layout.GetRawPixel(0, 0).Component0Raw, Is.EqualTo(1));
        }

        [Test]
        public void PreviewPackExactLengthUsesCheckedThreeComponentContract()
        {
            PreviewPackReadResult result = ReadPack(ReadMetadata("0,0,2,1"), SelectedInput(6), new FakeBackend(new byte[] { 1, 2, 3, 4, 5, 6 }), PreviewChannelProfile.RawUnknown, PreviewRowOrderProfile.RowMajorTopDown);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Decoded.ExpectedLength, Is.EqualTo(6));
            Assert.That(result.Decoded.ActualLength, Is.EqualTo(6));
        }

        [Test]
        public void MetadataZeroDiagnosticBudgetStillFailsClosed()
        {
            var limits = new PreviewReadLimits(maxDiagnostics: 0);
            var policy = new PreviewMetadataReadPolicy(limits: limits);
            PreviewMetadataReadResult result = new PreviewMetadataReader().Read(new[] { MetadataSection("0,0,1") }, policy);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.HasFatalError, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.SuppressedDiagnosticCount, Is.GreaterThan(0));
        }

        [Test]
        public void PreviewPackZeroDiagnosticBudgetStillFailsBackendUnavailable()
        {
            var limits = new PreviewReadLimits(maxDiagnostics: 0);
            var packedPolicy = new PackedSectionDecodePolicy(
                PackedIniFragmentOrderingPolicy.StrictSequentialFromOne,
                StrictBase64Policy.StandardAlphabetNoWhitespace,
                ChunkSentinelPolicy.RejectAllZero,
                PackedCodecKind.RawLzo1X);
            var policy = new PreviewPackReadPolicy(packedPolicy, limits: limits);
            PreviewPackReadResult result = new PreviewPackSectionReader().Read(
                ReadMetadata("0,0,1,1"),
                SelectedInput(3),
                policy,
                null);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.HasFatalError, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.SuppressedDiagnosticCount, Is.GreaterThan(0));
        }

        [Test]
        public void PreviewPackCancellationStopsBeforeLazyFragmentEnumeration()
        {
            int moves = 0;
            IEnumerable<PackedIniFragmentOccurrence> Lazy()
            {
                moves++;
                yield return Occurrence("1", EnvelopeBase64(3));
            }

            var cancellation = new System.Threading.CancellationTokenSource();
            cancellation.Cancel();
            var packedPolicy = new PackedSectionDecodePolicy(
                PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder,
                StrictBase64Policy.StandardAlphabetNoWhitespace,
                ChunkSentinelPolicy.RejectAllZero,
                PackedCodecKind.RawLzo1X,
                cancellationToken: cancellation.Token);
            PreviewPackReadResult result = new PreviewPackSectionReader().Read(
                ReadMetadata("0,0,1,1"),
                new PreviewPackSectionInput("PreviewPack", PreviewSectionSelectionStatus.SelectedOccurrence, 0, 1, Lazy(), Source(), new[] { Provenance() }),
                new PreviewPackReadPolicy(packedPolicy),
                new FakeBackend(new byte[] { 1, 2, 3 }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(moves, Is.EqualTo(0));
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.Cancellation), Is.True);
        }

        [Test]
        public void PreviewPackUnknownLengthPolicyFailsBeforeFragmentEnumeration()
        {
            int moves = 0;
            IEnumerable<PackedIniFragmentOccurrence> Lazy()
            {
                moves++;
                yield return Occurrence("1", EnvelopeBase64(3));
            }

            var packedPolicy = new PackedSectionDecodePolicy(
                PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder,
                StrictBase64Policy.StandardAlphabetNoWhitespace,
                ChunkSentinelPolicy.RejectAllZero,
                PackedCodecKind.RawLzo1X);
            PreviewPackReadResult result = new PreviewPackSectionReader().Read(
                ReadMetadata("0,0,1,1"),
                new PreviewPackSectionInput("PreviewPack", PreviewSectionSelectionStatus.SelectedOccurrence, 0, 1, Lazy(), Source(), new[] { Provenance() }),
                new PreviewPackReadPolicy(packedPolicy, lengthPolicy: (PreviewLengthPolicy)99),
                new FakeBackend(new byte[] { 1, 2, 3 }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(moves, Is.EqualTo(0));
            Assert.That(result.Diagnostics.Any(item => item.Code == PreviewDiagnosticCode.UnknownLengthPolicy), Is.True);
        }

        [Test]
        public void PreviewPackRawLayoutRequiresExplicitRowOrderForCoordinateAccess()
        {
            PreviewPackReadResult result = ReadPack(ReadMetadata("0,0,1,1"), SelectedInput(3), new FakeBackend(new byte[] { 1, 2, 3 }));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(() => result.Layout.GetRawPixel(0, 0), Throws.InvalidOperationException);
        }

        private static PreviewMetadataReadResult ReadMetadata(string value)
        {
            return new PreviewMetadataReader().Read(new[] { MetadataSection(value, 0) }, new PreviewMetadataReadPolicy());
        }

        private static PreviewMetadataSectionOccurrence MetadataSection(string value, int ordinal)
        {
            return new PreviewMetadataSectionOccurrence(ordinal, new[] { new PreviewSizeOccurrence(value, ordinal + 1, Provenance()) }, Source(), Provenance());
        }

        private static PreviewMetadataSectionOccurrence MetadataSection(IEnumerable<PreviewSizeOccurrence> sizes)
        {
            return new PreviewMetadataSectionOccurrence(0, sizes, Source(), Provenance());
        }

        private static PreviewPackSectionInput SelectedInput(int outputLength)
        {
            return new PreviewPackSectionInput("PreviewPack", PreviewSectionSelectionStatus.SelectedOccurrence, 0, 1, new[] { Occurrence("1", EnvelopeBase64(outputLength)) }, Source(), new[] { Provenance() });
        }

        private static PreviewPackReadResult ReadPack(PreviewMetadataReadResult metadata, PreviewPackSectionInput input, ILzoDecodeBackend backend, PreviewChannelProfile channel = PreviewChannelProfile.RawUnknown, PreviewRowOrderProfile row = PreviewRowOrderProfile.Unknown)
        {
            var policy = new PreviewPackReadPolicy(
                new PackedSectionDecodePolicy(PackedIniFragmentOrderingPolicy.StrictSequentialFromOne, StrictBase64Policy.StandardAlphabetNoWhitespace, ChunkSentinelPolicy.RejectAllZero, PackedCodecKind.RawLzo1X),
                channelProfile: channel,
                rowOrderProfile: row);
            return new PreviewPackSectionReader().Read(metadata, input, policy, backend);
        }

        private static string EnvelopeBase64(int outputLength)
        {
            return Convert.ToBase64String(new byte[] { 1, 0, (byte)outputLength, 0, 0xA5 });
        }

        private static PackedIniFragmentOccurrence Occurrence(string key, string value)
        {
            return new PackedIniFragmentOccurrence("PreviewPack", key, value, 0, "synthetic", 0, Provenance());
        }

        private static IniSourceProvenance Provenance()
        {
            return new IniSourceProvenance("synthetic-preview", new[] { LogicalContentPath.Parse("synthetic-preview.ini") });
        }

        private static BinarySourceContext Source()
        {
            return new BinarySourceContext("preview-pack-tests", "synthetic-preview", LogicalContentPath.Parse("synthetic-preview.bin"));
        }

        private sealed class FakeBackend : ILzoDecodeBackend
        {
            private readonly byte[] output;

            public FakeBackend(byte[] output)
            {
                this.output = output == null ? null : (byte[])output.Clone();
            }

            public LzoDecodeResult Decode(LzoDecodeRequest request)
            {
                return new LzoDecodeResult(output, request.Compressed.Length, "preview-fake", Array.Empty<PackedMapDiagnostic>(), request.SourceProvenance);
            }
        }
    }
}
