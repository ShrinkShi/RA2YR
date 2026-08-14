using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RA2YR.Presentation
{
    public enum PresentationObjectFamily
    {
        TerrainObject,
        Foundation,
        BuildingBody,
        GroundActor,
        Infantry,
        Aircraft,
        Shadow,
        Effect,
        BridgeUnderlay,
        BridgeDeck,
        Attachment
    }

    public enum PresentationElevationLayer
    {
        Ground,
        UnderBridge,
        BridgeDeck,
        Air,
        Shadow
    }

    public enum PresentationAnchorKind
    {
        LogicalGround,
        AuthoredFrame,
        FoundationOrigin,
        RenderPivot,
        ShadowAnchor,
        AttachmentPivot
    }

    public enum PresentationBoundsKind
    {
        Visual,
        ConservativeCulling,
        Selection,
        Occupancy,
        Foundation,
        Shadow
    }

    public enum PresentationDepthPrimary
    {
        AnchorY,
        GridSum
    }

    public enum PresentationDuplicateObjectPolicy
    {
        PreserveAndDiagnose,
        RejectAnyDuplicate
    }

    public enum ObjectPresentationDiagnosticCode
    {
        InvalidDescriptor,
        InvalidPolicy,
        CellBudgetExceeded,
        DiagnosticBudgetExceeded,
        DuplicateStableIdentity,
        MissingAttachmentParent,
        DepthComponentOverflow,
        UnresolvedExactTie,
        CameraDependentDepthRejected,
        FoundationInferredFromImageRejected,
        OccupancyInferredFromVisualRejected,
        NullDescriptor
    }

    public enum ObjectPresentationDiagnosticSeverity
    {
        Warning,
        Error
    }

    public sealed class ObjectPresentationDiagnostic
    {
        public ObjectPresentationDiagnostic(ObjectPresentationDiagnosticCode code, ObjectPresentationDiagnosticSeverity severity, string stage, string message, long sourceOrdinal = -1)
        {
            Code = code;
            Severity = severity;
            Stage = stage ?? throw new ArgumentNullException(nameof(stage));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            SourceOrdinal = sourceOrdinal;
        }

        public ObjectPresentationDiagnosticCode Code { get; }
        public ObjectPresentationDiagnosticSeverity Severity { get; }
        public string Stage { get; }
        public string Message { get; }
        public long SourceOrdinal { get; }
    }

    public readonly struct PresentationAnchor : IEquatable<PresentationAnchor>
    {
        public PresentationAnchor(PresentationAnchorKind kind, long x, long y, long z = 0)
        {
            if (!Enum.IsDefined(typeof(PresentationAnchorKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
            X = x;
            Y = y;
            Z = z;
        }

        public PresentationAnchorKind Kind { get; }
        public long X { get; }
        public long Y { get; }
        public long Z { get; }
        public bool Equals(PresentationAnchor other) => Kind == other.Kind && X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is PresentationAnchor && Equals((PresentationAnchor)obj);
        public override int GetHashCode() => (((int)Kind * 397) ^ X.GetHashCode()) * 397 ^ Y.GetHashCode() ^ Z.GetHashCode();
    }

    public readonly struct PresentationBounds : IEquatable<PresentationBounds>
    {
        public PresentationBounds(PresentationBoundsKind kind, long minX, long minY, long maxX, long maxY)
        {
            if (!Enum.IsDefined(typeof(PresentationBoundsKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (maxX < minX || maxY < minY) throw new ArgumentException("Bounds must be ordered.");
            Kind = kind;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public PresentationBoundsKind Kind { get; }
        public long MinX { get; }
        public long MinY { get; }
        public long MaxX { get; }
        public long MaxY { get; }
        public bool Equals(PresentationBounds other) => Kind == other.Kind && MinX == other.MinX && MinY == other.MinY && MaxX == other.MaxX && MaxY == other.MaxY;
        public override bool Equals(object obj) => obj is PresentationBounds && Equals((PresentationBounds)obj);
        public override int GetHashCode() => (((int)Kind * 397) ^ MinX.GetHashCode()) ^ MaxX.GetHashCode() ^ MinY.GetHashCode() ^ MaxY.GetHashCode();
    }

    public sealed class ObjectVisualPresentationDescriptor
    {
        public ObjectVisualPresentationDescriptor(
            VisualAssetId visualAssetId,
            PresentationObjectFamily family,
            PresentationRenderPass renderPass,
            PresentationElevationLayer elevationLayer,
            PresentationAnchor logicalGroundAnchor,
            PresentationBounds visualBounds,
            PresentationBounds conservativeCullingBounds,
            string stableIdentity,
            long sourceOrdinal,
            long gridX,
            long gridY,
            long levelRaw = 0,
            long heightRaw = 0,
            long explicitZAdjust = 0,
            PresentationBounds? selectionBounds = null,
            PresentationBounds? occupancyBounds = null,
            PresentationBounds? foundationBounds = null,
            PresentationBounds? shadowBounds = null,
            long parentStableId = 0,
            int attachmentOrdinal = 0,
            int duplicateOrdinal = 0)
        {
            if (!visualAssetId.IsValid || string.IsNullOrWhiteSpace(stableIdentity)) throw new ArgumentException("A stable visual identity is required.");
            if (!Enum.IsDefined(typeof(PresentationObjectFamily), family) || !Enum.IsDefined(typeof(PresentationRenderPass), renderPass) || !Enum.IsDefined(typeof(PresentationElevationLayer), elevationLayer)) throw new ArgumentOutOfRangeException();
            if (sourceOrdinal < 0 || attachmentOrdinal < 0 || duplicateOrdinal < 0) throw new ArgumentOutOfRangeException();
            if (family == PresentationObjectFamily.Attachment && parentStableId == 0) throw new ArgumentException("An attachment requires a parent stable id.", nameof(parentStableId));
            if (visualBounds.Kind != PresentationBoundsKind.Visual || conservativeCullingBounds.Kind != PresentationBoundsKind.ConservativeCulling) throw new ArgumentException("Visual bounds kinds must remain explicit.");
            VisualAssetId = visualAssetId;
            Family = family;
            RenderPass = renderPass;
            ElevationLayer = elevationLayer;
            LogicalGroundAnchor = logicalGroundAnchor;
            VisualBounds = visualBounds;
            ConservativeCullingBounds = conservativeCullingBounds;
            SelectionBounds = selectionBounds;
            OccupancyBounds = occupancyBounds;
            FoundationBounds = foundationBounds;
            ShadowBounds = shadowBounds;
            StableIdentity = stableIdentity;
            SourceOrdinal = sourceOrdinal;
            GridX = gridX;
            GridY = gridY;
            LevelRaw = levelRaw;
            HeightRaw = heightRaw;
            ExplicitZAdjust = explicitZAdjust;
            ParentStableId = parentStableId;
            AttachmentOrdinal = attachmentOrdinal;
            DuplicateOrdinal = duplicateOrdinal;
        }

        public VisualAssetId VisualAssetId { get; }
        public PresentationObjectFamily Family { get; }
        public PresentationRenderPass RenderPass { get; }
        public PresentationElevationLayer ElevationLayer { get; }
        public PresentationAnchor LogicalGroundAnchor { get; }
        public PresentationBounds VisualBounds { get; }
        public PresentationBounds ConservativeCullingBounds { get; }
        public PresentationBounds? SelectionBounds { get; }
        public PresentationBounds? OccupancyBounds { get; }
        public PresentationBounds? FoundationBounds { get; }
        public PresentationBounds? ShadowBounds { get; }
        public string StableIdentity { get; }
        public long SourceOrdinal { get; }
        public long GridX { get; }
        public long GridY { get; }
        public long LevelRaw { get; }
        public long HeightRaw { get; }
        public long ExplicitZAdjust { get; }
        public long ParentStableId { get; }
        public int AttachmentOrdinal { get; }
        public int DuplicateOrdinal { get; }
    }

    public readonly struct RenderDepthKey : IComparable<RenderDepthKey>, IEquatable<RenderDepthKey>
    {
        public RenderDepthKey(int passOrdinal, int elevationOrdinal, long primaryDepth, long explicitZAdjust, int familyPriority, long parentStableId, int attachmentOrdinal, long sourceOrdinal, string stableIdentity, int duplicateOrdinal)
        {
            PassOrdinal = passOrdinal;
            ElevationOrdinal = elevationOrdinal;
            PrimaryDepth = primaryDepth;
            ExplicitZAdjust = explicitZAdjust;
            FamilyPriority = familyPriority;
            ParentStableId = parentStableId;
            AttachmentOrdinal = attachmentOrdinal;
            SourceOrdinal = sourceOrdinal;
            StableIdentity = stableIdentity ?? throw new ArgumentNullException(nameof(stableIdentity));
            DuplicateOrdinal = duplicateOrdinal;
        }

        public int PassOrdinal { get; }
        public int ElevationOrdinal { get; }
        public long PrimaryDepth { get; }
        public long ExplicitZAdjust { get; }
        public int FamilyPriority { get; }
        public long ParentStableId { get; }
        public int AttachmentOrdinal { get; }
        public long SourceOrdinal { get; }
        public string StableIdentity { get; }
        public int DuplicateOrdinal { get; }
        public int CompareTo(RenderDepthKey other)
        {
            int value = PassOrdinal.CompareTo(other.PassOrdinal); if (value != 0) return value;
            value = ElevationOrdinal.CompareTo(other.ElevationOrdinal); if (value != 0) return value;
            value = PrimaryDepth.CompareTo(other.PrimaryDepth); if (value != 0) return value;
            value = ExplicitZAdjust.CompareTo(other.ExplicitZAdjust); if (value != 0) return value;
            value = FamilyPriority.CompareTo(other.FamilyPriority); if (value != 0) return value;
            value = ParentStableId.CompareTo(other.ParentStableId); if (value != 0) return value;
            value = AttachmentOrdinal.CompareTo(other.AttachmentOrdinal); if (value != 0) return value;
            value = SourceOrdinal.CompareTo(other.SourceOrdinal); if (value != 0) return value;
            value = string.CompareOrdinal(StableIdentity, other.StableIdentity); if (value != 0) return value;
            return DuplicateOrdinal.CompareTo(other.DuplicateOrdinal);
        }
        public bool Equals(RenderDepthKey other) => CompareTo(other) == 0;
        public override bool Equals(object obj) => obj is RenderDepthKey && Equals((RenderDepthKey)obj);
        public override int GetHashCode() => StableIdentity.GetHashCode() ^ PrimaryDepth.GetHashCode() ^ SourceOrdinal.GetHashCode();
    }

    public sealed class ObjectVisualPresentationPolicy
    {
        public ObjectVisualPresentationPolicy(
            PresentationDepthPrimary primary = PresentationDepthPrimary.GridSum,
            PresentationDuplicateObjectPolicy duplicates = PresentationDuplicateObjectPolicy.PreserveAndDiagnose,
            int maxObjects = 65536,
            int maxDiagnostics = 256,
            bool cameraDependent = false)
        {
            if (!Enum.IsDefined(typeof(PresentationDepthPrimary), primary) || !Enum.IsDefined(typeof(PresentationDuplicateObjectPolicy), duplicates)) throw new ArgumentOutOfRangeException();
            if (maxObjects < 0 || maxDiagnostics < 0) throw new ArgumentOutOfRangeException();
            if (cameraDependent) throw new ArgumentException("Camera-dependent depth is not a logical presentation policy.", nameof(cameraDependent));
            Primary = primary;
            Duplicates = duplicates;
            MaxObjects = maxObjects;
            MaxDiagnostics = maxDiagnostics;
        }

        public PresentationDepthPrimary Primary { get; }
        public PresentationDuplicateObjectPolicy Duplicates { get; }
        public int MaxObjects { get; }
        public int MaxDiagnostics { get; }
    }

    public sealed class ObjectVisualPresentationEntry
    {
        internal ObjectVisualPresentationEntry(ObjectVisualPresentationDescriptor descriptor, RenderDepthKey depthKey)
        {
            Descriptor = descriptor;
            DepthKey = depthKey;
        }
        public ObjectVisualPresentationDescriptor Descriptor { get; }
        public RenderDepthKey DepthKey { get; }
    }

    public sealed class ObjectVisualPresentationResult
    {
        internal ObjectVisualPresentationResult(IEnumerable<ObjectVisualPresentationEntry> entries, IEnumerable<ObjectPresentationDiagnostic> diagnostics, PresentationExecutionState execution)
        {
            Entries = new ReadOnlyCollection<ObjectVisualPresentationEntry>((entries ?? Enumerable.Empty<ObjectVisualPresentationEntry>()).ToArray());
            Diagnostics = new ReadOnlyCollection<ObjectPresentationDiagnostic>((diagnostics ?? Enumerable.Empty<ObjectPresentationDiagnostic>()).ToArray());
            Execution = execution ?? throw new ArgumentNullException(nameof(execution));
        }
        public IReadOnlyList<ObjectVisualPresentationEntry> Entries { get; }
        public IReadOnlyList<ObjectPresentationDiagnostic> Diagnostics { get; }
        public PresentationExecutionState Execution { get; }
        public bool IsSuccess => Execution.CompletionStatus == PresentationCompletionStatus.Succeeded;
    }

    public static class ObjectVisualPresentationComposer
    {
        public static ObjectVisualPresentationResult Compose(IEnumerable<ObjectVisualPresentationDescriptor> source, ObjectVisualPresentationPolicy policy = null)
        {
            policy = policy ?? new ObjectVisualPresentationPolicy();
            var diagnostics = new List<ObjectPresentationDiagnostic>();
            var execution = new PresentationExecutionState();
            var entries = new List<ObjectVisualPresentationEntry>();
            if (source == null)
            {
                AddFailure(diagnostics, execution, policy, ObjectPresentationDiagnosticCode.InvalidDescriptor, "source", "Object descriptor source is required.");
                return new ObjectVisualPresentationResult(entries, diagnostics, execution);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ObjectVisualPresentationDescriptor descriptor in source)
            {
                execution.MarkExecuted();
                if (descriptor == null)
                {
                    AddFailure(diagnostics, execution, policy, ObjectPresentationDiagnosticCode.NullDescriptor, "source", "Null object descriptor is not accepted.");
                    break;
                }
                if (entries.Count >= policy.MaxObjects)
                {
                    AddFailure(diagnostics, execution, policy, ObjectPresentationDiagnosticCode.CellBudgetExceeded, "objects", "Object presentation budget exceeded.", descriptor.SourceOrdinal);
                    break;
                }
                if (!seen.Add(descriptor.StableIdentity))
                {
                    if (policy.Duplicates == PresentationDuplicateObjectPolicy.RejectAnyDuplicate)
                        AddFailure(diagnostics, execution, policy, ObjectPresentationDiagnosticCode.DuplicateStableIdentity, "identity", "Duplicate stable identity is rejected by policy.", descriptor.SourceOrdinal);
                    else
                        AddWarning(diagnostics, execution, policy, ObjectPresentationDiagnosticCode.DuplicateStableIdentity, "identity", "Duplicate stable identity was preserved and diagnosed.", descriptor.SourceOrdinal);
                }
                RenderDepthKey key;
                try { key = CreateKey(descriptor, policy); }
                catch (OverflowException)
                {
                    AddFailure(diagnostics, execution, policy, ObjectPresentationDiagnosticCode.DepthComponentOverflow, "depth", "Depth component arithmetic exceeded the checked contract.", descriptor.SourceOrdinal);
                    break;
                }
                entries.Add(new ObjectVisualPresentationEntry(descriptor, key));
            }

            var parentIds = new HashSet<long>(entries.Where(e => e.Descriptor.Family != PresentationObjectFamily.Attachment).Select(e => e.Descriptor.ParentStableId));
            foreach (ObjectVisualPresentationEntry entry in entries)
            {
                if (entry.Descriptor.Family == PresentationObjectFamily.Attachment && !entries.Any(e => e.Descriptor.StableIdentity == entry.Descriptor.ParentStableId.ToString() || e.Descriptor.SourceOrdinal == entry.Descriptor.ParentStableId))
                    AddFailure(diagnostics, execution, policy, ObjectPresentationDiagnosticCode.MissingAttachmentParent, "attachment", "Attachment parent is not present in the composed source.", entry.Descriptor.SourceOrdinal);
            }

            entries.Sort((left, right) => left.DepthKey.CompareTo(right.DepthKey));
            return new ObjectVisualPresentationResult(entries, diagnostics, execution);
        }

        private static RenderDepthKey CreateKey(ObjectVisualPresentationDescriptor descriptor, ObjectVisualPresentationPolicy policy)
        {
            long primary = policy.Primary == PresentationDepthPrimary.AnchorY ? descriptor.LogicalGroundAnchor.Y : checked(descriptor.GridX + descriptor.GridY);
            primary = checked(primary + descriptor.LevelRaw + descriptor.HeightRaw);
            return new RenderDepthKey((int)descriptor.RenderPass, (int)descriptor.ElevationLayer, primary, descriptor.ExplicitZAdjust, (int)descriptor.Family, descriptor.ParentStableId, descriptor.AttachmentOrdinal, descriptor.SourceOrdinal, descriptor.StableIdentity, descriptor.DuplicateOrdinal);
        }

        private static void AddFailure(List<ObjectPresentationDiagnostic> diagnostics, PresentationExecutionState execution, ObjectVisualPresentationPolicy policy, ObjectPresentationDiagnosticCode code, string stage, string message, long sourceOrdinal = -1)
        {
            execution.Fail();
            Add(diagnostics, execution, policy, new ObjectPresentationDiagnostic(code, ObjectPresentationDiagnosticSeverity.Error, stage, message, sourceOrdinal));
        }

        private static void AddWarning(List<ObjectPresentationDiagnostic> diagnostics, PresentationExecutionState execution, ObjectVisualPresentationPolicy policy, ObjectPresentationDiagnosticCode code, string stage, string message, long sourceOrdinal = -1)
        {
            execution.Observe(PresentationDiagnosticSeverity.Warning);
            Add(diagnostics, execution, policy, new ObjectPresentationDiagnostic(code, ObjectPresentationDiagnosticSeverity.Warning, stage, message, sourceOrdinal));
        }

        private static void Add(List<ObjectPresentationDiagnostic> diagnostics, PresentationExecutionState execution, ObjectVisualPresentationPolicy policy, ObjectPresentationDiagnostic diagnostic)
        {
            if (diagnostics.Count < policy.MaxDiagnostics) diagnostics.Add(diagnostic);
            else execution.Suppress(1);
        }
    }
}
