using System.Collections;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class SandBowlPresenterTests
    {
        [Test]
        public void FillFraction_TracksCapturedFractionExactly()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();
                Assert.That(rig.Presenter.CurrentFillFraction, Is.Zero);

                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                // The bowl's fill level is the single source of truth for
                // "how full is the bowl" -- it must match CapturedFraction
                // exactly and immediately, never lag behind or approximate
                // it via a separately-timed animation.
                Assert.That(
                    rig.Presenter.CurrentFillFraction,
                    Is.EqualTo(rig.Controller.Session.CapturedFraction)
                        .Within(0.0001f));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void TargetText_ShowsConfiguredTargetPercentage()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();

                int expectedPercent = Mathf.FloorToInt(
                    (rig.Controller.Session.TargetCapturedFraction * 100f)
                    + 0.5f);
                Assert.That(
                    rig.Presenter.TargetText.text,
                    Is.EqualTo($"Target {expectedPercent}%"));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void Retry_ResetsFillFractionToEmpty()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();

                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();
                Assert.That(rig.Presenter.CurrentFillFraction, Is.GreaterThan(0f));

                rig.Controller.RetryLevel();
                rig.Presenter.RefreshNow();

                Assert.That(rig.Presenter.CurrentFillFraction, Is.Zero);
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void NextLevel_ResetsFillFractionToEmpty()
        {
            var rig = new IsolatedRig(2);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();

                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();
                Assert.That(rig.Presenter.CurrentFillFraction, Is.GreaterThan(0f));

                Assert.That(rig.Controller.TryAdvanceToNextLevel(), Is.True);
                rig.Presenter.RefreshNow();

                Assert.That(rig.Presenter.CurrentFillFraction, Is.Zero);
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator DisablingPresentationDoesNotChangeGameplayState()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();
                rig.Presenter.enabled = false;

                bool completed = rig.CompleteWithoutAdvancing();
                yield return null;

                Assert.That(completed, Is.True);
                Assert.That(
                    rig.Controller.Session.CapturedFraction,
                    Is.GreaterThan(0f));
            }
            finally
            {
                rig.Dispose();
            }
        }

        private sealed class IsolatedRig
        {
            private readonly GameObject _simulationObject;

            public IsolatedRig(int levelCount)
            {
                _simulationObject = new GameObject("SandBowlTestRig");
                _simulationObject.SetActive(false);
                Controller =
                    _simulationObject.AddComponent<FirstPlayableController>();

                var levels = new CoreFunLevelDefinition[levelCount];
                for (int index = 0; index < levelCount; index++)
                {
                    levels[index] = new CoreFunLevelDefinition(
                        $"tiny-{index}",
                        index + 1,
                        new Vector2(5f, 8f),
                        new Vector2(1f, 0f),
                        1f,
                        0.35f,
                        0.05f,
                        8f,
                        0.08f,
                        3f,
                        8,
                        16,
                        8,
                        "Sand bowl test level.",
                        10f);
                }

                Controller.ConfigureLevelsForSetup(levels);

                var fillRootObject = new GameObject(
                    "SandFill",
                    typeof(RectTransform));
                var sandFillRect = (RectTransform)fillRootObject.transform;
                sandFillRect.SetParent(_simulationObject.transform, false);
                sandFillRect.anchorMin = new Vector2(0f, 0f);
                sandFillRect.anchorMax = new Vector2(1f, 0f);

                var targetTextObject = new GameObject(
                    "TargetText",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                targetTextObject.transform.SetParent(
                    _simulationObject.transform,
                    false);
                Text targetText = targetTextObject.GetComponent<Text>();

                var fillTargetObject = new GameObject(
                    "FillTarget",
                    typeof(RectTransform));
                var fillTargetRect = (RectTransform)fillTargetObject.transform;
                fillTargetRect.SetParent(_simulationObject.transform, false);

                Presenter = _simulationObject.AddComponent<SandBowlPresenter>();
                Presenter.Configure(
                    Controller,
                    sandFillRect,
                    targetText,
                    fillTargetRect);

                _simulationObject.SetActive(true);
            }

            public FirstPlayableController Controller { get; }
            public SandBowlPresenter Presenter { get; }

            public bool CompleteWithoutAdvancing()
            {
                BarrierStartResult start = Controller.SubmitBarrierIntent(
                    new BarrierIntent(
                        new LogicalPoint(0.6f, 8f),
                        BarrierOrientation.Vertical));
                if (!start.Accepted)
                {
                    return false;
                }

                for (int tick = 0;
                     tick < 600
                     && Controller.Session.LevelStatus
                         != CaptureLevelStatus.Completed;
                     tick++)
                {
                    Controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }

                return Controller.Session.LevelStatus
                    == CaptureLevelStatus.Completed;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(_simulationObject);
            }
        }
    }
}
