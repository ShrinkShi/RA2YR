using System;
using System.Collections.Generic;
using System.Linq;
using RA2YR.Presentation;
using RA2YR.Simulation;
using UnityEngine;
using SimulationQueueMode = RA2YR.Simulation.QueueMode;

namespace RA2YR.UnityIntegration
{
    public sealed class UnityInteractiveClientPolicy
    {
        public UnityInteractiveClientPolicy(int maxPickTargets = 65536, int maxSelection = 256)
        { if (maxPickTargets < 0 || maxSelection < 0) throw new ArgumentOutOfRangeException(); MaxPickTargets = maxPickTargets; MaxSelection = maxSelection; }
        public int MaxPickTargets { get; } public int MaxSelection { get; }
    }

    public readonly struct UnityPickTarget
    {
        public UnityPickTarget(EntityId entity, CellCoordinate cell, string stableIdentity, int sourceOrdinal = 0)
        { if (!entity.IsValid || string.IsNullOrWhiteSpace(stableIdentity) || sourceOrdinal < 0) throw new ArgumentException(); Entity = entity; Cell = cell; StableIdentity = stableIdentity; SourceOrdinal = sourceOrdinal; }
        public EntityId Entity { get; } public CellCoordinate Cell { get; } public string StableIdentity { get; } public int SourceOrdinal { get; }
    }

    public sealed class UnityInteractiveClient : MonoBehaviour
    {
        private readonly List<UnityPickTarget> pickTargets = new List<UnityPickTarget>();
        private UnityInteractiveClientPolicy policy;
        private IsometricPointerProfile pointerProfile;
        private VisibilitySnapshot visibility;
        private SelectionState selection = new SelectionState(Array.Empty<EntityId>());
        private CommandQueue commandQueue;
        private HudSnapshot hud;
        public EnvironmentPresentationProfile Environment { get; private set; }
        public PlacementPreview CurrentPlacement { get; private set; }
        public SelectionState Selection => selection;
        public VisibilitySnapshot Visibility => visibility;
        public HudSnapshot Hud => hud;
        public IReadOnlyList<UnityPickTarget> PickTargets => pickTargets.AsReadOnly();

        public static UnityInteractiveClient CreateSynthetic(string name = "SyntheticInteractiveClient")
        { GameObject root = new GameObject(name); var client = root.AddComponent<UnityInteractiveClient>(); client.Configure(new UnityInteractiveClientPolicy(), new IsometricPointerProfile()); return client; }

        public void Configure(UnityInteractiveClientPolicy clientPolicy, IsometricPointerProfile profile, CommandQueue queue = null)
        { policy = clientPolicy ?? throw new ArgumentNullException(nameof(clientPolicy)); pointerProfile = profile ?? throw new ArgumentNullException(nameof(profile)); commandQueue = queue; }

        public void SetVisibility(VisibilitySnapshot snapshot) { visibility = snapshot; }

        public bool RegisterPickTarget(UnityPickTarget target)
        { if (policy == null) policy = new UnityInteractiveClientPolicy(); if (pickTargets.Count >= policy.MaxPickTargets) return false; if (pickTargets.Any(x => x.Entity.Equals(target.Entity))) return false; pickTargets.Add(target); pickTargets.Sort((a, b) => { int c = a.Cell.CompareTo(b.Cell); return c != 0 ? c : a.SourceOrdinal.CompareTo(b.SourceOrdinal); }); return true; }

        public ClientPointerResult ResolvePointer(Vector2 screenPoint)
        { if (pointerProfile == null) pointerProfile = new IsometricPointerProfile(); return IsometricPointerInterpreter.Resolve(new ClientScreenPoint(Mathf.RoundToInt(screenPoint.x), Mathf.RoundToInt(screenPoint.y)), pointerProfile); }

        public SelectionResult SelectAt(Vector2 screenPoint, bool additive = false)
        {
            ClientPointerResult pointer = ResolvePointer(screenPoint); if (!pointer.IsSuccess) return SelectionService.Replace(null, new SelectionPolicy(policy == null ? 0 : policy.MaxSelection));
            var hits = pickTargets.Where(x => x.Cell.Equals(pointer.Coordinate)).OrderBy(x => x.SourceOrdinal).ThenBy(x => x.Entity).Select(x => x.Entity).ToList();
            if (additive) hits = selection.Entities.Concat(hits).Distinct().ToList();
            SelectionResult result = SelectionService.Replace(hits, new SelectionPolicy(policy == null ? 256 : policy.MaxSelection)); if (result.IsSuccess) selection = result.Selection; return result;
        }

        public void SetSelection(SelectionState value) { selection = value ?? throw new ArgumentNullException(nameof(value)); }

        public ClientCommandResult SubmitCommand(CommandKind kind, CellCoordinate? cell, EntityId? target, long tick, SimulationQueueMode queueMode = SimulationQueueMode.Replace)
        {
            if (commandQueue == null) { return ClientCommandGateway.Submit(null, selection, kind, new CommandTarget(cell, target), tick, queueMode); }
            return ClientCommandGateway.Submit(commandQueue, selection, kind, new CommandTarget(cell, target), tick, queueMode);
        }

        public void RefreshHud(SimulationReadSnapshot snapshot, int credits = 0, bool lowPower = false, string autonomyLabel = "Manual")
        { hud = HudSnapshotBuilder.Build(snapshot, selection, visibility, commandQueue, credits, lowPower, autonomyLabel); }

        public ProductionPanelSnapshot BuildProductionPanel(IEnumerable<ProductionDefinitionDescriptor> definitions, int techLevel, IEnumerable<string> capabilities, long existingCount, ProductionReadLimits limits)
        { return ProductionPanelBuilder.Build(definitions, techLevel, capabilities, existingCount, limits); }

        public PlacementPreview PreviewPlacement(CellCoordinate coordinate, bool occupied, bool inBounds)
        { CurrentPlacement = PlacementPreviewBuilder.Build(coordinate, occupied, inBounds); return CurrentPlacement; }

        public void SetEnvironment(EnvironmentPresentationProfile profile) { Environment = profile; }

        public void ClearTargets() { pickTargets.Clear(); selection = new SelectionState(Array.Empty<EntityId>()); }
    }
}
