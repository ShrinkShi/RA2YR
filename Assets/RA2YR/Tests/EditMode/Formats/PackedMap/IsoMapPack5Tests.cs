using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Binary.Seekable;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.PackedMap;

namespace RA2YR.Tests.EditMode.Formats.PackedMap
{
    public sealed class IsoMapPack5Tests
    {
        [Test]
        public void EmptyStreamIsSuccessfulWithNoRecords() { Assert.That(Read(Array.Empty<byte>()).Records, Is.Empty); }

        [Test]
        public void OneRecordIsRead() { Assert.That(Read(Record(1, 2, 3, 4, 5, 6)).Records, Has.Count.EqualTo(1)); }

        [Test]
        public void MultipleRecordsPreserveSourceOrder()
        {
            var result = Read(Concat(Record(1, 0, 0, 0, 0, 0), Record(2, 0, 0, 0, 0, 0)));
            Assert.That(result.Records.Select(item => item.SourceOrdinal), Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void AllZeroRecordIsPreserved() { Assert.That(Read(Record(0, 0, 0, 0, 0, 0)).Records[0].GetRawBytesCopy(), Is.EqualTo(new byte[11])); }

        [Test]
        public void MaxUnsignedFieldsArePreserved()
        {
            IsoMapPack5RecordRaw record = Read(Record(ushort.MaxValue, ushort.MaxValue, uint.MaxValue, 255, 255, 255)).Records[0];
            Assert.That(record.XRawU16LittleEndian, Is.EqualTo(ushort.MaxValue));
            Assert.That(record.YRawU16LittleEndian, Is.EqualTo(ushort.MaxValue));
            Assert.That(record.TileRawU32LittleEndian, Is.EqualTo(uint.MaxValue));
        }

        [Test]
        public void LittleEndianCoordinatesAreDecoded() { IsoMapPack5RecordRaw r = Read(Record(0x1234, 0x5678, 0, 0, 0, 0)).Records[0]; Assert.That(r.XRawU16LittleEndian, Is.EqualTo(0x1234)); Assert.That(r.YRawU16LittleEndian, Is.EqualTo(0x5678)); }

        [Test]
        public void LittleEndianTileIsDecoded() { Assert.That(Read(Record(0, 0, 0x78563412, 0, 0, 0)).Records[0].TileRawU32LittleEndian, Is.EqualTo(0x78563412u)); }

        [Test]
        public void TileLowViewIsRetained() { Assert.That(Read(Record(0, 0, 0x78563412, 0, 0, 0)).Records[0].TileLowU16LittleEndian, Is.EqualTo(0x3412)); }

        [Test]
        public void TileHighViewIsRetained() { Assert.That(Read(Record(0, 0, 0x78563412, 0, 0, 0)).Records[0].TileHighU16LittleEndian, Is.EqualTo(0x7856)); }

        [Test]
        public void SubTileIsRetained() { Assert.That(Read(Record(0, 0, 0, 0xA1, 0, 0)).Records[0].SubTileRaw, Is.EqualTo(0xA1)); }

        [Test]
        public void LevelIsRetained() { Assert.That(Read(Record(0, 0, 0, 0, 0xB2, 0)).Records[0].LevelRaw, Is.EqualTo(0xB2)); }

        [Test]
        public void TailIsRetainedWithoutIceGrowthInterpretation() { Assert.That(Read(Record(0, 0, 0, 0, 0, 0xC3)).Records[0].TailRaw, Is.EqualTo(0xC3)); }

        [Test]
        public void RawBytesAreCopied() { byte[] bytes = Record(1, 2, 3, 4, 5, 6); IsoMapPack5RecordRaw record = Read(bytes).Records[0]; bytes[0] = 99; Assert.That(record.XRawU16LittleEndian, Is.EqualTo(1)); }

        [Test]
        public void RawBytesCopyIsDefensive() { IsoMapPack5RecordRaw record = Read(Record(1, 2, 3, 4, 5, 6)).Records[0]; byte[] copy = record.GetRawBytesCopy(); copy[0] = 99; Assert.That(record.XRawU16LittleEndian, Is.EqualTo(1)); }

        [Test]
        public void SourceOffsetUsesAbsoluteOrigin() { Assert.That(Read(Record(1, 2, 3, 4, 5, 6), absoluteOffset: 77).Records[0].SourceOffset, Is.EqualTo(77)); }

        [Test]
        public void ProvenanceIsRetained() { Assert.That(Read(Record(1, 2, 3, 4, 5, 6)).Records[0].Provenance[0].SourceId, Is.EqualTo("synthetic")); }

        [TestCase(0, 0)]
        [TestCase(1, 2)]
        [TestCase(15, 27)]
        [TestCase(255, 511)]
        [TestCase(1024, 2048)]
        [TestCase(32767, 32768)]
        [TestCase(65535, 65535)]
        public void CoordinatePairsRemainRaw(int x, int y) { ushort xRaw = checked((ushort)x); ushort yRaw = checked((ushort)y); IsoMapPack5RecordRaw r = Read(Record(xRaw, yRaw, 0, 0, 0, 0)).Records[0]; Assert.That(r.XRawU16LittleEndian, Is.EqualTo(xRaw)); Assert.That(r.YRawU16LittleEndian, Is.EqualTo(yRaw)); }

        [TestCase(0u)]
        [TestCase(1u)]
        [TestCase(65535u)]
        [TestCase(65536u)]
        [TestCase(0xFFFFFFFFu)]
        public void TileViewsRemainUnresolved(uint tile) { IsoMapPack5RecordRaw r = Read(Record(0, 0, tile, 0, 0, 0)).Records[0]; Assert.That(r.TileRawU32LittleEndian, Is.EqualTo(tile)); }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        public void RemaindersAreRejectedByDefault(int remainder)
        {
            IsoMapPack5RecordReadResult result = Read(Concat(Record(1, 1, 1, 1, 1, 1), Enumerable.Repeat((byte)0x7F, remainder).ToArray()));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == IsoMapDiagnosticCode.UnexpectedTrailingBytes), Is.True);
        }

