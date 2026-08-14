using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RA2YR.Simulation;

namespace RA2YR.Presentation
{
    public enum PresentationDiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum PresentationDiagnosticCode
    {
        InvalidPolicy,
        InvalidVisualAssetId,
        EntityBudgetExceeded,
        DuplicateEntity,
        EntityNotInSimulationSnapshot,
        MissingVisualAsset,
        AmbiguousVisualAssetProvider,
        InvalidVisualAssetProvider,
        DiagnosticBudgetExceeded,
        InterpolationArithmeticOverflow,
        InvalidInterpolationFraction,
        SnapshotSourceMissing,
        SnapshotEntityMutation,
        RenderPassUnavailable
    }

    public enum PresentationCompletionStatus
    {
        NotRun,
        Succeeded,
        Failed
    }

    public enum PresentationRenderPass
    {
        Terrain,
        TerrainOverlay,
        GroundShadow,
        GroundObject,
        Structure,
        Vehicle,
        Infantry,
        Projectile,
        Aircraft,
        Effect,
        FogShroud,
        UIWorld
    }

    public enum PresentationEntityChangeKind
    {
        Created,
        Persisted,
        Despawned
    }

    public enum MissingVisualAssetBehavior
    {
        Fail,
        PreserveUnresolved
    }

    public readonly struct VisualAssetId : IEquatable<VisualAssetId>, IComparable<VisualAssetId>
    {
        private readonly string value;

        public VisualAssetId(string value)
        {
            if (!TryValidate(value))
                throw new ArgumentException("A VisualAssetId must be a non-empty stable logical identifier.", nameof(value));
            this.value = value;
        }

        public string Value => value;
        public bool IsValid => !string.IsNullOrEmpty(value);

        public static bool TryCreate(string value, out VisualAssetId id)
        {
            if (!TryValidate(value))
            {
                id = default(VisualAssetId);
                return false;
            }
            id = new VisualAssetId(value);
            return true;
        }

        public bool Equals(VisualAssetId other) => string.Equals(value, other.value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is VisualAssetId && Equals((VisualAssetId)obj);
        public override int GetHashCode() => value == null ? 0 : StringComparer.Ordinal.GetHashCode(value);
        public int CompareTo(VisualAssetId other) => string.CompareOrdinal(value, other.value);
        public override string ToString() => value ?? string.Empty;

        private static bool TryValidate(string candidate)
        {
            return !string.IsNullOrEmpty(candidate) && candidate.Length <= 512 &&
                   candidate.IndexOf('\0') < 0 && candidate.IndexOf('\r') < 0 && candidate.IndexOf('\n') < 0;
        }
    }

    public readonly struct PresentationPosition : IEquatable<PresentationPosition>
    {
        public PresentationPosition(int x, int y, int layer = 0)
        {
            X = x;
            Y = y;
            Layer = layer;
        }

        public int X { get; }
        public int Y { get; }
        public int Layer { get; }

        public bool Equals(PresentationPosition other) => X == other.X && Y == other.Y && Layer == other.Layer;
        public override bool Equals(object obj) => obj is PresentationPosition && Equals((PresentationPosition)obj);
        public override int GetHashCode() => ((X * 397) ^ Y) * 397 ^ Layer;
    }

    public readonly struct PresentationEntityDescriptor : IEquatable<PresentationEntityDescriptor>
    {
        public PresentationEntityDescriptor(
            EntityId entity,
            VisualAssetId visualAssetId,
            PresentationRenderPass renderPass,
            PresentationPosition position,
            int stableSourceOrdinal = 0,
            long parentStableId = 0,
            int attachmentOrdinal = 0,
            bool visible = true)
        {
            if (!entity.IsValid) throw new ArgumentException("A presentation entity requires a valid simulation entity.", nameof(entity));
            if (!visualAssetId.IsValid) throw new ArgumentException("A presentation entity requires a VisualAssetId.", nameof(visualAssetId));
            if (!Enum.IsDefined(typeof(PresentationRenderPass), renderPass)) throw new ArgumentOutOfRangeException(nameof(renderPass));
            if (stableSourceOrdinal < 0 || attachmentOrdinal < 0) throw new ArgumentOutOfRangeException();
            Entity = entity;
            VisualAssetId = visualAssetId;
            RenderPass = renderPass;
            Position = position;
            StableSourceOrdinal = stableSourceOrdinal;
            ParentStableId = parentStableId;
            AttachmentOrdinal = attachmentOrdinal;
            Visible = visible;
        }

        public EntityId Entity { get; }
        public VisualAssetId VisualAssetId { get; }
        public PresentationRenderPass RenderPass { get; }
        public PresentationPosition Position { get; }
        public int StableSourceOrdinal { get; }
        public long ParentStableId { get; }
        public int AttachmentOrdinal { get; }
        public bool Visible { get; }

        public bool Equals(PresentationEntityDescriptor other)
        {
            return Entity.Equals(other.Entity) && VisualAssetId.Equals(other.VisualAssetId) &&
                   RenderPass == other.RenderPass && Position.Equals(other.Position) &&
                   StableSourceOrdinal == other.StableSourceOrdinal && ParentStableId == other.ParentStableId &&
                   AttachmentOrdinal == other.AttachmentOrdinal && Visible == other.Visible;
        }

        public override bool Equals(object obj) => obj is PresentationEntityDescriptor && Equals((PresentationEntityDescriptor)obj);
        public override int GetHashCode() => Entity.GetHashCode();
    }

    public readonly struct PresentationEntityChange
    {
        public PresentationEntityChange(PresentationEntityChangeKind kind, EntityId entity, PresentationEntityDescriptor descriptor)
        {
            Kind = kind;
            Entity = entity;
            Descriptor = descriptor;
        }

        public PresentationEntityChangeKind Kind { get; }
        public EntityId Entity { get; }
        public PresentationEntityDescriptor Descriptor { get; }
    }

    public sealed class PresentationDiagnostic
    {
        public PresentationDiagnostic(
            PresentationDiagnosticSeverity severity,
            PresentationDiagnosticCode code,
            string stage,
            string message,
            EntityId? entity = null)
        {
            Severity = severity;
            Code = code;
            Stage = string.IsNullOrEmpty(stage) ? "presentation" : stage;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Entity = entity;
        }

        public PresentationDiagnosticSeverity Severity { get; }
        public PresentationDiagnosticCode Code { get; }
        public string Stage { get; }
        public string Message { get; }
        public EntityId? Entity { get; }
    }

    public sealed class PresentationExecutionState
    {
        private bool executed;
        private bool fatal;
        private PresentationDiagnosticSeverity highest;
        private int suppressed;

        public PresentationCompletionStatus CompletionStatus => !executed
            ? PresentationCompletionStatus.NotRun
            : fatal ? PresentationCompletionStatus.Failed : PresentationCompletionStatus.Succeeded;
        public bool HasFatalError => fatal;
        public PresentationDiagnosticSeverity HighestSeverity => highest;
        public int SuppressedDiagnosticCount => suppressed;

        internal void MarkExecuted() => executed = true;

        internal void Observe(PresentationDiagnosticSeverity severity)
        {
            executed = true;
            if ((int)severity > (int)highest) highest = severity;
            if (severity == PresentationDiagnosticSeverity.Error) fatal = true;
        }

        internal void Fail()
        {
            executed = true;
            fatal = true;
            if (highest < PresentationDiagnosticSeverity.Error) highest = PresentationDiagnosticSeverity.Error;
        }

        internal void Suppress(int count)
        {
            if (count <= 0) return;
            executed = true;
            long total = (long)suppressed + count;
            suppressed = total >= int.MaxValue ? int.MaxValue : (int)total;
        }
    }

    public sealed class PresentationAssemblyPolicy
    {
        public PresentationAssemblyPolicy(
            int maxEntities = 65536,
            int maxDiagnostics = 256,
            MissingVisualAssetBehavior missingVisualAssetBehavior = MissingVisualAssetBehavior.Fail)
        {
            if (maxEntities < 0 || maxDiagnostics < 0) throw new ArgumentOutOfRangeException();
            if (!Enum.IsDefined(typeof(MissingVisualAssetBehavior), missingVisualAssetBehavior)) throw new ArgumentOutOfRangeException(nameof(missingVisualAssetBehavior));
            MaxEntities = maxEntities;
            MaxDiagnostics = maxDiagnostics;
            MissingVisualAssetBehavior = missingVisualAssetBehavior;
        }

        public int MaxEntities { get; }
        public int MaxDiagnostics { get; }
        public MissingVisualAssetBehavior MissingVisualAssetBehavior { get; }
    }

    public enum VisualAssetProviderResolutionStatus
    {
        Missing,
        Resolved,
        Failed
    }

    public sealed class VisualAssetProviderResult
    {
        public VisualAssetProviderResult(VisualAssetProviderResolutionStatus status, string providerId, VisualAssetId assetId)
        {
            if (!Enum.IsDefined(typeof(VisualAssetProviderResolutionStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            Status = status;
            ProviderId = providerId;
            AssetId = assetId;
        }

        public VisualAssetProviderResolutionStatus Status { get; }
        public string ProviderId { get; }
        public VisualAssetId AssetId { get; }
    }

    public interface IVisualAssetProvider
    {
        string ProviderId { get; }
        VisualAssetProviderResult Resolve(VisualAssetId assetId);
    }

    public readonly struct PresentationInterpolatedPosition
    {
        public PresentationInterpolatedPosition(long xScaled, long yScaled, long layerScaled, long scale)
        {
            XScaled = xScaled;
            YScaled = yScaled;
            LayerScaled = layerScaled;
            Scale = scale;
        }

        public long XScaled { get; }
        public long YScaled { get; }
        public long LayerScaled { get; }
        public long Scale { get; }
    }

    public sealed class PresentationInterpolationProfile
    {
        public PresentationInterpolationProfile(long fixedPointScale = 1000)
        {
            if (fixedPointScale <= 0) throw new ArgumentOutOfRangeException(nameof(fixedPointScale));
            FixedPointScale = fixedPointScale;
        }

        public long FixedPointScale { get; }
    }

    public sealed class PresentationInterpolationResult
    {
        internal PresentationInterpolationResult(PresentationInterpolatedPosition? position, IEnumerable<PresentationDiagnostic> diagnostics, PresentationExecutionState execution)
        {
            Position = position;
            Diagnostics = new ReadOnlyCollection<PresentationDiagnostic>((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
            Execution = execution ?? throw new ArgumentNullException(nameof(execution));
        }

        public PresentationInterpolatedPosition? Position { get; }
        public IReadOnlyList<PresentationDiagnostic> Diagnostics { get; }
        public PresentationExecutionState Execution { get; }
        public bool IsSuccess => Position.HasValue && Execution.CompletionStatus == PresentationCompletionStatus.Succeeded;
    }

    public sealed class PresentationSnapshot
    {
        internal PresentationSnapshot(
            long simulationTick,
            IEnumerable<PresentationEntityDescriptor> entities,
            IEnumerable<PresentationEntityChange> changes,
            IEnumerable<PresentationDiagnostic> diagnostics,
            PresentationExecutionState execution)
        {
            SimulationTick = simulationTick;
            Entities = new ReadOnlyCollection<PresentationEntityDescriptor>((entities ?? throw new ArgumentNullException(nameof(entities))).ToArray());
            Changes = new ReadOnlyCollection<PresentationEntityChange>((changes ?? throw new ArgumentNullException(nameof(changes))).ToArray());
            Diagnostics = new ReadOnlyCollection<PresentationDiagnostic>((diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray());
            Execution = execution ?? throw new ArgumentNullException(nameof(execution));
            CanonicalHash = ComputeHash(SimulationTick, Entities, Changes);
        }

        public long SimulationTick { get; }
        public IReadOnlyList<PresentationEntityDescriptor> Entities { get; }
        public IReadOnlyList<PresentationEntityChange> Changes { get; }
        public IReadOnlyList<PresentationDiagnostic> Diagnostics { get; }
        public PresentationExecutionState Execution { get; }
        public PresentationCompletionStatus CompletionStatus => Execution.CompletionStatus;
        public bool HasFatalError => Execution.HasFatalError;
        public PresentationDiagnosticSeverity HighestSeverity => Execution.HighestSeverity;
        public int SuppressedDiagnosticCount => Execution.SuppressedDiagnosticCount;
        public bool IsSuccess => CompletionStatus == PresentationCompletionStatus.Succeeded;
        public string CanonicalHash { get; }

        private static string ComputeHash(long tick, IReadOnlyList<PresentationEntityDescriptor> entities, IReadOnlyList<PresentationEntityChange> changes)
        {
            var builder = new StringBuilder();
            builder.Append("tick=").Append(tick).Append(';');
            foreach (PresentationEntityDescriptor entity in entities)
            {
                builder.Append(entity.Entity.Index).Append(':').Append(entity.Entity.Generation).Append('|')
                    .Append((int)entity.RenderPass).Append('|').Append(entity.VisualAssetId.Value).Append('|')
                    .Append(entity.Position.X).Append(',').Append(entity.Position.Y).Append(',').Append(entity.Position.Layer).Append('|')
                    .Append(entity.StableSourceOrdinal).Append('|').Append(entity.ParentStableId).Append('|')
                    .Append(entity.AttachmentOrdinal).Append('|').Append(entity.Visible ? '1' : '0').Append(';');
            }
            foreach (PresentationEntityChange change in changes)
                builder.Append("change=").Append((int)change.Kind).Append(':').Append(change.Entity.Index).Append(':').Append(change.Entity.Generation).Append(';');
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()))).Replace("-", string.Empty).ToLowerInvariant();
        }
    }

    public static class PresentationInterpolator
    {
        public static PresentationInterpolationResult Interpolate(
            PresentationPosition previous,
            PresentationPosition current,
            int alphaNumerator,
            int alphaDenominator,
            PresentationInterpolationProfile profile = null)
        {
            var diagnostics = new List<PresentationDiagnostic>();
            var execution = new PresentationExecutionState();
            profile = profile ?? new PresentationInterpolationProfile();
            if (alphaDenominator <= 0 || alphaNumerator < 0 || alphaNumerator > alphaDenominator)
            {
                execution.Fail();
                diagnostics.Add(new PresentationDiagnostic(PresentationDiagnosticSeverity.Error, PresentationDiagnosticCode.InvalidInterpolationFraction, "interpolation", "The interpolation fraction is outside its explicit bounded range."));
                return new PresentationInterpolationResult(null, diagnostics, execution);
            }
            try
            {
                long x = Interpolate(previous.X, current.X, alphaNumerator, alphaDenominator, profile.FixedPointScale);
                long y = Interpolate(previous.Y, current.Y, alphaNumerator, alphaDenominator, profile.FixedPointScale);
                long layer = Interpolate(previous.Layer, current.Layer, alphaNumerator, alphaDenominator, profile.FixedPointScale);
                execution.MarkExecuted();
                return new PresentationInterpolationResult(new PresentationInterpolatedPosition(x, y, layer, profile.FixedPointScale), diagnostics, execution);
            }
            catch (OverflowException)
            {
                execution.Fail();
                diagnostics.Add(new PresentationDiagnostic(PresentationDiagnosticSeverity.Error, PresentationDiagnosticCode.InterpolationArithmeticOverflow, "interpolation", "Interpolation arithmetic exceeded the checked contract."));
                return new PresentationInterpolationResult(null, diagnostics, execution);
            }
        }

        private static long Interpolate(int previous, int current, int numerator, int denominator, long scale)
        {
            long start = checked((long)previous * scale);
            long delta = checked((long)current - previous);
            long weighted = checked(delta * scale);
            return checked(start + (weighted * numerator) / denominator);
        }
    }
}
