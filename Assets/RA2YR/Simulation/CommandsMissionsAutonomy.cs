using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RA2YR.Simulation
{
    public enum CommandSource { Human, ComputerAI, Script, Trigger, Internal }
    public enum CommandKind { Move, Attack, AttackMove, Stop, Hold, Guard, Patrol, Harvest }
    public enum QueueMode { Replace, Append }
    public enum CommandAcceptance { Accepted, Rejected, QueueFull, Invalid }
    public enum CommandDiagnosticCode { InvalidCommand, QueueFull, DuplicateCommand, ForcedPlayerOverride, InvalidTarget, UnknownMission, InvalidPolicy, BudgetExceeded }

    public readonly struct CommandTarget
    {
        public CommandTarget(CellCoordinate? cell, EntityId? entity, string rawIdentity = null) { Cell = cell; Entity = entity; RawIdentity = rawIdentity ?? string.Empty; }
        public CellCoordinate? Cell { get; }
        public EntityId? Entity { get; }
        public string RawIdentity { get; }
        public bool IsEmpty => !Cell.HasValue && !Entity.HasValue;
    }

    public readonly struct CommandRequest : IComparable<CommandRequest>
    {
        public CommandRequest(long commandId, EntityId actor, CommandSource source, CommandKind kind, CommandTarget target, QueueMode queueMode, long issuedTick, bool forced = false)
        { if (commandId < 0 || !actor.IsValid || !Enum.IsDefined(typeof(CommandSource), source) || !Enum.IsDefined(typeof(CommandKind), kind) || !Enum.IsDefined(typeof(QueueMode), queueMode)) throw new ArgumentOutOfRangeException(); CommandId = commandId; Actor = actor; Source = source; Kind = kind; Target = target; QueueMode = queueMode; IssuedTick = issuedTick; Forced = forced; }
        public long CommandId { get; }
        public EntityId Actor { get; }
        public CommandSource Source { get; }
        public CommandKind Kind { get; }
        public CommandTarget Target { get; }
        public QueueMode QueueMode { get; }
        public long IssuedTick { get; }
        public bool Forced { get; }
        public int CompareTo(CommandRequest other) { int actor = Actor.CompareTo(other.Actor); return actor != 0 ? actor : CommandId.CompareTo(other.CommandId); }
    }

    public sealed class CommandDiagnostic
    {
        public CommandDiagnostic(CommandDiagnosticCode code, long commandId, EntityId actor, string message) { Code = code; CommandId = commandId; Actor = actor; Message = message ?? string.Empty; }
        public CommandDiagnosticCode Code { get; }
        public long CommandId { get; }
        public EntityId Actor { get; }
        public string Message { get; }
    }

    public sealed class CommandAcceptanceResult
    {
        internal CommandAcceptanceResult(CommandRequest request, CommandAcceptance status, IEnumerable<CommandDiagnostic> diagnostics) { Request = request; Status = status; Diagnostics = new ReadOnlyCollection<CommandDiagnostic>((diagnostics ?? Enumerable.Empty<CommandDiagnostic>()).ToList()); }
        public CommandRequest Request { get; }
        public CommandAcceptance Status { get; }
        public bool IsAccepted => Status == CommandAcceptance.Accepted;
        public IReadOnlyList<CommandDiagnostic> Diagnostics { get; }
    }

    public sealed class CommandQueue
    {
        private readonly int capacity;
        private readonly Dictionary<EntityId, List<CommandRequest>> queues = new Dictionary<EntityId, List<CommandRequest>>();
        public CommandQueue(int capacity = 64) { if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity)); this.capacity = capacity; }
        public IReadOnlyList<CommandRequest> Get(EntityId actor) { List<CommandRequest> list; return queues.TryGetValue(actor, out list) ? new ReadOnlyCollection<CommandRequest>(list.ToList()) : new ReadOnlyCollection<CommandRequest>(new List<CommandRequest>()); }
        public CommandAcceptanceResult Enqueue(CommandRequest request)
        {
            List<CommandRequest> list; if (!queues.TryGetValue(request.Actor, out list)) { list = new List<CommandRequest>(); queues.Add(request.Actor, list); }
            if (list.Any(x => x.CommandId == request.CommandId)) return new CommandAcceptanceResult(request, CommandAcceptance.Rejected, new[] { new CommandDiagnostic(CommandDiagnosticCode.DuplicateCommand, request.CommandId, request.Actor, "Command id is already queued.") });
            if (request.QueueMode == QueueMode.Replace) list.Clear();
            if (list.Count >= capacity) return new CommandAcceptanceResult(request, CommandAcceptance.QueueFull, new[] { new CommandDiagnostic(CommandDiagnosticCode.QueueFull, request.CommandId, request.Actor, "Command queue budget exceeded.") });
            list.Add(request); list.Sort(); return new CommandAcceptanceResult(request, CommandAcceptance.Accepted, Array.Empty<CommandDiagnostic>());
        }
        public IReadOnlyList<CommandRequest> SnapshotCanonical() => new ReadOnlyCollection<CommandRequest>(queues.Values.SelectMany(x => x).OrderBy(x => x).ToList());
    }

    public readonly struct RuntimeMissionSnapshot
    {
        public RuntimeMissionSnapshot(EntityId actor, CommandKind kind, long commandId, long enteredTick, EntityId? target, string authoredMissionRaw, bool interruptible, string completionReason)
        { Actor = actor; Kind = kind; CommandId = commandId; EnteredTick = enteredTick; Target = target; AuthoredMissionRaw = authoredMissionRaw ?? string.Empty; Interruptible = interruptible; CompletionReason = completionReason ?? string.Empty; }
        public EntityId Actor { get; }
        public CommandKind Kind { get; }
        public long CommandId { get; }
        public long EnteredTick { get; }
        public EntityId? Target { get; }
        public string AuthoredMissionRaw { get; }
        public bool Interruptible { get; }
        public string CompletionReason { get; }
    }

    public readonly struct PerceptionCandidate : IComparable<PerceptionCandidate>
    {
        public PerceptionCandidate(EntityId observer, EntityId target, int distance, int threat) { Observer = observer; Target = target; Distance = distance; Threat = threat; }
        public EntityId Observer { get; }
        public EntityId Target { get; }
        public int Distance { get; }
        public int Threat { get; }
        public int CompareTo(PerceptionCandidate other) { int o = Observer.CompareTo(other.Observer); return o != 0 ? o : Target.CompareTo(other.Target); }
    }

    public sealed class PerceptionService
    {
        private readonly DeterministicSpatialIndex index;
        private readonly Dictionary<EntityId, CellCoordinate> positions = new Dictionary<EntityId, CellCoordinate>();
        public PerceptionService(DeterministicSpatialIndex index) { this.index = index ?? throw new ArgumentNullException(nameof(index)); }
        public void Track(EntityId entity, CellCoordinate cell) { positions[entity] = cell; }
        public IReadOnlyList<PerceptionCandidate> Query(EntityId observer, int radius, int threat = 0)
        {
            CellCoordinate cell; if (!positions.TryGetValue(observer, out cell)) return Array.Empty<PerceptionCandidate>();
            var result = new List<PerceptionCandidate>(); foreach (EntityId target in index.QueryNeighbors(cell, radius)) if (target != observer) { CellCoordinate targetCell; if (positions.TryGetValue(target, out targetCell)) result.Add(new PerceptionCandidate(observer, target, Math.Abs(targetCell.X - cell.X) + Math.Abs(targetCell.Y - cell.Y), threat)); }
            result.Sort(); return new ReadOnlyCollection<PerceptionCandidate>(result);
        }
    }

    public sealed class TargetEvaluationProfile
    {
        public TargetEvaluationProfile(int distanceWeight = 1, int threatWeight = 1, int strategicWeight = 0, int hysteresis = 0, int maxCandidates = 64)
        { if (maxCandidates <= 0 || distanceWeight < 0 || threatWeight < 0 || strategicWeight < 0 || hysteresis < 0) throw new ArgumentOutOfRangeException(); DistanceWeight = distanceWeight; ThreatWeight = threatWeight; StrategicWeight = strategicWeight; Hysteresis = hysteresis; MaxCandidates = maxCandidates; }
        public int DistanceWeight { get; }
        public int ThreatWeight { get; }
        public int StrategicWeight { get; }
        public int Hysteresis { get; }
        public int MaxCandidates { get; }
    }

    public readonly struct TargetScore : IComparable<TargetScore>
    {
        public TargetScore(EntityId target, int score, int distance, int threat) { Target = target; Score = score; Distance = distance; Threat = threat; }
        public EntityId Target { get; }
        public int Score { get; }
        public int Distance { get; }
        public int Threat { get; }
        public int CompareTo(TargetScore other) { int score = other.Score.CompareTo(Score); return score != 0 ? score : Target.CompareTo(other.Target); }
    }

    public static class TargetEvaluator
    {
        public static IReadOnlyList<TargetScore> Rank(IEnumerable<PerceptionCandidate> candidates, TargetEvaluationProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile)); var scores = new List<TargetScore>();
            foreach (PerceptionCandidate candidate in (candidates ?? Enumerable.Empty<PerceptionCandidate>()).Take(profile.MaxCandidates)) scores.Add(new TargetScore(candidate.Target, checked(candidate.Threat * profile.ThreatWeight - candidate.Distance * profile.DistanceWeight), candidate.Distance, candidate.Threat));
            scores.Sort(); return new ReadOnlyCollection<TargetScore>(scores);
        }
    }

    public readonly struct TargetMemory
    {
        public TargetMemory(EntityId? current, EntityId? last, int score, long seenTick, int hysteresis) { Current = current; Last = last; Score = score; LastSeenTick = seenTick; Hysteresis = hysteresis; }
        public EntityId? Current { get; }
        public EntityId? Last { get; }
        public int Score { get; }
        public long LastSeenTick { get; }
        public int Hysteresis { get; }
        public TargetMemory Update(TargetScore candidate, long tick) => !Current.HasValue || candidate.Score >= Score + Hysteresis ? new TargetMemory(candidate.Target, Current, candidate.Score, tick, Hysteresis) : new TargetMemory(Current, Last, Score, LastSeenTick, Hysteresis);
    }

    public enum ArbitrationKind { PlayerCommand, MissionContinuation, TargetProposal, RetreatProposal, EvadeProposal }
    public readonly struct ActionArbitrationProposal : IComparable<ActionArbitrationProposal>
    {
        public ActionArbitrationProposal(EntityId actor, ArbitrationKind kind, int priority, long sequence, CommandRequest command)
        { Actor = actor; Kind = kind; Priority = priority; Sequence = sequence; Command = command; }
        public EntityId Actor { get; }
        public ArbitrationKind Kind { get; }
        public int Priority { get; }
        public long Sequence { get; }
        public CommandRequest Command { get; }
        public int CompareTo(ActionArbitrationProposal other) { int p = other.Priority.CompareTo(Priority); if (p != 0) return p; int a = Actor.CompareTo(other.Actor); return a != 0 ? a : Sequence.CompareTo(other.Sequence); }
    }

    public sealed class ActionArbitrationSystem
    {
        public IReadOnlyList<ActionArbitrationProposal> Resolve(IEnumerable<ActionArbitrationProposal> proposals)
        {
            var result = new List<ActionArbitrationProposal>(); foreach (IGrouping<EntityId, ActionArbitrationProposal> group in (proposals ?? Enumerable.Empty<ActionArbitrationProposal>()).GroupBy(x => x.Actor).OrderBy(x => x.Key)) result.Add(group.OrderBy(x => x).First());
            return new ReadOnlyCollection<ActionArbitrationProposal>(result);
        }
    }

    public enum HoldPolicy { StrictHold, TacticalHold }
    public sealed class AutonomyDecisionService
    {
        public bool AllowsAutonomousMovement(ResolvedAutonomyProfile profile, HoldPolicy hold, bool emergency)
        { if (profile.Mode == AutonomyMode.Manual) return false; if (hold == HoldPolicy.StrictHold) return false; return profile.Mode == AutonomyMode.Automatic || emergency; }
        public bool AllowsAutoAcquire(ResolvedAutonomyProfile profile) => profile.Mode != AutonomyMode.Manual && (profile.Capabilities & AutonomyCapabilities.AutoAcquire) != 0;
        public bool AllowsAutoKite(ResolvedAutonomyProfile profile) => profile.Mode == AutonomyMode.Automatic && (profile.Capabilities & AutonomyCapabilities.AutoKite) != 0;
    }
}
