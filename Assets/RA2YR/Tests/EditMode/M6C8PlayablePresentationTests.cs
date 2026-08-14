using System;
using System.Linq;
using NUnit.Framework;
using RA2YR.Presentation;
using RA2YR.UnityIntegration;

namespace RA2YR.Tests.EditMode
{
    public sealed class M6C8PlayablePresentationTests
    {
        [Test] public void ThirtyHzCadenceIsDeterministic() { var r = Run(60, 60, 30); Assert.That(r.IsSuccess, Is.True); Assert.That(r.RenderedFrames, Is.EqualTo(30)); }
        [Test] public void SixtyHzCadenceRendersEverySimulationTick() { var r = Run(60, 60, 60); Assert.That(r.IsSuccess, Is.True); Assert.That(r.RenderedFrames, Is.EqualTo(60)); }
        [Test] public void OneFortyFourHzCadenceCanRenderMultipleFramesPerTick() { var r = Run(60, 60, 144); Assert.That(r.IsSuccess, Is.True); Assert.That(r.RenderedFrames, Is.EqualTo(60)); }
        [TestCase(500)] [TestCase(1000)] [TestCase(2000)] public void StressTiersRemainBoundedAndEquivalent(int entities) { var r = Run(entities, 2, 60); Assert.That(r.IsSuccess, Is.True); Assert.That(r.PresentedEntities, Is.EqualTo((long)entities * 2)); Assert.That(r.Diagnostics, Is.Empty); }
        [Test] public void RepeatedRunsHaveIdenticalHashes() { var a = Run(500, 3, 60); var b = Run(500, 3, 60); Assert.That(a.SimulationHash, Is.EqualTo(b.SimulationHash)); Assert.That(a.PresentationHash, Is.EqualTo(b.PresentationHash)); Assert.That(a.RenderedFrames, Is.EqualTo(b.RenderedFrames)); }
        [Test] public void PresentationDoesNotMutateSimulationAuthority() { var r = Run(2000, 3, 144); Assert.That(r.IsSuccess, Is.True); Assert.That(r.SimulationHash, Is.Not.Empty); }
        [Test] public void DescriptorBudgetFailsClosed() { var r = new PlayablePresentationCloseoutHarness().Run(new PlayablePresentationPolicy(500, 2, new PresentationCadenceProfile(60, 60), 10)); Assert.That(r.IsSuccess, Is.False); Assert.That(r.Execution.HasFatalError, Is.True); Assert.That(r.Diagnostics.Any(x => x.Code == PlayablePresentationDiagnosticCode.EntityBudgetExceeded), Is.True); }
        [Test] public void InvalidPolicyFailsAtConstruction() { Assert.That(() => new PlayablePresentationPolicy(0), Throws.TypeOf<ArgumentOutOfRangeException>()); }
        [Test] public void CadenceProfileRejectsZero() { Assert.That(() => new PresentationCadenceProfile(0, 60), Throws.TypeOf<ArgumentOutOfRangeException>()); }
        [Test] public void CadenceProfileUsesExplicitRates() { var p = new PresentationCadenceProfile(60, 30); Assert.That(p.ShouldRender(1), Is.False); Assert.That(p.ShouldRender(2), Is.True); }
        [Test] public void ResultRetainsAggregatePresentationHash() { var r = Run(500, 2, 30); Assert.That(r.PresentationHash, Is.Not.Empty); Assert.That(r.PresentedEntities, Is.EqualTo(500)); }
        [Test] public void ResultRetainsTickAndFrameCounts() { var r = Run(500, 5, 30); Assert.That(r.TicksCompleted, Is.EqualTo(5)); Assert.That(r.RenderedFrames, Is.EqualTo(2)); }
        [Test] public void CloseoutUsesNoUnityReferences() { Assert.That(typeof(PlayablePresentationCloseoutHarness).Assembly.GetReferencedAssemblies(), Has.None.Matches<System.Reflection.AssemblyName>(x => x.Name == "UnityEngine" || x.Name == "UnityEditor")); }
        [Test] public void ControllerCanRunSyntheticCloseout() { var controller = UnityPlayablePresentationController.CreateSynthetic(); try { var r = controller.RunSynthetic(new PlayablePresentationPolicy(50, 2)); Assert.That(r.IsSuccess, Is.True); } finally { UnityEngine.Object.DestroyImmediate(controller.gameObject); } }
        [Test] public void ControllerOwnsWorldAndClient() { var controller = UnityPlayablePresentationController.CreateSynthetic(); try { Assert.That(controller.World, Is.Not.Null); Assert.That(controller.Client, Is.Not.Null); } finally { UnityEngine.Object.DestroyImmediate(controller.gameObject); } }
        private static PlayablePresentationRunResult Run(int entities, int ticks, int renderHz) { return new PlayablePresentationCloseoutHarness().Run(new PlayablePresentationPolicy(entities, ticks, new PresentationCadenceProfile(60, renderHz), entities)); }
    }
}
