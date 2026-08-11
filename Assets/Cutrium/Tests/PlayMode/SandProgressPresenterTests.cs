using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class SandProgressPresenterTests
    {
        [Test]
        public void CurrentOverTarget_DrivesFillAndExactCombinedText()
        {
            using (var rig = new IsolatedRig(0.97f))
            {
                Assert.That(rig.CaptureToX(3.5f), Is.True);
                rig.Presenter.AdvancePresentation(0f);

                Assert.That(rig.Controller.Session.CapturedFraction,
                    Is.EqualTo(0.35f).Within(0.0001f));
                Assert.That(rig.Presenter.DisplayedCapturedFraction, Is.Zero);
                Assert.That(rig.Presenter.WaitingForSandArrival, Is.True);

                rig.Presenter.NotifySandArrived(
                    rig.Controller.Session.CapturedFraction);
                rig.Presenter.AdvancePresentation(
                    rig.Presenter.AnimationSeconds);

                Assert.That(rig.Presenter.DisplayedCapturedFraction,
                    Is.EqualTo(rig.Controller.Session.CapturedFraction));
                Assert.That(rig.Presenter.CurrentFillRatio,
                    Is.EqualTo(0.35f / 0.97f).Within(0.0001f));
                Assert.That(
                    rig.Presenter.FillMaskRect.rect.width
                        / rig.Presenter.FillImage.rectTransform.rect.width,
                    Is.EqualTo(0.35f / 0.97f).Within(0.0001f));
                Assert.That(rig.Presenter.ProgressText.text,
                    Is.EqualTo("35% / 97%"));
            }
        }

        [Test]
        public void CurrentAtOrAboveTarget_EndsAtFullVisualBar()
        {
            using (var rig = new IsolatedRig(0.35f))
            {
                Assert.That(rig.CaptureToX(4f), Is.True);
                float logical = rig.Controller.Session.CapturedFraction;
                Assert.That(logical, Is.EqualTo(0.4f).Within(0.0001f));

                rig.Presenter.AdvancePresentation(0f);
                rig.Presenter.NotifySandArrived(logical);
                rig.Presenter.AdvancePresentation(
                    rig.Presenter.AnimationSeconds);

                Assert.That(rig.Presenter.CurrentFillRatio, Is.EqualTo(1f));
                Assert.That(rig.Presenter.FillMaskRect.rect.width,
                    Is.EqualTo(
                        rig.Presenter.FillImage.rectTransform.rect.width)
                        .Within(0.001f));
                Assert.That(rig.Presenter.ProgressText.text,
                    Is.EqualTo("40% / 35%"));
            }
        }

        [Test]
        public void ArrivalStartsSmoothlyInsteadOfJumpingAheadOfSand()
        {
            using (var rig = new IsolatedRig(0.97f))
            {
                Assert.That(rig.CaptureToX(3.5f), Is.True);
                float logical = rig.Controller.Session.CapturedFraction;

                rig.Presenter.AdvancePresentation(0.1f);
                Assert.That(rig.Presenter.DisplayedCapturedFraction, Is.Zero);

                rig.Presenter.NotifySandArrived(logical);
                rig.Presenter.AdvancePresentation(
                    rig.Presenter.AnimationSeconds * 0.5f);
                Assert.That(rig.Presenter.DisplayedCapturedFraction,
                    Is.GreaterThan(0f));
                Assert.That(rig.Presenter.DisplayedCapturedFraction,
                    Is.LessThan(logical));

                rig.Presenter.AdvancePresentation(
                    rig.Presenter.AnimationSeconds * 0.5f);
                Assert.That(rig.Presenter.DisplayedCapturedFraction,
                    Is.EqualTo(logical));
            }
        }

        [Test]
        public void RapidCapturesRetargetAndStaleArrivalCannotMoveBackward()
        {
            using (var rig = new IsolatedRig(0.95f))
            {
                Assert.That(rig.CaptureToX(2f), Is.True);
                float first = rig.Controller.Session.CapturedFraction;
                rig.Presenter.AdvancePresentation(0f);
                rig.Presenter.NotifySandArrived(first);
                rig.Presenter.AdvancePresentation(
                    rig.Presenter.AnimationSeconds * 0.35f);
                float midAnimation =
                    rig.Presenter.DisplayedCapturedFraction;

                Assert.That(rig.CaptureToX(4f), Is.True);
                float latest = rig.Controller.Session.CapturedFraction;
                Assert.That(latest, Is.GreaterThan(first));
                rig.Presenter.AdvancePresentation(0f);
                rig.Presenter.NotifySandArrived(latest);
                rig.Presenter.NotifySandArrived(first);
                rig.Presenter.AdvancePresentation(
                    rig.Presenter.AnimationSeconds);

                Assert.That(rig.Presenter.DisplayedCapturedFraction,
                    Is.GreaterThanOrEqualTo(midAnimation));
                Assert.That(rig.Presenter.DisplayedCapturedFraction,
                    Is.EqualTo(latest));
                Assert.That(rig.Presenter.LatestLogicalCapturedFraction,
                    Is.EqualTo(rig.Controller.Session.CapturedFraction));
            }
        }

        [Test]
        public void MissingSandNotificationFallsBackAndStillSettlesExactly()
        {
            using (var rig = new IsolatedRig(0.97f))
            {
                Assert.That(rig.CaptureToX(3.5f), Is.True);
                float logical = rig.Controller.Session.CapturedFraction;

                rig.Presenter.AdvancePresentation(
                    rig.Presenter.ArrivalFallbackSeconds);
                rig.Presenter.AdvancePresentation(
                    rig.Presenter.AnimationSeconds);

                Assert.That(rig.Presenter.DisplayedCapturedFraction,
                    Is.EqualTo(logical));
                Assert.That(rig.Presenter.ProgressText.text,
                    Is.EqualTo("35% / 97%"));
            }
        }

        [Test]
        public void RetryAndNextSessionResetPresentationImmediately()
        {
            using (var rig = new IsolatedRig(0.35f, levelCount: 2))
            {
                Assert.That(rig.CaptureToX(4f), Is.True);
                rig.Presenter.AdvancePresentation(0f);
                rig.Presenter.NotifySandArrived(
                    rig.Controller.Session.CapturedFraction);
                rig.Presenter.AdvancePresentation(
                    rig.Presenter.AnimationSeconds);
                Assert.That(rig.Presenter.CurrentFillRatio, Is.EqualTo(1f));

                rig.Controller.RetryLevel();
                rig.Presenter.AdvancePresentation(0f);
                Assert.That(rig.Presenter.DisplayedCapturedFraction, Is.Zero);
                Assert.That(rig.Presenter.CurrentFillRatio, Is.Zero);
                Assert.That(rig.Presenter.ProgressText.text,
                    Is.EqualTo("0% / 35%"));

                Assert.That(rig.CaptureToX(4f), Is.True);
                Assert.That(rig.Controller.TryAdvanceToNextLevel(), Is.True);
                rig.Presenter.AdvancePresentation(0f);
                Assert.That(rig.Presenter.DisplayedCapturedFraction, Is.Zero);
                Assert.That(rig.Presenter.ProgressText.text,
                    Is.EqualTo("0% / 35%"));
            }
        }

        [Test]
        public void PresentationDisabled_DoesNotAffectDeterministicCapture()
        {
            using (var rig = new IsolatedRig(0.97f))
            {
                rig.Presenter.enabled = false;

                Assert.That(rig.CaptureToX(3.5f), Is.True);

                Assert.That(rig.Controller.Session.CapturedFraction,
                    Is.EqualTo(0.35f).Within(0.0001f));
                Assert.That(rig.Controller.Session.Board.CapturedRooms,
                    Has.Count.EqualTo(1));
            }
        }

        private sealed class IsolatedRig : System.IDisposable
        {
            private readonly GameObject _root;

            public IsolatedRig(float target, int levelCount = 1)
            {
                _root = new GameObject(
                    "SandProgressTestRig",
                    typeof(RectTransform));
                _root.SetActive(false);
                var rootRect = (RectTransform)_root.transform;
                rootRect.sizeDelta = new Vector2(1000f, 1000f);

                Controller = _root.AddComponent<FirstPlayableController>();
                var levels = new CoreFunLevelDefinition[levelCount];
                for (int index = 0; index < levels.Length; index++)
                {
                    levels[index] = new CoreFunLevelDefinition(
                        $"progress-{index}",
                        index + 1,
                        new Vector2(9f, 8f),
                        Vector2.up,
                        0.1f,
                        0.2f,
                        target,
                        100f,
                        0.05f,
                        0.1f,
                        8,
                        16,
                        8,
                        "Sand progress test level.",
                        10f);
                }

                Controller.ConfigureLevelsForSetup(levels);

                RectTransform boardFrame = CreateRect(
                    rootRect,
                    "BoardFrame",
                    new Vector2(625f, 1000f));
                RectTransform progressRect = CreateRect(
                    rootRect,
                    "ProgressBar",
                    new Vector2(720f, 80f));
                Image background = CreateImage(progressRect, "Background");
                RectTransform fillMask = CreateRect(
                    progressRect,
                    "FillMask",
                    new Vector2(700f, 40f));
                Image fill = CreateImage(fillMask, "Fill");
                Image frame = CreateImage(progressRect, "Frame");
                Text text = CreateText(progressRect, "ProgressText");
                RectTransform fillStart = CreateRect(
                    fillMask,
                    "FillStartTarget",
                    Vector2.zero);

                Presenter = progressRect.gameObject
                    .AddComponent<SandProgressPresenter>();
                Presenter.Configure(
                    Controller,
                    boardFrame,
                    progressRect,
                    background,
                    fillMask,
                    fill,
                    frame,
                    text,
                    fillStart);
                Presenter.ConfigureAnimationForSetup(0.5f, 0.8f, 0.86f);
                _root.SetActive(true);
                Presenter.RefreshNow();
            }

            public FirstPlayableController Controller { get; }
            public SandProgressPresenter Presenter { get; }

            public bool CaptureToX(float x)
            {
                BarrierStartResult start = Controller.SubmitBarrierIntent(
                    new BarrierIntent(
                        new LogicalPoint(x, 8f),
                        BarrierOrientation.Vertical));
                if (!start.Accepted)
                {
                    return false;
                }

                for (int tick = 0;
                     tick < 120 && Controller.Session.ActiveBarrier.HasValue;
                     tick++)
                {
                    Controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }

                return !Controller.Session.ActiveBarrier.HasValue;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(_root);
            }

            private static RectTransform CreateRect(
                Transform parent,
                string name,
                Vector2 size)
            {
                var gameObject = new GameObject(name, typeof(RectTransform));
                var rect = (RectTransform)gameObject.transform;
                rect.SetParent(parent, false);
                rect.sizeDelta = size;
                return rect;
            }

            private static Image CreateImage(Transform parent, string name)
            {
                var gameObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                gameObject.transform.SetParent(parent, false);
                return gameObject.GetComponent<Image>();
            }

            private static Text CreateText(Transform parent, string name)
            {
                var gameObject = new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                gameObject.transform.SetParent(parent, false);
                return gameObject.GetComponent<Text>();
            }
        }
    }
}
