using System;
using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class ResourceEconomyTests
    {
        private static ResourceEconomyReadLimits Limits(int diagnostics = 32) => new ResourceEconomyReadLimits(8, 8, 4, 2, 100, diagnostics);

        [Test] public void RawCellPreservesOverlayFieldsAndProvenance()
        {
            var raw = new ResourceCellRaw(3, 7, 4, ResourceFamily.Tiberium, ResourceVisualStage.Mature);
            var result = ResourceEconomyConsistencyAnalysis.Analyze(new[] { raw }, new ResourceTypeRaw[0], ResourceQuantityProfile.PreserveOnly, ResourceValueProfile.PreserveOnly, Limits());
            Assert.That(result.Cells[0].Raw.OverlayTypeRaw, Is.EqualTo(7));
            Assert.That(result.Cells[0].Raw.SourceOrdinal, Is.EqualTo(3));
        }

        [Test] public void ExplicitQuantityAndValueProfilesRemainDerived()
        {
            var result = ResourceEconomyConsistencyAnalysis.Analyze(
                new[] { new ResourceCellRaw(0, 2, 4, ResourceFamily.Ore, ResourceVisualStage.Mature) },
                new[] { new ResourceTypeRaw(0, "Ore", "25", ResourceFamily.Ore, 25) },
                ResourceQuantityProfile.OverlayDataPlusOne, ResourceValueProfile.RulesResourceValue, Limits());
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Cells[0].QuantityCandidate, Is.EqualTo(5));
            Assert.That(result.Cells[0].ValueCandidate, Is.EqualTo(125));
            Assert.That(result.Cells[0].QuantityPolicy, Is.EqualTo(nameof(ResourceQuantityProfile.OverlayDataPlusOne)));
        }

        [Test] public void PreserveOnlyDoesNotInventRuntimeQuantity()
        {
            var result = ResourceEconomyConsistencyAnalysis.Analyze(new[] { new ResourceCellRaw(0, 2, 4, ResourceFamily.Ore, ResourceVisualStage.Mature) }, null, ResourceQuantityProfile.PreserveOnly, ResourceValueProfile.PreserveOnly, Limits());
            Assert.That(result.Cells[0].QuantityCandidate.HasValue, Is.False);
            Assert.That(result.Cells[0].ValueCandidate.HasValue, Is.False);
        }

        [Test] public void InvalidPolicyFailsBeforeEnumeration()
        {
            var consumed = false;
            System.Collections.Generic.IEnumerable<ResourceCellRaw> Explosive()
            {
                consumed = true;
                throw new InvalidOperationException("must not enumerate");
                #pragma warning disable CS0162
                yield break;
                #pragma warning restore CS0162
            }
            var result = ResourceEconomyConsistencyAnalysis.Analyze(Explosive(), null, (ResourceQuantityProfile)99, ResourceValueProfile.PreserveOnly, Limits());
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(consumed, Is.False);
        }

        [Test] public void ResourceTypeAndCellBudgetsFailClosed()
        {
            var result = ResourceEconomyConsistencyAnalysis.Analyze(
                new[] { new ResourceCellRaw(0, 1, 1, ResourceFamily.Ore, ResourceVisualStage.Mature), new ResourceCellRaw(1, 1, 1, ResourceFamily.Ore, ResourceVisualStage.Mature) },
                new[] { new ResourceTypeRaw(0, "A", "", ResourceFamily.Ore, 1), new ResourceTypeRaw(1, "B", "", ResourceFamily.Gem, 1) },
                ResourceQuantityProfile.PreserveOnly, ResourceValueProfile.PreserveOnly, new ResourceEconomyReadLimits(1, 1, 4, 2, 100, 0));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Execution.SuppressedDiagnosticCount, Is.GreaterThan(0));
        }

        [Test] public void QuantityAndValueOverflowAreStructuredFailures()
        {
            var result = ResourceEconomyConsistencyAnalysis.Analyze(
                new[] { new ResourceCellRaw(0, 1, int.MaxValue, ResourceFamily.Ore, ResourceVisualStage.Mature) },
                new[] { new ResourceTypeRaw(0, "Ore", "", ResourceFamily.Ore, long.MaxValue) },
                ResourceQuantityProfile.OverlayDataPlusOne, ResourceValueProfile.RulesResourceValue, Limits());
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Count, Is.GreaterThan(0));
        }

        [Test] public void CargoSnapshotChecksCapacityAndKeepsStableOrder()
        {
            HarvesterCargoSnapshot snapshot;
            System.Collections.Generic.IReadOnlyList<ResourceEconomyDiagnostic> diagnostics;
            Assert.That(HarvesterCargoSnapshot.TryCreate(10, new[] { new HarvesterCargoEntry(ResourceFamily.Ore, 3, 1), new HarvesterCargoEntry(ResourceFamily.Tiberium, 2, 0) }, Limits(), out snapshot, out diagnostics), Is.True);
            Assert.That(snapshot.TotalQuantity, Is.EqualTo(5));
            Assert.That(snapshot.Entries[0].SourceOrdinal, Is.EqualTo(0));
        }

        [Test] public void CargoCapacityFailureDoesNotMutateResult()
        {
            HarvesterCargoSnapshot snapshot;
            System.Collections.Generic.IReadOnlyList<ResourceEconomyDiagnostic> diagnostics;
            Assert.That(HarvesterCargoSnapshot.TryCreate(3, new[] { new HarvesterCargoEntry(ResourceFamily.Ore, 4, 0) }, Limits(), out snapshot, out diagnostics), Is.False);
            Assert.That(snapshot, Is.Null);
            Assert.That(diagnostics[0].Code, Is.EqualTo(ResourceEconomyDiagnosticCode.CapacityExceeded));
        }

        [Test] public void ZeroDiagnosticBudgetStillFailsForNegativeCargo()
        {
            HarvesterCargoSnapshot snapshot;
            System.Collections.Generic.IReadOnlyList<ResourceEconomyDiagnostic> diagnostics;
            Assert.That(HarvesterCargoSnapshot.TryCreate(10, new[] { new HarvesterCargoEntry(ResourceFamily.Ore, -1, 0) }, Limits(0), out snapshot, out diagnostics), Is.False);
            Assert.That(diagnostics.Count, Is.EqualTo(0));
        }

        [Test] public void RefineryAcceptanceIsExplicit()
        {
            HarvesterCargoSnapshot cargo;
            System.Collections.Generic.IReadOnlyList<ResourceEconomyDiagnostic> cargoDiagnostics;
            HarvesterCargoSnapshot.TryCreate(10, new[] { new HarvesterCargoEntry(ResourceFamily.Gem, 1, 0) }, Limits(), out cargo, out cargoDiagnostics);
            var refinery = new RefineryCapabilityDescriptor("Proc", new[] { ResourceFamily.Ore }, new[] { new DockingSlotDescriptor(0, 0, 0, 1, 0, 2, 0, 1) });
            System.Collections.Generic.IReadOnlyList<ResourceEconomyDiagnostic> diagnostics;
            Assert.That(ResourceEconomyConsistencyAnalysis.ValidateRefinery(cargo, refinery, Limits(), out diagnostics), Is.False);
            Assert.That(diagnostics[0].Code, Is.EqualTo(ResourceEconomyDiagnosticCode.UnacceptedResource));
        }

        [Test] public void DockBudgetAndInvalidSlotAreStructured()
        {
            var refinery = new RefineryCapabilityDescriptor("Proc", new[] { ResourceFamily.Ore }, new[]
            {
                new DockingSlotDescriptor(0, 0, 0, 1, 0, 2, 0, -1),
                new DockingSlotDescriptor(1, 0, 1, 1, 1, 2, 1, 1),
                new DockingSlotDescriptor(2, 0, 2, 1, 2, 2, 2, 1)
            });
            System.Collections.Generic.IReadOnlyList<ResourceEconomyDiagnostic> diagnostics;
            Assert.That(ResourceEconomyConsistencyAnalysis.ValidateRefinery(null, refinery, Limits(), out diagnostics), Is.False);
            Assert.That(diagnostics.Count, Is.EqualTo(2));
        }

        [Test] public void CanonicalHashIsStableForInputOrder()
        {
            var a = new[] { new ResourceCellRaw(1, 2, 3, ResourceFamily.Ore, ResourceVisualStage.Mature), new ResourceCellRaw(0, 1, 2, ResourceFamily.Ore, ResourceVisualStage.Seed) };
            var b = new[] { a[1], a[0] };
            var one = ResourceEconomyConsistencyAnalysis.Analyze(a, null, ResourceQuantityProfile.PreserveOnly, ResourceValueProfile.PreserveOnly, Limits());
            var two = ResourceEconomyConsistencyAnalysis.Analyze(b, null, ResourceQuantityProfile.PreserveOnly, ResourceValueProfile.PreserveOnly, Limits());
            Assert.That(one.CanonicalHash, Is.EqualTo(two.CanonicalHash));
        }

        [Test] public void CoreResourceTypesHaveNoUnityDependency()
        {
            var assembly = typeof(ResourceCellRaw).Assembly;
            Assert.That(assembly.GetReferencedAssemblies(), Has.None.Matches<System.Reflection.AssemblyName>(x => x.Name == "UnityEngine" || x.Name == "UnityEditor"));
        }
    }
}
