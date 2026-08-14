using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RA2YR.Presentation;
using RA2YR.Simulation;

namespace RA2YR.Tests.EditMode
{
    public sealed class M6C7InteractiveClientTests
    {
        [Test] public void VisibilityIsSortedByCoordinate() { var r = VisibilitySnapshotBuilder.Build(new[] { V(2, 1, ClientVisibilityState.Visible, 1), V(0, 0, ClientVisibilityState.Fogged, 0) }); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Snapshot.Cells[0].Coordinate, Is.EqualTo(new CellCoordinate(0, 0))); }
        [Test] public void UnknownVisibilityCanBePreservedExplicitly() { var r = VisibilitySnapshotBuilder.Build(new[] { V(0, 0, ClientVisibilityState.Unknown) }); Assert.That(r.IsSuccess, Is.True); }
        [Test] public void UnknownVisibilityRejectsWithoutFallback() { var r = VisibilitySnapshotBuilder.Build(new[] { V(0, 0, ClientVisibilityState.Unknown) }, new VisibilityPolicy(unknown: ClientUnknownVisibilityPolicy.Reject)); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Diagnostics.Any(x => x.Code == ClientDiagnosticCode.UnknownVisibility), Is.True); }
        [Test] public void DuplicateVisibilityIsDiagnosed() { var r = VisibilitySnapshotBuilder.Build(new[] { V(0, 0, ClientVisibilityState.Visible), V(0, 0, ClientVisibilityState.Fogged) }); Assert.That(r.Diagnostics.Any(x => x.Code == ClientDiagnosticCode.DuplicateVisibility), Is.True); }
        [Test] public void DuplicateVisibilityCanFailClosed() { var r = VisibilitySnapshotBuilder.Build(new[] { V(0, 0, ClientVisibilityState.Visible), V(0, 0, ClientVisibilityState.Fogged) }, new VisibilityPolicy(duplicates: ClientDuplicateVisibilityPolicy.RejectAnyDuplicate)); Assert.That(r.IsSuccess, Is.False); }
        [Test] public void VisibilityBudgetStopsLazySource() { int consumed = 0; IEnumerable<VisibilityCell> Lazy() { consumed++; yield return V(0, 0, ClientVisibilityState.Visible); consumed++; yield return V(1, 0, ClientVisibilityState.Visible); consumed++; yield return V(2, 0, ClientVisibilityState.Visible); } var r = VisibilitySnapshotBuilder.Build(Lazy(), new VisibilityPolicy(maxCells: 2)); Assert.That(r.IsSuccess, Is.False); Assert.That(consumed, Is.EqualTo(3)); }
        [Test] public void VisibilityZeroDiagnosticBudgetStillFails() { var r = VisibilitySnapshotBuilder.Build(new[] { V(0, 0, ClientVisibilityState.Unknown) }, new VisibilityPolicy(maxDiagnostics: 0, unknown: ClientUnknownVisibilityPolicy.Reject)); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Execution.SuppressedDiagnosticCount, Is.GreaterThan(0)); }

        [Test] public void SelectionIsDeterministicAndUnique() { var r = SelectionService.Replace(new[] { E(2), E(1), E(2) }); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Selection.Entities.Select(x => x.Index), Is.EqualTo(new[] { 0, 1 })); }
        [Test] public void SelectionRejectsInvalidEntity() { var r = SelectionService.Replace(new[] { new EntityId(-1, 0) }); Assert.That(r.IsSuccess, Is.False); }
        [Test] public void SelectionBudgetFailsClosed() { var r = SelectionService.Replace(new[] { E(1), E(2) }, new SelectionPolicy(1)); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Selection.Entities, Has.Count.EqualTo(1)); }
        [Test] public void SelectionNullFailsClosed() { var r = SelectionService.Replace(null); Assert.That(r.IsSuccess, Is.False); }

        [Test] public void PointerProjectionIsExplicit() { var r = IsometricPointerInterpreter.Resolve(new ClientScreenPoint(640, 360), new IsometricPointerProfile()); Assert.That(r.IsSuccess, Is.True); Assert.That(r.Coordinate, Is.EqualTo(new CellCoordinate(0, 0))); }
        [Test] public void PointerOutsideViewportFails() { var r = IsometricPointerInterpreter.Resolve(new ClientScreenPoint(-1, 1), new IsometricPointerProfile()); Assert.That(r.IsSuccess, Is.False); }
        [Test] public void PointerUsesPanWithoutSimulationMutation() { var r = IsometricPointerInterpreter.Resolve(new ClientScreenPoint(672, 360), new IsometricPointerProfile(panX: 32)); Assert.That(r.IsSuccess, Is.True); }
        [Test] public void PointerProfileRejectsInvalidTileSize() { Assert.That(() => new IsometricPointerProfile(tileWidth: 0), Throws.TypeOf<ArgumentOutOfRangeException>()); }

        [Test] public void HumanCommandGatewayUsesSelectedEntities() { var q = new CommandQueue(); var s = SelectionService.Replace(new[] { E(1), E(2) }).Selection; var r = ClientCommandGateway.Submit(q, s, CommandKind.Move, new CommandTarget(new CellCoordinate(3, 4), null), 7); Assert.That(r.IsSuccess, Is.True); Assert.That(q.SnapshotCanonical(), Has.Count.EqualTo(2)); Assert.That(q.SnapshotCanonical().All(x => x.Source == CommandSource.Human), Is.True); }
        [Test] public void CommandGatewayRequiresSelection() { var r = ClientCommandGateway.Submit(new CommandQueue(), new SelectionState(Array.Empty<EntityId>()), CommandKind.Move, new CommandTarget(), 0); Assert.That(r.IsSuccess, Is.False); }
        [Test] public void CommandGatewayPreservesQueueRejection() { var q = new CommandQueue(1); q.Enqueue(new CommandRequest(0, E(1), CommandSource.Human, CommandKind.Stop, new CommandTarget(), QueueMode.Append, 0)); var s = SelectionService.Replace(new[] { E(1) }).Selection; var r = ClientCommandGateway.Submit(q, s, CommandKind.Move, new CommandTarget(new CellCoordinate(1, 1), null), 0, QueueMode.Append); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Diagnostics.Any(x => x.Code == ClientDiagnosticCode.CommandRejected), Is.True); }
        [Test] public void CommandIdsAreDeterministicWithinPolicy() { var q = new CommandQueue(); var s = SelectionService.Replace(new[] { E(1) }).Selection; var r = ClientCommandGateway.Submit(q, s, CommandKind.Stop, new CommandTarget(), 2, policy: new ClientCommandPolicy(41)); Assert.That(r.Results[0].Request.CommandId, Is.EqualTo(41)); }

        [Test] public void HudCountsVisibleEntitiesAndCommands() { var world = new SimulationWorld(2); var e = world.CreateEntity(); world.Positions.Set(e, new PositionComponent(1, 2)); var q = new CommandQueue(); ClientCommandGateway.Submit(q, SelectionService.Replace(new[] { e }).Selection, CommandKind.Stop, new CommandTarget(), 0); var hud = HudSnapshotBuilder.Build(world.CaptureSnapshot(), SelectionService.Replace(new[] { e }).Selection, VisibilitySnapshotBuilder.Build(new[] { V(1, 2, ClientVisibilityState.Visible) }).Snapshot, q, 100); Assert.That(hud.SelectedCount, Is.EqualTo(1)); Assert.That(hud.VisibleCount, Is.EqualTo(1)); Assert.That(hud.QueuedCommands, Is.EqualTo(1)); Assert.That(hud.Credits, Is.EqualTo(100)); }
        [Test] public void HudTreatsMissingVisibilityAsVisibleCandidate() { var world = new SimulationWorld(1); var e = world.CreateEntity(); world.Positions.Set(e, new PositionComponent(0, 0)); var hud = HudSnapshotBuilder.Build(world.CaptureSnapshot(), new SelectionState(new[] { e }), null, null); Assert.That(hud.VisibleCount, Is.EqualTo(1)); }
        [Test] public void HudPreservesAutonomyLabelAsPresentationOnly() { var hud = HudSnapshotBuilder.Build(null, null, null, null, autonomyLabel: "Assisted"); Assert.That(hud.AutonomyLabel, Is.EqualTo("Assisted")); }

        [Test] public void ProductionPanelUsesSimulationAvailability() { var raw = new ProductionDefinitionRaw(2, "Tank", "Vehicle", 1, 100, 10, -1, new[] { "Factory" }); var def = new ProductionDefinitionDescriptor(raw, "synthetic"); var p = ProductionPanelBuilder.Build(new[] { def }, 1, new[] { "Factory" }, 0, ProductionReadLimits.Default); Assert.That(p.Entries, Has.Count.EqualTo(1)); Assert.That(p.Entries[0].Availability.IsRequestable, Is.True); }
        [Test] public void ProductionPanelKeepsBlockedEntryVisible() { var raw = new ProductionDefinitionRaw(1, "Tank", "Vehicle", 2, 100, 10, -1, Array.Empty<string>()); var p = ProductionPanelBuilder.Build(new[] { new ProductionDefinitionDescriptor(raw, "synthetic") }, 1, Array.Empty<string>(), 0, ProductionReadLimits.Default); Assert.That(p.Entries[0].Availability.IsVisible, Is.True); Assert.That(p.Entries[0].Availability.IsRequestable, Is.False); }
        [Test] public void PlacementPreviewDoesNotMutateOccupancy() { var p = PlacementPreviewBuilder.Build(new CellCoordinate(2, 3), true, true); Assert.That(p.IsValid, Is.False); Assert.That(p.Reason, Is.EqualTo("Occupied")); }
        [Test] public void PlacementPreviewRejectsOutOfBounds() { var p = PlacementPreviewBuilder.Build(new CellCoordinate(2, 3), false, false); Assert.That(p.IsValid, Is.False); Assert.That(p.Reason, Is.EqualTo("OutOfBounds")); }
        [Test] public void EnvironmentProfileIsExplicit() { var p = new EnvironmentPresentationProfile(LightingProfile.Night, WeatherProfile.Rain, 50); Assert.That(p.Lighting, Is.EqualTo(LightingProfile.Night)); Assert.That(p.Weather, Is.EqualTo(WeatherProfile.Rain)); }
        [Test] public void EnvironmentProfileBoundsIntensity() { Assert.That(() => new EnvironmentPresentationProfile(LightingProfile.Day, WeatherProfile.Clear, 101), Throws.TypeOf<ArgumentOutOfRangeException>()); }
        [Test] public void PresentationAssemblyRemainsUnityFree() { var names = typeof(HudSnapshot).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray(); Assert.That(names.Any(x => x != null && x.StartsWith("Unity", StringComparison.Ordinal)), Is.False); }

        private static VisibilityCell V(int x, int y, ClientVisibilityState state, long ordinal = 0) { return new VisibilityCell(new CellCoordinate(x, y), state, ordinal); }
        private static EntityId E(int number) { return new EntityId(number - 1, 1); }
    }
}
