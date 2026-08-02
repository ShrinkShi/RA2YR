using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Csf;
using RA2YR.Core.Formats.Mix;

namespace RA2YR.Tests.EditMode.Formats.Csf
{
    public sealed class WestwoodCsfReaderTests
    {
        [Test]
        public void ConfirmedMarkersUseTheirExactOnDiskLittleEndianBytes()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Normal("B"),
                    CsfSyntheticFixtureFactory.Extended("C", "D"))
            });

            Assert.That(input.Take(4), Is.EqualTo(new byte[] { 0x20, 0x46, 0x53, 0x43 }));
            Assert.That(input.Skip(24).Take(4),
                Is.EqualTo(new byte[] { 0x20, 0x4c, 0x42, 0x4c }));
            int normal = CsfSyntheticFixtureFactory.FindUInt32(
                input,
                WestwoodCsfReader.NormalValueMarker,
                24);
            int extended = CsfSyntheticFixtureFactory.FindUInt32(
                input,
                WestwoodCsfReader.ExtendedValueMarker,
                normal + 4);
            Assert.That(input.Skip(normal).Take(4),
                Is.EqualTo(new byte[] { 0x20, 0x52, 0x54, 0x53 }));
            Assert.That(input.Skip(extended).Take(4),
                Is.EqualTo(new byte[] { 0x57, 0x52, 0x54, 0x53 }));
        }

        [Test]
        public void MinimalEmptyDocumentRetainsHeaderFields()
        {
            CsfDocument document = AssertSuccess(Read(CsfSyntheticFixtureFactory.Build(
                Array.Empty<SyntheticCsfLabel>(),
                reserved: 0x12345678,
                language: 0xabcdef01)));

            Assert.That(document.Labels, Is.Empty);
            Assert.That(document.Header.Signature, Is.EqualTo(WestwoodCsfReader.FileSignature));
            Assert.That(document.Header.Version, Is.EqualTo(3));
            Assert.That(document.Header.DeclaredLabelCount, Is.Zero);
            Assert.That(document.Header.DeclaredValueCount, Is.Zero);
            Assert.That(document.Header.Reserved, Is.EqualTo(0x12345678));
            Assert.That(document.Header.Language.RawValue, Is.EqualTo(0xabcdef01));
        }

        [Test]
        public void SingleLabelWithNormalValueParses()
        {
            CsfDocument document = Parse(
                CsfSyntheticFixtureFactory.Label(
                    "GUI:OK",
                    CsfSyntheticFixtureFactory.Normal("Ready")));

            Assert.That(document.Labels, Has.Count.EqualTo(1));
            Assert.That(document[0].Name, Is.EqualTo("GUI:OK"));
            Assert.That(document[0].Values, Has.Count.EqualTo(1));
            Assert.That(document[0][0].Kind, Is.EqualTo(CsfValueKind.Normal));
            Assert.That(document[0][0].Text.CodeUnits, Is.EqualTo("Ready"));
            Assert.That(document[0][0].ExtraText, Is.Null);
        }

        [Test]
        public void SingleLabelWithExtendedValueKeepsAdditionalTextSeparate()
        {
            CsfValue value = Parse(
                CsfSyntheticFixtureFactory.Label(
                    "NAME",
                    CsfSyntheticFixtureFactory.Extended("Main", "sound.wav")))[0][0];

            Assert.That(value.Kind, Is.EqualTo(CsfValueKind.Extended));
            Assert.That(value.Text.CodeUnits, Is.EqualTo("Main"));
            Assert.That(value.ExtraText, Is.EqualTo("sound.wav"));
            Assert.That(value.HasExtraText, Is.True);
        }

        [Test]
        public void OneLabelPreservesMultipleValueOrder()
        {
            CsfLabel label = Parse(
                CsfSyntheticFixtureFactory.Label(
                    "MULTI",
                    CsfSyntheticFixtureFactory.Normal("first"),
                    CsfSyntheticFixtureFactory.Extended("second", "extra"),
                    CsfSyntheticFixtureFactory.Normal("third")))[0];

            Assert.That(label.Values.Select(value => value.Text.CodeUnits),
                Is.EqualTo(new[] { "first", "second", "third" }));
        }

        [Test]
        public void DocumentPreservesLabelOrder()
        {
            CsfDocument document = Parse(
                CsfSyntheticFixtureFactory.Label("THIRD"),
                CsfSyntheticFixtureFactory.Label("FIRST"),
                CsfSyntheticFixtureFactory.Label("SECOND"));

            Assert.That(document.Labels.Select(label => label.Name),
                Is.EqualTo(new[] { "THIRD", "FIRST", "SECOND" }));
        }

        [Test]
        public void DuplicateLabelsArePreservedAndExactQueryReturnsAllCandidates()
        {
            CsfDocument document = Parse(
                CsfSyntheticFixtureFactory.Label(
                    "DUP",
                    CsfSyntheticFixtureFactory.Normal("one")),
                CsfSyntheticFixtureFactory.Label("OTHER"),
                CsfSyntheticFixtureFactory.Label(
                    "DUP",
                    CsfSyntheticFixtureFactory.Normal("two")));

            Assert.That(document.Labels, Has.Count.EqualTo(3));
            IReadOnlyList<CsfLabel> matches =
                document.FindLabelsByExactOrdinalName("DUP");
            Assert.That(matches, Has.Count.EqualTo(2));
            Assert.That(matches.Select(label => label[0].Text.CodeUnits),
                Is.EqualTo(new[] { "one", "two" }));
        }

        [Test]
        public void LabelCaseIsPreservedAndExactQueryIsOrdinal()
        {
            CsfDocument document = Parse(
                CsfSyntheticFixtureFactory.Label("Label"),
                CsfSyntheticFixtureFactory.Label("LABEL"));

            Assert.That(document[0].Name, Is.EqualTo("Label"));
            Assert.That(document.FindLabelsByExactOrdinalName("Label"), Has.Count.EqualTo(1));
            Assert.That(document.FindLabelsByExactOrdinalName("label"), Is.Empty);
        }

        [Test]
        public void EmptyMainStringIsPreserved()
        {
            CsfValue value = Parse(CsfSyntheticFixtureFactory.Label(
                "EMPTY",
                CsfSyntheticFixtureFactory.Normal(string.Empty)))[0][0];

            Assert.That(value.Text.Length, Is.Zero);
            Assert.That(value.Text.CodeUnits, Is.EqualTo(string.Empty));
        }

        [Test]
        public void EmptyExtendedStringRemainsPresent()
        {
            CsfValue value = Parse(CsfSyntheticFixtureFactory.Label(
                "EMPTY",
                CsfSyntheticFixtureFactory.Extended("main", string.Empty)))[0][0];

            Assert.That(value.HasExtraText, Is.True);
            Assert.That(value.ExtraText, Is.EqualTo(string.Empty));
        }

        [Test]
        public void AsciiMainTextParsesWithoutCurrentCodePage()
        {
            string text = "ASCII 0123 !?";
            Assert.That(Parse(CsfSyntheticFixtureFactory.Label(
                "ASCII",
                CsfSyntheticFixtureFactory.Normal(text)))[0][0].Text.CodeUnits,
                Is.EqualTo(text));
        }

        [Test]
        public void ChineseBmpCodeUnitsArePreserved()
        {
            const string text = "尤里的复仇";
            Assert.That(Parse(CsfSyntheticFixtureFactory.Label(
                "ZH",
                CsfSyntheticFixtureFactory.Normal(text)))[0][0].Text.CodeUnits,
                Is.EqualTo(text));
        }

        [Test]
        public void SurrogatePairIsPreservedAsTwoCodeUnits()
        {
            string text = char.ConvertFromUtf32(0x1f642);
            CsfText parsed = Parse(CsfSyntheticFixtureFactory.Label(
                "PAIR",
                CsfSyntheticFixtureFactory.Normal(text)))[0][0].Text;

            Assert.That(parsed.Length, Is.EqualTo(2));
            Assert.That(parsed[0], Is.EqualTo((ushort)text[0]));
            Assert.That(parsed[1], Is.EqualTo((ushort)text[1]));
        }

        [Test]
        public void UnpairedSurrogateCodeUnitIsPreservedWithoutDecoderFallback()
        {
            string raw = new string(new[] { 'A', '\ud800', 'B' });
            CsfText parsed = Parse(CsfSyntheticFixtureFactory.Label(
                "RAW",
                CsfSyntheticFixtureFactory.Normal(raw)))[0][0].Text;

            Assert.That(parsed.Length, Is.EqualTo(3));
            Assert.That(parsed[1], Is.EqualTo(0xd800));
            Assert.That(parsed.CodeUnits, Is.EqualTo(raw));
        }

        [Test]
        public void UnicodeNormalizationIsNotApplied()
        {
            const string decomposed = "e\u0301";
            const string composed = "\u00e9";
            CsfDocument document = Parse(
                CsfSyntheticFixtureFactory.Label(
                    "DECOMPOSED",
                    CsfSyntheticFixtureFactory.Normal(decomposed)),
                CsfSyntheticFixtureFactory.Label(
                    "COMPOSED",
                    CsfSyntheticFixtureFactory.Normal(composed)));

            Assert.That(document[0][0].Text.CodeUnits, Is.EqualTo(decomposed));
            Assert.That(document[1][0].Text.CodeUnits, Is.EqualTo(composed));
            Assert.That(document[0][0].Text, Is.Not.EqualTo(document[1][0].Text));
        }

        [Test]
        public void NormalAndExtendedKindsRemainDistinctWithSameMainText()
        {
            CsfLabel label = Parse(CsfSyntheticFixtureFactory.Label(
                "KINDS",
                CsfSyntheticFixtureFactory.Normal("same"),
                CsfSyntheticFixtureFactory.Extended("same", string.Empty)))[0];

            Assert.That(label[0].Kind, Is.EqualTo(CsfValueKind.Normal));
            Assert.That(label[1].Kind, Is.EqualTo(CsfValueKind.Extended));
        }

        [Test]
        public void UnknownLanguageCodeIsRetainedWithoutFailure()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(
                Array.Empty<SyntheticCsfLabel>(),
                language: 0xfedcba98);

            Assert.That(AssertSuccess(Read(input)).Header.Language.RawValue,
                Is.EqualTo(0xfedcba98));
        }

        [Test]
        public void WrongSignatureFailsAtOffsetZero()
        {
            CsfDiagnostic diagnostic = AssertFailure(
                Read(CsfSyntheticFixtureFactory.Build(
                    Array.Empty<SyntheticCsfLabel>(),
                    signature: 0)),
                CsfDiagnosticCode.InvalidSignature);

            Assert.That(diagnostic.AbsoluteOffset, Is.Zero);
            Assert.That(diagnostic.RawRecordMarker, Is.EqualTo(0u));
        }

        [Test]
        public void UnsupportedVersionFailsAtVersionField()
        {
            CsfDiagnostic diagnostic = AssertFailure(
                Read(CsfSyntheticFixtureFactory.Build(
                    Array.Empty<SyntheticCsfLabel>(),
                    version: 4)),
                CsfDiagnosticCode.UnsupportedVersion);

            Assert.That(diagnostic.AbsoluteOffset, Is.EqualTo(4));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(23)]
        public void TruncatedHeaderFailsWithoutPartialDocument(int length)
        {
            byte[] complete = CsfSyntheticFixtureFactory.Build(
                Array.Empty<SyntheticCsfLabel>());
            byte[] truncated = complete.Take(length).ToArray();

            CsfParseResult result = Read(truncated);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(CsfDiagnosticCode.UnexpectedEndOfInput));
        }

        [Test]
        public void TruncatedLabelNameReportsLabelIndexAndExactOffset()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label("ABCD")
            });
            Array.Resize(ref input, input.Length - 1);

            CsfDiagnostic diagnostic = AssertFailure(
                Read(input),
                CsfDiagnosticCode.UnexpectedEndOfInput);

            Assert.That(diagnostic.LabelIndex, Is.Zero);
            Assert.That(diagnostic.ValueIndex, Is.EqualTo(-1));
            Assert.That(diagnostic.AbsoluteOffset, Is.EqualTo(input.Length - 3));
            Assert.That(diagnostic.RequestedLength, Is.EqualTo(4));
            Assert.That(diagnostic.RemainingLength, Is.EqualTo(3));
        }

        [Test]
        public void TruncatedNormalStringReportsValueIndex()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Normal("text"))
            });
            Array.Resize(ref input, input.Length - 1);

            CsfDiagnostic diagnostic = AssertFailure(
                Read(input),
                CsfDiagnosticCode.UnexpectedEndOfInput);

            Assert.That(diagnostic.LabelIndex, Is.Zero);
            Assert.That(diagnostic.ValueIndex, Is.Zero);
            Assert.That(diagnostic.RawRecordMarker,
                Is.EqualTo(WestwoodCsfReader.NormalValueMarker));
        }

        [Test]
        public void TruncatedExtendedStringReportsValueIndex()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Extended("text", "extra"))
            });
            Array.Resize(ref input, input.Length - 1);

            CsfDiagnostic diagnostic = AssertFailure(
                Read(input),
                CsfDiagnosticCode.UnexpectedEndOfInput);

            Assert.That(diagnostic.LabelIndex, Is.Zero);
            Assert.That(diagnostic.ValueIndex, Is.Zero);
        }

        [Test]
        public void InvalidLabelMarkerFailsWithoutSkippingRecord()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label("A")
            });
            CsfSyntheticFixtureFactory.WriteUInt32(input, WestwoodCsfReader.HeaderLength, 1);

            CsfDiagnostic diagnostic = AssertFailure(
                Read(input),
                CsfDiagnosticCode.InvalidLabelMarker);

            Assert.That(diagnostic.LabelIndex, Is.Zero);
            Assert.That(diagnostic.RawRecordMarker, Is.EqualTo(1u));
        }

        [Test]
        public void InvalidValueMarkerFailsWithoutSkippingRecord()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Normal("B"))
            });
            int markerOffset = CsfSyntheticFixtureFactory.FindUInt32(
                input,
                WestwoodCsfReader.NormalValueMarker,
                WestwoodCsfReader.HeaderLength);
            CsfSyntheticFixtureFactory.WriteUInt32(input, markerOffset, 2);

            CsfDiagnostic diagnostic = AssertFailure(
                Read(input),
                CsfDiagnosticCode.InvalidValueMarker);

            Assert.That(diagnostic.LabelIndex, Is.Zero);
            Assert.That(diagnostic.ValueIndex, Is.Zero);
            Assert.That(diagnostic.RawRecordMarker, Is.EqualTo(2u));
        }

        [Test]
        public void NonAsciiLabelByteFailsClosed()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label("A")
            });
            input[WestwoodCsfReader.HeaderLength + 12] = 0x80;

            AssertFailure(Read(input), CsfDiagnosticCode.InvalidAsciiByte);
        }

        [Test]
        public void NonAsciiExtendedByteFailsClosed()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Extended("B", "C"))
            });
            input[input.Length - 1] = 0x80;

            AssertFailure(Read(input), CsfDiagnosticCode.InvalidAsciiByte);
        }

        [Test]
        public void DeclaredLabelCountAboveActualFailsWithoutPartialDocument()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(
                Array.Empty<SyntheticCsfLabel>(),
                declaredLabelCount: 1);

            CsfParseResult result = Read(input);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(CsfDiagnosticCode.UnexpectedEndOfInput));
        }

        [Test]
        public void DeclaredLabelCountBelowActualHasExplicitMismatchDiagnostic()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(
                new[]
                {
                    CsfSyntheticFixtureFactory.Label("FIRST"),
                    CsfSyntheticFixtureFactory.Label("SECOND")
                },
                declaredLabelCount: 1,
                declaredValueCount: 0);

            CsfDiagnostic diagnostic = AssertFailure(
                Read(input),
                CsfDiagnosticCode.DeclaredLabelCountMismatch);

            Assert.That(diagnostic.LabelIndex, Is.EqualTo(1));
            Assert.That(diagnostic.RawRecordMarker,
                Is.EqualTo(WestwoodCsfReader.LabelMarker));
            Assert.That(diagnostic.AbsoluteOffset, Is.GreaterThan(24));
        }

        [Test]
        public void DeclaredTotalValueCountBelowActualFailsExplicitly()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(
                new[]
                {
                    CsfSyntheticFixtureFactory.Label(
                        "A",
                        CsfSyntheticFixtureFactory.Normal("B"))
                },
                declaredValueCount: 0);

            AssertFailure(Read(input), CsfDiagnosticCode.DeclaredValueCountMismatch);
        }

        [Test]
        public void DeclaredTotalValueCountAboveActualFailsExplicitly()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(
                new[] { CsfSyntheticFixtureFactory.Label("A") },
                declaredValueCount: 1);

            AssertFailure(Read(input), CsfDiagnosticCode.DeclaredValueCountMismatch);
        }

        [Test]
        public void TrailingByteIsRejected()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(
                Array.Empty<SyntheticCsfLabel>());
            Array.Resize(ref input, input.Length + 1);

            AssertFailure(Read(input), CsfDiagnosticCode.UnexpectedTrailingData);
        }

        [Test]
        public void MemoryAndSeekableStreamProduceSameCanonicalModel()
        {
            byte[] input = Sample();
            CsfDocument memory = AssertSuccess(Read(input));
            CsfDocument stream;
            using (var source = new MemoryStream(input, false))
            {
                stream = AssertSuccess(WestwoodCsfReader.ReadSeekable(
                    source,
                    Source(),
                    Provenance(),
                    leaveOpen: true));
            }

            Assert.That(stream.CanonicalModelSha256,
                Is.EqualTo(memory.CanonicalModelSha256));
            Assert.That(stream[0][0].Text.CodeUnits,
                Is.EqualTo(memory[0][0].Text.CodeUnits));
        }

        [Test]
        public void MultipleShortReadsProduceSameResult()
        {
            byte[] input = Sample();
            using (var stream = new ShortReadStream(input, 3))
            {
                CsfDocument document = AssertSuccess(WestwoodCsfReader.Read(
                    stream,
                    input.Length,
                    Source(),
                    Provenance(),
                    leaveOpen: true));

                Assert.That(document.Labels, Has.Count.EqualTo(1));
                Assert.That(stream.ReadCalls, Is.GreaterThan(1));
            }
        }

        [Test]
        public void MixEntryWindowProducesSameResultAndAbsoluteOffsets()
        {
            byte[] input = Sample();
            MixArchiveReadResult mixResult = ReadSyntheticMix(input);
            Assert.That(mixResult.IsSuccess, Is.True);
            using (mixResult.Archive)
            {
                CsfDocument document = AssertSuccess(WestwoodCsfReader.Read(
                    mixResult.Archive.Entries.Single().OpenPayloadWindow(),
                    Source(),
                    Provenance()));
                Assert.That(document.CanonicalModelSha256,
                    Is.EqualTo(AssertSuccess(Read(input)).CanonicalModelSha256));
            }
        }

        [Test]
        public void MixEntryWindowAddsNonzeroBaseToCorruptMarkerDiagnostic()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label("A")
            });
            CsfSyntheticFixtureFactory.WriteUInt32(
                input,
                WestwoodCsfReader.HeaderLength,
                0);
            MixArchiveReadResult mixResult = ReadSyntheticMix(input);
            Assert.That(mixResult.IsSuccess, Is.True);
            using (mixResult.Archive)
            {
                long payloadStart = mixResult.Archive.Entries.Single().PayloadAbsoluteOffset;
                CsfDiagnostic diagnostic = AssertFailure(
                    WestwoodCsfReader.Read(
                        mixResult.Archive.Entries.Single().OpenPayloadWindow(),
                        Source(),
                        Provenance()),
                    CsfDiagnosticCode.InvalidLabelMarker);

                Assert.That(diagnostic.AbsoluteOffset,
                    Is.EqualTo(payloadStart + WestwoodCsfReader.HeaderLength));
                Assert.That(diagnostic.RawRecordMarker, Is.EqualTo(0u));
            }
        }

        [Test]
        public void ModelCollectionsCannotBeMutated()
        {
            CsfDocument document = Parse(CsfSyntheticFixtureFactory.Label(
                "A",
                CsfSyntheticFixtureFactory.Normal("B")));

            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsfLabel>)document.Labels).Add(document[0]));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsfValue>)document[0].Values).Add(document[0][0]));
        }

        [Test]
        public void ParseResultAndDocumentCannotBeForgedThroughPublicConstructors()
        {
            Assert.That(typeof(CsfParseResult).GetConstructors(), Is.Empty);
            Assert.That(typeof(CsfDocument).GetConstructors(), Is.Empty);
        }

        [Test]
        public void CanonicalHashIncludesOrderDuplicatesKindsAndRawCodeUnits()
        {
            CsfDocument first = Parse(
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Normal("x")),
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Extended("x", string.Empty)));
            CsfDocument reordered = Parse(
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Extended("x", string.Empty)),
                CsfSyntheticFixtureFactory.Label(
                    "A",
                    CsfSyntheticFixtureFactory.Normal("x")));

            Assert.That(first.CanonicalModelSha256, Has.Length.EqualTo(64));
            Assert.That(first.CanonicalModelSha256,
                Is.Not.EqualTo(reordered.CanonicalModelSha256));
        }

        [Test]
        public void CanonicalHashMatchesFixedSchemaAndChangesForEveryKeyField()
        {
            byte[] baseline = CsfSyntheticFixtureFactory.Build(
                new[]
                {
                    CsfSyntheticFixtureFactory.Label(
                        "Label",
                        CsfSyntheticFixtureFactory.Normal("A"),
                        CsfSyntheticFixtureFactory.Extended("中", "EX"))
                },
                reserved: 0x11223344,
                language: 9);
            string digest = AssertSuccess(Read(baseline)).CanonicalModelSha256;

            Assert.That(digest,
                Is.EqualTo("06e8f2086dcf615e95f4fc07b4926b4633aba93fc83c21396974e80708f5dcec"));
            Assert.That(AssertSuccess(Read(CsfSyntheticFixtureFactory.Build(
                    new[]
                    {
                        CsfSyntheticFixtureFactory.Label(
                            "Label",
                            CsfSyntheticFixtureFactory.Normal("B"),
                            CsfSyntheticFixtureFactory.Extended("中", "EX"))
                    },
                    reserved: 0x11223344,
                    language: 9))).CanonicalModelSha256,
                Is.Not.EqualTo(digest));
            Assert.That(AssertSuccess(Read(CsfSyntheticFixtureFactory.Build(
                    new[]
                    {
                        CsfSyntheticFixtureFactory.Label(
                            "Label",
                            CsfSyntheticFixtureFactory.Normal("A"),
                            CsfSyntheticFixtureFactory.Extended("中", "EY"))
                    },
                    reserved: 0x11223344,
                    language: 9))).CanonicalModelSha256,
                Is.Not.EqualTo(digest));
            Assert.That(AssertSuccess(Read(CsfSyntheticFixtureFactory.Build(
                    new[]
                    {
                        CsfSyntheticFixtureFactory.Label(
                            "Label",
                            CsfSyntheticFixtureFactory.Normal("A"),
                            CsfSyntheticFixtureFactory.Extended("中", "EX"))
                    },
                    reserved: 0x11223345,
                    language: 9))).CanonicalModelSha256,
                Is.Not.EqualTo(digest));
            Assert.That(AssertSuccess(Read(CsfSyntheticFixtureFactory.Build(
                    new[]
                    {
                        CsfSyntheticFixtureFactory.Label(
                            "label",
                            CsfSyntheticFixtureFactory.Normal("A"),
                            CsfSyntheticFixtureFactory.Extended("中", "EX"))
                    },
                    reserved: 0x11223344,
                    language: 9))).CanonicalModelSha256,
                Is.Not.EqualTo(digest));
        }

        [Test]
        public void DiagnosticsContainLogicalContextButNoInputText()
        {
            byte[] input = CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label(
                    "PRIVATE_LABEL",
                    CsfSyntheticFixtureFactory.Normal("PRIVATE_TEXT"))
            });
            int markerOffset = CsfSyntheticFixtureFactory.FindUInt32(
                input,
                WestwoodCsfReader.NormalValueMarker,
                WestwoodCsfReader.HeaderLength);
            CsfSyntheticFixtureFactory.WriteUInt32(input, markerOffset, 0);

            CsfDiagnostic diagnostic = AssertFailure(
                Read(input),
                CsfDiagnosticCode.InvalidValueMarker);

            Assert.That(diagnostic.Source.LogicalPath.Value,
                Is.EqualTo("synthetic/strings.csf"));
            Assert.That(diagnostic.Provenance.LogicalChain.Select(path => path.Value),
                Is.EqualTo(new[] { "synthetic.mix", "strings.csf" }));
            Assert.That(diagnostic.Message, Does.Not.Contain("PRIVATE_LABEL"));
            Assert.That(diagnostic.Message, Does.Not.Contain("PRIVATE_TEXT"));
        }

        private static CsfDocument Parse(params SyntheticCsfLabel[] labels)
        {
            return AssertSuccess(Read(CsfSyntheticFixtureFactory.Build(labels)));
        }

        private static byte[] Sample()
        {
            return CsfSyntheticFixtureFactory.Build(new[]
            {
                CsfSyntheticFixtureFactory.Label(
                    "SAMPLE",
                    CsfSyntheticFixtureFactory.Extended("测试", "extra"))
            }, language: 9);
        }

        private static CsfParseResult Read(byte[] input, CsfReadLimits limits = null)
        {
            return WestwoodCsfReader.Read(input, Source(), Provenance(), limits);
        }

        private static CsfDocument AssertSuccess(CsfParseResult result)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Document, Is.Not.Null);
            return result.Document;
        }

        private static CsfDiagnostic AssertFailure(
            CsfParseResult result,
            CsfDiagnosticCode expectedCode)
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(expectedCode));
            return result.Diagnostics[0];
        }

        private static BinarySourceContext Source()
        {
            return new BinarySourceContext(
                "format.csf",
                "synthetic-source",
                LogicalContentPath.Parse("synthetic/strings.csf"));
        }

        private static CsfSourceProvenance Provenance()
        {
            return new CsfSourceProvenance(
                "synthetic-source",
                new[]
                {
                    LogicalContentPath.Parse("synthetic.mix"),
                    LogicalContentPath.Parse("strings.csf")
                });
        }

        private static MixArchiveReadResult ReadSyntheticMix(byte[] payload)
        {
            byte[] archive = new byte[checked(18 + payload.Length)];
            WriteUInt16(archive, 0, 1);
            CsfSyntheticFixtureFactory.WriteUInt32(archive, 2, checked((uint)payload.Length));
            CsfSyntheticFixtureFactory.WriteUInt32(
                archive,
                6,
                MixFileId.ComputeCandidateId("strings.csf").Value);
            CsfSyntheticFixtureFactory.WriteUInt32(archive, 10, 0);
            CsfSyntheticFixtureFactory.WriteUInt32(
                archive,
                14,
                checked((uint)payload.Length));
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
            bytes[offset] = checked((byte)(value & 0xff));
            bytes[offset + 1] = checked((byte)(value >> 8));
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
                int available = bytes.Length - position;
                int actual = Math.Min(Math.Min(count, maximumChunk), available);
                if (actual == 0)
                {
                    return 0;
                }

                Buffer.BlockCopy(bytes, position, buffer, offset, actual);
                position += actual;
                return actual;
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) =>
                throw new NotSupportedException();
            public override void SetLength(long value) =>
                throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();
        }
    }
}
