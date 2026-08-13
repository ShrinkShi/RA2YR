using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Simulation
{
    public enum EconomicAgentDiagnosticCode
    {
        InvalidPolicy,
        NullObservation,
        ChildExecutionFailure,
        InvalidObservation,
        InsufficientCredits,
        ProductionUnavailable,
        ResourceUnavailable,
        PowerDeficit,
        ActionBudgetExceeded,
        ArithmeticOverflow,
        NoProgress
    }

    public enum EconomicAgentSeverity { Warning, Error }
    public enum EconomicAgentCompletionStatus { Succeeded, Failed }
    public enum EconomicAgentPolicy { ConservativeDeterministic }
    public enum EconomicAgentStrategyProfile { AllIn, Rush, Pressure, Balanced, Macro, Turtle }
    public enum EconomicAgentIntent { Harvest, Build, Produce, ExpandCandidate, Repair, Sell, DefendEconomy }
    public enum EconomicAgentActionKind
    {
        HarvestCandidate,
        QueueProductionCandidate,
        RepairCandidate,
        SellCandidate,
        CaptureCandidate,
        DeployCandidate
    }

    public sealed class EconomicAgentDiagnostic
    {
        public EconomicAgentDiagnostic(EconomicAgentDiagnosticCode code, EconomicAgentSeverity severity, long sourceOrdinal, string stage, string message)
        {
            Code = code;
            Severity = severity;
            SourceOrdinal = sourceOrdinal;
            Stage = stage ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public EconomicAgentDiagnosticCode Code { get; }
        public EconomicAgentSeverity Severity { get; }
        public long SourceOrdinal { get; }
        public string Stage { get; }
        public string Message { get; }
    }

    public readonly struct EconomicAgentExecution
    {
        public EconomicAgentExecution(EconomicAgentCompletionStatus status, bool hasFatalError, EconomicAgentSeverity highestSeverity, int suppressedDiagnosticCount)
        {
            CompletionStatus = status;
            HasFatalError = hasFatalError;
            HighestSeverity = highestSeverity;
            SuppressedDiagnosticCount = suppressedDiagnosticCount;
        }

        public EconomicAgentCompletionStatus CompletionStatus { get; }
        public bool HasFatalError { get; }
        public EconomicAgentSeverity HighestSeverity { get; }
        public int SuppressedDiagnosticCount { get; }
        public bool IsSuccess => CompletionStatus == EconomicAgentCompletionStatus.Succeeded && !HasFatalError;
    }

    public readonly struct EconomicAgentReadLimits
    {
        public EconomicAgentReadLimits(int maxActions, int maxDiagnostics)
        {
            if (maxActions < 0 || maxDiagnostics < 0) throw new ArgumentOutOfRangeException();
            MaxActions = maxActions;
            MaxDiagnostics = maxDiagnostics;
        }

        public int MaxActions { get; }
        public int MaxDiagnostics { get; }
        public static EconomicAgentReadLimits Default => new EconomicAgentReadLimits(8, 64);
    }

    public readonly struct EconomicAgentActionProposal : IComparable<EconomicAgentActionProposal>
    {
        public EconomicAgentActionProposal(EconomicAgentActionKind kind, PlayerId owner, long sourceOrdinal, string targetRaw, long cost, int priority, string policy, EconomicAgentIntent intent = EconomicAgentIntent.DefendEconomy)
        {
            Kind = kind;
            Owner = owner;
            SourceOrdinal = sourceOrdinal;
            TargetRaw = targetRaw ?? string.Empty;
            Cost = cost;
            Priority = priority;
            Policy = policy ?? string.Empty;
            Intent = intent;
        }

        public EconomicAgentActionKind Kind { get; }
        public PlayerId Owner { get; }
        public long SourceOrdinal { get; }
        public string TargetRaw { get; }
        public long Cost { get; }
        public int Priority { get; }
        public string Policy { get; }
        public EconomicAgentIntent Intent { get; }

        public int CompareTo(EconomicAgentActionProposal other)
        {
            var c = Priority.CompareTo(other.Priority);
            if (c != 0) return c;
            c = SourceOrdinal.CompareTo(other.SourceOrdinal);
            if (c != 0) return c;
            c = Kind.CompareTo(other.Kind);
            if (c != 0) return c;
            return string.CompareOrdinal(TargetRaw, other.TargetRaw);
        }
    }

    public sealed class EconomicAgentObservation
    {
        public EconomicAgentObservation(
            long tick,
            CreditAccount credits,
            ResourceEconomyConsistencyAnalysis resources,
            HarvesterCargoSnapshot cargo,
            ProductionDefinitionDescriptor productionDefinition,
            ProductionAvailabilityResult production,
            StructurePowerProjection power,
            StructureInteractionCandidate interaction,
            IEnumerable<string> ownFactories = null,
            IEnumerable<string> ownProductionQueues = null,
            IEnumerable<string> knownRefineries = null,
            IEnumerable<string> knownExpansions = null,
            IEnumerable<string> ownArmyComposition = null,
            IEnumerable<string> visibleEnemyComposition = null,
            int ownTech = 0)
        {
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
            Tick = tick;
            Credits = credits;
            Resources = resources;
            Cargo = cargo;
            ProductionDefinition = productionDefinition;
            Production = production;
            Power = power;
            Interaction = interaction;
            OwnFactories = Freeze(ownFactories);
            OwnProductionQueues = Freeze(ownProductionQueues);
            KnownRefineries = Freeze(knownRefineries);
            KnownExpansions = Freeze(knownExpansions);
            OwnArmyComposition = Freeze(ownArmyComposition);
            VisibleEnemyComposition = Freeze(visibleEnemyComposition);
            OwnTech = ownTech;
        }

        public long Tick { get; }
        public CreditAccount Credits { get; }
        public ResourceEconomyConsistencyAnalysis Resources { get; }
        public HarvesterCargoSnapshot Cargo { get; }
        public ProductionDefinitionDescriptor ProductionDefinition { get; }
        public ProductionAvailabilityResult Production { get; }
        public StructurePowerProjection Power { get; }
        public StructureInteractionCandidate Interaction { get; }
        public IReadOnlyList<string> OwnFactories { get; }
        public IReadOnlyList<string> OwnProductionQueues { get; }
        public IReadOnlyList<string> KnownRefineries { get; }
        public IReadOnlyList<string> KnownExpansions { get; }
        public IReadOnlyList<string> OwnArmyComposition { get; }
        public IReadOnlyList<string> VisibleEnemyComposition { get; }
        public int OwnTech { get; }
        public long OwnPower => Power.Produced - Power.Consumed;

        private static IReadOnlyList<string> Freeze(IEnumerable<string> values)
        {
            return new ReadOnlyCollection<string>((values ?? Enumerable.Empty<string>()).Select(x => x ?? string.Empty).ToList());
        }
    }

    public sealed class EconomicAgentDecision
    {
        internal EconomicAgentDecision(EconomicAgentExecution execution, IEnumerable<EconomicAgentDiagnostic> diagnostics, IEnumerable<EconomicAgentActionProposal> proposals, EconomicAgentPolicy policy)
        {
            Execution = execution;
            Diagnostics = new ReadOnlyCollection<EconomicAgentDiagnostic>((diagnostics ?? Enumerable.Empty<EconomicAgentDiagnostic>()).ToList());
            Proposals = new ReadOnlyCollection<EconomicAgentActionProposal>((proposals ?? Enumerable.Empty<EconomicAgentActionProposal>()).OrderBy(x => x).ToList());
            Policy = policy;
        }

        public EconomicAgentExecution Execution { get; }
        public bool IsSuccess => Execution.IsSuccess;
        public IReadOnlyList<EconomicAgentDiagnostic> Diagnostics { get; }
        public IReadOnlyList<EconomicAgentActionProposal> Proposals { get; }
        public EconomicAgentPolicy Policy { get; }
        public string CanonicalHash
        {
            get
            {
                var text = string.Join("|", Proposals.Select(x => x.Kind + ":" + x.Owner.Value + ":" + x.SourceOrdinal + ":" + x.TargetRaw + ":" + x.Cost + ":" + x.Priority));
                using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(x => x.ToString("x2")));
            }
        }
    }

    public static class EconomicAgentEvaluator
    {
        public static EconomicAgentDecision Evaluate(EconomicAgentObservation observation, EconomicAgentPolicy policy, EconomicAgentReadLimits limits)
        {
            return Evaluate(observation, policy, EconomicAgentStrategyProfile.Balanced, limits);
        }

        public static EconomicAgentDecision Evaluate(EconomicAgentObservation observation, EconomicAgentPolicy policy, EconomicAgentStrategyProfile strategy, EconomicAgentReadLimits limits)
        {
            var collector = new EconomicAgentDiagnosticCollector(limits.MaxDiagnostics);
            var proposals = new List<EconomicAgentActionProposal>();
            if (!Enum.IsDefined(typeof(EconomicAgentPolicy), policy))
            {
                collector.Error(EconomicAgentDiagnosticCode.InvalidPolicy, -1, "policy", "unknown economic agent policy");
                return new EconomicAgentDecision(collector.Execution, collector.Items, proposals, policy);
            }
            if (observation == null)
            {
                collector.Error(EconomicAgentDiagnosticCode.NullObservation, -1, "observation", "observation is required");
                return new EconomicAgentDecision(collector.Execution, collector.Items, proposals, policy);
            }
            if (observation.Credits.Balance < 0 || observation.Cargo == null || observation.Resources == null || observation.Production == null)
            {
                collector.Error(EconomicAgentDiagnosticCode.InvalidObservation, -1, "observation", "required economic snapshot is invalid");
                return new EconomicAgentDecision(collector.Execution, collector.Items, proposals, policy);
            }

            if (!observation.Resources.Execution.IsSuccess)
                collector.Error(EconomicAgentDiagnosticCode.ChildExecutionFailure, -1, "resources", "resource child execution failed");
            if (!observation.Production.Execution.IsSuccess && HasStructuralProductionFailure(observation.Production))
                collector.Error(EconomicAgentDiagnosticCode.ChildExecutionFailure, observation.ProductionDefinition.Raw.SourceOrdinal, "production", "production child execution failed");
            if (!collector.Execution.IsSuccess)
                return new EconomicAgentDecision(collector.Execution, collector.Items, proposals, policy);

            if (observation.Power.LowPower)
                collector.Warning(EconomicAgentDiagnosticCode.PowerDeficit, -1, "power", "power deficit is preserved as a candidate warning");

            if (observation.Interaction.Allowed)
                AddProposal(proposals, new EconomicAgentActionProposal(observation.Interaction.Action == StructureInteractionAction.RepairCandidate ? EconomicAgentActionKind.RepairCandidate : MapInteraction(observation.Interaction.Action), observation.Interaction.SourceOwner, -1, "structure", 0, StrategyPriority(strategy, EconomicAgentIntent.Repair), "ConservativeDeterministic", EconomicAgentIntent.Repair), collector, limits);

            if (observation.Production.IsRequestable)
            {
                if (observation.ProductionDefinition.Raw.RawCost < 0)
                    collector.Error(EconomicAgentDiagnosticCode.InvalidObservation, observation.ProductionDefinition.Raw.SourceOrdinal, "production", "negative production cost candidate");
                else if (observation.ProductionDefinition.Raw.RawCost <= observation.Credits.Balance)
                    AddProposal(proposals, new EconomicAgentActionProposal(EconomicAgentActionKind.QueueProductionCandidate, observation.Credits.Player, observation.ProductionDefinition.Raw.SourceOrdinal, observation.ProductionDefinition.Raw.TypeRaw, observation.ProductionDefinition.Raw.RawCost, StrategyPriority(strategy, EconomicAgentIntent.Produce), "ConservativeDeterministic", EconomicAgentIntent.Produce), collector, limits);
                else
                    collector.Warning(EconomicAgentDiagnosticCode.InsufficientCredits, observation.ProductionDefinition.Raw.SourceOrdinal, "production", "credits do not cover the candidate cost");
            }
            else
                collector.Warning(EconomicAgentDiagnosticCode.ProductionUnavailable, observation.ProductionDefinition.Raw.SourceOrdinal, "production", "production candidate is not requestable");

            if (observation.Cargo.TotalQuantity < observation.Cargo.Capacity && observation.Resources.Cells.Count > 0)
            {
                var sourceOrdinal = observation.Resources.Cells[0].Raw.SourceOrdinal;
                AddProposal(proposals, new EconomicAgentActionProposal(EconomicAgentActionKind.HarvestCandidate, observation.Credits.Player, sourceOrdinal, "resource", 0, StrategyPriority(strategy, EconomicAgentIntent.Harvest), "ConservativeDeterministic", EconomicAgentIntent.Harvest), collector, limits);
            }
            else
                collector.Warning(EconomicAgentDiagnosticCode.ResourceUnavailable, -1, "resources", "no harvest candidate has available bounded capacity");

            return new EconomicAgentDecision(collector.Execution, collector.Items, proposals, policy);
        }

        private static int StrategyPriority(EconomicAgentStrategyProfile strategy, EconomicAgentIntent intent)
        {
            if (!Enum.IsDefined(typeof(EconomicAgentStrategyProfile), strategy)) return 100;
            switch (strategy)
            {
                case EconomicAgentStrategyProfile.Rush: return intent == EconomicAgentIntent.Produce ? 5 : 40;
                case EconomicAgentStrategyProfile.Macro: return intent == EconomicAgentIntent.Harvest ? 5 : 35;
                case EconomicAgentStrategyProfile.Turtle: return intent == EconomicAgentIntent.Repair || intent == EconomicAgentIntent.DefendEconomy ? 5 : 45;
                case EconomicAgentStrategyProfile.AllIn: return intent == EconomicAgentIntent.Produce ? 5 : 50;
                case EconomicAgentStrategyProfile.Pressure: return intent == EconomicAgentIntent.Produce ? 10 : 30;
                default: return intent == EconomicAgentIntent.Repair ? 10 : (intent == EconomicAgentIntent.Produce ? 20 : 30);
            }
        }

        private static bool HasStructuralProductionFailure(ProductionAvailabilityResult result)
        {
            return result.Diagnostics.Any(x => x.Code == ProductionDiagnosticCode.InvalidPolicy || x.Code == ProductionDiagnosticCode.InvalidDefinition || x.Code == ProductionDiagnosticCode.NoProgress);
        }

        private static EconomicAgentActionKind MapInteraction(StructureInteractionAction action)
        {
            switch (action)
            {
                case StructureInteractionAction.SellCandidate: return EconomicAgentActionKind.SellCandidate;
                case StructureInteractionAction.CaptureCandidate: return EconomicAgentActionKind.CaptureCandidate;
                case StructureInteractionAction.DeployCandidate: return EconomicAgentActionKind.DeployCandidate;
                default: return EconomicAgentActionKind.RepairCandidate;
            }
        }

        private static void AddProposal(List<EconomicAgentActionProposal> proposals, EconomicAgentActionProposal proposal, EconomicAgentDiagnosticCollector collector, EconomicAgentReadLimits limits)
        {
            if (proposals.Count >= limits.MaxActions)
            {
                collector.Error(EconomicAgentDiagnosticCode.ActionBudgetExceeded, proposal.SourceOrdinal, "actions", "action proposal budget exceeded");
                return;
            }
            proposals.Add(proposal);
        }
    }

    internal sealed class EconomicAgentDiagnosticCollector
    {
        private readonly List<EconomicAgentDiagnostic> items = new List<EconomicAgentDiagnostic>();
        private readonly int budget;
        private bool failed;
        private EconomicAgentSeverity highest;
        private int suppressed;

        public EconomicAgentDiagnosticCollector(int budget) { this.budget = Math.Max(0, budget); }
        public IReadOnlyList<EconomicAgentDiagnostic> Items => new ReadOnlyCollection<EconomicAgentDiagnostic>(items);
        public EconomicAgentExecution Execution => new EconomicAgentExecution(failed ? EconomicAgentCompletionStatus.Failed : EconomicAgentCompletionStatus.Succeeded, failed, highest, suppressed);
        public void Warning(EconomicAgentDiagnosticCode code, long ordinal, string stage, string message) { highest = EconomicAgentSeverity.Warning; Add(new EconomicAgentDiagnostic(code, EconomicAgentSeverity.Warning, ordinal, stage, message)); }
        public void Error(EconomicAgentDiagnosticCode code, long ordinal, string stage, string message) { failed = true; highest = EconomicAgentSeverity.Error; Add(new EconomicAgentDiagnostic(code, EconomicAgentSeverity.Error, ordinal, stage, message)); }
        private void Add(EconomicAgentDiagnostic diagnostic)
        {
            if (items.Count < budget) items.Add(diagnostic);
            else
            {
                try { suppressed = checked(suppressed + 1); }
                catch (OverflowException) { suppressed = int.MaxValue; }
            }
        }
    }
}
