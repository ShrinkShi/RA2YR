using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Simulation
{
    [Flags]
    public enum HumanPlaytestEntityCapabilities
    {
        None = 0,
        Selectable = 1,
        Controllable = 2,
        Mobile = 4,
        Structure = 8,
        Enemy = 16
    }

    public enum HumanPlaytestEntityKind { Unit, Harvester, Refinery, Factory, Power, MainBase }
    public enum HumanPlaytestMatchStatus { Running, Victory, Defeat }

    public readonly struct HumanPlaytestRuntimeConfig
    {
        public HumanPlaytestRuntimeConfig(int seed = 20260815, int width = 28, int height = 22, int maxEntities = 128, int startingCredits = 200)
        {
            if (width <= 0 || height <= 0 || maxEntities <= 0 || startingCredits < 0) throw new ArgumentOutOfRangeException();
            Seed = seed;
            Width = width;
            Height = height;
            MaxEntities = maxEntities;
            StartingCredits = startingCredits;
        }

        public int Seed { get; }
        public int Width { get; }
        public int Height { get; }
        public int MaxEntities { get; }
        public int StartingCredits { get; }
        public static HumanPlaytestRuntimeConfig Default => new HumanPlaytestRuntimeConfig(20260815, 28, 22, 128, 200);
    }

    public readonly struct HumanPlaytestEntitySnapshot : IComparable<HumanPlaytestEntitySnapshot>
    {
        internal HumanPlaytestEntitySnapshot(EntityId entity, PlayerId owner, HumanPlaytestEntityKind kind, HumanPlaytestEntityCapabilities capabilities, int x, int y, int health, int maximumHealth, MissionKind mission, AutonomyMode autonomy, long cargo)
        {
            Entity = entity;
            Owner = owner;
            Kind = kind;
            Capabilities = capabilities;
            X = x;
            Y = y;
            Health = health;
            MaximumHealth = maximumHealth;
            Mission = mission;
            Autonomy = autonomy;
            Cargo = cargo;
        }

        public EntityId Entity { get; }
        public PlayerId Owner { get; }
        public HumanPlaytestEntityKind Kind { get; }
        public HumanPlaytestEntityCapabilities Capabilities { get; }
        public bool IsSelectable => (Capabilities & HumanPlaytestEntityCapabilities.Selectable) != 0;
        public bool IsControllable => (Capabilities & HumanPlaytestEntityCapabilities.Controllable) != 0;
        public bool IsMobile => (Capabilities & HumanPlaytestEntityCapabilities.Mobile) != 0;
        public bool IsStructure => (Capabilities & HumanPlaytestEntityCapabilities.Structure) != 0;
        public bool IsEnemy => (Capabilities & HumanPlaytestEntityCapabilities.Enemy) != 0;
        public int X { get; }
        public int Y { get; }
        public int Health { get; }
        public int MaximumHealth { get; }
        public MissionKind Mission { get; }
        public AutonomyMode Autonomy { get; }
        public long Cargo { get; }
        public bool IsAlive => Health > 0;
        public int CompareTo(HumanPlaytestEntitySnapshot other) => Entity.CompareTo(other.Entity);
    }

    public sealed class HumanPlaytestSnapshot
    {
        internal HumanPlaytestSnapshot(long tick, HumanPlaytestMatchStatus status, PlayerId? winner, long credits, long cargo, int harvestEvents, int productionEvents, int combatEvents, int spawnedUnits, int destroyedUnits, int selectedPlayerUnits, int queueCount, IEnumerable<HumanPlaytestEntitySnapshot> entities, string stateHash)
        {
            Tick = tick;
            Status = status;
            Winner = winner;
            Credits = credits;
            HarvesterCargo = cargo;
            HarvestEvents = harvestEvents;
            ProductionEvents = productionEvents;
            CombatEvents = combatEvents;
            SpawnedUnits = spawnedUnits;
            DestroyedUnits = destroyedUnits;
            SelectedPlayerUnits = selectedPlayerUnits;
            ProductionQueueCount = queueCount;
            Entities = new ReadOnlyCollection<HumanPlaytestEntitySnapshot>((entities ?? Enumerable.Empty<HumanPlaytestEntitySnapshot>()).OrderBy(x => x).ToList());
            StateHash = stateHash ?? string.Empty;
        }

        public long Tick { get; }
        public HumanPlaytestMatchStatus Status { get; }
        public PlayerId? Winner { get; }
        public long Credits { get; }
        public long HarvesterCargo { get; }
        public int HarvestEvents { get; }
        public int ProductionEvents { get; }
        public int CombatEvents { get; }
        public int SpawnedUnits { get; }
        public int DestroyedUnits { get; }
        public int SelectedPlayerUnits { get; }
        public int ProductionQueueCount { get; }
        public IReadOnlyList<HumanPlaytestEntitySnapshot> Entities { get; }
        public string StateHash { get; }
        public bool IsComplete => Status != HumanPlaytestMatchStatus.Running;
    }

    public readonly struct HumanPlaytestEquivalenceResult
    {
        internal HumanPlaytestEquivalenceResult(bool equal, string headlessHash, string presentationHash, int ticks)
        { IsEqual = equal; HeadlessHash = headlessHash ?? string.Empty; PresentationHash = presentationHash ?? string.Empty; Ticks = ticks; }
        public bool IsEqual { get; }
        public string HeadlessHash { get; }
        public string PresentationHash { get; }
        public int Ticks { get; }
    }

    public sealed class HumanPlaytestRuntime
    {
        private readonly HumanPlaytestRuntimeConfig config;
        private readonly Dictionary<EntityId, PlayerId> owners = new Dictionary<EntityId, PlayerId>();
        private readonly Dictionary<EntityId, HumanPlaytestEntityKind> kinds = new Dictionary<EntityId, HumanPlaytestEntityKind>();
        private readonly Dictionary<EntityId, CellCoordinate> moveDestinations = new Dictionary<EntityId, CellCoordinate>();
        private readonly HashSet<long> processedCommands = new HashSet<long>();
        private readonly HashSet<long> completedProduction = new HashSet<long>();
        private long nextCommandId;
        private long nextQueueId;
        private long harvesterCargo;
        private int harvestEvents;
        private int productionEvents;
        private int combatEvents;
        private int spawnedUnits;
        private int destroyedUnits;
        private PlayerId? winner;
        private HumanPlaytestMatchStatus status;
        private EntityId humanBase;
        private EntityId aiBase;
        private EntityId harvester;
        private EntityId humanFactory;
        private EntityId aiUnit;
        private bool harvesterPlayerOverride;
        private CellCoordinate harvesterCommandTarget;
        private int resourceQuantity;
        private readonly CellCoordinate resourceCell = new CellCoordinate(9, 3);
        private readonly CellCoordinate refineryCell = new CellCoordinate(3, 3);

        public HumanPlaytestRuntime(HumanPlaytestRuntimeConfig config)
        {
            this.config = config;
            Reset();
        }

        public HumanPlaytestRuntimeConfig Config => config;
        public SimulationWorld World { get; private set; }
        public CommandQueue CommandQueue { get; private set; }
        public EconomyAuthority Economy { get; private set; }
        public ProductionQueue Production { get; private set; }
        public IReadOnlyList<ProductionDefinitionDescriptor> ProductionDefinitions { get; private set; }
        public ResourceEconomyConsistencyAnalysis ResourceAnalysis { get; private set; }
        public RefineryCapabilityDescriptor Refinery { get; private set; }
        public PlayerId HumanPlayer => new PlayerId(0);
        public PlayerId AiPlayer => new PlayerId(1);
        public long Tick => World == null ? 0 : World.Tick;
        public HumanPlaytestMatchStatus Status => status;
        public PlayerId? Winner => winner;
        public long HarvesterCargo => harvesterCargo;
        public int HarvestEvents => harvestEvents;
        public int ProductionEvents => productionEvents;
        public int CombatEvents => combatEvents;
        public int SpawnedUnits => spawnedUnits;
        public int DestroyedUnits => destroyedUnits;
        public EntityId HumanBase => humanBase;
        public EntityId AiBase => aiBase;
        public EntityId Harvester => harvester;
        public EntityId HumanFactory => humanFactory;
        public EntityId AiUnit => aiUnit;

        public IReadOnlyList<EntityId> HumanUnits => OrderedEntities().Where(x => owners[x].Equals(HumanPlayer) && kinds[x] == HumanPlaytestEntityKind.Unit).ToList().AsReadOnly();
        public IReadOnlyList<EntityId> HumanSelectableEntities => OrderedEntities().Where(x => IsSelectable(x, HumanPlayer)).ToList().AsReadOnly();
        public CellCoordinate SyntheticResourceCell => resourceCell;
        public CellCoordinate SyntheticRefineryCell => refineryCell;
        public int ResourceQuantity => resourceQuantity;

        public void Reset()
        {
            World = new SimulationWorld(config.MaxEntities);
            CommandQueue = new CommandQueue(256);
            Economy = new EconomyAuthority();
            Production = new ProductionQueue(new ProductionReadLimits(64, 16, 32, 64, 256));
            owners.Clear();
            kinds.Clear();
            moveDestinations.Clear();
            processedCommands.Clear();
            completedProduction.Clear();
            nextCommandId = 1;
            nextQueueId = 1;
            harvesterCargo = 0;
            harvesterPlayerOverride = false;
            harvesterCommandTarget = resourceCell;
            resourceQuantity = 100;
            harvestEvents = 0;
            productionEvents = 0;
            combatEvents = 0;
            spawnedUnits = 0;
            destroyedUnits = 0;
            winner = null;
            status = HumanPlaytestMatchStatus.Running;
            Economy.Register(HumanPlayer, config.StartingCredits);
            Economy.Register(AiPlayer, config.StartingCredits);
            ProductionDefinitions = new ReadOnlyCollection<ProductionDefinitionDescriptor>(new[]
            {
                new ProductionDefinitionDescriptor(new ProductionDefinitionRaw(0, "BasicScout", "Unit", 0, 50, 5, -1, Array.Empty<string>()), "SyntheticConfigured")
            });
            ResourceAnalysis = ResourceEconomyConsistencyAnalysis.Analyze(
                new[] { new ResourceCellRaw(0, 1, 24, ResourceFamily.Ore, ResourceVisualStage.Abundant) },
                new[] { new ResourceTypeRaw(0, "Ore", "25", ResourceFamily.Ore, 25) },
                ResourceQuantityProfile.OverlayDataPlusOne,
                ResourceValueProfile.RulesResourceValue,
                ResourceEconomyReadLimits.Default);
            Refinery = new RefineryCapabilityDescriptor("SyntheticRefinery", new[] { ResourceFamily.Ore }, new[] { new DockingSlotDescriptor(0, 3, 2, 3, 3, 4, 3, 100) });
            EntityId humanPower = AddEntity(HumanPlayer, HumanPlaytestEntityKind.Power, 2, 4, 120, AutonomyMode.Manual);
            humanBase = AddEntity(HumanPlayer, HumanPlaytestEntityKind.MainBase, 2, 2, 250, AutonomyMode.Manual);
            EntityId humanRefinery = AddEntity(HumanPlayer, HumanPlaytestEntityKind.Refinery, refineryCell.X, refineryCell.Y, 160, AutonomyMode.Manual);
            humanFactory = AddEntity(HumanPlayer, HumanPlaytestEntityKind.Factory, 4, 3, 180, AutonomyMode.Manual);
            harvester = AddEntity(HumanPlayer, HumanPlaytestEntityKind.Harvester, 5, 3, 90, AutonomyMode.Manual);
            AddEntity(HumanPlayer, HumanPlaytestEntityKind.Unit, 6, 3, 100, AutonomyMode.Manual);
            AddEntity(HumanPlayer, HumanPlaytestEntityKind.Unit, 6, 4, 100, AutonomyMode.Manual);
            aiBase = AddEntity(AiPlayer, HumanPlaytestEntityKind.MainBase, 24, 18, 250, AutonomyMode.Automatic);
            AddEntity(AiPlayer, HumanPlaytestEntityKind.Factory, 22, 18, 180, AutonomyMode.Automatic);
            aiUnit = AddEntity(AiPlayer, HumanPlaytestEntityKind.Unit, 19, 18, 100, AutonomyMode.Automatic);
            World.Targets.Set(humanRefinery, new TargetMemoryComponent(default(EntityId), 0));
            World.Targets.Set(harvester, new TargetMemoryComponent(default(EntityId), 0));
        }

        public IReadOnlyList<CommandAcceptanceResult> EnqueueHumanCommands(IEnumerable<EntityId> entities, CommandKind kind, CommandTarget target, QueueMode queueMode = QueueMode.Replace)
        {
            var results = new List<CommandAcceptanceResult>();
            foreach (EntityId entity in (entities ?? Enumerable.Empty<EntityId>()).OrderBy(x => x))
            {
                var request = new CommandRequest(nextCommandId++, entity, CommandSource.Human, kind, target, queueMode, Tick);
                results.Add(CommandQueue.Enqueue(request));
            }
            return new ReadOnlyCollection<CommandAcceptanceResult>(results);
        }

        public bool SetAutonomy(IEnumerable<EntityId> entities, AutonomyMode mode)
        {
            if (!Enum.IsDefined(typeof(AutonomyMode), mode)) return false;
            bool changed = false;
            foreach (EntityId entity in (entities ?? Enumerable.Empty<EntityId>()).OrderBy(x => x))
                if (IsOwnedAlive(entity, HumanPlayer) && (kinds[entity] == HumanPlaytestEntityKind.Unit || kinds[entity] == HumanPlaytestEntityKind.Harvester))
                {
                    World.Autonomy.Set(entity, new AutonomyComponent(mode, AutonomyCapabilities.AutoAcquire | AutonomyCapabilities.AutoKite | AutonomyCapabilities.AutoRetreat));
                    if (entity.Equals(harvester) && mode != AutonomyMode.Manual)
                    {
                        harvesterPlayerOverride = false;
                        moveDestinations.Remove(entity);
                        World.Missions.Set(entity, new MissionStateComponent(MissionKind.Idle, checked((int)Math.Min(Tick, int.MaxValue))));
                    }
                    changed = true;
                }
            return changed;
        }

        public bool QueueProduction(PlayerId owner = default(PlayerId))
        {
            if (owner.Value != HumanPlayer.Value) owner = HumanPlayer;
            if (!IsOwnedAlive(humanFactory, owner)) return false;
            ProductionDefinitionDescriptor definition = ProductionDefinitions[0];
            if (Economy.Get(owner).Balance < definition.Raw.RawCost) return false;
            ProductionQueueId id = new ProductionQueueId(nextQueueId++);
            var entry = new ProductionQueueEntry(id, owner, definition.Raw.TypeRaw, definition.Raw.CategoryRaw, definition.Raw.RawCost, definition.Raw.RawBuildTime, id.Value);
            EconomyTransaction transaction;
            if (!Economy.TryApply(EconomyTransactionSource.ProductionSpend, owner, Tick, -definition.Raw.RawCost, "synthetic-production", out transaction)) return false;
            IReadOnlyList<ProductionDiagnostic> diagnostics;
            if (!Production.TryEnqueue(entry, out diagnostics))
            {
                Economy.TryApply(EconomyTransactionSource.ScriptAdjustment, owner, Tick, definition.Raw.RawCost, "production-rejected-refund", out transaction);
                return false;
            }
            return true;
        }

        public void Step()
        {
            if (status != HumanPlaytestMatchStatus.Running) return;
            World.AdvanceTick();
            EmitRuleBasedOpponentCommand();
            ProcessPendingCommands();
            ProcessAutonomy();
            ProcessEconomy();
            ProcessProduction();
            ProcessMovement();
            ProcessCombat();
            ResolveMatch();
        }

        public HumanPlaytestSnapshot CaptureSnapshot(IEnumerable<EntityId> selected = null)
        {
            var selectedSet = new HashSet<EntityId>(selected ?? Enumerable.Empty<EntityId>());
            var entities = new List<HumanPlaytestEntitySnapshot>();
            foreach (EntityId entity in OrderedEntities())
            {
                PositionComponent position; HealthComponent health; MissionStateComponent mission; AutonomyComponent autonomy;
                if (!World.Positions.TryGet(entity, out position) || !World.Health.TryGet(entity, out health)) continue;
                World.Missions.TryGet(entity, out mission);
                World.Autonomy.TryGet(entity, out autonomy);
                entities.Add(new HumanPlaytestEntitySnapshot(entity, owners[entity], kinds[entity], CapabilitiesFor(kinds[entity], owners[entity]), position.X, position.Y, health.Current, health.Maximum, mission.Kind, autonomy.Mode, entity == harvester ? harvesterCargo : 0));
            }
            return new HumanPlaytestSnapshot(Tick, status, winner, Economy.Get(HumanPlayer).Balance, harvesterCargo, harvestEvents, productionEvents, combatEvents, spawnedUnits, destroyedUnits, selectedSet.Count(x => IsOwnedAlive(x, HumanPlayer)), Production.Entries.Count, entities, ComputeStateHash());
        }

        public string ComputeStateHash()
        {
            var builder = new StringBuilder();
            builder.Append(config.Seed).Append('|').Append(Tick).Append('|').Append((int)status).Append('|').Append(winner.HasValue ? winner.Value.Value.ToString() : "-").Append('|').Append(Economy.StateHash()).Append('|').Append(Production.CanonicalHash()).Append('|');
            foreach (EntityId entity in OrderedEntities())
            {
                PositionComponent position; HealthComponent health; MissionStateComponent mission; AutonomyComponent autonomy;
                World.Positions.TryGet(entity, out position); World.Health.TryGet(entity, out health); World.Missions.TryGet(entity, out mission); World.Autonomy.TryGet(entity, out autonomy);
                builder.Append(entity.Index).Append(':').Append(owners[entity].Value).Append(':').Append((int)kinds[entity]).Append(':').Append(position.X).Append(',').Append(position.Y).Append(':').Append(health.Current).Append(':').Append((int)mission.Kind).Append(',').Append((int)autonomy.Mode).Append(';');
            }
            builder.Append("cargo=").Append(harvesterCargo).Append(";events=").Append(harvestEvents).Append(',').Append(productionEvents).Append(',').Append(combatEvents).Append(';');
            using (SHA256 sha = SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())).Select(x => x.ToString("x2")));
        }

        public static HumanPlaytestEquivalenceResult ProvePresentationEquivalence(int ticks = 24)
        {
            if (ticks <= 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            var headless = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            var presented = new HumanPlaytestRuntime(HumanPlaytestRuntimeConfig.Default);
            EntityId headlessUnit = headless.HumanUnits[0];
            EntityId presentedUnit = presented.HumanUnits[0];
            headless.EnqueueHumanCommands(new[] { headlessUnit }, CommandKind.Move, new CommandTarget(new CellCoordinate(10, 6), null));
            presented.EnqueueHumanCommands(new[] { presentedUnit }, CommandKind.Move, new CommandTarget(new CellCoordinate(10, 6), null));
            for (int i = 0; i < ticks; i++)
            {
                headless.Step();
                presented.Step();
                presented.CaptureSnapshot(new[] { presentedUnit });
            }
            return new HumanPlaytestEquivalenceResult(string.Equals(headless.ComputeStateHash(), presented.ComputeStateHash(), StringComparison.Ordinal), headless.ComputeStateHash(), presented.ComputeStateHash(), ticks);
        }

        private EntityId AddEntity(PlayerId owner, HumanPlaytestEntityKind kind, int x, int y, int health, AutonomyMode autonomy)
        {
            EntityId entity = World.CreateEntity();
            owners.Add(entity, owner);
            kinds.Add(entity, kind);
            World.Positions.Set(entity, new PositionComponent(x, y));
            World.Health.Set(entity, new HealthComponent(health, health));
            World.Missions.Set(entity, new MissionStateComponent(MissionKind.Idle, 0));
            World.Autonomy.Set(entity, new AutonomyComponent(autonomy, AutonomyCapabilities.AutoAcquire | AutonomyCapabilities.AutoKite | AutonomyCapabilities.AutoRetreat));
            return entity;
        }

        private IEnumerable<EntityId> OrderedEntities() => owners.Keys.Where(x => World.Registry.IsAlive(x)).OrderBy(x => x);
        private bool IsOwnedAlive(EntityId entity, PlayerId owner) => entity.IsValid && owners.ContainsKey(entity) && owners[entity].Equals(owner) && World.Registry.IsAlive(entity);

        private void EmitRuleBasedOpponentCommand()
        {
            if (Tick % 5 != 0 || !IsOwnedAlive(aiUnit, AiPlayer)) return;
            EntityId target = HumanUnits.OrderBy(x => Distance(x, aiUnit)).ThenBy(x => x).FirstOrDefault();
            if (!target.IsValid) return;
            var request = new CommandRequest(nextCommandId++, aiUnit, CommandSource.ComputerAI, CommandKind.AttackMove, new CommandTarget(null, target, "rule-based-opponent"), QueueMode.Replace, Tick);
            CommandQueue.Enqueue(request);
        }

        private void ProcessPendingCommands()
        {
            foreach (CommandRequest request in CommandQueue.SnapshotCanonical().Where(x => x.IssuedTick <= Tick).OrderBy(x => x))
            {
                if (!processedCommands.Add(request.CommandId) || !World.Registry.IsAlive(request.Actor)) continue;
                MissionKind mission = request.Kind == CommandKind.Move ? MissionKind.Move : request.Kind == CommandKind.Attack ? MissionKind.Attack : request.Kind == CommandKind.AttackMove ? MissionKind.AttackMove : request.Kind == CommandKind.Stop ? MissionKind.Stop : request.Kind == CommandKind.Hold ? MissionKind.Hold : request.Kind == CommandKind.Harvest ? MissionKind.Harvest : MissionKind.Guard;
                World.Missions.Set(request.Actor, new MissionStateComponent(mission, checked((int)Math.Min(request.CommandId, int.MaxValue))));
                if (request.Target.Entity.HasValue) World.Targets.Set(request.Actor, new TargetMemoryComponent(request.Target.Entity.Value, 1));
                if (request.Target.Cell.HasValue) moveDestinations[request.Actor] = request.Target.Cell.Value;
                else if (mission == MissionKind.Stop || mission == MissionKind.Hold) moveDestinations.Remove(request.Actor);
                if (request.Actor.Equals(harvester) && request.Source == CommandSource.Human)
                {
                    harvesterPlayerOverride = true;
                    if (request.Target.Cell.HasValue) harvesterCommandTarget = request.Target.Cell.Value;
                    if (mission == MissionKind.Stop || mission == MissionKind.Hold)
                        moveDestinations.Remove(request.Actor);
                }
            }
        }

        private void ProcessAutonomy()
        {
            if (Tick % 6 != 0) return;
            foreach (EntityId entity in HumanUnits)
            {
                AutonomyComponent autonomy; MissionStateComponent mission;
                if (!World.Autonomy.TryGet(entity, out autonomy) || autonomy.Mode == AutonomyMode.Manual || !World.Missions.TryGet(entity, out mission) || (mission.Kind != MissionKind.Idle && mission.Kind != MissionKind.Stop && mission.Kind != MissionKind.Hold)) continue;
                EntityId target = OrderedEntities().Where(x => owners[x].Equals(AiPlayer) && kinds[x] == HumanPlaytestEntityKind.Unit).OrderBy(x => Distance(entity, x)).ThenBy(x => x).FirstOrDefault();
                if (!target.IsValid) continue;
                var request = new CommandRequest(nextCommandId++, entity, CommandSource.ComputerAI, CommandKind.AttackMove, new CommandTarget(null, target, "autonomy"), QueueMode.Replace, Tick);
                CommandQueue.Enqueue(request);
            }
        }

        private void ProcessEconomy()
        {
            if (!IsOwnedAlive(harvester, HumanPlayer)) return;
            MissionStateComponent currentMission;
            World.Missions.TryGet(harvester, out currentMission);
            if (harvesterPlayerOverride && currentMission.Kind != MissionKind.Idle && currentMission.Kind != MissionKind.Harvest && currentMission.Kind != MissionKind.ReturnToRefinery && currentMission.Kind != MissionKind.Unload)
                return;
            PositionComponent position; World.Positions.TryGet(harvester, out position);
            if (harvesterPlayerOverride && currentMission.Kind == MissionKind.Harvest)
            {
                if (position.X != harvesterCommandTarget.X || position.Y != harvesterCommandTarget.Y)
                {
                    MoveOneStep(harvester, harvesterCommandTarget);
                    return;
                }
                if (resourceQuantity > 0 && harvesterCargo < 100)
                {
                    int amount = Math.Min(25, Math.Min(resourceQuantity, 100 - (int)harvesterCargo));
                    harvesterCargo += amount;
                    resourceQuantity -= amount;
                    moveDestinations[harvester] = refineryCell;
                    World.Missions.Set(harvester, new MissionStateComponent(MissionKind.ReturnToRefinery, checked((int)Math.Min(Tick, int.MaxValue))));
                }
                return;
            }
            if (harvesterPlayerOverride && (currentMission.Kind == MissionKind.ReturnToRefinery || currentMission.Kind == MissionKind.Unload))
            {
                if (position.X != refineryCell.X || position.Y != refineryCell.Y)
                {
                    MoveOneStep(harvester, refineryCell);
                    return;
                }
                if (harvesterCargo > 0)
                {
                    EconomyTransaction transaction;
                    if (Economy.TryApply(EconomyTransactionSource.HarvestIncome, HumanPlayer, Tick, harvesterCargo, "player-harvester-unload", out transaction))
                    {
                        harvesterCargo = 0;
                        harvestEvents++;
                        World.Missions.Set(harvester, new MissionStateComponent(MissionKind.Idle, checked((int)Math.Min(Tick, int.MaxValue))));
                    }
                }
                return;
            }
            if (harvesterCargo == 0)
            {
                if (position.X != resourceCell.X || position.Y != resourceCell.Y) MoveOneStep(harvester, resourceCell);
                else
                {
                    HarvesterCargoSnapshot cargo;
                    IReadOnlyList<ResourceEconomyDiagnostic> diagnostics;
                    if (resourceQuantity > 0 && HarvesterCargoSnapshot.TryCreate(100, new[] { new HarvesterCargoEntry(ResourceFamily.Ore, Math.Min(25, resourceQuantity), checked((int)Tick)) }, ResourceEconomyReadLimits.Default, out cargo, out diagnostics))
                    {
                        harvesterCargo = cargo.TotalQuantity;
                        resourceQuantity = Math.Max(0, resourceQuantity - (int)harvesterCargo);
                    }
                }
            }
            else if (position.X != refineryCell.X || position.Y != refineryCell.Y) MoveOneStep(harvester, refineryCell);
            else
            {
                EconomyTransaction transaction;
                if (Economy.TryApply(EconomyTransactionSource.HarvestIncome, HumanPlayer, Tick, harvesterCargo, "synthetic-refinery-settlement", out transaction)) { harvesterCargo = 0; harvestEvents++; }
            }
        }

        private void ProcessProduction()
        {
            ProductionQueueEntry entry = Production.Entries.FirstOrDefault(x => !completedProduction.Contains(x.Id.Value) && !x.IsComplete);
            if (entry.Id.Value == 0) return;
            ProductionQueueEntry updated;
            IReadOnlyList<ProductionDiagnostic> diagnostics;
            if (!Production.TryAdvance(entry.Id, 1, out updated, out diagnostics)) return;
            if (updated.IsComplete && completedProduction.Add(updated.Id.Value))
            {
                AddEntity(HumanPlayer, HumanPlaytestEntityKind.Unit, 6, 5 + spawnedUnits, 100, AutonomyMode.Manual);
                spawnedUnits++;
                productionEvents++;
            }
        }

        private void ProcessMovement()
        {
            foreach (EntityId entity in OrderedEntities().Where(x => IsMobile(x)))
            {
                MissionStateComponent mission;
                if (!World.Missions.TryGet(entity, out mission) || (mission.Kind != MissionKind.Move && mission.Kind != MissionKind.AttackMove && mission.Kind != MissionKind.Harvest && mission.Kind != MissionKind.ReturnToRefinery)) continue;
                if (entity.Equals(harvester) && harvesterPlayerOverride &&
                    (mission.Kind == MissionKind.Harvest || mission.Kind == MissionKind.ReturnToRefinery || mission.Kind == MissionKind.Unload)) continue;
                TargetMemoryComponent target;
                if (World.Targets.TryGet(entity, out target) && target.CurrentTarget.IsValid && World.Registry.IsAlive(target.CurrentTarget))
                {
                    PositionComponent targetPosition; World.Positions.TryGet(target.CurrentTarget, out targetPosition); MoveOneStep(entity, new CellCoordinate(targetPosition.X, targetPosition.Y));
                }
                else
                {
                    CellCoordinate destination;
                    if (moveDestinations.TryGetValue(entity, out destination)) MoveOneStep(entity, destination);
                }
            }
        }

        private void ProcessCombat()
        {
            if (Tick % 3 != 0) return;
            foreach (EntityId source in OrderedEntities().Where(x => kinds[x] == HumanPlaytestEntityKind.Unit))
            {
                MissionStateComponent mission; if (!World.Missions.TryGet(source, out mission) || (mission.Kind != MissionKind.Attack && mission.Kind != MissionKind.AttackMove)) continue;
                TargetMemoryComponent memory; if (!World.Targets.TryGet(source, out memory) || !memory.CurrentTarget.IsValid || !World.Registry.IsAlive(memory.CurrentTarget)) continue;
                if (!owners.ContainsKey(memory.CurrentTarget) || owners[memory.CurrentTarget].Equals(owners[source])) continue;
                if (Distance(source, memory.CurrentTarget) > 4) continue;
                HealthComponent health; if (!World.Health.TryGet(memory.CurrentTarget, out health)) continue;
                World.Health.Set(memory.CurrentTarget, new HealthComponent(Math.Max(0, health.Current - 15), health.Maximum));
                combatEvents++;
                if (health.Current - 15 <= 0) DestroyEntity(memory.CurrentTarget);
            }
        }

        private void ResolveMatch()
        {
            if (!World.Registry.IsAlive(aiBase)) { status = HumanPlaytestMatchStatus.Victory; winner = HumanPlayer; }
            else if (!World.Registry.IsAlive(humanBase)) { status = HumanPlaytestMatchStatus.Defeat; winner = AiPlayer; }
        }

        private void DestroyEntity(EntityId entity)
        {
            HumanPlaytestEntityKind kind;
            if (!kinds.TryGetValue(entity, out kind)) return;
            if (kind == HumanPlaytestEntityKind.Unit) destroyedUnits++;
            World.DestroyEntity(entity);
        }

        private int Distance(EntityId left, EntityId right)
        {
            PositionComponent a; PositionComponent b;
            if (!World.Positions.TryGet(left, out a) || !World.Positions.TryGet(right, out b)) return int.MaxValue;
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        private void MoveOneStep(EntityId entity, CellCoordinate destination)
        {
            PositionComponent position;
            if (!World.Positions.TryGet(entity, out position)) return;
            int x = position.X;
            int y = position.Y;
            if (x < destination.X) x++; else if (x > destination.X) x--; else if (y < destination.Y) y++; else if (y > destination.Y) y--;
            x = Math.Max(0, Math.Min(config.Width - 1, x));
            y = Math.Max(0, Math.Min(config.Height - 1, y));
            World.Positions.Set(entity, new PositionComponent(x, y, position.Layer));
        }

        public bool IsSelectable(EntityId entity, PlayerId perspective)
        {
            if (!owners.ContainsKey(entity) || !World.Registry.IsAlive(entity)) return false;
            HumanPlaytestEntityCapabilities capabilities = CapabilitiesFor(kinds[entity], owners[entity]);
            return (capabilities & HumanPlaytestEntityCapabilities.Selectable) != 0 && (owners[entity].Equals(perspective) || (capabilities & HumanPlaytestEntityCapabilities.Enemy) != 0);
        }

        public bool IsControllable(EntityId entity, PlayerId perspective)
        {
            return owners.ContainsKey(entity) && World.Registry.IsAlive(entity) && owners[entity].Equals(perspective) && (CapabilitiesFor(kinds[entity], owners[entity]) & HumanPlaytestEntityCapabilities.Controllable) != 0;
        }

        public bool IsMobile(EntityId entity)
        {
            return owners.ContainsKey(entity) && (CapabilitiesFor(kinds[entity], owners[entity]) & HumanPlaytestEntityCapabilities.Mobile) != 0;
        }

        public bool IsResourceCell(CellCoordinate cell) => cell.Equals(resourceCell) && resourceQuantity > 0;

        private static HumanPlaytestEntityCapabilities CapabilitiesFor(HumanPlaytestEntityKind kind, PlayerId owner)
        {
            bool enemy = owner.Value != 0;
            HumanPlaytestEntityCapabilities result = HumanPlaytestEntityCapabilities.Selectable;
            if (enemy)
            {
                result |= HumanPlaytestEntityCapabilities.Enemy;
                if (kind == HumanPlaytestEntityKind.Unit || kind == HumanPlaytestEntityKind.Harvester) result |= HumanPlaytestEntityCapabilities.Mobile;
                else result |= HumanPlaytestEntityCapabilities.Structure;
                return result;
            }
            if (kind == HumanPlaytestEntityKind.Unit || kind == HumanPlaytestEntityKind.Harvester)
                result |= HumanPlaytestEntityCapabilities.Controllable | HumanPlaytestEntityCapabilities.Mobile;
            else result |= HumanPlaytestEntityCapabilities.Structure;
            return result;
        }

    }
}
