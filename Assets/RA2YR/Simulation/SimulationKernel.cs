using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Simulation
{
    public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
    {
        public EntityId(int index, int generation)
        {
            Index = index;
            Generation = generation;
        }

        public int Index { get; }
        public int Generation { get; }
        public bool IsValid => Index >= 0 && Generation > 0;

        public int CompareTo(EntityId other)
        {
            int index = Index.CompareTo(other.Index);
            return index != 0 ? index : Generation.CompareTo(other.Generation);
        }

        public bool Equals(EntityId other) => Index == other.Index && Generation == other.Generation;
        public override bool Equals(object obj) => obj is EntityId && Equals((EntityId)obj);
        public override int GetHashCode() => (Index * 397) ^ Generation;
        public override string ToString() => IsValid ? Index + ":" + Generation : "Invalid";
        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
    }

    public sealed class EntityRegistry
    {
        private readonly int[] generations;
        private readonly bool[] alive;

        public EntityRegistry(int maximumEntities)
        {
            if (maximumEntities <= 0) throw new ArgumentOutOfRangeException(nameof(maximumEntities));
            generations = new int[maximumEntities];
            alive = new bool[maximumEntities];
            for (int index = 0; index < generations.Length; index++) generations[index] = 1;
        }

        public int Capacity => generations.Length;
        public int AliveCount { get; private set; }

        public EntityId Create()
        {
            for (int index = 0; index < alive.Length; index++)
            {
                if (alive[index]) continue;
                alive[index] = true;
                AliveCount++;
                return new EntityId(index, generations[index]);
            }
            throw new InvalidOperationException("Entity capacity exceeded.");
        }

        public bool IsAlive(EntityId entity)
        {
            return entity.IsValid && entity.Index < alive.Length && alive[entity.Index] && generations[entity.Index] == entity.Generation;
        }

        public bool Destroy(EntityId entity)
        {
            if (!IsAlive(entity)) return false;
            alive[entity.Index] = false;
            AliveCount--;
            generations[entity.Index] = checked(generations[entity.Index] + 1);
            return true;
        }

        public IReadOnlyList<EntityId> SnapshotAlive()
        {
            var result = new List<EntityId>(AliveCount);
            for (int index = 0; index < alive.Length; index++)
                if (alive[index]) result.Add(new EntityId(index, generations[index]));
            return new ReadOnlyCollection<EntityId>(result);
        }
    }

    public sealed class ComponentStore<T> where T : struct
    {
        private readonly T[] values;
        private readonly bool[] present;
        private readonly EntityRegistry registry;

        public ComponentStore(int capacity)
            : this(capacity, null)
        {
        }

        internal ComponentStore(int capacity, EntityRegistry registry)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            values = new T[capacity];
            present = new bool[capacity];
            this.registry = registry;
        }

        public void Set(EntityId entity, T value)
        {
            ValidateIndex(entity);
            values[entity.Index] = value;
            present[entity.Index] = true;
        }

        public bool TryGet(EntityId entity, out T value)
        {
            if (!entity.IsValid || entity.Index < 0 || entity.Index >= values.Length || !present[entity.Index])
            {
                value = default(T);
                return false;
            }
            value = values[entity.Index];
            return true;
        }

        public bool Remove(EntityId entity)
        {
            if (!entity.IsValid || entity.Index < 0 || entity.Index >= values.Length || !present[entity.Index]) return false;
            present[entity.Index] = false;
            values[entity.Index] = default(T);
            return true;
        }

        public bool Has(EntityId entity) => entity.IsValid && entity.Index < present.Length && present[entity.Index];

        private void ValidateIndex(EntityId entity)
        {
            if (!entity.IsValid || entity.Index < 0 || entity.Index >= values.Length) throw new ArgumentException("Entity is not a valid component-store index.", nameof(entity));
            if (registry != null && !registry.IsAlive(entity)) throw new ArgumentException("Entity is stale or not alive.", nameof(entity));
        }
    }

    public readonly struct PositionComponent
    {
        public PositionComponent(int x, int y, int layer = 0) { X = x; Y = y; Layer = layer; }
        public int X { get; }
        public int Y { get; }
        public int Layer { get; }
    }

    public readonly struct HealthComponent
    {
        public HealthComponent(int current, int maximum)
        {
            if (maximum < 0 || current < 0 || current > maximum) throw new ArgumentOutOfRangeException();
            Current = current;
            Maximum = maximum;
        }
        public int Current { get; }
        public int Maximum { get; }
    }

    public enum MissionKind { Idle, Move, Attack, AttackMove, Stop, Hold, Guard, Harvest, ReturnToRefinery, Unload }

    public readonly struct MissionStateComponent
    {
        public MissionStateComponent(MissionKind kind, int commandId) { Kind = kind; CommandId = commandId; }
        public MissionKind Kind { get; }
        public int CommandId { get; }
    }

    public readonly struct TargetMemoryComponent
    {
        public TargetMemoryComponent(EntityId currentTarget, int score) { CurrentTarget = currentTarget; Score = score; }
        public EntityId CurrentTarget { get; }
        public int Score { get; }
    }

    public sealed class SimulationWorld
    {
        public SimulationWorld(int maximumEntities)
        {
            Registry = new EntityRegistry(maximumEntities);
            Positions = new ComponentStore<PositionComponent>(maximumEntities, Registry);
            Health = new ComponentStore<HealthComponent>(maximumEntities, Registry);
            Missions = new ComponentStore<MissionStateComponent>(maximumEntities, Registry);
            Targets = new ComponentStore<TargetMemoryComponent>(maximumEntities, Registry);
            Autonomy = new ComponentStore<AutonomyComponent>(maximumEntities, Registry);
        }

        public EntityRegistry Registry { get; }
        public ComponentStore<PositionComponent> Positions { get; }
        public ComponentStore<HealthComponent> Health { get; }
        public ComponentStore<MissionStateComponent> Missions { get; }
        public ComponentStore<TargetMemoryComponent> Targets { get; }
        public ComponentStore<AutonomyComponent> Autonomy { get; }
        public long Tick { get; private set; }

        public EntityId CreateEntity() => Registry.Create();

        public bool DestroyEntity(EntityId entity)
        {
            if (!Registry.Destroy(entity)) return false;
            Positions.Remove(entity); Health.Remove(entity); Missions.Remove(entity); Targets.Remove(entity); Autonomy.Remove(entity);
            return true;
        }

        public void AdvanceTick()
        {
            Tick = checked(Tick + 1);
        }

        public SimulationReadSnapshot CaptureSnapshot() => SimulationReadSnapshot.Capture(this);
        public string ComputeStateHash() => SimulationStateHasher.Compute(this);
    }

    public enum StructuralCommandKind { CreateEntity, DestroyEntity }

    public readonly struct StructuralCommand
    {
        internal StructuralCommand(long sequence, StructuralCommandKind kind, EntityId entity)
        { Sequence = sequence; Kind = kind; Entity = entity; }
        public long Sequence { get; }
        public StructuralCommandKind Kind { get; }
        public EntityId Entity { get; }
    }

    public sealed class StructuralCommandBuffer
    {
        private readonly List<StructuralCommand> commands = new List<StructuralCommand>();
        private long nextSequence;

        public int Count => commands.Count;
        public void EnqueueCreate() => commands.Add(new StructuralCommand(nextSequence++, StructuralCommandKind.CreateEntity, default(EntityId)));
        public void EnqueueDestroy(EntityId entity) => commands.Add(new StructuralCommand(nextSequence++, StructuralCommandKind.DestroyEntity, entity));

        public IReadOnlyList<StructuralCommand> SnapshotOrdered()
        {
            var copy = new List<StructuralCommand>(commands);
            copy.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            return new ReadOnlyCollection<StructuralCommand>(copy);
        }

        public IReadOnlyList<EntityId> Commit(SimulationWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var created = new List<EntityId>();
            foreach (StructuralCommand command in SnapshotOrdered())
            {
                if (command.Kind == StructuralCommandKind.CreateEntity) created.Add(world.CreateEntity());
                else if (command.Kind == StructuralCommandKind.DestroyEntity) world.DestroyEntity(command.Entity);
                else throw new InvalidOperationException("Unknown structural command.");
            }
            commands.Clear();
            return new ReadOnlyCollection<EntityId>(created);
        }
    }

    public sealed class SimulationTimeProfile
    {
        public SimulationTimeProfile(int ticksPerSecond)
        {
            if (ticksPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
            TicksPerSecond = ticksPerSecond;
        }
        public int TicksPerSecond { get; }
    }

    public sealed class SimulationClock
    {
        public SimulationClock(SimulationTimeProfile profile) { Profile = profile ?? throw new ArgumentNullException(nameof(profile)); }
        public SimulationTimeProfile Profile { get; }
        public long Tick { get; private set; }
        public long AdvanceOneTick() => Tick = checked(Tick + 1);
    }

    public enum SimulationPhase { Input, Command, Perception, Decision, MovementPlanning, MovementCommit, CombatPlanning, CombatCommit, Lifecycle, Finalize }

    public readonly struct SimulationSystemDescriptor
    {
        public SimulationSystemDescriptor(SimulationPhase phase, int order, string id)
        {
            if (!Enum.IsDefined(typeof(SimulationPhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("System id is required.", nameof(id));
            Phase = phase; Order = order; Id = id;
        }
        public SimulationPhase Phase { get; }
        public int Order { get; }
        public string Id { get; }
    }

    public sealed class DeterministicScheduler
    {
        private readonly List<SimulationSystemDescriptor> systems = new List<SimulationSystemDescriptor>();
        public void Register(SimulationSystemDescriptor descriptor) { systems.Add(descriptor); }
        public IReadOnlyList<SimulationSystemDescriptor> OrderedSystems()
        {
            var copy = new List<SimulationSystemDescriptor>(systems);
            copy.Sort((left, right) => { int phase = left.Phase.CompareTo(right.Phase); if (phase != 0) return phase; int order = left.Order.CompareTo(right.Order); return order != 0 ? order : string.CompareOrdinal(left.Id, right.Id); });
            return new ReadOnlyCollection<SimulationSystemDescriptor>(copy);
        }
    }

    public sealed class DeterministicRng
    {
        private uint state;
        public DeterministicRng(uint seed, string streamIdentity)
        {
            if (string.IsNullOrEmpty(streamIdentity)) throw new ArgumentException("Stream identity is required.", nameof(streamIdentity));
            state = seed ^ StableHash(streamIdentity);
            if (state == 0) state = 0x6D2B79F5u;
            StreamIdentity = streamIdentity;
        }
        public string StreamIdentity { get; }
        public long CallCount { get; private set; }
        public uint NextUInt()
        {
            uint x = state; x ^= x << 13; x ^= x >> 17; x ^= x << 5; state = x; CallCount = checked(CallCount + 1); return x;
        }
        public int NextInt(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            return (int)(NextUInt() % (uint)exclusiveMaximum);
        }
        private static uint StableHash(string value)
        {
            uint hash = 2166136261u;
            for (int index = 0; index < value.Length; index++) { hash ^= value[index]; hash *= 16777619u; }
            return hash;
        }
    }

    public static class SimulationStateHasher
    {
        public static string Compute(SimulationWorld world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            var builder = new StringBuilder();
            builder.Append("tick=").Append(world.Tick).Append(';');
            foreach (EntityId entity in world.Registry.SnapshotAlive())
            {
                builder.Append("e=").Append(entity.Index).Append(':').Append(entity.Generation).Append(';');
                PositionComponent position; if (world.Positions.TryGet(entity, out position)) builder.Append("p=").Append(position.X).Append(',').Append(position.Y).Append(',').Append(position.Layer).Append(';');
                HealthComponent health; if (world.Health.TryGet(entity, out health)) builder.Append("h=").Append(health.Current).Append(',').Append(health.Maximum).Append(';');
                MissionStateComponent mission; if (world.Missions.TryGet(entity, out mission)) builder.Append("m=").Append((int)mission.Kind).Append(',').Append(mission.CommandId).Append(';');
                TargetMemoryComponent target; if (world.Targets.TryGet(entity, out target)) builder.Append("t=").Append(target.CurrentTarget.Index).Append(':').Append(target.CurrentTarget.Generation).Append(',').Append(target.Score).Append(';');
                AutonomyComponent autonomy; if (world.Autonomy.TryGet(entity, out autonomy)) builder.Append("a=").Append((int)autonomy.Mode).Append(',').Append((int)autonomy.Capabilities).Append(';');
            }
            using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()))).Replace("-", string.Empty).ToLowerInvariant();
        }
    }

    public sealed class SimulationReadSnapshot
    {
        private SimulationReadSnapshot(long tick, IReadOnlyList<SnapshotEntity> entities) { Tick = tick; Entities = entities; }
        public long Tick { get; }
        public IReadOnlyList<SnapshotEntity> Entities { get; }
        internal static SimulationReadSnapshot Capture(SimulationWorld world)
        {
            var result = new List<SnapshotEntity>();
            foreach (EntityId entity in world.Registry.SnapshotAlive())
            {
                PositionComponent position; HealthComponent health; MissionStateComponent mission; TargetMemoryComponent target; AutonomyComponent autonomy;
                result.Add(new SnapshotEntity(entity, world.Positions.TryGet(entity, out position) ? (PositionComponent?)position : null,
                    world.Health.TryGet(entity, out health) ? (HealthComponent?)health : null,
                    world.Missions.TryGet(entity, out mission) ? (MissionStateComponent?)mission : null,
                    world.Targets.TryGet(entity, out target) ? (TargetMemoryComponent?)target : null,
                    world.Autonomy.TryGet(entity, out autonomy) ? (AutonomyComponent?)autonomy : null));
            }
            return new SimulationReadSnapshot(world.Tick, new ReadOnlyCollection<SnapshotEntity>(result));
        }
    }

    public readonly struct SnapshotEntity
    {
        internal SnapshotEntity(EntityId id, PositionComponent? position, HealthComponent? health, MissionStateComponent? mission, TargetMemoryComponent? target, AutonomyComponent? autonomy)
        { Id = id; Position = position; Health = health; Mission = mission; Target = target; Autonomy = autonomy; }
        public EntityId Id { get; }
        public PositionComponent? Position { get; }
        public HealthComponent? Health { get; }
        public MissionStateComponent? Mission { get; }
        public TargetMemoryComponent? Target { get; }
        public AutonomyComponent? Autonomy { get; }
    }

    public enum ActionProposalKind { None, Move, Attack, Retreat, Ability }
    public readonly struct ActionProposal : IComparable<ActionProposal>
    {
        public ActionProposal(EntityId entity, ActionProposalKind kind, int priority, long sequence)
        { Entity = entity; Kind = kind; Priority = priority; Sequence = sequence; }
        public EntityId Entity { get; }
        public ActionProposalKind Kind { get; }
        public int Priority { get; }
        public long Sequence { get; }
        public int CompareTo(ActionProposal other)
        {
            int priority = other.Priority.CompareTo(Priority); if (priority != 0) return priority;
            int entity = Entity.CompareTo(other.Entity); if (entity != 0) return entity;
            return Sequence.CompareTo(other.Sequence);
        }
    }

    public sealed class ActionProposalBuffer
    {
        private readonly List<ActionProposal> proposals = new List<ActionProposal>();
        public void Add(ActionProposal proposal) { proposals.Add(proposal); }
        public IReadOnlyList<ActionProposal> Ordered()
        {
            var copy = new List<ActionProposal>(proposals); copy.Sort(); return new ReadOnlyCollection<ActionProposal>(copy);
        }
        public void Clear() => proposals.Clear();
    }

    public interface IProposalComputeBackend
    {
        IReadOnlyList<ActionProposal> Evaluate(SimulationReadSnapshot snapshot, IReadOnlyList<ActionProposal> proposals);
    }

    public sealed class ManagedSequentialReferenceBackend : IProposalComputeBackend
    {
        public IReadOnlyList<ActionProposal> Evaluate(SimulationReadSnapshot snapshot, IReadOnlyList<ActionProposal> proposals)
        {
            if (snapshot == null || proposals == null) throw new ArgumentNullException();
            var copy = new List<ActionProposal>(proposals); copy.Sort(); return new ReadOnlyCollection<ActionProposal>(copy);
        }
    }

    [Flags]
    public enum AutonomyCapabilities { None = 0, AutoAcquire = 1, AutoKite = 2, AutoRetreat = 4, AutoCast = 8, Chase = 16, Evade = 32 }
    public enum AutonomyMode { Manual, Assisted, Automatic }
    public enum AutonomyOverride { Unspecified, Manual, Assisted, Automatic }

    public readonly struct AutonomyComponent
    {
        public AutonomyComponent(AutonomyMode mode, AutonomyCapabilities capabilities) { Mode = mode; Capabilities = capabilities; }
        public AutonomyMode Mode { get; }
        public AutonomyCapabilities Capabilities { get; }
    }

    public readonly struct AutonomyEnvelope
    {
        public AutonomyEnvelope(bool mayMove, bool mayAcquire, bool mayChase, bool mayRetreat, bool mayKite, bool mayCast, bool mayEvade)
        { MayMove = mayMove; MayAcquire = mayAcquire; MayChase = mayChase; MayRetreat = mayRetreat; MayKite = mayKite; MayCast = mayCast; MayEvade = mayEvade; }
        public bool MayMove { get; }
        public bool MayAcquire { get; }
        public bool MayChase { get; }
        public bool MayRetreat { get; }
        public bool MayKite { get; }
        public bool MayCast { get; }
        public bool MayEvade { get; }
        public static AutonomyEnvelope Manual => new AutonomyEnvelope(true, false, false, false, false, false, false);
        public static AutonomyEnvelope Automatic => new AutonomyEnvelope(true, true, true, true, true, true, true);
    }

    public readonly struct ResolvedAutonomyProfile
    {
        internal ResolvedAutonomyProfile(AutonomyMode mode, AutonomyCapabilities capabilities, AutonomyEnvelope envelope)
        { Mode = mode; Capabilities = capabilities; Envelope = envelope; }
        public AutonomyMode Mode { get; }
        public AutonomyCapabilities Capabilities { get; }
        public AutonomyEnvelope Envelope { get; }
    }

    public static class AutonomyResolver
    {
        public static ResolvedAutonomyProfile Resolve(AutonomyCapabilities baseCapabilities, AutonomyOverride global, AutonomyOverride player, AutonomyOverride group, AutonomyOverride unit)
        {
            AutonomyOverride selected = unit != AutonomyOverride.Unspecified ? unit : group != AutonomyOverride.Unspecified ? group : player != AutonomyOverride.Unspecified ? player : global;
            AutonomyMode mode = selected == AutonomyOverride.Unspecified ? AutonomyMode.Automatic : ToMode(selected);
            AutonomyCapabilities capabilities = mode == AutonomyMode.Manual ? AutonomyCapabilities.None : baseCapabilities;
            if (mode == AutonomyMode.Assisted) capabilities &= ~(AutonomyCapabilities.AutoKite | AutonomyCapabilities.AutoCast | AutonomyCapabilities.AutoRetreat);
            AutonomyEnvelope envelope = mode == AutonomyMode.Manual ? AutonomyEnvelope.Manual : mode == AutonomyMode.Assisted ? new AutonomyEnvelope(true, true, false, false, false, false, true) : AutonomyEnvelope.Automatic;
            return new ResolvedAutonomyProfile(mode, capabilities, envelope);
        }

        private static AutonomyMode ToMode(AutonomyOverride value)
        {
            if (!Enum.IsDefined(typeof(AutonomyOverride), value) || value == AutonomyOverride.Unspecified) throw new ArgumentOutOfRangeException(nameof(value));
            return (AutonomyMode)((int)value - 1);
        }
    }

    public sealed class DecisionSchedule
    {
        public DecisionSchedule(int period, int offset = 0)
        { if (period <= 0) throw new ArgumentOutOfRangeException(nameof(period)); Period = period; Offset = offset; }
        public int Period { get; }
        public int Offset { get; }
        public bool ShouldEvaluate(EntityId entity, long tick) => entity.IsValid && ((entity.Index + Offset) % Period + Period) % Period == tick % Period;
    }

    public sealed class SimulationMetrics
    {
        public long EntitiesProcessed { get; internal set; }
        public long SystemInvocations { get; internal set; }
        public long ProposalsGenerated { get; internal set; }
        public long StructuralChanges { get; internal set; }
        public long AiEvaluations { get; internal set; }
        public long SpatialQueries { get; internal set; }
        public long PathRequests { get; internal set; }
        public string StateHash { get; internal set; }
    }
}
