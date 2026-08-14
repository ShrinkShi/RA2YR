using System;
using NUnit.Framework;
using RA2YR.Presentation;

namespace RA2YR.Tests.EditMode
{
    public sealed class EffectsDepthFogTests
    {
        private static EffectPresentationDescriptor Effect(string id, long ordinal, PresentationVisibilityState visibility = PresentationVisibilityState.Visible, PresentationElevationLayer layer = PresentationElevationLayer.Ground, long anchorY = 0, long adjust = 0, int duplicate = 0)
        {
            return new EffectPresentationDescriptor(id, new VisualAssetId(id), PresentationEffectKind.Explosion, layer, new PresentationAnchor(PresentationAnchorKind.RenderPivot, 0, anchorY), new PresentationBounds(PresentationBoundsKind.Visual, -1, -1, 1, 1), new PresentationBounds(PresentationBoundsKind.ConservativeCulling, -2, -2, 2, 2), PresentationAlphaMode.Translucent, PresentationDepthTestMode.TestOnly, visibility, ordinal, adjust, 0, duplicate);
        }

        private static ShadowPresentationDescriptor Shadow(string id, long ordinal, PresentationShadowSourceKind source = PresentationShadowSourceKind.ShpFrameCandidate)
        {
            return new ShadowPresentationDescriptor(id, "caster", PresentationElevationLayer.Ground, source, new PresentationAnchor(PresentationAnchorKind.ShadowAnchor, 0, 0), new PresentationBounds(PresentationBoundsKind.Shadow, -2, -1, 2, 1), PresentationShadowColorProfile.AlphaMultiply, ordinal);
        }

