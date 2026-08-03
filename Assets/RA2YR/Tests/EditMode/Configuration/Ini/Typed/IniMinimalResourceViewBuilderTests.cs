using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Configuration.Ini.Typed;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;

namespace RA2YR.Tests.EditMode.Configuration.Ini.Typed
{
    public sealed class IniMinimalResourceViewBuilderTests
    {
        [Test]
        public void RulesAcceptsOnlyCompleteResolutionInput()
        {
            IniTypedViewResult<IniRulesResourceDocument> result =
                IniMinimalResourceViewBuilder.BuildRules(
                    Complete("[VehicleTypes]\n0=ONE"),
                    IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii);

            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Complete));
            Assert.That(result.Document.EntryCount, Is.EqualTo(1));
            Assert.That(result.InputStatus, Is.EqualTo(IniResolutionStatus.Complete));
        }

        [Test]
        public void AmbiguousResolutionIsRejectedWithoutTypedDocument()
        {
            IniResolutionResult input = Resolve(
                new[]
                {
                    Source("candidate-a", "layer-a", "source-a", 10,
                        "[VehicleTypes]\n0=ONE", "a.mix", "rulesmd.ini"),
                    Source("candidate-b", "layer-b", "source-b", 10,
                        "[VehicleTypes]\n0=TWO", "b.mix", "rulesmd.ini")
                },
                IniFileCompositionPolicy.SelectHighestPriorityDocument);

            IniTypedViewResult<IniRulesResourceDocument> result =
                IniMinimalResourceViewBuilder.BuildRules(
                    input,
                    IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii);

            Assert.That(input.Status, Is.EqualTo(IniResolutionStatus.Ambiguous));
            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Ambiguous));
            Assert.That(result.Document, Is.Null);
            Assert.That(result.InputTrace.DocumentCandidates.Count, Is.EqualTo(2));
        }

        [Test]
        public void FailedResolutionIsRejectedWithoutTypedDocument()
        {
            SourceFixture source = Source(
                "candidate-a", "layer-a", "source-a", 10,
                "[VehicleTypes]\n0=ONE", "rulesmd.ini");
            var mismatched = new IniLoadLayer(
                "layer-a",
                "DIFFERENT-source",
                IniLoadLayerKind.TestSource,
                source.Layer.LogicalChain,
                10,
                Evidence());
            IniResolutionResult input = new IniRuntimeResolver().Resolve(
                new IniLoadPlan("failed", new[] { mismatched }),
                new[] { source.Candidate },
                Policy(IniFileCompositionPolicy.SelectHighestPriorityDocument));

            IniTypedViewResult<IniRulesResourceDocument> result =
                IniMinimalResourceViewBuilder.BuildRules(
                    input,
                    IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii);

            Assert.That(input.Status, Is.EqualTo(IniResolutionStatus.Failed));
            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Failed));
            Assert.That(result.Document, Is.Null);
            Assert.That(result.InputDiagnostics.Any(value =>
                value.Code == IniResolutionDiagnosticCode.IncompleteProvenance), Is.True);
        }

        [Test]
        public void RulesRegistriesPreserveExplicitFileOrderAndOrdinalSpelling()
        {
            IniRulesRegistry registry = Rules(
                "[VehicleTypes]\n02=TWO\n0=ZERO\n1=ONE")
                .Registries.Single(value => value.Kind == IniRulesRegistryKind.VehicleTypes);

            Assert.That(registry.Entries.Select(value => value.OriginalOrdinalKey),
                Is.EqualTo(new[] { "02", "0", "1" }));
            Assert.That(registry.Entries.Select(value => value.Ordinal),
                Is.EqualTo(new[] { 2, 0, 1 }));
        }

        [Test]
        public void DuplicateRegistryIdentifiersArePreservedAndMarkedIncomplete()
        {
            IniTypedViewResult<IniRulesResourceDocument> result = RulesResult(
                "[VehicleTypes]\n0=ONE\n1=ONE");

            Assert.That(result.Document.EntryCount, Is.EqualTo(2));
            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Incomplete));
            Assert.That(result.Diagnostics.Count(value =>
                value.Code == IniTypedDiagnosticCode.DuplicateRegistryIdentifier),
                Is.EqualTo(2));
        }

        [Test]
        public void DuplicateRegistryOrdinalsArePreservedWithoutChoosingAWinner()
        {
            IniTypedViewResult<IniRulesResourceDocument> result = RulesResult(
                "[VehicleTypes]\n0=ONE\n00=TWO");
            IniRulesRegistry registry = result.Document.Registries.Single(value =>
                value.Kind == IniRulesRegistryKind.VehicleTypes);

            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Incomplete));
            Assert.That(registry.Entries.Select(value => value.OriginalOrdinalKey),
                Is.EqualTo(new[] { "0", "00" }));
            Assert.That(registry.Entries.Select(value => value.Ordinal),
                Is.EqualTo(new[] { 0, 0 }));
            Assert.That(result.Diagnostics.Count(value =>
                value.Code == IniTypedDiagnosticCode.DuplicateRegistryOrdinal),
                Is.EqualTo(2));
            Assert.That(registry.Entries.All(value =>
                value.Identifier.Value.SourceTrace.Winner != null), Is.True);
        }

        [Test]
        public void RegistryOrdinalsAreScopedToTheirOwnRegistry()
        {
            IniTypedViewResult<IniRulesResourceDocument> result = RulesResult(
                "[VehicleTypes]\n0=ONE\n[InfantryTypes]\n0=TWO");

            Assert.That(result.Document.EntryCount, Is.EqualTo(2));
            Assert.That(result.Diagnostics.Any(value =>
                value.Code == IniTypedDiagnosticCode.DuplicateRegistryOrdinal), Is.False);
        }

        [Test]
        public void InvalidRegistryOrdinalIsNotExecuted()
        {
            IniTypedViewResult<IniRulesResourceDocument> result = RulesResult(
                "[VehicleTypes]\n-1=ONE\nname=TWO");

            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Incomplete));
            Assert.That(result.Document.EntryCount, Is.EqualTo(0));
            Assert.That(result.Diagnostics.Count(value =>
                value.Code == IniTypedDiagnosticCode.InvalidRegistryOrdinal), Is.EqualTo(2));
        }

        [Test]
        public void AllFiveRequestedRegistriesAreRepresented()
        {
            IniRulesResourceDocument document = Rules(
                "[AircraftTypes]\n0=A\n" +
                "[BuildingTypes]\n0=B\n" +
                "[InfantryTypes]\n0=I\n" +
                "[VehicleTypes]\n0=V\n" +
                "[Animations]\n0=N");

            Assert.That(document.Registries.Select(value => value.Kind),
                Is.EquivalentTo(Enum.GetValues(typeof(IniRulesRegistryKind))));
            Assert.That(document.EntryCount, Is.EqualTo(5));
        }

        [Test]
        public void ArtUsesExplicitImageAndDoesNotUseSectionNameFallback()
        {
            IniArtResourceDocument document = Art(
                "[UNIT]\nVoxel=yes\n[OTHER]\nImage=EXPLICIT");
            IniArtResourceRecord unit = document.Records.Single(value =>
                value.SectionIdentifier == "unit");
            IniArtResourceRecord other = document.Records.Single(value =>
                value.SectionIdentifier == "other");

            Assert.That(Field(unit, IniArtFieldKind.Image).Status,
                Is.EqualTo(IniTypedValueStatus.Missing));
            Assert.That(Field(other, IniArtFieldKind.Image).Parsed.Value.Identifier,
                Is.EqualTo("EXPLICIT"));
        }

        [Test]
        public void ArtParsesExplicitVoxelAndReferenceFields()
        {
            IniArtResourceRecord record = Art(
                "[UNIT]\nImage=BODY.SHP\nVoxel=no\nCameo=ICON\nAltCameo=ALT\nBuildup=BUILD")
                .Records.Single();

            Assert.That(Field(record, IniArtFieldKind.Voxel).Parsed.Value.BooleanValue, Is.False);
            Assert.That(record.RouteCandidate, Is.EqualTo(IniResourceRouteCandidate.Shp));
            Assert.That(record.References.References.Select(value => value.Field),
                Is.EquivalentTo(new[]
                {
                    IniArtFieldKind.Image,
                    IniArtFieldKind.Cameo,
                    IniArtFieldKind.AltCameo,
                    IniArtFieldKind.Buildup
                }));
            Assert.That(record.References.References.Single(value =>
                value.Field == IniArtFieldKind.Image).ExplicitExtension,
                Is.EqualTo(IniExplicitResourceExtension.Shp));
        }

        [Test]
        public void VoxelYesProducesOnlyAnExplicitRouteCandidate()
        {
            IniArtResourceRecord record = Art("[UNIT]\nVoxel=YES").Records.Single();

            Assert.That(record.RouteCandidate, Is.EqualTo(IniResourceRouteCandidate.Vxl));
            Assert.That(Field(record, IniArtFieldKind.Image).Status,
                Is.EqualTo(IniTypedValueStatus.Missing));
            Assert.That(record.References.References, Is.Empty);
        }

        [Test]
        public void InvalidVoxelRemainsInvalidAndRouteUnknown()
        {
            IniTypedViewResult<IniArtResourceDocument> result = ArtResult(
                "[UNIT]\nVoxel=maybe");
            IniArtResourceRecord record = result.Document.Records.Single();

            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Incomplete));
            Assert.That(Field(record, IniArtFieldKind.Voxel).Status,
                Is.EqualTo(IniTypedValueStatus.Invalid));
            Assert.That(record.RouteCandidate, Is.EqualTo(IniResourceRouteCandidate.Unknown));
        }

        [Test]
        public void ArtCaseInsensitiveMultipleMatchIsAmbiguousAndFailClosed()
        {
            IniResolutionResult input = Resolve(
                new[]
                {
                    Source("candidate-only", "layer-only", "source-only", 10,
                        "[UNIT]\nImage=FIRST\nimage=SECOND", "artmd.ini")
                },
                IniFileCompositionPolicy.SelectHighestPriorityDocument,
                IniNameComparisonPolicy.OrdinalRawAscii);

            IniTypedViewResult<IniArtResourceDocument> result =
                IniMinimalResourceViewBuilder.BuildArt(
                    input,
                    IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                    IniBooleanCasePolicy.OrdinalIgnoreCaseAscii);
            IniArtResourceRecord record = result.Document.Records.Single();
            IniArtResourceField image = Field(record, IniArtFieldKind.Image);

            Assert.That(input.Sections.Single().Values.Select(value => value.KeyName),
                Is.EquivalentTo(new[] { "Image", "image" }));
            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Incomplete));
            Assert.That(image.Status, Is.EqualTo(IniTypedValueStatus.Ambiguous));
            Assert.That(image.Parsed, Is.Null);
            Assert.That(image.ParsedCandidates.Count, Is.EqualTo(2));
            Assert.That(image.ParsedCandidates.All(candidate =>
                candidate.Value.SourceTrace.Winner != null), Is.True);
            Assert.That(record.References.References.Any(reference =>
                reference.Field == IniArtFieldKind.Image), Is.False);
            Assert.That(record.RouteCandidate, Is.EqualTo(IniResourceRouteCandidate.Unknown));
            Assert.That(result.Diagnostics.Any(value =>
                value.Code == IniTypedDiagnosticCode.ArtSectionAmbiguous), Is.True);
        }

        [Test]
        public void ArtAmbiguityCanonicalOrderDoesNotDependOnResolvedValueEnumeration()
        {
            IniResolutionResult input = Resolve(
                new[]
                {
                    Source("candidate-only", "layer-only", "source-only", 10,
                        "[UNIT]\nImage=FIRST\nimage=SECOND", "artmd.ini")
                },
                IniFileCompositionPolicy.SelectHighestPriorityDocument,
                IniNameComparisonPolicy.OrdinalRawAscii);
            IniResolutionResult reversed = IniResolutionResult.Create(
                input.Status,
                input.Sections.Select(section => new IniResolvedSection(
                    section.Name,
                    section.Values.Reverse())),
                input.Trace,
                input.Diagnostics);

            IniArtResourceDocument first = IniMinimalResourceViewBuilder.BuildArt(
                input,
                IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                IniBooleanCasePolicy.OrdinalIgnoreCaseAscii).Document;
            IniArtResourceDocument second = IniMinimalResourceViewBuilder.BuildArt(
                reversed,
                IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                IniBooleanCasePolicy.OrdinalIgnoreCaseAscii).Document;
            IniArtResourceDocument changed = IniMinimalResourceViewBuilder.BuildArt(
                Resolve(
                    new[]
                    {
                        Source("candidate-only", "layer-only", "source-only", 10,
                            "[UNIT]\nImage=FIRST\nimage=CHANGED", "artmd.ini")
                    },
                    IniFileCompositionPolicy.SelectHighestPriorityDocument,
                    IniNameComparisonPolicy.OrdinalRawAscii),
                IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                IniBooleanCasePolicy.OrdinalIgnoreCaseAscii).Document;

            Assert.That(first.CanonicalModelSha256, Is.EqualTo(second.CanonicalModelSha256));
            Assert.That(first.CanonicalModelSha256, Is.Not.EqualTo(
                changed.CanonicalModelSha256));
            Assert.That(Field(first.Records.Single(), IniArtFieldKind.Image)
                .ParsedCandidates.Select(candidate => candidate.Value.Identifier),
                Is.EqualTo(Field(second.Records.Single(), IniArtFieldKind.Image)
                    .ParsedCandidates.Select(candidate => candidate.Value.Identifier)));
        }

        [Test]
        public void CandidateChainPreservesWinnerOverriddenPhysicalLinesAndMixProvenance()
        {
            IniResolutionResult input = Resolve(
                new[]
                {
                    Source("candidate-low", "layer-low", "source-low", 10,
                        "[UNIT]\nImage=LOW", "ra2md.mix", "localmd.mix", "artmd.ini"),
                    Source("candidate-high", "layer-high", "source-high", 20,
                        "[UNIT]\nImage=HIGH", "expandmd01.mix", "artmd.ini")
                },
                IniFileCompositionPolicy.OverlayDocumentsLowToHigh);

            IniTypedViewResult<IniArtResourceDocument> result =
                IniMinimalResourceViewBuilder.BuildArt(
                    input,
                    IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                    IniBooleanCasePolicy.OrdinalIgnoreCaseAscii);
            IniValueSourceTrace trace = Field(
                result.Document.Records.Single(),
                IniArtFieldKind.Image).Parsed.Value.SourceTrace;

            Assert.That(trace.Candidates.Count, Is.EqualTo(2));
            Assert.That(trace.Winner.SourceId, Is.EqualTo("source-high"));
            Assert.That(trace.Winner.KeyPhysicalLineId, Is.GreaterThanOrEqualTo(0));
            Assert.That(trace.Winner.SectionPhysicalLineId, Is.GreaterThanOrEqualTo(0));
            Assert.That(trace.Winner.LogicalChain.Select(value => value.Value),
                Is.EqualTo(new[] { "expandmd01.mix", "artmd.ini" }));
            Assert.That(trace.Candidates.Any(value =>
                value.Disposition == IniValueCandidateDisposition.OverriddenByFileComposition),
                Is.True);
        }

        [Test]
        public void OpaqueLinePreventsCompleteTypedClaim()
        {
            IniTypedViewResult<IniRulesResourceDocument> result = RulesResult(
                "[VehicleTypes]\n0=ONE\nUNRESOLVED DIRECTIVE");

            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Incomplete));
            Assert.That(result.Diagnostics.Any(value =>
                value.Code == IniTypedDiagnosticCode.OpaqueMayAffectTarget), Is.True);
        }

        [Test]
        public void InlineSemicolonPreventsCompleteTypedClaim()
        {
            IniTypedViewResult<IniArtResourceDocument> result = ArtResult(
                "[UNIT]\nImage=BODY;UNRESOLVED");

            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Incomplete));
            Assert.That(result.Diagnostics.Any(value =>
                value.Code == IniTypedDiagnosticCode.InlineSemicolonMayAffectTarget), Is.True);
        }

        [Test]
        public void DuplicateKeyPolicyIsVisibleAndDoesNotBecomeStockSemantics()
        {
            IniTypedViewResult<IniArtResourceDocument> result = ArtResult(
                "[UNIT]\nImage=FIRST\nImage=SECOND");

            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Incomplete));
            Assert.That(Field(result.Document.Records.Single(), IniArtFieldKind.Image)
                .Parsed.Value.Identifier, Is.EqualTo("SECOND"));
            Assert.That(result.Diagnostics.Any(value =>
                value.Code == IniTypedDiagnosticCode.DuplicateKeyMayAffectTarget), Is.True);
        }

        [Test]
        public void NameMatchingIsAsciiOnlyAndCultureIndependent()
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                IniRulesResourceDocument document = Rules("[vehicletYPES]\n0=UNIT");
                Assert.That(document.EntryCount, Is.EqualTo(1));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void DocumentsExposeReadOnlyCollectionsAndStableHashes()
        {
            IniArtResourceDocument first = Art("[UNIT]\nImage=BODY\nVoxel=no");
            IniArtResourceDocument second = Art("[UNIT]\nImage=BODY\nVoxel=no");

            Assert.That(first.CanonicalModelSha256, Is.EqualTo(second.CanonicalModelSha256));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<IniArtResourceRecord>)first.Records).Add(first.Records[0]));
        }

        [Test]
        public void RegistryBudgetFailsClosedWithoutTypedDocument()
        {
            var limits = new IniTypedViewLimits(100, 10, 1, 10, 10, 10);

            IniTypedViewResult<IniRulesResourceDocument> result =
                IniMinimalResourceViewBuilder.BuildRules(
                    Complete("[VehicleTypes]\n0=ONE\n1=TWO"),
                    IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                    limits);

            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Failed));
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo(IniTypedDiagnosticCode.RegistryEntryBudgetExceeded));
        }

        [Test]
        public void ArtRecordBudgetFailsClosedWithoutTypedDocument()
        {
            var limits = new IniTypedViewLimits(100, 10, 10, 1, 10, 10);

            IniTypedViewResult<IniArtResourceDocument> result =
                IniMinimalResourceViewBuilder.BuildArt(
                    Complete("[ONE]\nImage=A\n[TWO]\nImage=B", "artmd.ini"),
                    IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                    IniBooleanCasePolicy.OrdinalIgnoreCaseAscii,
                    limits);

            Assert.That(result.Status, Is.EqualTo(IniTypedViewStatus.Failed));
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics.Single().Code,
                Is.EqualTo(IniTypedDiagnosticCode.ArtRecordBudgetExceeded));
        }

        [Test]
        public void CoreAssemblyDoesNotReferenceUnityEngineOrSystemDrawing()
        {
            string[] references = typeof(IniMinimalResourceViewBuilder).Assembly
                .GetReferencedAssemblies()
                .Select(value => value.Name)
                .ToArray();

            Assert.That(references, Does.Not.Contain("UnityEngine"));
            Assert.That(references, Does.Not.Contain("System.Drawing"));
        }

        private static IniRulesResourceDocument Rules(string content)
        {
            IniTypedViewResult<IniRulesResourceDocument> result = RulesResult(content);
            Assert.That(result.Status,
                Is.EqualTo(IniTypedViewStatus.Complete).Or.EqualTo(IniTypedViewStatus.Incomplete));
            return result.Document;
        }

        private static IniTypedViewResult<IniRulesResourceDocument> RulesResult(string content)
        {
            return IniMinimalResourceViewBuilder.BuildRules(
                Complete(content),
                IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii);
        }

        private static IniArtResourceDocument Art(string content)
        {
            IniTypedViewResult<IniArtResourceDocument> result = ArtResult(content);
            Assert.That(result.Status,
                Is.EqualTo(IniTypedViewStatus.Complete).Or.EqualTo(IniTypedViewStatus.Incomplete));
            return result.Document;
        }

        private static IniTypedViewResult<IniArtResourceDocument> ArtResult(string content)
        {
            return IniMinimalResourceViewBuilder.BuildArt(
                Complete(content, "artmd.ini"),
                IniTypedNameComparisonPolicy.OrdinalIgnoreCaseAscii,
                IniBooleanCasePolicy.OrdinalIgnoreCaseAscii);
        }

        private static IniArtResourceField Field(
            IniArtResourceRecord record,
            IniArtFieldKind kind)
        {
            return record.Fields.Single(value => value.Kind == kind);
        }

        private static IniResolutionResult Complete(
            string content,
            string logicalName = "rulesmd.ini")
        {
            return Resolve(
                new[]
                {
                    Source("candidate-only", "layer-only", "source-only", 10,
                        content, logicalName)
                },
                IniFileCompositionPolicy.SelectHighestPriorityDocument);
        }

        private static IniResolutionResult Resolve(
            IEnumerable<SourceFixture> sources,
            IniFileCompositionPolicy composition,
            IniNameComparisonPolicy nameComparison =
                IniNameComparisonPolicy.OrdinalIgnoreCaseAscii)
        {
            SourceFixture[] values = sources.ToArray();
            return new IniRuntimeResolver().Resolve(
                new IniLoadPlan("wp02g2-tests", values.Select(value => value.Layer).ToArray()),
                values.Select(value => value.Candidate),
                Policy(composition, nameComparison));
        }

        private static SourceFixture Source(
            string candidateId,
            string layerId,
            string sourceId,
            int priority,
            string content,
            params string[] chain)
        {
            LogicalContentPath[] paths = chain.Select(LogicalContentPath.Parse).ToArray();
            LogicalContentPath logicalName = paths[paths.Length - 1];
            IniParseResult parsed = WestwoodIniReader.Read(
                Encoding.ASCII.GetBytes(content),
                new BinarySourceContext("wp02g2-tests", sourceId, logicalName),
                new IniSourceProvenance(sourceId, paths));
            Assert.That(parsed.IsSuccess, Is.True);
            return new SourceFixture(
                new IniLoadLayer(
                    layerId,
                    sourceId,
                    IniLoadLayerKind.TestSource,
                    paths,
                    priority,
                    Evidence()),
                new IniCandidateDocument(candidateId, layerId, logicalName, parsed.Document));
        }

        private static IniResolutionPolicy Policy(
            IniFileCompositionPolicy composition,
            IniNameComparisonPolicy nameComparison =
                IniNameComparisonPolicy.OrdinalIgnoreCaseAscii)
        {
            IniResolutionEvidence evidence = Evidence();
            return new IniResolutionPolicy(
                composition, evidence,
                nameComparison, evidence,
                IniDuplicateSectionPolicy.MergeSectionsInFileOrder, evidence,
                IniDuplicateKeyPolicy.LastKeyWins, evidence,
                IniInlineCommentPolicy.PreserveSemicolonInValue, evidence,
                IniWhitespaceReadPolicy.Preserve, evidence,
                IniEmptyValuePolicy.OverridesEarlierValue, evidence);
        }

        private static IniResolutionEvidence Evidence()
        {
            return new IniResolutionEvidence(
                IniResolutionEvidenceLevel.ConfiguredForTesting,
                "wp02g2-synthetic-policy");
        }

        private sealed class SourceFixture
        {
            public SourceFixture(IniLoadLayer layer, IniCandidateDocument candidate)
            {
                Layer = layer;
                Candidate = candidate;
            }

            public IniLoadLayer Layer { get; }
            public IniCandidateDocument Candidate { get; }
        }
    }
}
