using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Tests.EditMode.Configuration.Ini
{
    public sealed class IniRuntimeResolverTests
    {
        [Test]
        public void SingleLayerSingleDocumentResolves()
        {
            Fixture fixture = One("[S]\nK=V");

            IniResolutionResult result = Resolve(fixture);

            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Complete));
            Assert.That(Value(result, "s", "k"), Is.EqualTo("V"));
        }

        [Test]
        public void HigherExplicitPriorityOverridesLowerPriority()
        {
            Fixture fixture = Two("[S]\nK=low", "[S]\nK=high", 10, 20);

            IniResolutionResult result = Resolve(fixture);

            Assert.That(Value(result, "s", "k"), Is.EqualTo("high"));
        }

        [Test]
        public void ThreeLayersRetainTheCompleteCandidateChain()
        {
            Fixture fixture = Three("one", "two", "three");

            IniResolvedValue value = GetValue(Resolve(fixture), "s", "k");

            Assert.That(value.CandidateChain, Has.Count.EqualTo(3));
            Assert.That(value.CandidateChain.Select(item => item.Document.CandidateId),
                Is.EqualTo(new[] { "candidate-high", "candidate-mid", "candidate-low" }));
        }

        [Test]
        public void EqualHighestPriorityIsAmbiguous()
        {
            Fixture fixture = Two("[S]\nK=one", "[S]\nK=two", 20, 20);

            IniResolutionResult result = Resolve(fixture);

            AssertAmbiguous(result, IniResolutionDiagnosticCode.EqualLayerPriority);
        }

        [Test]
        public void CandidateIdentityDoesNotBreakPriorityTies()
        {
            Fixture fixture = Two("[S]\nK=one", "[S]\nK=two", 20, 20,
                "aaa-source", "zzz-source");

            IniResolutionResult result = Resolve(fixture);

            AssertAmbiguous(result, IniResolutionDiagnosticCode.EqualLayerPriority);
        }

        [Test]
        public void EnumerationOrderDoesNotChangeWinnerOrTrace()
        {
            Fixture fixture = Three("one", "two", "three");
            IniResolutionResult forward = Resolve(fixture);
            fixture.Candidates.Reverse();
            fixture.Layers.Reverse();

            IniResolutionResult reversed = Resolve(fixture);

            Assert.That(Value(reversed, "s", "k"), Is.EqualTo(Value(forward, "s", "k")));
            Assert.That(TraceIds(reversed), Is.EqualTo(TraceIds(forward)));
        }

        [Test]
        public void LooseAndMixCandidatesRemainDistinctLayers()
        {
            Fixture fixture = Two("[S]\nK=mix", "[S]\nK=loose", 10, 20);
            fixture.Layers[0] = Layer(
                "layer-low", "source-low", IniLoadLayerKind.NestedMix,
                10, "ra2md.mix", "localmd.mix", "rulesmd.ini");
            fixture.Layers[1] = Layer(
                "layer-high", "source-high", IniLoadLayerKind.LooseDirectory,
                20, "rulesmd.ini");
            fixture.Candidates[0] = Candidate(
                "candidate-low", "layer-low", "source-low", "[S]\nK=mix",
                "ra2md.mix", "localmd.mix", "rulesmd.ini");
            fixture.Candidates[1] = Candidate(
                "candidate-high", "layer-high", "source-high", "[S]\nK=loose",
                "rulesmd.ini");

            IniResolutionResult result = Resolve(fixture);

            Assert.That(Value(result, "s", "k"), Is.EqualTo("loose"));
            Assert.That(result.Trace.DocumentCandidates.Select(value => value.LayerId),
                Is.EqualTo(new[] { "layer-high", "layer-low" }));
        }

        [Test]
        public void NestedMixProvenanceIsValidatedAsACompleteChain()
        {
            Fixture fixture = One(
                "[S]\nK=V",
                "ra2md.mix", "localmd.mix", "rulesmd.ini");

            IniResolutionResult result = Resolve(fixture);

            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Trace.DocumentCandidates.Single().Document.Provenance.LogicalChain,
                Has.Count.EqualTo(3));
        }

        [Test]
        public void ExactSourceIdCaseMatchesProvenance()
        {
            Fixture fixture = One("[S]\nK=V");
            fixture.Layers[0] = Layer(
                "layer-only", "Source-Only", IniLoadLayerKind.TestSource,
                10, "rulesmd.ini");
            fixture.Candidates[0] = Candidate(
                "candidate-only", "layer-only", "Source-Only", "[S]\nK=V",
                "rulesmd.ini");

            Assert.That(Resolve(fixture).Status, Is.EqualTo(IniResolutionStatus.Complete));
        }

        [Test]
        public void SourceIdCaseMismatchFailsAsIncompleteProvenanceWithoutHostPath()
        {
            Fixture fixture = One("[S]\nK=V");
            fixture.Layers[0] = Layer(
                "layer-only", "Source-Only", IniLoadLayerKind.TestSource,
                10, "rulesmd.ini");
            fixture.Candidates[0] = Candidate(
                "candidate-only", "layer-only", "source-only", "[S]\nK=V",
                "rulesmd.ini");

            IniResolutionResult result = Resolve(fixture);

            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Failed));
            AssertCode(result, IniResolutionDiagnosticCode.IncompleteProvenance);
            Assert.That(result.Diagnostics.All(value =>
                !value.Message.Contains(":\\") &&
                (value.LogicalPath == null ||
                 !value.LogicalPath.Value.Contains(":") &&
                 !value.LogicalPath.Value.Contains("\\"))), Is.True);
        }

        [Test]
        public void IncompleteProvenanceFailsClosed()
        {
            Fixture fixture = One("[S]\nK=V", "rulesmd.ini");
            fixture.Layers[0] = Layer(
                "layer-only", "source-only", IniLoadLayerKind.NestedMix,
                10, "ra2md.mix", "rulesmd.ini");

            IniResolutionResult result = Resolve(fixture);

            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Failed));
            AssertCode(result, IniResolutionDiagnosticCode.IncompleteProvenance);
        }

        [Test]
        public void MissingCandidateLayerFailsClosed()
        {
            Fixture fixture = One("[S]\nK=V");
            fixture.Candidates[0] = Candidate(
                "candidate-only", "missing-layer", "source-only", "[S]\nK=V",
                "rulesmd.ini");

            IniResolutionResult result = Resolve(fixture);

            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Failed));
            AssertCode(result, IniResolutionDiagnosticCode.CandidateLayerMissing);
        }

        [Test]
        public void SelectDocumentPolicyExcludesUniqueLowerDocumentValues()
        {
            Fixture fixture = Two("[S]\nLow=1", "[S]\nHigh=2", 10, 20);
            IniResolutionPolicy policy = Policy(
                fileComposition: IniFileCompositionPolicy.SelectHighestPriorityDocument);

            IniResolutionResult result = Resolve(fixture, policy);

            Assert.That(result.Sections.Single().Values.Select(value => value.KeyName),
                Is.EqualTo(new[] { "high" }));
        }

        [Test]
        public void OverlayPolicyRetainsUniqueLowerDocumentValues()
        {
            Fixture fixture = Two("[S]\nLow=1", "[S]\nHigh=2", 10, 20);

            IniResolutionResult result = Resolve(fixture);

            Assert.That(result.Sections.Single().Values.Select(value => value.KeyName),
                Is.EqualTo(new[] { "high", "low" }));
        }

        [Test]
        public void UnresolvedFileCompositionReturnsAmbiguous()
        {
            Fixture fixture = Two("[S]\nK=one", "[S]\nK=two", 10, 20);

            IniResolutionResult result = Resolve(fixture, Policy(
                fileComposition: IniFileCompositionPolicy.Unresolved));

            AssertAmbiguous(result, IniResolutionDiagnosticCode.UnresolvedFileComposition);
        }

        [Test]
        public void MissingPriorityReturnsAmbiguous()
        {
            Fixture fixture = Two("[S]\nK=one", "[S]\nK=two", 10, 20);
            fixture.Layers[1] = Layer(
                "layer-high", "source-high", IniLoadLayerKind.ExpandMix,
                null, "expandmd01.mix", "rulesmd.ini");

            IniResolutionResult result = Resolve(fixture);

            AssertAmbiguous(result, IniResolutionDiagnosticCode.MissingLayerPriority);
        }

        [Test]
        public void DuplicateSectionsCanSelectFirstPhysicalSection()
        {
            Fixture fixture = One("[S]\nK=first\n[S]\nK=last");

            IniResolutionResult result = Resolve(fixture, Policy(
                duplicateSections: IniDuplicateSectionPolicy.FirstSectionWins));

            Assert.That(Value(result, "s", "k"), Is.EqualTo("first"));
        }

        [Test]
        public void DuplicateSectionsCanSelectLastPhysicalSection()
        {
            Fixture fixture = One("[S]\nK=first\n[S]\nK=last");

            IniResolutionResult result = Resolve(fixture, Policy(
                duplicateSections: IniDuplicateSectionPolicy.LastSectionWins));

            Assert.That(Value(result, "s", "k"), Is.EqualTo("last"));
        }

        [Test]
        public void DuplicateSectionsCanMergeBeforeKeyResolution()
        {
            Fixture fixture = One("[S]\nK=first\n[S]\nK=last");

            IniResolutionResult result = Resolve(fixture, Policy(
                duplicateSections: IniDuplicateSectionPolicy.MergeSectionsInFileOrder,
                duplicateKeys: IniDuplicateKeyPolicy.FirstKeyWins));

            Assert.That(Value(result, "s", "k"), Is.EqualTo("first"));
            Assert.That(GetValue(result, "s", "k").CandidateChain, Has.Count.EqualTo(2));
        }

        [Test]
        public void UnresolvedDuplicateSectionReturnsAmbiguous()
        {
            Fixture fixture = One("[S]\nK=first\n[S]\nK=last");

            IniResolutionResult result = Resolve(fixture, Policy(
                duplicateSections: IniDuplicateSectionPolicy.Unresolved));

            AssertAmbiguous(result, IniResolutionDiagnosticCode.UnresolvedDuplicateSection);
        }

        [Test]
        public void DuplicateKeysCanSelectFirstPhysicalKey()
        {
            Fixture fixture = One("[S]\nK=first\nK=last");

            IniResolutionResult result = Resolve(fixture, Policy(
                duplicateKeys: IniDuplicateKeyPolicy.FirstKeyWins));

            Assert.That(Value(result, "s", "k"), Is.EqualTo("first"));
        }

        [Test]
        public void DuplicateKeysCanSelectLastPhysicalKey()
        {
            Fixture fixture = One("[S]\nK=first\nK=last");

            IniResolutionResult result = Resolve(fixture, Policy(
                duplicateKeys: IniDuplicateKeyPolicy.LastKeyWins));

            Assert.That(Value(result, "s", "k"), Is.EqualTo("last"));
        }

        [Test]
        public void UnresolvedDuplicateKeyReturnsAmbiguous()
        {
            Fixture fixture = One("[S]\nK=first\nK=last");

            IniResolutionResult result = Resolve(fixture, Policy(
                duplicateKeys: IniDuplicateKeyPolicy.Unresolved));

            AssertAmbiguous(result, IniResolutionDiagnosticCode.UnresolvedDuplicateKey);
        }

        [Test]
        public void OrdinalRawAsciiKeepsCaseDistinct()
        {
            Fixture fixture = One("[S]\nKey=one\nkey=two");

            IniResolutionResult result = Resolve(fixture, Policy(
                nameComparison: IniNameComparisonPolicy.OrdinalRawAscii));

            Assert.That(result.Sections.Single().Values, Has.Count.EqualTo(2));
        }

        [Test]
        public void OrdinalIgnoreCaseAsciiCombinesCaseVariants()
        {
            Fixture fixture = One("[S]\nKey=one\nkey=two");

            IniResolutionResult result = Resolve(fixture, Policy(
                nameComparison: IniNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                duplicateKeys: IniDuplicateKeyPolicy.LastKeyWins));

            Assert.That(result.Sections.Single().Values, Has.Count.EqualTo(1));
            Assert.That(Value(result, "s", "key"), Is.EqualTo("two"));
        }

        [Test]
        public void CurrentTurkishCultureDoesNotChangeAsciiComparison()
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                Fixture fixture = One("[INDEX]\nID=one\nid=two");

                IniResolutionResult result = Resolve(fixture, Policy(
                    nameComparison: IniNameComparisonPolicy.OrdinalIgnoreCaseAscii));

                Assert.That(Value(result, "index", "id"), Is.EqualTo("two"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void UnresolvedNameComparisonReturnsAmbiguous()
        {
            Fixture fixture = One("[S]\nK=V");

            IniResolutionResult result = Resolve(fixture, Policy(
                nameComparison: IniNameComparisonPolicy.Unresolved));

            AssertAmbiguous(result, IniResolutionDiagnosticCode.UnresolvedNameComparison);
        }

        [Test]
        public void EmptyValueCanOverrideEarlierNonemptyValue()
        {
            Fixture fixture = Two("[S]\nK=low", "[S]\nK=", 10, 20);

            IniResolutionResult result = Resolve(fixture, Policy(
                emptyValues: IniEmptyValuePolicy.OverridesEarlierValue));

            Assert.That(Value(result, "s", "k"), Is.Empty);
        }

        [Test]
        public void EmptyValueCanBeConfiguredNotToOverride()
        {
            Fixture fixture = Two("[S]\nK=low", "[S]\nK=", 10, 20);

            IniResolutionResult result = Resolve(fixture, Policy(
                emptyValues: IniEmptyValuePolicy.DoesNotOverrideEarlierValue));

            Assert.That(Value(result, "s", "k"), Is.EqualTo("low"));
        }

        [Test]
        public void UnresolvedEmptyValueReturnsAmbiguous()
        {
            Fixture fixture = One("[S]\nK=");

            IniResolutionResult result = Resolve(fixture, Policy(
                emptyValues: IniEmptyValuePolicy.Unresolved));

            AssertAmbiguous(result, IniResolutionDiagnosticCode.UnresolvedEmptyValue);
        }

        [Test]
        public void SemicolonPreservePolicyKeepsTheFullValue()
        {
            Fixture fixture = One("[S]\nK=value;comment");

            IniResolutionResult result = Resolve(fixture, Policy(
                inlineComments: IniInlineCommentPolicy.PreserveSemicolonInValue));

            Assert.That(Value(result, "s", "k"), Is.EqualTo("value;comment"));
        }

        [Test]
        public void SemicolonCommentPolicyCutsAtTheFirstSemicolon()
        {
            Fixture fixture = One("[S]\nK=value;comment");

            IniResolutionResult result = Resolve(fixture, Policy(
                inlineComments: IniInlineCommentPolicy.SemicolonStartsComment));

            Assert.That(Value(result, "s", "k"), Is.EqualTo("value"));
        }

        [Test]
        public void Utf16LittleEndianRecognizesOnlyTrueSemicolon()
        {
            IniResolutionResult trueSemicolon = Resolve(
                OneEncoded("[S]\nK=value;comment",
                    IniPhysicalEncodingKind.Utf16LittleEndianWithBom),
                Policy(inlineComments: IniInlineCommentPolicy.SemicolonStartsComment));
            IniResolutionResult reverseLookalike = Resolve(
                OneEncoded("[S]\nK=value\u3b00tail",
                    IniPhysicalEncodingKind.Utf16LittleEndianWithBom),
                Policy(inlineComments: IniInlineCommentPolicy.SemicolonStartsComment));

            Assert.That(DecodedValue(
                trueSemicolon,
                IniPhysicalEncodingKind.Utf16LittleEndianWithBom), Is.EqualTo("value"));
            Assert.That(DecodedValue(
                reverseLookalike,
                IniPhysicalEncodingKind.Utf16LittleEndianWithBom),
                Is.EqualTo("value\u3b00tail"));
            Assert.That(GetValue(reverseLookalike, "s", "k").Winner.ContainsInlineSemicolon,
                Is.False);
        }

        [Test]
        public void Utf16BigEndianRecognizesOnlyTrueSemicolon()
        {
            IniResolutionResult trueSemicolon = Resolve(
                OneEncoded("[S]\nK=value;comment",
                    IniPhysicalEncodingKind.Utf16BigEndianWithBom),
                Policy(inlineComments: IniInlineCommentPolicy.SemicolonStartsComment));
            IniResolutionResult reverseLookalike = Resolve(
                OneEncoded("[S]\nK=value\u3b00tail",
                    IniPhysicalEncodingKind.Utf16BigEndianWithBom),
                Policy(inlineComments: IniInlineCommentPolicy.SemicolonStartsComment));

            Assert.That(DecodedValue(
                trueSemicolon,
                IniPhysicalEncodingKind.Utf16BigEndianWithBom), Is.EqualTo("value"));
            Assert.That(DecodedValue(
                reverseLookalike,
                IniPhysicalEncodingKind.Utf16BigEndianWithBom),
                Is.EqualTo("value\u3b00tail"));
            Assert.That(GetValue(reverseLookalike, "s", "k").Winner.ContainsInlineSemicolon,
                Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Utf16TrueAsciiSpaceAndTabAreTrimmed(bool bigEndian)
        {
            IniPhysicalEncodingKind encoding = bigEndian
                ? IniPhysicalEncodingKind.Utf16BigEndianWithBom
                : IniPhysicalEncodingKind.Utf16LittleEndianWithBom;
            IniResolutionResult result = Resolve(
                OneEncoded("[S]\nK= \tvalue\t ", encoding),
                Policy(whitespace: IniWhitespaceReadPolicy.TrimAsciiSpaceAndTab));

            Assert.That(DecodedValue(result, encoding), Is.EqualTo("value"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Utf16U2000IsNotTrimmedAsAsciiSpace(bool bigEndian)
        {
            IniPhysicalEncodingKind encoding = bigEndian
                ? IniPhysicalEncodingKind.Utf16BigEndianWithBom
                : IniPhysicalEncodingKind.Utf16LittleEndianWithBom;
            IniResolutionResult result = Resolve(
                OneEncoded("[S]\nK=\u2000value\u2000", encoding),
                Policy(whitespace: IniWhitespaceReadPolicy.TrimAsciiSpaceAndTab));

            Assert.That(DecodedValue(result, encoding), Is.EqualTo("\u2000value\u2000"));
        }

        [Test]
        public void UnresolvedSemicolonPolicyReturnsAmbiguous()
        {
            Fixture fixture = One("[S]\nK=value;comment");

            IniResolutionResult result = Resolve(fixture, Policy(
                inlineComments: IniInlineCommentPolicy.Unresolved));

            AssertAmbiguous(result, IniResolutionDiagnosticCode.UnresolvedInlineComment);
        }

        [Test]
        public void WhitespacePreservePolicyKeepsEqualsAndTrailingWhitespace()
        {
            Fixture fixture = One("[S]\nK=  value  ");

            IniResolutionResult result = Resolve(fixture, Policy(
                whitespace: IniWhitespaceReadPolicy.Preserve));

            Assert.That(Value(result, "s", "k"), Is.EqualTo("  value  "));
        }

        [Test]
        public void WhitespaceTrimPolicyRemovesAsciiSpaceAndTabOnly()
        {
            Fixture fixture = One("[S]\nK= \tvalue\t ");

            IniResolutionResult result = Resolve(fixture, Policy(
                whitespace: IniWhitespaceReadPolicy.TrimAsciiSpaceAndTab));

            Assert.That(Value(result, "s", "k"), Is.EqualTo("value"));
        }

        [Test]
        public void UnresolvedWhitespacePolicyReturnsAmbiguousWhenObservable()
        {
            Fixture fixture = One("[S]\nK= value ");

            IniResolutionResult result = Resolve(fixture, Policy(
                whitespace: IniWhitespaceReadPolicy.Unresolved));

            AssertAmbiguous(result, IniResolutionDiagnosticCode.UnresolvedWhitespace);
        }

        [Test]
        public void OpaqueNodesAreDiagnosedAndNeverExecuted()
        {
            Fixture fixture = One("[S]\nopaque-directive\nK=V");

            IniResolutionResult result = Resolve(fixture);

            Assert.That(result.IsComplete, Is.True);
            AssertCode(result, IniResolutionDiagnosticCode.OpaqueNodeNotExecuted);
            Assert.That(result.Sections.Single().Values, Has.Count.EqualTo(1));
        }

        [Test]
        public void EveryWinnerRetainsOverriddenCandidates()
        {
            Fixture fixture = Two("[S]\nK=low", "[S]\nK=high", 10, 20);

            IniResolvedValue value = GetValue(Resolve(fixture), "s", "k");

            Assert.That(value.CandidateChain.Single(candidate =>
                candidate.Document.CandidateId == "candidate-low").Disposition,
                Is.EqualTo(IniValueCandidateDisposition.OverriddenByFileComposition));
        }

        [Test]
        public void ResolutionDoesNotModifyTheRawDocuments()
        {
            Fixture fixture = One("[S]\nK=  V  ");
            string hash = fixture.Candidates[0].Document.CanonicalModelSha256;

            Resolve(fixture, Policy(whitespace: IniWhitespaceReadPolicy.TrimAsciiSpaceAndTab));

            Assert.That(fixture.Candidates[0].Document.CanonicalModelSha256, Is.EqualTo(hash));
            Assert.That(Encoding.ASCII.GetString(
                ((IniKeyValueNode)fixture.Candidates[0].Document.Nodes[1]).Value.ToArray()),
                Is.EqualTo("V  "));
        }

        [Test]
        public void CandidateBudgetFailsWithoutPartialSuccess()
        {
            Fixture fixture = One("[S]\nA=1\nB=2");
            var limits = new IniResolutionLimits(4, 4, 1, 10, 20);

            IniResolutionResult result = Resolve(fixture, Policy(), limits);

            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Failed));
            AssertCode(result, IniResolutionDiagnosticCode.ValueCandidateBudgetExceeded);
        }

        [Test]
        public void DocumentBudgetFailsWithoutUnboundedWork()
        {
            Fixture fixture = Two("[S]\nA=1", "[S]\nA=2", 10, 20);
            var limits = new IniResolutionLimits(1, 4, 10, 10, 20);

            IniResolutionResult result = Resolve(fixture, Policy(), limits);

            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Failed));
            AssertCode(result, IniResolutionDiagnosticCode.DocumentBudgetExceeded);
        }

        [Test]
        public void CandidateEnumerationStopsAfterMaximumPlusOne()
        {
            Fixture fixture = Three("one", "two", "three");
            var lazy = new ThrowingCandidateEnumerable(fixture.Candidates);
            var limits = new IniResolutionLimits(2, 4, 10, 10, 20);

            IniResolutionResult result = new IniRuntimeResolver().Resolve(
                new IniLoadPlan("bounded-lazy-plan", fixture.Layers),
                lazy,
                Policy(),
                limits);

            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Failed));
            AssertCode(result, IniResolutionDiagnosticCode.DocumentBudgetExceeded);
            Assert.That(lazy.MoveNextCount, Is.EqualTo(3));
            Assert.That(lazy.ThrowReached, Is.False);
            Assert.That(result.Trace.DocumentCandidates.Select(value => value.CandidateId),
                Is.EqualTo(new[] { "candidate-low", "candidate-mid" }));
        }

        [Test]
        public void NullCandidateWithinBudgetStillThrowsArgumentException()
        {
            Fixture fixture = One("[S]\nK=V");
            fixture.Candidates[0] = null;

            Assert.Throws<ArgumentException>(() => Resolve(fixture));
        }

        [Test]
        public void DuplicateCandidateIdWithinBudgetStillFailsClosed()
        {
            Fixture fixture = Two("[S]\nK=low", "[S]\nK=high", 10, 20);
            fixture.Candidates[1] = Candidate(
                "candidate-low", "layer-high", "source-high", "[S]\nK=high",
                "expandmd01.mix", "rulesmd.ini");

            IniResolutionResult result = Resolve(fixture);

            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Failed));
            AssertCode(result, IniResolutionDiagnosticCode.DuplicateCandidateId);
        }

        [Test]
        public void LoadPlanRequiresPrematerializedTrustedLayerList()
        {
            ParameterInfo layerParameter = typeof(IniLoadPlan).GetConstructors()
                .Single().GetParameters()[1];

            Assert.That(layerParameter.ParameterType,
                Is.EqualTo(typeof(IReadOnlyList<IniLoadLayer>)));
        }

        [Test]
        public void DiagnosticsExposeLogicalPathsButNoAbsolutePaths()
        {
            Fixture fixture = Two("[S]\nK=one", "[S]\nK=two", 20, 20);

            IniResolutionResult result = Resolve(fixture);

            Assert.That(result.Diagnostics.All(value =>
                value.LogicalPath == null ||
                !value.LogicalPath.Value.Contains(":") &&
                !value.LogicalPath.Value.Contains("\\")), Is.True);
        }

        [Test]
        public void CompleteResolutionResultCannotBeForgedThroughPublicApi()
        {
            Type resultType = typeof(IniResolutionResult);
            Assert.That(resultType.IsPublic || resultType.IsNestedPublic, Is.False);
            Assert.That(
                resultType.GetConstructors(
                    BindingFlags.Public | BindingFlags.Instance),
                Is.Empty);
            Assert.That(
                resultType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(method => method.ReturnType == resultType),
                Is.Empty);
        }

        [Test]
        public void AdditionalEqualsSignsRemainInTheEffectiveValue()
        {
            Fixture fixture = One("[S]\nK=A=B=C");

            Assert.That(Value(Resolve(fixture), "s", "k"), Is.EqualTo("A=B=C"));
        }

        private static IniResolutionResult Resolve(
            Fixture fixture,
            IniResolutionPolicy policy = null,
            IniResolutionLimits limits = null)
        {
            return new IniRuntimeResolver().Resolve(
                new IniLoadPlan("synthetic-load-plan", fixture.Layers),
                fixture.Candidates,
                policy ?? Policy(),
                limits);
        }

        private static Fixture One(string content, params string[] chain)
        {
            string[] actualChain = chain.Length == 0
                ? new[] { "rulesmd.ini" }
                : chain;
            return new Fixture(
                new List<IniLoadLayer>
                {
                    Layer("layer-only", "source-only", IniLoadLayerKind.TestSource,
                        10, actualChain)
                },
                new List<IniCandidateDocument>
                {
                    Candidate("candidate-only", "layer-only", "source-only", content,
                        actualChain)
                });
        }

        private static Fixture OneEncoded(
            string content,
            IniPhysicalEncodingKind encoding)
        {
            string[] chain = { "rulesmd.ini" };
            return new Fixture(
                new List<IniLoadLayer>
                {
                    Layer("layer-only", "source-only", IniLoadLayerKind.TestSource,
                        10, chain)
                },
                new List<IniCandidateDocument>
                {
                    Candidate(
                        "candidate-only",
                        "layer-only",
                        "source-only",
                        EncodeWithBom(content, encoding),
                        chain)
                });
        }

        private static Fixture Two(
            string low,
            string high,
            int lowPriority,
            int highPriority,
            string lowSource = "source-low",
            string highSource = "source-high")
        {
            return new Fixture(
                new List<IniLoadLayer>
                {
                    Layer("layer-low", lowSource, IniLoadLayerKind.BaseMix,
                        lowPriority, "ra2md.mix", "localmd.mix", "rulesmd.ini"),
                    Layer("layer-high", highSource, IniLoadLayerKind.ExpandMix,
                        highPriority, "expandmd01.mix", "rulesmd.ini")
                },
                new List<IniCandidateDocument>
                {
                    Candidate("candidate-low", "layer-low", lowSource, low,
                        "ra2md.mix", "localmd.mix", "rulesmd.ini"),
                    Candidate("candidate-high", "layer-high", highSource, high,
                        "expandmd01.mix", "rulesmd.ini")
                });
        }

        private static Fixture Three(string low, string middle, string high)
        {
            return new Fixture(
                new List<IniLoadLayer>
                {
                    Layer("layer-low", "source-low", IniLoadLayerKind.BaseMix,
                        10, "ra2md.mix", "localmd.mix", "rulesmd.ini"),
                    Layer("layer-mid", "source-mid", IniLoadLayerKind.ExpandMix,
                        20, "expandmd02.mix", "rulesmd.ini"),
                    Layer("layer-high", "source-high", IniLoadLayerKind.LooseDirectory,
                        30, "rulesmd.ini")
                },
                new List<IniCandidateDocument>
                {
                    Candidate("candidate-low", "layer-low", "source-low", "[S]\nK=" + low,
                        "ra2md.mix", "localmd.mix", "rulesmd.ini"),
                    Candidate("candidate-mid", "layer-mid", "source-mid", "[S]\nK=" + middle,
                        "expandmd02.mix", "rulesmd.ini"),
                    Candidate("candidate-high", "layer-high", "source-high", "[S]\nK=" + high,
                        "rulesmd.ini")
                });
        }

        private static IniLoadLayer Layer(
            string layerId,
            string sourceId,
            IniLoadLayerKind kind,
            int? priority,
            params string[] chain)
        {
            return new IniLoadLayer(
                layerId,
                sourceId,
                kind,
                chain.Select(LogicalContentPath.Parse),
                priority,
                Evidence());
        }

        private static IniCandidateDocument Candidate(
            string candidateId,
            string layerId,
            string sourceId,
            string content,
            params string[] chain)
        {
            return Candidate(
                candidateId,
                layerId,
                sourceId,
                Encoding.ASCII.GetBytes(content),
                chain);
        }

        private static IniCandidateDocument Candidate(
            string candidateId,
            string layerId,
            string sourceId,
            byte[] content,
            params string[] chain)
        {
            LogicalContentPath[] paths = chain.Select(LogicalContentPath.Parse).ToArray();
            LogicalContentPath logicalName = paths[paths.Length - 1];
            IniParseResult parsed = WestwoodIniReader.Read(
                content,
                new BinarySourceContext(
                    "ini-runtime-resolution",
                    sourceId,
                    logicalName),
                new IniSourceProvenance(sourceId, paths));
            Assert.That(parsed.IsSuccess, Is.True);
            return new IniCandidateDocument(candidateId, layerId, logicalName, parsed.Document);
        }

        private static IniResolutionPolicy Policy(
            IniFileCompositionPolicy fileComposition =
                IniFileCompositionPolicy.OverlayDocumentsLowToHigh,
            IniNameComparisonPolicy nameComparison =
                IniNameComparisonPolicy.OrdinalIgnoreCaseAscii,
            IniDuplicateSectionPolicy duplicateSections =
                IniDuplicateSectionPolicy.MergeSectionsInFileOrder,
            IniDuplicateKeyPolicy duplicateKeys = IniDuplicateKeyPolicy.LastKeyWins,
            IniInlineCommentPolicy inlineComments =
                IniInlineCommentPolicy.PreserveSemicolonInValue,
            IniWhitespaceReadPolicy whitespace = IniWhitespaceReadPolicy.Preserve,
            IniEmptyValuePolicy emptyValues = IniEmptyValuePolicy.OverridesEarlierValue)
        {
            IniResolutionEvidence evidence = Evidence();
            return new IniResolutionPolicy(
                fileComposition, evidence,
                nameComparison, evidence,
                duplicateSections, evidence,
                duplicateKeys, evidence,
                inlineComments, evidence,
                whitespace, evidence,
                emptyValues, evidence);
        }

        private static IniResolutionEvidence Evidence()
        {
            return new IniResolutionEvidence(
                IniResolutionEvidenceLevel.ConfiguredForTesting,
                "wp02g1-synthetic-policy");
        }

        private static IniResolvedValue GetValue(
            IniResolutionResult result,
            string section,
            string key)
        {
            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Complete),
                string.Join(",", result.Diagnostics.Select(value => value.Code)));
            return result.Sections.Single(value => value.Name == section)
                .Values.Single(value => value.KeyName == key);
        }

        private static string Value(IniResolutionResult result, string section, string key)
        {
            return Encoding.ASCII.GetString(
                GetValue(result, section, key).Winner.CopyEffectiveValueBytes());
        }

        private static string DecodedValue(
            IniResolutionResult result,
            IniPhysicalEncodingKind encoding)
        {
            return GetUtf16Encoding(encoding).GetString(
                GetValue(result, "s", "k").Winner.CopyEffectiveValueBytes());
        }

        private static byte[] EncodeWithBom(
            string value,
            IniPhysicalEncodingKind encoding)
        {
            Encoding textEncoding = GetUtf16Encoding(encoding);
            return textEncoding.GetPreamble()
                .Concat(textEncoding.GetBytes(value))
                .ToArray();
        }

        private static Encoding GetUtf16Encoding(IniPhysicalEncodingKind encoding)
        {
            if (encoding == IniPhysicalEncodingKind.Utf16LittleEndianWithBom)
            {
                return new UnicodeEncoding(false, true, true);
            }

            if (encoding == IniPhysicalEncodingKind.Utf16BigEndianWithBom)
            {
                return new UnicodeEncoding(true, true, true);
            }

            throw new ArgumentOutOfRangeException(nameof(encoding));
        }

        private static string[] TraceIds(IniResolutionResult result)
        {
            return result.Trace.DocumentCandidates.Select(value => value.CandidateId).ToArray();
        }

        private static void AssertAmbiguous(
            IniResolutionResult result,
            IniResolutionDiagnosticCode code)
        {
            Assert.That(result.Status, Is.EqualTo(IniResolutionStatus.Ambiguous));
            AssertCode(result, code);
        }

        private static void AssertCode(
            IniResolutionResult result,
            IniResolutionDiagnosticCode code)
        {
            Assert.That(result.Diagnostics.Any(value => value.Code == code), Is.True,
                string.Join(",", result.Diagnostics.Select(value => value.Code)));
        }

        private sealed class Fixture
        {
            public Fixture(
                List<IniLoadLayer> layers,
                List<IniCandidateDocument> candidates)
            {
                Layers = layers;
                Candidates = candidates;
            }

            public List<IniLoadLayer> Layers { get; }
            public List<IniCandidateDocument> Candidates { get; }
        }

        private sealed class ThrowingCandidateEnumerable :
            IEnumerable<IniCandidateDocument>
        {
            private readonly IReadOnlyList<IniCandidateDocument> candidates;

            public ThrowingCandidateEnumerable(
                IReadOnlyList<IniCandidateDocument> candidates)
            {
                this.candidates = candidates;
            }

            public int MoveNextCount { get; private set; }
            public bool ThrowReached { get; private set; }

            public IEnumerator<IniCandidateDocument> GetEnumerator()
            {
                return Enumerate().GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private IEnumerable<IniCandidateDocument> Enumerate()
            {
                foreach (IniCandidateDocument candidate in candidates)
                {
                    MoveNextCount++;
                    yield return candidate;
                }

                MoveNextCount++;
                ThrowReached = true;
                throw new InvalidOperationException(
                    "The resolver enumerated MaxDocuments + 2 candidates.");
            }
        }
    }
}
