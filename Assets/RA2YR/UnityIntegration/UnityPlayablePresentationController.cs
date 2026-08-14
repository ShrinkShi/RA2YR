using System;
using RA2YR.Presentation;
using UnityEngine;

namespace RA2YR.UnityIntegration
{
    public sealed class UnityPlayablePresentationController : MonoBehaviour
    {
        public UnityPresentationWorld World { get; private set; }
        public UnityInteractiveClient Client { get; private set; }
        public static UnityPlayablePresentationController CreateSynthetic(string name = "SyntheticPlayablePresentation")
        {
            GameObject root = new GameObject(name); var controller = root.AddComponent<UnityPlayablePresentationController>(); controller.World = root.AddComponent<UnityPresentationWorld>(); controller.Client = root.AddComponent<UnityInteractiveClient>(); controller.World.Configure(new UnityPresentationWorldPolicy()); controller.Client.Configure(new UnityInteractiveClientPolicy(), new IsometricPointerProfile()); return controller;
        }

        public UnityPresentationApplyResult Apply(ObjectVisualPresentationResult objects, EffectPresentationResult effects)
        {
            if (World == null) { return new UnityPresentationApplyResult(false, 0, new[] { new UnityRenderDiagnostic("WorldUnavailable", "WorldUnavailable") }); }
            return World.Apply(ObjectVisualDrawCommandBuilder.Build(objects), effects);
        }

        public PlayablePresentationRunResult RunSynthetic(PlayablePresentationPolicy policy)
        { return new PlayablePresentationCloseoutHarness().Run(policy); }
    }
}
