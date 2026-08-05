using System.Collections;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.HUD;
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
        public void Scene_StartsLevelOneWithSerializedThreeLevelFlow()
        {
            Assert.That(_controller.LevelDefinitions.Count, Is.EqualTo(3));
            Assert.That(_controller.LevelCount, Is.EqualTo(3));
            Assert.That(_controller.CurrentLevelIndex, Is.Zero);
            Assert.That(_controller.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(_controller.CurrentLevelId, Is.EqualTo("learn-the-cut"));
            Assert.That(_controller.TargetCapturedFraction, Is.EqualTo(0.625f));
            Assert.That(_controller.ThreatSpeed, Is.EqualTo(2.6f));
            Assert.That(_controller.BarrierGrowthSpeed, Is.EqualTo(9.5f));
            Assert.That(_controller.BoardBounds,
                Is.EqualTo(new LogicalRect(0f, 0f, 10f, 16f)));
            Assert.That(_hud.Controller, Is.SameAs(_controller));
            Assert.That(_hud.LevelText, Is.Not.Null);
            Assert.That(_hud.NextButton, Is.Not.Null);
            Assert.That(_hud.NextButtonLabel, Is.Not.Null);

            _hud.RefreshNow();

            Assert.That(_hud.LevelText.text, Is.EqualTo("LEVEL 1"));
            Assert.That(_hud.PercentageText.text, Is.EqualTo("Captured 0%"));
            Assert.That(_hud.TargetText.text, Is.EqualTo("Target 63%"));
        }

        [Test]
        public void Retry_RestoresSameLevelConfigurationAndDeterministicState()
        {
            CoreFunLevelConfiguration before =
                _controller.CurrentLevelConfiguration;
            var initialThreat = _controller.Session.Threat;
            _controller.AdvanceSimulation(0.5f);
            _controller.SubmitBarrierIntent(new BarrierIntent(
                new LogicalPoint(3f, 4f),
                BarrierOrientation.Horizontal));

            _hud.RetryButton.onClick.Invoke();

            Assert.That(_controller.CurrentLevelConfiguration.StableId,
                Is.EqualTo(before.StableId));
            Assert.That(_controller.Session.Threat, Is.EqualTo(initialThreat));
            Assert.That(_controller.Session.CapturedFraction, Is.Zero);
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_controller.Session.Board.CompletedBarriers, Is.Empty);
            Assert.That(_controller.Metrics.Current.RetryCount, Is.EqualTo(1));
            Assert.That(_controller.Metrics.Current.BarrierAttempts, Is.Zero);
            Assert.That(_gesture.IsTracking, Is.False);
            Assert.That(_composition.PointerInput.HasActiveInteraction, Is.False);
        }

        [Test]
        public void Completion_ShowsLevelAndNextLoadsLevelTwoInSameScene()
        {
            Scene sceneBefore = SceneManager.GetActiveScene();
            CompleteCurrentLevel();
            _hud.RefreshNow();

            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed));
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
                Is.EqualTo("timing-and-failure"));
            Assert.That(_controller.TargetCapturedFraction, Is.EqualTo(0.7f));
            Assert.That(_controller.ThreatSpeed, Is.EqualTo(3.2f));
            Assert.That(_controller.BarrierGrowthSpeed, Is.EqualTo(8f));
            Assert.That(_controller.Session.CapturedFraction, Is.Zero);
            Assert.That(_hud.LevelText.text, Is.EqualTo("LEVEL 2"));
            Assert.That(_hud.TargetText.text, Is.EqualTo("Target 70%"));
            Assert.That(_hud.CompletionCanvasGroup.alpha, Is.Zero);
            Assert.That(_controller.Metrics.SequenceRuns.Count, Is.EqualTo(1));
            Assert.That(_controller.Metrics.SequenceRuns[0].NextPressed, Is.True);
        }

        [Test]
        public void FullSequence_LevelThreeCompletionRestartsDevelopmentSequence()
        {
            CompleteCurrentLevel();
            Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
            CompleteCurrentLevel();
            Assert.That(_controller.TryAdvanceToNextLevel(), Is.True);
            CompleteCurrentLevel();
            _hud.RefreshNow();

            Assert.That(_controller.CurrentLevelNumber, Is.EqualTo(3));
            Assert.That(_controller.CurrentLevelId,
                Is.EqualTo("confident-capture"));
            Assert.That(_controller.TargetCapturedFraction, Is.EqualTo(0.75f));
            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed));
            Assert.That(_controller.HasNextLevel, Is.False);
            Assert.That(_hud.NextButtonLabel.text,
                Is.EqualTo("RESTART SEQUENCE"));

            _hud.NextButton.onClick.Invoke();

            Assert.That(_controller.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Playing));
            Assert.That(_controller.Metrics.SequenceCompletionCount,
                Is.EqualTo(1));
            Assert.That(_controller.Metrics.LastCompletedSequence.Count,
                Is.EqualTo(3));
            Assert.That(_controller.Metrics.LastCompletedSequence
                .Select(run => run.LevelNumber), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void RepeatedSequence_DoesNotDuplicateSystemsOrSubscriptions()
        {
            for (int sequence = 0; sequence < 2; sequence++)
            {
                for (int level = 0; level < 3; level++)
                {
                    CompleteCurrentLevel();
                    Assert.That(
                        _controller.AdvanceLevelOrRestartSequence(), Is.True);
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
        }

        [Test]
        public void UiAndOverlayStartsNeverCreateBarriers()
        {
            int attempts = _controller.Metrics.Current.BarrierAttempts;
            ProcessBlockedInteraction(new LogicalPoint(5f, 8f));

            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_controller.Metrics.Current.BarrierAttempts,
                Is.EqualTo(attempts));

            CompleteCurrentLevel();
            _hud.RefreshNow();
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
                new LogicalPoint(2f, 3f));
            ProcessAccepted(
                PointerSamplePhase.Moved,
                new LogicalPoint(3f, 3.05f));
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
        public void MappingStillRejectsDecorativeMarginAfterEveryTransition()
        {
            for (int level = 1; level <= 3; level++)
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

                if (level < 3)
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
            _hud = _root.GetComponentInChildren<CaptureHudPresenter>(true);
            Canvas.ForceUpdateCanvases();
            _composition.BoardCameraFitter.RefreshNow();
            _boardPresenter.RefreshNow();
            _hud.RefreshNow();
        }

        private void CompleteCurrentLevel()
        {
            for (int attempt = 0;
                 attempt < 80
                 && _controller.Session.LevelStatus == CaptureLevelStatus.Playing;
                 attempt++)
            {
                for (int tick = 0; tick < 12; tick++)
                {
                    _controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }

                ThreatMotionSession session = _controller.Session;
                RoomState room = session.Board.ActiveRooms.Single();
                float split = session.Threat.Position.Y >= room.Bounds.Center.Y
                    ? room.Bounds.MinY + room.Bounds.Height * 0.3f
                    : room.Bounds.MaxY - room.Bounds.Height * 0.3f;
                var origin = new LogicalPoint(room.Bounds.Center.X, split);
                BarrierStartResult start = _controller.SubmitBarrierIntent(
                    new BarrierIntent(origin, BarrierOrientation.Horizontal));
                if (!start.Accepted)
                {
                    continue;
                }

                for (int tick = 0;
                     tick < 180 && session.ActiveBarrier.HasValue;
                     tick++)
                {
                    _controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }
            }

            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed),
                $"Level {_controller.CurrentLevelNumber} did not complete.");
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
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
            LogicalPoint start = new LogicalPoint(2f, 3f);
            LogicalPoint end = new LogicalPoint(3f, 3.05f);
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
    }
}
