using System.Collections;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Threats;
using Cutrium.Presentation.Barriers;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.Theme;
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
    public sealed class Milestone5ThemePlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        private GameObject _root;
        private ThemePresenter _theme;
        private FirstPlayableController _controller;
        private ThreatPresenter _threat;
        private BarrierPresenter _barrier;
        private CaptureBoardPresenter _capture;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
            yield return null;
            _root = SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            _theme = _root.GetComponentInChildren<ThemePresenter>(true);
            _controller = _root
                .GetComponentInChildren<FirstPlayableController>(true);
            _threat = _root.GetComponentInChildren<ThreatPresenter>(true);
            _barrier = _root.GetComponentInChildren<BarrierPresenter>(true);
            _capture = _root
                .GetComponentInChildren<CaptureBoardPresenter>(true);
            Canvas.ForceUpdateCanvases();
            _theme.ApplyNow();
            _threat.RefreshNow();
        }

        [Test]
        public void Scene_HasOneCoherentThemeAndMinimalFallback()
        {
            Assert.That(_root.GetComponentsInChildren<ThemePresenter>(true),
                Has.Length.EqualTo(1));
            Assert.That(_theme.SelectedTheme, Is.Not.Null);
            Assert.That(_theme.SelectedTheme.StableId,
                Is.EqualTo("cleanup-chamber-prototype"));
            Assert.That(_theme.FallbackTheme, Is.Not.Null);
            Assert.That(_theme.FallbackTheme.StableId,
                Is.EqualTo("minimal-flat-fallback"));
            Assert.That(_theme.FallbackTheme.BackgroundColor,
                Is.EqualTo(new Color(0.09f, 0.05f, 0.035f, 1f)));
            Assert.That(_theme.SelectedTheme.BackgroundSprite, Is.Null);
            Assert.That(_theme.SelectedTheme.BackgroundColor,
                Is.EqualTo(new Color(0.12f, 0.07f, 0.045f, 1f)));
            Assert.That(_theme.SelectedTheme.BoardSprite, Is.Not.Null);
            Assert.That(_theme.SelectedTheme.FrameSprite, Is.Not.Null);
            Assert.That(_theme.SelectedTheme.ThreatSprite, Is.Not.Null);
            Assert.That(_theme.SelectedTheme.ThreatSprite.name,
                Is.EqualTo("Threat_Visual_0"));
            Assert.That(_theme.SelectedTheme.ThreatTrailColor,
                Is.EqualTo(new Color(1f, 0.78f, 0.68f, 0.86f)));
            Assert.That(_theme.SelectedTheme.BarrierGrowingColor,
                Is.EqualTo(new Color(0.38f, 0.15f, 0.055f, 1f)));
            Assert.That(_theme.SelectedTheme.BarrierPreviewColor,
                Is.EqualTo(new Color(0.38f, 0.15f, 0.055f, 0.9f)));
            Assert.That(_theme.SelectedTheme.BarrierBodySprite, Is.Not.Null);
            Assert.That(_theme.SelectedTheme.BarrierCapSprite, Is.Not.Null);
            Assert.That(_theme.SelectedTheme.CaptureSprite, Is.Null);
            Assert.That(_theme.SelectedTheme.CaptureColor, Is.EqualTo(Color.clear));
            Assert.That(_theme.FallbackTheme.ThreatSprite, Is.Null);
            Assert.That(_theme.FallbackTheme.CaptureMaterial, Is.Null);
            Assert.That(_theme.Current.StableId,
                Is.EqualTo(_theme.SelectedTheme.StableId));
        }

        [Test]
        public void ThemeSwap_ChangesPresentationOnly()
        {
            ThreatState[] threatsBefore = _controller.Session.Threats.ToArray();
            LogicalRect[] roomsBefore = _controller.Session.Board.ActiveRooms
                .Select(room => room.Bounds)
                .ToArray();
            float capturedBefore = _controller.Session.CapturedFraction;
            int ticksBefore = _controller.Session.TickCount;
            ThemeDefinition cleanup = _theme.SelectedTheme;

            _theme.SetSelectedTheme(null);

            Assert.That(_theme.Current.StableId,
                Is.EqualTo("minimal-flat-fallback"));
            Assert.That(_theme.Background.sprite, Is.Null);
            Assert.That(_theme.BoardSurface.sprite, Is.Null);
            Assert.That(_threat.Image.sprite, Is.Null);
            Assert.That(_controller.Session.Threats, Is.EqualTo(threatsBefore));
            Assert.That(_controller.Session.Board.ActiveRooms
                    .Select(room => room.Bounds),
                Is.EqualTo(roomsBefore));
            Assert.That(_controller.Session.CapturedFraction,
                Is.EqualTo(capturedBefore));
            Assert.That(_controller.Session.TickCount, Is.EqualTo(ticksBefore));

            _theme.SetSelectedTheme(cleanup);
            Assert.That(_theme.Background.sprite, Is.Null);
            Assert.That(_theme.Background.color,
                Is.EqualTo(cleanup.BackgroundColor));
            Assert.That(_threat.Image.sprite, Is.Not.Null);
        }

        [Test]
        public void ThreatVisualScaleAndOffsetNeverChangeLogicalRadiusOrMotion()
        {
            ThemeDefinition temporary = CreateRuntimeTheme(
                "scaled-test",
                new Vector2(1.6f, 0.7f),
                new Vector2(0.2f, -0.1f));
            ThemeDefinition cleanup = _theme.SelectedTheme;
            try
            {
                ThreatState logicalBefore = _controller.Session.Threat;
                _theme.SetSelectedTheme(temporary);
                _threat.RefreshNow();
                Vector2 size = _threat.Visual.sizeDelta;

                Assert.That(size.x / size.y,
                    Is.EqualTo(1.6f / 0.7f).Within(0.001f));
                Assert.That(_controller.Session.Threat.Radius,
                    Is.EqualTo(logicalBefore.Radius));
                Assert.That(_controller.Session.Threat.Position,
                    Is.EqualTo(logicalBefore.Position));
                Assert.That(_controller.Session.Threat.Velocity,
                    Is.EqualTo(logicalBefore.Velocity));
            }
            finally
            {
                _theme.SetSelectedTheme(cleanup);
                Object.DestroyImmediate(temporary);
            }
        }

        [Test]
        public void ThreatTrail_IsVisibleAndPointsOppositeMotion()
        {
            _threat.RefreshNow();
            RectTransform trail = (RectTransform)_threat.Visual.parent.Find(
                "ThreatTrail");
            Assert.That(trail, Is.Not.Null);
            Assert.That(trail.parent, Is.SameAs(_threat.Visual.parent));
            Assert.That(trail.GetSiblingIndex(),
                Is.LessThan(_threat.Visual.GetSiblingIndex()));

            Image trailImage = trail.GetComponent<Image>();
            Assert.That(trailImage.sprite,
                Is.SameAs(_theme.Current.Threat.TrailSprite));
            Assert.That(trailImage.preserveAspect, Is.True);
            Assert.That(trailImage.color.a, Is.GreaterThanOrEqualTo(0.8f));

            float diameter = _threat.Visual.sizeDelta.x;
            Assert.That(trail.sizeDelta.x,
                Is.GreaterThan(diameter * 1.5f));
            Assert.That(trail.sizeDelta.y,
                Is.EqualTo(trail.sizeDelta.x).Within(0.001f));
            Vector2 relativeTrailPosition =
                trail.anchoredPosition - _threat.Visual.anchoredPosition;
            Assert.That(relativeTrailPosition.magnitude,
                Is.EqualTo(diameter * 0.78f).Within(0.001f));

            ThreatState threat = _controller.Session.Threat;
            Vector2 direction = new Vector2(
                threat.Velocity.X,
                threat.Velocity.Y).normalized;
            Assert.That(
                Vector2.Dot(relativeTrailPosition.normalized, direction),
                Is.LessThan(-0.99f));
        }

        [Test]
        public void GrowingBarrier_UsesOpaqueDarkBrownPresentation()
        {
            BarrierStartResult start = _controller.SubmitBarrierIntent(
                new BarrierIntent(
                    new LogicalPoint(5f, 4f),
                    BarrierOrientation.Horizontal));
            Assert.That(start.Accepted, Is.True);

            _barrier.RefreshNow();
            Color expected = new Color(0.38f, 0.15f, 0.055f, 1f);
            Assert.That(_barrier.ThemeStyle.GrowingColor, Is.EqualTo(expected));
            Assert.That(
                _barrier.NegativeHalf.GetComponent<Image>().color,
                Is.EqualTo(expected));
            Assert.That(
                _barrier.PositiveHalf.GetComponent<Image>().color,
                Is.EqualTo(expected));
        }

        [Test]
        public void ThemeSwapDuringGrowthPreservesLogicalBarrierEndpoints()
        {
            BarrierStartResult start = _controller.SubmitBarrierIntent(
                new BarrierIntent(
                    new LogicalPoint(5f, 4f),
                    BarrierOrientation.Horizontal));
            Assert.That(start.Accepted, Is.True);
            _controller.AdvanceSimulation(
                FirstPlayableController.SimulationStep);
            BarrierState before = _controller.Session.ActiveBarrier.Value;
            ThemeDefinition cleanup = _theme.SelectedTheme;

            _theme.SetSelectedTheme(null);

            BarrierState after = _controller.Session.ActiveBarrier.Value;
            Assert.That(after, Is.EqualTo(before));
            Assert.That(after.NegativeEndpoint,
                Is.EqualTo(before.NegativeEndpoint));
            Assert.That(after.PositiveEndpoint,
                Is.EqualTo(before.PositiveEndpoint));
            Assert.That(_barrier.NegativeHalf.GetComponent<Image>().sprite,
                Is.Null);
            _theme.SetSelectedTheme(cleanup);
        }

        [UnityTest]
        public IEnumerator CapturedRegionStaysTransparentAndSpriteFree()
        {
            ThemeDefinition cleanup = _theme.SelectedTheme;
            _theme.SetSelectedTheme(null);
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

            _capture.RefreshNow();
            Assert.That(_capture.VisibleCapturedRegionCount,
                Is.GreaterThan(0));
            Image image = _capture.CapturedRegionRoot
                .GetChild(0).GetComponent<Image>();
            Assert.That(image.sprite, Is.Null);
            Assert.That(_capture.ThemeStyle.Material, Is.Null);
            Assert.That(image.material, Is.SameAs(image.defaultMaterial));
            Assert.That(image.color, Is.EqualTo(Color.clear));
            Assert.That(_controller.Session.CapturedFraction,
                Is.GreaterThan(0f));
            _theme.SetSelectedTheme(cleanup);
            yield return null;
        }

        [Test]
        public void ThemeReconcilesTwoStableThreatViewsWithoutDuplicates()
        {
            GameObject simulationObject = new GameObject("ThemeTestSimulation");
            simulationObject.SetActive(false);
            FirstPlayableController controller =
                simulationObject.AddComponent<FirstPlayableController>();
            CoreFunLevelDefinition levelThree =
                CoreFunLevelDefinition.CreateMilestone3Defaults()[2];
            var twoThreatLevel = new CoreFunLevelDefinition(
                "theme-two-threat-test",
                1,
                levelThree.Threats,
                levelThree.TargetCapturedFraction,
                levelThree.BarrierGrowthSpeed,
                levelThree.BarrierCollisionHalfWidth,
                levelThree.MinimumCutMargin,
                levelThree.MaximumBarrierSolverIterations,
                levelThree.MaximumCatchUpTicks,
                "Theme reconciliation test.",
                levelThree.MaximumExpectedCompletionSeconds,
                levelThree.PurposeLine);
            controller.ConfigureLevelsForSetup(new[]
            {
                twoThreatLevel,
            });
            var frameObject = new GameObject(
                "Frame",
                typeof(RectTransform));
            var frame = (RectTransform)frameObject.transform;
            frame.SetParent(simulationObject.transform, false);
            frame.sizeDelta = new Vector2(625f, 1000f);
            var visualObject = new GameObject(
                "ThreatVisual",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var visual = (RectTransform)visualObject.transform;
            visual.SetParent(frame, false);
            ThreatPresenter presenter =
                simulationObject.AddComponent<ThreatPresenter>();
            presenter.Configure(
                controller,
                frame,
                visual,
                visualObject.GetComponent<Image>(),
                null,
                0.9f);
            presenter.ApplyTheme(_theme.Current.Threat);
            simulationObject.SetActive(true);
            try
            {
                controller.AdvanceSimulation(0f);
                presenter.RefreshNow();
                Assert.That(controller.ThreatCount, Is.EqualTo(2));
                Assert.That(presenter.ActiveViewCount, Is.EqualTo(2));
                Assert.That(presenter.PresentedThreatIds.Select(id => id.Value),
                    Is.EquivalentTo(new[] { 1, 2 }));

                presenter.RefreshNow();
                Assert.That(presenter.ActiveViewCount, Is.EqualTo(2));
                Assert.That(frame.GetComponentsInChildren<Image>(true)
                    .Count(image => image.transform.name.StartsWith(
                        "ThreatVisual")), Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(simulationObject);
            }
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void ThemePreservesFixedBoardAcrossTargetAspects(
            float width,
            float height)
        {
            Rect viewport = new Rect(0f, 0f, width, height - 96f);
            Rect board = BoardViewportLayout.CalculateAspectFitRect(viewport);
            Assert.That(board.width / board.height,
                Is.EqualTo(10f / 16f).Within(0.0001f));
            Assert.That(BoardViewportLayout.LogicalSize,
                Is.EqualTo(new Vector2(10f, 16f)));
            Assert.That(_theme.Background.raycastTarget, Is.False);
            Assert.That(_theme.BoardSurface.raycastTarget, Is.False);
        }

        private static ThemeDefinition CreateRuntimeTheme(
            string id,
            Vector2 scale,
            Vector2 offset)
        {
            ThemeDefinition theme =
                ScriptableObject.CreateInstance<ThemeDefinition>();
            theme.ConfigureForSetup(
                id,
                null,
                Color.black,
                null,
                Color.gray,
                null,
                Color.cyan,
                null,
                Color.red,
                scale,
                offset,
                null,
                Color.black,
                null,
                Color.clear,
                null,
                null,
                null,
                Color.cyan,
                Color.cyan,
                Color.green,
                Color.red,
                null,
                null,
                Color.green,
                Color.black,
                Color.cyan,
                Color.white);
            return theme;
        }
    }
}
