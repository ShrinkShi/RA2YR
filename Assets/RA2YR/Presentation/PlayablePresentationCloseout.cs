using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RA2YR.Simulation;

namespace RA2YR.Presentation
{
    public enum PlayablePresentationDiagnosticCode { InvalidPolicy, SimulationDivergence, PresentationMutation, EntityBudgetExceeded, NoProgress }
    public sealed class PlayablePresentationDiagnostic
    {
        public PlayablePresentationDiagnostic(PlayablePresentationDiagnosticCode code, string stage, string message, long tick = -1) { Code = code; Stage = stage ?? string.Empty; Message = message ?? string.Empty; Tick = tick; }
        public PlayablePresentationDiagnosticCode Code { get; } public string Stage { get; } public string Message { get; } public long Tick { get; }
    }
    public enum PlayablePresentationCompletionStatus { NotRun, Succeeded, Failed }
    public sealed class PlayablePresentationExecution
    {
        internal PlayablePresentationExecution(PlayablePresentationCompletionStatus status, bool fatal) { CompletionStatus = status; HasFatalError = fatal; }
        public PlayablePresentationCompletionStatus CompletionStatus { get; } public bool HasFatalError { get; } public bool IsSuccess => CompletionStatus == PlayablePresentationCompletionStatus.Succeeded && !HasFatalError;
    }

    public readonly struct PresentationCadenceProfile
    {
        public PresentationCadenceProfile(int simulationTicksPerSecond, int renderFramesPerSecond)
        { if (simulationTicksPerSecond <= 0 || renderFramesPerSecond <= 0 || simulationTicksPerSecond > 1000 || renderFramesPerSecond > 1000) throw new ArgumentOutOfRangeException(); SimulationTicksPerSecond = simulationTicksPerSecond; RenderFramesPerSecond = renderFramesPerSecond; }
        public int SimulationTicksPerSecond { get; } public int RenderFramesPerSecond { get; }
        public bool ShouldRender(long completedTick)
        { if (completedTick <= 0) return false; long before = checked((completedTick - 1) * (long)RenderFramesPerSecond); long after = checked(completedTick * (long)RenderFramesPerSecond); return after / SimulationTicksPerSecond > before / SimulationTicksPerSecond; }
    }

    public readonly struct PlayablePresentationPolicy
    {
        public PlayablePresentationPolicy(int entityCount = 500, int simulationTicks = 3, PresentationCadenceProfile cadence = default(PresentationCadenceProfile), int maxDescriptors = 65536)
        { if (entityCount <= 0 || entityCount > 10000 || simulationTicks <= 0 || simulationTicks > 100000 || maxDescriptors < 0) throw new ArgumentOutOfRangeException(); EntityCount = entityCount; SimulationTicks = simulationTicks; Cadence = cadence.SimulationTicksPerSecond <= 0 ? new PresentationCadenceProfile(60, 60) : cadence; MaxDescriptors = maxDescriptors; }
        public int EntityCount { get; } public int SimulationTicks { get; } public PresentationCadenceProfile Cadence { get; } public int MaxDescriptors { get; }
    }

    public sealed class PlayablePresentationRunResult
    {
        internal PlayablePresentationRunResult(PlayablePresentationExecution execution, int ticks, int renderedFrames, long presentedEntities, string simulationHash, string presentationHash, IEnumerable<PlayablePresentationDiagnostic> diagnostics)
        { Execution = execution; TicksCompleted = ticks; RenderedFrames = renderedFrames; PresentedEntities = presentedEntities; SimulationHash = simulationHash ?? string.Empty; PresentationHash = presentationHash ?? string.Empty; Diagnostics = new ReadOnlyCollection<PlayablePresentationDiagnostic>((diagnostics ?? Enumerable.Empty<PlayablePresentationDiagnostic>()).ToList()); }
        public PlayablePresentationExecution Execution { get; } public int TicksCompleted { get; } public int RenderedFrames { get; } public long PresentedEntities { get; } public string SimulationHash { get; } public string PresentationHash { get; } public IReadOnlyList<PlayablePresentationDiagnostic> Diagnostics { get; } public bool IsSuccess => Execution.IsSuccess;
    }

