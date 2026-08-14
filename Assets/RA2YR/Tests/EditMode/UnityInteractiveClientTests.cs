using System;
using NUnit.Framework;
using RA2YR.Presentation;
using RA2YR.Simulation;
using RA2YR.UnityIntegration;
using UnityEngine;

namespace RA2YR.Tests.EditMode
{
    public sealed class UnityInteractiveClientTests
    {
        [Test]
        public void AdapterRegistersBoundedPickTarget()
        {
            UnityInteractiveClient client = UnityInteractiveClient.CreateSynthetic();
            try { Assert.That(client.RegisterPickTarget(new UnityPickTarget(new EntityId(0, 1), new CellCoordinate(0, 0), "unit")), Is.True); Assert.That(client.PickTargets, Has.Count.EqualTo(1)); }
            finally { UnityEngine.Object.DestroyImmediate(client.gameObject); }
        }

        [Test]
        public void AdapterSelectsAtExplicitScreenCell()
        {
            UnityInteractiveClient client = UnityInteractiveClient.CreateSynthetic();
            try { client.RegisterPickTarget(new UnityPickTarget(new EntityId(0, 1), new CellCoordinate(0, 0), "unit")); SelectionResult result = client.SelectAt(new Vector2(640, 360)); Assert.That(result.IsSuccess, Is.True); Assert.That(client.Selection.Entities, Has.Count.EqualTo(1)); }
            finally { UnityEngine.Object.DestroyImmediate(client.gameObject); }
        }

        [Test]
        public void AdapterDoesNotSelectOutsideViewport()
        {
            UnityInteractiveClient client = UnityInteractiveClient.CreateSynthetic();
            try { SelectionResult result = client.SelectAt(new Vector2(-1, 0)); Assert.That(result.IsSuccess, Is.False); Assert.That(client.Selection.Entities, Has.Count.EqualTo(0)); }
            finally { UnityEngine.Object.DestroyImmediate(client.gameObject); }
        }

        [Test]
        public void AdapterSubmitsOnlyHumanCommands()
        {
            var queue = new CommandQueue(); UnityInteractiveClient client = UnityInteractiveClient.CreateSynthetic();
            try { client.Configure(new UnityInteractiveClientPolicy(), new IsometricPointerProfile(), queue); client.SetSelection(SelectionService.Replace(new[] { new EntityId(0, 1) }).Selection); ClientCommandResult result = client.SubmitCommand(CommandKind.Move, new CellCoordinate(1, 1), null, 4); Assert.That(result.IsSuccess, Is.True); Assert.That(queue.SnapshotCanonical()[0].Source, Is.EqualTo(CommandSource.Human)); }
            finally { UnityEngine.Object.DestroyImmediate(client.gameObject); }
        }

        [Test]
        public void AdapterRefreshesReadOnlyHud()
        {
            var world = new SimulationWorld(1); EntityId entity = world.CreateEntity(); world.Positions.Set(entity, new PositionComponent(0, 0)); UnityInteractiveClient client = UnityInteractiveClient.CreateSynthetic();
            try { client.SetSelection(SelectionService.Replace(new[] { entity }).Selection); client.RefreshHud(world.CaptureSnapshot(), 25, true, "Assisted"); Assert.That(client.Hud.SelectedCount, Is.EqualTo(1)); Assert.That(client.Hud.Credits, Is.EqualTo(25)); Assert.That(client.Hud.LowPower, Is.True); }
            finally { UnityEngine.Object.DestroyImmediate(client.gameObject); }
        }

        [Test]
        public void AdapterProducesExplicitPlacementPreview()
        { UnityInteractiveClient client = UnityInteractiveClient.CreateSynthetic(); try { PlacementPreview preview = client.PreviewPlacement(new CellCoordinate(3, 4), false, true); Assert.That(preview.IsValid, Is.True); } finally { UnityEngine.Object.DestroyImmediate(client.gameObject); } }

        [Test]
        public void AdapterStoresEnvironmentProfileWithoutSimulationMutation()
        { UnityInteractiveClient client = UnityInteractiveClient.CreateSynthetic(); try { client.SetEnvironment(new EnvironmentPresentationProfile(LightingProfile.Storm, WeatherProfile.Sandstorm, 75)); Assert.That(client.Environment.Weather, Is.EqualTo(WeatherProfile.Sandstorm)); } finally { UnityEngine.Object.DestroyImmediate(client.gameObject); } }

        [Test]
        public void AdapterTargetBudgetFailsClosed()
        { UnityInteractiveClient client = UnityInteractiveClient.CreateSynthetic(); try { client.Configure(new UnityInteractiveClientPolicy(maxPickTargets: 0), new IsometricPointerProfile()); Assert.That(client.RegisterPickTarget(new UnityPickTarget(new EntityId(0, 1), new CellCoordinate(0, 0), "unit")), Is.False); } finally { UnityEngine.Object.DestroyImmediate(client.gameObject); } }
    }
}
