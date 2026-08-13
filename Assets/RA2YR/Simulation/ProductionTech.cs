using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Simulation
{
    public enum ProductionDiagnosticCode { InvalidPolicy, InvalidDefinition, PrerequisiteBlocked, MissingCapability, TechLevelBlocked, BuildLimitBlocked, CostOverflow, TimeOverflow, QueueBudgetExceeded, DuplicateQueueId, InvalidProgress, QueueNotFound, NoProgress }
    public enum ProductionSeverity { Warning, Error }
    public enum ProductionCompletionStatus { Succeeded, Failed }
    public enum ProductionAvailabilityProfile { ExplicitCapabilitiesAndLimits }
    public enum ProductionQueueProfile { FifoPerFactory }
    public enum ProductionPaymentProfile { CandidateOnly }

    public sealed class ProductionDiagnostic
    {
        public ProductionDiagnostic(ProductionDiagnosticCode code, ProductionSeverity severity, long sourceOrdinal, string stage, string message) { Code = code; Severity = severity; SourceOrdinal = sourceOrdinal; Stage = stage ?? string.Empty; Message = message ?? string.Empty; }
        public ProductionDiagnosticCode Code { get; }
        public ProductionSeverity Severity { get; }
        public long SourceOrdinal { get; }
        public string Stage { get; }
        public string Message { get; }
    }

    public readonly struct ProductionExecution
    {
        public ProductionExecution(ProductionCompletionStatus status, bool fatal, ProductionSeverity severity, int suppressed) { CompletionStatus = status; HasFatalError = fatal; HighestSeverity = severity; SuppressedDiagnosticCount = suppressed; }
        public ProductionCompletionStatus CompletionStatus { get; }
        public bool HasFatalError { get; }
        public ProductionSeverity HighestSeverity { get; }
        public int SuppressedDiagnosticCount { get; }
        public bool IsSuccess => CompletionStatus == ProductionCompletionStatus.Succeeded && !HasFatalError;
    }

    public readonly struct ProductionReadLimits
    {
        public ProductionReadLimits(int maxDefinitions, int maxPrerequisites, int maxQueues, int maxEntries, int maxDiagnostics) { MaxDefinitions = maxDefinitions; MaxPrerequisites = maxPrerequisites; MaxQueues = maxQueues; MaxEntries = maxEntries; MaxDiagnostics = maxDiagnostics; }
        public int MaxDefinitions { get; }
        public int MaxPrerequisites { get; }
        public int MaxQueues { get; }
        public int MaxEntries { get; }
        public int MaxDiagnostics { get; }
        public static ProductionReadLimits Default => new ProductionReadLimits(512, 32, 64, 256, 256);
    }

    public readonly struct ProductionDefinitionRaw : IComparable<ProductionDefinitionRaw>
    {
        public ProductionDefinitionRaw(long sourceOrdinal, string typeRaw, string categoryRaw, int rawTechLevel, long rawCost, long rawBuildTime, long buildLimit, IEnumerable<string> prerequisiteTokens)
        { if (sourceOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(sourceOrdinal)); SourceOrdinal = sourceOrdinal; TypeRaw = typeRaw ?? string.Empty; CategoryRaw = categoryRaw ?? string.Empty; RawTechLevel = rawTechLevel; RawCost = rawCost; RawBuildTime = rawBuildTime; BuildLimit = buildLimit; PrerequisiteTokens = new ReadOnlyCollection<string>((prerequisiteTokens ?? Enumerable.Empty<string>()).Select(x => x ?? string.Empty).ToList()); }
        public long SourceOrdinal { get; }
        public string TypeRaw { get; }
        public string CategoryRaw { get; }
        public int RawTechLevel { get; }
        public long RawCost { get; }
        public long RawBuildTime { get; }
        public long BuildLimit { get; }
        public IReadOnlyList<string> PrerequisiteTokens { get; }
        public int CompareTo(ProductionDefinitionRaw other) => SourceOrdinal.CompareTo(other.SourceOrdinal);
    }

    public readonly struct ProductionDefinitionDescriptor : IComparable<ProductionDefinitionDescriptor>
    {
        public ProductionDefinitionDescriptor(ProductionDefinitionRaw raw, string evidence) { Raw = raw; Evidence = evidence ?? string.Empty; }
        public ProductionDefinitionRaw Raw { get; }
        public string Evidence { get; }
        public int CompareTo(ProductionDefinitionDescriptor other) => Raw.SourceOrdinal.CompareTo(other.Raw.SourceOrdinal);
    }

    public readonly struct ProductionAvailabilityQuery
    {
        public ProductionAvailabilityQuery(ProductionDefinitionDescriptor definition, int techLevel, IEnumerable<string> capabilities, long existingCount, ProductionAvailabilityProfile profile) { Definition = definition; TechLevel = techLevel; capabilitySource = Enum.IsDefined(typeof(ProductionAvailabilityProfile), profile) ? (capabilities ?? Enumerable.Empty<string>()) : null; ExistingCount = existingCount; Profile = profile; }
        public ProductionDefinitionDescriptor Definition { get; }
        public int TechLevel { get; }
        private readonly IEnumerable<string> capabilitySource;
        public IReadOnlyList<string> Capabilities => new ReadOnlyCollection<string>((capabilitySource ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList());
        public long ExistingCount { get; }
        public ProductionAvailabilityProfile Profile { get; }
    }

    public sealed class ProductionAvailabilityResult
    {
        internal ProductionAvailabilityResult(ProductionExecution execution, IEnumerable<ProductionDiagnostic> diagnostics, bool visible, bool requestable, IEnumerable<string> blockers) { Execution = execution; Diagnostics = new ReadOnlyCollection<ProductionDiagnostic>((diagnostics ?? Enumerable.Empty<ProductionDiagnostic>()).ToList()); IsVisible = visible; IsRequestable = requestable; Blockers = new ReadOnlyCollection<string>((blockers ?? Enumerable.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToList()); }
        public ProductionExecution Execution { get; }
        public bool IsSuccess => Execution.IsSuccess;
        public bool IsVisible { get; }
        public bool IsRequestable { get; }
        public IReadOnlyList<string> Blockers { get; }
        public IReadOnlyList<ProductionDiagnostic> Diagnostics { get; }
        public static ProductionAvailabilityResult Evaluate(ProductionAvailabilityQuery query, ProductionReadLimits limits)
        {
            var c = new Collector(limits.MaxDiagnostics);
            if (!Enum.IsDefined(typeof(ProductionAvailabilityProfile), query.Profile)) { c.Error(ProductionDiagnosticCode.InvalidPolicy, query.Definition.Raw.SourceOrdinal, "availability", "unknown availability profile"); return new ProductionAvailabilityResult(c.Execution, c.Items, false, false, new string[0]); }
            var raw = query.Definition.Raw;
            var blockers = new List<string>();
            if (query.TechLevel < raw.RawTechLevel) { blockers.Add("TechLevel"); c.Error(ProductionDiagnosticCode.TechLevelBlocked, raw.SourceOrdinal, "availability", "tech level is below candidate"); }
            if (raw.BuildLimit >= 0 && query.ExistingCount >= raw.BuildLimit) { blockers.Add("BuildLimit"); c.Error(ProductionDiagnosticCode.BuildLimitBlocked, raw.SourceOrdinal, "availability", "build limit reached"); }
            foreach (var token in raw.PrerequisiteTokens) if (!query.Capabilities.Contains(token, StringComparer.Ordinal)) { blockers.Add("Prerequisite:" + token); c.Error(ProductionDiagnosticCode.PrerequisiteBlocked, raw.SourceOrdinal, "availability", "prerequisite candidate is not satisfied"); }
            return new ProductionAvailabilityResult(c.Execution, c.Items, true, c.Execution.IsSuccess && blockers.Count == 0, blockers);
        }
    }

    public readonly struct ProductionQueueId : IEquatable<ProductionQueueId>, IComparable<ProductionQueueId> { public ProductionQueueId(long value) { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); Value = value; } public long Value { get; } public bool Equals(ProductionQueueId other) => Value == other.Value; public override bool Equals(object obj) => obj is ProductionQueueId && Equals((ProductionQueueId)obj); public override int GetHashCode() => Value.GetHashCode(); public int CompareTo(ProductionQueueId other) => Value.CompareTo(other.Value); }
    public readonly struct ProductionQueueEntry : IComparable<ProductionQueueEntry>
    {
        public ProductionQueueEntry(ProductionQueueId id, PlayerId owner, string typeRaw, string categoryRaw, long cost, long buildTime, long ordinal) { Id = id; Owner = owner; TypeRaw = typeRaw ?? string.Empty; CategoryRaw = categoryRaw ?? string.Empty; Cost = cost; BuildTime = buildTime; Progress = 0; Ordinal = ordinal; }
        private ProductionQueueEntry(ProductionQueueId id, PlayerId owner, string typeRaw, string categoryRaw, long cost, long buildTime, long progress, long ordinal) { Id = id; Owner = owner; TypeRaw = typeRaw; CategoryRaw = categoryRaw; Cost = cost; BuildTime = buildTime; Progress = progress; Ordinal = ordinal; }
        public ProductionQueueId Id { get; }
        public PlayerId Owner { get; }
        public string TypeRaw { get; }
        public string CategoryRaw { get; }
        public long Cost { get; }
        public long BuildTime { get; }
        public long Progress { get; }
        public long Ordinal { get; }
        public bool IsComplete => BuildTime > 0 && Progress >= BuildTime;
        public ProductionQueueEntry WithProgress(long progress) => new ProductionQueueEntry(Id, Owner, TypeRaw, CategoryRaw, Cost, BuildTime, progress, Ordinal);
        public int CompareTo(ProductionQueueEntry other) { var c = Ordinal.CompareTo(other.Ordinal); return c != 0 ? c : Id.CompareTo(other.Id); }
    }

    public sealed class ProductionQueue
    {
        private readonly List<ProductionQueueEntry> entries = new List<ProductionQueueEntry>();
        private readonly ProductionReadLimits limits;
        public ProductionQueue(ProductionReadLimits limits) { this.limits = limits; }
        public IReadOnlyList<ProductionQueueEntry> Entries => new ReadOnlyCollection<ProductionQueueEntry>(entries.OrderBy(x => x).ToList());
        public bool TryEnqueue(ProductionQueueEntry entry, out IReadOnlyList<ProductionDiagnostic> diagnostics)
        { var c = new Collector(limits.MaxDiagnostics); if (entries.Count >= limits.MaxEntries) c.Error(ProductionDiagnosticCode.QueueBudgetExceeded, entry.Ordinal, "queue", "queue entry budget exceeded"); if (entries.Any(x => x.Id.Equals(entry.Id))) c.Error(ProductionDiagnosticCode.DuplicateQueueId, entry.Ordinal, "queue", "duplicate queue id"); if (entry.Cost < 0 || entry.BuildTime <= 0) c.Error(ProductionDiagnosticCode.InvalidDefinition, entry.Ordinal, "queue", "cost/build time candidate is invalid"); diagnostics = c.Items; if (!c.Execution.IsSuccess) return false; entries.Add(entry); return true; }
        public bool TryAdvance(ProductionQueueId id, long delta, out ProductionQueueEntry updated, out IReadOnlyList<ProductionDiagnostic> diagnostics)
        { var c = new Collector(limits.MaxDiagnostics); updated = default(ProductionQueueEntry); var index = entries.FindIndex(x => x.Id.Equals(id)); if (index < 0) c.Error(ProductionDiagnosticCode.QueueNotFound, -1, "queue", "queue id not found"); else if (delta <= 0) c.Error(ProductionDiagnosticCode.InvalidProgress, entries[index].Ordinal, "queue", "progress delta must be positive"); else { var current = entries[index]; try { var next = checked(current.Progress + delta); if (next > current.BuildTime) next = current.BuildTime; updated = current.WithProgress(next); entries[index] = updated; } catch (OverflowException) { c.Error(ProductionDiagnosticCode.TimeOverflow, current.Ordinal, "queue", "progress overflow"); } } diagnostics = c.Items; return c.Execution.IsSuccess; }
        public string CanonicalHash() { var text = string.Join("|", Entries.Select(x => x.Id.Value + ":" + x.Owner.Value + ":" + x.TypeRaw + ":" + x.Progress + "/" + x.BuildTime + ":" + x.Ordinal)); using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(x => x.ToString("x2"))); }
    }

    internal sealed class Collector
    {
        private readonly List<ProductionDiagnostic> items = new List<ProductionDiagnostic>(); private readonly int budget; private bool failed; private ProductionSeverity highest; private int suppressed;
        public Collector(int budget) { this.budget = Math.Max(0, budget); }
        public IReadOnlyList<ProductionDiagnostic> Items => new ReadOnlyCollection<ProductionDiagnostic>(items);
        public ProductionExecution Execution => new ProductionExecution(failed ? ProductionCompletionStatus.Failed : ProductionCompletionStatus.Succeeded, failed, highest, suppressed);
        public void Error(ProductionDiagnosticCode code, long ordinal, string stage, string message) { failed = true; highest = ProductionSeverity.Error; var d = new ProductionDiagnostic(code, ProductionSeverity.Error, ordinal, stage, message); if (items.Count < budget) items.Add(d); else { try { suppressed = checked(suppressed + 1); } catch (OverflowException) { suppressed = int.MaxValue; } } }
    }
}
