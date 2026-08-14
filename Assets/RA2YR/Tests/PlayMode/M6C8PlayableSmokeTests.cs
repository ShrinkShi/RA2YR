using System.Collections;
using NUnit.Framework;
using RA2YR.UnityIntegration;
using UnityEngine.TestTools;

namespace RA2YR.Tests.PlayMode
{
    public sealed class M6C8PlayableSmokeTests
    {
        [UnityTest]
        public IEnumerator SyntheticPlayableControllerRunsWithoutSimulationLoop()
        {
            var controller = UnityPlayablePresentationController.CreateSynthetic();
            yield return null;
            var result = controller.RunSynthetic(new RA2YR.Presentation.PlayablePresentationPolicy(50, 2));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.RenderedFrames, Is.EqualTo(2));
            UnityEngine.Object.Destroy(controller.gameObject);
        }
    }
}
