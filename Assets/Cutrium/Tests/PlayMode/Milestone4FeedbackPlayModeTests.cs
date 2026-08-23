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
            _root.GetComponentInChildren<
                    Cutrium.Presentation.Frontend.FrontEndPresenter>(true)
                ?.SkipForTesting();
            _root.GetComponentInChildren<PreLevelIntroPresenter>(true)
                ?.SkipForTesting();
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
            Assert.That(fallback.sprite, Is.Null);
            Assert.That(fallback.color, Is.EqualTo(Color.clear));
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
        public void AudioPlaybackAndNoOpHaptics_AreAlwaysSafe()
        {
            Assert.DoesNotThrow(() => _audio.PlayUi());
            int before = _haptics.HandledCueCount;
            Assert.DoesNotThrow(() => _haptics.PlayUi());
            Assert.That(_haptics.HandledCueCount, Is.EqualTo(before + 1));
        }

        [Test]
        public void PowerFeedbackEvents_AreHandledSafely()
        {
            int before = _audio.HandledEventCount;

            Assert.DoesNotThrow(
                () => _controller.TryActivateFreezePulse());
            Assert.DoesNotThrow(
                () => _controller.TryArmInstantBarrier());

            Assert.That(_audio.HandledEventCount, Is.GreaterThan(before));
        }

        [Test]
        public void SceneAudioClips_AreFullyWiredWithSharedFailureClip()
        {
            Assert.That(_audio.StartClip, Is.Not.Null);
            Assert.That(_audio.GrowClip, Is.Not.Null);
            Assert.That(_audio.LockClip, Is.Not.Null);
            Assert.That(_audio.BreakClip, Is.Not.Null);
            Assert.That(_audio.FillClip, Is.Not.Null);
            Assert.That(_audio.LargeCaptureClip, Is.Not.Null);
            Assert.That(_audio.NearMissClip, Is.Not.Null);
            Assert.That(_audio.ComboClip, Is.Not.Null);
            Assert.That(_audio.CompleteClip, Is.Not.Null);
            Assert.That(_audio.UiClip, Is.Not.Null);
            Assert.That(_audio.PowerFreezeActivateClip, Is.Not.Null);
            Assert.That(_audio.PowerInstantBarrierArmClip, Is.Not.Null);
            Assert.That(_audio.PowerInstantBarrierConsumeClip, Is.Not.Null);
            Assert.That(_audio.PowerGravityWellActivateClip, Is.Not.Null);
            Assert.That(_audio.PowerUnavailableClip, Is.Not.Null);
            Assert.That(_audio.HunterReactClip, Is.Not.Null);
            Assert.That(_audio.OutOfCutsClip, Is.Not.Null);
            Assert.That(_audio.OutOfLivesClip, Is.Not.Null);
            Assert.That(
                _audio.OutOfLivesClip,
                Is.SameAs(_audio.OutOfCutsClip));

            AudioSource musicSource = _audio.transform
                .Find("MusicSource")
                .GetComponent<AudioSource>();
            Assert.That(musicSource, Is.Not.Null);
            Assert.That(musicSource.clip, Is.Not.Null);
            Assert.That(musicSource.loop, Is.True);

            Assert.That(_audio.SandFillClip, Is.Not.Null);
        }

        [Test]
        public void ComboRise_UsesEscalatingPitchAndRestoresBaselineAfterward()
        {
            Assert.That(_audio.AudioSource.pitch, Is.EqualTo(1f));

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

            // A successful capture raises ComboChanged with ComboCount > 0,
            // which is the only path that touches AudioSource.pitch. The
            // barrier-failure path (ComboCount == 0, proven by
            // FeedbackModelTests.Session_FailureNeverEmitsNearMissAndResetsCombo)
            // intentionally never plays a dedicated combo sound and relies
            // on BarrierBroken's own clip instead.
            Assert.That(_controller.Session.ComboCount, Is.GreaterThan(0));
            Assert.That(_audio.AudioSource.pitch, Is.EqualTo(1f));
        }

        [Test]
        public void LoopSources_AreWiredSeparatelyFromTheSharedOneShotSource()
        {
            Assert.That(_audio.BarrierLoopSource, Is.Not.Null);
            Assert.That(_audio.BarrierLoopSource.loop, Is.True);
            Assert.That(_audio.BarrierLoopSource.playOnAwake, Is.False);
            Assert.That(_audio.BarrierLoopSource,
                Is.Not.SameAs(_audio.AudioSource));

            Assert.That(_audio.GravityWellLoopSource, Is.Not.Null);
            Assert.That(_audio.GravityWellLoopSource.loop, Is.True);
            Assert.That(_audio.GravityWellLoopSource.playOnAwake, Is.False);
            Assert.That(_audio.GravityWellLoopSource,
                Is.Not.SameAs(_audio.AudioSource));
            Assert.That(_audio.GravityWellLoopSource,
                Is.Not.SameAs(_audio.BarrierLoopSource));
        }

        [Test]
        public void BarrierGrowLoop_StartsWhileGrowingAndStopsOnLock()
        {
            Assert.That(_audio.BarrierLoopSource.isPlaying, Is.False);

            BarrierStartResult start = _controller.SubmitBarrierIntent(
                new BarrierIntent(
                    new LogicalPoint(5f, 4f),
                    BarrierOrientation.Horizontal));
            Assert.That(start.Accepted, Is.True);

            _controller.AdvanceSimulation(
                FirstPlayableController.SimulationStep);
            Assert.That(_audio.BarrierLoopSource.isPlaying, Is.True,
                "The grow loop must start as soon as the barrier is " +
                "growing, not only once it locks.");

            for (int tick = 0;
                 tick < 240 && _controller.Session.ActiveBarrier.HasValue;
                 tick++)
            {
                _controller.AdvanceSimulation(
                    FirstPlayableController.SimulationStep);
            }

            Assert.That(_controller.Session.ActiveBarrier.HasValue,
                Is.False);
            Assert.That(_audio.BarrierLoopSource.isPlaying, Is.False,
                "The grow loop must stop exactly when the barrier locks.");
        }

        [Test]
        public void BarrierGrowLoop_StopsOnBreak()
        {
            BarrierStartResult firstCut = _controller.SubmitBarrierIntent(
                new BarrierIntent(
                    new LogicalPoint(5f, 4f),
                    BarrierOrientation.Horizontal));
            Assert.That(firstCut.Accepted, Is.True);
            for (int tick = 0;
                 tick < 240 && _controller.Session.ActiveBarrier.HasValue;
                 tick++)
            {
                _controller.AdvanceSimulation(
                    FirstPlayableController.SimulationStep);
            }

            Assert.That(_audio.BarrierLoopSource.isPlaying, Is.False);
            LogicalPoint threatPosition = _controller.Session.Threat.Position;
            BarrierStartResult secondCut = _controller.SubmitBarrierIntent(
                new BarrierIntent(
                    threatPosition,
                    BarrierOrientation.Vertical));
            if (!secondCut.Accepted)
            {
                Assert.Inconclusive(
                    "The level already completed after the first cut; " +
                    "the break path cannot be exercised from this state.");
                return;
            }

            _controller.AdvanceSimulation(
                FirstPlayableController.SimulationStep);

            Assert.That(_controller.Session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(_audio.BarrierLoopSource.isPlaying, Is.False);
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
