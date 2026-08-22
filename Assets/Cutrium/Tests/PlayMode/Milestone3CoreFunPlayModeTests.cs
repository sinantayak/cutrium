using System;
using System.Collections;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Landmark;
using Cutrium.Presentation.Threats;
using Cutrium.Unity.Bootstrap;
using Cutrium.Unity.Input;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class Milestone3CoreFunPlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        private GameObject _root;
        private SceneCompositionRoot _composition;
        private FirstPlayableController _controller;
        private BarrierGestureAdapter _gesture;
        private CaptureBoardPresenter _boardPresenter;
        private ThreatPresenter _threatPresenter;
        private CaptureHudPresenter _hud;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
            yield return null;
            BindScene();
        }

        [Test]
        public void Scene_StartsLevelOneWithSerializedTwentyFourLevelFlow()
        {
            Assert.That(_controller.LevelDefinitions.Count, Is.EqualTo(24));
            Assert.That(_controller.LevelCount, Is.EqualTo(24));
            Assert.That(_controller.CurrentLevelIndex, Is.Zero);
            Assert.That(_controller.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(_controller.CurrentLevelId, Is.EqualTo("learn-the-cut"));
            Assert.That(_controller.TargetCapturedFraction, Is.EqualTo(0.75f));
            Assert.That(_controller.ThreatSpeed, Is.EqualTo(1.6f));
            Assert.That(_controller.BarrierGrowthSpeed, Is.EqualTo(3.4f));
            Assert.That(_controller.ThreatCount, Is.EqualTo(1));
            Assert.That(_controller.BoardBounds,
                Is.EqualTo(new LogicalRect(0f, 0f, 10f, 16f)));
            Assert.That(_hud.Controller, Is.SameAs(_controller));
            Assert.That(_hud.LevelText, Is.Not.Null);
            Assert.That(_hud.PurposeText, Is.Not.Null);
            Assert.That(_hud.NextButton, Is.Not.Null);
            Assert.That(_hud.NextButtonLabel, Is.Not.Null);

            _hud.RefreshNow();

            Assert.That(_hud.LevelText.text, Is.EqualTo("LEVEL 1"));
            Assert.That(_hud.PurposeText.text, Is.EqualTo("LEARN THE CUT"));
            Assert.That(_hud.PercentageText.text, Is.EqualTo("Captured 0%"));
            // TargetText now mirrors the current captured fraction (the
            // target moved onto the bar itself as a tick mark), so at level
            // start with nothing captured yet it reads "0%".
            Assert.That(_hud.TargetText.text, Is.EqualTo("0%"));
        }

        [Test]
        public void Retry_RestoresSameLevelConfigurationAndDeterministicState()
        {
            CoreFunLevelConfiguration before =
                _controller.CurrentLevelConfiguration;
            ThreatState[] initialThreats =
                _controller.Session.Threats.ToArray();
            _controller.AdvanceSimulation(0.5f);
            _controller.SubmitBarrierIntent(new BarrierIntent(
                new LogicalPoint(3f, 4f),
                BarrierOrientation.Horizontal));

            _hud.RetryButton.onClick.Invoke();

            Assert.That(_controller.CurrentLevelConfiguration.StableId,
                Is.EqualTo(before.StableId));
            Assert.That(_controller.Session.Threats, Is.EqualTo(initialThreats));
            Assert.That(_controller.Session.CapturedFraction, Is.Zero);
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_controller.Session.Board.CompletedBarriers, Is.Empty);
            Assert.That(_controller.Metrics.Current.RetryCount, Is.EqualTo(1));
            Assert.That(_controller.Metrics.Current.BarrierAttempts, Is.Zero);
            Assert.That(_gesture.IsTracking, Is.False);
            Assert.That(_composition.PointerInput.HasActiveInteraction, Is.False);
        }

        [UnityTest]
        public IEnumerator Completion_WaitsForFinalRevealThenNextLoadsLevelTwoInSameScene()
        {
            Scene sceneBefore = SceneManager.GetActiveScene();
            CompleteCurrentLevel();
            _hud.RefreshNow();

            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed));
            LandmarkRevealPresenter landmark = _hud.CompletionRevealGate;
            if (landmark != null)
            {
                Assert.That(_hud.CompletionCanvasGroup.alpha, Is.Zero);
                float waited = 0f;
                while (!landmark.CompletionPresentationReady && waited < 3f)
                {
                    yield return null;
                    landmark.RefreshNow();
                    _hud.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                Assert.That(landmark.CompletionPresentationReady, Is.True);
            }

            _hud.RefreshNow();
            Assert.That(_hud.CompletionCanvasGroup.alpha, Is.EqualTo(1f));
            Assert.That(_hud.CompletionCanvasGroup.interactable, Is.True);
            Assert.That(_hud.CompletionCanvasGroup.blocksRaycasts, Is.True);
            Assert.That(_hud.NextButtonLabel.text, Is.EqualTo("NEXT"));
            Assert.That(_hud.CompleteOverlay.transform.GetSiblingIndex(),
                Is.EqualTo(_hud.CompleteOverlay.transform.parent.childCount - 1));
            Assert.That(_hud.CompleteOverlay.GetComponent<LayoutElement>()
                .ignoreLayout, Is.True);
            Assert.That(_hud.CompleteOverlay.transform.Find("CompleteText")
                .GetComponent<Text>().text, Does.Contain("LEVEL 1"));

            _hud.NextButton.onClick.Invoke();
            _hud.RefreshNow();

            Assert.That(SceneManager.GetActiveScene().handle,
                Is.EqualTo(sceneBefore.handle));
            Assert.That(_controller.CurrentLevelNumber, Is.EqualTo(2));
            Assert.That(_controller.CurrentLevelId,
                Is.EqualTo("vulnerable-barrier-timing"));
            Assert.That(_controller.TargetCapturedFraction, Is.EqualTo(0.78f));
            Assert.That(_controller.ThreatSpeed, Is.EqualTo(2.35f));
            Assert.That(_controller.BarrierGrowthSpeed, Is.EqualTo(2.15f));
            Assert.That(_controller.ThreatCount, Is.EqualTo(1));
            Assert.That(_controller.Session.CapturedFraction, Is.Zero);
            Assert.That(_hud.LevelText.text, Is.EqualTo("LEVEL 2"));
            Assert.That(_hud.PurposeText.text, Is.EqualTo("WATCH THE THREAT"));
            Assert.That(_hud.TargetText.text, Is.EqualTo("0%"));
            Assert.That(_hud.CompletionCanvasGroup.alpha, Is.Zero);
            Assert.That(_controller.Metrics.SequenceRuns.Count, Is.EqualTo(1));
            Assert.That(_controller.Metrics.SequenceRuns[0].NextPressed, Is.True);
        }

        [Test]
        public void FullSequence_LevelTwentyFourCompletionRestartsDevelopmentSequence()
        {
            for (int level = 1; level < 24; level++)
            {
                CompleteCurrentLevel();
                Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
            }

            CompleteCurrentLevel();
            _hud.RefreshNow();

            Assert.That(_controller.CurrentLevelNumber, Is.EqualTo(24));
            Assert.That(_controller.CurrentLevelId,
                Is.EqualTo("motion-and-gravity-mastery"));
            Assert.That(_controller.TargetCapturedFraction, Is.EqualTo(0.86f));
            Assert.That(_controller.ThreatCount, Is.EqualTo(3));
            Assert.That(_hud.PurposeText.text, Is.EqualTo("MASTER THE MOTION"));
            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed));
            Assert.That(_controller.HasNextLevel, Is.False);
            Assert.That(_hud.NextButtonLabel.text,
                Is.EqualTo("RESTART SEQUENCE"));

            _hud.NextButton.onClick.Invoke();
            _threatPresenter.RefreshNow();

            Assert.That(_controller.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(_controller.ThreatCount, Is.EqualTo(1));
            Assert.That(_threatPresenter.ActiveViewCount, Is.EqualTo(1));
            Assert.That(_threatPresenter.TryGetVisual(
                new ThreatId(2), out _), Is.False);
            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Playing));
            Assert.That(_controller.Metrics.SequenceCompletionCount,
                Is.EqualTo(1));
            Assert.That(_controller.Metrics.LastCompletedSequence.Count,
                Is.EqualTo(24));
            Assert.That(_controller.Metrics.LastCompletedSequence
                .Select(run => run.LevelNumber),
                Is.EqualTo(Enumerable.Range(1, 24)));
        }

        [Test]
        public void LevelThree_ShowsTwoIndependentStableIdThreatViews()
        {
            CompleteCurrentLevel();
            Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
            CompleteCurrentLevel();
            Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
            _threatPresenter.RefreshNow();

            Assert.That(_controller.Session.Threats.Count, Is.EqualTo(2));
            Assert.That(_controller.Session.Threats.Select(
                    threat => threat.Id.Value),
                Is.EqualTo(new[] { 1, 2 }));
            Assert.That(_threatPresenter.ActiveViewCount, Is.EqualTo(2));
            Assert.That(_threatPresenter.TryGetVisual(
                new ThreatId(1), out RectTransform firstView), Is.True);
            Assert.That(_threatPresenter.TryGetVisual(
                new ThreatId(2), out RectTransform secondView), Is.True);
            Assert.That(firstView, Is.Not.SameAs(secondView));
            Vector2 firstBefore = firstView.anchoredPosition;
            Vector2 secondBefore = secondView.anchoredPosition;

            _controller.AdvanceSimulation(0.5f);
            _threatPresenter.RefreshNow();

            Assert.That(firstView.anchoredPosition, Is.Not.EqualTo(firstBefore));
            Assert.That(secondView.anchoredPosition,
                Is.Not.EqualTo(secondBefore));
            Assert.That(firstView.anchoredPosition,
                Is.Not.EqualTo(secondView.anchoredPosition));
        }

        [Test]
        public void LevelThree_RetryRestoresBothInitialThreatsAndViews()
        {
            CompleteCurrentLevel();
            Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
            CompleteCurrentLevel();
            Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
            ThreatState[] initial = _controller.Session.Threats.ToArray();

            _controller.AdvanceSimulation(1f);
            _controller.RetryLevel();
            _threatPresenter.RefreshNow();

            Assert.That(_controller.Session.Threats, Is.EqualTo(initial));
            Assert.That(_controller.Session.CapturedFraction, Is.Zero);
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_threatPresenter.ActiveViewCount, Is.EqualTo(2));
            Assert.That(_threatPresenter.PresentedThreatIds.Select(
                    id => id.Value).OrderBy(value => value),
                Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void LevelThree_MetricsCountSharedBarrierOnceAcrossTwoThreats()
        {
            CompleteCurrentLevel();
            Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
            CompleteCurrentLevel();
            Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
            LogicalPoint firstPosition =
                _controller.Session.Threats[0].Position;

            BarrierStartResult start = _controller.SubmitBarrierIntent(
                new BarrierIntent(
                    firstPosition,
                    BarrierOrientation.Horizontal));
            _controller.AdvanceSimulation(
                FirstPlayableController.SimulationStep);

            Assert.That(start.Accepted, Is.True);
            Assert.That(_controller.Session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(_controller.Metrics.Current.BarrierAttempts,
                Is.EqualTo(1));
            Assert.That(_controller.Metrics.Current.FailedBarriers,
                Is.EqualTo(1));
            Assert.That(_controller.Metrics.Current.SuccessfulBarriers,
                Is.Zero);
        }

        [Test]
        public void RepeatedSequence_DoesNotDuplicateSystemsOrSubscriptions()
        {
            for (int sequence = 0; sequence < 2; sequence++)
            {
                for (int level = 0; level < 24; level++)
                {
                    CompleteCurrentLevel();
                    Assert.That(
                        _controller.AdvanceLevelOrRestartSequence(), Is.True);
                    _threatPresenter.RefreshNow();
                }
            }

            Assert.That(_controller.Metrics.SequenceCompletionCount,
                Is.EqualTo(2));
            Assert.That(
                _root.GetComponentsInChildren<FirstPlayableController>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                _root.GetComponentsInChildren<CaptureHudPresenter>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                _root.GetComponentsInChildren<CaptureBoardPresenter>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                _root.GetComponentsInChildren<ThreatPresenter>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                _root.GetComponentsInChildren<BarrierGestureAdapter>(true),
                Has.Length.EqualTo(1));
            Assert.That(_controller.InitializationCount, Is.EqualTo(1));
            Assert.That(_threatPresenter.ActiveViewCount, Is.EqualTo(1));
            Assert.That(_threatPresenter.Visual.parent.Cast<Transform>()
                    .Count(child => child.name.StartsWith(
                        "ThreatVisual",
                        StringComparison.Ordinal)),
                Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator UiAndOverlayStartsNeverCreateBarriers()
        {
            int attempts = _controller.Metrics.Current.BarrierAttempts;
            ProcessBlockedInteraction(new LogicalPoint(5f, 8f));

            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_controller.Metrics.Current.BarrierAttempts,
                Is.EqualTo(attempts));

            CompleteCurrentLevel();
            _hud.RefreshNow();
            float waited = 0f;
            while (_hud.CompletionRevealGate != null
                && !_hud.CompletionRevealGate.CompletionPresentationReady
                && waited < 3f)
            {
                yield return null;
                _hud.RefreshNow();
                waited += Time.unscaledDeltaTime;
            }

            Assert.That(_hud.CompletionCanvasGroup.blocksRaycasts, Is.True);
            Assert.That(RaycastOverlayCenter(), Does.Contain(
                _hud.CompleteOverlay));
            attempts = _controller.Metrics.Current.BarrierAttempts;
            ProcessBlockedInteraction(new LogicalPoint(5f, 8f));
            Assert.That(_controller.Metrics.Current.BarrierAttempts,
                Is.EqualTo(attempts));
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
        }

        [Test]
        public void GestureEdgeCasesRemainStableAcrossRetryAndNext()
        {
            ProcessAccepted(
                PointerSamplePhase.Started,
                new LogicalPoint(3f, 3f));
            ProcessAccepted(
                PointerSamplePhase.Moved,
                new LogicalPoint(4f, 3.98f));
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.Horizontal));
            ProcessAccepted(
                PointerSamplePhase.Moved,
                new LogicalPoint(3.99f, 4.03f));
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.Horizontal));
            _gesture.ProcessSample(new PointerSample(
                PointerSamplePhase.Cancelled,
                Vector2.zero,
                1,
                false,
                true,
                false,
                default));
            Assert.That(_gesture.IsTracking, Is.False);
            Assert.That(_gesture.CommittedIntentCount, Is.Zero);

            ProcessAccepted(
                PointerSamplePhase.Started,
                new LogicalPoint(5f, 4f));
            ProcessAccepted(
                PointerSamplePhase.Moved,
                new LogicalPoint(6f, 4.05f));
            _gesture.ProcessSample(new PointerSample(
                PointerSamplePhase.Released,
                Vector2.zero,
                1,
                false,
                false,
                false,
                default));
            Assert.That(_gesture.IsTracking, Is.False);
            Assert.That(_gesture.CommittedIntentCount, Is.EqualTo(1));
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.True);

            _controller.RetryLevel();
            CommitWithNormalizedSamples(11);
            BarrierOrientation mouseOrientation = _controller.Session
                .ActiveBarrier.Value.Orientation;
            _controller.RetryLevel();
            CompleteCurrentLevel();
            Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
            CommitWithNormalizedSamples(27);

            Assert.That(_controller.Session.ActiveBarrier.Value.Orientation,
                Is.EqualTo(mouseOrientation));
            Assert.That(_gesture.CommittedIntentCount, Is.EqualTo(1));
        }

        [Test]
        public void NextAndRestartSequenceAcceptFreshAlternatingGestureLocks()
        {
            CompleteCurrentLevel();
            Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
            Assert.That(_controller.CurrentLevelNumber, Is.EqualTo(2));

            LockGestureWhenSafe(new BarrierIntent(
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Horizontal));
            RoomState levelTwoChild = ActiveThreatRoom();
            LockGestureWhenSafe(new BarrierIntent(
                new LogicalPoint(5f, levelTwoChild.Bounds.Center.Y),
                BarrierOrientation.Vertical));

            _controller.RestartSequence();
            Assert.That(_controller.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.None));

            LockGestureWhenSafe(new BarrierIntent(
                new LogicalPoint(4f, 8f),
                BarrierOrientation.Vertical));
            RoomState restartedChild = ActiveThreatRoom();
            LockGestureWhenSafe(new BarrierIntent(
                new LogicalPoint(restartedChild.Bounds.Center.X, 8f),
                BarrierOrientation.Horizontal));
        }

        [Test]
        public void MappingStillRejectsDecorativeMarginAfterEveryTransition()
        {
            for (int level = 1; level <= 24; level++)
            {
                _composition.BoardCameraFitter.RefreshNow();
                Rect board = _composition.BoardCameraFitter.BoardScreenRect;
                Assert.That(_composition.BoardMapper.TryMap(
                    board.center,
                    out LogicalPoint center), Is.True);
                Assert.That(center.X, Is.EqualTo(5f).Within(0.001f));
                Assert.That(center.Y, Is.EqualTo(8f).Within(0.001f));
                Assert.That(_composition.BoardMapper.TryMap(
                    new Vector2(board.xMin - 1f, board.center.y),
                    out _), Is.False);

                if (level < 24)
                {
                    CompleteCurrentLevel();
                    Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
                }
            }
        }

        [Test]
        public void CompletionMetricsAreProducedWithoutChangingGameplay()
        {
            CompleteCurrentLevel();

            CoreFunLevelMetrics metrics = _controller.Metrics.Current;
            Assert.That(metrics.LevelNumber, Is.EqualTo(1));
            Assert.That(metrics.ElapsedSeconds, Is.GreaterThan(0f));
            Assert.That(metrics.BarrierAttempts, Is.GreaterThanOrEqualTo(1));
            Assert.That(metrics.SuccessfulBarriers, Is.GreaterThanOrEqualTo(1));
            Assert.That(metrics.LargestSingleCapturedFraction,
                Is.GreaterThan(0f));
            Assert.That(metrics.FinalCapturedFraction,
                Is.EqualTo(_controller.Session.CapturedFraction));
            Assert.That(_controller.CompletionLogCount, Is.EqualTo(1));
            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed));
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void CompletionOverlayButtonsRemainInsideSafeAreaAtTargetAspect(
            float width,
            float height)
        {
            RectTransform safe = _root.transform.Find("Canvas/SafeAreaRoot")
                .GetComponent<RectTransform>();
            RectTransform overlay = _hud.CompleteOverlay
                .GetComponent<RectTransform>();
            RectTransform retry = _hud.RetryButton
                .GetComponent<RectTransform>();
            RectTransform next = _hud.NextButton.GetComponent<RectTransform>();

            Assert.That(width / height, Is.LessThan(1f));
            Assert.That(overlay.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(overlay.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(retry.anchorMin.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(retry.anchorMax.x, Is.LessThanOrEqualTo(1f));
            Assert.That(next.anchorMin.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(next.anchorMax.x, Is.LessThanOrEqualTo(1f));
            Assert.That(overlay.transform.parent, Is.SameAs(safe));
        }

        private void BindScene()
        {
            _root = SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            _composition = _root.GetComponentInChildren<SceneCompositionRoot>(true);
            _controller = _root
                .GetComponentInChildren<FirstPlayableController>(true);
            _gesture = _root.GetComponentInChildren<BarrierGestureAdapter>(true);
            _boardPresenter = _root
                .GetComponentInChildren<CaptureBoardPresenter>(true);
            _threatPresenter = _root
                .GetComponentInChildren<ThreatPresenter>(true);
            _hud = _root.GetComponentInChildren<CaptureHudPresenter>(true);
            _root.GetComponentInChildren<
                    Cutrium.Presentation.Frontend.FrontEndPresenter>(true)
                ?.SkipForTesting();
            _root.GetComponentInChildren<PreLevelIntroPresenter>(true)
                ?.SkipForTesting();
            Canvas.ForceUpdateCanvases();
            _composition.BoardCameraFitter.RefreshNow();
            _boardPresenter.RefreshNow();
            _threatPresenter.RefreshNow();
            _hud.RefreshNow();
        }

        private void CompleteCurrentLevel()
        {
            for (int attempt = 0;
                 attempt < 240
                 && _controller.Session.LevelStatus == CaptureLevelStatus.Playing;
                 attempt++)
            {
                for (int tick = 0; tick < 30; tick++)
                {
                    _controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }

                ThreatMotionSession session = _controller.Session;
                if (!TryChooseCapturingIntent(
                        session,
                        out BarrierIntent intent))
                {
                    continue;
                }

                BarrierStartResult start = _controller.SubmitBarrierIntent(
                    intent);
                if (!start.Accepted)
                {
                    continue;
                }

                for (int tick = 0;
                     tick < 600 && session.ActiveBarrier.HasValue;
                     tick++)
                {
                    _controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }
            }

            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed),
                $"Level {_controller.CurrentLevelNumber} did not complete: "
                + $"captured={_controller.Session.CapturedFraction:0.000}, "
                + $"rooms={_controller.Session.Board.ActiveRooms.Count}, "
                + "bounds=" + string.Join(",", _controller.Session.Board
                    .ActiveRooms.Select(room => room.Bounds.ToString())) + ", "
                + "threats=" + string.Join(",", _controller.Session.Threats
                    .Select(threat => threat.Position.ToString())) + ", "
                + $"locked={_controller.Session.LockedBarrierCount}, "
                + $"failed={_controller.Session.FailedBarrierCount}, "
                + $"attempts={_controller.Metrics.Current.BarrierAttempts}.");
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
        }

        private bool TryChooseCapturingIntent(
            ThreatMotionSession session,
            out BarrierIntent intent)
        {
            float margin = _controller.BarrierMinimumEdgeMargin;
            float collisionHalfWidth =
                _controller.BarrierCollisionHalfWidth;
            var best = new CutCandidate(default, -1f);
            foreach (RoomState room in session.Board.ActiveRooms)
            {
                ThreatState[] threats = session.Threats
                    .Where(threat => threat.RoomId == room.Id)
                    .ToArray();
                float clearance = threats.Max(threat => threat.Radius)
                    + collisionHalfWidth
                    + 0.1f;
                if (room.Bounds.Height * 0.5f > margin + 0.001f)
                {
                    float below = threats.Min(threat => threat.Position.Y)
                        - clearance;
                    ConsiderCandidate(
                        room,
                        new BarrierIntent(
                            new LogicalPoint(room.Bounds.Center.X, below),
                            BarrierOrientation.Horizontal),
                        (below - room.Bounds.MinY) * room.Bounds.Width,
                        threats,
                        ref best);
                    float above = threats.Max(threat => threat.Position.Y)
                        + clearance;
                    ConsiderCandidate(
                        room,
                        new BarrierIntent(
                            new LogicalPoint(room.Bounds.Center.X, above),
                            BarrierOrientation.Horizontal),
                        (room.Bounds.MaxY - above) * room.Bounds.Width,
                        threats,
                        ref best);
                }

                if (room.Bounds.Width * 0.5f > margin + 0.001f)
                {
                    float left = threats.Min(threat => threat.Position.X)
                        - clearance;
                    ConsiderCandidate(
                        room,
                        new BarrierIntent(
                            new LogicalPoint(left, room.Bounds.Center.Y),
                            BarrierOrientation.Vertical),
                        (left - room.Bounds.MinX) * room.Bounds.Height,
                        threats,
                        ref best);
                    float right = threats.Max(threat => threat.Position.X)
                        + clearance;
                    ConsiderCandidate(
                        room,
                        new BarrierIntent(
                            new LogicalPoint(right, room.Bounds.Center.Y),
                            BarrierOrientation.Vertical),
                        (room.Bounds.MaxX - right) * room.Bounds.Height,
                        threats,
                        ref best);
                }
            }

            intent = best.Intent;
            return best.CapturedArea > 0f;
        }

        private void ConsiderCandidate(
            RoomState room,
            BarrierIntent intent,
            float capturedArea,
            ThreatState[] threats,
            ref CutCandidate best)
        {
            if (capturedArea <= 0f
                || !room.Bounds.Contains(intent.Origin)
                || !LeavesCompletionRoute(room, intent, capturedArea)
                || !IsSafeUntilBarrierLock(room, intent, threats)
                || capturedArea <= best.CapturedArea)
            {
                return;
            }

            best = new CutCandidate(intent, capturedArea);
        }

        private bool LeavesCompletionRoute(
            RoomState room,
            BarrierIntent intent,
            float capturedArea)
        {
            ThreatMotionSession session = _controller.Session;
            float capturedAfter = session.Board.CapturedArea + capturedArea;
            float targetArea = session.InitialRoom.Bounds.Area
                * session.TargetCapturedFraction;
            if (capturedAfter > targetArea
                || _controller.Tolerance.IsAreaApproximatelyEqual(
                    capturedAfter,
                    targetArea))
            {
                return true;
            }

            float retainedWidth = room.Bounds.Width;
            float retainedHeight = room.Bounds.Height;
            if (intent.Orientation == BarrierOrientation.Horizontal)
            {
                retainedHeight -= capturedArea / room.Bounds.Width;
            }
            else
            {
                retainedWidth -= capturedArea / room.Bounds.Height;
            }

            float minimumSpan =
                _controller.BarrierMinimumEdgeMargin * 2f;
            return retainedWidth > minimumSpan
                && retainedHeight > minimumSpan;
        }

        private bool IsSafeUntilBarrierLock(
            RoomState room,
            BarrierIntent intent,
            ThreatState[] threats)
        {
            CoreFunLevelConfiguration level =
                _controller.CurrentLevelConfiguration;
            BarrierStartResult start = BarrierFactory.TryCreate(
                new BarrierId(int.MaxValue),
                room,
                intent,
                level.Barrier,
                _controller.Tolerance);
            if (!start.Accepted)
            {
                return false;
            }

            float lockTime = Math.Max(
                    start.Barrier.NegativeTargetLength,
                    start.Barrier.PositiveTargetLength)
                / start.Barrier.GrowthSpeed;
            for (int index = 0; index < threats.Length; index++)
            {
                ThreatState threat = threats[index];
                int configurationIndex = threat.Id.Value - 1;
                BarrierSimulationResult probe =
                    GrowingBarrierMotionSolver.Move(
                        room,
                        threat,
                        start.Barrier,
                        lockTime + FirstPlayableController.SimulationStep,
                        level.Barrier.MaximumSolverIterations,
                        level.ThreatMotions[configurationIndex]
                            .MaximumImpactsPerTick,
                        _controller.Tolerance);
                if (probe.SimulationEvent != BarrierSimulationEvent.Locked)
                {
                    return false;
                }

                float sideBefore = intent.Orientation
                    == BarrierOrientation.Horizontal
                        ? threat.Position.Y - intent.Origin.Y
                        : threat.Position.X - intent.Origin.X;
                float sideAfter = intent.Orientation
                    == BarrierOrientation.Horizontal
                        ? probe.Threat.Position.Y - intent.Origin.Y
                        : probe.Threat.Position.X - intent.Origin.X;
                if (sideBefore * sideAfter <= 0f)
                {
                    return false;
                }
            }

            return true;
        }

        private void ProcessBlockedInteraction(LogicalPoint point)
        {
            _gesture.ProcessSample(new PointerSample(
                PointerSamplePhase.Started,
                Vector2.zero,
                1,
                true,
                true,
                true,
                point));
            _gesture.ProcessSample(new PointerSample(
                PointerSamplePhase.Moved,
                Vector2.zero,
                1,
                true,
                true,
                true,
                point + new LogicalVector(1f, 0f)));
            _gesture.ProcessSample(new PointerSample(
                PointerSamplePhase.Released,
                Vector2.zero,
                1,
                true,
                true,
                true,
                point + new LogicalVector(1f, 0f)));
        }

        private void CommitWithNormalizedSamples(int pointerId)
        {
            LogicalPoint start = new LogicalPoint(5f, 4f);
            LogicalPoint end = new LogicalPoint(6f, 4.05f);
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Started,
                start,
                pointerId));
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Moved,
                end,
                pointerId));
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Released,
                end,
                pointerId));
        }

        private void LockGestureWhenSafe(BarrierIntent intent)
        {
            for (int idleTick = 0; idleTick < 1800; idleTick++)
            {
                if (_controller.Session.Board.TryGetActiveRoomAt(
                        intent.Origin,
                        out RoomState room))
                {
                    ThreatState[] threats = _controller.Session.Threats
                        .Where(threat => threat.RoomId == room.Id)
                        .ToArray();
                    if (IsSafeUntilBarrierLock(
                            room,
                            intent,
                            threats))
                    {
                        int previousLocks =
                            _controller.Session.LockedBarrierCount;
                        LogicalPoint end = intent.Orientation
                            == BarrierOrientation.Horizontal
                                ? intent.Origin + new LogicalVector(1f, 0f)
                                : intent.Origin + new LogicalVector(0f, 1f);
                        _gesture.ProcessSample(Sample(
                            PointerSamplePhase.Started,
                            intent.Origin,
                            303));
                        _gesture.ProcessSample(Sample(
                            PointerSamplePhase.Moved,
                            end,
                            303));
                        Assert.That(_gesture.SelectedOrientation,
                            Is.EqualTo(intent.Orientation));
                        _gesture.ProcessSample(Sample(
                            PointerSamplePhase.Released,
                            end,
                            303));

                        Assert.That(_controller.LastBarrierStartResult.Accepted,
                            Is.True,
                            _controller.LastBarrierStartResult.RejectionReason
                                .ToString());
                        Assert.That(_controller.Session.ActiveBarrier.HasValue,
                            Is.True);
                        BarrierState started =
                            _controller.Session.ActiveBarrier.Value;
                        Assert.That(started.Orientation,
                            Is.EqualTo(intent.Orientation));
                        Assert.That(started.Origin, Is.EqualTo(intent.Origin));
                        Assert.That(started.ParentRoomId, Is.EqualTo(room.Id));
                        Assert.That(_gesture.SelectedOrientation,
                            Is.EqualTo(BarrierOrientation.None));

                        _controller.AdvanceSimulation(
                            FirstPlayableController.SimulationStep);
                        Assert.That(_controller.Session.ActiveBarrier.HasValue,
                            Is.True);
                        Assert.That(_controller.Session.ActiveBarrier.Value
                                .NegativeLength,
                            Is.GreaterThan(0f));
                        Assert.That(_controller.Session.ActiveBarrier.Value
                                .PositiveLength,
                            Is.GreaterThan(0f));
                        for (int growthTick = 0;
                             growthTick < 600
                             && _controller.Session.ActiveBarrier.HasValue;
                             growthTick++)
                        {
                            _controller.AdvanceSimulation(
                                FirstPlayableController.SimulationStep);
                        }

                        Assert.That(_controller.Session.ActiveBarrier.HasValue,
                            Is.False);
                        Assert.That(_controller.Session.LastBarrierEvent,
                            Is.EqualTo(BarrierSimulationEvent.Locked));
                        Assert.That(_controller.Session.LockedBarrierCount,
                            Is.EqualTo(previousLocks + 1));
                        return;
                    }
                }

                _controller.AdvanceSimulation(
                    FirstPlayableController.SimulationStep);
            }

            Assert.Fail(
                $"No safe {intent.Orientation} lock window was found at "
                + intent.Origin + ".");
        }

        private RoomState ActiveThreatRoom()
        {
            ThreatState threat = _controller.Session.Threats[0];
            Assert.That(_controller.Session.Board.TryGetActiveRoom(
                threat.RoomId,
                out RoomState room), Is.True);
            return room;
        }

        private void ProcessAccepted(
            PointerSamplePhase phase,
            LogicalPoint point) =>
            _gesture.ProcessSample(Sample(phase, point, 1));

        private static PointerSample Sample(
            PointerSamplePhase phase,
            LogicalPoint point,
            int pointerId) =>
            new PointerSample(
                phase,
                Vector2.zero,
                pointerId,
                false,
                true,
                true,
                point);

        private GameObject[] RaycastOverlayCenter()
        {
            RectTransform overlay = _hud.CompleteOverlay
                .GetComponent<RectTransform>();
            Vector2 center = RectTransformUtility.WorldToScreenPoint(
                null,
                overlay.TransformPoint(overlay.rect.center));
            var data = new PointerEventData(_composition.EventSystem)
            {
                position = center,
            };
            var results = new System.Collections.Generic.List<RaycastResult>();
            _composition.EventSystem.RaycastAll(data, results);
            return results.Select(result => result.gameObject).ToArray();
        }

        private readonly struct CutCandidate
        {
            public CutCandidate(BarrierIntent intent, float capturedArea)
            {
                Intent = intent;
                CapturedArea = capturedArea;
            }

            public BarrierIntent Intent { get; }

            public float CapturedArea { get; }
        }
    }
}
