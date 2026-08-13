using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RA2YR.Simulation
{
    public enum StructureDiagnosticCode { InvalidPolicy, InvalidDefinition, PlacementOutOfBounds, PlacementOverlap, FootprintOverflow, PowerOverflow, PowerDeficit, RepairInvalid, SellInvalid, CaptureInvalid, DeployInvalid, BudgetExceeded }
    public enum StructureSeverity { Warning, Error }
    public enum StructureCompletionStatus { Succeeded, Failed }
    public enum StructurePlacementProfile { ExplicitRectangularFootprint }
    public enum StructureInteractionAction { RepairCandidate, SellCandidate, CaptureCandidate, DeployCandidate }

    public sealed class StructureDiagnostic
    {
        public StructureDiagnostic(StructureDiagnosticCode code, StructureSeverity severity, long ordinal, string stage, string message) { Code = code; Severity = severity; SourceOrdinal = ordinal; Stage = stage ?? string.Empty; Message = message ?? string.Empty; }
        public StructureDiagnosticCode Code { get; }
        public StructureSeverity Severity { get; }
        public long SourceOrdinal { get; }
        public string Stage { get; }
        public string Message { get; }
    }

    public readonly struct StructureExecution
    {
        public StructureExecution(StructureCompletionStatus status, bool fatal, StructureSeverity highest, int suppressed) { CompletionStatus = status; HasFatalError = fatal; HighestSeverity = highest; SuppressedDiagnosticCount = suppressed; }
        public StructureCompletionStatus CompletionStatus { get; }
        public bool HasFatalError { get; }
        public StructureSeverity HighestSeverity { get; }
        public int SuppressedDiagnosticCount { get; }
        public bool IsSuccess => CompletionStatus == StructureCompletionStatus.Succeeded && !HasFatalError;
    }

    public readonly struct StructureReadLimits
    {
        public StructureReadLimits(int maxDefinitions, int maxCells, int maxDiagnostics) { MaxDefinitions = maxDefinitions; MaxCells = maxCells; MaxDiagnostics = maxDiagnostics; }
        public int MaxDefinitions { get; }
        public int MaxCells { get; }
        public int MaxDiagnostics { get; }
        public static StructureReadLimits Default => new StructureReadLimits(512, 65536, 256);
    }

    public readonly struct StructureDefinitionRaw : IComparable<StructureDefinitionRaw>
    {
        public StructureDefinitionRaw(long ordinal, string typeRaw, int width, int height, long maxHealth, int powerProduced, int powerConsumed, string ownerRaw)
        { if (ordinal < 0) throw new ArgumentOutOfRangeException(nameof(ordinal)); SourceOrdinal = ordinal; TypeRaw = typeRaw ?? string.Empty; Width = width; Height = height; MaxHealth = maxHealth; PowerProduced = powerProduced; PowerConsumed = powerConsumed; OwnerRaw = ownerRaw ?? string.Empty; }
        public long SourceOrdinal { get; }
        public string TypeRaw { get; }
        public int Width { get; }
        public int Height { get; }
        public long MaxHealth { get; }
        public int PowerProduced { get; }
        public int PowerConsumed { get; }
        public string OwnerRaw { get; }
        public int CompareTo(StructureDefinitionRaw other) => SourceOrdinal.CompareTo(other.SourceOrdinal);
    }

    public readonly struct StructureFootprintCell : IComparable<StructureFootprintCell>
    {
        public StructureFootprintCell(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public int CompareTo(StructureFootprintCell other) { var c = Y.CompareTo(other.Y); return c != 0 ? c : X.CompareTo(other.X); }
    }

    public sealed class StructurePlacementResult
    {
        internal StructurePlacementResult(StructureExecution execution, IEnumerable<StructureDiagnostic> diagnostics, StructureDefinitionRaw definition, int originX, int originY, IEnumerable<StructureFootprintCell> cells)
        { Execution = execution; Diagnostics = new ReadOnlyCollection<StructureDiagnostic>((diagnostics ?? Enumerable.Empty<StructureDiagnostic>()).ToList()); Definition = definition; OriginX = originX; OriginY = originY; Footprint = new ReadOnlyCollection<StructureFootprintCell>((cells ?? Enumerable.Empty<StructureFootprintCell>()).OrderBy(x => x).ToList()); }
        public StructureExecution Execution { get; }
        public bool IsSuccess => Execution.IsSuccess;
        public StructureDefinitionRaw Definition { get; }
        public int OriginX { get; }
        public int OriginY { get; }
        public IReadOnlyList<StructureFootprintCell> Footprint { get; }
        public IReadOnlyList<StructureDiagnostic> Diagnostics { get; }
        public string CanonicalHash { get { var s = string.Join("|", Footprint.Select(x => x.X + "," + x.Y)); using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(s)).Select(x => x.ToString("x2"))); } }

        public static StructurePlacementResult Evaluate(StructureDefinitionRaw definition, int originX, int originY, int mapWidth, int mapHeight, IEnumerable<StructureFootprintCell> occupied, StructurePlacementProfile profile, StructureReadLimits limits)
        {
            var c = new StructureCollector(limits.MaxDiagnostics);
            if (!Enum.IsDefined(typeof(StructurePlacementProfile), profile)) { c.Error(StructureDiagnosticCode.InvalidPolicy, definition.SourceOrdinal, "placement", "unknown placement profile"); return new StructurePlacementResult(c.Execution, c.Items, definition, originX, originY, null); }
            if (definition.Width <= 0 || definition.Height <= 0 || definition.MaxHealth < 0) c.Error(StructureDiagnosticCode.InvalidDefinition, definition.SourceOrdinal, "placement", "invalid footprint or health");
            var cells = new List<StructureFootprintCell>();
            try
            {
                var area = checked(definition.Width * definition.Height);
                if (area > limits.MaxCells) c.Error(StructureDiagnosticCode.BudgetExceeded, definition.SourceOrdinal, "placement", "footprint budget exceeded");
                for (var y = 0; y < definition.Height && cells.Count <= limits.MaxCells; y++) for (var x = 0; x < definition.Width && cells.Count <= limits.MaxCells; x++)
                {
                    var cell = new StructureFootprintCell(checked(originX + x), checked(originY + y)); cells.Add(cell);
                    if (cell.X < 0 || cell.Y < 0 || cell.X >= mapWidth || cell.Y >= mapHeight) c.Error(StructureDiagnosticCode.PlacementOutOfBounds, definition.SourceOrdinal, "placement", "footprint is outside explicit map bounds");
                    if ((occupied ?? Enumerable.Empty<StructureFootprintCell>()).Contains(cell)) c.Error(StructureDiagnosticCode.PlacementOverlap, definition.SourceOrdinal, "placement", "footprint overlaps supplied occupied candidate");
                }
            }
            catch (OverflowException) { c.Error(StructureDiagnosticCode.FootprintOverflow, definition.SourceOrdinal, "placement", "footprint arithmetic overflow"); }
            return new StructurePlacementResult(c.Execution, c.Items, definition, originX, originY, cells);
        }
    }

    public readonly struct StructurePowerProjection
    {
        public StructurePowerProjection(IEnumerable<StructureDefinitionRaw> definitions)
        {
            long produced = 0, consumed = 0; foreach (var d in definitions ?? Enumerable.Empty<StructureDefinitionRaw>()) { produced = checked(produced + d.PowerProduced); consumed = checked(consumed + d.PowerConsumed); }
            Produced = produced; Consumed = consumed;
        }
        public long Produced { get; }
        public long Consumed { get; }
        public long Deficit => Math.Max(0L, Consumed - Produced);
        public bool LowPower => Deficit > 0;
    }

    public readonly struct StructureInteractionCandidate
    {
        public StructureInteractionCandidate(StructureInteractionAction action, PlayerId sourceOwner, PlayerId targetOwner, long currentHealth, long maxHealth, bool allowed, string policy) { Action = action; SourceOwner = sourceOwner; TargetOwner = targetOwner; CurrentHealth = currentHealth; MaxHealth = maxHealth; Allowed = allowed; Policy = policy ?? string.Empty; }
        public StructureInteractionAction Action { get; }
        public PlayerId SourceOwner { get; }
        public PlayerId TargetOwner { get; }
        public long CurrentHealth { get; }
        public long MaxHealth { get; }
        public bool Allowed { get; }
        public string Policy { get; }
    }

    public static class StructureInteractionAnalyzer
    {
        public static StructureInteractionCandidate Analyze(StructureInteractionAction action, PlayerId sourceOwner, PlayerId targetOwner, long currentHealth, long maxHealth, bool deployed, bool captured, StructureReadLimits limits, out IReadOnlyList<StructureDiagnostic> diagnostics)
        { var c = new StructureCollector(limits.MaxDiagnostics); var allowed = true; if (!Enum.IsDefined(typeof(StructureInteractionAction), action)) { c.Error(StructureDiagnosticCode.InvalidPolicy, -1, "interaction", "unknown interaction action"); allowed = false; } if (currentHealth < 0 || maxHealth <= 0 || currentHealth > maxHealth) { c.Error(StructureDiagnosticCode.InvalidDefinition, -1, "interaction", "health candidate is invalid"); allowed = false; } if (action == StructureInteractionAction.RepairCandidate && currentHealth >= maxHealth) { c.Error(StructureDiagnosticCode.RepairInvalid, -1, "interaction", "repair has no damaged health candidate"); allowed = false; } if (action == StructureInteractionAction.CaptureCandidate && sourceOwner.Equals(targetOwner)) { c.Error(StructureDiagnosticCode.CaptureInvalid, -1, "interaction", "capture target already has source owner"); allowed = false; } if (action == StructureInteractionAction.DeployCandidate && deployed) { c.Error(StructureDiagnosticCode.DeployInvalid, -1, "interaction", "structure is already deployed"); allowed = false; } if (action == StructureInteractionAction.SellCandidate && captured) { c.Error(StructureDiagnosticCode.SellInvalid, -1, "interaction", "captured state requires explicit sell policy"); allowed = false; } diagnostics = c.Items; return new StructureInteractionCandidate(action, sourceOwner, targetOwner, currentHealth, maxHealth, allowed && c.Execution.IsSuccess, "ExplicitCandidateOnly"); }
    }

    internal sealed class StructureCollector
    {
        private readonly List<StructureDiagnostic> items = new List<StructureDiagnostic>(); private readonly int budget; private bool failed; private StructureSeverity highest; private int suppressed;
        public StructureCollector(int budget) { this.budget = Math.Max(0, budget); }
        public IReadOnlyList<StructureDiagnostic> Items => new ReadOnlyCollection<StructureDiagnostic>(items);
        public StructureExecution Execution => new StructureExecution(failed ? StructureCompletionStatus.Failed : StructureCompletionStatus.Succeeded, failed, highest, suppressed);
        public void Error(StructureDiagnosticCode code, long ordinal, string stage, string message) { failed = true; highest = StructureSeverity.Error; var d = new StructureDiagnostic(code, StructureSeverity.Error, ordinal, stage, message); if (items.Count < budget) items.Add(d); else { try { suppressed = checked(suppressed + 1); } catch (OverflowException) { suppressed = int.MaxValue; } } }
    }
}
