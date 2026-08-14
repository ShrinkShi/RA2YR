using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RA2YR.Presentation
{
    public enum PresentationEffectKind { Projectile, Particle, Animation, Smoke, Fire, Explosion, Wake }
    public enum PresentationAlphaMode { Opaque, Cutout, Translucent }
    public enum PresentationDepthTestMode { Disabled, TestOnly, TestAndWrite }
    public enum PresentationVisibilityState { Visible, Fogged, Shrouded, Unknown }
    public enum PresentationFogPolicyMode { PreserveLogicalAndAnnotate, HideVisualOnly }
    public enum PresentationUnknownVisibilityPolicy { PreserveUnresolved, Reject }
    public enum PresentationShadowSourceKind { None, TmpCandidate, ShpFrameCandidate, VxlCandidate, ProceduralCandidate }
    public enum PresentationShadowColorProfile { Unresolved, PaletteMask, AlphaMultiply, IndexedMask }

    public enum EffectPresentationDiagnosticCode
    {
        InvalidDescriptor,
        InvalidPolicy,
        EffectBudgetExceeded,
        ShadowBudgetExceeded,
        NullDescriptor,
        DuplicateStableIdentity,
        UnknownVisibility,
        ShadowSourceMissing,
        ShadowReceiverLayerMissing,
        ShadowUsedForOccupancyRejected,
        DepthComponentOverflow,
        DiagnosticBudgetExceeded,
        NoProgress
    }

    public sealed class EffectPresentationDiagnostic
    {
        public EffectPresentationDiagnostic(EffectPresentationDiagnosticCode code, ObjectPresentationDiagnosticSeverity severity, string stage, string message, long sourceOrdinal = -1)
        {
            Code = code; Severity = severity; Stage = stage ?? throw new ArgumentNullException(nameof(stage)); Message = message ?? throw new ArgumentNullException(nameof(message)); SourceOrdinal = sourceOrdinal;
        }
        public EffectPresentationDiagnosticCode Code { get; }
        public ObjectPresentationDiagnosticSeverity Severity { get; }
        public string Stage { get; }
        public string Message { get; }
        public long SourceOrdinal { get; }
    }

    public sealed class EffectPresentationDescriptor
    {
        public EffectPresentationDescriptor(
            string stableIdentity,
            VisualAssetId visualAssetId,
            PresentationEffectKind kind,
            PresentationElevationLayer elevationLayer,
            PresentationAnchor anchor,
            PresentationBounds visualBounds,
            PresentationBounds conservativeCullingBounds,
            PresentationAlphaMode alphaMode,
            PresentationDepthTestMode depthTestMode,
            PresentationVisibilityState visibility,
            long sourceOrdinal,
            long explicitSortAdjust = 0,
            long parentStableId = 0,
            int duplicateOrdinal = 0)
        {
            if (string.IsNullOrWhiteSpace(stableIdentity) || !visualAssetId.IsValid) throw new ArgumentException("A stable effect identity is required.");
            if (!Enum.IsDefined(typeof(PresentationEffectKind), kind) || !Enum.IsDefined(typeof(PresentationElevationLayer), elevationLayer) || !Enum.IsDefined(typeof(PresentationAlphaMode), alphaMode) || !Enum.IsDefined(typeof(PresentationDepthTestMode), depthTestMode) || !Enum.IsDefined(typeof(PresentationVisibilityState), visibility)) throw new ArgumentOutOfRangeException();
            if (visualBounds.Kind != PresentationBoundsKind.Visual || conservativeCullingBounds.Kind != PresentationBoundsKind.ConservativeCulling) throw new ArgumentException("Effect bounds kinds must remain explicit.");
            if (sourceOrdinal < 0 || duplicateOrdinal < 0) throw new ArgumentOutOfRangeException();
            StableIdentity = stableIdentity; VisualAssetId = visualAssetId; Kind = kind; ElevationLayer = elevationLayer; Anchor = anchor; VisualBounds = visualBounds; ConservativeCullingBounds = conservativeCullingBounds; AlphaMode = alphaMode; DepthTestMode = depthTestMode; Visibility = visibility; SourceOrdinal = sourceOrdinal; ExplicitSortAdjust = explicitSortAdjust; ParentStableId = parentStableId; DuplicateOrdinal = duplicateOrdinal;
        }
        public string StableIdentity { get; }
        public VisualAssetId VisualAssetId { get; }
        public PresentationEffectKind Kind { get; }
        public PresentationElevationLayer ElevationLayer { get; }
        public PresentationAnchor Anchor { get; }
        public PresentationBounds VisualBounds { get; }
        public PresentationBounds ConservativeCullingBounds { get; }
        public PresentationAlphaMode AlphaMode { get; }
        public PresentationDepthTestMode DepthTestMode { get; }
        public PresentationVisibilityState Visibility { get; }
        public long SourceOrdinal { get; }
        public long ExplicitSortAdjust { get; }
        public long ParentStableId { get; }
        public int DuplicateOrdinal { get; }
    }

    public sealed class ShadowPresentationDescriptor
    {
        public ShadowPresentationDescriptor(string stableIdentity, string casterStableIdentity, PresentationElevationLayer receiverLayer, PresentationShadowSourceKind sourceKind, PresentationAnchor anchor, PresentationBounds shadowBounds, PresentationShadowColorProfile colorProfile, long sourceOrdinal)
        {
            if (string.IsNullOrWhiteSpace(stableIdentity) || string.IsNullOrWhiteSpace(casterStableIdentity)) throw new ArgumentException("Shadow and caster identities are required.");
            if (!Enum.IsDefined(typeof(PresentationElevationLayer), receiverLayer) || !Enum.IsDefined(typeof(PresentationShadowSourceKind), sourceKind) || !Enum.IsDefined(typeof(PresentationShadowColorProfile), colorProfile)) throw new ArgumentOutOfRangeException();
            if (shadowBounds.Kind != PresentationBoundsKind.Shadow) throw new ArgumentException("Shadow bounds must remain explicit.");
            if (sourceOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(sourceOrdinal));
            StableIdentity = stableIdentity; CasterStableIdentity = casterStableIdentity; ReceiverLayer = receiverLayer; SourceKind = sourceKind; Anchor = anchor; ShadowBounds = shadowBounds; ColorProfile = colorProfile; SourceOrdinal = sourceOrdinal;
        }
        public string StableIdentity { get; }
        public string CasterStableIdentity { get; }
        public PresentationElevationLayer ReceiverLayer { get; }
        public PresentationShadowSourceKind SourceKind { get; }
        public PresentationAnchor Anchor { get; }
        public PresentationBounds ShadowBounds { get; }
        public PresentationShadowColorProfile ColorProfile { get; }
        public long SourceOrdinal { get; }
        public bool AffectsOccupancy => false;
    }

    public sealed class EffectPresentationPolicy
    {
        public EffectPresentationPolicy(int maxEffects = 65536, int maxShadows = 65536, int maxDiagnostics = 256, PresentationDuplicateObjectPolicy duplicates = PresentationDuplicateObjectPolicy.PreserveAndDiagnose, PresentationFogPolicyMode fogMode = PresentationFogPolicyMode.PreserveLogicalAndAnnotate, PresentationUnknownVisibilityPolicy unknownVisibility = PresentationUnknownVisibilityPolicy.PreserveUnresolved)
        {
            if (maxEffects < 0 || maxShadows < 0 || maxDiagnostics < 0) throw new ArgumentOutOfRangeException();
            if (!Enum.IsDefined(typeof(PresentationDuplicateObjectPolicy), duplicates) || !Enum.IsDefined(typeof(PresentationFogPolicyMode), fogMode) || !Enum.IsDefined(typeof(PresentationUnknownVisibilityPolicy), unknownVisibility)) throw new ArgumentOutOfRangeException();
            MaxEffects = maxEffects; MaxShadows = maxShadows; MaxDiagnostics = maxDiagnostics; Duplicates = duplicates; FogMode = fogMode; UnknownVisibility = unknownVisibility;
        }
        public int MaxEffects { get; }
        public int MaxShadows { get; }
        public int MaxDiagnostics { get; }
        public PresentationDuplicateObjectPolicy Duplicates { get; }
        public PresentationFogPolicyMode FogMode { get; }
        public PresentationUnknownVisibilityPolicy UnknownVisibility { get; }
    }

    public readonly struct EffectDepthKey : IComparable<EffectDepthKey>, IEquatable<EffectDepthKey>
    {
        public EffectDepthKey(int elevation, long primary, long adjust, long parent, long source, string identity, int duplicate)
        { Elevation = elevation; Primary = primary; Adjust = adjust; Parent = parent; Source = source; Identity = identity ?? throw new ArgumentNullException(nameof(identity)); Duplicate = duplicate; }
        public int Elevation { get; } public long Primary { get; } public long Adjust { get; } public long Parent { get; } public long Source { get; } public string Identity { get; } public int Duplicate { get; }
        public int CompareTo(EffectDepthKey other) { int c = Elevation.CompareTo(other.Elevation); if (c != 0) return c; c = Primary.CompareTo(other.Primary); if (c != 0) return c; c = Adjust.CompareTo(other.Adjust); if (c != 0) return c; c = Parent.CompareTo(other.Parent); if (c != 0) return c; c = Source.CompareTo(other.Source); if (c != 0) return c; c = string.CompareOrdinal(Identity, other.Identity); return c != 0 ? c : Duplicate.CompareTo(other.Duplicate); }
        public bool Equals(EffectDepthKey other) => CompareTo(other) == 0; public override bool Equals(object obj) => obj is EffectDepthKey && Equals((EffectDepthKey)obj); public override int GetHashCode() => Identity.GetHashCode() ^ Primary.GetHashCode();
    }

    public sealed class EffectPresentationEntry
    {
        internal EffectPresentationEntry(EffectPresentationDescriptor descriptor, EffectDepthKey key, bool submitted)
        { Descriptor = descriptor; DepthKey = key; IsVisuallySubmitted = submitted; }
        public EffectPresentationDescriptor Descriptor { get; }
        public EffectDepthKey DepthKey { get; }
        public bool IsVisuallySubmitted { get; }
    }

    public sealed class EffectPresentationResult
    {
        internal EffectPresentationResult(IEnumerable<EffectPresentationEntry> entries, IEnumerable<ShadowPresentationDescriptor> shadows, IEnumerable<EffectPresentationDiagnostic> diagnostics, PresentationExecutionState execution)
        { Entries = new ReadOnlyCollection<EffectPresentationEntry>((entries ?? Enumerable.Empty<EffectPresentationEntry>()).ToArray()); Shadows = new ReadOnlyCollection<ShadowPresentationDescriptor>((shadows ?? Enumerable.Empty<ShadowPresentationDescriptor>()).ToArray()); Diagnostics = new ReadOnlyCollection<EffectPresentationDiagnostic>((diagnostics ?? Enumerable.Empty<EffectPresentationDiagnostic>()).ToArray()); Execution = execution ?? throw new ArgumentNullException(nameof(execution)); }
        public IReadOnlyList<EffectPresentationEntry> Entries { get; }
        public IReadOnlyList<ShadowPresentationDescriptor> Shadows { get; }
        public IReadOnlyList<EffectPresentationDiagnostic> Diagnostics { get; }
        public PresentationExecutionState Execution { get; }
        public bool IsSuccess => Execution.CompletionStatus == PresentationCompletionStatus.Succeeded;
    }

    public static class EffectPresentationComposer
    {
        public static EffectPresentationResult Compose(IEnumerable<EffectPresentationDescriptor> effects, IEnumerable<ShadowPresentationDescriptor> shadows, EffectPresentationPolicy policy = null)
        {
            policy = policy ?? new EffectPresentationPolicy();
            var diagnostics = new List<EffectPresentationDiagnostic>(); var execution = new PresentationExecutionState(); var entries = new List<EffectPresentationEntry>(); var outputShadows = new List<ShadowPresentationDescriptor>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            if (effects == null && shadows == null) { Fail(diagnostics, execution, policy, EffectPresentationDiagnosticCode.InvalidDescriptor, "effects", "At least one effect or shadow source is required."); return new EffectPresentationResult(entries, outputShadows, diagnostics, execution); }
            if (effects != null) foreach (EffectPresentationDescriptor descriptor in effects)
            {
                execution.MarkExecuted();
                if (descriptor == null) { Fail(diagnostics, execution, policy, EffectPresentationDiagnosticCode.NullDescriptor, "effects", "Null effect descriptor is not accepted."); break; }
                if (entries.Count >= policy.MaxEffects) { Fail(diagnostics, execution, policy, EffectPresentationDiagnosticCode.EffectBudgetExceeded, "effects", "Effect budget exceeded.", descriptor.SourceOrdinal); break; }
                if (!seen.Add(descriptor.StableIdentity))
                {
                    if (policy.Duplicates == PresentationDuplicateObjectPolicy.RejectAnyDuplicate) Fail(diagnostics, execution, policy, EffectPresentationDiagnosticCode.DuplicateStableIdentity, "effects", "Duplicate effect identity rejected.", descriptor.SourceOrdinal);
                    else Warn(diagnostics, execution, policy, EffectPresentationDiagnosticCode.DuplicateStableIdentity, "effects", "Duplicate effect identity preserved.", descriptor.SourceOrdinal);
                }
                if (descriptor.Visibility == PresentationVisibilityState.Unknown && policy.UnknownVisibility == PresentationUnknownVisibilityPolicy.Reject) Fail(diagnostics, execution, policy, EffectPresentationDiagnosticCode.UnknownVisibility, "visibility", "Unknown visibility rejected by policy.", descriptor.SourceOrdinal);
                bool submitted = descriptor.Visibility == PresentationVisibilityState.Visible;
                if (descriptor.Visibility == PresentationVisibilityState.Unknown || descriptor.Visibility == PresentationVisibilityState.Fogged || descriptor.Visibility == PresentationVisibilityState.Shrouded) submitted = false;
                EffectDepthKey key;
                try { key = new EffectDepthKey((int)descriptor.ElevationLayer, checked(descriptor.Anchor.Y + descriptor.ExplicitSortAdjust), descriptor.ExplicitSortAdjust, descriptor.ParentStableId, descriptor.SourceOrdinal, descriptor.StableIdentity, descriptor.DuplicateOrdinal); }
                catch (OverflowException) { Fail(diagnostics, execution, policy, EffectPresentationDiagnosticCode.DepthComponentOverflow, "depth", "Effect depth arithmetic exceeded the checked contract.", descriptor.SourceOrdinal); break; }
                entries.Add(new EffectPresentationEntry(descriptor, key, submitted));
            }
            if (shadows != null)
            {
                foreach (ShadowPresentationDescriptor shadow in shadows)
                {
                    execution.MarkExecuted();
                    if (outputShadows.Count >= policy.MaxShadows) { Fail(diagnostics, execution, policy, EffectPresentationDiagnosticCode.ShadowBudgetExceeded, "shadows", "Shadow budget exceeded.", shadow == null ? -1 : shadow.SourceOrdinal); break; }
                    if (shadow == null) { Fail(diagnostics, execution, policy, EffectPresentationDiagnosticCode.NullDescriptor, "shadows", "Null shadow descriptor is not accepted."); break; }
                    if (shadow.SourceKind == PresentationShadowSourceKind.None) Warn(diagnostics, execution, policy, EffectPresentationDiagnosticCode.ShadowSourceMissing, "shadows", "Shadow source remains unresolved.", shadow.SourceOrdinal);
                    outputShadows.Add(shadow);
                }
            }
            entries.Sort((left, right) => left.DepthKey.CompareTo(right.DepthKey));
            return new EffectPresentationResult(entries, outputShadows, diagnostics, execution);
        }
        private static void Fail(List<EffectPresentationDiagnostic> list, PresentationExecutionState execution, EffectPresentationPolicy policy, EffectPresentationDiagnosticCode code, string stage, string message, long ordinal = -1) { execution.Fail(); Add(list, execution, policy, new EffectPresentationDiagnostic(code, ObjectPresentationDiagnosticSeverity.Error, stage, message, ordinal)); }
        private static void Warn(List<EffectPresentationDiagnostic> list, PresentationExecutionState execution, EffectPresentationPolicy policy, EffectPresentationDiagnosticCode code, string stage, string message, long ordinal = -1) { execution.Observe(PresentationDiagnosticSeverity.Warning); Add(list, execution, policy, new EffectPresentationDiagnostic(code, ObjectPresentationDiagnosticSeverity.Warning, stage, message, ordinal)); }
        private static void Add(List<EffectPresentationDiagnostic> list, PresentationExecutionState execution, EffectPresentationPolicy policy, EffectPresentationDiagnostic diagnostic) { if (list.Count < policy.MaxDiagnostics) list.Add(diagnostic); else execution.Suppress(1); }
    }
}
