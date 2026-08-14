using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RA2YR.Simulation;

namespace RA2YR.Presentation
{
    public enum ClientDiagnosticSeverity { Info, Warning, Error }
    public enum ClientDiagnosticCode
    {
        InvalidPolicy,
        BudgetExceeded,
        DuplicateVisibility,
        UnknownVisibility,
        InvalidSelection,
        SelectionBudgetExceeded,
        NoSelection,
        InvalidCommand,
        CommandRejected,
        ProjectionArithmeticOverflow,
        InvalidPointer,
        PlacementUnavailable,
        ProductionUnavailable
    }

    public sealed class ClientDiagnostic
    {
        public ClientDiagnostic(ClientDiagnosticCode code, ClientDiagnosticSeverity severity, string stage, string message, EntityId? entity = null)
        { Code = code; Severity = severity; Stage = stage ?? string.Empty; Message = message ?? string.Empty; Entity = entity; }
        public ClientDiagnosticCode Code { get; }
        public ClientDiagnosticSeverity Severity { get; }
        public string Stage { get; }
        public string Message { get; }
        public EntityId? Entity { get; }
    }

    public enum ClientCompletionStatus { NotRun, Succeeded, Failed }

    public sealed class ClientExecution
    {
        private bool executed;
        private bool fatal;
        private ClientDiagnosticSeverity highest;
        private int suppressed;
        public ClientCompletionStatus CompletionStatus => !executed ? ClientCompletionStatus.NotRun : fatal ? ClientCompletionStatus.Failed : ClientCompletionStatus.Succeeded;
        public bool HasFatalError => fatal;
        public ClientDiagnosticSeverity HighestSeverity => highest;
        public int SuppressedDiagnosticCount => suppressed;
        internal void MarkExecuted() { executed = true; }
        internal void Fail() { executed = true; fatal = true; highest = ClientDiagnosticSeverity.Error; }
        internal void Observe(ClientDiagnosticSeverity severity) { executed = true; if ((int)severity > (int)highest) highest = severity; if (severity == ClientDiagnosticSeverity.Error) fatal = true; }
        internal void Suppress() { executed = true; suppressed = suppressed == int.MaxValue ? int.MaxValue : suppressed + 1; }
    }

    public enum ClientDuplicateVisibilityPolicy { PreserveAndDiagnose, RejectAnyDuplicate }
    public enum ClientUnknownVisibilityPolicy { PreserveUnresolved, Reject }

    public sealed class VisibilityPolicy
    {
        public VisibilityPolicy(int maxCells = 65536, int maxDiagnostics = 256, ClientDuplicateVisibilityPolicy duplicates = ClientDuplicateVisibilityPolicy.PreserveAndDiagnose, ClientUnknownVisibilityPolicy unknown = ClientUnknownVisibilityPolicy.PreserveUnresolved)
        {
            if (maxCells < 0 || maxDiagnostics < 0 || !Enum.IsDefined(typeof(ClientDuplicateVisibilityPolicy), duplicates) || !Enum.IsDefined(typeof(ClientUnknownVisibilityPolicy), unknown)) throw new ArgumentOutOfRangeException();
            MaxCells = maxCells; MaxDiagnostics = maxDiagnostics; Duplicates = duplicates; Unknown = unknown;
        }
        public int MaxCells { get; }
        public int MaxDiagnostics { get; }
        public ClientDuplicateVisibilityPolicy Duplicates { get; }
        public ClientUnknownVisibilityPolicy Unknown { get; }
    }

    public readonly struct VisibilityCell : IComparable<VisibilityCell>
    {
        public VisibilityCell(CellCoordinate coordinate, ClientVisibilityState state, long sourceOrdinal = 0)
        { if (sourceOrdinal < 0 || !Enum.IsDefined(typeof(ClientVisibilityState), state)) throw new ArgumentOutOfRangeException(); Coordinate = coordinate; State = state; SourceOrdinal = sourceOrdinal; }
        public CellCoordinate Coordinate { get; }
        public ClientVisibilityState State { get; }
        public long SourceOrdinal { get; }
        public int CompareTo(VisibilityCell other) { int c = Coordinate.CompareTo(other.Coordinate); return c != 0 ? c : SourceOrdinal.CompareTo(other.SourceOrdinal); }
    }

    public enum ClientVisibilityState { Visible, Fogged, Shrouded, Unknown }

