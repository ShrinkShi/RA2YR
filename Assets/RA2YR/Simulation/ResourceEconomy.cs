using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Simulation
{
    public enum ResourceFamily { Unknown, Tiberium, Ore, Gem, Custom }
    public enum ResourceVisualStage { Unknown, Seed, Mature, Abundant, Depleted }
    public enum ResourceEconomyDiagnosticCode
    {
        InvalidPolicy,
        InvalidRawField,
        ResourceTypeBudgetExceeded,
        ResourceCellBudgetExceeded,
        CargoTypeBudgetExceeded,
        CargoAmountOverflow,
        CapacityExceeded,
        DuplicateResourceType,
        UnknownResourceType,
        UnacceptedResource,
        DockBudgetExceeded,
        InvalidDock,
        ValueOverflow,
        QuantityOverflow,
        ArithmeticOverflow,
        NoProgress
    }

    public enum ResourceEconomySeverity { Warning, Error }
    public enum ResourceEconomyCompletionStatus { Succeeded, Failed }
    public enum ResourceQuantityProfile { PreserveOnly, OverlayDataPlusOne }
    public enum ResourceValueProfile { PreserveOnly, RulesResourceValue }

    public sealed class ResourceEconomyDiagnostic
    {
        public ResourceEconomyDiagnostic(ResourceEconomyDiagnosticCode code, ResourceEconomySeverity severity, int sourceOrdinal, string stage, string message)
        {
            Code = code;
            Severity = severity;
            SourceOrdinal = sourceOrdinal;
            Stage = stage ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public ResourceEconomyDiagnosticCode Code { get; }
        public ResourceEconomySeverity Severity { get; }
        public int SourceOrdinal { get; }
        public string Stage { get; }
        public string Message { get; }
    }

    public readonly struct ResourceEconomyExecution
    {
        public ResourceEconomyExecution(ResourceEconomyCompletionStatus status, bool hasFatalError, ResourceEconomySeverity highestSeverity, int suppressedDiagnosticCount)
        {
            CompletionStatus = status;
            HasFatalError = hasFatalError;
            HighestSeverity = highestSeverity;
            SuppressedDiagnosticCount = suppressedDiagnosticCount;
        }

        public ResourceEconomyCompletionStatus CompletionStatus { get; }
        public bool HasFatalError { get; }
        public ResourceEconomySeverity HighestSeverity { get; }
        public int SuppressedDiagnosticCount { get; }
        public bool IsSuccess => CompletionStatus == ResourceEconomyCompletionStatus.Succeeded && !HasFatalError;
    }

    public readonly struct ResourceEconomyReadLimits
    {
        public ResourceEconomyReadLimits(int maxResourceTypes, int maxResourceCells, int maxCargoTypes, int maxDockSlots, int maxCargoAmount, int maxDiagnostics)
        {
            MaxResourceTypes = maxResourceTypes;
            MaxResourceCells = maxResourceCells;
            MaxCargoTypes = maxCargoTypes;
            MaxDockSlots = maxDockSlots;
            MaxCargoAmount = maxCargoAmount;
            MaxDiagnostics = maxDiagnostics;
        }

        public int MaxResourceTypes { get; }
        public int MaxResourceCells { get; }
        public int MaxCargoTypes { get; }
        public int MaxDockSlots { get; }
        public int MaxCargoAmount { get; }
        public int MaxDiagnostics { get; }

        public static ResourceEconomyReadLimits Default => new ResourceEconomyReadLimits(256, 65536, 32, 16, 100000000, 256);
    }

    public readonly struct ResourceTypeRaw : IComparable<ResourceTypeRaw>
    {
        public ResourceTypeRaw(int sourceOrdinal, string keyRaw, string valueRaw, ResourceFamily family, long rawRulesValue)
        {
            if (sourceOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(sourceOrdinal));
            SourceOrdinal = sourceOrdinal;
            KeyRaw = keyRaw ?? string.Empty;
            ValueRaw = valueRaw ?? string.Empty;
            Family = family;
            RawRulesValue = rawRulesValue;
        }

        public int SourceOrdinal { get; }
        public string KeyRaw { get; }
        public string ValueRaw { get; }
        public ResourceFamily Family { get; }
        public long RawRulesValue { get; }
        public int CompareTo(ResourceTypeRaw other) => SourceOrdinal.CompareTo(other.SourceOrdinal);
    }

    public readonly struct ResourceTypeDescriptor : IComparable<ResourceTypeDescriptor>
    {
        public ResourceTypeDescriptor(ResourceTypeRaw raw, long? rulesValueCandidate, string evidence)
        {
            Raw = raw;
            RulesValueCandidate = rulesValueCandidate;
            Evidence = evidence ?? string.Empty;
        }

        public ResourceTypeRaw Raw { get; }
        public long? RulesValueCandidate { get; }
        public string Evidence { get; }
        public int CompareTo(ResourceTypeDescriptor other) => Raw.SourceOrdinal.CompareTo(other.Raw.SourceOrdinal);
    }

    public readonly struct ResourceCellRaw : IComparable<ResourceCellRaw>
    {
        public ResourceCellRaw(int sourceOrdinal, int overlayTypeRaw, int overlayDataRaw, ResourceFamily family, ResourceVisualStage stage)
        {
            if (sourceOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(sourceOrdinal));
            SourceOrdinal = sourceOrdinal;
            OverlayTypeRaw = overlayTypeRaw;
            OverlayDataRaw = overlayDataRaw;
            Family = family;
            Stage = stage;
        }

        public int SourceOrdinal { get; }
        public int OverlayTypeRaw { get; }
        public int OverlayDataRaw { get; }
        public ResourceFamily Family { get; }
        public ResourceVisualStage Stage { get; }
        public int CompareTo(ResourceCellRaw other) => SourceOrdinal.CompareTo(other.SourceOrdinal);
    }

    public readonly struct ResourceCellDescriptor : IComparable<ResourceCellDescriptor>
    {
        public ResourceCellDescriptor(ResourceCellRaw raw, long? quantityCandidate, long? valueCandidate, string quantityPolicy, string valuePolicy)
        {
            Raw = raw;
            QuantityCandidate = quantityCandidate;
            ValueCandidate = valueCandidate;
            QuantityPolicy = quantityPolicy ?? string.Empty;
            ValuePolicy = valuePolicy ?? string.Empty;
        }

        public ResourceCellRaw Raw { get; }
        public long? QuantityCandidate { get; }
        public long? ValueCandidate { get; }
        public string QuantityPolicy { get; }
        public string ValuePolicy { get; }
        public int CompareTo(ResourceCellDescriptor other) => Raw.SourceOrdinal.CompareTo(other.Raw.SourceOrdinal);
    }

    public readonly struct HarvesterCapacityDescriptor
    {
        public HarvesterCapacityDescriptor(string rawType, long rawCapacity, IEnumerable<ResourceFamily> acceptedFamilies)
        {
            RawType = rawType ?? string.Empty;
            RawCapacity = rawCapacity;
            AcceptedFamilies = new ReadOnlyCollection<ResourceFamily>((acceptedFamilies ?? Enumerable.Empty<ResourceFamily>()).Distinct().OrderBy(x => x).ToList());
        }

        public string RawType { get; }
        public long RawCapacity { get; }
        public IReadOnlyList<ResourceFamily> AcceptedFamilies { get; }
    }

    public readonly struct HarvesterCargoEntry : IComparable<HarvesterCargoEntry>
    {
        public HarvesterCargoEntry(ResourceFamily family, long quantity, int sourceOrdinal)
        {
            Family = family;
            Quantity = quantity;
            SourceOrdinal = sourceOrdinal;
        }

        public ResourceFamily Family { get; }
        public long Quantity { get; }
        public int SourceOrdinal { get; }
        public int CompareTo(HarvesterCargoEntry other) => SourceOrdinal.CompareTo(other.SourceOrdinal);
    }

    public sealed class HarvesterCargoSnapshot
    {
        private HarvesterCargoSnapshot(long capacity, IEnumerable<HarvesterCargoEntry> entries, long total)
        {
            Capacity = capacity;
            Entries = new ReadOnlyCollection<HarvesterCargoEntry>((entries ?? Enumerable.Empty<HarvesterCargoEntry>()).OrderBy(x => x).ToList());
            TotalQuantity = total;
        }

        public long Capacity { get; }
        public IReadOnlyList<HarvesterCargoEntry> Entries { get; }
        public long TotalQuantity { get; }

        public static bool TryCreate(long capacity, IEnumerable<HarvesterCargoEntry> entries, ResourceEconomyReadLimits limits, out HarvesterCargoSnapshot snapshot, out IReadOnlyList<ResourceEconomyDiagnostic> diagnostics)
        {
            var collector = new ResourceEconomyConsistencyAnalysis.DiagnosticCollector(limits.MaxDiagnostics);
            snapshot = null;
            if (capacity < 0 || capacity > limits.MaxCargoAmount)
            {
                collector.Error(ResourceEconomyDiagnosticCode.InvalidRawField, -1, "cargo", "capacity is outside the explicit budget");
                diagnostics = collector.Items;
                return false;
            }
            var source = entries ?? Enumerable.Empty<HarvesterCargoEntry>();
            var list = new List<HarvesterCargoEntry>();
            long total = 0;
            foreach (var entry in source)
            {
                if (list.Count >= limits.MaxCargoTypes)
                {
                    collector.Error(ResourceEconomyDiagnosticCode.CargoTypeBudgetExceeded, entry.SourceOrdinal, "cargo", "cargo type budget exceeded");
                    break;
                }
                if (entry.Quantity < 0)
                {
                    collector.Error(ResourceEconomyDiagnosticCode.InvalidRawField, entry.SourceOrdinal, "cargo", "negative cargo is not accepted");
                    continue;
                }
                try { total = checked(total + entry.Quantity); }
                catch (OverflowException) { collector.Error(ResourceEconomyDiagnosticCode.CargoAmountOverflow, entry.SourceOrdinal, "cargo", "cargo sum overflow"); break; }
                if (total > capacity || total > limits.MaxCargoAmount)
                {
                    collector.Error(ResourceEconomyDiagnosticCode.CapacityExceeded, entry.SourceOrdinal, "cargo", "cargo exceeds capacity");
                    break;
                }
                list.Add(entry);
            }
            diagnostics = collector.Items;
            if (!collector.Execution.IsSuccess) return false;
            snapshot = new HarvesterCargoSnapshot(capacity, list, total);
            return true;
        }
    }

    public readonly struct DockingSlotDescriptor : IComparable<DockingSlotDescriptor>
    {
        public DockingSlotDescriptor(int slotOrdinal, int approachX, int approachY, int dockX, int dockY, int exitX, int exitY, int capacity)
        {
            SlotOrdinal = slotOrdinal;
            ApproachX = approachX;
            ApproachY = approachY;
            DockX = dockX;
            DockY = dockY;
            ExitX = exitX;
            ExitY = exitY;
            Capacity = capacity;
        }

        public int SlotOrdinal { get; }
        public int ApproachX { get; }
        public int ApproachY { get; }
        public int DockX { get; }
        public int DockY { get; }
        public int ExitX { get; }
        public int ExitY { get; }
        public int Capacity { get; }
        public int CompareTo(DockingSlotDescriptor other) => SlotOrdinal.CompareTo(other.SlotOrdinal);
    }

    public readonly struct RefineryCapabilityDescriptor
    {
        public RefineryCapabilityDescriptor(string rawType, IEnumerable<ResourceFamily> acceptedFamilies, IEnumerable<DockingSlotDescriptor> slots)
        {
            RawType = rawType ?? string.Empty;
            AcceptedFamilies = new ReadOnlyCollection<ResourceFamily>((acceptedFamilies ?? Enumerable.Empty<ResourceFamily>()).Distinct().OrderBy(x => x).ToList());
            Slots = new ReadOnlyCollection<DockingSlotDescriptor>((slots ?? Enumerable.Empty<DockingSlotDescriptor>()).OrderBy(x => x).ToList());
        }

        public string RawType { get; }
        public IReadOnlyList<ResourceFamily> AcceptedFamilies { get; }
        public IReadOnlyList<DockingSlotDescriptor> Slots { get; }
    }

    public sealed class ResourceEconomyConsistencyAnalysis
    {
        private ResourceEconomyConsistencyAnalysis(ResourceEconomyExecution execution, IEnumerable<ResourceEconomyDiagnostic> diagnostics, IEnumerable<ResourceCellDescriptor> cells, IEnumerable<ResourceTypeDescriptor> types)
        {
            Execution = execution;
            Diagnostics = new ReadOnlyCollection<ResourceEconomyDiagnostic>((diagnostics ?? Enumerable.Empty<ResourceEconomyDiagnostic>()).ToList());
            Cells = new ReadOnlyCollection<ResourceCellDescriptor>((cells ?? Enumerable.Empty<ResourceCellDescriptor>()).OrderBy(x => x).ToList());
            Types = new ReadOnlyCollection<ResourceTypeDescriptor>((types ?? Enumerable.Empty<ResourceTypeDescriptor>()).OrderBy(x => x).ToList());
        }

        public ResourceEconomyExecution Execution { get; }
        public bool IsSuccess => Execution.IsSuccess;
        public IReadOnlyList<ResourceEconomyDiagnostic> Diagnostics { get; }
        public IReadOnlyList<ResourceCellDescriptor> Cells { get; }
        public IReadOnlyList<ResourceTypeDescriptor> Types { get; }
        public string CanonicalHash
        {
            get
            {
                var text = string.Join("|", Cells.Select(x => x.Raw.SourceOrdinal + ":" + x.Raw.OverlayTypeRaw + ":" + x.Raw.OverlayDataRaw + ":" + (x.QuantityCandidate.HasValue ? x.QuantityCandidate.Value.ToString() : "-") + ":" + (x.ValueCandidate.HasValue ? x.ValueCandidate.Value.ToString() : "-")));
                using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(x => x.ToString("x2")));
            }
        }

        public static ResourceEconomyConsistencyAnalysis Analyze(IEnumerable<ResourceCellRaw> rawCells, IEnumerable<ResourceTypeRaw> rawTypes, ResourceQuantityProfile quantityProfile, ResourceValueProfile valueProfile, ResourceEconomyReadLimits limits)
        {
            var collector = new DiagnosticCollector(limits.MaxDiagnostics);
            var types = new List<ResourceTypeDescriptor>();
            var cells = new List<ResourceCellDescriptor>();
            if (!Enum.IsDefined(typeof(ResourceQuantityProfile), quantityProfile) || !Enum.IsDefined(typeof(ResourceValueProfile), valueProfile))
            {
                collector.Error(ResourceEconomyDiagnosticCode.InvalidPolicy, -1, "policy", "unknown resource interpretation policy");
                return new ResourceEconomyConsistencyAnalysis(collector.Execution, collector.Items, cells, types);
            }
            foreach (var raw in rawTypes ?? Enumerable.Empty<ResourceTypeRaw>())
            {
                if (types.Count >= limits.MaxResourceTypes) { collector.Error(ResourceEconomyDiagnosticCode.ResourceTypeBudgetExceeded, raw.SourceOrdinal, "types", "resource type budget exceeded"); break; }
                long? value = valueProfile == ResourceValueProfile.RulesResourceValue ? raw.RawRulesValue : (long?)null;
                if (value.HasValue && value.Value < 0) collector.Error(ResourceEconomyDiagnosticCode.InvalidRawField, raw.SourceOrdinal, "types", "negative resource value");
                types.Add(new ResourceTypeDescriptor(raw, value, valueProfile.ToString()));
            }
            foreach (var raw in rawCells ?? Enumerable.Empty<ResourceCellRaw>())
            {
                if (cells.Count >= limits.MaxResourceCells) { collector.Error(ResourceEconomyDiagnosticCode.ResourceCellBudgetExceeded, raw.SourceOrdinal, "cells", "resource cell budget exceeded"); break; }
                long? quantity = null;
                if (quantityProfile == ResourceQuantityProfile.OverlayDataPlusOne)
                {
                    try { quantity = checked((long)raw.OverlayDataRaw + 1L); }
                    catch (OverflowException) { collector.Error(ResourceEconomyDiagnosticCode.QuantityOverflow, raw.SourceOrdinal, "cells", "quantity candidate overflow"); }
                }
                long? valueCandidate = null;
                if (valueProfile == ResourceValueProfile.RulesResourceValue)
                {
                    var match = types.FirstOrDefault(x => x.Raw.Family == raw.Family);
                    if (match.RulesValueCandidate.HasValue && quantity.HasValue)
                    {
                        try { valueCandidate = checked(quantity.Value * match.RulesValueCandidate.Value); }
                        catch (OverflowException) { collector.Error(ResourceEconomyDiagnosticCode.ValueOverflow, raw.SourceOrdinal, "cells", "value candidate overflow"); }
                    }
                }
                cells.Add(new ResourceCellDescriptor(raw, quantity, valueCandidate, quantityProfile.ToString(), valueProfile.ToString()));
            }
            return new ResourceEconomyConsistencyAnalysis(collector.Execution, collector.Items, cells, types);
        }

        public static bool ValidateRefinery(HarvesterCargoSnapshot cargo, RefineryCapabilityDescriptor refinery, ResourceEconomyReadLimits limits, out IReadOnlyList<ResourceEconomyDiagnostic> diagnostics)
        {
            var collector = new DiagnosticCollector(limits.MaxDiagnostics);
            if (refinery.Slots.Count > limits.MaxDockSlots) collector.Error(ResourceEconomyDiagnosticCode.DockBudgetExceeded, -1, "refinery", "dock slot budget exceeded");
            foreach (var slot in refinery.Slots)
            {
                if (slot.Capacity < 0) collector.Error(ResourceEconomyDiagnosticCode.InvalidDock, slot.SlotOrdinal, "refinery", "negative dock capacity");
            }
            if (cargo != null)
            {
                foreach (var entry in cargo.Entries)
                    if (!refinery.AcceptedFamilies.Contains(entry.Family)) collector.Error(ResourceEconomyDiagnosticCode.UnacceptedResource, entry.SourceOrdinal, "refinery", "cargo family is not accepted");
            }
            diagnostics = collector.Items;
            return collector.Execution.IsSuccess;
        }

        internal sealed class DiagnosticCollector
        {
            private readonly List<ResourceEconomyDiagnostic> items = new List<ResourceEconomyDiagnostic>();
            private bool failed;
            private ResourceEconomySeverity highest;
            private int suppressed;
            public DiagnosticCollector(int budget) { Budget = Math.Max(0, budget); }
            private int Budget { get; }
            public IReadOnlyList<ResourceEconomyDiagnostic> Items => new ReadOnlyCollection<ResourceEconomyDiagnostic>(items);
            public ResourceEconomyExecution Execution => new ResourceEconomyExecution(failed ? ResourceEconomyCompletionStatus.Failed : ResourceEconomyCompletionStatus.Succeeded, failed, highest, suppressed);
            public void Error(ResourceEconomyDiagnosticCode code, int ordinal, string stage, string message) { failed = true; highest = ResourceEconomySeverity.Error; Add(new ResourceEconomyDiagnostic(code, ResourceEconomySeverity.Error, ordinal, stage, message)); }
            private void Add(ResourceEconomyDiagnostic diagnostic) { if (items.Count < Budget) items.Add(diagnostic); else { try { suppressed = checked(suppressed + 1); } catch (OverflowException) { suppressed = int.MaxValue; } } }
        }
    }
}
