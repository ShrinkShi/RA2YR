using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RA2YR.Simulation;

namespace RA2YR.Presentation
{
    public static class PresentationSnapshotAssembler
    {
        public static PresentationSnapshot Assemble(
            SimulationReadSnapshot simulationSnapshot,
            IEnumerable<PresentationEntityDescriptor> descriptors,
            PresentationSnapshot previous = null,
            IEnumerable<IVisualAssetProvider> providers = null,
            PresentationAssemblyPolicy policy = null)
        {
            policy = policy ?? new PresentationAssemblyPolicy();
            var diagnostics = new List<PresentationDiagnostic>();
            var execution = new PresentationExecutionState();
            var entitySet = new HashSet<EntityId>();
            if (simulationSnapshot == null)
            {
                Fail(diagnostics, execution, policy, PresentationDiagnosticCode.SnapshotSourceMissing, "assembly", "A simulation snapshot is required.");
                return new PresentationSnapshot(0, Array.Empty<PresentationEntityDescriptor>(), Array.Empty<PresentationEntityChange>(), diagnostics, execution);
            }

            foreach (SnapshotEntity entity in simulationSnapshot.Entities ?? Array.Empty<SnapshotEntity>())
                entitySet.Add(entity.Id);

            var collected = new List<PresentationEntityDescriptor>();
            if (descriptors == null)
            {
                Fail(diagnostics, execution, policy, PresentationDiagnosticCode.InvalidPolicy, "assembly", "A descriptor enumerable is required.");
                return new PresentationSnapshot(simulationSnapshot.Tick, collected, Array.Empty<PresentationEntityChange>(), diagnostics, execution);
            }

            var providerList = (providers ?? Array.Empty<IVisualAssetProvider>()).ToArray();
            foreach (IVisualAssetProvider provider in providerList)
            {
                if (provider == null || string.IsNullOrEmpty(provider.ProviderId))
                    Fail(diagnostics, execution, policy, PresentationDiagnosticCode.InvalidVisualAssetProvider, "provider", "A provider must have a stable non-empty identity.");
            }

            foreach (PresentationEntityDescriptor descriptor in descriptors)
            {
                execution.MarkExecuted();
                if (collected.Count >= policy.MaxEntities)
                {
                    Fail(diagnostics, execution, policy, PresentationDiagnosticCode.EntityBudgetExceeded, "assembly", "The presentation entity budget was exceeded.", descriptor.Entity);
                    break;
                }
                if (!entitySet.Contains(descriptor.Entity))
                {
                    Fail(diagnostics, execution, policy, PresentationDiagnosticCode.EntityNotInSimulationSnapshot, "assembly", "A descriptor referenced an entity absent from the simulation snapshot.", descriptor.Entity);
                    continue;
                }
                if (collected.Any(item => item.Entity.Equals(descriptor.Entity)))
                {
                    Fail(diagnostics, execution, policy, PresentationDiagnosticCode.DuplicateEntity, "assembly", "A simulation entity had more than one presentation descriptor.", descriptor.Entity);
                    continue;
                }
                VisualAssetProviderResolutionStatus status = ResolveAsset(descriptor.VisualAssetId, providerList, diagnostics, execution, policy, descriptor.Entity);
                if (status == VisualAssetProviderResolutionStatus.Missing && policy.MissingVisualAssetBehavior == MissingVisualAssetBehavior.Fail)
                    Fail(diagnostics, execution, policy, PresentationDiagnosticCode.MissingVisualAsset, "asset", "No visual asset provider resolved the logical asset.", descriptor.Entity);
                if (status == VisualAssetProviderResolutionStatus.Failed)
                    Fail(diagnostics, execution, policy, PresentationDiagnosticCode.MissingVisualAsset, "asset", "The logical visual asset could not be resolved.", descriptor.Entity);
                collected.Add(descriptor);
            }

            collected.Sort(CompareDescriptors);
            var changes = BuildChanges(previous, collected);
            return new PresentationSnapshot(simulationSnapshot.Tick, collected, changes, diagnostics, execution);
        }

