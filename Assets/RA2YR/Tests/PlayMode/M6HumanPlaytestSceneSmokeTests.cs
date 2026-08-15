using System.Collections;
using System.Linq;
using NUnit.Framework;
using RA2YR.Simulation;
using RA2YR.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RA2YR.Tests.PlayMode
{
    public sealed class M6HumanPlaytestSceneSmokeTests
    {
        private static IEnumerator LoadPlaytestScene()
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync("RA2YRSyntheticSkirmish", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone) yield return null;
            yield return null;
        }

        private static UnitySyntheticSkirmishBootstrap FindBootstrap()
        {
            UnitySyntheticSkirmishBootstrap bootstrap = UnityEngine.Object.FindObjectOfType<UnitySyntheticSkirmishBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            return bootstrap;
        }

        [UnityTest]
        public IEnumerator SceneLoadsConfiguredSyntheticSkirmish()
        {
            yield return LoadPlaytestScene();
            UnitySyntheticSkirmishBootstrap bootstrap = FindBootstrap();

            Assert.That(bootstrap.IsInitialized, Is.True);
            Assert.That(bootstrap.Runtime, Is.Not.Null);
            Assert.That(bootstrap.PresentationWorld, Is.Not.Null);
            Assert.That(bootstrap.Client, Is.Not.Null);
            Assert.That(bootstrap.TerrainCellCount, Is.EqualTo(bootstrap.Runtime.Config.Width * bootstrap.Runtime.Config.Height));
            Assert.That(bootstrap.LastPresentation, Is.Not.Null);
            Assert.That(bootstrap.LastPresentation.Entities.Count, Is.GreaterThan(0));
            if (bootstrap.ExternalVisualStatus != null &&
                bootstrap.ExternalVisualStatus.IsConfigured &&
                bootstrap.ExternalVisualStatus.SourceAvailable)
            {
                TestContext.WriteLine(
                    "M6_SCENE_EXTERNAL_VISUAL_ROUTE" +
                    ";gate=" + bootstrap.ExternalVisualStatus.RouteGateStatus +
                    ";externalObjects=" + bootstrap.ExternalObjectCount +
                    ";fallbackObjects=" + bootstrap.SyntheticObjectFallbackCount);
                Assert.That(bootstrap.ExternalVisualStatus.IsLocalExternalVisualReady, Is.True);
                Assert.That(bootstrap.ExternalObjectCount, Is.GreaterThan(0));
            }
        }

        [UnityTest]
        public IEnumerator SceneAcceptsHumanSelectionMoveAndProduction()
        {
            yield return LoadPlaytestScene();
            UnitySyntheticSkirmishBootstrap bootstrap = FindBootstrap();
            EntityId unit = bootstrap.Runtime.HumanUnits[0];
            HumanPlaytestEntitySnapshot before = bootstrap.Runtime.CaptureSnapshot().Entities.Single(x => x.Entity.Equals(unit));

            Assert.That(bootstrap.SelectSingle(unit), Is.True);
            Assert.That(bootstrap.IssueMove(new CellCoordinate(12, 7)).IsSuccess, Is.True);
            bootstrap.StepSimulation(8);
            HumanPlaytestEntitySnapshot after = bootstrap.Runtime.CaptureSnapshot().Entities.Single(x => x.Entity.Equals(unit));
            Assert.That(after.X != before.X || after.Y != before.Y, Is.True);

            Assert.That(bootstrap.QueueProduction(), Is.True);
            bootstrap.StepSimulation(6);
            Assert.That(bootstrap.Runtime.ProductionEvents, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SceneRunsRuleBasedOpponentAndCombat()
        {
            yield return LoadPlaytestScene();
            UnitySyntheticSkirmishBootstrap bootstrap = FindBootstrap();
            bootstrap.StepSimulation(80);

            Assert.That(bootstrap.Runtime.CombatEvents, Is.GreaterThan(0));
            HumanPlaytestSnapshot snapshot = bootstrap.Runtime.CaptureSnapshot();
            Assert.That(snapshot.Entities.Any(x => x.Owner.Value == bootstrap.Runtime.HumanPlayer.Value && x.Health < x.MaximumHealth) || bootstrap.Runtime.DestroyedUnits > 0, Is.True);
        }

        [UnityTest]
        public IEnumerator SceneRestartRecreatesPresentationAndSelectionTargets()
        {
            yield return LoadPlaytestScene();
            UnitySyntheticSkirmishBootstrap bootstrap = FindBootstrap();
            Assert.That(bootstrap.SelectSingle(bootstrap.Runtime.HumanUnits[0]), Is.True);
            bootstrap.StepSimulation(3);
            bootstrap.RestartMatch();

            Assert.That(bootstrap.Runtime.Tick, Is.EqualTo(0));
            Assert.That(bootstrap.Client.Selection.Entities.Count, Is.EqualTo(0));
            Assert.That(bootstrap.LastPresentation, Is.Not.Null);
            Assert.That(bootstrap.LastPresentation.Entities.Count, Is.GreaterThan(0));
        }
    }
}
