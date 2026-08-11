using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
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
            Assert.That(_controller.TargetCapturedFraction, Is.EqualTo(0.825f));
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
            CompleteCut(new LogicalPoint(4f, 8f), BarrierOrientation.Vertical);
            _boardPresenter.RefreshNow();
            _hudPresenter.RefreshNow();

            Assert.That(_controller.Session.Board.CapturedRooms,
                Has.Count.EqualTo(1));
            Assert.That(_controller.Session.Board.ActiveRooms,
                Has.Count.EqualTo(1));
            Assert.That(_controller.Session.CapturedFraction,
                Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(_boardPresenter.VisibleCapturedRegionCount,
                Is.EqualTo(1));
            Assert.That(_boardPresenter.VisibleCompletedBarrierCount,
                Is.EqualTo(1));
            Assert.That(_hudPresenter.PercentageText.text,
                Does.Contain("40%"));
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
                Is.GreaterThanOrEqualTo(
                    _controller.TargetCapturedFraction - 0.0001f));
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
            CompleteLevel();

            Assert.That(_controller.Session.CapturedFraction,
                Is.GreaterThanOrEqualTo(
                    _controller.TargetCapturedFraction - 0.0001f));
            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed));

            yield return null;

            CanvasGroup group = _hudPresenter.CompletionCanvasGroup;
            Assert.That(_hudPresenter.PercentageText.text,
                Does.StartWith("Captured "));
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
                Is.EqualTo(1.28f).Within(0.000001f));
            Assert.That(_controller.Session.Threat.Velocity.Y,
                Is.EqualTo(0.96f).Within(0.000001f));
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
            RectTransform boardStage = safeArea.Find("BoardStage")
                .GetComponent<RectTransform>();
            RectTransform boardViewport = safeArea
                .Find("BoardStage/BoardViewport").GetComponent<RectTransform>();
            RectTransform bottom = safeArea.Find("BottomHUD")
                .GetComponent<RectTransform>();
            RectTransform legacyProgress = safeArea.Find("TopHUD/ProgressArea")
                .GetComponent<RectTransform>();
            RectTransform progress = safeArea.Find("BottomHUD/ProgressBar")
                .GetComponent<RectTransform>();
            LayoutElement overlayLayout = _hudPresenter.CompleteOverlay
                .GetComponent<LayoutElement>();
            LayoutElement topLayout = top.GetComponent<LayoutElement>();
            LayoutElement boardLayout = boardStage.GetComponent<LayoutElement>();
            LayoutElement bottomLayout = bottom.GetComponent<LayoutElement>();
            VerticalLayoutGroup safeLayout = safeArea
                .GetComponent<VerticalLayoutGroup>();
            HorizontalLayoutGroup topRow = top
                .GetComponent<HorizontalLayoutGroup>();

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                (RectTransform)safeArea);
            _root.GetComponentInChildren<BoardCameraFitter>(true).RefreshNow();

            Assert.That(boardStage.rect.height, Is.GreaterThan(top.rect.height));
            Assert.That(boardStage.rect.height, Is.GreaterThan(bottom.rect.height));
            Assert.That(top.gameObject.activeSelf, Is.True);
            Assert.That(topLayout.minHeight, Is.EqualTo(52f));
            Assert.That(topLayout.preferredHeight, Is.EqualTo(60f));
            Assert.That(topLayout.flexibleHeight, Is.Zero);
            Assert.That(bottomLayout.preferredHeight, Is.EqualTo(98f));
            Assert.That(bottomLayout.flexibleHeight, Is.Zero);
            Assert.That(boardLayout.preferredHeight, Is.Zero);
            Assert.That(boardLayout.flexibleHeight, Is.EqualTo(1f));
            Assert.That(safeLayout.childControlHeight, Is.True);
            Assert.That(safeLayout.childForceExpandHeight, Is.False);
            Assert.That(topRow.childControlHeight, Is.True);
            Assert.That(topRow.childForceExpandHeight, Is.False);
            Assert.That(topRow.childForceExpandWidth, Is.False);
            Assert.That(progress.gameObject.activeSelf, Is.True);
            Assert.That(progress.rect.width,
                Is.LessThan(boardViewport.rect.width));
            Assert.That(progress.GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(_hudPresenter.PercentageText.transform.parent,
                Is.SameAs(legacyProgress));
            Assert.That(_hudPresenter.TargetText.transform.parent,
                Is.SameAs(legacyProgress));
            AssertChildrenHaveNonFlexibleHeight(top);
            AssertChildrenHaveNonFlexibleHeight(legacyProgress);
            AssertChildrenHaveNonFlexibleHeight(bottom);
            Assert.That(overlayLayout.ignoreLayout, Is.True);
            Assert.That(_hudPresenter.CompleteOverlay.transform.GetSiblingIndex(),
                Is.EqualTo(safeArea.childCount - 1));
            Assert.That(safeArea.Find("TopHUD/Title").GetComponent<Text>().text,
                Is.EqualTo("LEARN THE CUT"));
            Assert.That(safeArea.Find("TopHUD/HudBlockerButton/Label")
                .GetComponent<Text>().text, Is.EqualTo("UI TEST"));
            for (int index = 0; index < top.childCount; index++)
            {
                Assert.That(top.GetChild(index).gameObject.activeSelf,
                    Is.False,
                    top.GetChild(index).name);
            }
            Assert.That(safeArea.Find(
                    "BoardStage/BoardViewport/BoardFrame/BoardLabel")
                .gameObject.activeSelf, Is.False);

            // BoardViewport must resolve to exactly the aspect-fitted rect
            // within BoardStage -- no leftover letterbox margin of its own.
            Rect expectedFit = BoardViewportLayout.CalculateAspectFitRect(
                new Rect(Vector2.zero, boardStage.rect.size),
                0.5f);
            Assert.That(boardViewport.rect.width,
                Is.EqualTo(expectedFit.width).Within(0.5f));
            Assert.That(boardViewport.rect.height,
                Is.EqualTo(expectedFit.height).Within(0.5f));
            Assert.That(boardViewport.anchoredPosition.x,
                Is.EqualTo(0f).Within(0.01f));
            Assert.That(boardViewport.anchoredPosition.y,
                Is.EqualTo(0f).Within(0.01f));
            Assert.That(_root.GetComponentInChildren<BoardCameraFitter>(true)
                .VerticalAlignment, Is.EqualTo(0.5f));
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
                $"{width:0}x{height:0} pixels: " +
                $"SafeAreaRoot={layout.SafePixelRect}, " +
                $"TopHUD={layout.TopPixelRect}, " +
                $"BoardViewportRegion={layout.BoardStagePixelRect}, " +
                $"Board10x16={layout.BoardPixelRect}, " +
                $"BottomHUD={layout.BottomPixelRect}, " +
                $"ProgressBar={layout.ProgressPixelRect}");

            Assert.That(layout.TopRect.height, Is.GreaterThan(0f));
            Assert.That(layout.TopRect.height,
                Is.LessThanOrEqualTo(layout.SafeSize.y * 0.08f));
            Assert.That(layout.BottomSize.y,
                Is.LessThanOrEqualTo(layout.SafeSize.y * 0.08f));
            Assert.That(layout.BoardStageSize.y,
                Is.GreaterThanOrEqualTo(layout.SafeSize.y * 0.7f));
            Assert.That(layout.BoardRect.width / layout.BoardRect.height,
                Is.EqualTo(10f / 16f).Within(0.00001f));
            Assert.That(layout.ProgressRect.width,
                Is.InRange(
                    layout.BoardRect.width * 0.75f,
                    layout.BoardRect.width * 0.9f));
            Assert.That(layout.ProgressRect.yMax,
                Is.LessThanOrEqualTo(layout.BoardRect.yMin + 0.01f));
            Assert.That(layout.TopRect.yMin,
                Is.GreaterThanOrEqualTo(
                    layout.BoardStageRect.yMax - 0.01f));
            Assert.That(layout.BottomRect.yMax,
                Is.LessThanOrEqualTo(
                    layout.BoardStageRect.yMin + 0.01f));
            Assert.That(layout.BoardRect.center.y,
                Is.EqualTo(layout.BoardStageRect.center.y).Within(0.01f));
            float emptyAbove =
                layout.BoardStageRect.yMax - layout.BoardRect.yMax;
            float emptyBelow =
                layout.BoardRect.yMin - layout.BoardStageRect.yMin;
            Assert.That(emptyAbove,
                Is.EqualTo(emptyBelow).Within(0.01f));
            Assert.That(Mathf.Max(emptyAbove, emptyBelow),
                Is.LessThanOrEqualTo(layout.SafeSize.y * 0.13f));
            Assert.That(layout.ProgressRect.xMin,
                Is.GreaterThanOrEqualTo(layout.BottomRect.xMin - 0.01f));
            Assert.That(layout.ProgressRect.xMax,
                Is.LessThanOrEqualTo(layout.BottomRect.xMax + 0.01f));
            Assert.That(layout.ProgressRect.yMin,
                Is.GreaterThanOrEqualTo(layout.BottomRect.yMin - 0.01f));
            Assert.That(layout.ProgressRect.yMax,
                Is.LessThanOrEqualTo(layout.BottomRect.yMax + 0.01f));
            Assert.That(layout.ProgressRect.center.y,
                Is.EqualTo(layout.BottomRect.center.y).Within(0.01f));
            Assert.That(layout.OverlaySize.x,
                Is.EqualTo(layout.SafeSize.x).Within(0.01f));
            Assert.That(layout.OverlaySize.y,
                Is.EqualTo(layout.SafeSize.y).Within(0.01f));
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void BoardCameraFitter_ResizesBoardViewportWithNoLetterboxing(
            float width,
            float height)
        {
            // The available board area at this target aspect, exactly as
            // CompactLayout_TargetAspectKeepsFullBoardDominant already
            // verifies it via the real HUD layout.
            ResolvedLayout layout = ResolveLayoutAt(width, height);

            var stageObject = new GameObject(
                "IsolatedBoardStage",
                typeof(RectTransform));
            var viewportObject = new GameObject(
                "IsolatedBoardViewport",
                typeof(RectTransform));
            var frameObject = new GameObject(
                "IsolatedBoardFrame",
                typeof(RectTransform));
            var cameraObject = new GameObject("IsolatedBoardCamera");
            try
            {
                var stage = (RectTransform)stageObject.transform;
                stage.sizeDelta = layout.BoardSize;

                var viewport = (RectTransform)viewportObject.transform;
                viewport.SetParent(stage, false);

                var frame = (RectTransform)frameObject.transform;
                frame.SetParent(viewport, false);

                Camera camera = cameraObject.AddComponent<Camera>();
                BoardCameraFitter fitter =
                    stageObject.AddComponent<BoardCameraFitter>();
                fitter.Configure(camera, null, stage, viewport, frame);

                bool applied = fitter.Apply(
                    new Rect(Vector2.zero, layout.BoardSize),
                    layout.BoardSize);

                Assert.That(applied, Is.True);

                Rect expectedFit = BoardViewportLayout.CalculateAspectFitRect(
                    new Rect(Vector2.zero, layout.BoardSize));

                // Bit-level tight: BoardCameraFitter must set BoardViewport
                // to exactly the fitted rect, with no leftover margin of
                // its own -- the whole point of this pass.
                Assert.That(viewport.sizeDelta.x,
                    Is.EqualTo(expectedFit.width).Within(0.001f));
                Assert.That(viewport.sizeDelta.y,
                    Is.EqualTo(expectedFit.height).Within(0.001f));
                Assert.That(viewport.sizeDelta.x,
                    Is.LessThanOrEqualTo(layout.BoardSize.x + 0.001f));
                Assert.That(viewport.sizeDelta.y,
                    Is.LessThanOrEqualTo(layout.BoardSize.y + 0.001f));
                Assert.That(viewport.sizeDelta.x / viewport.sizeDelta.y,
                    Is.EqualTo(10f / 16f).Within(0.0001f));
                Assert.That(viewport.anchoredPosition.x,
                    Is.EqualTo(0f).Within(0.001f));
                Assert.That(viewport.anchoredPosition.y,
                    Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                // viewportObject/frameObject are children of stageObject by
                // this point, so destroying it cascades to both.
                Object.DestroyImmediate(stageObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void BoardMapping_RoundTripsAtAllFourEdgesAndRejectsOutsideBoard()
        {
            Canvas.ForceUpdateCanvases();
            BoardCameraFitter fitter =
                _root.GetComponentInChildren<BoardCameraFitter>(true);
            ScreenToLogicalBoardMapper mapper =
                _root.GetComponentInChildren<ScreenToLogicalBoardMapper>(true);
            fitter.RefreshNow();
            Rect board = fitter.BoardScreenRect;

            // All four edges/corners remain playable and round-trip to the
            // correct logical extreme (screen Y grows upward; logical Y=0
            // is the board's bottom edge, matching LogicalRect's MinY).
            AssertBoardEdgeRoundTrips(
                mapper,
                new Vector2(board.xMin + 1f, board.yMin + 1f),
                new LogicalPoint(0f, 0f));
            AssertBoardEdgeRoundTrips(
                mapper,
                new Vector2(board.xMax - 1f, board.yMin + 1f),
                new LogicalPoint(10f, 0f));
            AssertBoardEdgeRoundTrips(
                mapper,
                new Vector2(board.xMin + 1f, board.yMax - 1f),
                new LogicalPoint(0f, 16f));
            AssertBoardEdgeRoundTrips(
                mapper,
                new Vector2(board.xMax - 1f, board.yMax - 1f),
                new LogicalPoint(10f, 16f));

            // A point just outside the fitted board -- in what used to be
            // visible BoardViewport letterbox space -- must not be treated
            // as board input; it now belongs to the decorative background.
            Assert.That(
                mapper.TryMap(
                    new Vector2(board.xMin - 5f, board.center.y),
                    out _),
                Is.False);
            Assert.That(
                mapper.TryMap(
                    new Vector2(board.xMax + 5f, board.center.y),
                    out _),
                Is.False);
            Assert.That(
                mapper.TryMap(
                    new Vector2(board.center.x, board.yMin - 5f),
                    out _),
                Is.False);
            Assert.That(
                mapper.TryMap(
                    new Vector2(board.center.x, board.yMax + 5f),
                    out _),
                Is.False);
        }

        private static void AssertBoardEdgeRoundTrips(
            ScreenToLogicalBoardMapper mapper,
            Vector2 screenPoint,
            LogicalPoint expected)
        {
            Assert.That(
                mapper.TryMap(screenPoint, out LogicalPoint logical),
                Is.True,
                $"screen point {screenPoint} should map onto the board");
            Assert.That(logical.X, Is.EqualTo(expected.X).Within(0.05f));
            Assert.That(logical.Y, Is.EqualTo(expected.Y).Within(0.05f));
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

            CompleteCut(new LogicalPoint(4f, 8f), BarrierOrientation.Vertical);

            Assert.That(_controller.Session.CapturedFraction,
                Is.EqualTo(0.4f).Within(0.0001f));
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

            CompleteCut(new LogicalPoint(4f, 8f), BarrierOrientation.Vertical);

            Assert.That(_controller.Session.Board.InitialBounds,
                Is.EqualTo(new LogicalRect(0f, 0f, 10f, 16f)));
            Assert.That(_controller.Session.CapturedFraction,
                Is.EqualTo(0.4f).Within(0.0001f));
        }

        private void CompleteLevel()
        {
            for (int attempt = 0;
                 attempt < 240
                 && _controller.Session.LevelStatus
                    == CaptureLevelStatus.Playing;
                 attempt++)
            {
                for (int tick = 0; tick < 30; tick++)
                {
                    _controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }

                Assert.That(TryChooseCapturingIntent(
                    out BarrierIntent intent), Is.True);
                CompleteCutEventually(intent.Origin, intent.Orientation);
            }

            Assert.That(_controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Completed));
        }

        private void CompleteCut(
            LogicalPoint origin,
            BarrierOrientation orientation)
        {
            BarrierStartResult start = _controller.SubmitBarrierIntent(
                new BarrierIntent(origin, orientation));
            Assert.That(start.Accepted, Is.True, start.RejectionReason.ToString());
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
                     tick < 600 && _controller.Session.ActiveBarrier.HasValue;
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

            Assert.Fail("The deterministic cut never found a safe window.");
        }

        private bool TryChooseCapturingIntent(out BarrierIntent intent)
        {
            ThreatMotionSession session = _controller.Session;
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
                        ref best);
                    float above = threats.Max(threat => threat.Position.Y)
                        + clearance;
                    ConsiderCandidate(
                        room,
                        new BarrierIntent(
                            new LogicalPoint(room.Bounds.Center.X, above),
                            BarrierOrientation.Horizontal),
                        (room.Bounds.MaxY - above) * room.Bounds.Width,
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
                        ref best);
                    float right = threats.Max(threat => threat.Position.X)
                        + clearance;
                    ConsiderCandidate(
                        room,
                        new BarrierIntent(
                            new LogicalPoint(right, room.Bounds.Center.Y),
                            BarrierOrientation.Vertical),
                        (room.Bounds.MaxX - right) * room.Bounds.Height,
                        ref best);
                }
            }

            intent = best.Intent;
            return best.CapturedArea > 0f;
        }

        private static void ConsiderCandidate(
            RoomState room,
            BarrierIntent intent,
            float capturedArea,
            ref CutCandidate best)
        {
            if (capturedArea <= 0f
                || !room.Bounds.Contains(intent.Origin)
                || capturedArea <= best.CapturedArea)
            {
                return;
            }

            best = new CutCandidate(intent, capturedArea);
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

            // BoardStage -- not BoardViewport -- is the pure
            // VerticalLayoutGroup-controlled slot that a static clone (no
            // live BoardCameraFitter running) can correctly resolve; it
            // represents the same "how much space is available for the
            // board" role BoardViewport itself used to play before it
            // became the fitted-and-shrunk shell.
            RectTransform board = (RectTransform)clone.Find("BoardStage");
            RectTransform boardViewport = (RectTransform)clone.Find(
                "BoardStage/BoardViewport");
            RectTransform top = (RectTransform)clone.Find("TopHUD");
            RectTransform bottom = (RectTransform)clone.Find("BottomHUD");
            RectTransform progress = (RectTransform)clone.Find(
                "BottomHUD/ProgressBar");
            RectTransform overlay = (RectTransform)clone.Find(
                "LevelCompleteOverlay");
            Rect fittedBoardLocal = BoardViewportLayout.CalculateAspectFitRect(
                board.rect,
                0.5f);
            boardViewport.anchorMin = new Vector2(0.5f, 0.5f);
            boardViewport.anchorMax = new Vector2(0.5f, 0.5f);
            boardViewport.pivot = new Vector2(0.5f, 0.5f);
            boardViewport.anchoredPosition =
                fittedBoardLocal.center - board.rect.center;
            boardViewport.sizeDelta = fittedBoardLocal.size;
            progress.GetComponent<SandProgressPresenter>().RefreshLayoutNow();
            Canvas.ForceUpdateCanvases();

            var result = new ResolvedLayout(
                clone.rect.size,
                clone.rect,
                RectInAncestor(top, clone),
                RectInAncestor(board, clone),
                RectInAncestor(bottom, clone),
                overlay.rect.size,
                RectInAncestor(boardViewport, clone),
                RectInAncestor(progress, clone),
                width / canvasSize.x);
            Object.DestroyImmediate(host);
            return result;
        }

        private static Rect RectInAncestor(
            RectTransform rect,
            RectTransform ancestor)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 minimum = ancestor.InverseTransformPoint(corners[0]);
            Vector3 maximum = ancestor.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(
                minimum.x,
                minimum.y,
                maximum.x,
                maximum.y);
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
                Rect safeRect,
                Rect topRect,
                Rect boardStageRect,
                Rect bottomRect,
                Vector2 overlaySize,
                Rect boardRect,
                Rect progressRect,
                float pixelScale)
            {
                SafeSize = safeSize;
                SafeRect = safeRect;
                TopRect = topRect;
                BoardStageRect = boardStageRect;
                BottomRect = bottomRect;
                OverlaySize = overlaySize;
                BoardRect = boardRect;
                ProgressRect = progressRect;
                PixelScale = pixelScale;
            }

            public Vector2 SafeSize { get; }

            public Rect SafeRect { get; }

            public Rect TopRect { get; }

            public Rect BoardStageRect { get; }

            public Vector2 BoardStageSize => BoardStageRect.size;

            public Vector2 BoardSize => BoardStageSize;

            public Rect BottomRect { get; }

            public Vector2 BottomSize => BottomRect.size;

            public Vector2 OverlaySize { get; }

            public Rect BoardRect { get; }

            public Rect ProgressRect { get; }

            public float PixelScale { get; }

            public Rect SafePixelRect => ToPixelRect(SafeRect);

            public Rect TopPixelRect => ToPixelRect(TopRect);

            public Rect BoardStagePixelRect => ToPixelRect(BoardStageRect);

            public Rect BoardPixelRect => ToPixelRect(BoardRect);

            public Rect BottomPixelRect => ToPixelRect(BottomRect);

            public Rect ProgressPixelRect => ToPixelRect(ProgressRect);

            private Rect ToPixelRect(Rect rect)
            {
                return new Rect(
                    (rect.xMin - SafeRect.xMin) * PixelScale,
                    (rect.yMin - SafeRect.yMin) * PixelScale,
                    rect.width * PixelScale,
                    rect.height * PixelScale);
            }
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