    public sealed class PlayablePresentationCloseoutHarness
    {
        public PlayablePresentationRunResult Run(PlayablePresentationPolicy policy)
        {
            var diagnostics = new List<PlayablePresentationDiagnostic>(); var authority = CreateWorld(policy.EntityCount); var observed = CreateWorld(policy.EntityCount); var execution = new PlayablePresentationExecution(PlayablePresentationCompletionStatus.Succeeded, false); int frames = 0; long presented = 0; string presentationHash = string.Empty;
            try
            {
                for (int tick = 0; tick < policy.SimulationTicks; tick++)
                {
                    authority.AdvanceTick(); observed.AdvanceTick();
                    if (!policy.Cadence.ShouldRender(observed.Tick)) continue;
                    if (policy.EntityCount > policy.MaxDescriptors) { diagnostics.Add(new PlayablePresentationDiagnostic(PlayablePresentationDiagnosticCode.EntityBudgetExceeded, "presentation", "Descriptor budget exceeded.", observed.Tick)); return Failed(diagnostics, authority, observed, tick + 1, frames, presented, presentationHash); }
                    string before = observed.ComputeStateHash(); PresentationSnapshot snapshot = PresentationSnapshotAssembler.Assemble(observed.CaptureSnapshot(), Descriptors(observed, policy.MaxDescriptors), providers: new[] { new SyntheticProvider() }, policy: new PresentationAssemblyPolicy(policy.MaxDescriptors));
                    if (!snapshot.IsSuccess) { diagnostics.Add(new PlayablePresentationDiagnostic(PlayablePresentationDiagnosticCode.EntityBudgetExceeded, "presentation", "Presentation snapshot failed.", observed.Tick)); return Failed(diagnostics, authority, observed, tick + 1, frames, presented, presentationHash); }
                    if (!string.Equals(before, observed.ComputeStateHash(), StringComparison.Ordinal)) { diagnostics.Add(new PlayablePresentationDiagnostic(PlayablePresentationDiagnosticCode.PresentationMutation, "presentation", "Presentation changed simulation state.", observed.Tick)); return Failed(diagnostics, authority, observed, tick + 1, frames, presented, presentationHash); }
                    frames++; presented = checked(presented + snapshot.Entities.Count); presentationHash = snapshot.CanonicalHash;
                }
                if (authority.Tick != observed.Tick || !string.Equals(authority.ComputeStateHash(), observed.ComputeStateHash(), StringComparison.Ordinal)) { diagnostics.Add(new PlayablePresentationDiagnostic(PlayablePresentationDiagnosticCode.SimulationDivergence, "equivalence", "Presentation and headless simulation diverged.", observed.Tick)); return Failed(diagnostics, authority, observed, policy.SimulationTicks, frames, presented, presentationHash); }
                if (policy.SimulationTicks > 0 && frames == 0) { diagnostics.Add(new PlayablePresentationDiagnostic(PlayablePresentationDiagnosticCode.NoProgress, "cadence", "Cadence produced no presentation frame.", observed.Tick)); return Failed(diagnostics, authority, observed, policy.SimulationTicks, frames, presented, presentationHash); }
                return new PlayablePresentationRunResult(execution, policy.SimulationTicks, frames, presented, authority.ComputeStateHash(), presentationHash, diagnostics);
            }
            catch (OverflowException) { diagnostics.Add(new PlayablePresentationDiagnostic(PlayablePresentationDiagnosticCode.EntityBudgetExceeded, "arithmetic", "Playable presentation arithmetic exceeded its checked contract.")); return Failed(diagnostics, authority, observed, 0, frames, presented, presentationHash); }
        }

        private static PlayablePresentationRunResult Failed(List<PlayablePresentationDiagnostic> diagnostics, SimulationWorld authority, SimulationWorld observed, int ticks, int frames, long presented, string presentationHash)
        { return new PlayablePresentationRunResult(new PlayablePresentationExecution(PlayablePresentationCompletionStatus.Failed, true), ticks, frames, presented, authority.ComputeStateHash(), presentationHash, diagnostics); }

        private static SimulationWorld CreateWorld(int count)
        { var world = new SimulationWorld(count); for (int i = 0; i < count; i++) { EntityId entity = world.CreateEntity(); world.Positions.Set(entity, new PositionComponent(i % 256, i / 256)); world.Health.Set(entity, new HealthComponent(100, 100)); world.Missions.Set(entity, new MissionStateComponent(MissionKind.Idle, i)); } return world; }

        private static IEnumerable<PresentationEntityDescriptor> Descriptors(SimulationWorld world, int max)
        { int ordinal = 0; foreach (SnapshotEntity entity in world.CaptureSnapshot().Entities) { if (ordinal >= max) yield break; PositionComponent position = entity.Position.HasValue ? entity.Position.Value : new PositionComponent(0, 0); yield return new PresentationEntityDescriptor(entity.Id, new VisualAssetId("synthetic/unit"), PresentationRenderPass.Vehicle, new PresentationPosition(position.X, position.Y), ordinal++); } }

        private sealed class SyntheticProvider : IVisualAssetProvider
        { public string ProviderId => "synthetic"; public VisualAssetProviderResult Resolve(VisualAssetId assetId) { return new VisualAssetProviderResult(VisualAssetProviderResolutionStatus.Resolved, ProviderId, assetId); } }
    }
}
