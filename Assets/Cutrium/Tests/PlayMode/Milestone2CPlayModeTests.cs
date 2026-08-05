using System.Collections;
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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
            Assert.That(_hudPresenter.RetryButton, Is.Not.Null);
            Assert.That(_controller.TargetCapturedFraction, Is.EqualTo(0.75f));
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
            Assert.That(_hudPresenter.CompleteOverlay.activeSelf, Is.False);
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
    }
}