        [Test] public void VisibleEffectIsSubmitted()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("visible", 0) }, null); Assert.IsTrue(result.IsSuccess); Assert.IsTrue(result.Entries[0].IsVisuallySubmitted); }

        [Test] public void FoggedEffectRemainsLogicalButIsNotSubmitted()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("fogged", 0, PresentationVisibilityState.Fogged) }, null); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(1, result.Entries.Count); Assert.IsFalse(result.Entries[0].IsVisuallySubmitted); }

        [Test] public void ShroudedEffectRemainsLogicalButIsNotSubmitted()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("shrouded", 0, PresentationVisibilityState.Shrouded) }, null); Assert.AreEqual(1, result.Entries.Count); Assert.IsFalse(result.Entries[0].IsVisuallySubmitted); }

        [Test] public void UnknownVisibilityCanBePreservedExplicitly()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("unknown", 0, PresentationVisibilityState.Unknown) }, null, new EffectPresentationPolicy(unknownVisibility: PresentationUnknownVisibilityPolicy.PreserveUnresolved)); Assert.IsTrue(result.IsSuccess); Assert.IsFalse(result.Entries[0].IsVisuallySubmitted); }

        [Test] public void UnknownVisibilityCanBeRejectedExplicitly()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("unknown", 0, PresentationVisibilityState.Unknown) }, null, new EffectPresentationPolicy(unknownVisibility: PresentationUnknownVisibilityPolicy.Reject)); Assert.IsFalse(result.IsSuccess); Assert.AreEqual(0, result.Entries.Count); Assert.IsTrue(Has(result, EffectPresentationDiagnosticCode.UnknownVisibility)); }

        [Test] public void ZeroDiagnosticBudgetCannotFailOpenUnknownVisibility()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("unknown", 0, PresentationVisibilityState.Unknown) }, null, new EffectPresentationPolicy(maxDiagnostics: 0, unknownVisibility: PresentationUnknownVisibilityPolicy.Reject)); Assert.IsFalse(result.IsSuccess); Assert.AreEqual(0, result.Entries.Count); Assert.Greater(result.Execution.SuppressedDiagnosticCount, 0); }

        [Test] public void EffectDepthUsesExplicitLayerAndCheckedTuple()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("air", 0, layer: PresentationElevationLayer.Air, anchorY: 1), Effect("ground", 1, anchorY: 100) }, null); Assert.AreEqual("ground", result.Entries[0].Descriptor.StableIdentity); Assert.AreEqual((int)PresentationElevationLayer.Air, result.Entries[1].DepthKey.Elevation); }

        [Test] public void EffectDepthTieUsesSourceOrdinal()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("late", 8), Effect("early", 2) }, null); Assert.AreEqual("early", result.Entries[0].Descriptor.StableIdentity); }

        [Test] public void EffectDepthOverflowFailsClosed()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("overflow", 0, anchorY: long.MaxValue, adjust: 1) }, null); Assert.IsFalse(result.IsSuccess); Assert.IsTrue(Has(result, EffectPresentationDiagnosticCode.DepthComponentOverflow)); }

        [Test] public void DuplicateEffectsCanBePreservedAndDiagnosed()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("same", 0, duplicate: 0), Effect("same", 1, duplicate: 1) }, null); Assert.IsTrue(result.IsSuccess); Assert.AreEqual(2, result.Entries.Count); Assert.IsTrue(Has(result, EffectPresentationDiagnosticCode.DuplicateStableIdentity)); }

        [Test] public void DuplicateEffectsCanBeRejected()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("same", 0), Effect("same", 1) }, null, new EffectPresentationPolicy(duplicates: PresentationDuplicateObjectPolicy.RejectAnyDuplicate)); Assert.IsFalse(result.IsSuccess); Assert.AreEqual(1, result.Entries.Count); }

        [Test] public void EffectDepthKeepsPrimaryAndAdjustIndependent()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("adjusted", 0, anchorY: 10, adjust: 5) }, null); Assert.AreEqual(10, result.Entries[0].DepthKey.Primary); Assert.AreEqual(5, result.Entries[0].DepthKey.Adjust); }

        [Test] public void EffectDepthKeyEqualityHasEqualHash()
        { EffectDepthKey left = new EffectDepthKey(1, 2, 3, 4, 5, "same", 6); EffectDepthKey right = new EffectDepthKey(1, 2, 3, 4, 5, "same", 6); Assert.IsTrue(left.Equals(right)); Assert.AreEqual(left.GetHashCode(), right.GetHashCode()); }

        [Test] public void EffectBudgetStopsInput()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("one", 0), Effect("two", 1) }, null, new EffectPresentationPolicy(maxEffects: 1)); Assert.IsFalse(result.IsSuccess); Assert.AreEqual(1, result.Entries.Count); }

        [Test] public void NullEffectStopsEnumeration()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new EffectPresentationDescriptor[] { null, Effect("never", 9) }, null); Assert.IsFalse(result.IsSuccess); Assert.AreEqual(0, result.Entries.Count); }

        [Test] public void ShadowIsSeparateAndCannotAffectOccupancy()
        { ShadowPresentationDescriptor shadow = Shadow("shadow", 0); Assert.IsFalse(shadow.AffectsOccupancy); Assert.AreEqual(PresentationBoundsKind.Shadow, shadow.ShadowBounds.Kind); }

        [Test] public void ShadowSourceNoneIsDiagnosedWithoutInventingGeometry()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(null, new[] { Shadow("shadow", 0, PresentationShadowSourceKind.None) }); Assert.IsTrue(result.IsSuccess); Assert.IsTrue(Has(result, EffectPresentationDiagnosticCode.ShadowSourceMissing)); }

        [Test] public void ShadowBudgetFailsClosed()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(null, new[] { Shadow("a", 0), Shadow("b", 1) }, new EffectPresentationPolicy(maxShadows: 1)); Assert.IsFalse(result.IsSuccess); Assert.AreEqual(1, result.Shadows.Count); }

        [Test] public void EffectAlphaAndDepthPoliciesRemainExplicit()
        { EffectPresentationDescriptor descriptor = Effect("alpha", 0); Assert.AreEqual(PresentationAlphaMode.Translucent, descriptor.AlphaMode); Assert.AreEqual(PresentationDepthTestMode.TestOnly, descriptor.DepthTestMode); }

        [Test] public void ParentIdentityIsRetainedForAttachedEffect()
        { EffectPresentationDescriptor descriptor = new EffectPresentationDescriptor("attached", new VisualAssetId("attached"), PresentationEffectKind.Animation, PresentationElevationLayer.Ground, new PresentationAnchor(PresentationAnchorKind.AttachmentPivot, 1, 2), new PresentationBounds(PresentationBoundsKind.Visual, 0, 0, 1, 1), new PresentationBounds(PresentationBoundsKind.ConservativeCulling, 0, 0, 2, 2), PresentationAlphaMode.Cutout, PresentationDepthTestMode.TestOnly, PresentationVisibilityState.Visible, 0, parentStableId: 42); Assert.AreEqual(42, descriptor.ParentStableId); }

        [Test] public void NullSourcesFailClosed()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(null, null); Assert.IsFalse(result.IsSuccess); }

        [Test] public void PresentationDoesNotDependOnUnity()
        { Assert.IsFalse(typeof(EffectPresentationDescriptor).Assembly.FullName.Contains("UnityEngine")); }

        [Test] public void ShadowReceiverLayerIsExplicit()
        { ShadowPresentationDescriptor shadow = Shadow("bridge-shadow", 0); Assert.AreEqual(PresentationElevationLayer.Ground, shadow.ReceiverLayer); }

        [Test] public void LogicalVisibilityFilteringDoesNotDeleteEntries()
        { EffectPresentationResult result = EffectPresentationComposer.Compose(new[] { Effect("visible", 0), Effect("fogged", 1, PresentationVisibilityState.Fogged) }, null); Assert.AreEqual(2, result.Entries.Count); }

        private static bool Has(EffectPresentationResult result, EffectPresentationDiagnosticCode code)
        { foreach (EffectPresentationDiagnostic diagnostic in result.Diagnostics) if (diagnostic.Code == code) return true; return false; }
    }
}
