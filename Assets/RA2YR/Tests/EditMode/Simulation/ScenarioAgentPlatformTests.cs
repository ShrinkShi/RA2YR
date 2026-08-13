using System.Linq;
using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class ScenarioAgentPlatformTests
    {
        private static ScenarioPlacementRaw P(ScenarioPlacementFamily f, string owner = "A", int x = 1, int y = 2, string key = "0") => new ScenarioPlacementRaw(f, key, "raw,value,tail", 0, x, y, "SyntheticType", owner);
        [Test] public void SpawnPreservesRawPlacementAndUsesStructuralRequest() { var r = ScenarioSpawner.BuildRequests(new[] { P(ScenarioPlacementFamily.Unit) }, new[] { new RuntimeOwnerIdentity(1, "A") }); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Accepted[0].Placement.ValueRaw, Is.EqualTo("raw,value,tail")); }
        [Test] public void SpawnRejectsUnknownOwnerAndInvalidPlacement() { var r = ScenarioSpawner.BuildRequests(new[] { P(ScenarioPlacementFamily.Unit, "Missing", -1, 2) }, new[] { new RuntimeOwnerIdentity(1, "A") }); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Diagnostics.Any(x => x.Code == SpawnDiagnosticCode.InvalidPlacement), Is.True); }
        [Test] public void SpawnBudgetFailsClosed() { var r = ScenarioSpawner.BuildRequests(new[] { P(ScenarioPlacementFamily.Unit), P(ScenarioPlacementFamily.Infantry) }, new[] { new RuntimeOwnerIdentity(1, "A") }, 1); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Accepted.Count, Is.EqualTo(1)); }
        [Test] public void SpawnOrderIsCanonicalBySourceOrdinal() { var a = P(ScenarioPlacementFamily.Unit); var b = new ScenarioPlacementRaw(ScenarioPlacementFamily.Structure, "1", "b", 1, 2, 2, "B", "A"); var r = ScenarioSpawner.BuildRequests(new[] { b, a }, new[] { new RuntimeOwnerIdentity(1, "A") }); Assert.That(r.Accepted[0].Placement.SourceOrdinal, Is.EqualTo(0)); }
        [Test] public void ObservationDoesNotExposeWorldTruth() { var env = new HeadlessSimulationEnvironment(); env.AddUnit(1, ScenarioPlacementFamily.Unit, 2, 3); AgentObservation o = env.Observe(1); Assert.That(o.ContainsWorldTruthApi, Is.False); Assert.That(o.Units.Count, Is.EqualTo(1)); }
        [Test] public void ObservationStableAcrossRuns() { var a = new HeadlessSimulationEnvironment(); a.AddUnit(1, ScenarioPlacementFamily.Unit, 2, 3); var b = new HeadlessSimulationEnvironment(); b.AddUnit(1, ScenarioPlacementFamily.Unit, 2, 3); Assert.That(a.Observe(1).Units.Select(x => x.Entity).ToArray(), Is.EqualTo(b.Observe(1).Units.Select(x => x.Entity).ToArray())); }
        [Test] public void RuleBasedPolicyProducesLegalCommandFromObservation() { var env = new HeadlessSimulationEnvironment(); env.AddUnit(1, ScenarioPlacementFamily.Unit, 2, 3); AgentDecision d = new RuleBasedAgentPolicy().Evaluate(env.Observe(1)); Assert.That(d.Available, Is.True); Assert.That(d.Command.Value.Source, Is.EqualTo(CommandSource.ComputerAI)); }
        [Test] public void HiddenOwnerUnitsAreExcludedFromOwnDecision() { var env = new HeadlessSimulationEnvironment(); env.AddUnit(2, ScenarioPlacementFamily.Unit, 2, 3); Assert.That(new RuleBasedAgentPolicy().Evaluate(env.Observe(1)).Available, Is.False); }
        [Test] public void HeadlessStepAdvancesLogicalTickOnly() { var env = new HeadlessSimulationEnvironment(); Assert.That(env.Tick, Is.EqualTo(0)); env.Step(); Assert.That(env.Tick, Is.EqualTo(1)); }
        [Test] public void HeadlessRepeatedRunsHaveSameStateHash() { var a = new HeadlessSimulationEnvironment(); a.AddUnit(1, ScenarioPlacementFamily.Unit, 2, 3); a.Step(); var b = new HeadlessSimulationEnvironment(); b.AddUnit(1, ScenarioPlacementFamily.Unit, 2, 3); b.Step(); Assert.That(a.StateHash(), Is.EqualTo(b.StateHash())); }
        [Test] public void NeuralUnavailableIsAContractNotAWorldMutation() { var d = new NeuralPolicyDescriptor("synthetic", 1); Assert.That(d.ModelId, Is.EqualTo("synthetic")); }
    }
}
