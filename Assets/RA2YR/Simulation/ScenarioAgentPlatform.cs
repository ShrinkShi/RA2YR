using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Simulation
{
    public enum ScenarioPlacementFamily { Unit, Infantry, Structure, Aircraft }
    public enum SpawnDiagnosticCode { InvalidPlacement, UnknownOwner, DuplicateIdentity, BudgetExceeded, UnsupportedFamily }

    public readonly struct ScenarioPlacementRaw
    {
        public ScenarioPlacementRaw(ScenarioPlacementFamily family, string keyRaw, string valueRaw, int sourceOrdinal, int x, int y, string typeRaw, string ownerRaw)
        { if (sourceOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(sourceOrdinal)); Family = family; KeyRaw = keyRaw ?? string.Empty; ValueRaw = valueRaw ?? string.Empty; SourceOrdinal = sourceOrdinal; X = x; Y = y; TypeRaw = typeRaw ?? string.Empty; OwnerRaw = ownerRaw ?? string.Empty; }
        public ScenarioPlacementFamily Family { get; }
        public string KeyRaw { get; }
        public string ValueRaw { get; }
        public int SourceOrdinal { get; }
        public int X { get; }
        public int Y { get; }
        public string TypeRaw { get; }
        public string OwnerRaw { get; }
    }

    public readonly struct RuntimeOwnerIdentity : IEquatable<RuntimeOwnerIdentity>, IComparable<RuntimeOwnerIdentity>
    {
        public RuntimeOwnerIdentity(int id, string rawName) { if (id < 0) throw new ArgumentOutOfRangeException(nameof(id)); Id = id; RawName = rawName ?? string.Empty; }
        public int Id { get; }
        public string RawName { get; }
        public bool Equals(RuntimeOwnerIdentity other) => Id == other.Id && string.Equals(RawName, other.RawName, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is RuntimeOwnerIdentity && Equals((RuntimeOwnerIdentity)obj);
        public override int GetHashCode() => (Id * 397) ^ RawName.GetHashCode();
        public int CompareTo(RuntimeOwnerIdentity other) => Id.CompareTo(other.Id);
    }

    public readonly struct SpawnRequest : IComparable<SpawnRequest>
    {
        public SpawnRequest(ScenarioPlacementRaw placement, RuntimeOwnerIdentity owner, long ordinal) { if (ordinal < 0) throw new ArgumentOutOfRangeException(nameof(ordinal)); Placement = placement; Owner = owner; Ordinal = ordinal; }
        public ScenarioPlacementRaw Placement { get; }
        public RuntimeOwnerIdentity Owner { get; }
        public long Ordinal { get; }
        public int CompareTo(SpawnRequest other) { int c = Placement.SourceOrdinal.CompareTo(other.Placement.SourceOrdinal); return c != 0 ? c : Ordinal.CompareTo(other.Ordinal); }
    }

    public sealed class SpawnDiagnostic
    {
        public SpawnDiagnostic(SpawnDiagnosticCode code, ScenarioPlacementRaw placement, string message) { Code = code; Placement = placement; Message = message ?? string.Empty; }
        public SpawnDiagnosticCode Code { get; }
        public ScenarioPlacementRaw Placement { get; }
        public string Message { get; }
    }

    public sealed class SpawnResult
    {
        internal SpawnResult(IEnumerable<SpawnRequest> accepted, IEnumerable<SpawnDiagnostic> diagnostics) { Accepted = new ReadOnlyCollection<SpawnRequest>((accepted ?? Enumerable.Empty<SpawnRequest>()).OrderBy(x => x).ToList()); Diagnostics = new ReadOnlyCollection<SpawnDiagnostic>((diagnostics ?? Enumerable.Empty<SpawnDiagnostic>()).ToList()); }
        public IReadOnlyList<SpawnRequest> Accepted { get; }
        public IReadOnlyList<SpawnDiagnostic> Diagnostics { get; }
        public bool IsSuccess => Diagnostics.Count == 0;
    }

    public static class ScenarioSpawner
    {
        public static SpawnResult BuildRequests(IEnumerable<ScenarioPlacementRaw> placements, IEnumerable<RuntimeOwnerIdentity> owners, int maxRequests = 1024)
        {
            if (maxRequests <= 0) throw new ArgumentOutOfRangeException(nameof(maxRequests));
            var ownerMap = (owners ?? Enumerable.Empty<RuntimeOwnerIdentity>()).ToDictionary(x => x.RawName, StringComparer.OrdinalIgnoreCase);
            var accepted = new List<SpawnRequest>(); var diagnostics = new List<SpawnDiagnostic>(); long ordinal = 0;
            foreach (ScenarioPlacementRaw placement in placements ?? Enumerable.Empty<ScenarioPlacementRaw>())
            {
                if (accepted.Count >= maxRequests) { diagnostics.Add(new SpawnDiagnostic(SpawnDiagnosticCode.BudgetExceeded, placement, "Spawn request budget exceeded.")); break; }
                if (!Enum.IsDefined(typeof(ScenarioPlacementFamily), placement.Family) || string.IsNullOrEmpty(placement.TypeRaw) || placement.X < 0 || placement.Y < 0) { diagnostics.Add(new SpawnDiagnostic(SpawnDiagnosticCode.InvalidPlacement, placement, "Placement is invalid.")); continue; }
                RuntimeOwnerIdentity owner; if (!ownerMap.TryGetValue(placement.OwnerRaw, out owner)) { diagnostics.Add(new SpawnDiagnostic(SpawnDiagnosticCode.UnknownOwner, placement, "Owner is unresolved.")); continue; }
                accepted.Add(new SpawnRequest(placement, owner, ordinal++));
            }
            return new SpawnResult(accepted, diagnostics);
        }
    }

    public readonly struct AgentUnitObservation : IComparable<AgentUnitObservation>
    {
        public AgentUnitObservation(EntityId entity, int ownerId, ScenarioPlacementFamily family, int x, int y, int health, bool visibleToAgent) { Entity = entity; OwnerId = ownerId; Family = family; X = x; Y = y; Health = health; VisibleToAgent = visibleToAgent; }
        public EntityId Entity { get; }
        public int OwnerId { get; }
        public ScenarioPlacementFamily Family { get; }
        public int X { get; }
        public int Y { get; }
        public int Health { get; }
        public bool VisibleToAgent { get; }
        public int CompareTo(AgentUnitObservation other) => Entity.CompareTo(other.Entity);
    }

    public sealed class AgentObservation
    {
        internal AgentObservation(long tick, int ownerId, IEnumerable<AgentUnitObservation> units, IEnumerable<CellCoordinate> terrain, string objectiveDigest)
        { Tick = tick; OwnerId = ownerId; Units = new ReadOnlyCollection<AgentUnitObservation>((units ?? Enumerable.Empty<AgentUnitObservation>()).OrderBy(x => x).ToList()); KnownTerrain = new ReadOnlyCollection<CellCoordinate>((terrain ?? Enumerable.Empty<CellCoordinate>()).OrderBy(x => x).ToList()); ObjectiveDigest = objectiveDigest ?? string.Empty; }
        public long Tick { get; }
        public int OwnerId { get; }
        public IReadOnlyList<AgentUnitObservation> Units { get; }
        public IReadOnlyList<CellCoordinate> KnownTerrain { get; }
        public string ObjectiveDigest { get; }
        public bool ContainsWorldTruthApi => false;
    }

    public interface IAgentObservationProvider { AgentObservation Observe(int ownerId); }
    public interface IAgentPolicy { AgentDecision Evaluate(AgentObservation observation); }
    public readonly struct AgentDecision
    {
        public AgentDecision(bool available, CommandRequest? command, string diagnostic) { Available = available; Command = command; Diagnostic = diagnostic ?? string.Empty; }
        public bool Available { get; }
        public CommandRequest? Command { get; }
        public string Diagnostic { get; }
    }

    public enum NeuralBackendStatus { Available, Unavailable, SchemaMismatch }
    public readonly struct NeuralPolicyDescriptor { public NeuralPolicyDescriptor(string modelId, int schemaVersion) { ModelId = modelId ?? string.Empty; SchemaVersion = schemaVersion; } public string ModelId { get; } public int SchemaVersion { get; } }
    public interface INeuralPolicyBackend { NeuralBackendStatus Status { get; } AgentDecision Evaluate(NeuralPolicyDescriptor descriptor, AgentObservation observation); }

    public sealed class RuleBasedAgentPolicy : IAgentPolicy
    {
        private readonly int commandBudget;
        public RuleBasedAgentPolicy(int commandBudget = 1) { if (commandBudget <= 0) throw new ArgumentOutOfRangeException(nameof(commandBudget)); this.commandBudget = commandBudget; }
        public AgentDecision Evaluate(AgentObservation observation)
        {
            if (observation == null) return new AgentDecision(false, null, "ObservationUnavailable");
            AgentUnitObservation unit = observation.Units.FirstOrDefault(x => x.OwnerId == observation.OwnerId);
            if (!unit.Entity.IsValid || commandBudget == 0) return new AgentDecision(false, null, "NoCommandCandidate");
            var request = new CommandRequest(checked(observation.Tick + 1), unit.Entity, CommandSource.ComputerAI, CommandKind.Guard, new CommandTarget(new CellCoordinate(unit.X, unit.Y), null), QueueMode.Append, observation.Tick);
            return new AgentDecision(true, request, string.Empty);
        }
    }

    public readonly struct AgentCommandBudget { public AgentCommandBudget(int maxCommands, long interval, long reactionDelay) { if (maxCommands <= 0 || interval <= 0 || reactionDelay < 0) throw new ArgumentOutOfRangeException(); MaxCommands = maxCommands; Interval = interval; ReactionDelay = reactionDelay; } public int MaxCommands { get; } public long Interval { get; } public long ReactionDelay { get; } }
    public sealed class HeadlessSimulationEnvironment : IAgentObservationProvider
    {
        private readonly SimulationWorld world;
        private readonly List<AgentUnitObservation> units = new List<AgentUnitObservation>();
        public HeadlessSimulationEnvironment(int maximumEntities = 128) { world = new SimulationWorld(maximumEntities); }
        public long Tick => world.Tick;
        public EntityId AddUnit(int ownerId, ScenarioPlacementFamily family, int x, int y, int health = 100) { EntityId entity = world.CreateEntity(); units.Add(new AgentUnitObservation(entity, ownerId, family, x, y, health, true)); units.Sort(); return entity; }
        public AgentObservation Observe(int ownerId) => new AgentObservation(world.Tick, ownerId, units, Array.Empty<CellCoordinate>(), string.Empty);
        public void Step() => world.AdvanceTick();
        public string StateHash() => world.ComputeStateHash();
    }
}