    public sealed class VisibilitySnapshot
    {
        internal VisibilitySnapshot(IEnumerable<VisibilityCell> cells)
        { Cells = new ReadOnlyCollection<VisibilityCell>((cells ?? Enumerable.Empty<VisibilityCell>()).OrderBy(x => x.Coordinate).ThenBy(x => x.SourceOrdinal).ToList()); }
        public IReadOnlyList<VisibilityCell> Cells { get; }
        public bool TryGet(CellCoordinate coordinate, out ClientVisibilityState state)
        { foreach (VisibilityCell cell in Cells) if (cell.Coordinate.Equals(coordinate)) { state = cell.State; return true; } state = ClientVisibilityState.Unknown; return false; }
    }

    public sealed class VisibilitySnapshotResult
    {
        internal VisibilitySnapshotResult(VisibilitySnapshot snapshot, IEnumerable<ClientDiagnostic> diagnostics, ClientExecution execution)
        { Snapshot = snapshot; Diagnostics = new ReadOnlyCollection<ClientDiagnostic>((diagnostics ?? Enumerable.Empty<ClientDiagnostic>()).ToList()); Execution = execution; }
        public VisibilitySnapshot Snapshot { get; }
        public IReadOnlyList<ClientDiagnostic> Diagnostics { get; }
        public ClientExecution Execution { get; }
        public bool IsSuccess => Execution != null && Execution.CompletionStatus == ClientCompletionStatus.Succeeded;
    }

    public static class VisibilitySnapshotBuilder
    {
        public static VisibilitySnapshotResult Build(IEnumerable<VisibilityCell> source, VisibilityPolicy policy = null)
        {
            policy = policy ?? new VisibilityPolicy(); var list = new List<VisibilityCell>(); var diagnostics = new List<ClientDiagnostic>(); var execution = new ClientExecution(); var seen = new HashSet<CellCoordinate>();
            if (source == null) { Fail(diagnostics, execution, policy, ClientDiagnosticCode.InvalidPolicy, "visibility", "Visibility source is required."); return new VisibilitySnapshotResult(new VisibilitySnapshot(list), diagnostics, execution); }
            execution.MarkExecuted();
            foreach (VisibilityCell cell in source)
            {
                execution.MarkExecuted();
                if (list.Count >= policy.MaxCells) { Fail(diagnostics, execution, policy, ClientDiagnosticCode.BudgetExceeded, "visibility", "Visibility cell budget exceeded."); break; }
                if (!seen.Add(cell.Coordinate))
                {
                    if (policy.Duplicates == ClientDuplicateVisibilityPolicy.RejectAnyDuplicate) Fail(diagnostics, execution, policy, ClientDiagnosticCode.DuplicateVisibility, "visibility", "Duplicate visibility coordinate rejected.");
                    else Warn(diagnostics, execution, policy, ClientDiagnosticCode.DuplicateVisibility, "visibility", "Duplicate visibility coordinate preserved.");
                    continue;
                }
                if (cell.State == ClientVisibilityState.Unknown && policy.Unknown == ClientUnknownVisibilityPolicy.Reject) { Fail(diagnostics, execution, policy, ClientDiagnosticCode.UnknownVisibility, "visibility", "Unknown visibility rejected."); continue; }
                list.Add(cell);
            }
            list.Sort(); return new VisibilitySnapshotResult(new VisibilitySnapshot(list), diagnostics, execution);
        }
        private static void Fail(List<ClientDiagnostic> list, ClientExecution execution, VisibilityPolicy policy, ClientDiagnosticCode code, string stage, string message) { execution.Fail(); Add(list, execution, policy, new ClientDiagnostic(code, ClientDiagnosticSeverity.Error, stage, message)); }
        private static void Warn(List<ClientDiagnostic> list, ClientExecution execution, VisibilityPolicy policy, ClientDiagnosticCode code, string stage, string message) { execution.Observe(ClientDiagnosticSeverity.Warning); Add(list, execution, policy, new ClientDiagnostic(code, ClientDiagnosticSeverity.Warning, stage, message)); }
        private static void Add(List<ClientDiagnostic> list, ClientExecution execution, VisibilityPolicy policy, ClientDiagnostic diagnostic) { if (list.Count < policy.MaxDiagnostics) list.Add(diagnostic); else execution.Suppress(); }
    }

    public sealed class SelectionPolicy
    {
        public SelectionPolicy(int maxSelected = 256) { if (maxSelected < 0) throw new ArgumentOutOfRangeException(nameof(maxSelected)); MaxSelected = maxSelected; }
        public int MaxSelected { get; }
    }

