using System.Collections;
using NUnit.Framework;
using RA2YR.UnityIntegration;
using UnityEngine;
using UnityEngine.TestTools;

namespace RA2YR.Tests.PlayMode
{
    public sealed class M6C6RendererSmokeTests
    {
        [UnityTest]
        public IEnumerator SyntheticPresentationWorldHasCentralLifecycle()
        {
            UnityPresentationWorld world = UnityPresentationWorld.CreateSynthetic("M6C6SmokeWorld");
            try
            {
                yield return null;
                Assert.IsNotNull(world);
                Assert.AreEqual(0, world.LastSubmissionCount);
                Assert.AreEqual(0, world.transform.childCount);
            }
            finally
            {
                if (world != null) Object.Destroy(world.gameObject);
            }
        }
    }
}
