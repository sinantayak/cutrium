using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.Barriers;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Landmark;
using Cutrium.Unity.Bootstrap;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class LandmarkRevealPlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        private GameObject _root;
        private LandmarkRevealPresenter _landmarkPresenter;
        private BarrierPresenter _barrierPresenter;
        private EventSystem _eventSystem;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
            yield return null;
            _root = SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            _landmarkPresenter = _root
                .GetComponentInChildren<LandmarkRevealPresenter>(true);
            _barrierPresenter = _root
                .GetComponentInChildren<BarrierPresenter>(true);
            _eventSystem = _root.GetComponentInChildren<SceneCompositionRoot>(true)
                .EventSystem;
            Canvas.ForceUpdateCanvases();
        }

        [Test]
        public void Scene_HasOneLandmarkRevealPresenterWithThreeLandmarksAndTunedBarrier()
        {
            Assert.That(
                _root.GetComponentsInChildren<LandmarkRevealPresenter>(true),
                Has.Length.EqualTo(1));
            Assert.That(_landmarkPresenter.ArtworkImage, Is.Not.Null);
            Assert.That(_landmarkPresenter.VeilRoot, Is.Not.Null);
            Assert.That(_landmarkPresenter.CompletionArtworkImage, Is.Not.Null);
            Assert.That(_landmarkPresenter.CompletionTitleText, Is.Not.Null);
            Assert.That(
                _landmarkPresenter.CompletionDescriptionText,
                Is.Not.Null);
            Assert.That(_landmarkPresenter.CompletionSectorText, Is.Not.Null);
            Assert.That(_landmarkPresenter.Landmarks.Count, Is.EqualTo(3));
            Assert.That(
                _landmarkPresenter.Landmarks.Select(l => l.LandmarkId),
                Is.EqualTo(new[]
                {
                    "galata-kulesi",
                    "coastal-lagoon",
                    "desert-dunes",
                }));
            foreach (LandmarkDefinition landmark in _landmarkPresenter.Landmarks)
            {
                Assert.That(landmark.Artwork, Is.Not.Null);
            }

            Assert.That(_barrierPresenter.VisualLogicalThickness,
                Is.EqualTo(0.13f).Within(0.0001f));
        }

        [Test]
        public void PowerButtons_AreHiddenFromTheDefaultGameplayHud()
        {
            // The default level catalog grants zero Freeze Pulse/Instant
            // Barrier charges, which left these buttons permanently
            // non-interactable (visible but dead) in real play; see
            // Milestone6ThreatsAndPowersPlayModeTests for the matching
            // reference-still-valid check.
            Transform powerControls = _root.transform.Find(
                "Canvas/SafeAreaRoot/PowerControls");
            Assert.That(powerControls, Is.Not.Null);
            Assert.That(powerControls.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void NormalGameplayHud_ShowsGameplayTopHudAndBottomProgress()
        {
            Transform safeArea = _root.transform.Find("Canvas/SafeAreaRoot");
            Transform bottomHud = safeArea.Find("BottomHUD");
            Transform topHud = safeArea.Find("TopHUD");
            Transform retryTransform = bottomHud.Find("QuickRetryButton");
            Transform bowl = bottomHud.Find("SandBowl");
            Transform bowlText = bottomHud.Find("BowlTargetText");
            Transform progress = bottomHud.Find("ProgressBar");

            Assert.That(topHud, Is.Not.Null);
            Assert.That(topHud.gameObject.activeSelf, Is.True);
            LayoutElement topLayout = topHud.GetComponent<LayoutElement>();
            Assert.That(topLayout, Is.Not.Null);
            Assert.That(topLayout.preferredHeight, Is.EqualTo(150f));
            Assert.That(topLayout.flexibleHeight, Is.Zero);
            Transform gameplayRow = topHud.Find("GameplayHudRow");
            Assert.That(gameplayRow, Is.Not.Null);
            Assert.That(gameplayRow.gameObject.activeSelf, Is.True);
            for (int index = 0; index < topHud.childCount; index++)
            {
                Transform child = topHud.GetChild(index);
                Assert.That(child.gameObject.activeSelf,
                    Is.EqualTo(child == gameplayRow),
                    child.name);
            }

            AssertTopHudPanel(
                gameplayRow.Find("HealthColumn/HealthHUD"),
                "Health_HUD_0",
                "10x");
            AssertTopHudPanel(
                gameplayRow.Find("ScoreColumn/ScoreHUD"),
                "Score_HUD_0",
                "4200");
            AssertTopHudPanel(
                gameplayRow.Find("CoinColumn/CoinHUD"),
                "Coin_HUD_0",
                "10x");
            Transform settings = gameplayRow.Find(
                "CoinColumn/SettingsSlot/SettingsButton");
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.GetComponent<Image>().sprite.name,
                Is.EqualTo("Settings_Button_0"));
            Assert.That(settings.GetComponent<Button>().interactable, Is.False);
            RectTransform healthPanel = (RectTransform)gameplayRow.Find(
                "HealthColumn/HealthHUD");
            RectTransform scorePanel = (RectTransform)gameplayRow.Find(
                "ScoreColumn/ScoreHUD");
            RectTransform coinPanel = (RectTransform)gameplayRow.Find(
                "CoinColumn/CoinHUD");
            RectTransform settingsRect = (RectTransform)settings;
            Canvas.ForceUpdateCanvases();
            Assert.That(WorldCenter(healthPanel).x,
                Is.LessThan(WorldCenter(scorePanel).x));
            Assert.That(WorldCenter(scorePanel).x,
                Is.LessThan(WorldCenter(coinPanel).x));
            Assert.That(WorldBottom(settingsRect),
                Is.GreaterThanOrEqualTo(WorldTop(coinPanel) - 0.01f));
            Assert.That(retryTransform, Is.Not.Null);
            Assert.That(retryTransform.gameObject.activeSelf, Is.False);
            Assert.That(bowl, Is.Not.Null);
            Assert.That(bowl.gameObject.activeSelf, Is.False);
            Assert.That(bowlText, Is.Not.Null);
            Assert.That(bowlText.gameObject.activeSelf, Is.False);
            Assert.That(progress, Is.Not.Null);
            Assert.That(progress.gameObject.activeSelf, Is.True);

            Button retryButton = retryTransform.GetComponent<Button>();
            Assert.That(retryButton, Is.Not.Null);
            Assert.That(retryButton.interactable, Is.True);
            QuickRetryPresenter quickRetry = _root
                .GetComponentInChildren<QuickRetryPresenter>(true);
            Assert.That(quickRetry, Is.Not.Null);
            Assert.That(quickRetry.RetryButton, Is.SameAs(retryButton));

            SandProgressPresenter progressPresenter = progress
                .GetComponent<SandProgressPresenter>();
            Assert.That(progressPresenter, Is.Not.Null);
            Assert.That(progressPresenter.FrameImage.sprite.name,
                Is.EqualTo("Progress_Frame_0"));
            Assert.That(progressPresenter.BackgroundImage.sprite.name,
                Is.EqualTo("Progress_Background_0"));
            Assert.That(progressPresenter.FillImage.sprite.name,
                Is.EqualTo("Progress_Fill_0"));
            Assert.That(progressPresenter.StartStarImage, Is.Not.Null);
            Assert.That(progressPresenter.StartStarImage.sprite.name,
                Is.EqualTo("Yellow_Star_0"));
            Assert.That(progressPresenter.FillMaskRect
                .GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(progressPresenter.FillStartTarget, Is.Not.Null);
            Assert.That(progressPresenter.FillStartTarget.parent,
                Is.SameAs(progressPresenter.FillMaskRect));
            Assert.That(progressPresenter.FillStartTarget.anchorMin.x,
                Is.Zero);
            Assert.That(_landmarkPresenter.SandDestination,
                Is.SameAs(progressPresenter.FillStartTarget));
            Assert.That(
                Vector3.Distance(
                    progressPresenter.StartStarImage.rectTransform
                        .TransformPoint(Vector3.zero),
                    progressPresenter.FillStartTarget
                        .TransformPoint(Vector3.zero)),
                Is.LessThan(0.01f));
            var starCorners = new Vector3[4];
            var progressCorners = new Vector3[4];
            progressPresenter.StartStarImage.rectTransform.GetWorldCorners(
                starCorners);
            progressPresenter.ProgressBarRect.GetWorldCorners(progressCorners);
            Assert.That(starCorners[0].x,
                Is.EqualTo(progressCorners[0].x).Within(0.01f));
            Assert.That(_landmarkPresenter.SandProgressPresenter,
                Is.SameAs(progressPresenter));

            GameObject[] hits = RaycastAtCenter((RectTransform)progress);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0], Is.SameAs(progress.gameObject));
        }

        private static void AssertTopHudPanel(
            Transform panel,
            string spriteName,
            string value)
        {
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.gameObject.activeSelf, Is.True);
            Image image = panel.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.sprite.name, Is.EqualTo(spriteName));
            Assert.That(image.preserveAspect, Is.True);
            TextMeshProUGUI text = panel.Find("ValueText")
                .GetComponent<TextMeshProUGUI>();
            Assert.That(text.text, Is.EqualTo(value));
            Assert.That(text.font.name, Is.EqualTo("gomarice_rocks SDF"));
            Assert.That(text.alignment,
                Is.EqualTo(TextAlignmentOptions.Center));
            Assert.That(text.color,
                Is.EqualTo(new Color(0.34f, 0.105f, 0.025f, 1f)));
            TextMeshProUGUI shadow = panel.Find("ShadowText")
                .GetComponent<TextMeshProUGUI>();
            Assert.That(shadow, Is.Not.Null);
            Assert.That(shadow.text, Is.EqualTo(value));
            Assert.That(shadow.font, Is.SameAs(text.font));
            Assert.That(shadow.color, Is.EqualTo(Color.white));
            Assert.That(shadow.rectTransform.anchoredPosition,
                Is.EqualTo(new Vector2(2f, -2f)));
        }

        private static Vector3 WorldCenter(RectTransform rect)
        {
            return rect.TransformPoint(rect.rect.center);
        }

        private static float WorldTop(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[2].y;
        }

        private static float WorldBottom(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return corners[0].y;
        }

        private GameObject[] RaycastAtCenter(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 center = RectTransformUtility.WorldToScreenPoint(
                null,
                (corners[0] + corners[2]) * 0.5f);
            var eventData = new PointerEventData(_eventSystem)
            {
                position = center,
            };
            var results = new List<RaycastResult>();
            _eventSystem.RaycastAll(eventData, results);
            return results.Select(result => result.gameObject).ToArray();
        }

        [Test]
        public void FirstThreeLevelsUseFirstThreeLandmarksInCatalogOrder()
        {
            var rig = new IsolatedRig(3);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();
                Assert.That(rig.Presenter.CurrentLandmark,
                    Is.SameAs(rig.Landmarks[0]));
                Assert.That(rig.Presenter.ArtworkImage.sprite,
                    Is.SameAs(rig.Landmarks[0].Artwork));

                Assert.That(rig.CompleteAndAdvance(), Is.True);
                rig.Presenter.RefreshNow();
                Assert.That(rig.Presenter.CurrentLandmark,
                    Is.SameAs(rig.Landmarks[1]));
                Assert.That(rig.Presenter.ArtworkImage.sprite,
                    Is.SameAs(rig.Landmarks[1].Artwork));

                Assert.That(rig.CompleteAndAdvance(), Is.True);
                rig.Presenter.RefreshNow();
                Assert.That(rig.Presenter.CurrentLandmark,
                    Is.SameAs(rig.Landmarks[2]));
                Assert.That(rig.Presenter.ArtworkImage.sprite,
                    Is.SameAs(rig.Landmarks[2].Artwork));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator VeilCoversOnlyActiveRoomsAndFadesOutOnCapture()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();
                Assert.That(rig.Presenter.VisibleVeilCount, Is.EqualTo(1));

                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                // The completed level forces a full reveal regardless of any
                // remaining active area.
                Assert.That(rig.Presenter.VisibleVeilCount, Is.EqualTo(0));

                float waited = 0f;
                while (!rig.Presenter.AllVeilsFullyRevealed && waited < 2f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                Assert.That(rig.Presenter.AllVeilsFullyRevealed, Is.True);
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void ActiveRoomsStayFullyObscuredUntilCaptured()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();

                RoomState activeRoom =
                    rig.Controller.Session.Board.ActiveRooms[0];
                Assert.That(
                    rig.Presenter.ObscuredRoomBounds,
                    Has.Some.EqualTo(activeRoom.Bounds));
                Assert.That(rig.Presenter.WipingRoomBounds, Is.Empty);
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator CapturedRoomsShowSharpArtworkOnceWipeCompletes()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();

                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                LogicalRect capturedBounds =
                    rig.Controller.Session.Board.CapturedRooms[0].Bounds;

                float waited = 0f;
                while (!rig.Presenter.AllVeilsFullyRevealed && waited < 2f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                // Once its wipe finishes, a captured room must show only
                // the sharp artwork -- no lingering sand/wipe composite at
                // its exact rectangle.
                Assert.That(
                    rig.Presenter.ObscuredRoomBounds,
                    Has.None.EqualTo(capturedBounds));
                Assert.That(
                    rig.Presenter.WipingRoomBounds,
                    Has.None.EqualTo(capturedBounds));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void WipeCompletionExactlyMatchesLogicalCapturedRectangle()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();

                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                LogicalRect capturedBounds =
                    rig.Controller.Session.Board.CapturedRooms[0].Bounds;

                // Immediately on capture (no real time has passed yet), the
                // wipe rectangle must be exactly the logical captured
                // rectangle -- not the old parent room, not an
                // approximation.
                Assert.That(
                    rig.Presenter.WipingRoomBounds,
                    Has.Exactly(1).EqualTo(capturedBounds));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator SandGrainBurstSpawnsOnCaptureAndEventuallyReturnsToPool()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();
                Assert.That(rig.Presenter.ActiveGrainCount, Is.Zero);

                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                // A cosmetic grain burst launches the instant a room is
                // captured -- purely decorative, never gating or reading
                // from gameplay state.
                Assert.That(rig.Presenter.ActiveGrainCount, Is.GreaterThan(0));
                Assert.That(rig.Presenter.ActiveGrainCount,
                    Is.GreaterThanOrEqualTo(
                        rig.Presenter.MinimumGrainsPerCapture));
                Assert.That(rig.Presenter.CreatedGrainViewCount,
                    Is.LessThanOrEqualTo(
                        rig.Presenter.MaximumGrainViewCount));

                float waited = 0f;
                while (rig.Presenter.ActiveGrainCount > 0 && waited < 3f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                Assert.That(rig.Presenter.ActiveGrainCount, Is.Zero);
                Assert.That(rig.Presenter.CreatedGrainViewCount,
                    Is.LessThanOrEqualTo(
                        rig.Presenter.MaximumGrainViewCount));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator SandDestinationFollowsMovedProgressStartTarget()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();
                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();
                Assert.That(rig.Presenter.ActiveGrainCount, Is.GreaterThan(0));

                rig.SandDestination.anchoredPosition +=
                    new Vector2(180f, 75f);
                Vector3 expectedWorld =
                    rig.SandDestination.TransformPoint(Vector3.zero);
                float waited = 0f;
                while (rig.Presenter.ActiveGrainCount > 0 && waited < 3f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                Assert.That(rig.Presenter.ActiveGrainCount, Is.Zero);
                Assert.That(
                    Vector3.Distance(
                        rig.Presenter.LastGrainArrivalTargetWorldPosition,
                        expectedWorld),
                    Is.LessThan(0.01f));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator MultipleCapturesEachGetTheirOwnIndependentWipe()
        {
            var rig = new IsolatedRig(
                1,
                explicitLevels: new[] { CreateTwoCutLevel() });
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();

                BarrierStartResult first = rig.Controller.SubmitBarrierIntent(
                    new BarrierIntent(
                        new LogicalPoint(2f, 8f),
                        BarrierOrientation.Vertical));
                Assert.That(first.Accepted, Is.True);
                Assert.That(rig.RunUntilBarrierResolves(), Is.True);
                rig.Presenter.RefreshNow();

                Assert.That(
                    rig.Controller.Session.LevelStatus,
                    Is.Not.EqualTo(CaptureLevelStatus.Completed));
                Assert.That(rig.Presenter.WipingRoomBounds.Count, Is.EqualTo(1));
                LogicalRect firstCaptured = rig.Presenter.WipingRoomBounds[0];
                Assert.That(firstCaptured.Width, Is.EqualTo(2f).Within(0.01f));

                BarrierStartResult second = rig.Controller.SubmitBarrierIntent(
                    new BarrierIntent(
                        new LogicalPoint(8f, 8f),
                        BarrierOrientation.Vertical));
                Assert.That(second.Accepted, Is.True);
                Assert.That(rig.RunUntilBarrierResolves(), Is.True);
                rig.Presenter.RefreshNow();

                Assert.That(
                    rig.Controller.Session.LevelStatus,
                    Is.EqualTo(CaptureLevelStatus.Completed));
                // Three rectangles are wiped independently: the two
                // precisely-captured pieces from each cut, plus the
                // leftover still-active middle room (containing the
                // threat) that completion force-reveals even though it
                // was never individually captured (ADR-021). The first
                // captured piece is neither lost nor restarted when the
                // second capture happens.
                Assert.That(rig.Presenter.WipingRoomBounds.Count, Is.EqualTo(3));
                Assert.That(
                    rig.Presenter.WipingRoomBounds,
                    Has.Exactly(1).EqualTo(firstCaptured));

                float waited = 0f;
                while (!rig.Presenter.AllVeilsFullyRevealed && waited < 3f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                Assert.That(rig.Presenter.AllVeilsFullyRevealed, Is.True);
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void RetryRestoresFullSandCoverageAndClearsStaleWipeState()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();

                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();
                // A wipe is now in flight (not yet finished, since no real
                // time has passed).
                Assert.That(rig.Presenter.WipingRoomBounds, Is.Not.Empty);

                rig.Controller.RetryLevel();
                rig.Presenter.RefreshNow();

                // Retry must not carry over the previous session's
                // in-flight wipe (reused low RoomId values would otherwise
                // let stale bookkeeping leak into the new session) and the
                // fresh initial room must read as fully sand-covered again.
                Assert.That(rig.Presenter.WipingRoomBounds, Is.Empty);
                Assert.That(rig.Presenter.VisibleVeilCount, Is.EqualTo(1));
                RoomState freshRoom = rig.Controller.Session.Board.ActiveRooms[0];
                Assert.That(
                    rig.Presenter.ObscuredRoomBounds,
                    Has.Some.EqualTo(freshRoom.Bounds));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void NextLevelRestoresFullSandCoverageAndClearsStaleWipeState()
        {
            var rig = new IsolatedRig(2);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();

                Assert.That(rig.CompleteAndAdvance(), Is.True);
                rig.Presenter.RefreshNow();

                Assert.That(rig.Presenter.WipingRoomBounds, Is.Empty);
                Assert.That(rig.Presenter.VisibleVeilCount, Is.EqualTo(1));
                RoomState freshRoom = rig.Controller.Session.Board.ActiveRooms[0];
                Assert.That(
                    rig.Presenter.ObscuredRoomBounds,
                    Has.Some.EqualTo(freshRoom.Bounds));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void DisablingPresentationDoesNotChangeGameplayState()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();
                rig.Presenter.enabled = false;

                // With the presenter disabled (LateUpdate never runs, and
                // nothing else calls RefreshNow), gameplay must still
                // capture normally -- presentation reads gameplay state, it
                // never writes it.
                bool completed = rig.CompleteWithoutAdvancing();

                Assert.That(completed, Is.True);
                Assert.That(
                    rig.Controller.Session.LevelStatus,
                    Is.EqualTo(CaptureLevelStatus.Completed));
                Assert.That(
                    rig.Controller.Session.CapturedFraction,
                    Is.GreaterThan(0f));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [TestCase(625f, 1000f)]
        [TestCase(480f, 1500f)]
        [TestCase(1024f, 820f)]
        public void SandGeometryStaysProportionalAcrossBoardFrameSizes(
            float frameWidth,
            float frameHeight)
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.SetBoardFrameSize(new Vector2(frameWidth, frameHeight));
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();

                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                // The captured rectangle's *logical* bounds -- what the
                // wipe geometry is computed from -- must be identical
                // regardless of board frame pixel size, whether the frame
                // is a tall-phone or a squarer-tablet proportion; only the
                // on-screen pixel conversion (already covered by
                // BoardCameraFitter's own aspect-fit tests) varies.
                LogicalRect capturedBounds =
                    rig.Controller.Session.Board.CapturedRooms[0].Bounds;
                Assert.That(capturedBounds.Width, Is.EqualTo(0.6f).Within(0.01f));
                Assert.That(capturedBounds.Height, Is.EqualTo(16f).Within(0.01f));
                Assert.That(
                    rig.Presenter.WipingRoomBounds,
                    Has.Exactly(1).EqualTo(capturedBounds));
                // Completion also force-reveals the leftover still-active
                // room (containing the threat) that was never individually
                // captured (ADR-021) -- so two rectangles wipe, not one.
                Assert.That(rig.Presenter.WipingRoomBounds, Has.Count.EqualTo(2));
            }
            finally
            {
                rig.Dispose();
            }
        }

        private static CoreFunLevelDefinition CreateTwoCutLevel() =>
            new CoreFunLevelDefinition(
                "two-cut-test",
                1,
                new Vector2(5f, 8f),
                new Vector2(0f, 1f),
                2f,
                0.35f,
                0.30f,
                8f,
                0.08f,
                0f,
                8,
                16,
                8,
                "Landmark reveal multi-capture test level.",
                20f);

        [Test]
        public void CompletionRevealsFullArtworkAndPopulatesCard()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                Assert.That(rig.Presenter.CompletionArtworkImage.sprite,
                    Is.SameAs(rig.Landmarks[0].Artwork));
                Assert.That(rig.Presenter.CompletionTitleText.text,
                    Is.EqualTo(rig.Landmarks[0].DisplayTitle));
                Assert.That(rig.Presenter.CompletionDescriptionText.text,
                    Is.EqualTo(rig.Landmarks[0].ShortDescription));
                Assert.That(rig.Presenter.CompletionSectorText.text,
                    Is.EqualTo(rig.Landmarks[0].Sector));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator CompletionSequenceStagesScrimThenContentThenButtons()
        {
            var timing = new LandmarkCompletionTiming(
                scrimFadeSeconds: 0.05f,
                contentDelaySeconds: 0.05f,
                contentFadeSeconds: 0.05f,
                buttonsDelaySeconds: 0.2f,
                buttonsFadeSeconds: 0.05f);
            var rig = new IsolatedRig(1, timing);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                // Immediately after completion nothing has faded in yet.
                Assert.That(rig.Presenter.ScrimCanvasGroup.alpha, Is.Zero);
                Assert.That(rig.Presenter.ContentCanvasGroup.alpha, Is.Zero);
                Assert.That(rig.Presenter.NextCanvasGroup.alpha, Is.Zero);

                float waited = 0f;
                while (rig.Presenter.ScrimCanvasGroup.alpha < 1f && waited < 2f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                // The scrim finishes fading in well before content/buttons
                // are given a chance to start.
                Assert.That(rig.Presenter.ScrimCanvasGroup.alpha, Is.EqualTo(1f));
                Assert.That(rig.Presenter.NextCanvasGroup.alpha, Is.Zero);
                Assert.That(rig.Presenter.NextCanvasGroup.interactable, Is.False);

                waited = 0f;
                while (rig.Presenter.NextCanvasGroup.alpha < 1f && waited < 2f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                Assert.That(rig.Presenter.ContentCanvasGroup.alpha, Is.EqualTo(1f));
                Assert.That(rig.Presenter.RetryCanvasGroup.alpha, Is.EqualTo(1f));
                Assert.That(rig.Presenter.NextCanvasGroup.alpha, Is.EqualTo(1f));
                Assert.That(rig.Presenter.NextCanvasGroup.interactable, Is.True);
                Assert.That(rig.Presenter.NextCanvasGroup.blocksRaycasts, Is.True);
            }
            finally
            {
                rig.Dispose();
            }
        }

        private sealed class IsolatedRig
        {
            private readonly List<Texture2D> _textures = new List<Texture2D>();
            private readonly List<Sprite> _sprites = new List<Sprite>();
            private readonly GameObject _simulationObject;
            private RectTransform _frame;

            public IsolatedRig(
                int levelCount,
                LandmarkCompletionTiming? timing = null,
                IReadOnlyList<CoreFunLevelDefinition> explicitLevels = null)
            {
                _simulationObject = new GameObject("LandmarkRevealTestRig");
                _simulationObject.SetActive(false);
                Controller =
                    _simulationObject.AddComponent<FirstPlayableController>();

                CoreFunLevelDefinition[] levels;
                if (explicitLevels != null)
                {
                    levels = explicitLevels.ToArray();
                }
                else
                {
                    levels = new CoreFunLevelDefinition[levelCount];
                    for (int index = 0; index < levelCount; index++)
                    {
                        levels[index] = new CoreFunLevelDefinition(
                            $"tiny-{index}",
                            index + 1,
                            new Vector2(5f, 8f),
                            new Vector2(1f, 0f),
                            1f,
                            0.35f,
                            0.05f,
                            8f,
                            0.08f,
                            3f,
                            8,
                            16,
                            8,
                            "Landmark reveal test level.",
                            10f);
                    }
                }

                Controller.ConfigureLevelsForSetup(levels);

                Landmarks = new LandmarkDefinition[3];
                for (int index = 0; index < Landmarks.Length; index++)
                {
                    var texture = new Texture2D(2, 2);
                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, 2f, 2f),
                        new Vector2(0.5f, 0.5f));
                    _textures.Add(texture);
                    _sprites.Add(sprite);
                    LandmarkDefinition landmark =
                        ScriptableObject.CreateInstance<LandmarkDefinition>();
                    landmark.ConfigureForSetup(
                        $"landmark-{index}",
                        $"Landmark {index}",
                        $"Description {index}",
                        $"Sector {index}",
                        sprite);
                    Landmarks[index] = landmark;
                }

                var sandTexture = new Texture2D(2, 2);
                _textures.Add(sandTexture);

                var frameObject = new GameObject(
                    "Frame",
                    typeof(RectTransform));
                _frame = (RectTransform)frameObject.transform;
                _frame.SetParent(_simulationObject.transform, false);
                _frame.sizeDelta = new Vector2(625f, 1000f);
                RectTransform frame = _frame;

                var grainFlightRootObject = new GameObject(
                    "GrainFlightRoot",
                    typeof(RectTransform));
                var grainFlightRoot =
                    (RectTransform)grainFlightRootObject.transform;
                grainFlightRoot.SetParent(_simulationObject.transform, false);
                grainFlightRoot.anchorMin = Vector2.zero;
                grainFlightRoot.anchorMax = Vector2.one;

                var bowlFillTargetObject = new GameObject(
                    "BowlFillTarget",
                    typeof(RectTransform));
                var bowlFillTarget =
                    (RectTransform)bowlFillTargetObject.transform;
                bowlFillTarget.SetParent(_simulationObject.transform, false);
                bowlFillTarget.anchoredPosition = new Vector2(0f, -1200f);
                SandDestination = bowlFillTarget;

                var artworkObject = new GameObject(
                    "Artwork",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                artworkObject.transform.SetParent(frame, false);
                Image artworkImage = artworkObject.GetComponent<Image>();

                var veilRootObject = new GameObject(
                    "VeilRoot",
                    typeof(RectTransform));
                veilRootObject.transform.SetParent(frame, false);
                var veilRoot = (RectTransform)veilRootObject.transform;

                Text titleText = CreateText(_simulationObject.transform);
                Text descriptionText = CreateText(_simulationObject.transform);
                Text sectorText = CreateText(_simulationObject.transform);
                var completionArtworkObject = new GameObject(
                    "CompletionArtwork",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                completionArtworkObject.transform.SetParent(
                    _simulationObject.transform,
                    false);
                Image completionArtworkImage =
                    completionArtworkObject.GetComponent<Image>();

                CanvasGroup scrimGroup = CreateGroup(_simulationObject.transform);
                CanvasGroup contentGroup = CreateGroup(_simulationObject.transform);
                CanvasGroup statsGroup = CreateGroup(_simulationObject.transform);
                CanvasGroup retryGroup = CreateGroup(_simulationObject.transform);
                CanvasGroup nextGroup = CreateGroup(_simulationObject.transform);

                Presenter = _simulationObject
                    .AddComponent<LandmarkRevealPresenter>();
                Presenter.Configure(
                    Controller,
                    frame,
                    artworkImage,
                    veilRoot,
                    sandTexture,
                    grainFlightRoot,
                    bowlFillTarget,
                    null,
                    0.2f,
                    completionArtworkImage,
                    scrimGroup,
                    contentGroup,
                    statsGroup,
                    retryGroup,
                    nextGroup,
                    titleText,
                    sectorText,
                    descriptionText,
                    timing ?? LandmarkCompletionTiming.Default,
                    Landmarks);

                _simulationObject.SetActive(true);
            }

            public FirstPlayableController Controller { get; }
            public LandmarkRevealPresenter Presenter { get; }
            public LandmarkDefinition[] Landmarks { get; }
            public RectTransform SandDestination { get; }

            public bool CompleteWithoutAdvancing()
            {
                BarrierStartResult start = Controller.SubmitBarrierIntent(
                    new BarrierIntent(
                        new LogicalPoint(0.6f, 8f),
                        BarrierOrientation.Vertical));
                if (!start.Accepted)
                {
                    return false;
                }

                for (int tick = 0;
                     tick < 600
                     && Controller.Session.LevelStatus
                         != CaptureLevelStatus.Completed;
                     tick++)
                {
                    Controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }

                return Controller.Session.LevelStatus
                    == CaptureLevelStatus.Completed;
            }

            public void SetBoardFrameSize(Vector2 size)
            {
                _frame.sizeDelta = size;
            }

            public bool RunUntilBarrierResolves(int maxTicks = 600)
            {
                for (int tick = 0;
                     tick < maxTicks && Controller.Session.ActiveBarrier.HasValue;
                     tick++)
                {
                    Controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }

                return !Controller.Session.ActiveBarrier.HasValue;
            }

            public bool CompleteAndAdvance()
            {
                return CompleteWithoutAdvancing()
                    && Controller.TryAdvanceToNextLevel();
            }

            public void Dispose()
            {
                Object.DestroyImmediate(_simulationObject);
                foreach (LandmarkDefinition landmark in Landmarks)
                {
                    Object.DestroyImmediate(landmark);
                }

                foreach (Sprite sprite in _sprites)
                {
                    Object.DestroyImmediate(sprite);
                }

                foreach (Texture2D texture in _textures)
                {
                    Object.DestroyImmediate(texture);
                }
            }

            private static Text CreateText(Transform parent)
            {
                var textObject = new GameObject(
                    "Text",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                textObject.transform.SetParent(parent, false);
                return textObject.GetComponent<Text>();
            }

            private static CanvasGroup CreateGroup(Transform parent)
            {
                var groupObject = new GameObject(
                    "Group",
                    typeof(RectTransform),
                    typeof(CanvasGroup));
                groupObject.transform.SetParent(parent, false);
                return groupObject.GetComponent<CanvasGroup>();
            }
        }
    }
}
