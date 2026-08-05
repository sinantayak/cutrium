using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Input;
using Cutrium.Unity.Layout;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class Milestone2CPlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        private GameObject _root;
        private FirstPlayableController _controller;
        private CaptureBoardPresenter _boardPresenter;
        private CaptureHudPresenter _hudPresenter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;
            BindScene();
        }

        [Test]
        public void Scene_HasSerializedCaptureAndRetryReferences()
        {
            Assert.That(_boardPresenter.Controller, Is.SameAs(_controller));
            Assert.That(_boardPresenter.BoardFrame, Is.Not.Null);
            Assert.That(_boardPresenter.CapturedRegionRoot, Is.Not.Null);
            Assert.That(_boardPresenter.CompletedBarrierRoot, Is.Not.Null);
            Assert.That(_hudPresenter.Controller, Is.SameAs(_controller));
            Assert.That(_hudPresenter.PercentageText, Is.Not.Null);
            Assert.That(_hudPresenter.TargetText, Is.Not.Null);
            Assert.That(_hudPresenter.CompleteOverlay, Is.Not.Null);
            Assert.That(_hudPresenter.CompletionCanvasGroup, Is.Not.Null);
            Assert.That(_hudPresenter.RetryButton, Is.Not.Null);
            Assert.That(_controller.TargetCapturedFraction, Is.EqualTo(0.625f));
        }

        [Test]
        public void CompletionOverlay_IsActiveButHiddenBeforeTarget()
        {
            CanvasGroup group = _hudPresenter.CompletionCanvasGroup;

            Assert.That(_hudPresenter.CompleteOverlay.activeSelf, Is.True);
            Assert.That(_hudPresenter.CompleteOverlay.activeInHierarchy, Is.True);
            Assert.That(group.alpha, Is.Zero);
            Assert.That(group.interactable, Is.False);
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(
                RaycastUiAtOverlayCenter().Contains(
                    _hudPresenter.CompleteOverlay),
                Is.False);
        }

        [Test]
        public void SuccessfulLock_CreatesCapturedChildLineAndPercentageView()
        {
            CompleteCut(new LogicalPoint(2f, 8f), BarrierOrientation.Vertical);
            _boardPresenter.RefreshNow();
            _hudPresenter.RefreshNow();

            Assert.That(_controller.Session.Board.CapturedRooms,
                Has.Count.EqualTo(1));
            Assert.That(_controller.Session.Board.ActiveRooms,
                Has.Count.EqualTo(1));
            Assert.That(_controller.Session.CapturedFraction,
                Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(_boardPresenter.VisibleCapturedRegionCount,
                Is.EqualTo(1));
            Assert.That(_boardPresenter.VisibleCompletedBarrierCount,
                Is.EqualTo(1));
            Assert.That(_hudPresenter.PercentageText.text,
                Does.Contain("20%"));
            Assert.That(_boardPresenter.CapturedRegionRoot.GetChild(0)
                .gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Completion_BlocksNewBarrierAndRetryRestoresInitialState()
        {
            CompleteLevel();
            _boardPresenter.RefreshNow();
            _hudPresenter.RefreshNow();

            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed));
            Assert.That(_controller.Session.CapturedFraction,
                Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(_hudPresenter.CompleteOverlay.activeSelf, Is.True);
            Assert.That(_hudPresenter.CompletionCanvasGroup.alpha,
                Is.EqualTo(1f));
            Assert.That(_hudPresenter.CompletionCanvasGroup.interactable, Is.True);
            Assert.That(_hudPresenter.CompletionCanvasGroup.blocksRaycasts, Is.True);
            Assert.That(_controller.SubmitBarrierIntent(new BarrierIntent(
                new LogicalPoint(8f, 12f),
                BarrierOrientation.Vertical)).RejectionReason,
                Is.EqualTo(BarrierRejectionReason.LevelCompleted));

            _hudPresenter.RetryButton.onClick.Invoke();
            _boardPresenter.RefreshNow();
            _hudPresenter.RefreshNow();

            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Playing));
            Assert.That(_controller.Session.CapturedFraction, Is.Zero);
            Assert.That(_controller.Session.Board.ActiveRooms.Single(),
                Is.EqualTo(_controller.Session.InitialRoom));
            Assert.That(_controller.Session.Board.CapturedRooms, Is.Empty);
            Assert.That(_controller.Session.Board.CompletedBarriers, Is.Empty);
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_boardPresenter.VisibleCapturedRegionCount, Is.Zero);
            Assert.That(_boardPresenter.VisibleCompletedBarrierCount, Is.Zero);
            Assert.That(_hudPresenter.CompleteOverlay.activeSelf, Is.True);
            Assert.That(_hudPresenter.CompletionCanvasGroup.alpha, Is.Zero);
            Assert.That(_hudPresenter.CompletionCanvasGroup.interactable, Is.False);
            Assert.That(_hudPresenter.CompletionCanvasGroup.blocksRaycasts, Is.False);
        }

        [UnityTest]
        public IEnumerator AboveTarget_ShowsOverlayAndRetryResetsWholeLoop()
        {
            CompleteCut(new LogicalPoint(2f, 8f), BarrierOrientation.Vertical);
            CompleteCut(new LogicalPoint(4f, 8f), BarrierOrientation.Vertical);
            for (int tick = 0; tick < 90; tick++)
            {
                _controller.AdvanceSimulation(
                    FirstPlayableController.SimulationStep);
            }

            CompleteCutEventually(
                new LogicalPoint(7f, 38f / 3f),
                BarrierOrientation.Horizontal);

            Assert.That(_controller.Session.CapturedFraction,
                Is.EqualTo(0.875f).Within(0.000001f));
            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed));

            yield return null;

            CanvasGroup group = _hudPresenter.CompletionCanvasGroup;
            Assert.That(_hudPresenter.PercentageText.text,
                Is.EqualTo("Captured 88%"));
            Assert.That(_hudPresenter.CompleteOverlay.activeSelf, Is.True);
            Assert.That(_hudPresenter.CompleteOverlay.activeInHierarchy, Is.True);
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(group.interactable, Is.True);
            Assert.That(group.blocksRaycasts, Is.True);
            Assert.That(_hudPresenter.RetryButton.interactable, Is.True);
            Assert.That(
                RaycastUiAtOverlayCenter().Contains(
                    _hudPresenter.CompleteOverlay),
                Is.True);

            _hudPresenter.RetryButton.onClick.Invoke();

            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Playing));
            Assert.That(_controller.Session.CapturedFraction, Is.Zero);
            Assert.That(_controller.Session.Board.ActiveRooms.Single(),
                Is.EqualTo(_controller.Session.InitialRoom));
            Assert.That(_controller.Session.Threat.RoomId,
                Is.EqualTo(_controller.Session.InitialRoom.Id));
            Assert.That(_controller.Session.Threat.Position,
                Is.EqualTo(new LogicalPoint(5f, 8f)));
            Assert.That(_controller.Session.Threat.Velocity.X,
                Is.EqualTo(2.08f).Within(0.000001f));
            Assert.That(_controller.Session.Threat.Velocity.Y,
                Is.EqualTo(1.56f).Within(0.000001f));
            Assert.That(_controller.Session.Board.CapturedRooms, Is.Empty);
            Assert.That(_controller.Session.Board.CompletedBarriers, Is.Empty);
            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(group.alpha, Is.Zero);
            Assert.That(group.interactable, Is.False);
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(_controller.BarrierGesture.enabled, Is.True);
            Assert.That(
                _controller.BarrierGesture.PointerInput.PressAction.action.enabled,
                Is.True);

            yield return null;

            Assert.That(group.alpha, Is.Zero);
            Assert.That(group.interactable, Is.False);
            Assert.That(group.blocksRaycasts, Is.False);
        }

        [Test]
        public void CompactLayout_GivesBoardViewportDominantSafeAreaShare()
        {
            Transform safeArea = _root.transform.Find("Canvas/SafeAreaRoot");
            RectTransform top = safeArea.Find("TopHUD")
                .GetComponent<RectTransform>();
            RectTransform board = safeArea.Find("BoardViewport")
                .GetComponent<RectTransform>();
            RectTransform bottom = safeArea.Find("BottomHUD")
                .GetComponent<RectTransform>();
            RectTransform progress = safeArea.Find("TopHUD/ProgressArea")
                .GetComponent<RectTransform>();
            RectTransform blocker = safeArea.Find(
                "TopHUD/HudBlockerButton").GetComponent<RectTransform>();
            LayoutElement overlayLayout = _hudPresenter.CompleteOverlay
                .GetComponent<LayoutElement>();
            LayoutElement topLayout = top.GetComponent<LayoutElement>();
            LayoutElement boardLayout = board.GetComponent<LayoutElement>();
            LayoutElement bottomLayout = bottom.GetComponent<LayoutElement>();
            VerticalLayoutGroup safeLayout = safeArea
                .GetComponent<VerticalLayoutGroup>();
            HorizontalLayoutGroup topRow = top
                .GetComponent<HorizontalLayoutGroup>();

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                (RectTransform)safeArea);

            Assert.That(board.rect.height, Is.GreaterThan(top.rect.height));
            Assert.That(board.rect.height, Is.GreaterThan(bottom.rect.height));
            Assert.That(topLayout.preferredHeight, Is.EqualTo(60f));
            Assert.That(topLayout.flexibleHeight, Is.Zero);
            Assert.That(bottomLayout.preferredHeight, Is.EqualTo(32f));
            Assert.That(bottomLayout.flexibleHeight, Is.Zero);
            Assert.That(boardLayout.preferredHeight, Is.Zero);
            Assert.That(boardLayout.flexibleHeight, Is.EqualTo(1f));
            Assert.That(safeLayout.childControlHeight, Is.True);
            Assert.That(safeLayout.childForceExpandHeight, Is.False);
            Assert.That(topRow.childControlHeight, Is.True);
            Assert.That(topRow.childForceExpandHeight, Is.False);
            Assert.That(topRow.childForceExpandWidth, Is.False);
            Assert.That(blocker.rect.width, Is.InRange(72f, 100f));
            Assert.That(blocker.rect.height, Is.LessThanOrEqualTo(48f));
            Assert.That(_hudPresenter.PercentageText.transform.parent,
                Is.SameAs(progress));
            Assert.That(_hudPresenter.TargetText.transform.parent,
                Is.SameAs(progress));
            AssertChildrenHaveNonFlexibleHeight(top);
            AssertChildrenHaveNonFlexibleHeight(progress);
            AssertChildrenHaveNonFlexibleHeight(bottom);
            Assert.That(overlayLayout.ignoreLayout, Is.True);
            Assert.That(_hudPresenter.CompleteOverlay.transform.GetSiblingIndex(),
                Is.EqualTo(safeArea.childCount - 1));
            Assert.That(safeArea.Find("TopHUD/Title").GetComponent<Text>().text,
                Is.EqualTo("CUTRIUM"));
            Assert.That(safeArea.Find("TopHUD/HudBlockerButton/Label")
                .GetComponent<Text>().text, Is.EqualTo("UI TEST"));
            Assert.That(safeArea.Find("BoardViewport/BoardFrame/BoardLabel")
                .gameObject.activeSelf, Is.False);
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void CompactLayout_TargetAspectKeepsFullBoardDominant(
            float width,
            float height)
        {
            ResolvedLayout layout = ResolveLayoutAt(width, height);
            TestContext.WriteLine(
                $"{width:0}x{height:0}: Safe={layout.SafeSize}, " +
                $"Top={layout.TopSize}, Board={layout.BoardSize}, " +
                $"Bottom={layout.BottomSize}, UI TEST={layout.ButtonSize}");

            Assert.That(layout.TopSize.y,
                Is.LessThanOrEqualTo(layout.SafeSize.y * 0.12f));
            Assert.That(layout.BottomSize.y,
                Is.LessThanOrEqualTo(layout.SafeSize.y * 0.08f));
            Assert.That(layout.BoardSize.y,
                Is.GreaterThanOrEqualTo(layout.SafeSize.y * 0.7f));
            Assert.That(layout.ButtonSize.x,
                Is.InRange(72f, 100f));
            Assert.That(layout.ButtonSize.y,
                Is.LessThanOrEqualTo(48f));
            Assert.That(layout.FittedBoard.width / layout.FittedBoard.height,
                Is.EqualTo(10f / 16f).Within(0.00001f));
            Assert.That(layout.FittedBoard.xMin,
                Is.GreaterThanOrEqualTo(-0.001f));
            Assert.That(layout.FittedBoard.yMin,
                Is.GreaterThanOrEqualTo(-0.001f));
            Assert.That(layout.FittedBoard.xMax,
                Is.LessThanOrEqualTo(layout.BoardSize.x + 0.001f));
            Assert.That(layout.FittedBoard.yMax,
                Is.LessThanOrEqualTo(layout.BoardSize.y + 0.001f));
            Assert.That(layout.OverlaySize.x,
                Is.EqualTo(layout.SafeSize.x).Within(0.01f));
            Assert.That(layout.OverlaySize.y,
                Is.EqualTo(layout.SafeSize.y).Within(0.01f));
        }

        [Test]
        public void RepeatedRetry_DoesNotDuplicateSceneObjectsOrSubscriptions()
        {
            int initializationCount = _controller.InitializationCount;

            _hudPresenter.RetryButton.onClick.Invoke();
            _hudPresenter.RetryButton.onClick.Invoke();

            Assert.That(_controller.RetryCount, Is.EqualTo(2));
            Assert.That(_controller.InitializationCount,
                Is.EqualTo(initializationCount));
            Assert.That(
                _root.GetComponentsInChildren<FirstPlayableController>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                _root.GetComponentsInChildren<CaptureBoardPresenter>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                _root.GetComponentsInChildren<CaptureHudPresenter>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                _root.GetComponentsInChildren<BarrierGestureAdapter>(true),
                Has.Length.EqualTo(1));
        }

        [Test]
        public void PlaceholderCaptureVisuals_CanBeDisabledWithoutLogicalEffect()
        {
            _boardPresenter.enabled = false;

            CompleteCut(new LogicalPoint(2f, 8f), BarrierOrientation.Vertical);

            Assert.That(_controller.Session.CapturedFraction,
                Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(_controller.Session.Board.CapturedRooms,
                Has.Count.EqualTo(1));
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void SupportedAspect_DoesNotChangeLogicalCapture(
            float width,
            float height)
        {
            Rect fitted = BoardViewportLayout.CalculateAspectFitRect(
                new Rect(0f, 0f, width, height));
            Assert.That(fitted.width / fitted.height,
                Is.EqualTo(10f / 16f).Within(0.0001f));

            CompleteCut(new LogicalPoint(2f, 8f), BarrierOrientation.Vertical);

            Assert.That(_controller.Session.Board.InitialBounds,
                Is.EqualTo(new LogicalRect(0f, 0f, 10f, 16f)));
            Assert.That(_controller.Session.CapturedFraction,
                Is.EqualTo(0.2f).Within(0.0001f));
        }

        private void CompleteLevel()
        {
            CompleteCut(new LogicalPoint(2f, 8f), BarrierOrientation.Vertical);
            CompleteCut(new LogicalPoint(4f, 8f), BarrierOrientation.Vertical);
            CompleteCut(new LogicalPoint(6f, 8f), BarrierOrientation.Vertical);
            CompleteCut(new LogicalPoint(8f, 6f), BarrierOrientation.Horizontal);
        }

        private void CompleteCut(
            LogicalPoint origin,
            BarrierOrientation orientation)
        {
            BarrierStartResult start = _controller.SubmitBarrierIntent(
                new BarrierIntent(origin, orientation));
            Assert.That(start.Accepted, Is.True, start.RejectionReason.ToString());
            for (int tick = 0;
                 tick < 120 && _controller.Session.ActiveBarrier.HasValue;
                 tick++)
            {
                _controller.AdvanceSimulation(
                    FirstPlayableController.SimulationStep);
            }

            Assert.That(_controller.Session.ActiveBarrier.HasValue, Is.False);
            Assert.That(_controller.Session.LastBarrierEvent,
                Is.EqualTo(BarrierSimulationEvent.Locked));
        }

        private void CompleteCutEventually(
            LogicalPoint origin,
            BarrierOrientation orientation)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                BarrierStartResult start = _controller.SubmitBarrierIntent(
                    new BarrierIntent(origin, orientation));
                Assert.That(
                    start.Accepted,
                    Is.True,
                    start.RejectionReason.ToString());
                for (int tick = 0;
                     tick < 120 && _controller.Session.ActiveBarrier.HasValue;
                     tick++)
                {
                    _controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }

                if (_controller.Session.LastBarrierEvent
                    == BarrierSimulationEvent.Locked)
                {
                    return;
                }

                Assert.That(_controller.Session.LastBarrierEvent,
                    Is.EqualTo(BarrierSimulationEvent.Failed));
                for (int tick = 0; tick < 30; tick++)
                {
                    _controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }
            }

            Assert.Fail("The deterministic 87.5% cut never found a safe window.");
        }

        private void BindScene()
        {
            _root = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            _controller = _root
                .GetComponentInChildren<FirstPlayableController>(true);
            _boardPresenter = _root
                .GetComponentInChildren<CaptureBoardPresenter>(true);
            _hudPresenter = _root
                .GetComponentInChildren<CaptureHudPresenter>(true);
            Canvas.ForceUpdateCanvases();
            _boardPresenter.RefreshNow();
            _hudPresenter.RefreshNow();
        }

        private IReadOnlyList<GameObject> RaycastUiAtOverlayCenter()
        {
            EventSystem eventSystem = _root
                .GetComponentInChildren<EventSystem>(true);
            RectTransform overlay = (RectTransform)_hudPresenter
                .CompleteOverlay.transform;
            Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(
                null,
                overlay.TransformPoint(overlay.rect.center));
            var eventData = new PointerEventData(eventSystem)
            {
                position = screenCenter,
            };
            var raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(eventData, raycastResults);
            return raycastResults
                .Select(result => result.gameObject)
                .ToArray();
        }

        private ResolvedLayout ResolveLayoutAt(float width, float height)
        {
            Transform sourceSafeArea = _root.transform.Find(
                "Canvas/SafeAreaRoot");
            CanvasScaler scaler = sourceSafeArea.parent
                .GetComponent<CanvasScaler>();
            Vector2 canvasSize = CalculateCanvasSize(scaler, width, height);

            var host = new GameObject(
                "ResolvedLayoutTestHost",
                typeof(RectTransform));
            host.SetActive(false);
            RectTransform hostRect = (RectTransform)host.transform;
            hostRect.sizeDelta = canvasSize;
            GameObject cloneObject = Object.Instantiate(
                sourceSafeArea.gameObject,
                hostRect,
                false);
            RectTransform clone = (RectTransform)cloneObject.transform;
            clone.anchorMin = Vector2.zero;
            clone.anchorMax = Vector2.one;
            clone.offsetMin = Vector2.zero;
            clone.offsetMax = Vector2.zero;
            clone.GetComponent<SafeAreaFitter>().enabled = false;
            clone.GetComponentInChildren<DebugPointerStatusView>(true).enabled =
                false;
            host.SetActive(true);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(clone);
            Canvas.ForceUpdateCanvases();

            RectTransform top = (RectTransform)clone.Find("TopHUD");
            RectTransform board = (RectTransform)clone.Find("BoardViewport");
            RectTransform bottom = (RectTransform)clone.Find("BottomHUD");
            RectTransform button = (RectTransform)clone.Find(
                "TopHUD/HudBlockerButton");
            RectTransform overlay = (RectTransform)clone.Find(
                "LevelCompleteOverlay");
            Rect fittedBoard = BoardViewportLayout.CalculateAspectFitRect(
                new Rect(Vector2.zero, board.rect.size));
            var result = new ResolvedLayout(
                clone.rect.size,
                top.rect.size,
                board.rect.size,
                bottom.rect.size,
                button.rect.size,
                overlay.rect.size,
                fittedBoard);
            Object.DestroyImmediate(host);
            return result;
        }

        private static Vector2 CalculateCanvasSize(
            CanvasScaler scaler,
            float width,
            float height)
        {
            Assert.That(scaler.uiScaleMode,
                Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.screenMatchMode,
                Is.EqualTo(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight));
            Vector2 reference = scaler.referenceResolution;
            float logWidth = Mathf.Log(width / reference.x, 2f);
            float logHeight = Mathf.Log(height / reference.y, 2f);
            float scale = Mathf.Pow(
                2f,
                Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight));
            return new Vector2(width / scale, height / scale);
        }

        private static void AssertChildrenHaveNonFlexibleHeight(
            Transform parent)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                LayoutElement layout = child.GetComponent<LayoutElement>();
                Assert.That(layout, Is.Not.Null, child.name);
                Assert.That(layout.flexibleHeight,
                    Is.LessThanOrEqualTo(0f),
                    child.name);
            }
        }

        private readonly struct ResolvedLayout
        {
            public ResolvedLayout(
                Vector2 safeSize,
                Vector2 topSize,
                Vector2 boardSize,
                Vector2 bottomSize,
                Vector2 buttonSize,
                Vector2 overlaySize,
                Rect fittedBoard)
            {
                SafeSize = safeSize;
                TopSize = topSize;
                BoardSize = boardSize;
                BottomSize = bottomSize;
                ButtonSize = buttonSize;
                OverlaySize = overlaySize;
                FittedBoard = fittedBoard;
            }

            public Vector2 SafeSize { get; }

            public Vector2 TopSize { get; }

            public Vector2 BoardSize { get; }

            public Vector2 BottomSize { get; }

            public Vector2 ButtonSize { get; }

            public Vector2 OverlaySize { get; }

            public Rect FittedBoard { get; }
        }
    }
}
