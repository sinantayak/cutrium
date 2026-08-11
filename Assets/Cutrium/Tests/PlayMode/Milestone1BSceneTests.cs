using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cutrium.Gameplay.Geometry;
using Cutrium.Unity.Bootstrap;
using Cutrium.Unity.Input;
using Cutrium.Unity.Layout;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class Milestone1BSceneTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        private SceneCompositionRoot _compositionRoot;
        private Mouse _testMouse;
        private Touchscreen _testTouchscreen;
        private InputSettings.BackgroundBehavior _originalBackgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode
            _originalEditorInputBehavior;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _originalBackgroundBehavior =
                InputSystem.settings.backgroundBehavior;
            _originalEditorInputBehavior =
                InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior =
                InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode
                    .AllDeviceInputAlwaysGoesToGameView;

            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;

            Scene scene = SceneManager.GetActiveScene();
            GameObject root = scene.GetRootGameObjects().Single(
                candidate => candidate.name == "VerticalSliceRoot");
            _compositionRoot = root.transform
                .Find("SceneCompositionRoot")
                .GetComponent<SceneCompositionRoot>();

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                _compositionRoot.SafeAreaFitter.Target);
            _compositionRoot.BoardCameraFitter.RefreshNow();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_testMouse != null && _testMouse.added)
            {
                InputSystem.RemoveDevice(_testMouse);
            }

            if (_testTouchscreen != null && _testTouchscreen.added)
            {
                InputSystem.RemoveDevice(_testTouchscreen);
            }

            _testMouse = null;
            _testTouchscreen = null;
            InputSystem.settings.backgroundBehavior =
                _originalBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                _originalEditorInputBehavior;
            yield return null;
        }

        [Test]
        public void Scene_HasRequiredHierarchyAndSerializedReferences()
        {
            Transform root = _compositionRoot.transform.parent;

            Assert.That(root.name, Is.EqualTo("VerticalSliceRoot"));
            Assert.That(root.Find("Main Camera"), Is.Not.Null);
            Assert.That(root.Find("Global Light 2D"), Is.Not.Null);
            Assert.That(root.Find("SceneCompositionRoot"), Is.Not.Null);
            Assert.That(root.Find("Canvas"), Is.Not.Null);
            Assert.That(root.Find("EventSystem"), Is.Not.Null);

            Transform safeAreaRoot = root.Find("Canvas/SafeAreaRoot");
            Assert.That(safeAreaRoot, Is.Not.Null);
            Assert.That(safeAreaRoot.Find("TopHUD"), Is.Not.Null);
            Assert.That(safeAreaRoot.Find("BoardStage"), Is.Not.Null);
            Assert.That(
                safeAreaRoot.Find("BoardStage/BoardViewport"),
                Is.Not.Null);
            Assert.That(
                safeAreaRoot.Find("BoardStage/BoardViewport/BoardFrame"),
                Is.Not.Null);
            Assert.That(safeAreaRoot.Find("BottomHUD"), Is.Not.Null);

            Assert.That(_compositionRoot.BoardCamera, Is.Not.Null);
            Assert.That(_compositionRoot.Canvas, Is.Not.Null);
            Assert.That(_compositionRoot.SafeAreaFitter, Is.Not.Null);
            Assert.That(_compositionRoot.BoardCameraFitter, Is.Not.Null);
            Assert.That(
                _compositionRoot.BoardCameraFitter.BoardStage,
                Is.Not.Null);
            Assert.That(
                _compositionRoot.BoardCameraFitter.BoardViewport,
                Is.Not.Null);
            Assert.That(
                _compositionRoot.BoardCameraFitter.BoardFrame,
                Is.Not.Null);
            Assert.That(_compositionRoot.BoardMapper, Is.Not.Null);
            Assert.That(_compositionRoot.EventSystem, Is.Not.Null);
            Assert.That(_compositionRoot.UiInputModule, Is.Not.Null);
            Assert.That(_compositionRoot.UiBlocker, Is.Not.Null);
            Assert.That(_compositionRoot.PointerInput, Is.Not.Null);
        }

        [Test]
        public void EventSystem_UsesDedicatedConfiguredInputSystemUiModule()
        {
            EventSystem eventSystem = _compositionRoot.EventSystem;
            InputSystemUIInputModule module = _compositionRoot.UiInputModule;
            PointerInputAdapter pointerInput = _compositionRoot.PointerInput;

            Assert.That(eventSystem.currentInputModule, Is.SameAs(module));
            Assert.That(module.actionsAsset, Is.Not.Null);
            Assert.That(module.actionsAsset.name, Is.EqualTo("CutriumInput"));
            AssertAction(module.point, "UI", "Point");
            AssertAction(module.leftClick, "UI", "LeftClick");
            AssertAction(module.move, "UI", "Navigate");
            AssertAction(module.submit, "UI", "Submit");
            AssertAction(module.cancel, "UI", "Cancel");

            AssertAction(pointerInput.PointAction, "Gameplay", "Point");
            AssertAction(pointerInput.PressAction, "Gameplay", "Press");
            AssertAction(pointerInput.CancelAction, "Gameplay", "Cancel");
            Assert.That(pointerInput.PointAction.action.enabled, Is.True);
            Assert.That(pointerInput.PressAction.action.enabled, Is.True);
            Assert.That(pointerInput.CancelAction.action.enabled, Is.True);
        }

        [Test]
        public void FixedLogicalBoard_IsFullyVisibleThroughConfiguredCamera()
        {
            BoardCameraFitter fitter = _compositionRoot.BoardCameraFitter;
            Rect viewport = fitter.ViewportScreenRect;
            Rect board = fitter.BoardScreenRect;

            Assert.That(fitter.LogicalBoardSize, Is.EqualTo(new Vector2(10f, 16f)));
            Assert.That(board.width, Is.GreaterThan(0f));
            Assert.That(board.height, Is.GreaterThan(0f));
            Assert.That(board.xMin, Is.GreaterThanOrEqualTo(viewport.xMin - 0.01f));
            Assert.That(board.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 0.01f));
            Assert.That(board.xMax, Is.LessThanOrEqualTo(viewport.xMax + 0.01f));
            Assert.That(board.yMax, Is.LessThanOrEqualTo(viewport.yMax + 0.01f));

            Camera camera = fitter.BoardCamera;
            Vector3 lowerLeft = camera.WorldToScreenPoint(new Vector3(0f, 0f, 0f));
            Vector3 upperRight = camera.WorldToScreenPoint(new Vector3(10f, 16f, 0f));
            Assert.That(lowerLeft.x, Is.EqualTo(board.xMin).Within(1f));
            Assert.That(lowerLeft.y, Is.EqualTo(board.yMin).Within(1f));
            Assert.That(upperRight.x, Is.EqualTo(board.xMax).Within(1f));
            Assert.That(upperRight.y, Is.EqualTo(board.yMax).Within(1f));
        }

        [UnityTest]
        public IEnumerator BoardPress_IsAcceptedByInfrastructure()
        {
            _testMouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(_testMouse);
            Vector2 boardCenter = _compositionRoot.BoardCameraFitter.BoardScreenRect.center;
            var samples = new List<PointerSample>();
            _compositionRoot.PointerInput.Sampled += samples.Add;

            QueueMouse(boardCenter, true);
            yield return null;

            Assert.That(_testMouse.enabled, Is.True, "The simulated mouse must be enabled.");
            Assert.That(_testMouse.press.isPressed, Is.True,
                "The queued simulated mouse press must reach the Input System.");
            Assert.That(
                _compositionRoot.PointerInput.PressAction.action.controls,
                Does.Contain(_testMouse.press),
                "The gameplay Press action must resolve the simulated mouse.");
            PointerSample sample = samples.First(
                candidate => candidate.Phase == PointerSamplePhase.Started);
            Assert.That(sample.Phase, Is.EqualTo(PointerSamplePhase.Started));
            Assert.That(sample.StartedOverUi, Is.False);
            Assert.That(sample.StartedInsideBoard, Is.True);
            Assert.That(sample.IsInsideBoard, Is.True);
            Assert.That(sample.IsAcceptedBoardStart, Is.True);
            Assert.That(sample.LogicalPoint.X, Is.EqualTo(5f).Within(0.01f));
            Assert.That(sample.LogicalPoint.Y, Is.EqualTo(8f).Within(0.01f));

            QueueMouse(boardCenter, false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HudPress_RemainsBlockedAfterMovingOntoBoard()
        {
            _testMouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(_testMouse);
            RectTransform hudBlocker = _compositionRoot.transform.parent
                .Find("Canvas/SafeAreaRoot/BottomHUD/ProgressBar")
                .GetComponent<RectTransform>();
            Vector2 hudCenter = GetScreenCenter(hudBlocker);
            Vector2 boardCenter = _compositionRoot.BoardCameraFitter.BoardScreenRect.center;
            var samples = new List<PointerSample>();
            _compositionRoot.PointerInput.Sampled += samples.Add;

            QueueMouse(hudCenter, true);
            yield return null;

            PointerSample started = samples.First(
                candidate => candidate.Phase == PointerSamplePhase.Started);
            Assert.That(started.Phase, Is.EqualTo(PointerSamplePhase.Started));
            Assert.That(started.StartedOverUi, Is.True);
            Assert.That(started.IsAcceptedBoardStart, Is.False);

            QueueMouse(boardCenter, true);
            yield return null;

            PointerSample moved = samples.Last(
                candidate => candidate.Phase == PointerSamplePhase.Moved);
            Assert.That(moved.Phase, Is.EqualTo(PointerSamplePhase.Moved));
            Assert.That(moved.StartedOverUi, Is.True);
            Assert.That(moved.IsInsideBoard, Is.True);

            QueueMouse(boardCenter, false);
            yield return null;

            PointerSample released = samples.Last(
                candidate => candidate.Phase == PointerSamplePhase.Released);
            Assert.That(released.Phase, Is.EqualTo(PointerSamplePhase.Released));
            Assert.That(released.StartedOverUi, Is.True);
            Assert.That(_compositionRoot.PointerInput.HasActiveInteraction, Is.False);
        }

        [UnityTest]
        public IEnumerator MouseAndPrimaryTouch_ProduceEquivalentBoardSamples()
        {
            Vector2 boardCenter = _compositionRoot.BoardCameraFitter.BoardScreenRect.center;
            var samples = new List<PointerSample>();
            _compositionRoot.PointerInput.Sampled += samples.Add;

            _testMouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(_testMouse);
            QueueMouse(boardCenter, true);
            yield return null;
            PointerSample mouseSample = samples.First(
                candidate => candidate.Phase == PointerSamplePhase.Started);
            QueueMouse(boardCenter, false);
            yield return null;
            InputSystem.RemoveDevice(_testMouse);
            _testMouse = null;

            samples.Clear();
            _testTouchscreen = InputSystem.AddDevice<Touchscreen>();
            InputSystem.EnableDevice(_testTouchscreen);
            QueueTouch(boardCenter, 17, UnityEngine.InputSystem.TouchPhase.Began);
            yield return null;
            PointerSample touchSample = samples.First(
                candidate => candidate.Phase == PointerSamplePhase.Started);

            Assert.That(mouseSample.Phase, Is.EqualTo(PointerSamplePhase.Started));
            Assert.That(touchSample.Phase, Is.EqualTo(PointerSamplePhase.Started));
            Assert.That(mouseSample.StartedOverUi, Is.False);
            Assert.That(touchSample.StartedOverUi, Is.False);
            Assert.That(mouseSample.StartedInsideBoard, Is.True);
            Assert.That(touchSample.StartedInsideBoard, Is.True);
            Assert.That(mouseSample.LogicalPoint.X,
                Is.EqualTo(touchSample.LogicalPoint.X).Within(0.001f));
            Assert.That(mouseSample.LogicalPoint.Y,
                Is.EqualTo(touchSample.LogicalPoint.Y).Within(0.001f));
            Assert.That(touchSample.PointerId, Is.EqualTo(17));

            QueueTouch(boardCenter, 17, UnityEngine.InputSystem.TouchPhase.Ended);
            yield return null;
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void TargetAspect_FitsCompleteFixedBoardAndRejectsMargins(
            float width,
            float height)
        {
            var viewport = new Rect(0f, 0f, width, height);
            Rect board = BoardViewportLayout.CalculateAspectFitRect(viewport);

            Assert.That(board.width / board.height,
                Is.EqualTo(10f / 16f).Within(0.00001f));
            Assert.That(board.xMin, Is.GreaterThanOrEqualTo(viewport.xMin));
            Assert.That(board.yMin, Is.GreaterThanOrEqualTo(viewport.yMin));
            Assert.That(board.xMax, Is.LessThanOrEqualTo(viewport.xMax));
            Assert.That(board.yMax, Is.LessThanOrEqualTo(viewport.yMax));

            Assert.That(
                BoardScreenMapper.TryMap(
                    board,
                    board.center,
                    out LogicalPoint center),
                Is.True);
            Assert.That(center, Is.EqualTo(new LogicalPoint(5f, 8f)));

            if (board.xMin > viewport.xMin)
            {
                Assert.That(
                    BoardScreenMapper.TryMap(
                        board,
                        new Vector2(viewport.xMin, viewport.center.y),
                        out _),
                    Is.False);
            }

            if (board.yMin > viewport.yMin)
            {
                Assert.That(
                    BoardScreenMapper.TryMap(
                        board,
                        new Vector2(viewport.center.x, viewport.yMin),
                        out _),
                    Is.False);
            }
        }

        [Test]
        public void SafeAreaFitter_AppliesChangesAndSkipsRepeatedWrites()
        {
            var gameObject = new GameObject(
                "SafeAreaFitterTest",
                typeof(RectTransform),
                typeof(SafeAreaFitter));
            var rectTransform = (RectTransform)gameObject.transform;
            SafeAreaFitter fitter = gameObject.GetComponent<SafeAreaFitter>();
            fitter.Configure(rectTransform);
            var screenSize = new Vector2(1080f, 1920f);
            var safeArea = Rect.MinMaxRect(54f, 96f, 1026f, 1872f);

            bool firstApply = fitter.Apply(safeArea, screenSize);
            int countAfterFirstApply = fitter.AppliedLayoutCount;
            bool repeatedApply = fitter.Apply(safeArea, screenSize);

            Assert.That(firstApply, Is.True);
            Assert.That(repeatedApply, Is.False);
            Assert.That(fitter.AppliedLayoutCount, Is.EqualTo(countAfterFirstApply));
            Assert.That(rectTransform.anchorMin.x,
                Is.EqualTo(54f / 1080f).Within(0.00001f));
            Assert.That(rectTransform.anchorMin.y,
                Is.EqualTo(96f / 1920f).Within(0.00001f));
            Assert.That(rectTransform.anchorMax.x,
                Is.EqualTo(1026f / 1080f).Within(0.00001f));
            Assert.That(rectTransform.anchorMax.y,
                Is.EqualTo(1872f / 1920f).Within(0.00001f));

            Object.DestroyImmediate(gameObject);
        }

        private void QueueMouse(Vector2 position, bool pressed)
        {
            InputSystem.QueueDeltaStateEvent(_testMouse.position, position);
            InputSystem.QueueDeltaStateEvent(
                _testMouse.press,
                pressed);
        }

        private void QueueTouch(
            Vector2 position,
            int touchId,
            UnityEngine.InputSystem.TouchPhase phase)
        {
            InputSystem.QueueStateEvent(
                _testTouchscreen,
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

        private static Vector2 GetScreenCenter(RectTransform rectTransform)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return RectTransformUtility.WorldToScreenPoint(
                null,
                (corners[0] + corners[2]) * 0.5f);
        }

        private static void AssertAction(
            InputActionReference reference,
            string mapName,
            string actionName)
        {
            Assert.That(reference, Is.Not.Null);
            Assert.That(reference.action, Is.Not.Null);
            Assert.That(reference.action.actionMap.name, Is.EqualTo(mapName));
            Assert.That(reference.action.name, Is.EqualTo(actionName));
        }
    }
}