    public sealed class SelectionState
    {
        public SelectionState(IEnumerable<EntityId> entities)
        { Entities = new ReadOnlyCollection<EntityId>((entities ?? Enumerable.Empty<EntityId>()).Distinct().OrderBy(x => x).ToList()); }
        public IReadOnlyList<EntityId> Entities { get; }
        public bool Contains(EntityId entity) => Entities.Contains(entity);
    }

    public sealed class SelectionResult
    {
        internal SelectionResult(SelectionState selection, IEnumerable<ClientDiagnostic> diagnostics, ClientExecution execution)
        { Selection = selection; Diagnostics = new ReadOnlyCollection<ClientDiagnostic>((diagnostics ?? Enumerable.Empty<ClientDiagnostic>()).ToList()); Execution = execution; }
        public SelectionState Selection { get; }
        public IReadOnlyList<ClientDiagnostic> Diagnostics { get; }
        public ClientExecution Execution { get; }
        public bool IsSuccess => Execution.CompletionStatus == ClientCompletionStatus.Succeeded;
    }

    public static class SelectionService
    {
        public static SelectionResult Replace(IEnumerable<EntityId> entities, SelectionPolicy policy = null)
        {
            policy = policy ?? new SelectionPolicy(); var diagnostics = new List<ClientDiagnostic>(); var execution = new ClientExecution(); var list = new List<EntityId>();
            if (entities == null) { Fail(diagnostics, execution, ClientDiagnosticCode.InvalidSelection, "selection", "Selection source is required."); return new SelectionResult(new SelectionState(list), diagnostics, execution); }
            execution.MarkExecuted();
            foreach (EntityId entity in entities)
            {
                execution.MarkExecuted(); if (!entity.IsValid) { Fail(diagnostics, execution, ClientDiagnosticCode.InvalidSelection, "selection", "Invalid entity cannot be selected.", entity); continue; }
                if (list.Contains(entity)) continue; if (list.Count >= policy.MaxSelected) { Fail(diagnostics, execution, ClientDiagnosticCode.SelectionBudgetExceeded, "selection", "Selection budget exceeded.", entity); break; } list.Add(entity);
            }
            list.Sort(); return new SelectionResult(new SelectionState(list), diagnostics, execution);
        }
        private static void Fail(List<ClientDiagnostic> list, ClientExecution execution, ClientDiagnosticCode code, string stage, string message, EntityId? entity = null) { execution.Fail(); list.Add(new ClientDiagnostic(code, ClientDiagnosticSeverity.Error, stage, message, entity)); }
    }

    public enum ClientPointerMode { Select, Move, Attack, Place }
    public readonly struct ClientScreenPoint
    {
        public ClientScreenPoint(int x, int y) { X = x; Y = y; }
        public int X { get; } public int Y { get; }
    }

    public sealed class IsometricPointerProfile
    {
        public IsometricPointerProfile(int tileWidth = 64, int tileHeight = 32, int viewportWidth = 1280, int viewportHeight = 720, int panX = 0, int panY = 0, int maxCells = 1048576)
        { if (tileWidth <= 0 || tileHeight <= 0 || viewportWidth <= 0 || viewportHeight <= 0 || maxCells < 0) throw new ArgumentOutOfRangeException(); TileWidth = tileWidth; TileHeight = tileHeight; ViewportWidth = viewportWidth; ViewportHeight = viewportHeight; PanX = panX; PanY = panY; MaxCells = maxCells; }
        public int TileWidth { get; } public int TileHeight { get; } public int ViewportWidth { get; } public int ViewportHeight { get; } public int PanX { get; } public int PanY { get; } public int MaxCells { get; }
    }

    public readonly struct ClientPointerResult
    {
        internal ClientPointerResult(bool success, CellCoordinate coordinate, IEnumerable<ClientDiagnostic> diagnostics, ClientExecution execution)
        { IsSuccess = success; Coordinate = coordinate; Diagnostics = new ReadOnlyCollection<ClientDiagnostic>((diagnostics ?? Enumerable.Empty<ClientDiagnostic>()).ToList()); Execution = execution; }
        public bool IsSuccess { get; } public CellCoordinate Coordinate { get; } public IReadOnlyList<ClientDiagnostic> Diagnostics { get; } public ClientExecution Execution { get; }
    }

