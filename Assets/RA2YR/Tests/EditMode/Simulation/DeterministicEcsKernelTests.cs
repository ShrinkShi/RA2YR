using System;
using System.Linq;
using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class DeterministicEcsKernelTests
    {
        [Test]
        public void EntityAllocationIsDeterministicAndReuseAdvancesGeneration()
        {
            EntityRegistry registry = new EntityRegistry(3);
            EntityId first = registry.Create();
            EntityId second = registry.Create();
            Assert.That(first.Index, Is.EqualTo(0));
            Assert.That(second.Index, Is.EqualTo(1));
            Assert.That(registry.Destroy(first), Is.True);
            EntityId reused = registry.Create();
            Assert.That(reused.Index, Is.EqualTo(first.Index));
            Assert.That(reused.Generation, Is.EqualTo(first.Generation + 1));
            Assert.That(registry.IsAlive(first), Is.False);
            Assert.That(registry.IsAlive(reused), Is.True);
        }

        [Test]
        public void EntityCapacityAndInvalidHandlesFailClosed()
        {
            EntityRegistry registry = new EntityRegistry(1);
            EntityId entity = registry.Create();
            Assert.Throws<InvalidOperationException>(() => registry.Create());
            Assert.That(registry.IsAlive(new EntityId(-1, 1)), Is.False);
            Assert.That(registry.Destroy(new EntityId(entity.Index, entity.Generation + 1)), Is.False);
        }

        [Test]
        public void ComponentStoreRejectsStaleEntityAndSupportsRemoval()
        {
            SimulationWorld world = new SimulationWorld(2);
            EntityId entity = world.CreateEntity();
            world.Positions.Set(entity, new PositionComponent(4, 5, 1));
            PositionComponent position;
            Assert.That(world.Positions.TryGet(entity, out position), Is.True);
            Assert.That(position.X, Is.EqualTo(4));
            Assert.That(world.DestroyEntity(entity), Is.True);
            Assert.Throws<ArgumentException>(() => world.Positions.Set(entity, new PositionComponent(1, 1)));
            Assert.That(world.Positions.TryGet(entity, out position), Is.False);
        }

        [Test]
        public void StructuralCommandsCommitInSequenceOrder()
        {
            SimulationWorld world = new SimulationWorld(4);
            EntityId existing = world.CreateEntity();
            StructuralCommandBuffer buffer = new StructuralCommandBuffer();
            buffer.EnqueueDestroy(existing);
            buffer.EnqueueCreate();
            buffer.EnqueueCreate();
            var created = buffer.Commit(world);
            Assert.That(created.Count, Is.EqualTo(2));
            Assert.That(created[0].Index, Is.EqualTo(0));
            Assert.That(created[1].Index, Is.EqualTo(1));
            Assert.That(world.Registry.IsAlive(existing), Is.False);
        }

        [Test]
        public void FixedClockDoesNotUseFrameDelta()
        {
            SimulationClock clock = new SimulationClock(new SimulationTimeProfile(15));
            Assert.That(clock.Tick, Is.EqualTo(0));
            Assert.That(clock.AdvanceOneTick(), Is.EqualTo(1));
            Assert.That(clock.Profile.TicksPerSecond, Is.EqualTo(15));
        }

        [Test]
        public void SchedulerUsesPhaseOrderThenStableId()
        {
            DeterministicScheduler scheduler = new DeterministicScheduler();
            scheduler.Register(new SimulationSystemDescriptor(SimulationPhase.Decision, 0, "z"));
            scheduler.Register(new SimulationSystemDescriptor(SimulationPhase.Input, 5, "late"));
            scheduler.Register(new SimulationSystemDescriptor(SimulationPhase.Input, 1, "b"));
            scheduler.Register(new SimulationSystemDescriptor(SimulationPhase.Input, 1, "a"));
            var ordered = scheduler.OrderedSystems();
            Assert.That(ordered.Select(item => item.Id).ToArray(), Is.EqualTo(new[] { "a", "b", "late", "z" }));
        }

        [Test]
        public void RngIsExplicitAndRepeatablePerStream()
        {
            DeterministicRng left = new DeterministicRng(123u, "decision");
            DeterministicRng right = new DeterministicRng(123u, "decision");
            Assert.That(left.NextUInt(), Is.EqualTo(right.NextUInt()));
            Assert.That(left.NextUInt(), Is.EqualTo(right.NextUInt()));
            Assert.That(left.CallCount, Is.EqualTo(2));
            Assert.That(new DeterministicRng(123u, "movement").NextUInt(), Is.Not.EqualTo(new DeterministicRng(123u, "decision").NextUInt()));
        }

        [Test]
        public void StateHashIsCanonicalAndChangesWithState()
        {
            SimulationWorld left = new SimulationWorld(2);
            SimulationWorld right = new SimulationWorld(2);
            EntityId leftEntity = left.CreateEntity();
            EntityId rightEntity = right.CreateEntity();
            left.Positions.Set(leftEntity, new PositionComponent(3, 4));
            right.Positions.Set(rightEntity, new PositionComponent(3, 4));
            Assert.That(left.ComputeStateHash(), Is.EqualTo(right.ComputeStateHash()));
            left.AdvanceTick();
            Assert.That(left.ComputeStateHash(), Is.Not.EqualTo(right.ComputeStateHash()));
        }

        [Test]
        public void SnapshotIsStableAfterWorldMutation()
        {
            SimulationWorld world = new SimulationWorld(2);
            EntityId entity = world.CreateEntity();
            world.Positions.Set(entity, new PositionComponent(1, 2));
            SimulationReadSnapshot snapshot = world.CaptureSnapshot();
            world.Positions.Set(entity, new PositionComponent(9, 9));
            Assert.That(snapshot.Entities[0].Position.Value.X, Is.EqualTo(1));
            Assert.That(snapshot.Entities[0].Position.Value.Y, Is.EqualTo(2));
        }

        [Test]
        public void ProposalsSortByPriorityThenEntityThenSequence()
        {
            EntityId first = new EntityId(0, 1);
            EntityId second = new EntityId(1, 1);
            ActionProposalBuffer buffer = new ActionProposalBuffer();
            buffer.Add(new ActionProposal(second, ActionProposalKind.Move, 10, 0));
            buffer.Add(new ActionProposal(first, ActionProposalKind.Attack, 10, 1));
            buffer.Add(new ActionProposal(first, ActionProposalKind.Move, 20, 2));
            Assert.That(buffer.Ordered().Select(item => item.Kind).ToArray(), Is.EqualTo(new[] { ActionProposalKind.Move, ActionProposalKind.Attack, ActionProposalKind.Move }));
        }

        [Test]
        public void AutonomyResolverHonorsUnitGroupPlayerGlobalPrecedence()
        {
            ResolvedAutonomyProfile profile = AutonomyResolver.Resolve(
                AutonomyCapabilities.AutoAcquire | AutonomyCapabilities.AutoCast,
                AutonomyOverride.Automatic,
                AutonomyOverride.Assisted,
                AutonomyOverride.Unspecified,
                AutonomyOverride.Manual);
            Assert.That(profile.Mode, Is.EqualTo(AutonomyMode.Manual));
            Assert.That(profile.Capabilities, Is.EqualTo(AutonomyCapabilities.None));
            Assert.That(profile.Envelope.MayMove, Is.True);
            Assert.That(profile.Envelope.MayCast, Is.False);
        }

        [Test]
        public void AssistedAutonomyDisablesFullyAutomaticActions()
        {
            ResolvedAutonomyProfile profile = AutonomyResolver.Resolve(
                AutonomyCapabilities.AutoAcquire | AutonomyCapabilities.AutoCast | AutonomyCapabilities.AutoKite | AutonomyCapabilities.AutoRetreat,
                AutonomyOverride.Assisted,
                AutonomyOverride.Unspecified,
                AutonomyOverride.Unspecified,
                AutonomyOverride.Unspecified);
            Assert.That(profile.Mode, Is.EqualTo(AutonomyMode.Assisted));
            Assert.That(profile.Capabilities, Is.EqualTo(AutonomyCapabilities.AutoAcquire));
            Assert.That(profile.Envelope.MayMove, Is.True);
            Assert.That(profile.Envelope.MayChase, Is.False);
        }

        [Test]
        public void DecisionScheduleIsDeterministic()
        {
            DecisionSchedule schedule = new DecisionSchedule(3, 1);
            EntityId entity = new EntityId(2, 1);
            Assert.That(schedule.ShouldEvaluate(entity, 0), Is.True);
            Assert.That(schedule.ShouldEvaluate(entity, 1), Is.False);
            Assert.That(schedule.ShouldEvaluate(entity, 3), Is.True);
        }

        [Test]
        public void UnknownEnumsAreRejectedAtConstruction()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationSystemDescriptor((SimulationPhase)999, 0, "bad"));
        }

        [Test]
        public void ManagedReferenceBackendReturnsStableProposalOrder()
        {
            SimulationWorld world = new SimulationWorld(1);
            world.CreateEntity();
            SimulationReadSnapshot snapshot = world.CaptureSnapshot();
            var input = new[] { new ActionProposal(new EntityId(0, 1), ActionProposalKind.Move, 1, 2), new ActionProposal(new EntityId(0, 1), ActionProposalKind.Attack, 2, 1) };
            var output = new ManagedSequentialReferenceBackend().Evaluate(snapshot, input);
            Assert.That(output[0].Kind, Is.EqualTo(ActionProposalKind.Attack));
        }
    }
}
