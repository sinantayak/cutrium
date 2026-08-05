using System.Collections;
using System.Linq;
using Cutrium.Gameplay.Threats;
using Cutrium.Presentation.Threats;
using Cutrium.Unity.Layout;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class Milestone2APlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        private FirstPlayableController _controller;
        private ThreatPresenter _presenter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
            yield return null;

            GameObject root = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            _controller = root.GetComponentInChildren<FirstPlayableController>(true);
            _presenter = root.GetComponentInChildren<ThreatPresenter>(true);
            Canvas.ForceUpdateCanvases();
            _presenter.RefreshNow();
        }

        [Test]
        public void Scene_SerializesOneSimulationAndPresenterWithRequiredReferences()
        {
            GameObject root = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");

            Assert.That(
                root.GetComponentsInChildren<FirstPlayableController>(true),
                Has.Length.EqualTo(1));
            Assert.That(
                root.GetComponentsInChildren<ThreatPresenter>(true),
                Has.Length.EqualTo(1));
            Assert.That(_presenter.Controller, Is.SameAs(_controller));
            Assert.That(_presenter.BoardFrame, Is.Not.Null);
            Assert.That(_presenter.Visual, Is.Not.Null);
            Assert.That(_presenter.Image, Is.Not.Null);
            Assert.That(_presenter.Image.raycastTarget, Is.False);
            Assert.That(_presenter.Image.sprite, Is.Not.Null);
            Assert.That(_presenter.PresentedThreatId,
                Is.EqualTo(_controller.Session.Threat.Id));
            Assert.That(_controller.Session.InitialRoom.Bounds.Width, Is.EqualTo(10f));
            Assert.That(_controller.Session.InitialRoom.Bounds.Height, Is.EqualTo(16f));
        }

        [UnityTest]
        public IEnumerator Threat_MovesAndPresenterTracksItsLogicalPosition()
        {
            ThreatState before = _controller.Session.Threat;
            Vector2 visibleBefore = _presenter.Visual.anchoredPosition;

            _controller.AdvanceSimulation(FirstPlayableController.SimulationStep);
            _presenter.RefreshNow();
            yield return null;

            ThreatState after = _controller.Session.Threat;
            Assert.That(after.Position, Is.Not.EqualTo(before.Position));
            Assert.That(_presenter.Visual.anchoredPosition,
                Is.Not.EqualTo(visibleBefore));

            Rect frame = _presenter.BoardFrame.rect;
            float expectedX = ((after.Position.X / 10f) - 0.5f) * frame.width;
            float expectedY = ((after.Position.Y / 16f) - 0.5f) * frame.height;
            Assert.That(_presenter.Visual.anchoredPosition.x,
                Is.EqualTo(expectedX).Within(0.01f));
            Assert.That(_presenter.Visual.anchoredPosition.y,
                Is.EqualTo(expectedY).Within(0.01f));
        }

        [Test]
        public void VisualScale_IsIndependentFromLogicalThreatRadiusAndState()
        {
            ThreatState stateBefore = _controller.Session.Threat;
            float radiusBefore = _controller.ThreatRadius;
            float originalDiameter = _presenter.VisualLogicalDiameter;

            _presenter.SetVisualLogicalDiameter(originalDiameter * 1.5f);
            _presenter.RefreshNow();

            Assert.That(_presenter.Visual.sizeDelta.x, Is.GreaterThan(0f));
            Assert.That(_presenter.Visual.sizeDelta.x,
                Is.EqualTo(_presenter.Visual.sizeDelta.y).Within(0.001f));
            Assert.That(_controller.ThreatRadius, Is.EqualTo(radiusBefore));
            Assert.That(_controller.Session.Threat, Is.EqualTo(stateBefore));
        }

        [UnityTest]
        public IEnumerator Reenable_DoesNotInitializeADuplicateSession()
        {
            Assert.That(_controller.InitializationCount, Is.EqualTo(1));

            _controller.enabled = false;
            yield return null;
            _controller.enabled = true;
            yield return null;

            Assert.That(_controller.InitializationCount, Is.EqualTo(1));
            Assert.That(_controller.Session, Is.Not.Null);
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void SupportedAspect_KeepsThreatVisualInsideCompleteBoard(
            float width,
            float height)
        {
            Rect boardRect = BoardViewportLayout.CalculateAspectFitRect(
                new Rect(0f, 0f, width, height));
            ThreatState threat = _controller.Session.Threat;
            float scale = Mathf.Min(boardRect.width / 10f, boardRect.height / 16f);
            float visualRadius = _presenter.VisualLogicalDiameter * scale * 0.5f;
            var center = new Vector2(
                boardRect.xMin + (threat.Position.X / 10f) * boardRect.width,
                boardRect.yMin + (threat.Position.Y / 16f) * boardRect.height);

            Assert.That(center.x - visualRadius,
                Is.GreaterThanOrEqualTo(boardRect.xMin));
            Assert.That(center.y - visualRadius,
                Is.GreaterThanOrEqualTo(boardRect.yMin));
            Assert.That(center.x + visualRadius,
                Is.LessThanOrEqualTo(boardRect.xMax));
            Assert.That(center.y + visualRadius,
                Is.LessThanOrEqualTo(boardRect.yMax));
        }
    }
}
