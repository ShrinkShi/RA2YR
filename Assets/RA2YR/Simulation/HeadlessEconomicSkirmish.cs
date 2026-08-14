using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Simulation
{
    public enum HeadlessSkirmishDiagnosticCode { InvalidConfiguration, CommandRejected, ArithmeticOverflow, NoProgress }
    public enum HeadlessSkirmishCompletionStatus { Running, Completed, Failed }
    public enum HeadlessPlayerState { Alive, Defeated }

    public readonly struct HeadlessSkirmishConfig
    {
        public HeadlessSkirmishConfig(int seed, int maxTicks, long harvestYield, long powerCost, long factoryCost, long productionCost, int structureHealth, bool computerAgents, EconomicAgentStrategyProfile playerOneStrategy, EconomicAgentStrategyProfile playerTwoStrategy)
        {
            if (maxTicks <= 0 || harvestYield < 0 || powerCost < 0 || factoryCost < 0 || productionCost < 0 || structureHealth <= 0) throw new ArgumentOutOfRangeException();
            if (!Enum.IsDefined(typeof(EconomicAgentStrategyProfile), playerOneStrategy) || !Enum.IsDefined(typeof(EconomicAgentStrategyProfile), playerTwoStrategy)) throw new ArgumentOutOfRangeException();
            Seed = seed;
            MaxTicks = maxTicks;
            HarvestYield = harvestYield;
            PowerCost = powerCost;
            FactoryCost = factoryCost;
            ProductionCost = productionCost;
            StructureHealth = structureHealth;
            ComputerAgents = computerAgents;
            PlayerOneStrategy = playerOneStrategy;
            PlayerTwoStrategy = playerTwoStrategy;
        }

        public int Seed { get; }
        public int MaxTicks { get; }
        public long HarvestYield { get; }
        public long PowerCost { get; }
        public long FactoryCost { get; }
        public long ProductionCost { get; }
        public int StructureHealth { get; }
        public bool ComputerAgents { get; }
        public EconomicAgentStrategyProfile PlayerOneStrategy { get; }
        public EconomicAgentStrategyProfile PlayerTwoStrategy { get; }
        public static HeadlessSkirmishConfig Default => new HeadlessSkirmishConfig(7, 32, 40, 10, 10, 20, 30, true, EconomicAgentStrategyProfile.Rush, EconomicAgentStrategyProfile.Turtle);
    }

    public readonly struct HeadlessPlayerSnapshot : IComparable<HeadlessPlayerSnapshot>
    {
        internal HeadlessPlayerSnapshot(PlayerId player, long credits, long collectedResource, bool hasPower, bool hasFactory, int units, int structureHealth, HeadlessPlayerState state, EconomicAgentStrategyProfile strategy)
        {
            Player = player;
            Credits = credits;
            CollectedResource = collectedResource;
            HasPower = hasPower;
            HasFactory = hasFactory;
            Units = units;
            StructureHealth = structureHealth;
            State = state;
            Strategy = strategy;
        }

        public PlayerId Player { get; }
        public long Credits { get; }
        public long CollectedResource { get; }
        public bool HasPower { get; }
        public bool HasFactory { get; }
        public int Units { get; }
        public int StructureHealth { get; }
        public HeadlessPlayerState State { get; }
        public EconomicAgentStrategyProfile Strategy { get; }
        public int CompareTo(HeadlessPlayerSnapshot other) => Player.CompareTo(other.Player);
    }

    public sealed class HeadlessSkirmishResult
    {
        internal HeadlessSkirmishResult(HeadlessSkirmishCompletionStatus status, int tick, PlayerId? winner, IEnumerable<HeadlessPlayerSnapshot> players, IEnumerable<CommandRequest> commands, int harvestEvents, int incomeEvents, int powerBuilds, int factoryBuilds, int productionEvents, int rallyEvents, int attackEvents, int combatEvents, int destroyedStructures, int manualCommands, IEnumerable<HeadlessSkirmishDiagnostic> diagnostics, string hash)
        {
            CompletionStatus = status;
            Tick = tick;
            Winner = winner;
            Players = new ReadOnlyCollection<HeadlessPlayerSnapshot>((players ?? Enumerable.Empty<HeadlessPlayerSnapshot>()).OrderBy(x => x).ToList());
            Commands = new ReadOnlyCollection<CommandRequest>((commands ?? Enumerable.Empty<CommandRequest>()).OrderBy(x => x).ToList());
            HarvestEvents = harvestEvents;
            IncomeEvents = incomeEvents;
            PowerBuilds = powerBuilds;
            FactoryBuilds = factoryBuilds;
            ProductionEvents = productionEvents;
            RallyEvents = rallyEvents;
            AttackEvents = attackEvents;
            CombatEvents = combatEvents;
            DestroyedStructures = destroyedStructures;
            ManualCommands = manualCommands;
            Diagnostics = new ReadOnlyCollection<HeadlessSkirmishDiagnostic>((diagnostics ?? Enumerable.Empty<HeadlessSkirmishDiagnostic>()).ToList());
            StateHash = hash ?? string.Empty;
        }

        public HeadlessSkirmishCompletionStatus CompletionStatus { get; }
        public bool MatchComplete => CompletionStatus == HeadlessSkirmishCompletionStatus.Completed;
        public bool IsSuccess => CompletionStatus == HeadlessSkirmishCompletionStatus.Completed && Diagnostics.Count == 0;
        public int Tick { get; }
        public PlayerId? Winner { get; }
        public IReadOnlyList<HeadlessPlayerSnapshot> Players { get; }
        public IReadOnlyList<CommandRequest> Commands { get; }
        public int HarvestEvents { get; }
        public int IncomeEvents { get; }
        public int PowerBuilds { get; }
        public int FactoryBuilds { get; }
        public int ProductionEvents { get; }
        public int RallyEvents { get; }
        public int AttackEvents { get; }
        public int CombatEvents { get; }
        public int DestroyedStructures { get; }
        public int ManualCommands { get; }
        public IReadOnlyList<HeadlessSkirmishDiagnostic> Diagnostics { get; }
        public string StateHash { get; }
    }

    public sealed class HeadlessSkirmishDiagnostic
    {
        public HeadlessSkirmishDiagnostic(HeadlessSkirmishDiagnosticCode code, int tick, string stage, string message) { Code = code; Tick = tick; Stage = stage ?? string.Empty; Message = message ?? string.Empty; }
        public HeadlessSkirmishDiagnosticCode Code { get; }
        public int Tick { get; }
        public string Stage { get; }
        public string Message { get; }
    }

    public sealed class HeadlessEconomicSkirmish
    {
        private sealed class PlayerState
        {
            public PlayerState(PlayerId player, EconomicAgentStrategyProfile strategy, int structureHealth) { Player = player; Strategy = strategy; StructureHealth = structureHealth; UnitEntity = new EntityId(100 + player.Value, 1); }
            public PlayerId Player;
            public EconomicAgentStrategyProfile Strategy;
            public EntityId UnitEntity;
            public long Credits;
            public long CollectedResource;
            public bool HasHarvested;
            public bool HasPower;
            public bool HasFactory;
            public int Units;
            public int StructureHealth;
            public HeadlessPlayerState State => StructureHealth > 0 ? HeadlessPlayerState.Alive : HeadlessPlayerState.Defeated;
        }

        private readonly HeadlessSkirmishConfig config;
        private readonly EconomyAuthority economy = new EconomyAuthority();
        private readonly PlayerState[] players;
        private readonly List<CommandRequest> commands = new List<CommandRequest>();
        private readonly List<HeadlessSkirmishDiagnostic> diagnostics = new List<HeadlessSkirmishDiagnostic>();
        private int tick;
        private int harvestEvents;
        private int incomeEvents;
        private int powerBuilds;
        private int factoryBuilds;
        private int productionEvents;
        private int rallyEvents;
        private int attackEvents;
        private int combatEvents;
        private int destroyedStructures;
        private int manualCommands;
        private long nextCommandId;
        private bool complete;
        private PlayerId? winner;

        public HeadlessEconomicSkirmish(HeadlessSkirmishConfig config)
        {
            this.config = config;
            players = new[]
            {
                new PlayerState(new PlayerId(0), config.PlayerOneStrategy, config.StructureHealth),
                new PlayerState(new PlayerId(1), config.PlayerTwoStrategy, config.StructureHealth)
            };
            economy.Register(players[0].Player, 0);
            economy.Register(players[1].Player, 0);
        }

        public int Tick => tick;
        public bool MatchComplete => complete;
        public IReadOnlyList<CommandRequest> CommandStream => new ReadOnlyCollection<CommandRequest>(commands.OrderBy(x => x).ToList());
        public void QueueCommand(CommandRequest command) { commands.Add(command); }

        public HeadlessSkirmishResult Step()
        {
            if (!complete)
            {
                if (tick >= config.MaxTicks) { diagnostics.Add(new HeadlessSkirmishDiagnostic(HeadlessSkirmishDiagnosticCode.NoProgress, tick, "match", "tick budget exhausted before terminal state")); complete = true; }
                else
                {
                    tick = checked(tick + 1);
                    if (config.ComputerAgents) StepComputerAgents();
                    StepManualCommands();
                    ResolveCombat();
                    ResolveTerminalState();
                }
            }
            return Snapshot();
        }

        public HeadlessSkirmishResult RunToCompletion()
        {
            while (!complete) Step();
            return Snapshot();
        }

        public string StateHash => SnapshotHash();

        private void StepComputerAgents()
        {
            foreach (var state in players)
            {
                if (state.State == HeadlessPlayerState.Defeated) continue;
                if (!state.HasHarvested)
                {
                    state.HasHarvested = true;
                    state.CollectedResource = checked(state.CollectedResource + config.HarvestYield);
                    ApplyIncome(state, config.HarvestYield);
                    harvestEvents++;
                    incomeEvents++;
                }
                if (!state.HasPower && state.Credits >= config.PowerCost)
                {
                    Spend(state, config.PowerCost, EconomyTransactionSource.ProductionSpend);
                    state.HasPower = true;
                    powerBuilds++;
                }
                if (state.HasPower && !state.HasFactory && state.Credits >= config.FactoryCost)
                {
                    Spend(state, config.FactoryCost, EconomyTransactionSource.ProductionSpend);
                    state.HasFactory = true;
                    factoryBuilds++;
                }
                if (state.HasFactory && state.Units == 0 && state.Credits >= config.ProductionCost)
                {
                    Spend(state, config.ProductionCost, EconomyTransactionSource.ProductionSpend);
                    state.Units = 1;
                    productionEvents++;
                }
                if (state.Units > 0)
                {
                    rallyEvents++;
                    AppendComputerCommand(state);
                    attackEvents++;
                }
            }
        }

        private void StepManualCommands()
        {
            foreach (var command in commands.Where(x => x.IssuedTick == tick && x.Source != CommandSource.ComputerAI).OrderBy(x => x))
            {
                manualCommands++;
                if (command.Kind == CommandKind.Attack || command.Kind == CommandKind.AttackMove) attackEvents++;
            }
        }

        private void AppendComputerCommand(PlayerState state)
        {
            var target = state.Player.Value == 0 ? new PlayerId(1) : new PlayerId(0);
            commands.Add(new CommandRequest(nextCommandId++, state.UnitEntity, CommandSource.ComputerAI, CommandKind.Attack, new CommandTarget(new CellCoordinate(target.Value, 0), null, "enemy-base"), QueueMode.Replace, tick));
        }

        private void ResolveCombat()
        {
            var damage = new int[players.Length];
            foreach (var state in players)
            {
                if (state.State == HeadlessPlayerState.Defeated || state.Units <= 0) continue;
                var other = players[state.Player.Value == 0 ? 1 : 0];
                var amount = state.Strategy == EconomicAgentStrategyProfile.Rush || state.Strategy == EconomicAgentStrategyProfile.AllIn ? 12 : 8;
                damage[other.Player.Value] = checked(damage[other.Player.Value] + amount);
            }
            for (var i = 0; i < players.Length; i++)
            {
                if (damage[i] == 0) continue;
                players[i].StructureHealth = Math.Max(0, players[i].StructureHealth - damage[i]);
                combatEvents++;
                if (players[i].StructureHealth == 0) destroyedStructures++;
            }
        }

        private void ResolveTerminalState()
        {
            var alive = players.Where(x => x.State == HeadlessPlayerState.Alive).ToList();
            if (alive.Count == 1)
            {
                winner = alive[0].Player;
                complete = true;
            }
            else if (alive.Count == 0)
            {
                complete = true;
            }
        }

        private void ApplyIncome(PlayerState state, long amount)
        {
            if (amount == 0) return;
            if (!economy.TryApply(EconomyTransactionSource.HarvestIncome, state.Player, tick, amount, "synthetic-harvest", out _)) diagnostics.Add(new HeadlessSkirmishDiagnostic(HeadlessSkirmishDiagnosticCode.ArithmeticOverflow, tick, "income", "income transaction was rejected"));
            state.Credits = economy.Get(state.Player).Balance;
        }

        private void Spend(PlayerState state, long amount, EconomyTransactionSource source)
        {
            if (amount == 0) return;
            if (!economy.TryApply(source, state.Player, tick, -amount, "synthetic-build", out _)) diagnostics.Add(new HeadlessSkirmishDiagnostic(HeadlessSkirmishDiagnosticCode.ArithmeticOverflow, tick, "spend", "spend transaction was rejected"));
            state.Credits = economy.Get(state.Player).Balance;
        }

        private HeadlessSkirmishResult Snapshot()
        {
            return new HeadlessSkirmishResult(complete ? (diagnostics.Count == 0 ? HeadlessSkirmishCompletionStatus.Completed : HeadlessSkirmishCompletionStatus.Failed) : HeadlessSkirmishCompletionStatus.Running, tick, winner, players.Select(x => new HeadlessPlayerSnapshot(x.Player, economy.Get(x.Player).Balance, x.CollectedResource, x.HasPower, x.HasFactory, x.Units, x.StructureHealth, x.State, x.Strategy)), commands, harvestEvents, incomeEvents, powerBuilds, factoryBuilds, productionEvents, rallyEvents, attackEvents, combatEvents, destroyedStructures, manualCommands, diagnostics, SnapshotHash());
        }

        private string SnapshotHash()
        {
            var state = string.Join("|", new[] { config.Seed.ToString(), tick.ToString(), (winner.HasValue ? winner.Value.Value.ToString() : "-"), string.Join(";", players.Select(x => x.Player.Value + ":" + economy.Get(x.Player).Balance + ":" + x.CollectedResource + ":" + x.HasPower + ":" + x.HasFactory + ":" + x.Units + ":" + x.StructureHealth)), string.Join(";", commands.OrderBy(x => x).Select(x => x.CommandId + ":" + x.Actor.Index + ":" + x.Source + ":" + x.Kind + ":" + x.IssuedTick)) });
            using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(state)).Select(x => x.ToString("x2")));
        }
    }
}
