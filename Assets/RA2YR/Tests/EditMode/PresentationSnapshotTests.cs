using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RA2YR.Presentation;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode
{
    public sealed class PresentationSnapshotTests
    {
        [Test]
        public void VisualAssetIdIsStableLogicalIdentity()
        {
            var left = new VisualAssetId("units/tank");
            var right = new VisualAssetId("units/tank");
            Assert.That(left, Is.EqualTo(right));
            Assert.That(left.ToString(), Is.EqualTo("units/tank"));
        }

        [Test]
        public void VisualAssetIdRejectsEmptyIdentity()
        {
            Assert.That(() => new VisualAssetId(string.Empty), Throws.ArgumentException);
            VisualAssetId id;
            Assert.That(VisualAssetId.TryCreate(string.Empty, out id), Is.False);
        }

        [Test]
        public void DescriptorRetainsSemanticRenderPassAndRawPosition()
        {
            PresentationEntityDescriptor descriptor = Descriptor(2, "units/tank", PresentationRenderPass.Vehicle, 4, 5);
            Assert.That(descriptor.RenderPass, Is.EqualTo(PresentationRenderPass.Vehicle));
            Assert.That(descriptor.Position, Is.EqualTo(new PresentationPosition(4, 5, 0)));
            Assert.That(descriptor.VisualAssetId.Value, Is.EqualTo("units/tank"));
        }

        [Test]
        public void SnapshotSortIsDeterministicAcrossInputOrder()
        {
            SimulationReadSnapshot simulation = Simulation(1, 2);
            PresentationEntityDescriptor a = Descriptor(1, "a", PresentationRenderPass.Vehicle, 5, 1);
            PresentationEntityDescriptor b = Descriptor(2, "b", PresentationRenderPass.Terrain, 1, 1);
            PresentationSnapshot first = Assemble(simulation, new[] { a, b });
            PresentationSnapshot second = Assemble(simulation, new[] { b, a });
            Assert.That(first.IsSuccess, Is.True);
            Assert.That(first.CanonicalHash, Is.EqualTo(second.CanonicalHash));
            Assert.That(first.Entities.Select(item => item.Entity).ToArray(), Is.EqualTo(second.Entities.Select(item => item.Entity).ToArray()));
        }

        [Test]
        public void SnapshotIsImmutableAfterCallerListMutation()
        {
            SimulationReadSnapshot simulation = Simulation(1);
            var descriptors = new List<PresentationEntityDescriptor> { Descriptor(1, "a", PresentationRenderPass.Vehicle, 0, 0) };
            PresentationSnapshot snapshot = Assemble(simulation, descriptors);
            descriptors.Clear();
            Assert.That(snapshot.Entities, Has.Count.EqualTo(1));
            Assert.That(snapshot.CanonicalHash, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void SnapshotReportsCreatedEntity()
        {
            PresentationSnapshot snapshot = Assemble(Simulation(1), new[] { Descriptor(1, "a", PresentationRenderPass.Vehicle, 0, 0) });
            Assert.That(snapshot.Changes.Single().Kind, Is.EqualTo(PresentationEntityChangeKind.Created));
        }

        [Test]
        public void SnapshotReportsPersistedAndDespawnedEntities()
        {
            SimulationReadSnapshot firstSimulation = Simulation(1, 2);
            PresentationSnapshot first = Assemble(firstSimulation, new[] { Descriptor(1, "a", PresentationRenderPass.Vehicle, 0, 0), Descriptor(2, "b", PresentationRenderPass.Infantry, 1, 0) });
            PresentationSnapshot second = PresentationSnapshotAssembler.Assemble(
                Simulation(1),
                new[] { Descriptor(1, "a", PresentationRenderPass.Vehicle, 0, 0) },
                first,
                new[] { new FakeProvider("synthetic", true) });
            Assert.That(second.Changes.Any(item => item.Kind == PresentationEntityChangeKind.Persisted && item.Entity.Index == 0), Is.True);
            Assert.That(second.Changes.Any(item => item.Kind == PresentationEntityChangeKind.Despawned && item.Entity.Index == 1), Is.True);
        }

        [Test]
        public void MissingVisualAssetFailsClosedWithoutFallback()
        {
            PresentationSnapshot snapshot = PresentationSnapshotAssembler.Assemble(Simulation(1), new[] { Descriptor(1, "missing", PresentationRenderPass.Vehicle, 0, 0) });
            Assert.That(snapshot.IsSuccess, Is.False);
            Assert.That(snapshot.Diagnostics.Any(item => item.Code == PresentationDiagnosticCode.MissingVisualAsset), Is.True);
        }

        [Test]
        public void MissingVisualAssetCanBePreservedAsExplicitUnresolved()
        {
            var policy = new PresentationAssemblyPolicy(missingVisualAssetBehavior: MissingVisualAssetBehavior.PreserveUnresolved);
            PresentationSnapshot snapshot = PresentationSnapshotAssembler.Assemble(Simulation(1), new[] { Descriptor(1, "missing", PresentationRenderPass.Vehicle, 0, 0) }, policy: policy);
            Assert.That(snapshot.IsSuccess, Is.True);
            Assert.That(snapshot.Entities, Has.Count.EqualTo(1));
        }

        [Test]
        public void AmbiguousProvidersFailClosed()
        {
            var providers = new IVisualAssetProvider[] { new FakeProvider("a", true), new FakeProvider("b", true) };
            PresentationSnapshot snapshot = PresentationSnapshotAssembler.Assemble(Simulation(1), new[] { Descriptor(1, "x", PresentationRenderPass.Vehicle, 0, 0) }, providers: providers);
            Assert.That(snapshot.IsSuccess, Is.False);
            Assert.That(snapshot.Diagnostics.Any(item => item.Code == PresentationDiagnosticCode.AmbiguousVisualAssetProvider), Is.True);
        }

        [Test]
        public void OneProviderResolutionIsExplicitAndDeterministic()
        {
            var providers = new IVisualAssetProvider[] { new FakeProvider("provider", true) };
            PresentationSnapshot snapshot = PresentationSnapshotAssembler.Assemble(Simulation(1), new[] { Descriptor(1, "x", PresentationRenderPass.Vehicle, 0, 0) }, providers: providers);
            Assert.That(snapshot.IsSuccess, Is.True);
        }

        [Test]
        public void DescriptorOutsideSimulationSnapshotFails()
        {
            PresentationSnapshot snapshot = Assemble(Simulation(1), new[] { Descriptor(2, "x", PresentationRenderPass.Vehicle, 0, 0) });
            Assert.That(snapshot.IsSuccess, Is.False);
            Assert.That(snapshot.Diagnostics.Any(item => item.Code == PresentationDiagnosticCode.EntityNotInSimulationSnapshot), Is.True);
        }

        [Test]
        public void DuplicateDescriptorFailsWithoutWinnerSelection()
        {
            PresentationSnapshot snapshot = Assemble(Simulation(1), new[] { Descriptor(1, "a", PresentationRenderPass.Vehicle, 0, 0), Descriptor(1, "b", PresentationRenderPass.Vehicle, 0, 0) });
            Assert.That(snapshot.IsSuccess, Is.False);
            Assert.That(snapshot.Diagnostics.Any(item => item.Code == PresentationDiagnosticCode.DuplicateEntity), Is.True);
            Assert.That(snapshot.Entities, Has.Count.EqualTo(1));
        }

        [Test]
        public void DiagnosticBudgetCannotFailOpen()
        {
            var policy = new PresentationAssemblyPolicy(maxDiagnostics: 0);
            PresentationSnapshot snapshot = PresentationSnapshotAssembler.Assemble(Simulation(1), new[] { Descriptor(2, "x", PresentationRenderPass.Vehicle, 0, 0) }, policy: policy);
            Assert.That(snapshot.IsSuccess, Is.False);
            Assert.That(snapshot.Diagnostics, Is.Empty);
            Assert.That(snapshot.SuppressedDiagnosticCount, Is.GreaterThan(0));
        }

        [Test]
        public void EntityBudgetStopsEnumerationAtBound()
        {
            int consumed = 0;
            IEnumerable<PresentationEntityDescriptor> Lazy()
            {
                consumed++;
                yield return Descriptor(1, "a", PresentationRenderPass.Vehicle, 0, 0);
                consumed++;
                yield return Descriptor(2, "b", PresentationRenderPass.Vehicle, 1, 0);
                consumed++;
                yield return Descriptor(3, "c", PresentationRenderPass.Vehicle, 2, 0);
            }
            PresentationSnapshot snapshot = PresentationSnapshotAssembler.Assemble(Simulation(1, 2, 3), Lazy(), policy: new PresentationAssemblyPolicy(maxEntities: 2));
            Assert.That(snapshot.IsSuccess, Is.False);
            Assert.That(consumed, Is.EqualTo(3));
            Assert.That(snapshot.Diagnostics.Any(item => item.Code == PresentationDiagnosticCode.EntityBudgetExceeded), Is.True);
        }

        [Test]
        public void NullSimulationSnapshotFailsClosed()
        {
            PresentationSnapshot snapshot = PresentationSnapshotAssembler.Assemble(null, Array.Empty<PresentationEntityDescriptor>());
            Assert.That(snapshot.IsSuccess, Is.False);
            Assert.That(snapshot.Diagnostics.Any(item => item.Code == PresentationDiagnosticCode.SnapshotSourceMissing), Is.True);
        }

        [Test]
        public void SnapshotAssemblyDoesNotMutateSimulationSnapshot()
        {
            SimulationReadSnapshot simulation = Simulation(1);
            long tick = simulation.Tick;
            PresentationSnapshot snapshot = Assemble(simulation, new[] { Descriptor(1, "a", PresentationRenderPass.Vehicle, 10, 11) });
            Assert.That(snapshot.SimulationTick, Is.EqualTo(tick));
            Assert.That(simulation.Entities, Has.Count.EqualTo(1));
            Assert.That(simulation.Entities[0].Id.Index, Is.EqualTo(0));
        }

        [Test]
        public void InterpolationIsRepeatableWithIntegerFraction()
        {
            PresentationInterpolationResult first = PresentationInterpolator.Interpolate(new PresentationPosition(0, 10, 1), new PresentationPosition(10, 30, 3), 1, 2);
            PresentationInterpolationResult second = PresentationInterpolator.Interpolate(new PresentationPosition(0, 10, 1), new PresentationPosition(10, 30, 3), 1, 2);
            Assert.That(first.IsSuccess, Is.True);
            Assert.That(first.Position.Value.XScaled, Is.EqualTo(5000));
            Assert.That(first.Position.Value.YScaled, Is.EqualTo(20000));
            Assert.That(first.Position.Value.LayerScaled, Is.EqualTo(second.Position.Value.LayerScaled));
        }

        [Test]
        public void InterpolationRejectsInvalidFraction()
        {
            PresentationInterpolationResult result = PresentationInterpolator.Interpolate(new PresentationPosition(), new PresentationPosition(1, 1), 2, 1);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PresentationDiagnosticCode.InvalidInterpolationFraction), Is.True);
        }

        [Test]
        public void InterpolationReportsCheckedArithmeticFailure()
        {
            PresentationInterpolationResult result = PresentationInterpolator.Interpolate(new PresentationPosition(int.MinValue, 0), new PresentationPosition(int.MaxValue, 0), 2, 3, new PresentationInterpolationProfile(int.MaxValue));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics.Any(item => item.Code == PresentationDiagnosticCode.InterpolationArithmeticOverflow), Is.True);
        }

        [Test]
        public void AllSemanticRenderPassesAreRepresentable()
        {
            Assert.That(Enum.GetValues(typeof(PresentationRenderPass)).Length, Is.EqualTo(12));
            foreach (PresentationRenderPass pass in Enum.GetValues(typeof(PresentationRenderPass)))
                Assert.That(Enum.IsDefined(typeof(PresentationRenderPass), pass), Is.True);
        }

        [Test]
        public void PresentationAssemblyHasNoUnityReferences()
        {
            string[] names = typeof(PresentationSnapshot).Assembly.GetReferencedAssemblies().Select(item => item.Name).ToArray();
            Assert.That(names.Any(item => item != null && (item.StartsWith("UnityEngine", StringComparison.Ordinal) || item.StartsWith("UnityEditor", StringComparison.Ordinal))), Is.False);
        }

        private static PresentationSnapshot Assemble(SimulationReadSnapshot simulation, IEnumerable<PresentationEntityDescriptor> descriptors)
        {
            return PresentationSnapshotAssembler.Assemble(simulation, descriptors, providers: new[] { new FakeProvider("synthetic", true) });
        }

        private static PresentationEntityDescriptor Descriptor(int index, string asset, PresentationRenderPass pass, int x, int y)
        {
            return new PresentationEntityDescriptor(new EntityId(index - 1, 1), new VisualAssetId(asset), pass, new PresentationPosition(x, y));
        }

        private static SimulationReadSnapshot Simulation(params int[] indexes)
        {
            var world = new SimulationWorld(Math.Max(1, indexes.Length + 1));
            foreach (int ignored in indexes) world.CreateEntity();
            return world.CaptureSnapshot();
        }

        private sealed class FakeProvider : IVisualAssetProvider
        {
            private readonly bool resolves;
            public FakeProvider(string id, bool resolves) { ProviderId = id; this.resolves = resolves; }
            public string ProviderId { get; }
            public VisualAssetProviderResult Resolve(VisualAssetId assetId)
            {
                return new VisualAssetProviderResult(resolves ? VisualAssetProviderResolutionStatus.Resolved : VisualAssetProviderResolutionStatus.Missing, ProviderId, assetId);
            }
        }
    }
}
