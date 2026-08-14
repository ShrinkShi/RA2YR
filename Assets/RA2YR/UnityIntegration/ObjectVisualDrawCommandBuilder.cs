using System;
using System.Collections.Generic;
using RA2YR.Presentation;
using UnityEngine;

namespace RA2YR.UnityIntegration
{
    public sealed class ObjectVisualDrawCommand
    {
        internal ObjectVisualDrawCommand(string stableIdentity, VisualAssetId visualAssetId, Vector3 logicalAnchor, RenderDepthKey depthKey)
        {
            StableIdentity = stableIdentity;
            VisualAssetId = visualAssetId;
            LogicalAnchor = logicalAnchor;
            DepthKey = depthKey;
        }

        public string StableIdentity { get; }
        public VisualAssetId VisualAssetId { get; }
        public Vector3 LogicalAnchor { get; }
        public RenderDepthKey DepthKey { get; }
    }

    public sealed class ObjectVisualDrawCommandResult
    {
        internal ObjectVisualDrawCommandResult(IEnumerable<ObjectVisualDrawCommand> commands, bool isSuccess, string failure)
        {
            Commands = new List<ObjectVisualDrawCommand>(commands ?? new ObjectVisualDrawCommand[0]).AsReadOnly();
            IsSuccess = isSuccess;
            Failure = failure;
        }

        public IReadOnlyList<ObjectVisualDrawCommand> Commands { get; }
        public bool IsSuccess { get; }
        public string Failure { get; }
    }

    /// <summary>
    /// Downstream Unity adapter for already ordered object descriptors. It emits
    /// draw commands only; it does not create GameObjects, textures, materials,
    /// simulation state, or palette bindings.
    /// </summary>
    public static class ObjectVisualDrawCommandBuilder
    {
        public static ObjectVisualDrawCommandResult Build(ObjectVisualPresentationResult presentation, int maxCommands = 65536)
        {
            if (presentation == null) return new ObjectVisualDrawCommandResult(null, false, "Presentation result is required.");
            if (!presentation.IsSuccess) return new ObjectVisualDrawCommandResult(null, false, "Object presentation failed.");
            if (maxCommands < 0 || presentation.Entries.Count > maxCommands) return new ObjectVisualDrawCommandResult(null, false, "Draw command budget exceeded.");
            var commands = new List<ObjectVisualDrawCommand>(presentation.Entries.Count);
            foreach (ObjectVisualPresentationEntry entry in presentation.Entries)
            {
                ObjectVisualPresentationDescriptor descriptor = entry.Descriptor;
                commands.Add(new ObjectVisualDrawCommand(
                    descriptor.StableIdentity,
                    descriptor.VisualAssetId,
                    new Vector3((float)descriptor.LogicalGroundAnchor.X, (float)descriptor.LogicalGroundAnchor.Z, (float)descriptor.LogicalGroundAnchor.Y),
                    entry.DepthKey));
            }
            return new ObjectVisualDrawCommandResult(commands, true, null);
        }
    }
}
