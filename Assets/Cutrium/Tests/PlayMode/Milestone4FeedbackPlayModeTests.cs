using System.Collections;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Feedback;
using Cutrium.Gameplay.Geometry;
using Cutrium.Presentation.Barriers;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.Feedback;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Bootstrap;
using Cutrium.Unity.Layout;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class Milestone4FeedbackPlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        private GameObject _root;
        private FirstPlayableController _controller;
        private CaptureBoardPresenter _boardPresenter;
        private BarrierPresenter _barrierPresenter;
        private CaptureHudPresenter _hud;
        private FeedbackPresenter _feedback;
        private FeedbackAudioPresenter _audio;
        private FeedbackHapticPresenter _haptics;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
            yield return null;
            _root = SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            _controller = _root
                .GetComponentInChildren<FirstPlayableController>(true);
            _boardPresenter = _root
                .GetComponentInChildren<CaptureBoardPresenter>(true);
            _barrierPresenter = _root
                .GetComponentInChildren<BarrierPresenter>(true);
            _hud = _root.GetComponentInChildren<CaptureHudPresenter>(true);
            _feedback = _root
                .GetComponentInChildren<FeedbackPresenter>(true);
            _audio = _root
                .GetComponentInChildren<FeedbackAudioPresenter>(true);
            _haptics = _root
                .GetComponentInChildren<FeedbackHapticPresenter>(true);
        }

        [Test]
        public void Scene_HasExplicitOptionalFeedbackComposition()
        {
            Assert.That(_controller.FeedbackTuning, Is.Not.Null);
            Assert.That(_feedback, Is.Not.Null);
            Assert.That(_feedback.Controller, Is.SameAs(_controller));
            Assert.That(_feedback.Tuning,
                Is.SameAs(_controller.FeedbackTuning));
            Assert.That(_feedback.CueLabel, Is.Not.Null);
            Assert.That(_feedback.CueCanvasGroup, Is.Not.Null);
            Assert.That(_feedback.CueCanvasGroup.blocksRaycasts, Is.False);
            Assert.That(_audio.Controller, Is.SameAs(_controller));
            Assert.That(_audio.AudioSource, Is.Not.Null);
            Assert.That(_audio.AudioSource.playOnAwake, Is.False);
            Assert.That(_audio.AudioSource.loop, Is.False);
            Assert.That(_haptics.Controller, Is.SameAs(_controller));
            Assert.That(_boardPresenter.CaptureRevealDuration,
                Is.EqualTo(_controller.FeedbackTuning.CaptureRevealSeconds));
            Assert.That(_hud.PercentageAnimationSeconds,
                Is.EqualTo(_controller.FeedbackTuning
                    .PercentageAnimationSeconds));
        }

        [Test]
        public void BarrierStartAndGrowth_AreDeliveredOncePerSubscription()
        {
            _feedback.enabled = false;
            _feedback.enabled = true;
            _feedback.enabled = false;
            _feedback.enabled = true;
            int feedbackBefore = _feedback.ReceivedEventCount;
            int audioBefore = _audio.HandledEventCount;

            BarrierStartResult start = _controller.SubmitBarrierIntent(
                new BarrierIntent(
                    new LogicalPoint(5f, 4f),
                    BarrierOrientation.Horizontal));

            Assert.That(start.Accepted, Is.True);
            Assert.That(_feedback.ReceivedEventCount,
                Is.EqualTo(feedbackBefore + 1));
            Assert.That(_feedback.LastEventKind,
                Is.EqualTo(FeedbackEventKind.BarrierStarted));
            Assert.That(_barrierPresenter.LastFeedbackEventKind,
                Is.EqualTo(FeedbackEventKind.BarrierStarted));
            Assert.That(_audio.HandledEventCount,
                Is.EqualTo(audioBefore + 1));

            _controller.AdvanceSimulation(
                FirstPlayableController.SimulationStep);

            Assert.That(_feedback.LastEventKind,
                Is.EqualTo(FeedbackEventKind.BarrierGrowing));
            Assert.That(_barrierPresenter.LastFeedbackEventKind,
                Is.EqualTo(FeedbackEventKind.BarrierGrowing));
            Assert.That(_feedback.GrowthIntensity, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator CapturingLock_RevealsFlatFallbackAndPercentageLandsExactly()
        {
            BarrierStartResult start = _controller.SubmitBarrierIntent(
                new BarrierIntent(
                    new LogicalPoint(5f, 4f),
                    BarrierOrientation.Horizontal));
            Assert.That(start.Accepted, Is.True);
            for (int tick = 0;
                 tick < 240 && _controller.Session.ActiveBarrier.HasValue;
                 tick++)
            {
                _controller.AdvanceSimulation(
                    FirstPlayableController.SimulationStep);
            }

            Assert.That(_controller.Session.LockedBarrierCount,
                Is.EqualTo(1));
            Assert.That(_controller.Session.CapturedFraction,
                Is.GreaterThan(0f));
            _boardPresenter.RefreshNow();
            Assert.That(_boardPresenter.VisibleCapturedRegionCount,
                Is.EqualTo(1));
            Image fallback = _boardPresenter.CapturedRegionRoot
                .GetChild(0)
                .GetComponent<Image>();
            Assert.That(fallback, Is.Not.Null);
            Assert.That(fallback.color.a, Is.GreaterThan(0f));
            Assert.That(_boardPresenter.AllVisibleCapturesRevealed, Is.False);

            _hud.AdvancePercentageAnimation(0f);
            Assert.That(_hud.DisplayedCapturedFraction,
                Is.LessThan(_controller.Session.CapturedFraction));
            _hud.AdvancePercentageAnimation(
                _hud.PercentageAnimationSeconds);
            Assert.That(_hud.DisplayedCapturedFraction,
                Is.EqualTo(_controller.Session.CapturedFraction));

            yield return new WaitForSecondsRealtime(
                _boardPresenter.CaptureRevealDuration + 0.05f);
            _boardPresenter.RefreshNow();
            Assert.That(_boardPresenter.AllVisibleCapturesRevealed, Is.True);
        }

        [Test]
        public void Retry_ResetsLogicalRewardsAndPresentationQueue()
        {
            _controller.SubmitBarrierIntent(new BarrierIntent(
                new LogicalPoint(5f, 4f),
                BarrierOrientation.Horizontal));
            _controller.RetryLevel();

            Assert.That(_controller.Session.ComboCount, Is.Zero);
            Assert.That(_controller.Session.CapturedFraction, Is.Zero);
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_feedback.LastEventKind,
                Is.EqualTo(FeedbackEventKind.SessionReset));
            Assert.That(_feedback.PendingCueCount, Is.Zero);
        }

        [Test]
        public void MissingAudioAndNoOpHaptics_AreAlwaysSafe()
        {
            Assert.DoesNotThrow(() => _audio.PlayUi());
            int before = _haptics.HandledCueCount;
            Assert.DoesNotThrow(() => _haptics.PlayUi());
            Assert.That(_haptics.HandledCueCount, Is.EqualTo(before + 1));
            Assert.That(_audio.AudioSource.isPlaying, Is.False);
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void FeedbackOverlay_PreservesBoardFitAndNeverBlocksInput(
            float width,
            float height)
        {
            RectTransform overlay =
                (RectTransform)_feedback.transform;
            Assert.That(overlay.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(overlay.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(overlay.GetComponent<LayoutElement>().ignoreLayout,
                Is.True);
            Assert.That(_feedback.CueCanvasGroup.blocksRaycasts, Is.False);

            Rect viewport = new Rect(0f, 0f, width, height - 96f);
            Rect board = BoardViewportLayout.CalculateAspectFitRect(viewport);
            Assert.That(board.width / board.height,
                Is.EqualTo(10f / 16f).Within(0.0001f));
            Assert.That(board.xMin, Is.GreaterThanOrEqualTo(viewport.xMin));
            Assert.That(board.yMin, Is.GreaterThanOrEqualTo(viewport.yMin));
            Assert.That(board.xMax, Is.LessThanOrEqualTo(viewport.xMax));
            Assert.That(board.yMax, Is.LessThanOrEqualTo(viewport.yMax));
            Assert.That(BoardViewportLayout.LogicalSize,
                Is.EqualTo(new Vector2(10f, 16f)));
        }

        [Test]
        public void CompletionOverlay_RemainsFinalAndRaycastAuthority()
        {
            Assert.That(_hud.CompleteOverlay.transform.GetSiblingIndex(),
                Is.EqualTo(
                    _hud.CompleteOverlay.transform.parent.childCount - 1));
            Assert.That(_feedback.transform.GetSiblingIndex(),
                Is.LessThan(_hud.CompleteOverlay.transform.GetSiblingIndex()));
            Assert.That(_feedback.CueCanvasGroup.blocksRaycasts, Is.False);
            Assert.That(_hud.CompletionCanvasGroup.blocksRaycasts, Is.False);
        }
    }
}
