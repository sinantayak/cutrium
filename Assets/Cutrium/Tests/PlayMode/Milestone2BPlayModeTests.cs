using System.Collections;
using System.Linq;
using Cutrium.Gameplay.Barriers;
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
            Vector2 start = LogicalToScreen(new LogicalPoint(2f, 3f));
            Vector2 end = LogicalToScreen(new LogicalPoint(3f, 3.05f));

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
            start = LogicalToScreen(new LogicalPoint(2f, 3f));
            end = LogicalToScreen(new LogicalPoint(3f, 3.05f));

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
                new LogicalPoint(2f, 3f)));
            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Moved,
                new LogicalPoint(3f, 3.05f)));
            _presenter.RefreshNow();

            Assert.That(_presenter.Preview.gameObject.activeSelf, Is.True);
            Assert.That(_presenter.NegativeHalf.gameObject.activeSelf, Is.False);

            _gesture.ProcessSample(Sample(
                PointerSamplePhase.Released,
                new LogicalPoint(3f, 3.05f)));
            _presenter.RefreshNow();

            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.True);
            Assert.That(_presenter.Preview.gameObject.activeSelf, Is.False);
            Assert.That(_presenter.NegativeHalf.gameObject.activeSelf, Is.True);
            Assert.That(_presenter.PositiveHalf.gameObject.activeSelf, Is.True);
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
                new LogicalPoint(2f, 3f),
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

            CommitDirect(new LogicalPoint(2f, 3f), new LogicalPoint(3f, 3f));
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