        [Test]
        public void PreservePolicyKeepsRemainder() { byte[] input = Concat(Record(1, 1, 1, 1, 1, 1), new byte[] { 1, 2, 3 }); IsoMapPack5RecordReadResult r = Read(input, IsoMapPack5TrailingPolicy.PreserveRemainderWithDiagnostic); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Trailing.Classification, Is.EqualTo(IsoMapTrailingClassification.PreservedRemainder)); Assert.That(r.Trailing.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 })); }

        [Test]
        public void ExactFourZeroTrailerIsAllowedExplicitly() { IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), new byte[] { 0, 0, 0, 0 }), IsoMapPack5TrailingPolicy.AllowExactFourZeroTrailer); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Trailing.Classification, Is.EqualTo(IsoMapTrailingClassification.ExactFourZeroTrailer)); }

        [Test]
        public void NonzeroFourTrailerIsRejected() { IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), new byte[] { 0, 0, 0, 1 }), IsoMapPack5TrailingPolicy.AllowExactFourZeroTrailer); Assert.That(r.IsSuccess, Is.False); Assert.That(HasDiagnostic(r, IsoMapDiagnosticCode.InvalidFourZeroTrailer), Is.True); }

        [Test]
        public void FiveZeroTrailerIsRejected() { IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), new byte[] { 0, 0, 0, 0, 0 }), IsoMapPack5TrailingPolicy.AllowExactFourZeroTrailer); Assert.That(r.IsSuccess, Is.False); Assert.That(HasDiagnostic(r, IsoMapDiagnosticCode.InvalidFourZeroTrailer), Is.True); }

        [Test]
        public void TrailingOffsetIsExact() { IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), new byte[] { 9 }), IsoMapPack5TrailingPolicy.PreserveRemainderWithDiagnostic, absoluteOffset: 20); Assert.That(r.Trailing.AbsoluteOffset, Is.EqualTo(31)); }

        [Test]
        public void TrailingBudgetFailsBeforePreservation() { IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), new byte[] { 1, 2 }), IsoMapPack5TrailingPolicy.PreserveRemainderWithDiagnostic, new IsoMapPack5ReadLimits(maxTrailingBytes: 1)); Assert.That(HasDiagnostic(r, IsoMapDiagnosticCode.TrailingBudgetExceeded), Is.True); }

        [Test]
        public void RecordsAreNotSilentlyTruncated() { IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), new byte[] { 0xAA })); Assert.That(r.Trailing.Classification, Is.EqualTo(IsoMapTrailingClassification.RejectedRemainder)); }

        [Test]
        public void ZeroRecordBudgetStopsBeforeAllocation() { IsoMapPack5RecordReadResult r = Read(Record(1, 1, 1, 1, 1, 1), limits: new IsoMapPack5ReadLimits(maxRecords: 0)); Assert.That(HasDiagnostic(r, IsoMapDiagnosticCode.RecordBudgetExceeded), Is.True); Assert.That(r.Records, Is.Empty); }

        [Test]
        public void ExactRecordBudgetSucceeds() { IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), Record(2, 2, 2, 2, 2, 2)), limits: new IsoMapPack5ReadLimits(maxRecords: 2)); Assert.That(r.IsSuccess, Is.True); }

        [Test]
        public void InputBudgetStopsBeforeMaterialization() { IsoMapPack5RecordReadResult r = Read(Record(1, 1, 1, 1, 1, 1), limits: new IsoMapPack5ReadLimits(maxInputBytes: 10)); Assert.That(HasDiagnostic(r, IsoMapDiagnosticCode.InputBudgetExceeded), Is.True); }

        [Test]
        public void DiagnosticBudgetBoundsDiagnostics() { IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), new byte[] { 1, 2 }), limits: new IsoMapPack5ReadLimits(maxDiagnostics: 1)); Assert.That(r.Diagnostics.Count, Is.LessThanOrEqualTo(1)); }

        [Test]
        public void ZeroDiagnosticBudgetStillFailsInputBudget() { IsoMapPack5RecordReadResult r = Read(Record(1, 1, 1, 1, 1, 1), limits: new IsoMapPack5ReadLimits(maxInputBytes: 1, maxDiagnostics: 0)); Assert.That(r.IsSuccess, Is.False); Assert.That(r.SuppressedDiagnosticCount, Is.EqualTo(1)); }

        [Test]
        public void ZeroDiagnosticBudgetStillFailsInvalidTrailer() { IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), new byte[] { 7 }), limits: new IsoMapPack5ReadLimits(maxDiagnostics: 0)); Assert.That(r.IsSuccess, Is.False); Assert.That(r.HasFatalError, Is.True); }

        [Test]
        public void TrailingOffsetOverflowIsStructured() { IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), new byte[] { 7 }), limits: new IsoMapPack5ReadLimits(maxDiagnostics: 0), absoluteOffset: long.MaxValue); Assert.That(r.IsSuccess, Is.False); Assert.That(r.HasFatalError, Is.True); Assert.That(r.SuppressedDiagnosticCount, Is.EqualTo(1)); }

        [Test]
        public void MemoryStreamUsesSameReader() { byte[] bytes = Record(1, 2, 3, 4, 5, 6); using (var stream = new MemoryStream(bytes)) { Assert.That(new IsoMapPack5RecordReader().Read(stream, bytes.Length, Source()).Records[0].GetRawBytesCopy(), Is.EqualTo(bytes)); } }

        [Test]
        public void ShortReadStreamUsesBoundedSession() { byte[] bytes = Concat(Record(1, 2, 3, 4, 5, 6), Record(7, 8, 9, 10, 11, 12)); using (var stream = new ShortReadStream(bytes, 1)) { Assert.That(new IsoMapPack5RecordReader().Read(stream, bytes.Length, Source()).Records, Has.Count.EqualTo(2)); } }

        [Test]
        public void WindowInputUsesAbsoluteOffset() { byte[] bytes = Concat(new byte[] { 99 }, Record(1, 2, 3, 4, 5, 6), new byte[] { 88 }); using (var stream = new MemoryStream(bytes)) using (var session = ReadOnlyDataWindowSession.FromSeekableStream(stream, Source(), 1, 11)) { Assert.That(new IsoMapPack5RecordReader().Read(session.Root).Records[0].SourceOffset, Is.EqualTo(1)); } }

        [Test]
        public void ShortReadStreamStopsOnDeclaredLength() { using (var stream = new ShortReadStream(Record(1, 2, 3, 4, 5, 6), 2)) { IsoMapPack5RecordReadResult r = new IsoMapPack5RecordReader().Read(stream, 11, Source()); Assert.That(r.IsSuccess, Is.True); } }

        [Test]
        public void CoordinateUniqueIsIndexed() { IsoMapCoordinateAnalysis a = Analyze(Record(1, 2, 0, 0, 0, 0)); Assert.That(a.Index.Occurrences, Has.Count.EqualTo(1)); Assert.That(a.Index.DuplicateGroups, Is.Empty); }

        [Test]
        public void CoordinateByteIdenticalDuplicateIsPreserved() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(1, 2, 0, 0, 0, 0), Record(1, 2, 0, 0, 0, 0))); Assert.That(a.Index.Occurrences, Has.Count.EqualTo(2)); Assert.That(a.Index.DuplicateGroups[0].ConflictingPayload, Is.False); }

        [Test]
        public void CoordinateConflictingDuplicateIsDiagnosed() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(1, 2, 0, 0, 0, 0), Record(1, 2, 0, 0, 0, 1))); Assert.That(HasDiagnostic(a, IsoMapDiagnosticCode.ConflictingDuplicateCoordinate), Is.True); }

        [Test]
        public void CoordinateRejectDuplicatePolicyFails() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(1, 2, 0, 0, 0, 0), Record(1, 2, 0, 0, 0, 0)), IsoMapCoordinateDuplicatePolicy.RejectAnyDuplicate); Assert.That(a.IsSuccess, Is.False); }

        [Test]
        public void RejectDuplicateStillFailsWithZeroDiagnosticBudget() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(1, 2, 0, 0, 0, 0), Record(1, 2, 0, 0, 0, 0)), IsoMapCoordinateDuplicatePolicy.RejectAnyDuplicate, limits: new IsoMapPack5ReadLimits(maxDiagnostics: 0)); Assert.That(a.IsSuccess, Is.False); Assert.That(a.SuppressedDiagnosticCount, Is.EqualTo(1)); }

        [Test]
        public void CoordinatePreservePolicyWarns() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(1, 2, 0, 0, 0, 0), Record(1, 2, 0, 0, 0, 0)), IsoMapCoordinateDuplicatePolicy.PreserveAllAndDiagnose); Assert.That(a.IsSuccess, Is.True); Assert.That(HasDiagnostic(a, IsoMapDiagnosticCode.DuplicateCoordinate), Is.True); }

        [Test]
        public void AllowIdenticalPolicyRejectsConflictingDuplicate() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(1, 2, 3, 4, 5, 6), Record(1, 2, 3, 4, 5, 7)), IsoMapCoordinateDuplicatePolicy.AllowByteIdenticalDuplicatesButDiagnose); Assert.That(a.IsSuccess, Is.False); Assert.That(a.Index.DuplicateGroups[0].ConflictingPayload, Is.True); }

        [Test]
        public void WarningBudgetThenDuplicateErrorStillFails() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(2, 2, 0, 0, 0, 0), Record(2, 2, 0, 0, 0, 1)), IsoMapCoordinateDuplicatePolicy.AllowByteIdenticalDuplicatesButDiagnose, new IsoMapCoordinateValidationProfile(width: 1, height: 1), new IsoMapPack5ReadLimits(maxDiagnostics: 1)); Assert.That(a.IsSuccess, Is.False); Assert.That(a.SuppressedDiagnosticCount, Is.GreaterThanOrEqualTo(1)); }

        [Test]
        public void WarningBudgetThenCoordinateBudgetErrorStillFails() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(2, 2, 0, 0, 0, 0), Record(3, 3, 0, 0, 0, 0)), limits: new IsoMapPack5ReadLimits(maxCoordinateEntries: 1, maxDiagnostics: 1), profile: new IsoMapCoordinateValidationProfile(width: 1, height: 1)); Assert.That(a.IsSuccess, Is.False); Assert.That(a.SuppressedDiagnosticCount, Is.GreaterThanOrEqualTo(1)); }

        [Test]
        public void PackedChildFailureWithZeroDiagnosticBudgetFailsTopLevel() { IsoMapPack5PackedReadResult r = PackedRead(null, limits: new IsoMapPack5ReadLimits(maxDiagnostics: 0)); Assert.That(r.CompletionStatus, Is.EqualTo(IsoMapCompletionStatus.Failed)); Assert.That(r.HasFatalError, Is.True); Assert.That(r.SuppressedDiagnosticCount, Is.GreaterThan(0)); }

        [Test]
        public void CoordinateChildFailureWithZeroDiagnosticBudgetFailsTopLevel() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Concat(Record(1, 2, 0, 0, 0, 0), Record(1, 2, 0, 0, 0, 0))), limits: new IsoMapPack5ReadLimits(maxDiagnostics: 0), duplicatePolicy: IsoMapCoordinateDuplicatePolicy.RejectAnyDuplicate); Assert.That(r.CompletionStatus, Is.EqualTo(IsoMapCompletionStatus.Failed)); Assert.That(r.HasFatalError, Is.True); Assert.That(r.SuppressedDiagnosticCount, Is.GreaterThan(0)); }

        [Test]
        public void RecordChildFailureWithZeroDiagnosticBudgetFailsTopLevel() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Concat(Record(1, 2, 3, 4, 5, 6), new byte[] { 9 })), limits: new IsoMapPack5ReadLimits(maxDiagnostics: 0)); Assert.That(r.CompletionStatus, Is.EqualTo(IsoMapCompletionStatus.Failed)); Assert.That(r.HasFatalError, Is.True); Assert.That(r.SuppressedDiagnosticCount, Is.GreaterThan(0)); Assert.That(r.Records.IsSuccess, Is.False); Assert.That(r.Coordinates, Is.Null); }

        [Test]
        public void SuppressedCountsFromMultipleChildrenAccumulate() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Concat(Record(1, 2, 0, 0, 0, 0), Record(1, 2, 0, 0, 0, 0), new byte[] { 9 })), trailingPolicy: IsoMapPack5TrailingPolicy.PreserveRemainderWithDiagnostic, limits: new IsoMapPack5ReadLimits(maxDiagnostics: 0), duplicatePolicy: IsoMapCoordinateDuplicatePolicy.RejectAnyDuplicate); Assert.That(r.IsSuccess, Is.False); Assert.That(r.SuppressedDiagnosticCount, Is.GreaterThanOrEqualTo(2)); }

        [Test]
        public void SuppressedDiagnosticAggregationSaturatesAtIntMaxValue()
        {
            var first = new IsoMapExecutionState();
            first.Suppress(int.MaxValue);
            var second = new IsoMapExecutionState();
            second.SuppressOne();
            var aggregate = new IsoMapExecutionState();
            aggregate.Merge(first);
            aggregate.Merge(second);
            Assert.That(aggregate.SuppressedDiagnosticCount, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void HighestSeverityAggregatesWarningChildState() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Concat(Record(1, 2, 0, 0, 0, 0), new byte[] { 9 })), trailingPolicy: IsoMapPack5TrailingPolicy.PreserveRemainderWithDiagnostic, limits: new IsoMapPack5ReadLimits(maxDiagnostics: 0)); Assert.That(r.IsSuccess, Is.True); Assert.That(r.HighestObservedSeverity, Is.EqualTo(BinaryDiagnosticSeverity.Warning)); Assert.That(r.SuppressedDiagnosticCount, Is.GreaterThan(0)); }

        [Test]
        public void SuccessfulPackedAndRecordsButMissingCoordinateStageFailsTopLevel()
        {
            var packed = new PackedSectionDecodeResult(null, null, null, Array.Empty<byte[]>(), Array.Empty<PackedMapDiagnostic>());
            var records = new IsoMapPack5RecordReadResult(Array.Empty<IsoMapPack5RecordRaw>(), null, Array.Empty<IsoMapDiagnostic>());
            var result = new IsoMapPack5PackedReadResult(packed, records, null, Array.Empty<IsoMapDiagnostic>());
            Assert.That(result.CompletionStatus, Is.EqualTo(IsoMapCompletionStatus.Failed));
            Assert.That(result.HasFatalError, Is.True);
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void PackedAdapterConsumedMismatchDoesNotRunRecordParser() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6), consumed: 0)); Assert.That(r.Records, Is.Null); Assert.That(r.IsSuccess, Is.False); }

        [Test]
        public void TruncatedDeclaredChunkNeverRunsRecordStage()
        {
            string input = Convert.ToBase64String(new byte[] { 2, 0, 11, 0, 0xAA });
            IsoMapPack5PackedReadResult result = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6)), base64: input);
            Assert.That(result.Packed.IsSuccess, Is.False);
            Assert.That(result.Records, Is.Null);
            Assert.That(result.Coordinates, Is.Null);
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public void EmptyPackedInputWithoutLzoBackendFailsClosed()
        {
            var result = new IsoMapPack5PackedSectionReader().Read(
                Array.Empty<PackedIniFragmentOccurrence>(),
                PackedPolicy(limits: new IsoMapPack5ReadLimits(maxDiagnostics: 0)),
                null);
            Assert.That(result.CompletionStatus, Is.EqualTo(IsoMapCompletionStatus.Failed));
            Assert.That(result.HasFatalError, Is.True);
            Assert.That(result.SuppressedDiagnosticCount, Is.EqualTo(1));
            Assert.That(result.Records, Is.Null);
            Assert.That(result.Coordinates, Is.Null);
        }

        [Test]
        public void EmptyPackedInputWithBackendIsIndependentlyRejected()
        {
            var result = new IsoMapPack5PackedSectionReader().Read(
                Array.Empty<PackedIniFragmentOccurrence>(),
                PackedPolicy(),
                new FakeBackend(Array.Empty<byte>()));
            Assert.That(HasDiagnostic(result, IsoMapDiagnosticCode.EmptyPackedInput), Is.True);
            Assert.That(result.Records, Is.Null);
            Assert.That(result.Coordinates, Is.Null);
        }

        [Test]
        public void ZeroChunkEnvelopeWithBackendIsIndependentlyRejected()
        {
            var result = new IsoMapPack5PackedSectionReader().Read(
                new[] { Occurrence("IsoMapPack5", "1", Convert.ToBase64String(new byte[4])) },
                PackedPolicy(ChunkSentinelPolicy.AllowZeroZeroAsTerminator),
                new FakeBackend(Array.Empty<byte>()));
            Assert.That(HasDiagnostic(result, IsoMapDiagnosticCode.EmptyChunkEnvelope), Is.True);
            Assert.That(result.Records, Is.Null);
            Assert.That(result.Coordinates, Is.Null);
        }

        [Test]
        public void NullRecordStopsEnumerationImmediately() { IsoMapCoordinateAnalysis analysis = new IsoMapCoordinateIndexer().Analyze(NullThenThrow(), source: Source()); Assert.That(analysis.IsSuccess, Is.False); Assert.That(analysis.HasFatalError, Is.True); }

        [Test]
        public void MultipleDuplicateGroupsHaveDeterministicOrder() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(2, 2, 0, 0, 0, 0), Record(1, 1, 0, 0, 0, 0), Record(2, 2, 0, 0, 0, 1), Record(1, 1, 0, 0, 0, 2))); Assert.That(a.Index.DuplicateGroups.Select(g => g.Key.XRaw), Is.EqualTo(new ushort[] { 2, 1 })); Assert.That(a.Index.DuplicateGroups.SelectMany(g => g.Occurrences).Select(o => o.SourceOrdinal), Is.EqualTo(new[] { 0, 2, 1, 3 })); }

        [Test]
        public void CoordinateThreeWayDuplicateRetainsAllOrdinals() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(1, 2, 0, 0, 0, 0), Record(1, 2, 0, 0, 0, 1), Record(1, 2, 0, 0, 0, 2))); Assert.That(a.Index.DuplicateGroups[0].Occurrences.Select(x => x.SourceOrdinal), Is.EqualTo(new[] { 0, 1, 2 })); }

        [Test]
        public void CoordinateSourceOrderIsNotSorted() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(9, 9, 0, 0, 0, 0), Record(1, 1, 0, 0, 0, 0))); Assert.That(a.Index.Occurrences.Select(x => x.Key.XRaw), Is.EqualTo(new ushort[] { 9, 1 })); }

        [Test]
        public void CoordinateOutOfDomainIsDiagnosed() { IsoMapCoordinateAnalysis a = Analyze(Record(4, 4, 0, 0, 0, 0), profile: new IsoMapCoordinateValidationProfile(width: 4, height: 4)); Assert.That(HasDiagnostic(a, IsoMapDiagnosticCode.OutOfDomainCoordinate), Is.True); }

        [Test]
        public void CoordinateBoundaryIsAccepted() { IsoMapCoordinateAnalysis a = Analyze(Record(3, 3, 0, 0, 0, 0), profile: new IsoMapCoordinateValidationProfile(width: 4, height: 4)); Assert.That(HasDiagnostic(a, IsoMapDiagnosticCode.OutOfDomainCoordinate), Is.False); }

        [Test]
        public void CoordinateAxisOrderIsExplicit() { IsoMapCoordinateAnalysis a = Analyze(Record(1, 4, 0, 0, 0, 0), profile: new IsoMapCoordinateValidationProfile(IsoMapCoordinateAxisOrder.YThenX, width: 4, height: 2)); Assert.That(HasDiagnostic(a, IsoMapDiagnosticCode.OutOfDomainCoordinate), Is.True); }

        [Test]
        public void CoordinateSignednessDoesNotChangeRawKey() { IsoMapCoordinateAnalysis a = Analyze(Record(ushort.MaxValue, 0, 0, 0, 0, 0), profile: new IsoMapCoordinateValidationProfile(signedness: IsoMapCoordinateSignednessCandidate.Signed16Candidate, width: 10, height: 10)); Assert.That(a.Index.Occurrences[0].Key.XRaw, Is.EqualTo(ushort.MaxValue)); }

        [Test]
        public void CoordinateDoesNotAutoSwapAxes() { IsoMapCoordinateAnalysis a = Analyze(Record(8, 1, 0, 0, 0, 0), profile: new IsoMapCoordinateValidationProfile(width: 2, height: 10)); Assert.That(HasDiagnostic(a, IsoMapDiagnosticCode.OutOfDomainCoordinate), Is.True); }

        [Test]
        public void SparseCoordinatesAreAcceptedWithoutSynthesis() { IsoMapCoordinateAnalysis a = Analyze(Record(5, 5, 0, 0, 0, 0), profile: new IsoMapCoordinateValidationProfile(width: 10, height: 10)); Assert.That(a.Index.Occurrences, Has.Count.EqualTo(1)); }

        [Test]
        public void DenseCandidateIsExplicitOnly() { IsoMapCoordinateAnalysis a = Analyze(Record(1, 1, 0, 0, 0, 0), profile: new IsoMapCoordinateValidationProfile(width: 2, height: 2, configuredDenseCountCandidate: true)); Assert.That(a.DenseCountCandidate, Is.True); }

        [Test]
        public void CoordinateWidthWithoutHeightIsRejected() { Assert.Throws<ArgumentException>(() => new IsoMapCoordinateValidationProfile(width: 2)); }

        [Test]
        public void CoordinateHeightWithoutWidthIsRejected() { Assert.Throws<ArgumentException>(() => new IsoMapCoordinateValidationProfile(height: 2)); }

        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        public void CoordinateRectangleDimensionsMustBePositive(int width, int height) { Assert.Throws<ArgumentOutOfRangeException>(() => new IsoMapCoordinateValidationProfile(width: width, height: height)); }

        [Test]
        public void DenseCandidateRequiresRectangleDimensions() { Assert.Throws<ArgumentException>(() => new IsoMapCoordinateValidationProfile(configuredDenseCountCandidate: true)); }

        [Test]
        public void InvalidTrailingPolicyIsRejectedBeforeInputInspection() { Assert.Throws<ArgumentOutOfRangeException>(() => Read(Array.Empty<byte>(), (IsoMapPack5TrailingPolicy)99)); }

        [Test]
        public void InvalidDuplicatePolicyIsRejectedBeforeEnumeration() { Assert.Throws<ArgumentOutOfRangeException>(() => new IsoMapCoordinateIndexer().Analyze(NullThenThrow(), (IsoMapCoordinateDuplicatePolicy)99, source: Source())); }

        [Test]
        public void InvalidAxisOrderIsRejectedByProfile() { Assert.Throws<ArgumentOutOfRangeException>(() => new IsoMapCoordinateValidationProfile((IsoMapCoordinateAxisOrder)99)); }

        [Test]
        public void InvalidSignednessIsRejectedByProfile() { Assert.Throws<ArgumentOutOfRangeException>(() => new IsoMapCoordinateValidationProfile(signedness: (IsoMapCoordinateSignednessCandidate)99)); }

        [Test]
        public void CoordinateBudgetStopsIndexing() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(1, 1, 0, 0, 0, 0), Record(2, 2, 0, 0, 0, 0)), limits: new IsoMapPack5ReadLimits(maxCoordinateEntries: 1)); Assert.That(HasDiagnostic(a, IsoMapDiagnosticCode.CoordinateBudgetExceeded), Is.True); }

        [Test]
        public void DuplicateGroupBudgetStopsGroups() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(1, 1, 0, 0, 0, 0), Record(1, 1, 0, 0, 0, 0)), limits: new IsoMapPack5ReadLimits(maxDuplicateGroups: 0)); Assert.That(HasDiagnostic(a, IsoMapDiagnosticCode.CoordinateBudgetExceeded), Is.True); }

        [Test]
        public void PackedAdapterRequiresRawLzo() { IsoMapPack5PackedReadResult r = new IsoMapPack5PackedSectionReader().Read(Array.Empty<PackedIniFragmentOccurrence>(), new IsoMapPack5PackedReadPolicy(new PackedSectionDecodePolicy(PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder, StrictBase64Policy.StandardAlphabetNoWhitespace, ChunkSentinelPolicy.RejectAllZero, PackedCodecKind.Format80))); Assert.That(HasDiagnostic(r, IsoMapDiagnosticCode.WrongCodec), Is.True); }

        [Test]
        public void PackedAdapterBackendUnavailableIsStructured() { IsoMapPack5PackedReadResult r = PackedRead(null); Assert.That(r.IsSuccess, Is.False); Assert.That(HasDiagnostic(r, IsoMapDiagnosticCode.BackendUnavailable), Is.True); Assert.That(r.Packed, Is.Null); }

        [Test]
        public void PackedAdapterNullBackendResultIsStructured() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(null)); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Packed.Diagnostics.Any(x => x.Code == PackedMapDiagnosticCode.BackendFailure), Is.True); }

        [Test]
        public void PackedAdapterFakeBackendSuccessParsesRecords() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6))); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Records.Records, Has.Count.EqualTo(1)); }

        [Test]
        public void PackedAdapterDoesNotParseAfterBase64Failure() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6)), "not-base64"); Assert.That(r.Records, Is.Null); Assert.That(HasDiagnostic(r, IsoMapDiagnosticCode.PackedStageFailure), Is.True); }

        [Test]
        public void PackedAdapterPreservesTrailingFailure() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Concat(Record(1, 2, 3, 4, 5, 6), 1))); Assert.That(r.Records.IsSuccess, Is.False); Assert.That(r.Coordinates, Is.Null); Assert.That(HasDiagnostic(r.Records, IsoMapDiagnosticCode.UnexpectedTrailingBytes), Is.True); }

        [Test]
        public void PackedAdapterUsesExplicitPolicyNotSectionName() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6)), section: "OverlayPack"); Assert.That(r.Packed.Fragments.Occurrences[0].SectionName, Is.EqualTo("OverlayPack")); Assert.That(r.IsSuccess, Is.True); }

        [Test]
        public void PackedAdapterRetainsFragmentStage() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6))); Assert.That(r.Packed.Fragments.Occurrences, Has.Count.EqualTo(1)); }

        [Test]
        public void PackedAdapterRetainsChunkStage() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6))); Assert.That(r.Packed.Envelope.Blocks, Has.Count.EqualTo(1)); }

        [Test]
        public void PackedAdapterRetainsDecodedStream() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6))); Assert.That(r.Packed.DecodedBytes, Is.EqualTo(Record(1, 2, 3, 4, 5, 6))); }

        [Test]
        public void PackedAdapterWrongBackendOutputDoesNotParse() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(new byte[] { 1, 2 }), declaredOutputLength: 1); Assert.That(r.Records, Is.Null); Assert.That(r.IsSuccess, Is.False); }

        [Test]
        public void PackedAdapterProvenanceIsAvailableToRecords() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6))); Assert.That(r.Records.Records[0].Provenance[0].SourceId, Is.EqualTo("synthetic")); }

        [Test]
        public void PackedAdapterDoesNotExposeProjectBaseline() { Assert.That(typeof(IsoMapPack5PackedSectionReader).Assembly.GetTypes().Any(t => t.Name.IndexOf("ProjectBaseline", StringComparison.OrdinalIgnoreCase) >= 0 && t.Namespace != null && t.Namespace.Contains("PackedMap")), Is.False); }

        [Test]
        public void CoreTypesDoNotReferenceUnityEngine() { string[] references = typeof(RA2YR.Core.AssemblyMarker).Assembly.GetReferencedAssemblies().Select(item => item.Name).ToArray(); Assert.That(references.Any(item => item != null && (item.StartsWith("UnityEngine", StringComparison.Ordinal) || item.StartsWith("UnityEditor", StringComparison.Ordinal))), Is.False); }

        [Test]
        public void NoOverlayPreviewTmpModelsWereAdded() { Assert.That(typeof(IsoMapPack5RecordRaw).Assembly.GetTypes().Where(t => t.Namespace != null && t.Namespace.StartsWith("RA2YR.Core.Formats.PackedMap", StringComparison.Ordinal)).Select(t => t.Name), Has.None.Matches<string>(name => name.IndexOf("Overlay", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) >= 0 || name.Equals("TMP", StringComparison.OrdinalIgnoreCase))); }

        [Test]
        public void NoLzoAlgorithmTypeWasAdded() { Assert.That(typeof(IsoMapPack5RecordRaw).Assembly.GetTypes().Where(t => t.Namespace != null && t.Namespace.StartsWith("RA2YR.Core.Formats.PackedMap", StringComparison.Ordinal)).Select(t => t.Name), Has.None.Matches<string>(name => name.IndexOf("Lzo1X", StringComparison.OrdinalIgnoreCase) >= 0 && name != nameof(ILzoDecodeBackend))); }

        [Test]
        public void SyntheticRecordOracleDoesNotCallReader() { byte[] expected = Record(1, 2, 3, 4, 5, 6); Assert.That(expected.Length, Is.EqualTo(11)); Assert.That(expected[10], Is.EqualTo(6)); }

        [TestCase(0, 0, 0L)]
        [TestCase(1, 0, 0L)]
        [TestCase(0, 1, 0L)]
        [TestCase(1, 1, 1L)]
        [TestCase(2, 3, 4L)]
        [TestCase(255, 254, 253L)]
        [TestCase(1024, 2048, 4096L)]
        [TestCase(32768, 32767, 65535L)]
        [TestCase(65535, 65535, 4294967295L)]
        public void RecordRoundTripRetainsAllScalarFields(int x, int y, long tile) { ushort xRaw = checked((ushort)x); ushort yRaw = checked((ushort)y); uint tileRaw = checked((uint)tile); IsoMapPack5RecordRaw r = Read(Record(xRaw, yRaw, tileRaw, 17, 18, 19)).Records[0]; Assert.That(r.XRawU16LittleEndian, Is.EqualTo(xRaw)); Assert.That(r.YRawU16LittleEndian, Is.EqualTo(yRaw)); Assert.That(r.TileRawU32LittleEndian, Is.EqualTo(tileRaw)); Assert.That(r.SubTileRaw, Is.EqualTo(17)); Assert.That(r.LevelRaw, Is.EqualTo(18)); Assert.That(r.TailRaw, Is.EqualTo(19)); }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        public void PreservedTrailingLengthIsExact(int length) { byte[] trailing = Enumerable.Range(0, length).Select(x => (byte)x).ToArray(); IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), trailing), IsoMapPack5TrailingPolicy.PreserveRemainderWithDiagnostic); Assert.That(r.Trailing.Bytes, Is.EqualTo(trailing)); }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(10, 20)]
        [TestCase(100, 200)]
        public void ExplicitDomainsUseBothAxes(int x, int y) { ushort xRaw = checked((ushort)x); ushort yRaw = checked((ushort)y); IsoMapCoordinateAnalysis a = Analyze(Record(xRaw, yRaw, 0, 0, 0, 0), profile: new IsoMapCoordinateValidationProfile(width: 300, height: 300)); Assert.That(a.Index.Occurrences[0].Key, Is.EqualTo(new IsoMapCoordinateKey(xRaw, yRaw))); }

        [Test]
        public void EmptyTrailingDataIsNull() { Assert.That(Read(Array.Empty<byte>()).Trailing, Is.Null); }

        [Test]
        public void ExactMultipleHasNoTrailingClassification() { Assert.That(Read(Record(1, 2, 3, 4, 5, 6)).Trailing, Is.Null); }

        [Test]
        public void FourZeroTrailerIsRejectedByDefault() { IsoMapPack5RecordReadResult r = Read(Concat(Record(1, 1, 1, 1, 1, 1), 0, 0, 0, 0)); Assert.That(HasDiagnostic(r, IsoMapDiagnosticCode.UnexpectedTrailingBytes), Is.True); }

        [Test]
        public void PreservePolicyAddsWarningOnly() { IsoMapPack5RecordReadResult r = Read(new byte[] { 1, 2, 3 }, IsoMapPack5TrailingPolicy.PreserveRemainderWithDiagnostic); Assert.That(r.Diagnostics.Single().Severity, Is.EqualTo(BinaryDiagnosticSeverity.Warning)); }

        [Test]
        public void TrailingBytesAreDefensiveCopy() { byte[] input = Concat(Record(1, 1, 1, 1, 1, 1), 4); IsoMapPack5TrailingData trailing = Read(input, IsoMapPack5TrailingPolicy.PreserveRemainderWithDiagnostic).Trailing; input[11] = 9; Assert.That(trailing.Bytes, Is.EqualTo(new byte[] { 4 })); }

        [Test]
        public void ModifyingReturnedTrailingBytesDoesNotChangeResult() { IsoMapPack5TrailingData trailing = Read(Concat(Record(1, 1, 1, 1, 1, 1), new byte[] { 4 }), IsoMapPack5TrailingPolicy.PreserveRemainderWithDiagnostic).Trailing; byte[] returned = trailing.Bytes; returned[0] = 9; Assert.That(trailing.Bytes, Is.EqualTo(new byte[] { 4 })); }

        [Test]
        public void RecordProvenanceChainIsImmutableView() { IsoMapPack5RecordRaw r = Read(Record(1, 1, 1, 1, 1, 1)).Records[0]; Assert.That(r.Provenance, Has.Count.EqualTo(1)); }

        [Test]
        public void CoordinateDuplicateGroupRetainsFirstOrdinal() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(2, 3, 0, 0, 0, 0), Record(2, 3, 0, 0, 0, 1))); Assert.That(a.Index.DuplicateGroups[0].Occurrences[1].FirstOccurrenceOrdinal, Is.EqualTo(0)); }

        [Test]
        public void CoordinateConflictingGroupRetainsBothPayloads() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(2, 3, 0, 0, 0, 0), Record(2, 3, 0, 0, 0, 1))); Assert.That(a.Index.DuplicateGroups[0].Occurrences, Has.Count.EqualTo(2)); }

        [Test]
        public void AllowIdenticalDuplicatesStillDiagnoses() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(2, 3, 0, 0, 0, 0), Record(2, 3, 0, 0, 0, 0)), IsoMapCoordinateDuplicatePolicy.AllowByteIdenticalDuplicatesButDiagnose); Assert.That(HasDiagnostic(a, IsoMapDiagnosticCode.DuplicateCoordinate), Is.True); }

        [TestCase(0x01020304u, 0, 0, 0)]
        [TestCase(0u, 0x11, 0, 0)]
        [TestCase(0u, 0, 0x22, 0)]
        [TestCase(0u, 0, 0, 0x33)]
        public void AllowIdenticalPolicyRejectsEachConflictingPayloadField(uint tile, byte sub, byte level, byte tail) { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(2, 3, 0, 0, 0, 0), Record(2, 3, tile, sub, level, tail)), IsoMapCoordinateDuplicatePolicy.AllowByteIdenticalDuplicatesButDiagnose); Assert.That(a.IsSuccess, Is.False); Assert.That(a.Index.DuplicateGroups.Single().ConflictingPayload, Is.True); }

        [Test]
        public void SignednessCandidateIsRecordedByProfile() { var profile = new IsoMapCoordinateValidationProfile(signedness: IsoMapCoordinateSignednessCandidate.Signed16Candidate); Assert.That(profile.Signedness, Is.EqualTo(IsoMapCoordinateSignednessCandidate.Signed16Candidate)); }

        [Test]
        public void CoordinateAnalysisKeepsSparseSourceOrder() { IsoMapCoordinateAnalysis a = Analyze(Concat(Record(7, 8, 0, 0, 0, 0), Record(1, 2, 0, 0, 0, 0))); Assert.That(a.Index.Occurrences.Select(x => x.SourceOrdinal), Is.EqualTo(new[] { 0, 1 })); }

        [Test]
        public void PackedAdapterRejectsNullBackend() { IsoMapPack5PackedReadResult r = PackedRead(null); Assert.That(r.IsSuccess, Is.False); Assert.That(HasDiagnostic(r, IsoMapDiagnosticCode.BackendUnavailable), Is.True); Assert.That(r.Records, Is.Null); Assert.That(r.Coordinates, Is.Null); }

        [Test]
        public void PackedAdapterRetainsCoordinateAnalysis() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6))); Assert.That(r.Coordinates.Index.Occurrences, Has.Count.EqualTo(1)); }

        [Test]
        public void PackedAdapterUsesRecordTrailingPolicy() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Concat(Record(1, 2, 3, 4, 5, 6), 7))); Assert.That(r.Records.Trailing.Classification, Is.EqualTo(IsoMapTrailingClassification.RejectedRemainder)); }

        [Test]
        public void PackedAdapterSourceOffsetStartsAtZero() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6))); Assert.That(r.Records.Records[0].SourceOffset, Is.EqualTo(0)); }

        [Test]
        public void PackedAdapterDoesNotInventTileMeaning() { IsoMapPack5RecordRaw r = PackedRead(new FakeBackend(Record(1, 2, 0x12345678, 4, 5, 6))).Records.Records[0]; Assert.That(r.TileRawU32LittleEndian, Is.EqualTo(0x12345678u)); Assert.That(r.TileLowU16LittleEndian, Is.EqualTo(0x5678)); Assert.That(r.TileHighU16LittleEndian, Is.EqualTo(0x1234)); }

        [Test]
        public void PackedAdapterProvenanceFlowsThroughCoordinateStage() { IsoMapPack5PackedReadResult r = PackedRead(new FakeBackend(Record(1, 2, 3, 4, 5, 6))); Assert.That(r.Coordinates.Index.Occurrences[0].Record.Provenance[0].SourceId, Is.EqualTo("synthetic")); }

        private static IsoMapPack5RecordReadResult Read(byte[] bytes, IsoMapPack5TrailingPolicy policy = IsoMapPack5TrailingPolicy.RejectAnyRemainder, IsoMapPack5ReadLimits limits = null, long absoluteOffset = 0)
            => new IsoMapPack5RecordReader().Read(bytes, policy, limits, absoluteOffset, Source());

        private static IsoMapCoordinateAnalysis Analyze(byte[] bytes, IsoMapCoordinateDuplicatePolicy duplicatePolicy = IsoMapCoordinateDuplicatePolicy.PreserveAllAndDiagnose, IsoMapCoordinateValidationProfile profile = null, IsoMapPack5ReadLimits limits = null)
            => new IsoMapCoordinateIndexer().Analyze(Read(bytes).Records, duplicatePolicy, profile, limits, Source());

        private static IsoMapPack5PackedReadResult PackedRead(ILzoDecodeBackend backend, string base64 = null, string section = "IsoMapPack5", int? declaredOutputLength = null, IsoMapPack5ReadLimits limits = null, IsoMapPack5TrailingPolicy trailingPolicy = IsoMapPack5TrailingPolicy.RejectAnyRemainder, IsoMapCoordinateDuplicatePolicy duplicatePolicy = IsoMapCoordinateDuplicatePolicy.PreserveAllAndDiagnose, IsoMapCoordinateValidationProfile profile = null)
        {
            byte[] decoded = backend is FakeBackend fake && fake.Bytes != null ? fake.Bytes : Record(1, 2, 3, 4, 5, 6);
            string value = base64 ?? Convert.ToBase64String(Chunk(decoded, declaredOutputLength));
            return new IsoMapPack5PackedSectionReader().Read(new[] { Occurrence(section, "1", value) }, PackedPolicy(ChunkSentinelPolicy.RejectAllZero, limits, trailingPolicy, duplicatePolicy, profile), backend);
        }

        private static IsoMapPack5PackedReadPolicy PackedPolicy(
            ChunkSentinelPolicy sentinelPolicy = ChunkSentinelPolicy.RejectAllZero,
            IsoMapPack5ReadLimits limits = null,
            IsoMapPack5TrailingPolicy trailingPolicy = IsoMapPack5TrailingPolicy.RejectAnyRemainder,
            IsoMapCoordinateDuplicatePolicy duplicatePolicy = IsoMapCoordinateDuplicatePolicy.PreserveAllAndDiagnose,
            IsoMapCoordinateValidationProfile profile = null)
            => new IsoMapPack5PackedReadPolicy(
                new PackedSectionDecodePolicy(
                    PackedIniFragmentOrderingPolicy.SourceOccurrenceOrder,
                    StrictBase64Policy.StandardAlphabetNoWhitespace,
                    sentinelPolicy,
                    PackedCodecKind.RawLzo1X),
                trailingPolicy,
                duplicatePolicy,
                profile,
                limits);

        private static byte[] Chunk(byte[] decoded, int? declaredOutputLength = null) { int outputLength = declaredOutputLength ?? decoded.Length; byte[] result = new byte[4 + decoded.Length]; result[0] = (byte)decoded.Length; result[1] = (byte)(decoded.Length >> 8); result[2] = (byte)outputLength; result[3] = (byte)(outputLength >> 8); Buffer.BlockCopy(decoded, 0, result, 4, decoded.Length); return result; }
        private static byte[] Record(ushort x, ushort y, uint tile, byte sub, byte level, byte tail) { var bytes = new byte[11]; bytes[0] = (byte)x; bytes[1] = (byte)(x >> 8); bytes[2] = (byte)y; bytes[3] = (byte)(y >> 8); bytes[4] = (byte)tile; bytes[5] = (byte)(tile >> 8); bytes[6] = (byte)(tile >> 16); bytes[7] = (byte)(tile >> 24); bytes[8] = sub; bytes[9] = level; bytes[10] = tail; return bytes; }
        private static byte[] Concat(params byte[][] arrays) { int length = arrays.Sum(x => x.Length); var result = new byte[length]; int offset = 0; foreach (byte[] array in arrays) { Buffer.BlockCopy(array, 0, result, offset, array.Length); offset += array.Length; } return result; }
        private static byte[] Concat(byte[] first, params byte[] trailing) => Concat(new[] { first }.Concat(new[] { trailing }).ToArray());
        private static PackedIniFragmentOccurrence Occurrence(string section, string key, string value) => new PackedIniFragmentOccurrence(section, key, value, 0, "synthetic", 0, new IniSourceProvenance("synthetic", new[] { LogicalContentPath.Parse("synthetic.ini") }));
        private static BinarySourceContext Source() => new BinarySourceContext("isomap-pack5-tests", "synthetic", LogicalContentPath.Parse("synthetic-pack5.bin"));
        private static bool HasDiagnostic(IsoMapPack5RecordReadResult result, IsoMapDiagnosticCode code) => result.Diagnostics.Any(x => x.Code == code);
        private static bool HasDiagnostic(IsoMapCoordinateAnalysis result, IsoMapDiagnosticCode code) => result.Diagnostics.Any(x => x.Code == code);
        private static bool HasDiagnostic(IsoMapPack5PackedReadResult result, IsoMapDiagnosticCode code) => result.Diagnostics.Any(x => x.Code == code);

        private static IEnumerable<IsoMapPack5RecordRaw> NullThenThrow()
        {
            yield return null;
            throw new AssertionException("The coordinate analyzer continued after a null record.");
        }

        private sealed class FakeBackend : ILzoDecodeBackend
        {
            internal readonly byte[] Bytes;
            private readonly int consumed;
            internal FakeBackend(byte[] bytes, int consumed = -1) { Bytes = bytes; this.consumed = consumed; }
            public LzoDecodeResult Decode(LzoDecodeRequest request) { if (Bytes == null) return null; return new LzoDecodeResult(Bytes, consumed < 0 ? request.Compressed.Length : consumed, "fake-lzo", Array.Empty<PackedMapDiagnostic>(), request.SourceProvenance); }
        }

        private sealed class ShortReadStream : MemoryStream
        {
            private readonly int maxRead;
            internal ShortReadStream(byte[] bytes, int maxRead) : base(bytes) { this.maxRead = maxRead; }
            public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, maxRead));
        }
    }
}
