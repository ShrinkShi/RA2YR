using System.Collections.Generic;
using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class EconomicAgentTests
    {
        private static ResourceEconomyReadLimits ResourceLimits(int diagnostics = 32) => new ResourceEconomyReadLimits(8, 8, 4, 4, 1000, diagnostics);
        private static ProductionReadLimits ProductionLimits(int diagnostics = 32) => new ProductionReadLimits(8, 8, 2, 4, diagnostics);
        private static EconomicAgentReadLimits AgentLimits(int actions = 8, int diagnostics = 32) => new EconomicAgentReadLimits(actions, diagnostics);

        private static ResourceEconomyConsistencyAnalysis Resources(bool valid = true, int diagnostics = 32)
        {
            var cells = new[] { new ResourceCellRaw(0, 1, 2, ResourceFamily.Tiberium, ResourceVisualStage.Mature) };
            return ResourceEconomyConsistencyAnalysis.Analyze(cells, new[] { new ResourceTypeRaw(0, "Tiberium", "Tiberium", ResourceFamily.Tiberium, 25) }, valid ? ResourceQuantityProfile.OverlayDataPlusOne : (ResourceQuantityProfile)99, ResourceValueProfile.RulesResourceValue, ResourceLimits(diagnostics));
        }

        private static ProductionDefinitionDescriptor Definition(long cost = 10, long ordinal = 0) => new ProductionDefinitionDescriptor(new ProductionDefinitionRaw(ordinal, "Tank", "Vehicle", 1, cost, 10, 4, new[] { "WarFactory" }), "Synthetic");

        private static ProductionAvailabilityResult Production(bool requestable = true, int diagnostics = 32)
        {
            return ProductionAvailabilityResult.Evaluate(new ProductionAvailabilityQuery(Definition(), 1, requestable ? new[] { "WarFactory" } : new string[0], 0, ProductionAvailabilityProfile.ExplicitCapabilitiesAndLimits), ProductionLimits(diagnostics));
        }

        private static EconomicAgentObservation Observation(long credits = 100, ResourceEconomyConsistencyAnalysis resources = null, ProductionAvailabilityResult production = null, int cargo = 0)
        {
            HarvesterCargoSnapshot snapshot;
            IReadOnlyList<ResourceEconomyDiagnostic> cargoDiagnostics;
            Assert.That(HarvesterCargoSnapshot.TryCreate(100, new[] { new HarvesterCargoEntry(ResourceFamily.Tiberium, cargo, 0) }, ResourceLimits(), out snapshot, out cargoDiagnostics), Is.True);
            return new EconomicAgentObservation(7, new CreditAccount(new PlayerId(0), credits), resources ?? Resources(), snapshot, Definition(), production ?? Production(), new StructurePowerProjection(new[] { new StructureDefinitionRaw(0, "Plant", 1, 1, 100, 10, 0, "Player0") }), new StructureInteractionCandidate(StructureInteractionAction.RepairCandidate, new PlayerId(0), new PlayerId(0), 50, 100, true, "Synthetic"));
        }

        [Test] public void ValidObservationProducesDeterministicCandidatesWithoutMutation() { var r = EconomicAgentEvaluator.Evaluate(Observation(), EconomicAgentPolicy.ConservativeDeterministic, AgentLimits()); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Proposals, Has.Count.GreaterThanOrEqualTo(2)); Assert.That(r.Proposals[0].Kind, Is.EqualTo(EconomicAgentActionKind.RepairCandidate)); }
        [Test] public void PolicyIsExplicitAndInvalidPolicyFailsClosed() { var r = EconomicAgentEvaluator.Evaluate(Observation(), (EconomicAgentPolicy)99, AgentLimits()); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Diagnostics[0].Code, Is.EqualTo(EconomicAgentDiagnosticCode.InvalidPolicy)); }
        [Test] public void NullObservationFailsEvenWhenDiagnosticsSuppressed() { var r = EconomicAgentEvaluator.Evaluate(null, EconomicAgentPolicy.ConservativeDeterministic, AgentLimits(diagnostics: 0)); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Execution.HasFatalError, Is.True); Assert.That(r.Execution.SuppressedDiagnosticCount, Is.EqualTo(1)); }
        [Test] public void ResourceChildFailurePropagatesAndStopsActions() { var r = EconomicAgentEvaluator.Evaluate(Observation(resources: Resources(false)), EconomicAgentPolicy.ConservativeDeterministic, AgentLimits(diagnostics: 0)); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Proposals, Is.Empty); Assert.That(r.Execution.SuppressedDiagnosticCount, Is.GreaterThan(0)); }
        [Test] public void ProductionUnavailableRemainsWarningAndDoesNotPretendRequestable() { var r = EconomicAgentEvaluator.Evaluate(Observation(production: Production(false)), EconomicAgentPolicy.ConservativeDeterministic, AgentLimits()); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Proposals, Has.None.Matches<EconomicAgentActionProposal>(x => x.Kind == EconomicAgentActionKind.QueueProductionCandidate)); }
        [Test] public void InsufficientCreditsDoesNotMutateAuthorityOrProposePayment() { var r = EconomicAgentEvaluator.Evaluate(Observation(credits: 1), EconomicAgentPolicy.ConservativeDeterministic, AgentLimits()); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Proposals, Has.None.Matches<EconomicAgentActionProposal>(x => x.Kind == EconomicAgentActionKind.QueueProductionCandidate)); }
        [Test] public void PowerDeficitIsWarningOnlyAndExplicit() { var o = Observation(); var r = EconomicAgentEvaluator.Evaluate(new EconomicAgentObservation(o.Tick, o.Credits, o.Resources, o.Cargo, o.ProductionDefinition, o.Production, new StructurePowerProjection(new[] { new StructureDefinitionRaw(0, "Plant", 1, 1, 100, 0, 10, "Player0") }), o.Interaction), EconomicAgentPolicy.ConservativeDeterministic, AgentLimits()); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Diagnostics, Has.Some.Matches<EconomicAgentDiagnostic>(x => x.Code == EconomicAgentDiagnosticCode.PowerDeficit)); }
        [Test] public void ActionBudgetFailsClosedWithoutPartialSuccessAmbiguity() { var r = EconomicAgentEvaluator.Evaluate(Observation(), EconomicAgentPolicy.ConservativeDeterministic, AgentLimits(actions: 1)); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Execution.HasFatalError, Is.True); Assert.That(r.Diagnostics, Has.Some.Matches<EconomicAgentDiagnostic>(x => x.Code == EconomicAgentDiagnosticCode.ActionBudgetExceeded)); }
        [Test] public void SuppressedWarningsRemainObservableInExecution() { var r = EconomicAgentEvaluator.Evaluate(Observation(credits: 1, production: Production(false), cargo: 100), EconomicAgentPolicy.ConservativeDeterministic, AgentLimits(diagnostics: 1)); Assert.That(r.Execution.HighestSeverity, Is.EqualTo(EconomicAgentSeverity.Warning)); Assert.That(r.Execution.SuppressedDiagnosticCount, Is.GreaterThan(0)); }
        [Test] public void ProposalOrderingAndHashAreStableAcrossEquivalentInputs() { var a = EconomicAgentEvaluator.Evaluate(Observation(), EconomicAgentPolicy.ConservativeDeterministic, AgentLimits()); var b = EconomicAgentEvaluator.Evaluate(Observation(), EconomicAgentPolicy.ConservativeDeterministic, AgentLimits()); Assert.That(a.CanonicalHash, Is.EqualTo(b.CanonicalHash)); Assert.That(a.Proposals[0].Kind, Is.EqualTo(b.Proposals[0].Kind)); }
        [Test] public void ProposalStringsAreImmutableSnapshots() { var r = EconomicAgentEvaluator.Evaluate(Observation(), EconomicAgentPolicy.ConservativeDeterministic, AgentLimits()); Assert.That(r.Proposals[0].TargetRaw, Is.EqualTo("structure")); }
        [Test] public void CoreAgentAssemblyHasNoUnityReferences() { Assert.That(typeof(EconomicAgentEvaluator).Assembly.GetReferencedAssemblies(), Has.None.Matches<System.Reflection.AssemblyName>(x => x.Name == "UnityEngine" || x.Name == "UnityEditor")); }
    }
}
