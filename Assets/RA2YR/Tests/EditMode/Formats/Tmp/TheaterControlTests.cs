using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Configuration.Ini.Resolution;
using RA2YR.Core.Content;
using RA2YR.Core.Formats.Ini;
using RA2YR.Core.Formats.Tmp;

namespace RA2YR.Tests.EditMode.Formats.Tmp
{
    public sealed class TheaterControlTests
    {
        [Test] public void ComposedControlReaderPreservesEffectiveValueProvenance() { TheaterControlDocument d = TheaterControlReader.Read(Compose("[General]\nWaterSet=1\n[TileSet0000]\nFileName=clear\nTilesInSet=2\n[TileSet0001]\nFileName=rough\nTilesInSet=3\n"), TheaterProfiles.Get(TheaterKind.Temperate)); Assert.That(d.IsSuccess, Is.True); Assert.That(d.Find("General", "WaterSet").Single().Raw, Is.EqualTo("1")); Assert.That(d.Find("General", "WaterSet").Single().Provenance.SourceId, Is.EqualTo("synthetic-theater")); }
        [Test] public void MissingGeneralIsTypedFailure() { TheaterControlDocument d = TheaterControlReader.Read(Compose("[TileSet0000]\nFileName=clear\nTilesInSet=1\n"), TheaterProfiles.Get(TheaterKind.Temperate)); Assert.That(d.IsSuccess, Is.False); Assert.That(d.Diagnostics.Any(x => x.Code == TmpDiagnosticCode.MissingGeneral), Is.True); }
        [Test] public void NumericTileSetOrderIgnoresOccurrenceOrder() { TheaterTileRegistry r = TheaterTileRegistryBuilder.Build(TheaterControlReader.Read(Compose("[General]\nWaterSet=2\n[TileSet0002]\nFileName=two\nTilesInSet=1\n[TileSet0000]\nFileName=zero\nTilesInSet=2\n"), TheaterProfiles.Get(TheaterKind.Temperate))); Assert.That(r.TileSets[0].Index, Is.EqualTo(0)); Assert.That(r.TileSets[1].Index, Is.EqualTo(2)); Assert.That(r.IdRanges[1].StartInclusive, Is.EqualTo(2)); }
        [Test] public void NonCanonicalTileSetSectionIsDiagnosed() { TheaterTileRegistry r = TheaterTileRegistryBuilder.Build(TheaterControlReader.Read(Compose("[General]\n[TileSet1]\nFileName=bad\nTilesInSet=1\n"), TheaterProfiles.Get(TheaterKind.Temperate))); Assert.That(r.Diagnostics.Any(x => x.Code == TmpDiagnosticCode.InvalidTileSetSection), Is.True); }
        [Test] public void SpecialRoleOutOfRangeFailsClosed() { TheaterTileRegistry r = TheaterTileRegistryBuilder.Build(TheaterControlReader.Read(Compose("[General]\nWaterSet=9\n[TileSet0000]\nFileName=clear\nTilesInSet=1\n"), TheaterProfiles.Get(TheaterKind.Temperate))); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Diagnostics.Any(x => x.Code == TmpDiagnosticCode.SpecialRoleOutOfRange), Is.True); }
        [Test] public void CompositionDoesNotUseLastWinsForWholeDocument() { IniResolutionResult result = Compose("[General]\nWaterSet=1\n[TileSet0000]\nFileName=clear\nTilesInSet=1\n"); Assert.That(result.IsComplete, Is.True); TheaterControlDocument d = TheaterControlReader.Read(result, TheaterProfiles.Get(TheaterKind.Snow)); Assert.That(d.Profile.Kind, Is.EqualTo(TheaterKind.Snow)); }
        [Test] public void RegistryHashIsStableAcrossRepeatedBuilds() { string text = "[General]\n[TileSet0001]\nFileName=rough\nTilesInSet=3\n[TileSet0000]\nFileName=clear\nTilesInSet=2\n"; TheaterControlDocument d = TheaterControlReader.Read(Compose(text), TheaterProfiles.Get(TheaterKind.Urban)); Assert.That(TheaterTileRegistryBuilder.Build(d).CanonicalHash, Is.EqualTo(TheaterTileRegistryBuilder.Build(d).CanonicalHash)); }

        private static IniResolutionResult Compose(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            var source = new BinarySourceContext("theater-tests", "synthetic-theater", LogicalContentPath.Parse("theater.ini"));
            var provenance = new IniSourceProvenance("synthetic-theater", new[] { LogicalContentPath.Parse("theater.ini") });
            IniParseResult parse = WestwoodIniReader.Read(bytes, source, provenance);
            Assert.That(parse.IsSuccess, Is.True);
            var layer = new IniLoadLayer("layer", "synthetic-theater", IniLoadLayerKind.TestSource,
                new[] { LogicalContentPath.Parse("theater.ini") }, 0,
                new IniResolutionEvidence(IniResolutionEvidenceLevel.ConfiguredForTesting, "m3c6-tests"));
            var plan = new IniLoadPlan("synthetic-theater-plan", new[] { layer });
            var candidate = new IniCandidateDocument("candidate", "layer", LogicalContentPath.Parse("theater.ini"), parse.Document);
            var evidence = new IniResolutionEvidence(IniResolutionEvidenceLevel.ConfiguredForTesting, "m3c6-tests");
            var policy = new IniResolutionPolicy(IniFileCompositionPolicy.SelectHighestPriorityDocument, evidence,
                IniNameComparisonPolicy.OrdinalIgnoreCaseAscii, evidence, IniDuplicateSectionPolicy.MergeSectionsInFileOrder, evidence,
                IniDuplicateKeyPolicy.LastKeyWins, evidence, IniInlineCommentPolicy.PreserveSemicolonInValue, evidence,
                IniWhitespaceReadPolicy.TrimAsciiSpaceAndTab, evidence, IniEmptyValuePolicy.OverridesEarlierValue, evidence);
            return new IniRuntimeResolver().Resolve(plan, new[] { candidate }, policy);
        }
    }
}
