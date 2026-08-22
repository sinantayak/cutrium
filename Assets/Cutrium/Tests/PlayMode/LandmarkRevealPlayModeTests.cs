using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.Barriers;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.Feedback;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Landmark;
using Cutrium.Presentation.Powers;
using Cutrium.Presentation.Threats;
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
            _root.GetComponentInChildren<
                    Cutrium.Presentation.Frontend.FrontEndPresenter>(true)
                ?.SkipForTesting();
            _root.GetComponentInChildren<PreLevelIntroPresenter>(true)
                ?.SkipForTesting();
            Canvas.ForceUpdateCanvases();
        }

        [Test]
        public void Scene_HasOneLandmarkRevealPresenterWithFirstTwentyFourLandmarksAndTunedBarrier()
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
            Assert.That(_landmarkPresenter.Landmarks.Count, Is.EqualTo(24));
            Assert.That(
                _landmarkPresenter.Landmarks.Select(l => l.LandmarkId),
                Is.EqualTo(new[]
                {
                    "angkor-wat",
                    "aspendos-tiyatrosu",
                    "aziz-vasil-katedrali",
                    "big-ben-westminster",
                    "borobudur-tapinagi",
                    "brandenburg-kapisi",
                    "burj-al-arab",
                    "burj-khalifa",
                    "buyuk-kanyon",
                    "chichen-itza",
                    "kurtarici-isa-heykeli",
                    "cn-kulesi",
                }));
            foreach (LandmarkDefinition landmark in _landmarkPresenter.Landmarks)
            {
                Assert.That(landmark.Artwork, Is.Not.Null);
            }

            Assert.That(_barrierPresenter.VisualLogicalThickness,
                Is.EqualTo(0.13f).Within(0.0001f));
        }

        [Test]
        public void Scene_AllUiTextUsesLapsusProBold()
        {
            Text[] legacyTexts = _root.GetComponentsInChildren<Text>(true);
            TMP_Text[] tmpTexts = _root.GetComponentsInChildren<TMP_Text>(true);

            Assert.That(legacyTexts, Is.Not.Empty);
            Assert.That(tmpTexts, Is.Not.Empty);
            foreach (Text text in legacyTexts)
            {
                Assert.That(
                    text.font,
                    Is.Not.Null,
                    $"Legacy Text '{text.name}' has no font.");
                Assert.That(
                    text.font.name,
                    Is.EqualTo("LapsusPro-Bold"),
                    $"Legacy Text '{text.name}' uses the wrong font.");
            }

            foreach (TMP_Text text in tmpTexts)
            {
                Assert.That(
                    text.font,
                    Is.Not.Null,
                    $"TMP text '{text.name}' has no font.");
                Assert.That(
                    text.font.name,
                    Is.EqualTo("LapsusPro-Bold SDF"),
                    $"TMP text '{text.name}' uses the wrong font.");
            }
        }

        [Test]
        public void CompletionPopup_UsesLapsusProBoldAndAllocatesReadableTextSpace()
        {
            Transform overlay = _root.transform.Find(
                "Canvas/SafeAreaRoot/LevelCompleteOverlay");
            Transform content = overlay.Find("CompletionContent");
            Text title = content.Find("Title").GetComponent<Text>();
            Text sector = content.Find("Sector").GetComponent<Text>();
            Text description = content.Find("Description").GetComponent<Text>();
            Text summary = overlay.Find("CompleteText").GetComponent<Text>();
            Text retry = overlay.Find("RetryButton/Label").GetComponent<Text>();
            Text next = overlay.Find("NextButton/Label").GetComponent<Text>();
            Font font = title.font;

            Assert.That(font, Is.Not.Null);
            Assert.That(font.name, Is.EqualTo("LapsusPro-Bold"));
            Assert.That(new[] { sector, description, summary, retry, next }
                .All(text => text.font == font), Is.True);
            Assert.That(description.resizeTextForBestFit, Is.True);
            Assert.That(description.resizeTextMinSize,
                Is.GreaterThanOrEqualTo(22));
            Assert.That(description.resizeTextMaxSize, Is.EqualTo(36));
            Assert.That(title.resizeTextMaxSize, Is.EqualTo(56));
            Assert.That(sector.resizeTextMaxSize, Is.EqualTo(30));
            Assert.That(summary.resizeTextMaxSize, Is.EqualTo(30));
            Assert.That(retry.resizeTextMaxSize, Is.EqualTo(40));
            Assert.That(next.resizeTextMaxSize, Is.EqualTo(40));
            Assert.That(description.rectTransform.rect.height,
                Is.GreaterThanOrEqualTo(160f));
            Assert.That(description.rectTransform
                .GetComponent<LayoutElement>().flexibleHeight,
                Is.EqualTo(1f));
            Assert.That(content.GetComponent<VerticalLayoutGroup>().spacing,
                Is.EqualTo(6f));

            RectTransform retryRect = (RectTransform)retry.transform.parent;
            RectTransform nextRect = (RectTransform)next.transform.parent;
            Assert.That(retryRect.rect.height, Is.GreaterThanOrEqualTo(80f));
            Assert.That(retryRect.rect.width,
                Is.EqualTo(280f).Within(0.01f));
            Assert.That(nextRect.rect.height,
                Is.EqualTo(retryRect.rect.height).Within(0.01f));
            Assert.That(retryRect.rect.width / retryRect.rect.height,
                Is.EqualTo(512f / 210f).Within(0.02f));
            Assert.That(nextRect.rect.width / nextRect.rect.height,
                Is.EqualTo(512f / 210f).Within(0.02f));
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)]
        [TestCase(1536f, 2048f)]
        public void CompletionLayout_KeepsPhotoTextAndButtonsCompactAtTargetAspects(
            float width,
            float height)
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.SetCompletionLayoutSize(width, height);
                RectTransform summary = (RectTransform)rig.Presenter
                    .StatsCanvasGroup.transform;
                RectTransform hero = (RectTransform)rig.Presenter
                    .ScrimCanvasGroup.transform;
                RectTransform content = (RectTransform)rig.Presenter
                    .ContentCanvasGroup.transform;
                RectTransform retry = (RectTransform)rig.Presenter
                    .RetryCanvasGroup.transform;
                RectTransform next = (RectTransform)rig.Presenter
                    .NextCanvasGroup.transform;

                Assert.That(hero.rect.width,
                    Is.EqualTo(hero.rect.height).Within(0.01f));
                float expectedHeroSize = Mathf.Min(
                    width * 0.98f,
                    height * 0.63f);
                Assert.That(hero.rect.width,
                    Is.EqualTo(expectedHeroSize).Within(0.01f));
                Assert.That(summary.rect.size, Is.EqualTo(Vector2.zero));
                Assert.That(rig.Presenter.StatsCanvasGroup.alpha, Is.Zero);
                Assert.That(content.rect.height,
                    Is.GreaterThanOrEqualTo(180f));
                Assert.That(retry.rect.width,
                    Is.EqualTo(280f).Within(0.01f));
                Assert.That(retry.rect.height,
                    Is.EqualTo(280f / (512f / 210f)).Within(0.01f));
                Assert.That(next.rect.height,
                    Is.EqualTo(retry.rect.height).Within(0.01f));
                Assert.That(Left(next) - Right(retry),
                    Is.EqualTo(40f).Within(0.01f));
                Assert.That(Bottom(hero) - Top(content),
                    Is.EqualTo(8f).Within(0.01f));
                Assert.That(Bottom(content) - Top(retry),
                    Is.EqualTo(12f).Within(0.01f));
                Assert.That(Left(retry), Is.GreaterThan(-width * 0.5f));
                Assert.That(Right(next), Is.LessThan(width * 0.5f));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void PowerControls_StaysInactiveWhileBottomHudOwnsItsOwnSkillRow()
        {
            // Milestone6SceneSetup still creates its own Freeze/Instant
            // buttons inside a standalone "PowerControls" overlay so that
            // setup stays usable on its own -- those stay put and inert.
            // BottomHUD/BottomHudRow/SkillRow (see
            // GameplayDefaultHud_ShowsThreeSkillSlotsInBottomHudSkillRow)
            // builds and owns its own separate Freeze/Instant/Mock
            // GameObjects rather than reparenting Milestone6's, so
            // PowerHudPresenter's active wiring points at the SkillRow
            // copies while PowerControls' copies sit inactive and unused.
            Transform powerControls = _root.transform.Find(
                "Canvas/SafeAreaRoot/PowerControls");
            Assert.That(powerControls, Is.Not.Null);
            Assert.That(powerControls.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void GameplayDefaultHud_ShowsThreeSkillSlotsInBottomHudSkillRow()
        {
            Transform safeArea = _root.transform.Find("Canvas/SafeAreaRoot");
            Transform skillRow = safeArea.Find(
                "BottomHUD/BottomHudRow/SkillRow");
            Assert.That(skillRow, Is.Not.Null);
            Assert.That(skillRow.gameObject.activeSelf, Is.True);

            Transform freeze = skillRow.Find("FreezePulseButton");
            Transform instant = skillRow.Find("InstantBarrierButton");
            Transform gravity = skillRow.Find("GravityWellButton");
            Assert.That(freeze, Is.Not.Null);
            Assert.That(instant, Is.Not.Null);
            Assert.That(gravity, Is.Not.Null);
            Assert.That(freeze.gameObject.activeSelf, Is.True);
            Assert.That(instant.gameObject.activeSelf, Is.True);
            Assert.That(gravity.gameObject.activeSelf, Is.True);

            Assert.That(freeze.GetComponent<Image>().sprite.name,
                Is.EqualTo("FreezeSkill"));
            Assert.That(instant.GetComponent<Image>().sprite.name,
                Is.EqualTo("InstantBarrierSkill"));
            Assert.That(gravity.GetComponent<Image>().sprite.name,
                Is.EqualTo("GravityWellSkill"));
            Assert.That(gravity.GetComponent<Button>().interactable, Is.False);
            Outline gravityHighlight = gravity.GetComponent<Outline>();
            Assert.That(gravityHighlight, Is.Not.Null);
            Assert.That(gravityHighlight.effectColor,
                Is.EqualTo(new Color(1f, 0.87f, 0.35f, 0.95f)));
            Assert.That(gravityHighlight.effectDistance,
                Is.EqualTo(new Vector2(4f, -4f)));
            Assert.That(gravityHighlight.useGraphicAlpha, Is.True);
            Assert.That(gravityHighlight.enabled, Is.False);

            PowerHudPresenter powerHud = _root
                .GetComponentInChildren<PowerHudPresenter>(true);
            Assert.That(powerHud.FreezePulseRoot,
                Is.SameAs(freeze.gameObject));
            Assert.That(powerHud.InstantBarrierRoot,
                Is.SameAs(instant.gameObject));
            Assert.That(powerHud.GravityWellRoot,
                Is.SameAs(gravity.gameObject));
            Assert.That(powerHud.GravityWellHighlight,
                Is.SameAs(gravityHighlight));
        }

        [Test]
        public void ChapterTwoPresentation_HasGravityCueAndStyledTextButtons()
        {
            GravityWellPresenter[] gravityPresenters = _root
                .GetComponentsInChildren<GravityWellPresenter>(true);
            FirstPlayableController controller = _root
                .GetComponentInChildren<FirstPlayableController>(true);
            Assert.That(gravityPresenters, Has.Length.EqualTo(1));
            Assert.That(gravityPresenters[0].Controller, Is.SameAs(controller));
            Assert.That(gravityPresenters[0].CueRoot.gameObject.activeSelf,
                Is.True);
            Assert.That(gravityPresenters[0].VortexImage, Is.Not.Null);
            Assert.That(gravityPresenters[0].VortexRoot, Is.Not.Null);
            Assert.That(gravityPresenters[0].VortexImage.sprite.texture.name,
                Is.EqualTo("Vortex"));
            Assert.That(gravityPresenters[0].VortexImage.preserveAspect,
                Is.True);
            Assert.That(gravityPresenters[0].VortexImage.raycastTarget,
                Is.False);
            Assert.That(gravityPresenters[0].VortexRoot.anchorMin,
                Is.EqualTo(Vector2.zero));
            Assert.That(gravityPresenters[0].VortexRoot.anchorMax,
                Is.EqualTo(Vector2.one));
            Assert.That(gravityPresenters[0].CueRoot.Find("Icon"), Is.Null);
            Assert.That(gravityPresenters[0].CueRoot.Find("Range"), Is.Null);

            Transform safeArea = _root.transform.Find("Canvas/SafeAreaRoot");
            Transform completion = safeArea.Find("LevelCompleteOverlay");
            Button[] styledButtons =
            {
                completion.Find("RetryButton").GetComponent<Button>(),
                completion.Find("NextButton").GetComponent<Button>(),
            };
            foreach (Button button in styledButtons)
            {
                Image image = button.GetComponent<Image>();
                Text label = button.GetComponentInChildren<Text>(true);
                AspectRatioFitter aspect =
                    button.GetComponent<AspectRatioFitter>();
                Assert.That(image.sprite.name,
                    Is.EqualTo("GeneralButtonBackground"));
                Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(label.GetComponent<Shadow>(), Is.Not.Null);
                Assert.That(label.color, Is.EqualTo(Color.white));
                Assert.That(label.alignment,
                    Is.EqualTo(TextAnchor.MiddleCenter));
                Assert.That(label.fontSize, Is.EqualTo(40));
                Assert.That(label.resizeTextMaxSize, Is.EqualTo(40));
                Assert.That(label.rectTransform.anchorMin,
                    Is.EqualTo(new Vector2(0.08f, 0.08f)));
                Assert.That(label.rectTransform.anchorMax,
                    Is.EqualTo(new Vector2(0.98f, 0.96f)));
                Assert.That(label.rectTransform.offsetMin,
                    Is.EqualTo(new Vector2(0f, 8f)));
                Assert.That(label.rectTransform.offsetMax,
                    Is.EqualTo(new Vector2(0f, 8f)));
                Assert.That(aspect, Is.Not.Null);
                Assert.That(aspect.aspectMode, Is.EqualTo(
                    AspectRatioFitter.AspectMode.WidthControlsHeight));
                Assert.That(aspect.aspectRatio,
                    Is.EqualTo(512f / 210f).Within(0.001f));
            }

        }

        [Test]
        public void GameOverOverlay_UsesPanelArtAndSeparateSquareActions()
        {
            Transform safeArea = _root.transform.Find("Canvas/SafeAreaRoot");
            Transform panel = safeArea.Find(
                "CutLimitFailureOverlay/GameOverPanelBounds/GameOverPanel");
            Assert.That(panel, Is.Not.Null);

            RectTransform panelBounds = (RectTransform)panel.parent;
            Assert.That(panelBounds.anchorMin,
                Is.EqualTo(new Vector2(0.06f, 0.05f)));
            Assert.That(panelBounds.anchorMax,
                Is.EqualTo(new Vector2(0.94f, 0.95f)));

            Image panelImage = panel.GetComponent<Image>();
            AspectRatioFitter panelAspect =
                panel.GetComponent<AspectRatioFitter>();
            Assert.That(panelImage.sprite.texture.name,
                Is.EqualTo("GameOverPanel"));
            Assert.That(panelImage.type, Is.EqualTo(Image.Type.Simple));
            Assert.That(panelImage.preserveAspect, Is.True);
            Assert.That(panelAspect.aspectMode,
                Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));
            Assert.That(panelAspect.aspectRatio,
                Is.EqualTo(768f / 1037f).Within(0.001f));

            Text prompt = panel.Find("FailureText").GetComponent<Text>();
            Assert.That(prompt.text,
                Is.EqualTo("Watch an AD\nto Continue!"));
            Assert.That(prompt.color, Is.EqualTo(Color.white));
            Assert.That(prompt.resizeTextMaxSize, Is.EqualTo(86));
            Assert.That(prompt.resizeTextMinSize, Is.EqualTo(52));
            Assert.That(prompt.rectTransform.anchorMin,
                Is.EqualTo(new Vector2(0.14f, 0.5f)));
            Assert.That(prompt.rectTransform.anchorMax,
                Is.EqualTo(new Vector2(0.86f, 0.5f)));
            Assert.That(prompt.rectTransform.anchoredPosition,
                Is.EqualTo(new Vector2(0f, 50f)));
            Assert.That(prompt.rectTransform.sizeDelta,
                Is.EqualTo(new Vector2(0f, 210f)));
            Assert.That(prompt.GetComponent<Shadow>(), Is.Not.Null);

            Button retry = panel.Find("RetryButton").GetComponent<Button>();
            Button watchAd = panel.Find("WatchAdButton").GetComponent<Button>();
            Assert.That(retry.GetComponent<Image>().sprite.texture.name,
                Is.EqualTo("RetryButton"));
            Assert.That(watchAd.GetComponent<Image>().sprite.texture.name,
                Is.EqualTo("WatchADSButton"));
            Assert.That(retry.interactable, Is.True);
            Assert.That(watchAd.interactable, Is.False);
            Assert.That(watchAd.transition,
                Is.EqualTo(Selectable.Transition.None));
            Assert.That(retry.GetComponent<AspectRatioFitter>().aspectRatio,
                Is.EqualTo(1f));
            Assert.That(watchAd.GetComponent<AspectRatioFitter>().aspectRatio,
                Is.EqualTo(1f));
            Assert.That(((RectTransform)retry.transform).anchorMin.y,
                Is.EqualTo(0.345f).Within(0.0001f));
            Assert.That(((RectTransform)watchAd.transform).anchorMin.y,
                Is.EqualTo(0.345f).Within(0.0001f));
            Assert.That(((RectTransform)retry.transform).anchoredPosition.y,
                Is.EqualTo(-20f).Within(0.0001f));
            Assert.That(((RectTransform)watchAd.transform).anchoredPosition.y,
                Is.EqualTo(-20f).Within(0.0001f));
            Assert.That(retry.GetComponentInChildren<Text>(true), Is.Null);
            Assert.That(watchAd.GetComponentInChildren<Text>(true), Is.Null);

            Text retryLabel = panel.Find("RetryLabel").GetComponent<Text>();
            Text watchAdLabel = panel.Find("WatchAdLabel").GetComponent<Text>();
            Assert.That(retryLabel.text, Is.EqualTo("Retry"));
            Assert.That(watchAdLabel.text, Is.EqualTo("Watch AD"));
            Assert.That(retryLabel.resizeTextMaxSize, Is.EqualTo(48));
            Assert.That(watchAdLabel.resizeTextMaxSize, Is.EqualTo(48));
            Assert.That(retryLabel.rectTransform.anchorMin.y,
                Is.EqualTo(0.185f).Within(0.0001f));
            Assert.That(retryLabel.rectTransform.anchorMax.y,
                Is.EqualTo(0.255f).Within(0.0001f));
            Assert.That(watchAdLabel.rectTransform.anchorMin.y,
                Is.EqualTo(0.185f).Within(0.0001f));
            Assert.That(watchAdLabel.rectTransform.anchorMax.y,
                Is.EqualTo(0.255f).Within(0.0001f));
            Assert.That(retryLabel.GetComponent<Shadow>(), Is.Not.Null);
            Assert.That(watchAdLabel.GetComponent<Shadow>(), Is.Not.Null);

            GameplayIdentityHudPresenter presenter = safeArea
                .GetComponent<GameplayIdentityHudPresenter>();
            Assert.That(presenter.RetryButton, Is.SameAs(retry));
            Assert.That(presenter.WatchAdButton, Is.SameAs(watchAd));
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
            Transform progress = bottomHud.Find(
                "BottomHudRow/ProgressSlot/ProgressBar");

            Assert.That(topHud, Is.Not.Null);
            Assert.That(topHud.gameObject.activeSelf, Is.True);
            LayoutElement topLayout = topHud.GetComponent<LayoutElement>();
            Assert.That(topLayout, Is.Not.Null);
            Assert.That(topLayout.preferredHeight, Is.EqualTo(150f));
            Assert.That(topLayout.flexibleHeight, Is.Zero);
            LayoutElement bottomLayout = bottomHud.GetComponent<LayoutElement>();
            Assert.That(bottomLayout, Is.Not.Null);
            Assert.That(bottomLayout.minHeight, Is.EqualTo(112f));
            Assert.That(bottomLayout.preferredHeight, Is.EqualTo(116f));
            Assert.That(bottomLayout.flexibleHeight, Is.Zero);
            VerticalLayoutGroup safeLayout =
                safeArea.GetComponent<VerticalLayoutGroup>();
            Assert.That(safeLayout.padding.top, Is.EqualTo(10));
            Assert.That(safeLayout.padding.bottom, Is.EqualTo(10));
            Assert.That(safeLayout.spacing, Is.EqualTo(12f));
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

            Transform bar = gameplayRow.Find("TopHudBar");
            Assert.That(bar, Is.Not.Null);
            Assert.That(bar.gameObject.activeSelf, Is.True);
            Image barImage = bar.GetComponent<Image>();
            Assert.That(barImage, Is.Not.Null);
            Assert.That(barImage.sprite.name, Is.EqualTo("BigHUDBackground"));
            Assert.That(barImage.type, Is.EqualTo(Image.Type.Sliced));

            AssertTopHudRegion(bar.Find("HealthHUD"), "9X");
            AssertTopHudRegion(bar.Find("CutHUD"), null);
            // Speed, like Cut, is now dynamically owned by
            // GameplayIdentityHudPresenter (driven by the level's real
            // BarrierGrowthSpeed), so no fixed placeholder string holds
            // once the scene's controller/session start running.
            AssertTopHudRegion(bar.Find("SpeedHUD"), null);
            Transform settings = gameplayRow.Find(
                "SettingsSlot/SettingsButton");
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.GetComponent<Image>().sprite.name,
                Is.EqualTo("Settings_Button_0"));
            Assert.That(settings.GetComponent<Button>().interactable, Is.False);
            RectTransform healthPanel = (RectTransform)bar.Find("HealthHUD");
            RectTransform cutPanel = (RectTransform)bar.Find("CutHUD");
            RectTransform speedPanel = (RectTransform)bar.Find("SpeedHUD");
            RectTransform settingsRect = (RectTransform)settings;
            Canvas.ForceUpdateCanvases();
            Assert.That(WorldCenter(healthPanel).x,
                Is.LessThan(WorldCenter(cutPanel).x));
            Assert.That(WorldCenter(cutPanel).x,
                Is.LessThan(WorldCenter(speedPanel).x));
            Assert.That(healthPanel.rect.height,
                Is.EqualTo(cutPanel.rect.height).Within(0.01f));
            Assert.That(healthPanel.rect.height,
                Is.EqualTo(speedPanel.rect.height).Within(0.01f));
            Assert.That(WorldBottom(settingsRect),
                Is.GreaterThanOrEqualTo(WorldTop((RectTransform)bar) - 0.01f));
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
            Assert.That(progressPresenter.BackgroundImage.sprite.name,
                Is.EqualTo("ProgressBackground"));
            Assert.That(progressPresenter.FillImage.sprite.name,
                Is.EqualTo("ProgressFill"));
            Assert.That(progressPresenter.FillMaskRect
                .GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(progressPresenter.FillStartTarget, Is.Not.Null);
            Assert.That(progressPresenter.FillStartTarget.parent,
                Is.SameAs(progressPresenter.FillMaskRect));
            Assert.That(progressPresenter.FillStartTarget.anchorMin.x,
                Is.Zero);
            Assert.That(_landmarkPresenter.SandDestination,
                Is.SameAs(progressPresenter.FillStartTarget));
            Assert.That(_landmarkPresenter.SandProgressPresenter,
                Is.SameAs(progressPresenter));

            GameObject[] hits = RaycastAtCenter((RectTransform)progress);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0], Is.SameAs(progress.gameObject));
        }

        // A null value skips the exact-text check: the Cut region's text is
        // dynamically owned by GameplayIdentityHudPresenter (driven by the
        // wired level catalog's cut limit), so no fixed placeholder string
        // holds once the scene's controller/session start running.
        private static void AssertTopHudRegion(
            Transform panel,
            string value)
        {
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.gameObject.activeSelf, Is.True);
            TextMeshProUGUI text = panel.Find("ValueText")
                .GetComponent<TextMeshProUGUI>();
            if (value != null)
            {
                Assert.That(text.text, Is.EqualTo(value));
            }
            Assert.That(text.font.name, Is.EqualTo("LapsusPro-Bold SDF"));
            Assert.That(text.alignment,
                Is.EqualTo(TextAlignmentOptions.Center));
            Assert.That(text.color,
                Is.EqualTo(new Color(0.34f, 0.105f, 0.025f, 1f)));
            TextMeshProUGUI shadow = panel.Find("ShadowText")
                .GetComponent<TextMeshProUGUI>();
            Assert.That(shadow, Is.Not.Null);
            if (value != null)
            {
                Assert.That(shadow.text, Is.EqualTo(value));
            }
            Assert.That(shadow.font, Is.SameAs(text.font));
            Assert.That(shadow.color, Is.EqualTo(Color.white));
            // Component-wise with a tolerance, not exact Vector2 equality:
            // on the much wider full-width bar, a real Canvas layout pass
            // can leave anchoredPosition a hair off the literal (2,-2) it
            // was assigned, from float rounding in the wider anchor-
            // fraction math -- invisible on screen, but an exact `==`
            // check treats it as a mismatch.
            Assert.That(shadow.rectTransform.anchoredPosition.x,
                Is.EqualTo(2f).Within(0.01f));
            Assert.That(shadow.rectTransform.anchoredPosition.y,
                Is.EqualTo(-2f).Within(0.01f));
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

        private static float Top(RectTransform rect) =>
            rect.anchoredPosition.y + (rect.rect.height * 0.5f);

        private static float Bottom(RectTransform rect) =>
            rect.anchoredPosition.y - (rect.rect.height * 0.5f);

        private static float Left(RectTransform rect) =>
            rect.anchoredPosition.x - (rect.rect.width * 0.5f);

        private static float Right(RectTransform rect) =>
            rect.anchoredPosition.x + (rect.rect.width * 0.5f);

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
        public IEnumerator CompletionPopupWaitsForFinalCapturePresentation()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                Assert.That(rig.Controller.Session.LevelStatus,
                    Is.EqualTo(CaptureLevelStatus.Completed));
                Assert.That(rig.Presenter.InFlightWipeCount,
                    Is.GreaterThan(0));
                Assert.That(rig.Presenter.CompletionPresentationReady,
                    Is.False);
                Assert.That(rig.Presenter.ScrimCanvasGroup.alpha, Is.Zero);

                float waited = 0f;
                while (!rig.Presenter.CompletionPresentationReady
                    && waited < 2f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                Assert.That(rig.Presenter.AllVeilsFullyRevealed, Is.True);
                Assert.That(rig.Presenter.CompletionPresentationReady,
                    Is.True);
                Assert.That(waited,
                    Is.GreaterThanOrEqualTo(rig.Presenter.RevealFadeSeconds));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator CaptureHudKeepsCompletionOverlayHiddenUntilGateIsReady()
        {
            var rig = new IsolatedRig(1);
            try
            {
                var overlayObject = new GameObject(
                    "CompletionGateOverlay",
                    typeof(RectTransform),
                    typeof(CanvasGroup));
                overlayObject.transform.SetParent(rig.Root, false);
                CanvasGroup overlayGroup =
                    overlayObject.GetComponent<CanvasGroup>();
                var hudObject = new GameObject("CaptureHudGate");
                hudObject.transform.SetParent(rig.Root, false);
                CaptureHudPresenter hud =
                    hudObject.AddComponent<CaptureHudPresenter>();
                hud.Configure(
                    rig.Controller,
                    null,
                    null,
                    overlayObject,
                    overlayGroup,
                    null,
                    null);
                hud.ConfigureCompletionRevealGateForSetup(rig.Presenter);

                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();
                hud.RefreshNow();

                Assert.That(rig.Controller.Session.LevelStatus,
                    Is.EqualTo(CaptureLevelStatus.Completed));
                Assert.That(overlayGroup.alpha, Is.Zero);
                Assert.That(overlayGroup.blocksRaycasts, Is.False);

                float waited = 0f;
                while (!rig.Presenter.CompletionPresentationReady
                    && waited < 2f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                hud.AdvancePercentageAnimation(
                    hud.CompletionOverlayFadeSeconds * 0.5f);
                Assert.That(overlayGroup.alpha,
                    Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(overlayGroup.interactable, Is.False);
                Assert.That(overlayGroup.blocksRaycasts, Is.True);

                hud.AdvancePercentageAnimation(
                    hud.CompletionOverlayFadeSeconds * 0.5f);
                Assert.That(overlayGroup.alpha, Is.EqualTo(1f));
                Assert.That(overlayGroup.interactable, Is.True);
                Assert.That(overlayGroup.blocksRaycasts, Is.True);
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

        [UnityTest]
        public IEnumerator CompletionSummaryHoldsOnCleanBoardThenPopupOpensAndRestoresVisibility()
        {
            var rig = new IsolatedRig(
                1,
                completionSummarySeconds: 0.4f);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                float waited = 0f;
                while (!rig.Presenter.CompletionSummaryInProgress
                    && waited < 1f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                Assert.That(rig.Presenter.CompletionSummaryInProgress, Is.True);
                Assert.That(rig.Presenter.CompletionPresentationReady, Is.False);
                Assert.That(rig.Presenter.ActiveGrainCount, Is.GreaterThan(0));
                Assert.That(
                    rig.Presenter.GrainFlightRoot.gameObject.activeSelf,
                    Is.False);
                Assert.That(rig.ThreatPresenter.Visible, Is.False);
                Assert.That(
                    rig.CaptureBoardPresenter.CompletedBarriersVisible,
                    Is.False);
                Assert.That(rig.FeedbackPresenter.CompletionSummaryVisible,
                    Is.True);
                Assert.That(rig.FeedbackPresenter.CueLabel.text,
                    Does.Contain("<color=#F4C15D>LEVEL 1 COMPLETE</color>"));
                Assert.That(rig.FeedbackPresenter.CueLabel.text,
                    Does.Contain("CAPTURED"));
                Assert.That(rig.FeedbackPresenter.CueLabel.text,
                    Does.Contain("CUT"));
                Assert.That(rig.FeedbackPresenter.CueLabel.text,
                    Does.Contain("TIME"));
                Assert.That(rig.FeedbackPresenter.CueLabel.fontSize,
                    Is.EqualTo(54));
                Assert.That(rig.FeedbackPresenter.CueLabel.resizeTextMaxSize,
                    Is.EqualTo(54));
                Assert.That(rig.FeedbackPresenter.CueCanvasGroup.alpha,
                    Is.Zero.Within(0.001f));
                Assert.That(
                    rig.FeedbackPresenter.CompletionSummaryBackground,
                    Is.Not.Null);
                Assert.That(
                    rig.FeedbackPresenter.CompletionSummaryBackground
                        .gameObject.activeSelf,
                    Is.True);
                Assert.That(
                    rig.FeedbackPresenter.CompletionSummaryBackground
                        .raycastTarget,
                    Is.False);
                Assert.That(
                    rig.FeedbackPresenter.CompletionSummaryBackground
                        .rectTransform.rect.width,
                    Is.GreaterThan(
                        rig.FeedbackPresenter.CueLabel.rectTransform
                            .rect.width));
                Assert.That(
                    rig.FeedbackPresenter.CompletionSummaryBackground
                        .rectTransform.rect.height,
                    Is.GreaterThan(
                        rig.FeedbackPresenter.CueLabel.rectTransform
                            .rect.height));
                Assert.That(rig.Presenter.StatsCanvasGroup.alpha, Is.Zero);
                Assert.That(
                    rig.Root.Find("CompletionArtworkTransition"),
                    Is.Null,
                    "No moving artwork duplicate should be created.");

                yield return null;
                rig.Presenter.RefreshNow();
                Assert.That(rig.FeedbackPresenter.CueCanvasGroup.alpha,
                    Is.GreaterThan(0f));

                waited = 0f;
                while (!rig.Presenter.CompletionPresentationReady
                    && waited < 1f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                Assert.That(rig.Presenter.CompletionSummaryFinished, Is.True);
                Assert.That(rig.Presenter.CompletionPresentationReady, Is.True);
                Assert.That(rig.FeedbackPresenter.CompletionSummaryVisible,
                    Is.False);
                Assert.That(
                    rig.FeedbackPresenter.CompletionSummaryBackground
                        .gameObject.activeSelf,
                    Is.False);
                Assert.That(rig.Presenter.StatsCanvasGroup.alpha, Is.Zero);

                // Simulate the next pre-level intro taking ownership before
                // completion resets. Removing completion's visibility reason
                // must not override the intro's independent hidden state.
                rig.ThreatPresenter.SetVisible(false);
                rig.Controller.RetryLevel();
                rig.Presenter.RefreshNow();
                Assert.That(rig.ThreatPresenter.Visible, Is.False);
                Assert.That(
                    rig.CaptureBoardPresenter.CompletedBarriersVisible,
                    Is.True);
                Assert.That(
                    rig.Presenter.GrainFlightRoot.gameObject.activeSelf,
                    Is.True);
                Assert.That(rig.Presenter.CompletionPresentationReady,
                    Is.False);

                rig.ThreatPresenter.SetVisible(true);
                Assert.That(rig.ThreatPresenter.Visible, Is.True);
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
                float? completionSummarySeconds = null,
                IReadOnlyList<CoreFunLevelDefinition> explicitLevels = null)
            {
                _simulationObject = new GameObject(
                    "LandmarkRevealTestRig",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                ((RectTransform)_simulationObject.transform).sizeDelta =
                    new Vector2(1080f, 1920f);
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
                CanvasGroup scrimGroup = CreateGroup(
                    _simulationObject.transform);
                var completionArtworkObject = new GameObject(
                    "CompletionArtwork",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                completionArtworkObject.transform.SetParent(
                    scrimGroup.transform,
                    false);
                var completionArtworkRect =
                    (RectTransform)completionArtworkObject.transform;
                completionArtworkRect.anchorMin = Vector2.zero;
                completionArtworkRect.anchorMax = Vector2.one;
                completionArtworkRect.offsetMin = Vector2.zero;
                completionArtworkRect.offsetMax = Vector2.zero;
                Image completionArtworkImage =
                    completionArtworkObject.GetComponent<Image>();

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

                var threatVisualObject = new GameObject(
                    "ThreatVisual",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                threatVisualObject.transform.SetParent(frame, false);
                var threatPresenterObject = new GameObject("ThreatPresenter");
                threatPresenterObject.transform.SetParent(
                    _simulationObject.transform,
                    false);
                ThreatPresenter = threatPresenterObject
                    .AddComponent<ThreatPresenter>();
                ThreatPresenter.Configure(
                    Controller,
                    frame,
                    (RectTransform)threatVisualObject.transform,
                    threatVisualObject.GetComponent<Image>(),
                    null,
                    0.9f);

                var capturedRootObject = new GameObject(
                    "CapturedRoot",
                    typeof(RectTransform));
                capturedRootObject.transform.SetParent(frame, false);
                var completedBarrierRootObject = new GameObject(
                    "CompletedBarrierRoot",
                    typeof(RectTransform));
                completedBarrierRootObject.transform.SetParent(frame, false);
                var capturePresenterObject = new GameObject(
                    "CaptureBoardPresenter");
                capturePresenterObject.transform.SetParent(
                    _simulationObject.transform,
                    false);
                CaptureBoardPresenter = capturePresenterObject
                    .AddComponent<CaptureBoardPresenter>();
                CaptureBoardPresenter.Configure(
                    Controller,
                    frame,
                    (RectTransform)capturedRootObject.transform,
                    (RectTransform)completedBarrierRootObject.transform,
                    0.22f);

                var feedbackCueObject = new GameObject(
                    "CompletionFeedbackCue",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text),
                    typeof(CanvasGroup));
                feedbackCueObject.transform.SetParent(
                    _simulationObject.transform,
                    false);
                var feedbackPresenterObject = new GameObject(
                    "FeedbackPresenter");
                feedbackPresenterObject.transform.SetParent(
                    _simulationObject.transform,
                    false);
                FeedbackPresenter = feedbackPresenterObject
                    .AddComponent<FeedbackPresenter>();
                FeedbackPresenter.Configure(
                    Controller,
                    null,
                    feedbackCueObject.GetComponent<Text>(),
                    feedbackCueObject.GetComponent<CanvasGroup>(),
                    null);

                Presenter.ConfigureCompletionRewardFlowForSetup(
                    ThreatPresenter,
                    CaptureBoardPresenter,
                    FeedbackPresenter,
                    completionSummarySeconds ?? 0.12f);

                _simulationObject.SetActive(true);
            }

            public FirstPlayableController Controller { get; }
            public LandmarkRevealPresenter Presenter { get; }
            public LandmarkDefinition[] Landmarks { get; }
            public RectTransform SandDestination { get; }
            public ThreatPresenter ThreatPresenter { get; }
            public CaptureBoardPresenter CaptureBoardPresenter { get; }
            public FeedbackPresenter FeedbackPresenter { get; }
            public Transform Root => _simulationObject.transform;

            public void SetCompletionLayoutSize(float width, float height)
            {
                ((RectTransform)_simulationObject.transform).sizeDelta =
                    new Vector2(width, height);
                Presenter.RefreshCompletionLayoutNow();
            }

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
