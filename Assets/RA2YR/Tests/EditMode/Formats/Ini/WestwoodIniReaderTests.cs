using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Tests.EditMode.Formats.Ini
{
    public sealed class WestwoodIniReaderTests
    {
        [Test]
        public void EmptyFileIsACompleteZeroLineDocument()
        {
            IniRawDocument document = AssertSuccess(Read(Array.Empty<byte>()));

            Assert.That(document.OriginalLength, Is.Zero);
            Assert.That(document.Lines, Is.Empty);
            Assert.That(document.Nodes, Is.Empty);
            Assert.That(document.ByteOrderMarkKind, Is.EqualTo(IniByteOrderMarkKind.None));
            Assert.That(document.Completeness, Is.EqualTo(IniDocumentCompleteness.Structured));
        }

        [Test]
        public void OnlyUtf8BomIsPreservedWithoutInventingALine()
        {
            byte[] input = { 0xef, 0xbb, 0xbf };
            IniRawDocument document = AssertSuccess(Read(input));

            Assert.That(document.ByteOrderMarkKind, Is.EqualTo(IniByteOrderMarkKind.Utf8));
            Assert.That(document.ByteOrderMark.ToArray(), Is.EqualTo(input));
            Assert.That(document.Lines, Is.Empty);
        }

        [Test]
        public void SingleSectionPreservesItsRawAndTrimmedName()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("  [ Section ]  "));
            var section = (IniSectionNode)document.Nodes.Single();

            Assert.That(Ascii(section.RawName), Is.EqualTo(" Section "));
            Assert.That(Ascii(section.Name), Is.EqualTo("Section"));
            Assert.That(Ascii(section.Line.Content), Is.EqualTo("  [ Section ]  "));
        }

        [Test]
        public void SingleKeyValueBelongsToItsPhysicalSection()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\nK=V"));
            var section = (IniSectionNode)document.Nodes[0];
            var keyValue = (IniKeyValueNode)document.Nodes[1];

            Assert.That(keyValue.ContainingSectionLineId, Is.EqualTo(section.PhysicalLineId));
            Assert.That(Ascii(keyValue.Key), Is.EqualTo("K"));
            Assert.That(Ascii(keyValue.Value), Is.EqualTo("V"));
        }

        [Test]
        public void MultipleSectionsRetainFileOrder()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[B]\nK=1\n[A]\nK=2"));

            Assert.That(document.Nodes.Select(node => node.Kind), Is.EqualTo(new[]
            {
                IniNodeKind.Section,
                IniNodeKind.KeyValue,
                IniNodeKind.Section,
                IniNodeKind.KeyValue
            }));
            Assert.That(Ascii(((IniSectionNode)document.Nodes[0]).Name), Is.EqualTo("B"));
            Assert.That(Ascii(((IniSectionNode)document.Nodes[2]).Name), Is.EqualTo("A"));
        }

        [Test]
        public void DuplicateSectionsRemainSeparateOrderedNodes()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\nA=1\n[S]\nA=2"));

            Assert.That(document.FindSectionsByTrimmedRawAsciiName(
                "S",
                IniRawAsciiComparison.Ordinal), Has.Count.EqualTo(2));
        }

        [Test]
        public void DuplicateKeysRemainSeparateOrderedCandidates()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\nK=1\nK=2"));

            IReadOnlyList<IniKeyValueNode> candidates =
                document.FindKeyValuesByTrimmedRawAsciiKey(
                    "K",
                    IniRawAsciiComparison.Ordinal);
            Assert.That(candidates, Has.Count.EqualTo(2));
            Assert.That(candidates.Select(value => Ascii(value.Value)),
                Is.EqualTo(new[] { "1", "2" }));
        }

        [Test]
        public void SectionAndKeyCaseAreNeverFolded()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[Section]\nKey=1\nkey=2"));

            Assert.That(document.FindSectionsByTrimmedRawAsciiName(
                "section",
                IniRawAsciiComparison.Ordinal), Is.Empty);
            Assert.That(document.FindKeyValuesByTrimmedRawAsciiKey(
                "Key",
                IniRawAsciiComparison.Ordinal), Has.Count.EqualTo(1));
            Assert.That(document.FindKeyValuesByTrimmedRawAsciiKey(
                "key",
                IniRawAsciiComparison.Ordinal), Has.Count.EqualTo(1));
        }

        [Test]
        public void EmptyValueIsAZeroLengthRawSlice()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\nK="));
            var value = (IniKeyValueNode)document.Nodes[1];

            Assert.That(value.Value.Length, Is.Zero);
        }

        [Test]
        public void AdditionalEqualsSignsRemainInTheValue()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\nK=A=B=C"));

            Assert.That(Ascii(((IniKeyValueNode)document.Nodes[1]).Value),
                Is.EqualTo("A=B=C"));
        }

        [Test]
        public void LeadingWhitespaceIsKeptAsAnIndependentRawSlice()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\n \tK=V"));
            var node = (IniKeyValueNode)document.Nodes[1];

            Assert.That(Ascii(node.LeadingWhitespace), Is.EqualTo(" \t"));
            Assert.That(Ascii(node.Line.Content), Is.EqualTo(" \tK=V"));
        }

        [Test]
        public void TrailingWhitespaceRemainsPartOfTheValueAndRawLine()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\nK=V \t"));
            var node = (IniKeyValueNode)document.Nodes[1];

            Assert.That(Ascii(node.Value), Is.EqualTo("V \t"));
            Assert.That(Ascii(node.Line.Content), Is.EqualTo("K=V \t"));
        }

        [Test]
        public void EqualsWhitespaceIsPreservedOnBothSides()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\nKey \t= \tValue"));
            var node = (IniKeyValueNode)document.Nodes[1];

            Assert.That(Ascii(node.Key), Is.EqualTo("Key"));
            Assert.That(Ascii(node.WhitespaceBeforeEquals), Is.EqualTo(" \t"));
            Assert.That(Ascii(node.WhitespaceAfterEquals), Is.EqualTo(" \t"));
            Assert.That(Ascii(node.Value), Is.EqualTo("Value"));
        }

        [Test]
        public void LeadingSemicolonCreatesAFullLineComment()
        {
            IniRawDocument document = AssertSuccess(ReadAscii(" \t;comment"));
            var comment = (IniCommentNode)document.Nodes.Single();

            Assert.That(Ascii(comment.Body), Is.EqualTo("comment"));
        }

        [Test]
        public void InlineSemicolonIsNotTruncatedAndProducesAmbiguityWarning()
        {
            IniParseResult result = ReadAscii("[S]\nK=value;possibly-comment");
            IniRawDocument document = AssertSuccess(result);
            var node = (IniKeyValueNode)document.Nodes[1];

            Assert.That(Ascii(node.Value), Is.EqualTo("value;possibly-comment"));
            Assert.That(result.Diagnostics.Any(diagnostic =>
                diagnostic.Code == IniDiagnosticCode.AmbiguousInlineSemicolon &&
                diagnostic.LineIndex == 1), Is.True);
        }

        [Test]
        public void EmptyAndWhitespaceOnlyLinesAreBlankNodes()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("\n \t\r"));

            Assert.That(document.Nodes.Select(node => node.Kind),
                Is.EqualTo(new[] { IniNodeKind.Blank, IniNodeKind.Blank }));
        }

        [Test]
        public void CrLfIsRetainedPerPhysicalLine()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\r\nK=V\r\n"));

            Assert.That(document.Lines.All(line =>
                line.EndingKind == IniLineEnding.CarriageReturnLineFeed), Is.True);
            Assert.That(document.Lines.Select(line => line.Ending.Length),
                Is.EqualTo(new[] { 2, 2 }));
        }

        [Test]
        public void LfIsRetainedPerPhysicalLine()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\nK=V\n"));

            Assert.That(document.Lines.All(line =>
                line.EndingKind == IniLineEnding.LineFeed), Is.True);
        }

        [Test]
        public void CrIsRetainedPerPhysicalLine()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\rK=V\r"));

            Assert.That(document.Lines.All(line =>
                line.EndingKind == IniLineEnding.CarriageReturn), Is.True);
        }

        [Test]
        public void MixedLineEndingsRemainAttachedToTheirOriginalLines()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\r\nA=1\nB=2\rC=3"));

            Assert.That(document.Lines.Select(line => line.EndingKind), Is.EqualTo(new[]
            {
                IniLineEnding.CarriageReturnLineFeed,
                IniLineEnding.LineFeed,
                IniLineEnding.CarriageReturn,
                IniLineEnding.None
            }));
        }

        [Test]
        public void FinalLineWithoutEndingIsRepresentedExplicitly()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\r\nK=V"));

            Assert.That(document.Lines.Last().EndingKind, Is.EqualTo(IniLineEnding.None));
            Assert.That(document.Lines.Last().HasLineEnding, Is.False);
        }

        [Test]
        public void MultipleFinalEmptyLinesAreNotCollapsed()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\r\n\r\n\r\n"));

            Assert.That(document.Lines, Has.Count.EqualTo(3));
            Assert.That(document.Nodes.Count(node => node.Kind == IniNodeKind.Blank),
                Is.EqualTo(2));
        }

        [Test]
        public void UnknownDirectiveIsAnOpaquePreservedLine()
        {
            IniParseResult result = ReadAscii("!mod-directive value");
            IniRawDocument document = AssertSuccess(result);

            Assert.That(document.Nodes.Single().Kind, Is.EqualTo(IniNodeKind.Opaque));
            Assert.That(((IniOpaqueNode)document.Nodes.Single()).Reason,
                Is.EqualTo(IniOpaqueReason.MissingEquals));
            Assert.That(document.Completeness,
                Is.EqualTo(IniDocumentCompleteness.StructuredWithOpaqueLines));
            Assert.That(Ascii(document.Lines.Single().Content),
                Is.EqualTo("!mod-directive value"));
        }

        [Test]
        public void KeyOutsideSectionIsOpaqueRatherThanAssignedGlobalSemantics()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("K=V"));
            var opaque = (IniOpaqueNode)document.Nodes.Single();

            Assert.That(opaque.Reason, Is.EqualTo(IniOpaqueReason.KeyOutsideSection));
        }

        [Test]
        public void LineWithoutEqualsIsOpaque()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\nunknown"));

            Assert.That(((IniOpaqueNode)document.Nodes[1]).Reason,
                Is.EqualTo(IniOpaqueReason.MissingEquals));
        }

        [Test]
        public void UnterminatedSectionIsOpaqueAndClearsSectionAssociation()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[A]\n[B\nK=V"));

            Assert.That(((IniOpaqueNode)document.Nodes[1]).Reason,
                Is.EqualTo(IniOpaqueReason.UnterminatedSection));
            Assert.That(((IniOpaqueNode)document.Nodes[2]).Reason,
                Is.EqualTo(IniOpaqueReason.KeyOutsideSection));
        }

        [Test]
        public void SectionTrailingUnconfirmedContentIsOpaque()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S] trailing"));

            Assert.That(((IniOpaqueNode)document.Nodes.Single()).Reason,
                Is.EqualTo(IniOpaqueReason.SectionTrailingContent));
        }

        [Test]
        public void NonAsciiBytesRemainRawWithoutHostCodePageDecoding()
        {
            byte[] prefix = Encoding.ASCII.GetBytes("[S]\nK=");
            byte[] input = prefix.Concat(new byte[] { 0x80, 0xfe }).ToArray();
            IniRawDocument document = AssertSuccess(Read(input));

            Assert.That(((IniKeyValueNode)document.Nodes[1]).Value.ToArray(),
                Is.EqualTo(new byte[] { 0x80, 0xfe }));
            Assert.Throws<DecoderFallbackException>(() =>
                IniTextEncodingPolicy.StrictAscii.Decode(
                    ((IniKeyValueNode)document.Nodes[1]).Value));
        }

        [Test]
        public void Utf8BomUsesStrictUtf8ButKeepsRawBytes()
        {
            byte[] input = WithPreamble(
                new UTF8Encoding(true, true),
                "[节]\n键=值");
            IniRawDocument document = AssertSuccess(Read(input));

            Assert.That(document.PhysicalEncoding,
                Is.EqualTo(IniPhysicalEncodingKind.Utf8WithBom));
            var key = (IniKeyValueNode)document.Nodes[1];
            Assert.That(IniTextEncodingPolicy.StrictUtf8.Decode(key.Key), Is.EqualTo("键"));
            Assert.That(document.CopyOriginalBytes(), Is.EqualTo(input));
        }

        [Test]
        public void Utf16LittleEndianBomUsesCodeUnitAwareSyntaxScanning()
        {
            byte[] input = WithPreamble(
                new UnicodeEncoding(false, true, true),
                "[节]\r\n键 = 值");
            IniRawDocument document = AssertSuccess(Read(input));

            Assert.That(document.PhysicalEncoding,
                Is.EqualTo(IniPhysicalEncodingKind.Utf16LittleEndianWithBom));
            Assert.That(document.Lines[0].Ending.Length, Is.EqualTo(4));
            var key = (IniKeyValueNode)document.Nodes[1];
            Assert.That(IniTextEncodingPolicy.StrictUtf16LittleEndian.Decode(key.Key),
                Is.EqualTo("键"));
        }

        [Test]
        public void Utf16BomWithOddPayloadLengthFailsClosed()
        {
            byte[] input = { 0xff, 0xfe, 0x41 };

            AssertFailure(Read(input), IniDiagnosticCode.ByteOrderMarkLengthConflict);
        }

        [Test]
        public void NulByteFailsInsteadOfBeingDiscarded()
        {
            byte[] input = Encoding.ASCII.GetBytes("[S]\nK=V")
                .Concat(new byte[] { 0 })
                .ToArray();

            AssertFailure(Read(input), IniDiagnosticCode.NulCharacter);
        }

        [Test]
        public void LongLineBudgetFailsAtTheLineStart()
        {
            IniReadLimits limits = Limits(maxLineBytes: 3);
            IniDiagnostic diagnostic = AssertFailure(
                Read(Encoding.ASCII.GetBytes("[Long]"), limits),
                IniDiagnosticCode.LineLengthBudgetExceeded);

            Assert.That(diagnostic.AbsoluteOffset, Is.Zero);
            Assert.That(diagnostic.LineIndex, Is.Zero);
        }

        [Test]
        public void LineCountBudgetFailsBeforeCreatingAnExtraNode()
        {
            IniDiagnostic diagnostic = AssertFailure(
                Read(Encoding.ASCII.GetBytes("[S]\nK=V"), Limits(maxLineCount: 1)),
                IniDiagnosticCode.LineCountBudgetExceeded);

            Assert.That(diagnostic.LineIndex, Is.EqualTo(1));
        }

        [Test]
        public void TotalNodeBudgetIsIndependentFromLineBudget()
        {
            IniDiagnostic diagnostic = AssertFailure(
                Read(Encoding.ASCII.GetBytes("[S]\nK=V"), Limits(maxTotalNodes: 1)),
                IniDiagnosticCode.TotalNodeBudgetExceeded);

            Assert.That(diagnostic.LineIndex, Is.EqualTo(1));
        }

        [Test]
        public void AllocationBudgetIncludesOwnedSnapshotAndModel()
        {
            byte[] input = Encoding.ASCII.GetBytes("[S]");
            IniDiagnostic diagnostic = AssertFailure(
                Read(input, Limits(maxAllocatedBytes: input.Length)),
                IniDiagnosticCode.AllocationBudgetExceeded);

            Assert.That(diagnostic.BinaryCode,
                Is.EqualTo(BinaryDiagnosticCode.AllocationBudgetExceeded));
        }

        [Test]
        public void MemoryInputIsSnapshottedBeforeCallerMutation()
        {
            byte[] input = Encoding.ASCII.GetBytes("[S]\nK=V");
            IniRawDocument document = AssertSuccess(Read(input));
            input[1] = (byte)'X';

            Assert.That(Ascii(((IniSectionNode)document.Nodes[0]).Name), Is.EqualTo("S"));
        }

        [Test]
        public void SeekableStreamProducesTheSameCanonicalModel()
        {
            byte[] input = Encoding.ASCII.GetBytes("[S]\r\nK=V");
            using (var stream = new MemoryStream(input, false))
            {
                IniRawDocument streamDocument = AssertSuccess(WestwoodIniReader.ReadSeekable(
                    stream,
                    Source(),
                    Provenance(),
                    leaveOpen: true));
                Assert.That(streamDocument.CanonicalModelSha256,
                    Is.EqualTo(AssertSuccess(Read(input)).CanonicalModelSha256));
                Assert.That(stream.CanRead, Is.True);
            }
        }

        [Test]
        public void MultipleShortReadsProduceTheSameCanonicalModel()
        {
            byte[] input = Encoding.ASCII.GetBytes("[S]\r\nK=V\r\n");
            using (var stream = new ShortReadStream(input, 2))
            {
                IniRawDocument document = AssertSuccess(WestwoodIniReader.Read(
                    stream,
                    input.Length,
                    Source(),
                    Provenance(),
                    leaveOpen: true));
                Assert.That(document.CanonicalModelSha256,
                    Is.EqualTo(AssertSuccess(Read(input)).CanonicalModelSha256));
                Assert.That(stream.ReadCalls, Is.GreaterThan(1));
            }
        }

        [Test]
        public void MixEntryWindowProducesTheSameModelWithinItsParentBoundary()
        {
            byte[] input = Encoding.ASCII.GetBytes("[S]\r\nK=V");
            MixArchiveReadResult mix = ReadSyntheticMix(input);
            Assert.That(mix.IsSuccess, Is.True);
            using (mix.Archive)
            {
                IniRawDocument document = AssertSuccess(WestwoodIniReader.Read(
                    mix.Archive.Entries.Single().OpenPayloadWindow(),
                    Source(),
                    Provenance()));
                Assert.That(document.CanonicalModelSha256,
                    Is.EqualTo(AssertSuccess(Read(input)).CanonicalModelSha256));
                Assert.That(document.Lines[0].AbsoluteOffset,
                    Is.EqualTo(mix.Archive.Entries.Single().PayloadAbsoluteOffset));
            }
        }

        [Test]
        public void MemoryStreamAndWindowModelsAreEquivalent()
        {
            byte[] input = WithPreamble(new UTF8Encoding(true, true), "[S]\nK=值");
            IniRawDocument memory = AssertSuccess(Read(input));
            using (var stream = new MemoryStream(input, false))
            {
                IniRawDocument fromStream = AssertSuccess(WestwoodIniReader.Read(
                    stream,
                    input.Length,
                    Source(),
                    Provenance(),
                    leaveOpen: true));
                Assert.That(fromStream.CanonicalModelSha256,
                    Is.EqualTo(memory.CanonicalModelSha256));
            }
        }

        [Test]
        public void RawOffsetsAndLengthsAreExactWithNonzeroAbsoluteBase()
        {
            byte[] input = Encoding.ASCII.GetBytes("[S]\r\n K \t= V");
            IniRawDocument document = AssertSuccess(WestwoodIniReader.Read(
                input,
                Source(),
                Provenance(),
                absoluteStartOffset: 100));
            var key = (IniKeyValueNode)document.Nodes[1];

            Assert.That(document.Lines[0].AbsoluteOffset, Is.EqualTo(100));
            Assert.That(document.Lines[1].AbsoluteOffset, Is.EqualTo(105));
            Assert.That(key.Key.Offset, Is.EqualTo(6));
            Assert.That(key.Key.Length, Is.EqualTo(1));
            Assert.That(key.EqualsByteOffset, Is.EqualTo(9));
        }

        [Test]
        public void SectionScopedDuplicateQueryReturnsEveryCandidateWithoutOverwrite()
        {
            IniRawDocument document = AssertSuccess(ReadAscii(
                "[A]\nK=1\nK=2\n[B]\nK=3"));
            var sectionA = (IniSectionNode)document.Nodes[0];

            IReadOnlyList<IniKeyValueNode> values =
                document.FindKeyValuesByTrimmedRawAsciiKey(
                    sectionA,
                    "K",
                    IniRawAsciiComparison.Ordinal);
            Assert.That(values.Select(value => Ascii(value.Value)),
                Is.EqualTo(new[] { "1", "2" }));
        }

        [Test]
        public void ModelCollectionsAndRawCopiesCannotMutateTheDocument()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[S]\nK=V"));
            byte[] copy = document.Lines[0].Content.ToArray();
            copy[1] = (byte)'X';

            Assert.That(Ascii(((IniSectionNode)document.Nodes[0]).Name), Is.EqualTo("S"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<IniNode>)document.Nodes)[0] = document.Nodes[1]);
            Assert.That(typeof(IniRawDocument).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance), Is.Empty);
            Assert.That(typeof(IniParseResult).GetConstructors(
                BindingFlags.Public | BindingFlags.Instance), Is.Empty);
        }

        [Test]
        public void Utf8DecoderDoesNotNormalizeText()
        {
            string decomposed = "e\u0301";
            byte[] input = WithPreamble(
                new UTF8Encoding(true, true),
                "[S]\nK=" + decomposed);
            IniRawDocument document = AssertSuccess(Read(input));
            string decoded = IniTextEncodingPolicy.StrictUtf8.Decode(
                ((IniKeyValueNode)document.Nodes[1]).Value);

            Assert.That(decoded, Is.EqualTo(decomposed));
            Assert.That(decoded, Is.Not.EqualTo(decoded.Normalize()));
        }

        [Test]
        public void InvalidBomDeclaredUtf8FailsWithoutReplacementCharacters()
        {
            byte[] input = { 0xef, 0xbb, 0xbf, 0xc3, 0x28 };

            AssertFailure(Read(input), IniDiagnosticCode.InvalidEncoding);
        }

        [Test]
        public void EmptySectionNameIsOpaque()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[ \t]"));

            Assert.That(((IniOpaqueNode)document.Nodes.Single()).Reason,
                Is.EqualTo(IniOpaqueReason.EmptySectionName));
        }

        [Test]
        public void SectionTrailingSemicolonIsOpaqueUntilSemanticsAreConfirmed()
        {
            IniParseResult result = ReadAscii("[S] ;ambiguous");
            IniRawDocument document = AssertSuccess(result);

            Assert.That(document.Nodes.Single().Kind, Is.EqualTo(IniNodeKind.Opaque));
            Assert.That(((IniOpaqueNode)document.Nodes.Single()).Reason,
                Is.EqualTo(IniOpaqueReason.SectionTrailingContent));
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo(IniDiagnosticCode.OpaqueLinePreserved));
        }

        [Test]
        public void UnsupportedControlCharacterMakesLineOpaqueWithoutDataLoss()
        {
            byte[] input = Encoding.ASCII.GetBytes("[S]\nK=A")
                .Concat(new byte[] { 0x01 })
                .ToArray();
            IniRawDocument document = AssertSuccess(Read(input));

            Assert.That(((IniOpaqueNode)document.Nodes[1]).Reason,
                Is.EqualTo(IniOpaqueReason.UnsupportedControlCharacter));
            Assert.That(document.CopyOriginalBytes(), Is.EqualTo(input));
        }

        [Test]
        public void CategoryBudgetsFailIndependently()
        {
            AssertFailure(
                Read(Encoding.ASCII.GetBytes("[S]"), Limits(maxSectionNodes: 0)),
                IniDiagnosticCode.SectionBudgetExceeded);
            AssertFailure(
                Read(Encoding.ASCII.GetBytes("[S]\nK=V"), Limits(maxKeyValueNodes: 0)),
                IniDiagnosticCode.KeyValueBudgetExceeded);
            AssertFailure(
                Read(Encoding.ASCII.GetBytes(";C"), Limits(maxCommentNodes: 0)),
                IniDiagnosticCode.CommentBudgetExceeded);
            AssertFailure(
                Read(Encoding.ASCII.GetBytes("opaque"), Limits(maxOpaqueNodes: 0)),
                IniDiagnosticCode.OpaqueBudgetExceeded);
        }

        [Test]
        public void DiagnosticsContainLogicalContextButNeverRawLineText()
        {
            const string secret = "private-body-token";
            IniParseResult result = ReadAscii(secret);

            Assert.That(result.IsSuccess, Is.True);
            IniDiagnostic diagnostic = result.Diagnostics.Single();
            Assert.That(diagnostic.Source.LogicalPath.Value, Is.EqualTo("synthetic/test.ini"));
            Assert.That(diagnostic.Provenance.LogicalChain, Has.Count.EqualTo(2));
            Assert.That(diagnostic.Message, Does.Not.Contain(secret));
        }

        [Test]
        public void IdentityWriterReproducesMixedLineEndingsByteForByte()
        {
            byte[] input = Encoding.ASCII.GetBytes(
                "[A]\r\nK = V\nopaque\r[B]");
            IniRawDocument document = AssertSuccess(Read(input));

            IniIdentityWriteResult result = IniIdentityWriter.WriteToBytes(
                document,
                input.Length);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.GetBytes(), Is.EqualTo(input));
        }

        [Test]
        public void IdentityWriterRejectsAnOutputOverItsBudget()
        {
            IniRawDocument document = AssertSuccess(ReadAscii("[A]\nK=V"));

            IniIdentityWriteResult result = IniIdentityWriter.WriteToBytes(
                document,
                document.OriginalLength - 1);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo(IniIdentityWriteDiagnosticCode.OutputBudgetExceeded));
            Assert.Throws<InvalidOperationException>(() => result.GetBytes());
        }

        [Test]
        public void IdentityWriterResultReturnsDefensiveCopies()
        {
            byte[] input = Encoding.ASCII.GetBytes("[A]\nK=V");
            IniIdentityWriteResult result = IniIdentityWriter.WriteToBytes(
                AssertSuccess(Read(input)),
                input.Length);
            byte[] first = result.GetBytes();
            first[0] = (byte)'X';

            Assert.That(result.GetBytes(), Is.EqualTo(input));
        }

        [Test]
        public void OpaqueSectionTailDoesNotActivateFollowingKeys()
        {
            IniRawDocument document = AssertSuccess(
                ReadAscii("[Ambiguous] tail\nKey=Value"));

            Assert.That(document.Nodes.Select(node => node.Kind),
                Is.EqualTo(new[] { IniNodeKind.Opaque, IniNodeKind.Opaque }));
            Assert.That(((IniOpaqueNode)document.Nodes[1]).Reason,
                Is.EqualTo(IniOpaqueReason.KeyOutsideSection));
        }

        private static IniParseResult ReadAscii(string value)
        {
            return Read(Encoding.ASCII.GetBytes(value));
        }

        private static IniParseResult Read(byte[] bytes, IniReadLimits limits = null)
        {
            return WestwoodIniReader.Read(bytes, Source(), Provenance(), limits);
        }

        private static IniRawDocument AssertSuccess(IniParseResult result)
        {
            Assert.That(result.IsSuccess, Is.True,
                result.Diagnostics.Count == 0 ? null : result.Diagnostics[0].Message);
            Assert.That(result.Document, Is.Not.Null);
            Assert.That(result.Diagnostics.All(diagnostic =>
                diagnostic.Severity != IniDiagnosticSeverity.Error), Is.True);
            return result.Document;
        }

        private static IniDiagnostic AssertFailure(
            IniParseResult result,
            IniDiagnosticCode expectedCode)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(expectedCode));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(IniDiagnosticSeverity.Error));
            return result.Diagnostics[0];
        }

        private static string Ascii(IniRawSlice slice)
        {
            return IniTextEncodingPolicy.StrictAscii.Decode(slice);
        }

        private static BinarySourceContext Source()
        {
            return new BinarySourceContext(
                "format.ini-byte-document",
                "synthetic-source",
                LogicalContentPath.Parse("synthetic/test.ini"));
        }

        private static IniSourceProvenance Provenance()
        {
            return new IniSourceProvenance(
                "synthetic-source",
                new[]
                {
                    LogicalContentPath.Parse("synthetic.mix"),
                    LogicalContentPath.Parse("test.ini")
                });
        }

        private static IniReadLimits Limits(
            long? maxInputBytes = null,
            long? maxSingleReadBytes = null,
            long? maxLineCount = null,
            long? maxLineBytes = null,
            long? maxSectionNodes = null,
            long? maxKeyValueNodes = null,
            long? maxCommentNodes = null,
            long? maxOpaqueNodes = null,
            long? maxTotalNodes = null,
            long? maxCumulativeRawBytes = null,
            long? maxAllocatedBytes = null)
        {
            IniReadLimits defaults = IniReadLimits.Default;
            return new IniReadLimits(
                maxInputBytes ?? defaults.MaxInputBytes,
                maxSingleReadBytes ?? defaults.MaxSingleReadBytes,
                maxLineCount ?? defaults.MaxLineCount,
                maxLineBytes ?? defaults.MaxLineBytes,
                maxSectionNodes ?? defaults.MaxSectionNodes,
                maxKeyValueNodes ?? defaults.MaxKeyValueNodes,
                maxCommentNodes ?? defaults.MaxCommentNodes,
                maxOpaqueNodes ?? defaults.MaxOpaqueNodes,
                maxTotalNodes ?? defaults.MaxTotalNodes,
                maxCumulativeRawBytes ?? defaults.MaxCumulativeRawBytes,
                maxAllocatedBytes ?? defaults.MaxAllocatedBytes);
        }

        private static byte[] WithPreamble(Encoding encoding, string value)
        {
            byte[] preamble = encoding.GetPreamble();
            byte[] body = encoding.GetBytes(value);
            return preamble.Concat(body).ToArray();
        }

        private static MixArchiveReadResult ReadSyntheticMix(byte[] payload)
        {
            byte[] archive = new byte[checked(18 + payload.Length)];
            WriteUInt16(archive, 0, 1);
            WriteUInt32(archive, 2, checked((uint)payload.Length));
            WriteUInt32(
                archive,
                6,
                MixFileId.ComputeCandidateId("test.ini").Value);
            WriteUInt32(archive, 10, 0);
            WriteUInt32(archive, 14, checked((uint)payload.Length));
            Buffer.BlockCopy(payload, 0, archive, 18, payload.Length);
            return MixArchiveReader.Read(
                archive,
                new BinarySourceContext(
                    "format.mix-container-read",
                    "synthetic-source",
                    LogicalContentPath.Parse("synthetic.mix")),
                new MixReadLimits(
                    1024 * 1024,
                    16,
                    1024,
                    1024 * 1024,
                    1024 * 1024,
                    32,
                    8));
        }

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = (byte)(value & 0xff);
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)(value & 0xff);
            bytes[offset + 1] = (byte)((value >> 8) & 0xff);
            bytes[offset + 2] = (byte)((value >> 16) & 0xff);
            bytes[offset + 3] = (byte)((value >> 24) & 0xff);
        }

        private sealed class ShortReadStream : Stream
        {
            private readonly byte[] bytes;
            private readonly int maximumChunk;
            private int position;

            public ShortReadStream(byte[] bytes, int maximumChunk)
            {
                this.bytes = bytes;
                this.maximumChunk = maximumChunk;
            }

            public int ReadCalls { get; private set; }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadCalls++;
                if (position == bytes.Length)
                {
                    return 0;
                }

                int actual = Math.Min(
                    Math.Min(count, maximumChunk),
                    bytes.Length - position);
                Buffer.BlockCopy(bytes, position, buffer, offset, actual);
                position += actual;
                return actual;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }

}