    public static class IsometricPointerInterpreter
    {
        public static ClientPointerResult Resolve(ClientScreenPoint point, IsometricPointerProfile profile)
        {
            var diagnostics = new List<ClientDiagnostic>(); var execution = new ClientExecution();
            if (profile == null || point.X < 0 || point.Y < 0 || point.X >= (profile == null ? 0 : profile.ViewportWidth) || point.Y >= (profile == null ? 0 : profile.ViewportHeight)) { execution.Fail(); diagnostics.Add(new ClientDiagnostic(ClientDiagnosticCode.InvalidPointer, ClientDiagnosticSeverity.Error, "pointer", "Pointer lies outside the explicit viewport.")); return new ClientPointerResult(false, default(CellCoordinate), diagnostics, execution); }
            try
            {
                long sx = checked((long)point.X - profile.ViewportWidth / 2 - profile.PanX); long sy = checked((long)point.Y - profile.ViewportHeight / 2 - profile.PanY);
                long x = FloorDiv(sy, profile.TileHeight) + FloorDiv(sx, profile.TileWidth); long y = FloorDiv(sy, profile.TileHeight) - FloorDiv(sx, profile.TileWidth);
                if (Math.Abs(x) > profile.MaxCells || Math.Abs(y) > profile.MaxCells) { execution.Fail(); diagnostics.Add(new ClientDiagnostic(ClientDiagnosticCode.BudgetExceeded, ClientDiagnosticSeverity.Error, "pointer", "Pointer coordinate exceeded the bounded client domain.")); return new ClientPointerResult(false, default(CellCoordinate), diagnostics, execution); }
                execution.MarkExecuted(); return new ClientPointerResult(true, new CellCoordinate(checked((int)x), checked((int)y)), diagnostics, execution);
            }
            catch (OverflowException) { execution.Fail(); diagnostics.Add(new ClientDiagnostic(ClientDiagnosticCode.ProjectionArithmeticOverflow, ClientDiagnosticSeverity.Error, "pointer", "Pointer projection exceeded checked arithmetic.")); return new ClientPointerResult(false, default(CellCoordinate), diagnostics, execution); }
        }
        private static long FloorDiv(long value, long divisor) { long q = value / divisor; long r = value % divisor; return r < 0 ? q - 1 : q; }
    }

    public sealed class ClientCommandPolicy
    {
        public ClientCommandPolicy(long firstCommandId = 1) { if (firstCommandId < 0) throw new ArgumentOutOfRangeException(nameof(firstCommandId)); NextCommandId = firstCommandId; }
        internal long NextCommandId { get; set; }
    }

    public sealed class ClientCommandResult
    {
        internal ClientCommandResult(IEnumerable<CommandAcceptanceResult> results, IEnumerable<ClientDiagnostic> diagnostics, ClientExecution execution)
        { Results = new ReadOnlyCollection<CommandAcceptanceResult>((results ?? Enumerable.Empty<CommandAcceptanceResult>()).ToList()); Diagnostics = new ReadOnlyCollection<ClientDiagnostic>((diagnostics ?? Enumerable.Empty<ClientDiagnostic>()).ToList()); Execution = execution; }
        public IReadOnlyList<CommandAcceptanceResult> Results { get; } public IReadOnlyList<ClientDiagnostic> Diagnostics { get; } public ClientExecution Execution { get; } public bool IsSuccess => Execution.CompletionStatus == ClientCompletionStatus.Succeeded && Results.All(x => x.IsAccepted);
    }

    public static class ClientCommandGateway
    {
        public static ClientCommandResult Submit(CommandQueue queue, SelectionState selection, CommandKind kind, CommandTarget target, long issuedTick, QueueMode queueMode = QueueMode.Replace, ClientCommandPolicy policy = null)
        {
            policy = policy ?? new ClientCommandPolicy(); var results = new List<CommandAcceptanceResult>(); var diagnostics = new List<ClientDiagnostic>(); var execution = new ClientExecution();
            if (queue == null || selection == null || selection.Entities.Count == 0 || !Enum.IsDefined(typeof(CommandKind), kind) || !Enum.IsDefined(typeof(QueueMode), queueMode)) { execution.Fail(); diagnostics.Add(new ClientDiagnostic(ClientDiagnosticCode.InvalidCommand, ClientDiagnosticSeverity.Error, "command", "A bounded queue, valid selection and explicit command policy are required.")); return new ClientCommandResult(results, diagnostics, execution); }
            foreach (EntityId entity in selection.Entities)
            {
                execution.MarkExecuted(); CommandRequest request = new CommandRequest(policy.NextCommandId++, entity, CommandSource.Human, kind, target, queueMode, issuedTick); CommandAcceptanceResult result = queue.Enqueue(request); results.Add(result); if (!result.IsAccepted) { execution.Fail(); diagnostics.Add(new ClientDiagnostic(ClientDiagnosticCode.CommandRejected, ClientDiagnosticSeverity.Error, "command", "Simulation command queue rejected the request.", entity)); }
            }
            return new ClientCommandResult(results, diagnostics, execution);
        }
    }