        private static VisualAssetProviderResolutionStatus ResolveAsset(
            VisualAssetId id,
            IVisualAssetProvider[] providers,
            List<PresentationDiagnostic> diagnostics,
            PresentationExecutionState execution,
            PresentationAssemblyPolicy policy,
            EntityId entity)
        {
            int resolved = 0;
            VisualAssetProviderResolutionStatus firstFailure = VisualAssetProviderResolutionStatus.Missing;
            foreach (IVisualAssetProvider provider in providers.OrderBy(item => item == null ? string.Empty : item.ProviderId, StringComparer.Ordinal))
            {
                if (provider == null || string.IsNullOrEmpty(provider.ProviderId)) continue;
                VisualAssetProviderResult result;
                try { result = provider.Resolve(id); }
                catch (Exception)
                {
                    firstFailure = VisualAssetProviderResolutionStatus.Failed;
                    continue;
                }
                if (result == null) { firstFailure = VisualAssetProviderResolutionStatus.Failed; continue; }
                if (result.Status == VisualAssetProviderResolutionStatus.Resolved) resolved++;
                else if (result.Status == VisualAssetProviderResolutionStatus.Failed) firstFailure = VisualAssetProviderResolutionStatus.Failed;
            }
            if (resolved > 1)
            {
                Fail(diagnostics, execution, policy, PresentationDiagnosticCode.AmbiguousVisualAssetProvider, "asset", "More than one provider resolved the same logical visual asset.", entity);
                return VisualAssetProviderResolutionStatus.Failed;
            }
            if (resolved == 1) return VisualAssetProviderResolutionStatus.Resolved;
            return firstFailure;
        }

        private static int CompareDescriptors(PresentationEntityDescriptor left, PresentationEntityDescriptor right)
        {
            int compare = left.RenderPass.CompareTo(right.RenderPass);
            if (compare != 0) return compare;
            compare = left.Position.Layer.CompareTo(right.Position.Layer);
            if (compare != 0) return compare;
            compare = left.Position.Y.CompareTo(right.Position.Y);
            if (compare != 0) return compare;
            compare = left.Position.X.CompareTo(right.Position.X);
            if (compare != 0) return compare;
            compare = left.ParentStableId.CompareTo(right.ParentStableId);
            if (compare != 0) return compare;
            compare = left.AttachmentOrdinal.CompareTo(right.AttachmentOrdinal);
            if (compare != 0) return compare;
            compare = left.StableSourceOrdinal.CompareTo(right.StableSourceOrdinal);
            if (compare != 0) return compare;
            return left.Entity.CompareTo(right.Entity);
        }

        private static IReadOnlyList<PresentationEntityChange> BuildChanges(PresentationSnapshot previous, IReadOnlyList<PresentationEntityDescriptor> current)
        {
            var changes = new List<PresentationEntityChange>();
            var currentIds = new HashSet<EntityId>(current.Select(item => item.Entity));
            var previousById = previous == null
                ? new Dictionary<EntityId, PresentationEntityDescriptor>()
                : previous.Entities.ToDictionary(item => item.Entity, item => item);
            foreach (PresentationEntityDescriptor descriptor in current)
            {
                PresentationEntityDescriptor old;
                changes.Add(new PresentationEntityChange(previousById.TryGetValue(descriptor.Entity, out old) ? PresentationEntityChangeKind.Persisted : PresentationEntityChangeKind.Created, descriptor.Entity, descriptor));
            }
            if (previous != null)
            {
                foreach (PresentationEntityDescriptor descriptor in previous.Entities.OrderBy(item => item.Entity))
                    if (!currentIds.Contains(descriptor.Entity))
                        changes.Add(new PresentationEntityChange(PresentationEntityChangeKind.Despawned, descriptor.Entity, descriptor));
            }
            return new ReadOnlyCollection<PresentationEntityChange>(changes);
        }

        private static void Fail(
            List<PresentationDiagnostic> diagnostics,
            PresentationExecutionState execution,
            PresentationAssemblyPolicy policy,
            PresentationDiagnosticCode code,
            string stage,
            string message,
            EntityId? entity = null)
        {
            execution.Fail();
            if (diagnostics.Count < policy.MaxDiagnostics)
                diagnostics.Add(new PresentationDiagnostic(PresentationDiagnosticSeverity.Error, code, stage, message, entity));
            else
                execution.Suppress(1);
        }
    }
}
