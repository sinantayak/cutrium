using System.Collections;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Presentation.Barriers;
using Cutrium.Unity.Bootstrap;
using Cutrium.Unity.Input;
using Cutrium.Unity.Layout;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cutrium.PlayModeTests
{
    public sealed class Milestone2BPlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        private SceneCompositionRoot _composition;
        private FirstPlayableController _controller;
        private BarrierGestureAdapter _gesture;
        private BarrierPresenter _presenter;
        private Mouse _mouse;
        private Touchscreen _touchscreen;
        private InputSettings.BackgroundBehavior _originalBackgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode _originalEditorBehavior;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _originalBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            _originalEditorBehavior =
                InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior =
                InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode
                    .AllDeviceInputAlwaysGoesToGameView;
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;
            BindScene();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            RemoveTestDevices();
            InputSystem.settings.backgroundBehavior = _originalBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                _originalEditorBehavior;
            yield return null;
        }

        [UnityTest]
        public IEnumerator MouseAndPrimaryTouch_CommitEquivalentHorizontalIntents()
        {
            _mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(_mouse);
            Vector2 start = LogicalToScreen(new LogicalPoint(5f, 4f));
            Vector2 end = LogicalToScreen(new LogicalPoint(6f, 4.05f));

            QueueMouse(start, true);
            yield return null;
            QueueMouse(end, true);
            yield return null;
            QueueMouse(end, false);
            yield return null;

            BarrierState mouseBarrier = _controller.Session.ActiveBarrier.Value;
            Assert.That(mouseBarrier.Orientation,
                Is.EqualTo(BarrierOrientation.Horizontal));
            RemoveTestDevices();

            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;
            BindScene();
            _touchscreen = InputSystem.AddDevice<Touchscreen>();
            InputSystem.EnableDevice(_touchscreen);
            start = LogicalToScreen(new LogicalPoint(5f, 4f));
            end = LogicalToScreen(new LogicalPoint(6f, 4.05f));

            QueueTouch(start, 27, UnityEngine.InputSystem.TouchPhase.Began);
            yield return null;
            QueueTouch(end, 27, UnityEngine.InputSystem.TouchPhase.Moved);
            yield return null;
            QueueTouch(end, 27, UnityEngine.InputSystem.TouchPhase.Ended);
            yield return null;

            BarrierState touchBarrier = _controller.Session.ActiveBarrier.Value;
            Assert.That(touchBarrier.Orientation,
                Is.EqualTo(mouseBarrier.Orientation));
            Assert.That(touchBarrier.Origin.X,
                Is.EqualTo(mouseBarrier.Origin.X).Within(0.01f));
            Assert.That(touchBarrier.Origin.Y,
                Is.EqualTo(mouseBarrier.Origin.Y).Within(0.01f));
        }

        [Test]
        public void Gesture_UsesThresholdDominantAxisAndHysteresis()
        {
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Started,
                new LogicalPoint(2f, 3f)));
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Moved,
                new LogicalPoint(3f, 3.95f)));
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.Horizontal));

            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Moved,
                new LogicalPoint(2.99f, 4.02f)));
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.Horizontal));

            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Moved,
                new LogicalPoint(2.9f, 4.2f)));
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.Vertical));
        }

        [Test]
        public void ShortRelease_CancelsWithoutTapFallback()
        {
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Started,
                new LogicalPoint(2f, 3f)));
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Released,
                new LogicalPoint(2.1f, 3.1f)));

            Assert.That(_gesture.CancelledInteractionCount, Is.EqualTo(1));
            Assert.That(_gesture.CommittedIntentCount, Is.Zero);
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
        }

        [UnityTest]
        public IEnumerator HudStart_RemainsBlockedThroughBoardRelease()
        {
            _mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(_mouse);
            RectTransform topHud = _composition.transform.parent
                .Find("Canvas/SafeAreaRoot/TopHUD")
                .GetComponent<RectTransform>();
            Vector2 hudCenter = GetScreenCenter(topHud);
            Vector2 boardEnd = LogicalToScreen(new LogicalPoint(4f, 4f));

            QueueMouse(hudCenter, true);
            yield return null;
            QueueMouse(boardEnd, true);
            yield return null;
            QueueMouse(boardEnd, false);
            yield return null;

            Assert.That(_gesture.CommittedIntentCount, Is.Zero);
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
        }

        [Test]
        public void PreviewAndCommittedHalves_AreReplaceableSerializedViews()
        {
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Started,
                new LogicalPoint(5f, 4f)));
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Moved,
                new LogicalPoint(6f, 4.05f)));
            _presenter.RefreshNow();

            Assert.That(_presenter.Preview.gameObject.activeSelf, Is.True);
            Assert.That(_presenter.NegativeHalf.gameObject.activeSelf, Is.False);

            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Released,
                new LogicalPoint(6f, 4.05f)));
            _presenter.RefreshNow();

            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.True);
            Assert.That(_presenter.Preview.gameObject.activeSelf, Is.False);
            Assert.That(_presenter.NegativeHalf.gameObject.activeSelf, Is.True);
            Assert.That(_presenter.PositiveHalf.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void GrowthBoundaryGesture_IsRejectedWithoutExceptionOrPreview()
        {
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Started,
                new LogicalPoint(10f, 8f)));
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Moved,
                new LogicalPoint(9f, 8f)));

            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.Horizontal));
            Assert.DoesNotThrow(_presenter.RefreshNow);
            Assert.That(_presenter.Preview.gameObject.activeSelf, Is.False);

            Assert.DoesNotThrow(() => _gesture.ProcessSample(Sample(
                PointerSamplePhase.Released,
                new LogicalPoint(9f, 8f))));

            Assert.That(_controller.LastBarrierStartResult.Accepted, Is.False);
            Assert.That(_controller.LastBarrierStartResult.RejectionReason,
                Is.EqualTo(BarrierRejectionReason.NoGrowthSpan));
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.None));
        }

        [Test]
        public void LevelOne_InteriorNearRoomEdgesStartsBothOrientations()
        {
            CommitDirect(
                new LogicalPoint(5f, 0.1f),
                new LogicalPoint(6f, 0.1f));

            AssertCommittedOrientation(BarrierOrientation.Horizontal);
            Assert.That(_controller.Session.ActiveBarrier.Value.Origin.Y,
                Is.EqualTo(0.1f));
            AssertActiveBarrierGrowsAndIsVisible(
                BarrierOrientation.Horizontal);

            _controller.RetryLevel();
            CommitDirect(
                new LogicalPoint(0.1f, 8f),
                new LogicalPoint(0.1f, 9f));

            AssertCommittedOrientation(BarrierOrientation.Vertical);
            Assert.That(_controller.Session.ActiveBarrier.Value.Origin.X,
                Is.EqualTo(0.1f));
            AssertActiveBarrierGrowsAndIsVisible(
                BarrierOrientation.Vertical);
        }

        [UnityTest]
        public IEnumerator Mouse_HorizontalThenVerticalUsesCurrentInteraction()
        {
            _mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(_mouse);
            for (int tick = 0;
                 tick < 300
                 && (_controller.Session.Threat.Position.Y <= 10.45f
                     || _controller.Session.Threat.Velocity.Y <= 0f);
                 tick++)
            {
                _controller.AdvanceSimulation(
                    FirstPlayableController.SimulationStep);
            }

            Assert.That(_controller.Session.Threat.Position.Y,
                Is.GreaterThan(10.45f));
            Vector2 horizontalStart = LogicalToScreen(
                new LogicalPoint(5f, 10f));
            Vector2 horizontalEnd = LogicalToScreen(
                new LogicalPoint(6f, 10.05f));
            QueueMouse(horizontalStart, true);
            yield return null;
            QueueMouse(horizontalEnd, true);
            yield return null;
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.Horizontal));
            _presenter.RefreshNow();
            Assert.That(_presenter.Preview.gameObject.activeSelf, Is.True);
            QueueMouse(horizontalEnd, false);
            yield return null;

            Assert.That(_composition.PointerInput.LastSample.PointerId,
                Is.EqualTo(_mouse.deviceId));
            AssertCommittedOrientation(BarrierOrientation.Horizontal);
            LockActiveBarrier(BarrierOrientation.Horizontal);
            Assert.That(_controller.Session.Board.TryGetActiveRoomAt(
                new LogicalPoint(5f, 13f),
                out RoomState child), Is.True);
            Assert.That(child.Bounds,
                Is.EqualTo(new LogicalRect(0f, 10f, 10f, 6f)));

            Vector2 verticalStart = LogicalToScreen(
                new LogicalPoint(5f, 13f));
            Vector2 verticalEnd = LogicalToScreen(
                new LogicalPoint(5.05f, 14f));
            QueueMouse(verticalStart, true);
            yield return null;
            QueueMouse(verticalEnd, true);
            yield return null;
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.Vertical));
            _presenter.RefreshNow();
            Assert.That(_presenter.Preview.gameObject.activeSelf, Is.True);
            Assert.That(_presenter.Preview.sizeDelta.x,
                Is.EqualTo(_presenter.BoardFrame.rect.height * 6f / 16f)
                    .Within(0.1f));
            QueueMouse(verticalEnd, false);
            yield return null;

            AssertCommittedOrientation(BarrierOrientation.Vertical);
            Assert.That(_controller.Session.ActiveBarrier.Value.ParentRoomId,
                Is.EqualTo(child.Id));
            Assert.That(_controller.Session.ActiveBarrier.Value
                    .NegativeTargetLength,
                Is.EqualTo(3f).Within(_controller.Tolerance.DistanceTolerance));
            Assert.That(_controller.Session.ActiveBarrier.Value
                    .PositiveTargetLength,
                Is.EqualTo(3f).Within(_controller.Tolerance.DistanceTolerance));
            AssertActiveBarrierGrowsAndIsVisible(
                BarrierOrientation.Vertical);
        }

        [UnityTest]
        public IEnumerator PrimaryTouch_VerticalThenHorizontalUsesCurrentInteraction()
        {
            _touchscreen = InputSystem.AddDevice<Touchscreen>();
            InputSystem.EnableDevice(_touchscreen);
            Vector2 verticalStart = LogicalToScreen(
                new LogicalPoint(4f, 8f));
            Vector2 verticalEnd = LogicalToScreen(
                new LogicalPoint(4.05f, 9f));
            QueueTouch(verticalStart, 27, UnityEngine.InputSystem.TouchPhase.Began);
            yield return null;
            QueueTouch(verticalEnd, 27, UnityEngine.InputSystem.TouchPhase.Moved);
            yield return null;
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.Vertical));
            QueueTouch(verticalEnd, 27, UnityEngine.InputSystem.TouchPhase.Ended);
            yield return null;

            AssertCommittedOrientation(BarrierOrientation.Vertical);
            LockActiveBarrier(BarrierOrientation.Vertical);
            Assert.That(_controller.Session.Board.TryGetActiveRoomAt(
                new LogicalPoint(7f, 8f),
                out RoomState child), Is.True);
            // This bound is derived from a simulated screen-space touch
            // round-tripped through the live board's on-screen pixel rect,
            // so (unlike the codebase's other pure-logic LogicalRect
            // assertions) it is sensitive to sub-grid floating-point noise
            // whenever HUD layout changes shift that pixel rect by even a
            // fraction of a pixel; a tight tolerance still catches any real
            // grid-snap regression.
            Assert.That(child.Bounds.MinX, Is.EqualTo(4f).Within(0.01f));
            Assert.That(child.Bounds.MinY, Is.EqualTo(0f).Within(0.01f));
            Assert.That(child.Bounds.Width, Is.EqualTo(6f).Within(0.01f));
            Assert.That(child.Bounds.Height, Is.EqualTo(16f).Within(0.01f));

            Vector2 horizontalStart = LogicalToScreen(
                new LogicalPoint(7f, 8f));
            Vector2 horizontalEnd = LogicalToScreen(
                new LogicalPoint(8f, 8.05f));
            QueueTouch(horizontalStart, 28, UnityEngine.InputSystem.TouchPhase.Began);
            yield return null;
            QueueTouch(horizontalEnd, 28, UnityEngine.InputSystem.TouchPhase.Moved);
            yield return null;
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.Horizontal));
            QueueTouch(horizontalEnd, 28, UnityEngine.InputSystem.TouchPhase.Ended);
            yield return null;

            AssertCommittedOrientation(BarrierOrientation.Horizontal);
            Assert.That(_controller.Session.ActiveBarrier.Value.ParentRoomId,
                Is.EqualTo(child.Id));
            Assert.That(_controller.Session.ActiveBarrier.Value
                    .NegativeTargetLength,
                Is.EqualTo(3f).Within(_controller.Tolerance.DistanceTolerance));
            Assert.That(_controller.Session.ActiveBarrier.Value
                    .PositiveTargetLength,
                Is.EqualTo(3f).Within(_controller.Tolerance.DistanceTolerance));
            AssertActiveBarrierGrowsAndIsVisible(
                BarrierOrientation.Horizontal);
        }

        [Test]
        public void FailedHorizontalThenVerticalClearsTransientAndActiveState()
        {
            LogicalPoint threatPosition = _controller.Session.Threat.Position;
            CommitDirect(
                threatPosition,
                new LogicalPoint(threatPosition.X + 1f, threatPosition.Y));
            AssertCommittedOrientation(BarrierOrientation.Horizontal);

            _controller.AdvanceSimulation(
                FirstPlayableController.SimulationStep);

            Assert.That(_controller.Session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Failed));
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.None));
            Assert.That(_gesture.Origin, Is.EqualTo(default(LogicalPoint)));

            CommitDirect(
                new LogicalPoint(4f, 8f),
                new LogicalPoint(4.05f, 9f));

            AssertCommittedOrientation(BarrierOrientation.Vertical);
            AssertActiveBarrierGrowsAndIsVisible(
                BarrierOrientation.Vertical);
        }

        [Test]
        public void CancelRetryAndRestartAllowFreshVerticalHorizontalPairs()
        {
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Started,
                new LogicalPoint(5f, 8f)));
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Moved,
                new LogicalPoint(6f, 8f)));
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.Horizontal));
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Cancelled,
                new LogicalPoint(6f, 8f)));
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.None));
            CommitDirect(
                new LogicalPoint(4f, 8f),
                new LogicalPoint(4.05f, 9f));
            AssertCommittedOrientation(BarrierOrientation.Vertical);
            LockActiveBarrier(BarrierOrientation.Vertical);
            CommitDirect(
                new LogicalPoint(7f, 8f),
                new LogicalPoint(8f, 8.05f));
            AssertCommittedOrientation(BarrierOrientation.Horizontal);

            _controller.RetryLevel();
            CommitDirect(
                new LogicalPoint(4f, 8f),
                new LogicalPoint(4.05f, 9f));
            AssertCommittedOrientation(BarrierOrientation.Vertical);
            LockActiveBarrier(BarrierOrientation.Vertical);
            CommitDirect(
                new LogicalPoint(7f, 8f),
                new LogicalPoint(8f, 8.05f));
            AssertCommittedOrientation(BarrierOrientation.Horizontal);

            _controller.RestartSequence();
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.None));
            CommitDirect(
                new LogicalPoint(4f, 8f),
                new LogicalPoint(4.05f, 9f));
            AssertCommittedOrientation(BarrierOrientation.Vertical);
            LockActiveBarrier(BarrierOrientation.Vertical);
            CommitDirect(
                new LogicalPoint(7f, 8f),
                new LogicalPoint(8f, 8.05f));
            AssertCommittedOrientation(BarrierOrientation.Horizontal);
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void SupportedAspect_MappedVerticalHorizontalCreatesRealBarriers(
            float width,
            float height)
        {
            Rect boardRect = BoardViewportLayout.CalculateAspectFitRect(
                new Rect(0f, 0f, width, height));
            CommitMappedGesture(
                boardRect,
                new LogicalPoint(4f, 8f),
                new LogicalPoint(4.05f, 9f),
                101);
            AssertCommittedOrientation(BarrierOrientation.Vertical);
            LockActiveBarrier(BarrierOrientation.Vertical);
            CommitMappedGesture(
                boardRect,
                new LogicalPoint(7f, 8f),
                new LogicalPoint(8f, 8.05f),
                102);
            AssertCommittedOrientation(BarrierOrientation.Horizontal);
            AssertActiveBarrierGrowsAndIsVisible(
                BarrierOrientation.Horizontal);
        }

        [Test]
        public void FailureFeedback_AppearsAndCanBeCleanedWithoutRestart()
        {
            BarrierStartResult start = _controller.SubmitBarrierIntent(
                new BarrierIntent(
                    _controller.Session.Threat.Position,
                    BarrierOrientation.Horizontal));
            Assert.That(start.Accepted, Is.True);

            _controller.AdvanceSimulation(FirstPlayableController.SimulationStep);
            _presenter.RefreshNow();

            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_controller.Session.FailedBarrierCount, Is.EqualTo(1));
            Assert.That(_presenter.FailureFeedbackVisible, Is.True);

            _presenter.ClearFailureFeedbackNow();
            Assert.That(_presenter.FailureFeedbackVisible, Is.False);
            Assert.That(_controller.Session, Is.Not.Null);
        }

        [Test]
        public void VisualThickness_DoesNotChangeLogicalCollisionWidthOrState()
        {
            _controller.SubmitBarrierIntent(new BarrierIntent(
                new LogicalPoint(5f, 4f),
                BarrierOrientation.Horizontal));
            BarrierState before = _controller.Session.ActiveBarrier.Value;
            float collisionWidth = _controller.BarrierCollisionHalfWidth;

            _presenter.SetVisualLogicalThickness(
                _presenter.VisualLogicalThickness * 2f);
            _presenter.RefreshNow();

            Assert.That(_controller.BarrierCollisionHalfWidth,
                Is.EqualTo(collisionWidth));
            Assert.That(_controller.Session.ActiveBarrier.Value,
                Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator ReloadingScene_DoesNotDuplicateInputOrSimulation()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;
            BindScene();
            GameObject root = _composition.transform.parent.gameObject;

            Assert.That(
                root.GetComponentsInChildren<FirstPlayableController>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                root.GetComponentsInChildren<BarrierGestureAdapter>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                root.GetComponentsInChildren<BarrierPresenter>(true),
                Has.Length.EqualTo(1));

            CommitDirect(new LogicalPoint(5f, 4f), new LogicalPoint(6f, 4f));
            Assert.That(_gesture.CommittedIntentCount, Is.EqualTo(1));
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.True);
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void SupportedAspect_KeepsFullBarrierEndpointsInsideBoard(
            float width,
            float height)
        {
            Rect viewport = new Rect(0f, 0f, width, height);
            Rect board = BoardViewportLayout.CalculateAspectFitRect(viewport);
            Vector2 left = LogicalToRect(
                board,
                new LogicalPoint(0f, 3f));
            Vector2 right = LogicalToRect(
                board,
                new LogicalPoint(10f, 3f));

            Assert.That(left.x, Is.EqualTo(board.xMin).Within(0.01f));
            Assert.That(right.x, Is.EqualTo(board.xMax).Within(0.01f));
            Assert.That(left.y, Is.InRange(board.yMin, board.yMax));
            Assert.That(right.y, Is.InRange(board.yMin, board.yMax));
        }

        private void BindScene()
        {
            GameObject root = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            _composition = root.transform.Find("SceneCompositionRoot")
                .GetComponent<SceneCompositionRoot>();
            _controller = root.GetComponentInChildren<FirstPlayableController>(true);
            _gesture = root.GetComponentInChildren<BarrierGestureAdapter>(true);
            _presenter = root.GetComponentInChildren<BarrierPresenter>(true);
            Canvas.ForceUpdateCanvases();
            _composition.BoardCameraFitter.RefreshNow();
            _presenter.RefreshNow();
        }

        private void CommitDirect(LogicalPoint start, LogicalPoint end)
        {
            _gesture.ProcessSample(Sample(PointerSamplePhase.Started, start));
            _gesture.ProcessSample(Sample(PointerSamplePhase.Moved, end));
            _gesture.ProcessSample(Sample(PointerSamplePhase.Released, end));
        }

        private void CommitMappedGesture(
            Rect boardRect,
            LogicalPoint start,
            LogicalPoint end,
            int pointerId)
        {
            Vector2 startScreen = LogicalToRect(boardRect, start);
            Vector2 endScreen = LogicalToRect(boardRect, end);
            Assert.That(BoardScreenMapper.TryMap(
                boardRect,
                startScreen,
                out LogicalPoint mappedStart), Is.True);
            Assert.That(BoardScreenMapper.TryMap(
                boardRect,
                endScreen,
                out LogicalPoint mappedEnd), Is.True);
            _gesture.ProcessSample(new PointerSample(
                PointerSamplePhase.Started,
                startScreen,
                pointerId,
                false,
                true,
                true,
                mappedStart));
            _gesture.ProcessSample(new PointerSample(
                PointerSamplePhase.Moved,
                endScreen,
                pointerId,
                false,
                true,
                true,
                mappedEnd));
            _gesture.ProcessSample(new PointerSample(
                PointerSamplePhase.Released,
                endScreen,
                pointerId,
                false,
                true,
                true,
                mappedEnd));
        }

        private void AssertCommittedOrientation(
            BarrierOrientation orientation)
        {
            Assert.That(_controller.LastBarrierStartResult.Accepted, Is.True,
                _controller.LastBarrierStartResult.RejectionReason.ToString());
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.True);
            Assert.That(_controller.Session.ActiveBarrier.Value.Orientation,
                Is.EqualTo(orientation));
            Assert.That(_gesture.IsTracking, Is.False);
            Assert.That(_gesture.SelectedOrientation,
                Is.EqualTo(BarrierOrientation.None));
            Assert.That(_gesture.Origin, Is.EqualTo(default(LogicalPoint)));
        }

        private void LockActiveBarrier(BarrierOrientation orientation)
        {
            int previousLocks = _controller.Session.LockedBarrierCount;
            AssertActiveBarrierGrowsAndIsVisible(orientation);
            for (int tick = 0;
                 tick < 600 && _controller.Session.ActiveBarrier.HasValue;
                 tick++)
            {
                _controller.AdvanceSimulation(
                    FirstPlayableController.SimulationStep);
            }

            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_controller.Session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Locked));
            Assert.That(_controller.Session.LockedBarrierCount,
                Is.EqualTo(previousLocks + 1));
            BarrierState locked = _controller.Session.Board.CompletedBarriers[
                _controller.Session.Board.CompletedBarriers.Count - 1];
            Assert.That(locked.Orientation, Is.EqualTo(orientation));
        }

        private void AssertActiveBarrierGrowsAndIsVisible(
            BarrierOrientation orientation)
        {
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.True);
            _controller.AdvanceSimulation(
                FirstPlayableController.SimulationStep);
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.True);
            BarrierState growing = _controller.Session.ActiveBarrier.Value;
            Assert.That(growing.Orientation, Is.EqualTo(orientation));
            Assert.That(growing.NegativeLength, Is.GreaterThan(0f));
            Assert.That(growing.PositiveLength, Is.GreaterThan(0f));
            _presenter.RefreshNow();
            Assert.That(_presenter.Preview.gameObject.activeSelf, Is.False);
            Assert.That(_presenter.NegativeHalf.gameObject.activeSelf, Is.True);
            Assert.That(_presenter.PositiveHalf.gameObject.activeSelf, Is.True);
            Assert.That(_presenter.NegativeHalf.sizeDelta.x, Is.GreaterThan(0f));
            Assert.That(_presenter.PositiveHalf.sizeDelta.x, Is.GreaterThan(0f));
        }

        private static PointerSample Sample(
            PointerSamplePhase phase,
            LogicalPoint point) =>
            new PointerSample(
                phase,
                Vector2.zero,
                1,
                false,
                true,
                true,
                point);

        private Vector2 LogicalToScreen(LogicalPoint point)
        {
            Rect board = _composition.BoardCameraFitter.BoardScreenRect;
            return LogicalToRect(board, point);
        }

        private static Vector2 LogicalToRect(Rect board, LogicalPoint point) =>
            new Vector2(
                board.xMin + (point.X / 10f) * board.width,
                board.yMin + (point.Y / 16f) * board.height);

        private void QueueMouse(Vector2 position, bool pressed)
        {
            InputSystem.QueueDeltaStateEvent(_mouse.position, position);
            InputSystem.QueueDeltaStateEvent(_mouse.press, pressed);
        }

        private void QueueTouch(
            Vector2 position,
            int touchId,
            UnityEngine.InputSystem.TouchPhase phase)
        {
            InputSystem.QueueStateEvent(
                _touchscreen,
                new TouchState
                {
                    touchId = touchId,
                    position = position,
                    phase = phase,
                    pressure = phase == UnityEngine.InputSystem.TouchPhase.Ended
                        ? 0f
                        : 1f
                });
        }

        private void RemoveTestDevices()
        {
            if (_mouse != null && _mouse.added)
            {
                InputSystem.RemoveDevice(_mouse);
            }

            if (_touchscreen != null && _touchscreen.added)
            {
                InputSystem.RemoveDevice(_touchscreen);
            }

            _mouse = null;
            _touchscreen = null;
        }

        private static Vector2 GetScreenCenter(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return RectTransformUtility.WorldToScreenPoint(
                null,
                (corners[0] + corners[2]) * 0.5f);
        }
    }
}