    public readonly struct HudSnapshot
    {
        public HudSnapshot(long tick, int selectedCount, int visibleCount, int queuedCommands, int credits, bool lowPower, string autonomyLabel)
        { Tick = tick; SelectedCount = selectedCount; VisibleCount = visibleCount; QueuedCommands = queuedCommands; Credits = credits; LowPower = lowPower; AutonomyLabel = autonomyLabel ?? string.Empty; }
        public long Tick { get; } public int SelectedCount { get; } public int VisibleCount { get; } public int QueuedCommands { get; } public int Credits { get; } public bool LowPower { get; } public string AutonomyLabel { get; }
    }

    public static class HudSnapshotBuilder
    {
        public static HudSnapshot Build(SimulationReadSnapshot snapshot, SelectionState selection, VisibilitySnapshot visibility, CommandQueue commands, int credits = 0, bool lowPower = false, string autonomyLabel = "Manual")
        {
            int visible = 0; if (snapshot != null) foreach (SnapshotEntity entity in snapshot.Entities) { ClientVisibilityState state; if (visibility == null || !visibility.TryGet(new CellCoordinate(entity.Position.HasValue ? entity.Position.Value.X : 0, entity.Position.HasValue ? entity.Position.Value.Y : 0), out state) || state == ClientVisibilityState.Visible) visible++; }
            return new HudSnapshot(snapshot == null ? 0 : snapshot.Tick, selection == null ? 0 : selection.Entities.Count, visible, commands == null ? 0 : commands.SnapshotCanonical().Count, credits, lowPower, autonomyLabel);
        }
    }

    public readonly struct ProductionPanelEntry
    {
        public ProductionPanelEntry(ProductionDefinitionDescriptor definition, ProductionAvailabilityResult availability) { Definition = definition; Availability = availability; }
        public ProductionDefinitionDescriptor Definition { get; } public ProductionAvailabilityResult Availability { get; }
    }
    public sealed class ProductionPanelSnapshot
    {
        internal ProductionPanelSnapshot(IEnumerable<ProductionPanelEntry> entries) { Entries = new ReadOnlyCollection<ProductionPanelEntry>((entries ?? Enumerable.Empty<ProductionPanelEntry>()).OrderBy(x => x.Definition.Raw.SourceOrdinal).ToList()); }
        public IReadOnlyList<ProductionPanelEntry> Entries { get; }
    }
    public static class ProductionPanelBuilder
    {
        public static ProductionPanelSnapshot Build(IEnumerable<ProductionDefinitionDescriptor> definitions, int techLevel, IEnumerable<string> capabilities, long existingCount, ProductionReadLimits limits)
        { var result = new List<ProductionPanelEntry>(); foreach (ProductionDefinitionDescriptor definition in definitions ?? Enumerable.Empty<ProductionDefinitionDescriptor>()) { var query = new ProductionAvailabilityQuery(definition, techLevel, capabilities, existingCount, ProductionAvailabilityProfile.ExplicitCapabilitiesAndLimits); result.Add(new ProductionPanelEntry(definition, ProductionAvailabilityResult.Evaluate(query, limits))); } return new ProductionPanelSnapshot(result); }
    }

    public readonly struct PlacementPreview
    {
        public PlacementPreview(CellCoordinate coordinate, bool valid, string reason) { Coordinate = coordinate; IsValid = valid; Reason = reason ?? string.Empty; }
        public CellCoordinate Coordinate { get; } public bool IsValid { get; } public string Reason { get; }
    }
    public static class PlacementPreviewBuilder
    {
        public static PlacementPreview Build(CellCoordinate coordinate, bool occupied, bool inBounds) { return new PlacementPreview(coordinate, inBounds && !occupied, inBounds ? occupied ? "Occupied" : "Available" : "OutOfBounds"); }
    }

    public enum LightingProfile { Day, Night, Storm }
    public enum WeatherProfile { Clear, Rain, Snow, Sandstorm }
    public readonly struct EnvironmentPresentationProfile
    {
        public EnvironmentPresentationProfile(LightingProfile lighting, WeatherProfile weather, int intensity) { if (intensity < 0 || intensity > 100) throw new ArgumentOutOfRangeException(nameof(intensity)); Lighting = lighting; Weather = weather; Intensity = intensity; }
        public LightingProfile Lighting { get; } public WeatherProfile Weather { get; } public int Intensity { get; }
    }
}
