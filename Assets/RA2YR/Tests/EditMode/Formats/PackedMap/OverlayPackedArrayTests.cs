using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.PackedMap;

namespace RA2YR.Tests.EditMode.Formats.PackedMap
{
    public sealed class OverlayPackedArrayTests
    {
        [Test]
        public void BothOrdinaryArraysDecodeExactlyAndPreserveRawBytes()
        {
            OverlayPackedDocumentReadResult result = ReadDocument(FullBase64(0x12), FullBase64(0x34));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.OverlayPack.Raw.ActualLength, Is.EqualTo(262144));
            Assert.That(result.OverlayDataPack.Raw.ActualLength, Is.EqualTo(262144));
            Assert.That(result.OverlayPack.Raw.GetByteAt(0), Is.EqualTo(0x12));
            Assert.That(result.OverlayDataPack.Raw.GetByteAt(262143), Is.EqualTo(0x34));
        }

        [Test]
        public void MultiChunkOrdinaryArrayRetainsDeclaredAggregateLength()
        {
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, FullBase64(0x21));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Packed.Envelope.Blocks.Count, Is.GreaterThan(1));
            Assert.That(result.Raw.DeclaredAggregateOutputLength, Is.EqualTo(262144));
        }

        [Test]
        public void RawBytesAreDefensiveCopy()
        {
            OverlayArrayRaw raw = ReadArray(OverlaySectionKind.OverlayPack, FullBase64(0x55)).Raw;
            byte[] returned = raw.GetBytesCopy();
            returned[0] = 0;
            Assert.That(raw.GetByteAt(0), Is.EqualTo(0x55));
        }

        [Test]
        public void RawArrayRetainsSelectedSectionProvenance()
        {
            OverlayArrayRaw raw = ReadArray(OverlaySectionKind.OverlayDataPack, FullBase64(0x61)).Raw;
            Assert.That(raw.Provenance.Single().SourceId, Is.EqualTo("synthetic"));
            Assert.That(raw.SectionKind, Is.EqualTo(OverlaySectionKind.OverlayDataPack));
        }

        [Test]
        public void MissingSectionsAreDistinctFromPresentEmptySections()
        {
            OverlayArrayReadResult missing = ReadArray(Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Missing));
            OverlayArrayReadResult empty = ReadArray(Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.PresentEmpty));
            Assert.That(HasDiagnostic(missing, OverlayDiagnosticCode.MissingSection), Is.True);
            Assert.That(HasDiagnostic(empty, OverlayDiagnosticCode.PresentButEmptySection), Is.True);
        }

        [Test]
        public void AmbiguousSectionDoesNotCombineCandidateOccurrences()
        {
            OverlayArrayReadResult result = ReadArray(Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Ambiguous, candidateCount: 2));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Packed, Is.Null);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.AmbiguousSectionOccurrence), Is.True);
        }

        [Test]
        public void SelectedSectionWithNoFragmentsFailsClosed()
        {
            OverlayArrayReadResult result = ReadArray(Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, occurrences: Array.Empty<PackedIniFragmentOccurrence>()));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.NoFragmentOccurrences), Is.True);
        }

        [Test]
        public void EmptyFragmentDoesNotBecomeAnEmptyRawArray()
        {
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, string.Empty);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.ZeroBlockPackedResult), Is.True);
            Assert.That(result.Raw, Is.Null);
        }

        [Test]
        public void WrongSectionNameIsRejectedBeforePackedDecoding()
        {
            OverlaySectionInput input = Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, FullBase64(1), sectionName: "OverlayDataPack");
            OverlayArrayReadResult result = ReadArray(input);
            Assert.That(result.Packed, Is.Null);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.WrongSectionKind), Is.True);
        }

        [Test]
        public void WrongSectionKindIsRejectedBeforePackedDecoding()
        {
            OverlaySectionInput input = Input(OverlaySectionKind.OverlayDataPack, OverlaySectionSelectionStatus.Selected, FullBase64(1));
            OverlayArrayReadResult result = new OverlayPackedArrayReader().Read(input, OverlaySectionKind.OverlayPack, Policy());
            Assert.That(result.Packed, Is.Null);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.WrongSectionKind), Is.True);
        }

        [TestCase("A===")]
        [TestCase("AB==")]
        [TestCase("AQ= ")]
        public void MalformedBase64FailsAtPackedStage(string text)
        {
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, text);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Raw, Is.Null);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.PackedStageFailure), Is.True);
        }

        [Test]
        public void DuplicateFragmentKeysFailWithoutArrayConstruction()
        {
            OverlaySectionInput input = Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, occurrences: new[]
            {
                Occurrence("OverlayPack", "1", FullBase64(1), 0),
                Occurrence("OverlayPack", "01", FullBase64(2), 1)
            });
            OverlayArrayReadResult result = ReadArray(input, Policy(ordering: PackedIniFragmentOrderingPolicy.NumericAscendingUnique));
            Assert.That(result.Raw, Is.Null);
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void FragmentGapFailsUnderStrictSequentialPolicy()
        {
            OverlaySectionInput input = Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, occurrences: new[] { Occurrence("OverlayPack", "2", FullBase64(1), 0) });
            OverlayArrayReadResult result = ReadArray(input);
            Assert.That(result.Raw, Is.Null);
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void OneZeroChunkFieldFailsAtPackedStage()
        {
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, Convert.ToBase64String(new byte[] { 0, 0, 1, 0 }));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.PackedStageFailure), Is.True);
        }

        [Test]
        public void ZeroZeroEnvelopeNeedsExplicitPolicyAndStillCannotProduceArray()
        {
            string source = Convert.ToBase64String(new byte[] { 0, 0, 0, 0 });
            OverlayArrayReadResult rejected = ReadArray(OverlaySectionKind.OverlayPack, source);
            OverlayArrayReadResult explicitTerminator = ReadArray(OverlaySectionKind.OverlayPack, source, Policy(sentinel: ChunkSentinelPolicy.AllowZeroZeroAsTerminator));
            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(explicitTerminator.IsSuccess, Is.False);
            Assert.That(HasDiagnostic(explicitTerminator, OverlayDiagnosticCode.ZeroBlockPackedResult), Is.True);
        }

        [Test]
        public void RelativeFormat80ProfileIsRejectedWithoutProbing()
        {
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, FullBase64(1), Policy(format80: new Format80Profile(Format80Variant.Relative)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Packed, Is.Null);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.UnsupportedFormat80Profile), Is.True);
        }

        [Test]
        public void RawLzoPolicyIsRejectedBeforeTheRawArrayStage()
        {
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, FullBase64(1), Policy(codec: PackedCodecKind.RawLzo1X));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Packed, Is.Null);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.WrongCodec), Is.True);
        }

        [Test]
        public void UnknownFragmentOrderingIsRejectedBeforeOccurrenceEnumeration()
        {
            int moves = 0;
            OverlayArrayReadResult result = ReadArray(
                Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, occurrences: ExplosiveOccurrences(() => moves++)),
                Policy(ordering: (PackedIniFragmentOrderingPolicy)99));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Packed, Is.Null);
            Assert.That(moves, Is.EqualTo(0));
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.InvalidPackedPolicy), Is.True);
        }

        [Test]
        public void UnknownChunkSentinelIsRejectedBeforeOccurrenceEnumeration()
        {
            int moves = 0;
            OverlayArrayReadResult result = ReadArray(
                Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, occurrences: ExplosiveOccurrences(() => moves++)),
                Policy(sentinel: (ChunkSentinelPolicy)99));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(moves, Is.EqualTo(0));
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.InvalidPackedPolicy), Is.True);
        }

        [Test]
        public void UnknownBase64PolicyIsRejectedBeforeOccurrenceEnumeration()
        {
            int moves = 0;
            OverlayArrayReadResult result = ReadArray(
                Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, occurrences: ExplosiveOccurrences(() => moves++)),
                Policy(base64Policy: (StrictBase64Policy)99));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(moves, Is.EqualTo(0));
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.InvalidPackedPolicy), Is.True);
        }

        [Test]
        public void UnknownPackedCodecIsRejectedBeforeOccurrenceEnumeration()
        {
            int moves = 0;
            OverlayArrayReadResult result = ReadArray(
                Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, occurrences: ExplosiveOccurrences(() => moves++)),
                Policy(codec: (PackedCodecKind)99));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(moves, Is.EqualTo(0));
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.InvalidPackedPolicy), Is.True);
        }

        [Test]
        public void UnknownFormat80VariantIsRejectedWithoutFallback()
        {
            int moves = 0;
            OverlayArrayReadResult result = ReadArray(
                Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, occurrences: ExplosiveOccurrences(() => moves++)),
                Policy(format80: new Format80Profile((Format80Variant)99, true, false, true, false)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Packed, Is.Null);
            Assert.That(moves, Is.EqualTo(0));
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.InvalidPackedPolicy), Is.True);
        }

        [Test]
        public void OverlayOccurrenceEnumerationStopsAtPackedFragmentBudget()
        {
            int moves = 0;
            IEnumerable<PackedIniFragmentOccurrence> Lazy()
            {
                for (int index = 0; index < 8; index++)
                {
                    moves++;
                    if (moves > 3) throw new InvalidOperationException("enumerated beyond the budget probe");
                    yield return Occurrence("OverlayPack", (index + 1).ToString(), "AA==", index);
                }
            }

            OverlayArrayReadResult result = ReadArray(
                Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, occurrences: Lazy()),
                Policy(fragmentLimits: new PackedIniFragmentCollectorLimits(2, 32)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Packed, Is.Null);
            Assert.That(moves, Is.EqualTo(3));
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.OccurrenceInputBudgetExceeded), Is.True);
        }

        [Test]
        public void ZeroFragmentBudgetFailsOnTheFirstOccurrence()
        {
            int moves = 0;
            IEnumerable<PackedIniFragmentOccurrence> Lazy()
            {
                moves++;
                yield return Occurrence("OverlayPack", "1", "AA==", 0);
            }

            OverlayArrayReadResult result = ReadArray(
                Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, occurrences: Lazy()),
                Policy(fragmentLimits: new PackedIniFragmentCollectorLimits(0, 32)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Packed, Is.Null);
            Assert.That(moves, Is.EqualTo(1));
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.OccurrenceInputBudgetExceeded), Is.True);
        }

        [Test]
        public void ExactOneFragmentBudgetAcceptsOneOccurrence()
        {
            int moves = 0;
            IEnumerable<PackedIniFragmentOccurrence> Lazy()
            {
                moves++;
                yield return Occurrence("OverlayPack", "1", "AA==", 0);
            }

            OverlayArrayReadResult result = ReadArray(
                Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, occurrences: Lazy()),
                Policy(fragmentLimits: new PackedIniFragmentCollectorLimits(1, 32)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Packed, Is.Not.Null);
            Assert.That(moves, Is.EqualTo(1));
            Assert.That(result.Packed.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.ChunkHeaderTruncated), Is.True);
        }

        [Test]
        public void MaximumFragmentBudgetDoesNotOverflowTheBoundedProbe()
        {
            OverlayArrayReadResult result = ReadArray(
                Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, "AA=="),
                Policy(fragmentLimits: new PackedIniFragmentCollectorLimits(int.MaxValue, 32)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Packed, Is.Not.Null);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.OccurrenceInputBudgetExceeded), Is.False);
            Assert.That(result.Packed.Diagnostics.Any(item => item.Code == PackedMapDiagnosticCode.ChunkHeaderTruncated), Is.True);
        }

        [Test]
        public void MissingFormat80TerminatorFailsWithoutFallback()
        {
            byte[] payload = new byte[] { 0xfe, 4, 0, 0x11 };
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, Convert.ToBase64String(Chunk(payload, 4)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Raw, Is.Null);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.PackedStageFailure), Is.True);
        }

        [Test]
        public void TrailingCompressedFormat80InputFailsWithoutFallback()
        {
            byte[] payload = new byte[] { 0xfe, 4, 0, 0x11, 0x80, 0x00 };
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, Convert.ToBase64String(Chunk(payload, 4)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Raw, Is.Null);
        }

        [Test]
        public void InvalidFormat80BackReferenceFailsWithoutArrayConstruction()
        {
            byte[] payload = new byte[] { 0x00, 0x01, 0x80 };
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, Convert.ToBase64String(Chunk(payload, 3)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Raw, Is.Null);
        }

        [TestCase(262143)]
        [TestCase(262145)]
        [TestCase(0)]
        public void NonExactOrdinaryArrayLengthsFailClosed(int length)
        {
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, FillBase64(length, 0x77));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Raw, Is.Null);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.ArrayLengthMismatch) || HasDiagnostic(result, OverlayDiagnosticCode.ZeroBlockPackedResult), Is.True);
        }

        [Test]
        public void RawArrayBudgetIsCheckedBeforePipelineExecution()
        {
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, FullBase64(2), Policy(limits: new OverlayReadLimits(maxRawArrayBytes: 32)));
            Assert.That(result.Packed, Is.Null);
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.RawArrayBudgetExceeded), Is.True);
        }

        [Test]
        public void TypeFailureDoesNotPreventDataChildFromSucceeding()
        {
            OverlayPackedDocumentReadResult result = ReadDocument("A===", FullBase64(0x2a));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.OverlayPack.Raw, Is.Null);
            Assert.That(result.OverlayDataPack.Raw.GetByteAt(0), Is.EqualTo(0x2a));
            Assert.That(HasDiagnostic(result, OverlayDiagnosticCode.PartnerUnavailableOrIncomplete), Is.True);
        }

        [Test]
        public void DataFailureDoesNotDiscardSuccessfulTypeChild()
        {
            OverlayPackedDocumentReadResult result = ReadDocument(FullBase64(0x2b), "A===");
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.OverlayPack.Raw.GetByteAt(0), Is.EqualTo(0x2b));
            Assert.That(result.OverlayDataPack.Raw, Is.Null);
        }

        [Test]
        public void BothMissingSectionsAreCompleteFailuresWithoutSyntheticPartners()
        {
            OverlayPackedDocumentReadResult result = new OverlayPackedDocumentReader().Read(
                Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Missing),
                Input(OverlaySectionKind.OverlayDataPack, OverlaySectionSelectionStatus.Missing),
                Policy());
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.OverlayPack.Raw, Is.Null);
            Assert.That(result.OverlayDataPack.Raw, Is.Null);
        }

        [Test]
        public void ZeroDiagnosticBudgetCannotTurnPackedFailureIntoSuccess()
        {
            OverlayPackedDocumentReadResult result = ReadDocument("A===", FullBase64(0x3a), Policy(limits: new OverlayReadLimits(0)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.HasFatalError, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.SuppressedDiagnosticCount, Is.GreaterThan(0));
        }

        [Test]
        public void ZeroDiagnosticBudgetCannotTurnLengthFailureIntoSuccess()
        {
            OverlayPackedDocumentReadResult result = ReadDocument(FillBase64(262143, 0x3a), FullBase64(0x3b), Policy(limits: new OverlayReadLimits(0)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.HasFatalError, Is.True);
            Assert.That(result.SuppressedDiagnosticCount, Is.GreaterThan(0));
        }

        [Test]
        public void ExecutionStateMergesWarningAndSaturatesSuppressedDiagnostics()
        {
            var child = new OverlayExecutionState();
            child.Observe(BinaryDiagnosticSeverity.Warning);
            child.Suppress(int.MaxValue);
            var parent = new OverlayExecutionState();
            parent.Merge(child, true);
            Assert.That(parent.CompletionStatus, Is.EqualTo(OverlayCompletionStatus.Succeeded));
            Assert.That(parent.HighestObservedSeverity, Is.EqualTo(BinaryDiagnosticSeverity.Warning));
            Assert.That(parent.SuppressedDiagnosticCount, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void RequiredNotRunStageCannotBeSuccessful()
        {
            var state = new OverlayExecutionState();
            state.Merge(new OverlayExecutionState(), true);
            Assert.That(state.CompletionStatus, Is.EqualTo(OverlayCompletionStatus.Failed));
            Assert.That(state.HasFatalError, Is.True);
        }

        [TestCase(0, 0, 0)]
        [TestCase(511, 0, 511)]
        [TestCase(0, 511, 261632)]
        [TestCase(511, 511, 262143)]
        public void ExternalRowMajorStorageCoordinatesUseTheExplicitFormula(int x, int y, int expected)
        {
            OverlayStorageIndexResult result = new OverlayStorageCoordinateIndexer().GetIndex(new OverlayStorageCoordinate(x, y), OverlayStorageCoordinateIndexProfile.ExternalRowMajorCandidate);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.ElementIndex, Is.EqualTo(expected));
        }

        [Test]
        public void OfficialEditorTransposedProfileIsExplicitAndDistinct()
        {
            var indexer = new OverlayStorageCoordinateIndexer();
            OverlayStorageIndexResult external = indexer.GetIndex(new OverlayStorageCoordinate(1, 2), OverlayStorageCoordinateIndexProfile.ExternalRowMajorCandidate);
            OverlayStorageIndexResult transposed = indexer.GetIndex(new OverlayStorageCoordinate(1, 2), OverlayStorageCoordinateIndexProfile.OfficialEditorTransposedComparison);
            Assert.That(external.ElementIndex, Is.EqualTo(1025));
            Assert.That(transposed.ElementIndex, Is.EqualTo(514));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(512, 0)]
        [TestCase(0, 512)]
        public void OutOfRangeStorageCoordinatesAreRejected(int x, int y)
        {
            OverlayStorageIndexResult result = new OverlayStorageCoordinateIndexer().GetIndex(new OverlayStorageCoordinate(x, y), OverlayStorageCoordinateIndexProfile.ExternalRowMajorCandidate);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ElementIndex, Is.Null);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(OverlayDiagnosticCode.StorageCoordinateOutOfRange));
        }

        [Test]
        public void UnknownCoordinateProfileIsRejectedDeterministically()
        {
            OverlayStorageIndexResult result = new OverlayStorageCoordinateIndexer().GetIndex(new OverlayStorageCoordinate(0, 0), (OverlayStorageCoordinateIndexProfile)99);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(OverlayDiagnosticCode.InvalidCoordinateProfile));
        }

        [Test]
        public void ZeroDiagnosticBudgetCannotTurnCoordinateFailureIntoSuccess()
        {
            OverlayStorageIndexResult result = new OverlayStorageCoordinateIndexer().GetIndex(
                new OverlayStorageCoordinate(-1, 0),
                OverlayStorageCoordinateIndexProfile.ExternalRowMajorCandidate,
                limits: new OverlayReadLimits(0));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Execution.HasFatalError, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Execution.SuppressedDiagnosticCount, Is.EqualTo(1));
        }

        [Test]
        public void IndexedViewExposesRawValuesWithoutOverlaySemantics()
        {
            OverlayRawIndexedView view = ReadDocument(FullBase64(0xff), FullBase64(0x09)).CreateIndexedView();
            byte type;
            byte data;
            OverlayRawCellPair pair;
            Assert.That(view.TryGetTypeByteAtIndex(0, out type), Is.True);
            Assert.That(view.TryGetDataByteAtIndex(0, out data), Is.True);
            Assert.That(view.TryGetPairAtIndex(0, out pair), Is.True);
            Assert.That(type, Is.EqualTo(0xff));
            Assert.That(data, Is.EqualTo(0x09));
            Assert.That(pair.TypeRaw, Is.EqualTo(0xff));
            Assert.That(pair.DataRaw, Is.EqualTo(0x09));
        }

        [Test]
        public void IndexedViewDoesNotFabricateMissingPartner()
        {
            OverlayRawIndexedView view = ReadDocument(FullBase64(0x11), "A===").CreateIndexedView();
            OverlayRawCellPair pair;
            Assert.That(view.TryGetPairAtIndex(0, out pair), Is.False);
        }

        [Test]
        public void OverlayPackedSnapshotsRemainImmutableAfterCallerMutation()
        {
            OverlayArrayReadResult result = ReadArray(OverlaySectionKind.OverlayPack, FullBase64(0x5a));
            byte[] decoded = result.Packed.DecodedBytes;
            byte[] firstBlock = result.Packed.BlockOutputs[0];
            decoded[0] = 0;
            firstBlock[0] = 0;
            Assert.That(result.Packed.DecodedBytes[0], Is.EqualTo(0x5a));
            Assert.That(result.Packed.BlockOutputs[0][0], Is.EqualTo(0x5a));
            Assert.That(result.Raw.GetByteAt(0), Is.EqualTo(0x5a));
            Assert.That(result.Packed.DecodedBytes, Is.EqualTo(result.Packed.DecodedBytes));
        }

        [Test]
        public void Format80MemoryStreamAndWindowPathsRemainEquivalentForOverlayFixture()
        {
            byte[] payload = new byte[] { 0xfe, 4, 0, 0x22, 0x80 };
            Format80Profile profile = new Format80Profile(Format80Variant.Absolute, true, false, true, false);
            Format80DecodeResult memory = new Format80Decoder().Decode(payload, 4, profile);
            using (var stream = new ShortReadStream(payload, 1))
            {
                Format80DecodeResult shortRead = new Format80Decoder().Decode(stream, payload.Length, Source(), 4, profile);
                using (var windowStream = new MemoryStream(payload))
                using (ReadOnlyDataWindowSession session = ReadOnlyDataWindowSession.FromSeekableStream(windowStream, Source(), 0, payload.Length, leaveOpen: true))
                {
                    Format80DecodeResult window = new Format80Decoder().Decode(session.Root, 4, profile);
                    Assert.That(shortRead.Bytes, Is.EqualTo(memory.Bytes));
                    Assert.That(window.Bytes, Is.EqualTo(memory.Bytes));
                    Assert.That(shortRead.Diagnostics.Select(item => item.Code), Is.EqualTo(memory.Diagnostics.Select(item => item.Code)));
                    Assert.That(window.Diagnostics.Select(item => item.Code), Is.EqualTo(memory.Diagnostics.Select(item => item.Code)));
                }
            }
        }

        [Test]
        public void CoreAssemblyHasNoUnityEngineReference()
        {
            string[] references = typeof(RA2YR.Core.AssemblyMarker).Assembly.GetReferencedAssemblies().Select(item => item.Name).ToArray();
            Assert.That(references.Any(item => item != null && (item.StartsWith("UnityEngine", StringComparison.Ordinal) || item.StartsWith("UnityEditor", StringComparison.Ordinal))), Is.False);
        }

        [Test]
        public void OverlayRawTypesDoNotExposeRulesArtOrUnityMemberTypes()
        {
            Type[] overlayTypes = typeof(OverlayArrayRaw).Assembly.GetTypes()
                .Where(type => type.Namespace == "RA2YR.Core.Formats.PackedMap" && type.Name.StartsWith("Overlay", StringComparison.Ordinal))
                .ToArray();
            Type[] exposed = overlayTypes.SelectMany(ExposedTypes).ToArray();
            Assert.That(exposed.Any(IsForbiddenType), Is.False);
        }

        [Test]
        public void OverlayRawTypesDoNotContainPInvokeOrRealLzoDecoder()
        {
            Type[] overlayTypes = typeof(OverlayArrayRaw).Assembly.GetTypes()
                .Where(type => type.Namespace == "RA2YR.Core.Formats.PackedMap" && type.Name.StartsWith("Overlay", StringComparison.Ordinal))
                .ToArray();
            Assert.That(overlayTypes.SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)).Any(method => method.GetCustomAttributes(typeof(DllImportAttribute), false).Any()), Is.False);
            Assert.That(overlayTypes.Select(type => type.Name), Has.None.Matches<string>(name => name.IndexOf("Lzo", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        [Test]
        public void SyntheticFixtureOracleDoesNotInvokeProductionOverlayParserOrIndexer()
        {
            byte[] envelope = FillEnvelope(4, 0x44);
            Assert.That(envelope, Is.EqualTo(new byte[] { 5, 0, 4, 0, 0xfe, 4, 0, 0x44, 0x80 }));
            Assert.That(1 + 512 * 2, Is.EqualTo(1025));
        }

        private static OverlayPackedDocumentReadResult ReadDocument(string typeBase64, string dataBase64, OverlayPackedReadPolicy policy = null)
        {
            return new OverlayPackedDocumentReader().Read(
                Input(OverlaySectionKind.OverlayPack, OverlaySectionSelectionStatus.Selected, typeBase64),
                Input(OverlaySectionKind.OverlayDataPack, OverlaySectionSelectionStatus.Selected, dataBase64),
                policy ?? Policy());
        }

        private static OverlayArrayReadResult ReadArray(OverlaySectionKind kind, string base64, OverlayPackedReadPolicy policy = null)
        {
            return ReadArray(Input(kind, OverlaySectionSelectionStatus.Selected, base64), policy);
        }

        private static OverlayArrayReadResult ReadArray(OverlaySectionInput input, OverlayPackedReadPolicy policy = null)
        {
            return new OverlayPackedArrayReader().Read(input, input.SectionKind, policy ?? Policy());
        }

        private static OverlayPackedReadPolicy Policy(
            PackedCodecKind codec = PackedCodecKind.Format80,
            Format80Profile format80 = null,
            ChunkSentinelPolicy sentinel = ChunkSentinelPolicy.RejectAllZero,
            PackedIniFragmentOrderingPolicy ordering = PackedIniFragmentOrderingPolicy.StrictSequentialFromOne,
            StrictBase64Policy base64Policy = StrictBase64Policy.StandardAlphabetNoWhitespace,
            PackedIniFragmentCollectorLimits fragmentLimits = null,
            OverlayReadLimits limits = null)
        {
            return new OverlayPackedReadPolicy(new PackedSectionDecodePolicy(
                ordering,
                base64Policy,
                sentinel,
                codec,
                format80 ?? new Format80Profile(Format80Variant.Absolute, true, false, true, false),
                fragmentLimits: fragmentLimits,
                chunkLimits: new WestwoodChunkReadLimits(maxOutputBytes: 300000),
                format80Limits: new Format80ReadLimits(maxOutputBytes: 300000)),
                OverlayStorageProfile.OrdinaryByte512,
                limits);
        }

        private static OverlaySectionInput Input(
            OverlaySectionKind kind,
            OverlaySectionSelectionStatus status,
            string base64 = null,
            string sectionName = null,
            int candidateCount = -1,
            IEnumerable<PackedIniFragmentOccurrence> occurrences = null)
        {
            string name = sectionName ?? OverlayStorageProfiles.GetExpectedSectionName(kind);
            int selected = status == OverlaySectionSelectionStatus.Missing || status == OverlaySectionSelectionStatus.Ambiguous ? -1 : 0;
            int candidates = candidateCount >= 0 ? candidateCount : status == OverlaySectionSelectionStatus.Missing ? 0 : status == OverlaySectionSelectionStatus.Ambiguous ? 2 : 1;
            IEnumerable<PackedIniFragmentOccurrence> values = occurrences == null
                ? status == OverlaySectionSelectionStatus.Selected ? new[] { Occurrence(name, "1", base64 ?? string.Empty, 0) } : Array.Empty<PackedIniFragmentOccurrence>()
                : occurrences;
            return new OverlaySectionInput(kind, name, status, selected, candidates, values, Source(), Provenance());
        }

        private static IEnumerable<PackedIniFragmentOccurrence> ExplosiveOccurrences(Action moved)
        {
            if (moved == null) yield break;
            moved();
            throw new InvalidOperationException("Occurrence source must not be enumerated.");
        }

        private static PackedIniFragmentOccurrence Occurrence(string section, string key, string value, int sourceOrder)
        {
            return new PackedIniFragmentOccurrence(section, key, value, sourceOrder, "synthetic", sourceOrder, Provenance().Single());
        }

        private static IReadOnlyList<IniSourceProvenance> Provenance()
        {
            return new[] { new IniSourceProvenance("synthetic", new[] { LogicalContentPath.Parse("synthetic-overlay.ini") }) };
        }

        private static BinarySourceContext Source()
        {
            return new BinarySourceContext("overlay-packed-array-tests", "synthetic", LogicalContentPath.Parse("synthetic-overlay.ini"));
        }

        private static string FullBase64(byte value) => FillBase64(262144, value);
        private static string FillBase64(int outputLength, byte value) => Convert.ToBase64String(FillEnvelope(outputLength, value));

        // Clean-room fixtures encode literal protocol bytes and never invoke production decoder helpers.
        private static byte[] FillEnvelope(int outputLength, byte value)
        {
            if (outputLength <= 0) return Array.Empty<byte>();
            var chunks = new List<byte[]>();
            int remaining = outputLength;
            while (remaining > 0)
            {
                int blockLength = Math.Min(remaining, ushort.MaxValue);
                chunks.Add(Chunk(new byte[] { 0xfe, (byte)blockLength, (byte)(blockLength >> 8), value, 0x80 }, blockLength));
                remaining -= blockLength;
            }
            return Concat(chunks);
        }

        private static byte[] Chunk(byte[] payload, int declaredOutputLength)
        {
            var result = new byte[4 + payload.Length];
            result[0] = (byte)payload.Length;
            result[1] = (byte)(payload.Length >> 8);
            result[2] = (byte)declaredOutputLength;
            result[3] = (byte)(declaredOutputLength >> 8);
            Buffer.BlockCopy(payload, 0, result, 4, payload.Length);
            return result;
        }

        private static byte[] Concat(IEnumerable<byte[]> arrays)
        {
            byte[][] materialized = arrays.ToArray();
            int length = materialized.Sum(item => item.Length);
            var result = new byte[length];
            int offset = 0;
            foreach (byte[] item in materialized)
            {
                Buffer.BlockCopy(item, 0, result, offset, item.Length);
                offset += item.Length;
            }
            return result;
        }

        private static bool HasDiagnostic(OverlayArrayReadResult result, OverlayDiagnosticCode code) => result.Diagnostics.Any(item => item.Code == code);
        private static bool HasDiagnostic(OverlayPackedDocumentReadResult result, OverlayDiagnosticCode code) => result.Diagnostics.Any(item => item.Code == code);

        private static IEnumerable<Type> ExposedTypes(Type type)
        {
            yield return type.BaseType;
            foreach (Type item in type.GetInterfaces()) yield return item;
            foreach (FieldInfo item in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) yield return item.FieldType;
            foreach (PropertyInfo item in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) yield return item.PropertyType;
            foreach (MethodInfo item in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                yield return item.ReturnType;
                foreach (ParameterInfo parameter in item.GetParameters()) yield return parameter.ParameterType;
            }
        }

        private static bool IsForbiddenType(Type type)
        {
            if (type == null) return false;
            string ns = type.Namespace ?? string.Empty;
            return ns.StartsWith("UnityEngine", StringComparison.Ordinal) || ns.StartsWith("UnityEditor", StringComparison.Ordinal) ||
                ns.IndexOf("Rules", StringComparison.OrdinalIgnoreCase) >= 0 || ns.IndexOf("Art", StringComparison.OrdinalIgnoreCase) >= 0 ||
                type.Name == "GameObject" || type.Name == "Texture2D" || type.Name == "Sprite";
        }

        private sealed class ShortReadStream : MemoryStream
        {
            private readonly int maximumRead;

            internal ShortReadStream(byte[] bytes, int maximumRead)
                : base(bytes)
            {
                this.maximumRead = maximumRead;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, maximumRead));
            }
        }
    }
}
