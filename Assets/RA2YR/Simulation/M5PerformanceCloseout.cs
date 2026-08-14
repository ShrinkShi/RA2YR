using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Simulation
{
    public enum M5StressWorkload
    {
        Harvesting,
        Production,
        Combat,
        Occupancy,
        Targeting,
        Autonomy,
        Mixed
    }

    public enum M5StressDiagnosticCode
    {
        InvalidConfiguration,
        OperationBudgetExceeded,
        ArithmeticOverflow,
        NoProgress
    }

    public enum M5StressCompletionStatus
    {
        Completed,
        Failed
    }

    public sealed class M5StressDiagnostic
    {
        public M5StressDiagnostic(M5StressDiagnosticCode code, int tick, string stage, string message)
        {
            Code = code;
            Tick = tick;
            Stage = stage ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public M5StressDiagnosticCode Code { get; }
        public int Tick { get; }
        public string Stage { get; }
        public string Message { get; }
    }

    public readonly struct M5StressExecution
    {
        public M5StressExecution(M5StressCompletionStatus status, bool hasFatalError, int suppressedDiagnosticCount)
        {
            CompletionStatus = status;
            HasFatalError = hasFatalError;
            SuppressedDiagnosticCount = suppressedDiagnosticCount;
        }

        public M5StressCompletionStatus CompletionStatus { get; }
        public bool HasFatalError { get; }
        public int SuppressedDiagnosticCount { get; }
        public bool IsSuccess => CompletionStatus == M5StressCompletionStatus.Completed && !HasFatalError;
    }

    public readonly struct M5StressConfig
    {
        public M5StressConfig(int entityCount, int ticks, M5StressWorkload workload, long maxOperations = 0, int seed = 17, int maxDiagnostics = 32)
        {
            if (entityCount <= 0 || entityCount > 100000 || ticks <= 0 || ticks > 100000 || !Enum.IsDefined(typeof(M5StressWorkload), workload) || maxOperations < 0 || maxDiagnostics < 0)
                throw new ArgumentOutOfRangeException();
            EntityCount = entityCount;
            Ticks = ticks;
            Workload = workload;
            Seed = seed;
            MaxOperations = maxOperations == 0 ? checked((long)entityCount * ticks * 64L) : maxOperations;
            MaxDiagnostics = maxDiagnostics;
        }

        public int EntityCount { get; }
        public int Ticks { get; }
        public M5StressWorkload Workload { get; }
        public long Seed { get; }
        public long MaxOperations { get; }
        public int MaxDiagnostics { get; }
    }

    public readonly struct M5StressAggregateSnapshot
    {
        internal M5StressAggregateSnapshot(long credits, long cargo, long resources, long queueProgress, long power, long structureHealth, long ownerZero, long ownerOne)
        {
            Credits = credits;
            Cargo = cargo;
            Resources = resources;
            QueueProgress = queueProgress;
            Power = power;
            StructureHealth = structureHealth;
            OwnerZero = ownerZero;
            OwnerOne = ownerOne;
        }

        public long Credits { get; }
        public long Cargo { get; }
        public long Resources { get; }
        public long QueueProgress { get; }
        public long Power { get; }
        public long StructureHealth { get; }
        public long OwnerZero { get; }
        public long OwnerOne { get; }
    }

    public sealed class M5StressResult
    {
        internal M5StressResult(M5StressExecution execution, int ticksCompleted, long operations, int peakProposals, int descriptorParseCount, long factoryLookupCount, long prerequisiteEvaluationCount, long occupancyQueryCount, long targetingCandidateCount, long autonomyProposalCount, M5StressAggregateSnapshot aggregate, IEnumerable<M5StressDiagnostic> diagnostics, string stateHash)
        {
            Execution = execution;
            TicksCompleted = ticksCompleted;
            OperationCount = operations;
            PeakProposalCount = peakProposals;
            DescriptorParseCount = descriptorParseCount;
            FactoryLookupCount = factoryLookupCount;
            PrerequisiteEvaluationCount = prerequisiteEvaluationCount;
            OccupancyQueryCount = occupancyQueryCount;
            TargetingCandidateCount = targetingCandidateCount;
            AutonomyProposalCount = autonomyProposalCount;
            Aggregate = aggregate;
            Diagnostics = new ReadOnlyCollection<M5StressDiagnostic>((diagnostics ?? Enumerable.Empty<M5StressDiagnostic>()).ToList());
            StateHash = stateHash ?? string.Empty;
        }

        public M5StressExecution Execution { get; }
        public bool IsSuccess => Execution.IsSuccess;
        public int TicksCompleted { get; }
        public long OperationCount { get; }
        public int PeakProposalCount { get; }
        public int DescriptorParseCount { get; }
        public long FactoryLookupCount { get; }
        public long PrerequisiteEvaluationCount { get; }
        public long OccupancyQueryCount { get; }
        public long TargetingCandidateCount { get; }
        public long AutonomyProposalCount { get; }
        public M5StressAggregateSnapshot Aggregate { get; }
        public IReadOnlyList<M5StressDiagnostic> Diagnostics { get; }
        public string StateHash { get; }
    }

    public sealed class M5PerformanceCloseoutHarness
    {
        private enum ProposalKind { Harvest, Produce, Attack, Move, Autonomy }

        private readonly struct Proposal : IComparable<Proposal>
        {
            public Proposal(int source, int target, ProposalKind kind, long amount)
            {
                Source = source;
                Target = target;
                Kind = kind;
                Amount = amount;
            }

            public int Source { get; }
            public int Target { get; }
            public ProposalKind Kind { get; }
            public long Amount { get; }
            public int CompareTo(Proposal other)
            {
                var c = Target.CompareTo(other.Target);
                if (c != 0) return c;
                c = Source.CompareTo(other.Source);
                return c != 0 ? c : Kind.CompareTo(other.Kind);
            }
        }

        private struct EntityState
        {
            public EntityId Entity;
            public PlayerId Owner;
            public int X;
            public int Y;
            public long Cargo;
            public long Resources;
            public long QueueProgress;
            public long Power;
            public long StructureHealth;
            public bool DescriptorParsed;
            public int Target;
        }

        private readonly M5StressConfig config;
        private readonly EconomyAuthority economy = new EconomyAuthority();
        private readonly EntityState[] entities;
        private readonly DeterministicSpatialIndex occupancy;
        private readonly List<Proposal> proposals;
        private readonly List<M5StressDiagnostic> diagnostics = new List<M5StressDiagnostic>();
        private readonly long[] damage;
        private long operations;
        private int ticksCompleted;
        private int peakProposals;
        private int descriptorParseCount;
        private long factoryLookupCount;
        private long prerequisiteEvaluationCount;
        private long occupancyQueryCount;
        private long targetingCandidateCount;
        private long autonomyProposalCount;
        private bool failed;
        private int suppressedDiagnostics;

        public M5PerformanceCloseoutHarness(M5StressConfig config)
        {
            this.config = config;
            entities = new EntityState[config.EntityCount];
            damage = new long[config.EntityCount];
            occupancy = new DeterministicSpatialIndex(checked(config.EntityCount * 4));
            proposals = new List<Proposal>(config.EntityCount);
            for (var i = 0; i < entities.Length; i++)
            {
                var owner = new PlayerId(i % 2);
                entities[i] = new EntityState
                {
                    Entity = new EntityId(i, 1),
                    Owner = owner,
                    X = i % 256,
                    Y = i / 256,
                    StructureHealth = 100,
                    Target = (i + 1) % entities.Length
                };
                economy.Register(new PlayerId(i), 0);
                occupancy.Insert(entities[i].Entity, new CellCoordinate(entities[i].X, entities[i].Y));
            }
        }

        public M5StressResult Run()
        {
            if (config.EntityCount <= 0 || config.Ticks <= 0)
            {
                AddDiagnostic(M5StressDiagnosticCode.InvalidConfiguration, 0, "config", "stress dimensions must be positive");
                failed = true;
                return Snapshot();
            }

            for (var tick = 0; tick < config.Ticks && !failed; tick++)
            {
                proposals.Clear();
                Array.Clear(damage, 0, damage.Length);
                ObserveAndPropose(tick);
                if (failed) break;
                proposals.Sort();
                peakProposals = Math.Max(peakProposals, proposals.Count);
                Commit(tick);
                ticksCompleted = tick + 1;
            }

            if (!failed && ticksCompleted != config.Ticks)
            {
                AddDiagnostic(M5StressDiagnosticCode.NoProgress, ticksCompleted, "stress", "stress loop made no progress");
                failed = true;
            }
            return Snapshot();
        }

        private void ObserveAndPropose(int tick)
        {
            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (Includes(M5StressWorkload.Harvesting)) AddProposal(i, i, ProposalKind.Harvest, 1, tick);
                if (Includes(M5StressWorkload.Production))
                {
                    if (!entity.DescriptorParsed)
                    {
                        entity.DescriptorParsed = true;
                        descriptorParseCount++;
                    }
                    factoryLookupCount++;
                    prerequisiteEvaluationCount++;
                    AddProposal(i, i, ProposalKind.Produce, 1, tick);
                }
                if (Includes(M5StressWorkload.Combat))
                {
                    var target = (i + tick + 1) % entities.Length;
                    entity.Target = target;
                    AddProposal(i, target, ProposalKind.Attack, 1, tick);
                }
                if (Includes(M5StressWorkload.Occupancy))
                {
                    var nextX = (entity.X + 1) % 256;
                    var nextY = entity.Y;
                    occupancyQueryCount++;
                    occupancy.QueryNeighbors(new CellCoordinate(entity.X, entity.Y), 1);
                    AddProposal(i, i, ProposalKind.Move, nextX + (nextY * 256L), tick);
                }
                if (Includes(M5StressWorkload.Targeting))
                {
                    entity.Target = (i + 1) % entities.Length;
                    targetingCandidateCount++;
                }
                if (Includes(M5StressWorkload.Autonomy))
                {
                    autonomyProposalCount++;
                    AddProposal(i, i, ProposalKind.Autonomy, 1, tick);
                }
                entities[i] = entity;
            }
        }

        private void Commit(int tick)
        {
            for (var i = 0; i < proposals.Count; i++)
            {
                var proposal = proposals[i];
                if (proposal.Kind == ProposalKind.Harvest)
                {
                    if (!economy.TryApply(EconomyTransactionSource.HarvestIncome, new PlayerId(proposal.Source), tick, proposal.Amount, "m5-c7-harvest", out _))
                    {
                        AddDiagnostic(M5StressDiagnosticCode.ArithmeticOverflow, tick, "harvest", "authoritative economy rejected harvest");
                        failed = true;
                        return;
                    }
                    entities[proposal.Source].Cargo = checked(entities[proposal.Source].Cargo + proposal.Amount);
                    entities[proposal.Source].Resources = checked(entities[proposal.Source].Resources + proposal.Amount);
                }
                else if (proposal.Kind == ProposalKind.Produce)
                {
                    entities[proposal.Source].QueueProgress = checked(entities[proposal.Source].QueueProgress + proposal.Amount);
                }
                else if (proposal.Kind == ProposalKind.Attack)
                {
                    damage[proposal.Target] = checked(damage[proposal.Target] + proposal.Amount);
                }
                else if (proposal.Kind == ProposalKind.Move)
                {
                    var old = entities[proposal.Source];
                    var nextX = (int)(proposal.Amount % 256L);
                    if (occupancy.Move(old.Entity, new CellCoordinate(old.X, old.Y), new CellCoordinate(nextX, old.Y)))
                    {
                        old.X = nextX;
                        entities[proposal.Source] = old;
                    }
                }
                else
                {
                    entities[proposal.Source].Power = checked(entities[proposal.Source].Power + proposal.Amount);
                }
            }
            for (var i = 0; i < entities.Length; i++)
            {
                if (damage[i] == 0) continue;
                entities[i].StructureHealth = Math.Max(0, entities[i].StructureHealth - damage[i]);
            }
        }

        private void AddProposal(int source, int target, ProposalKind kind, long amount, int tick)
        {
            if (operations >= config.MaxOperations)
            {
                AddDiagnostic(M5StressDiagnosticCode.OperationBudgetExceeded, tick, "proposal", "stress operation budget exceeded");
                failed = true;
                return;
            }
            operations++;
            proposals.Add(new Proposal(source, target, kind, amount));
        }

        private bool Includes(M5StressWorkload workload) => config.Workload == M5StressWorkload.Mixed || config.Workload == workload;

        private void AddDiagnostic(M5StressDiagnosticCode code, int tick, string stage, string message)
        {
            if (diagnostics.Count < config.MaxDiagnostics) diagnostics.Add(new M5StressDiagnostic(code, tick, stage, message));
            else if (suppressedDiagnostics < int.MaxValue) suppressedDiagnostics++;
        }

        private M5StressResult Snapshot()
        {
            long credits = 0;
            long cargo = 0;
            long resources = 0;
            long queue = 0;
            long power = 0;
            long health = 0;
            long ownerZero = 0;
            long ownerOne = 0;
            for (var i = 0; i < entities.Length; i++)
            {
                credits = checked(credits + economy.Get(new PlayerId(i)).Balance);
                cargo = checked(cargo + entities[i].Cargo);
                resources = checked(resources + entities[i].Resources);
                queue = checked(queue + entities[i].QueueProgress);
                power = checked(power + entities[i].Power);
                health = checked(health + entities[i].StructureHealth);
                if (entities[i].Owner.Value == 0) ownerZero++;
                else ownerOne++;
            }
            var aggregate = new M5StressAggregateSnapshot(credits, cargo, resources, queue, power, health, ownerZero, ownerOne);
            var execution = new M5StressExecution(failed ? M5StressCompletionStatus.Failed : M5StressCompletionStatus.Completed, failed, suppressedDiagnostics);
            return new M5StressResult(execution, ticksCompleted, operations, peakProposals, descriptorParseCount, factoryLookupCount, prerequisiteEvaluationCount, occupancyQueryCount, targetingCandidateCount, autonomyProposalCount, aggregate, diagnostics, Hash(aggregate));
        }

        private string Hash(M5StressAggregateSnapshot aggregate)
        {
            var text = new StringBuilder();
            text.Append(config.Seed).Append('|').Append(config.Workload).Append('|').Append(ticksCompleted).Append('|');
            for (var i = 0; i < entities.Length; i++)
            {
                var state = entities[i];
                text.Append(i).Append(':').Append(economy.Get(new PlayerId(i)).Balance).Append(':').Append(state.Cargo).Append(':').Append(state.Resources).Append(':').Append(state.QueueProgress).Append(':').Append(state.Power).Append(':').Append(state.StructureHealth).Append(':').Append(state.Owner.Value).Append('|');
            }
            text.Append(aggregate.Credits).Append(':').Append(aggregate.Cargo).Append(':').Append(aggregate.Resources).Append(':').Append(aggregate.QueueProgress).Append(':').Append(aggregate.Power).Append(':').Append(aggregate.StructureHealth);
            using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())).Select(x => x.ToString("x2")));
        }
    }
}
