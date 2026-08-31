using Cutrium.Unity.Input;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class SimulationHoldTests
    {
        [Test]
        public void GuidedTrainingHold_FreezesSimulationButLeavesGestureEnabled()
        {
            var gameObject = new GameObject("SimulationHoldTest");
            try
            {
                var gesture = gameObject.AddComponent<BarrierGestureAdapter>();
                gesture.Configure(null, 0.35f, 0.1f);
                var controller = gameObject.AddComponent<
                    FirstPlayableController>();
                controller.ConfigureBarrierForSetup(
                    gesture,
                    3.4f,
                    0.08f,
                    3f,
                    16);

                controller.SetSimulationHold(
                    SimulationHoldReason.GuidedTraining,
                    true);

                Assert.That(controller.SimulationHeld, Is.True);
                Assert.That(controller.BarrierInputBlocked, Is.False);
                Assert.That(gesture.enabled, Is.True);

                controller.SetSimulationHold(
                    SimulationHoldReason.GuidedTraining,
                    false);

                Assert.That(controller.SimulationHeld, Is.False);
                Assert.That(gesture.enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [TestCase(SimulationHoldReason.Legacy)]
        [TestCase(SimulationHoldReason.PreLevelIntro)]
        [TestCase(SimulationHoldReason.FrontEnd)]
        [TestCase(SimulationHoldReason.Settings)]
        public void InputBlockingHolds_DisableGestureEvenDuringTraining(
            SimulationHoldReason reason)
        {
            var gameObject = new GameObject("SimulationHoldTest");
            try
            {
                var gesture = gameObject.AddComponent<BarrierGestureAdapter>();
                gesture.Configure(null, 0.35f, 0.1f);
                var controller = gameObject.AddComponent<
                    FirstPlayableController>();
                controller.ConfigureBarrierForSetup(
                    gesture,
                    3.4f,
                    0.08f,
                    3f,
                    16);

                controller.SetSimulationHold(
                    SimulationHoldReason.GuidedTraining,
                    true);
                controller.SetSimulationHold(reason, true);

                Assert.That(controller.BarrierInputBlocked, Is.True);
                Assert.That(gesture.enabled, Is.False);

                controller.SetSimulationHold(reason, false);

                Assert.That(controller.BarrierInputBlocked, Is.False);
                Assert.That(gesture.enabled, Is.True);
                Assert.That(controller.SimulationHeld, Is.True);

                controller.SetSimulationHold(
                    SimulationHoldReason.GuidedTraining,
                    false);
                Assert.That(controller.SimulationHeld, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
