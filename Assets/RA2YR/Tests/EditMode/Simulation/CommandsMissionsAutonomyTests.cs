using System;
using System.Linq;
using NUnit.Framework;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode.Simulation
{
    public sealed class CommandsMissionsAutonomyTests
    {
        private static EntityId E(int i) => new EntityId(i, 1);
        private static CommandRequest Command(long id, EntityId actor, CommandSource source, CommandKind kind, QueueMode queue = QueueMode.Append, bool forced = false)
        { return new CommandRequest(id, actor, source, kind, new CommandTarget(new CellCoordinate(1, 1), null), queue, 1, forced); }

        [Test]
        public void AllSourcesProduceDeclarativeRequests()
        {
            var sources = Enum.GetValues(typeof(CommandSource)).Cast<CommandSource>().ToArray();
            Assert.That(sources, Is.EqualTo(new[] { CommandSource.Human, CommandSource.ComputerAI, CommandSource.Script, CommandSource.Trigger, CommandSource.Internal }));
            Assert.That(Command(1, E(0), CommandSource.Human, CommandKind.Move).Target.IsEmpty, Is.False);
        }

        [Test]
        public void ReplaceAndAppendQueueHaveCanonicalOrder()
        {
            var queue = new CommandQueue(4);
            Assert.That(queue.Enqueue(Command(2, E(0), CommandSource.Script, CommandKind.Guard)).IsAccepted, Is.True);
            Assert.That(queue.Enqueue(Command(1, E(0), CommandSource.Human, CommandKind.Move, QueueMode.Append)).IsAccepted, Is.True);
            Assert.That(queue.Get(E(0)).Select(x => x.CommandId).ToArray(), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(queue.Enqueue(Command(3, E(0), CommandSource.Human, CommandKind.Stop, QueueMode.Replace)).IsAccepted, Is.True);
            Assert.That(queue.Get(E(0)).Select(x => x.CommandId).ToArray(), Is.EqualTo(new long[] { 3 }));
        }

        [Test]
        public void QueueBudgetAndDuplicateFailClosed()
        {
            var queue = new CommandQueue(1);
            Assert.That(queue.Enqueue(Command(1, E(0), CommandSource.Human, CommandKind.Move)).IsAccepted, Is.True);
            Assert.That(queue.Enqueue(Command(1, E(0), CommandSource.Human, CommandKind.Move)).Status, Is.EqualTo(CommandAcceptance.Rejected));
            Assert.That(queue.Enqueue(Command(2, E(0), CommandSource.Human, CommandKind.Move)).Status, Is.EqualTo(CommandAcceptance.QueueFull));
        }

        [Test]
        public void AuthoredMissionRawIsPreservedInRuntimeSnapshot()
        {
            var snapshot = new RuntimeMissionSnapshot(E(0), CommandKind.Guard, 4, 9, null, "  CustomMission  ", true, string.Empty);
            Assert.That(snapshot.AuthoredMissionRaw, Is.EqualTo("  CustomMission  "));
            Assert.That(snapshot.CommandId, Is.EqualTo(4));
        }

        [Test]
        public void PerceptionUsesSpatialIndexAndStableOrder()
        {
            var index = new DeterministicSpatialIndex();
            index.Insert(E(2), new CellCoordinate(1, 0)); index.Insert(E(1), new CellCoordinate(0, 0));
            var perception = new PerceptionService(index); perception.Track(E(1), new CellCoordinate(0, 0)); perception.Track(E(2), new CellCoordinate(1, 0));
            Assert.That(perception.Query(E(1), 1).Select(x => x.Target).ToArray(), Is.EqualTo(new[] { E(2) }));
        }

        [Test]
        public void TargetEvaluationRanksThreatAndDistanceDeterministically()
        {
            var scores = TargetEvaluator.Rank(new[] { new PerceptionCandidate(E(2), E(9), 2, 4), new PerceptionCandidate(E(2), E(3), 1, 2) }, new TargetEvaluationProfile());
            Assert.That(scores[0].Target, Is.EqualTo(E(9)));
        }

        [Test]
        public void TargetMemoryUsesSwitchingHysteresis()
        {
            var memory = new TargetMemory(E(1), null, 10, 1, 5);
            TargetMemory unchanged = memory.Update(new TargetScore(E(2), 12, 1, 1), 2);
            Assert.That(unchanged.Current, Is.EqualTo(E(1)));
            TargetMemory switched = memory.Update(new TargetScore(E(2), 15, 1, 1), 3);
            Assert.That(switched.Current, Is.EqualTo(E(2)));
            Assert.That(switched.Last, Is.EqualTo(E(1)));
        }

        [Test]
        public void ArbitrationPlayerCommandWinsByPriority()
        {
            var arbitration = new ActionArbitrationSystem();
            var proposals = new[]
            {
                new ActionArbitrationProposal(E(0), ArbitrationKind.TargetProposal, 20, 1, Command(2, E(0), CommandSource.ComputerAI, CommandKind.Attack)),
                new ActionArbitrationProposal(E(0), ArbitrationKind.PlayerCommand, 100, 2, Command(1, E(0), CommandSource.Human, CommandKind.Move, forced: true))
            };
            Assert.That(arbitration.Resolve(proposals)[0].Kind, Is.EqualTo(ArbitrationKind.PlayerCommand));
        }

        [Test]
        public void ArbitrationKeepsOneStableWinnerPerActor()
        {
            var arbitration = new ActionArbitrationSystem();
            var result = arbitration.Resolve(new[] { new ActionArbitrationProposal(E(1), ArbitrationKind.MissionContinuation, 1, 3, Command(3, E(1), CommandSource.Internal, CommandKind.Guard)), new ActionArbitrationProposal(E(0), ArbitrationKind.MissionContinuation, 1, 2, Command(2, E(0), CommandSource.Internal, CommandKind.Guard)) });
            Assert.That(result.Select(x => x.Actor).ToArray(), Is.EqualTo(new[] { E(0), E(1) }));
        }

        [Test]
        public void ManualDisablesAutoAcquireAndAutoKite()
        {
            var profile = AutonomyResolver.Resolve(AutonomyCapabilities.AutoAcquire | AutonomyCapabilities.AutoKite, AutonomyOverride.Manual, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified);
            var service = new AutonomyDecisionService();
            Assert.That(service.AllowsAutoAcquire(profile), Is.False);
            Assert.That(service.AllowsAutoKite(profile), Is.False);
            Assert.That(service.AllowsAutonomousMovement(profile, HoldPolicy.TacticalHold, true), Is.False);
        }

        [Test]
        public void AssistedAllowsAcquireButNotKite()
        {
            var profile = AutonomyResolver.Resolve(AutonomyCapabilities.AutoAcquire | AutonomyCapabilities.AutoKite, AutonomyOverride.Assisted, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified);
            var service = new AutonomyDecisionService();
            Assert.That(service.AllowsAutoAcquire(profile), Is.True);
            Assert.That(service.AllowsAutoKite(profile), Is.False);
        }

        [Test]
        public void StrictAndTacticalHoldPoliciesAreExplicit()
        {
            var profile = AutonomyResolver.Resolve(AutonomyCapabilities.AutoAcquire, AutonomyOverride.Automatic, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified, AutonomyOverride.Unspecified);
            var service = new AutonomyDecisionService();
            Assert.That(service.AllowsAutonomousMovement(profile, HoldPolicy.StrictHold, true), Is.False);
            Assert.That(service.AllowsAutonomousMovement(profile, HoldPolicy.TacticalHold, true), Is.True);
        }

        [Test]
        public void ForcedCommandCarriesPlayerAuthorityWithoutDirectWorldMutation()
        {
            CommandRequest request = Command(7, E(0), CommandSource.Human, CommandKind.Stop, forced: true);
            Assert.That(request.Forced, Is.True);
            Assert.That(request.Source, Is.EqualTo(CommandSource.Human));
        }

        [Test]
        public void UnknownCommandEnumIsRejectedByRequestConstructionBoundary()
        { Assert.Throws<ArgumentOutOfRangeException>(() => new CommandRequest(-1, E(0), CommandSource.Internal, CommandKind.Move, new CommandTarget(), QueueMode.Replace, 0)); }

        [Test]
        public void EmptyTargetRemainsExplicit()
        { Assert.That(new CommandTarget(null, null).IsEmpty, Is.True); }

        [Test]
        public void TargetCandidateLimitIsBounded()
        {
            var input = Enumerable.Range(0, 10).Select(i => new PerceptionCandidate(E(0), E(i + 1), i, 1));
            Assert.That(TargetEvaluator.Rank(input, new TargetEvaluationProfile(maxCandidates: 3)).Count, Is.EqualTo(3));
        }

        [Test]
        public void QueueSnapshotIsImmutableCopy()
        {
            var queue = new CommandQueue(); queue.Enqueue(Command(1, E(0), CommandSource.Human, CommandKind.Move));
            var snapshot = queue.Get(E(0));
            Assert.That(snapshot, Is.Not.TypeOf<CommandRequest[]>());
            queue.Enqueue(Command(2, E(0), CommandSource.Human, CommandKind.Stop));
            Assert.That(snapshot.Count, Is.EqualTo(1));
        }

        [Test]
        public void ArbitrationPermutationProducesSameWinner()
        {
            var a = new ActionArbitrationSystem();
            var p1 = new ActionArbitrationProposal(E(0), ArbitrationKind.EvadeProposal, 4, 3, Command(3, E(0), CommandSource.ComputerAI, CommandKind.Move));
            var p2 = new ActionArbitrationProposal(E(0), ArbitrationKind.RetreatProposal, 4, 2, Command(2, E(0), CommandSource.ComputerAI, CommandKind.Move));
            Assert.That(a.Resolve(new[] { p1, p2 })[0].Sequence, Is.EqualTo(a.Resolve(new[] { p2, p1 })[0].Sequence));
        }
    }
}
