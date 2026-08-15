using System.Linq;
using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode
{
    public sealed class M6HumanPlaytestRuntimeTests
    {
        [Test]
        public void RuntimeStartsWithHumanAndOpponentForces()
        {
            var runtime = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            HumanPlaytestSnapshot snapshot = runtime.CaptureSnapshot();

            Assert.That(snapshot.Status, Is.EqualTo(HumanPlaytestMatchStatus.Running));
            Assert.That(snapshot.Entities.Count(x => x.Owner.Value == runtime.HumanPlayer.Value), Is.GreaterThanOrEqualTo(6));
            Assert.That(snapshot.Entities.Count(x => x.Owner.Value == runtime.AiPlayer.Value), Is.GreaterThanOrEqualTo(3));
            Assert.That(snapshot.Entities.Any(x => x.Kind == HumanPlaytestEntityKind.Harvester), Is.True);
            Assert.That(snapshot.Entities.Any(x => x.Kind == HumanPlaytestEntityKind.Refinery), Is.True);
            Assert.That(snapshot.Entities.Any(x => x.Kind == HumanPlaytestEntityKind.Factory), Is.True);
            Assert.That(snapshot.Entities.Any(x => x.Kind == HumanPlaytestEntityKind.Power), Is.True);
        }

        [Test]
        public void HumanCommandQueuesAndMovesThroughSimulation()
        {
            var runtime = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            EntityId unit = runtime.HumanUnits[0];
            HumanPlaytestEntitySnapshot before = runtime.CaptureSnapshot().Entities.Single(x => x.Entity.Equals(unit));

            var accepted = runtime.EnqueueHumanCommands(new[] { unit }, CommandKind.Move, new CommandTarget(new CellCoordinate(10, 6), null));
            for (int i = 0; i < 8; i++) runtime.Step();
            HumanPlaytestEntitySnapshot after = runtime.CaptureSnapshot().Entities.Single(x => x.Entity.Equals(unit));

            Assert.That(accepted.Count, Is.EqualTo(1));
            Assert.That(accepted[0].IsAccepted, Is.True);
            Assert.That(after.X != before.X || after.Y != before.Y, Is.True);
            Assert.That(after.Mission, Is.EqualTo(MissionKind.Move));
        }

        [Test]
        public void ProductionSpendsCreditsAndSpawnsUnit()
        {
            var runtime = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            long beforeCredits = runtime.CaptureSnapshot().Credits;

            Assert.That(runtime.QueueProduction(), Is.True);
            for (int i = 0; i < 6; i++) runtime.Step();
            HumanPlaytestSnapshot snapshot = runtime.CaptureSnapshot();

            Assert.That(snapshot.ProductionEvents, Is.EqualTo(1));
            Assert.That(snapshot.SpawnedUnits, Is.EqualTo(1));
            Assert.That(snapshot.Credits, Is.EqualTo(beforeCredits - 50));
        }

        [Test]
        public void HarvesterSettlesSyntheticResourceAtRefinery()
        {
            var runtime = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            for (int i = 0; i < 24; i++) runtime.Step();

            HumanPlaytestSnapshot snapshot = runtime.CaptureSnapshot();
            Assert.That(snapshot.HarvestEvents, Is.GreaterThanOrEqualTo(1));
            Assert.That(snapshot.Credits, Is.GreaterThan(200));
        }

        [Test]
        public void ExplicitAutonomyChangesOnlyHumanUnits()
        {
            var runtime = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            Assert.That(runtime.SetAutonomy(runtime.HumanUnits, AutonomyMode.Assisted), Is.True);

            HumanPlaytestSnapshot snapshot = runtime.CaptureSnapshot();
            Assert.That(snapshot.Entities.Where(x => x.Owner.Value == runtime.HumanPlayer.Value && x.Kind == HumanPlaytestEntityKind.Unit).All(x => x.Autonomy == AutonomyMode.Assisted), Is.True);
            Assert.That(snapshot.Entities.Where(x => x.Owner.Value == runtime.AiPlayer.Value && x.Kind == HumanPlaytestEntityKind.Unit).All(x => x.Autonomy == AutonomyMode.Automatic), Is.True);
        }

        [Test]
        public void RuleBasedOpponentProducesCombatEvents()
        {
            var runtime = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            for (int i = 0; i < 80; i++) runtime.Step();

            Assert.That(runtime.CombatEvents, Is.GreaterThan(0));
            HumanPlaytestSnapshot snapshot = runtime.CaptureSnapshot();
            Assert.That(snapshot.Entities.Any(x => x.Owner.Value == runtime.HumanPlayer.Value && x.Health < x.MaximumHealth) || runtime.DestroyedUnits > 0, Is.True);
        }

        [Test]
        public void PresentationAndHeadlessRuntimeRemainDeterministicallyEquivalent()
        {
            HumanPlaytestEquivalenceResult result = HumanPlaytestRuntime.ProvePresentationEquivalence();

            Assert.That(result.IsEqual, Is.True);
            Assert.That(result.HeadlessHash, Is.EqualTo(result.PresentationHash));
            Assert.That(result.Ticks, Is.EqualTo(24));
        }

        [Test]
        public void ResetRestoresCanonicalStartingHash()
        {
            var runtime = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            string initial = runtime.ComputeStateHash();
            runtime.Step();
            runtime.Reset();

            Assert.That(runtime.ComputeStateHash(), Is.EqualTo(initial));
        }

        [Test]
        public void SnapshotStateHashChangesAfterAcceptedCommand()
        {
            var runtime = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            string before = runtime.ComputeStateHash();
            runtime.EnqueueHumanCommands(new[] { runtime.HumanUnits[0] }, CommandKind.Move, new CommandTarget(new CellCoordinate(12, 7), null));
            runtime.Step();

            Assert.That(runtime.ComputeStateHash(), Is.Not.EqualTo(before));
        }
    }
}
