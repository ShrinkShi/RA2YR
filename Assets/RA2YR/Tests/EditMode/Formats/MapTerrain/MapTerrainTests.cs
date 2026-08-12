using NUnit.Framework;
using RA2YR.Core.Binary;
using RA2YR.Core.Formats.MapTerrain;

namespace RA2YR.Tests.EditMode.Formats.MapTerrain
{
    public sealed class MapTerrainTests
    {
        [Test] public void PolicyRejectsUnknownProfiles()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new MapTerrainBindingPolicy((MapTerrainIsoTileIdProfile)99));
        }

        [Test] public void MissingIsoMapFailsClosedWithoutMapDocument()
        {
            MapTerrainBindingResult result = new MapTerrainComposer().Compose(null, null, null, null, null, null, null);
            Assert.AreEqual(MapTerrainCompletionStatus.Failed, result.CompletionStatus);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.Document);
            Assert.AreEqual(MapTerrainDiagnosticCode.MissingIsoMap, result.Diagnostics[0].Code);
        }

        [Test] public void ExecutionSuppressionDoesNotFailOpen()
        {
            var state = new MapTerrainExecutionState();
            state.Fail();
            state.Suppress();
            Assert.AreEqual(MapTerrainCompletionStatus.Failed, state.CompletionStatus);
            Assert.AreEqual(1, state.SuppressedDiagnosticCount);
        }

        [Test] public void SourceModelHasNoUnityReferenceInContract()
        {
            Assert.IsFalse(typeof(MapTerrainDocument).Assembly.FullName.Contains("UnityEngine"));
        }

        [Test] public void AuthorityRemainsCandidateOnly()
        {
            MapTerrainBindingResult result = new MapTerrainComposer().Compose(null, null, null, null, null, null, null);
            Assert.AreEqual(MapTerrainAuthorityStatus.StructuralOnly, result.Document.AuthorityStatus);
        }
    }
}
