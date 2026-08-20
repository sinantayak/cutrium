using System.Collections;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Threats;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class PreLevelIntroPlayModeTests
    {
        private const float PastThreshold = 999f;

        [UnityTest]
        public IEnumerator LevelEight_ShowsCutAndInstantAndRoutesOnlyHudValues()
        {
            var root = new GameObject("PreLevelIntroTestRoot");
            root.SetActive(false);
            FirstPlayableController controller =
                root.AddComponent<FirstPlayableController>();
            controller.ConfigureLevelsForSetup(
                new[] { FirstTwelveGameplayProgression.CreateDefinitions()[7] });

            PreLevelIntroPresenter presenter =
                root.AddComponent<PreLevelIntroPresenter>();
            ThreatPresenter threatPresenter = CreateThreatPresenter(
                root.transform,
                controller);
            RectTransform flightRoot = Rect(root.transform, "FlightRoot");
            CanvasGroup levelGroup = Group(flightRoot, "LevelGroup");
            TMP_Text levelText = TmpText(levelGroup.transform, "Text");
            CanvasGroup targetGroup = Group(flightRoot, "TargetGroup");
            TMP_Text targetText = TmpText(targetGroup.transform, "Text");
            CanvasGroup infoGroup = Group(flightRoot, "InfoGroup");
            TMP_Text infoTitleText = TmpText(infoGroup.transform, "Title");
            TMP_Text infoMessageText = TmpText(infoGroup.transform, "Message");
            RectTransform progressDestination =
                Rect(root.transform, "ProgressDestination");
            progressDestination.anchoredPosition = new Vector2(0f, -300f);
            RectTransform cutDestination =
                Rect(root.transform, "CutDestination");
            cutDestination.anchoredPosition = new Vector2(0f, 300f);

            presenter.Configure(
                controller,
                threatPresenter,
                levelGroup,
                levelText,
                targetGroup,
                targetText,
                infoGroup,
                infoTitleText,
                infoMessageText,
                flightRoot,
                progressDestination,
                cutDestination);

            root.SetActive(true);
            yield return null;

            presenter.RefreshNow(0f);
            Assert.That(threatPresenter.Visible, Is.False);
            Assert.That(controller.SimulationHeld, Is.True);
            Assert.That(levelText.text, Is.EqualTo("LEVEL 8"));

            presenter.RefreshNow(PastThreshold);
            Assert.That(targetText.text, Is.EqualTo("TARGET 84%"));

            presenter.RefreshNow(PastThreshold);
            presenter.RefreshNow(0.3f);
            Assert.That(
                ((RectTransform)targetGroup.transform).anchoredPosition.y,
                Is.LessThan(0f));

            presenter.RefreshNow(PastThreshold);
            Assert.That(infoTitleText.text, Is.EqualTo("9 CUTS"));
            Assert.That(infoMessageText.text, Is.EqualTo("MAKE THEM COUNT"));

            presenter.RefreshNow(PastThreshold);
            presenter.RefreshNow(0.3f);
            Assert.That(
                ((RectTransform)infoGroup.transform).anchoredPosition.y,
                Is.GreaterThan(0f));

            presenter.RefreshNow(PastThreshold);
            Assert.That(infoTitleText.text, Is.EqualTo("INSTANT"));
            Assert.That(
                infoMessageText.text,
                Is.EqualTo("SAVE IT FOR A RISKY CUT"));
            Assert.That(
                ((RectTransform)infoGroup.transform).anchoredPosition,
                Is.EqualTo(Vector2.zero));

            presenter.RefreshNow(0.1f);
            Assert.That(
                ((RectTransform)infoGroup.transform).anchoredPosition,
                Is.EqualTo(Vector2.zero));
            Assert.That(infoGroup.alpha, Is.GreaterThan(0f));

            presenter.RefreshNow(PastThreshold);
            Assert.That(threatPresenter.Visible, Is.True);
            Assert.That(controller.SimulationHeld, Is.False);
            Assert.That(presenter.IsPlaying, Is.False);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Retry_SkipsSequenceAndRevealsThreatsImmediately()
        {
            var root = new GameObject("PreLevelIntroRetryTestRoot");
            root.SetActive(false);
            FirstPlayableController controller =
                root.AddComponent<FirstPlayableController>();
            controller.ConfigureLevelsForSetup(new[] { Definition() });

            PreLevelIntroPresenter presenter =
                root.AddComponent<PreLevelIntroPresenter>();
            ThreatPresenter threatPresenter = CreateThreatPresenter(
                root.transform,
                controller);
            RectTransform flightRoot = Rect(root.transform, "FlightRoot");
            CanvasGroup levelGroup = Group(flightRoot, "LevelGroup");
            TMP_Text levelText = TmpText(levelGroup.transform, "Text");
            CanvasGroup targetGroup = Group(flightRoot, "TargetGroup");
            TMP_Text targetText = TmpText(targetGroup.transform, "Text");
            CanvasGroup infoGroup = Group(flightRoot, "InfoGroup");
            TMP_Text infoTitleText = TmpText(infoGroup.transform, "Title");
            TMP_Text infoMessageText = TmpText(infoGroup.transform, "Message");
            RectTransform progressDestination =
                Rect(root.transform, "ProgressDestination");
            RectTransform cutDestination =
                Rect(root.transform, "CutDestination");

            presenter.Configure(
                controller,
                threatPresenter,
                levelGroup,
                levelText,
                targetGroup,
                targetText,
                infoGroup,
                infoTitleText,
                infoMessageText,
                flightRoot,
                progressDestination,
                cutDestination);

            root.SetActive(true);
            yield return null;

            presenter.RefreshNow(0f);
            Assert.That(controller.SimulationHeld, Is.True);
            Assert.That(threatPresenter.Visible, Is.False);

            controller.RetryLevel();
            presenter.RefreshNow(0f);

            Assert.That(controller.SimulationHeld, Is.False);
            Assert.That(threatPresenter.Visible, Is.True);
            Assert.That(presenter.IsPlaying, Is.False);
            Assert.That(levelGroup.alpha, Is.Zero);
            Object.Destroy(root);
        }

        private static CoreFunLevelDefinition Definition() =>
            new CoreFunLevelDefinition(
                "pre-level-intro-test",
                1,
                new[]
                {
                    new CoreFunThreatDefinition(
                        new Vector2(5f, 8f),
                        new Vector2(0f, 1f),
                        2f,
                        0.35f,
                        8),
                },
                0.99f,
                8f,
                0.08f,
                0.6f,
                16,
                8,
                "Test",
                20f,
                "Test",
                null,
                "Test",
                1,
                1,
                1,
                "1 CUT",
                "MAKE IT COUNT");

        private static ThreatPresenter CreateThreatPresenter(
            Transform parent,
            FirstPlayableController controller)
        {
            RectTransform boardFrame = Rect(parent, "BoardFrame");
            RectTransform threatVisual = Rect(boardFrame, "ThreatVisual");
            Image threatImage = threatVisual.gameObject.AddComponent<Image>();
            var presenterObject = new GameObject("ThreatPresenter");
            presenterObject.transform.SetParent(parent, false);
            ThreatPresenter presenter =
                presenterObject.AddComponent<ThreatPresenter>();
            presenter.Configure(
                controller,
                boardFrame,
                threatVisual,
                threatImage,
                null,
                0.9f);
            return presenter;
        }

        private static RectTransform Rect(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        private static TMP_Text TmpText(Transform parent, string name)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<TextMeshProUGUI>();
        }

        private static CanvasGroup Group(Transform parent, string name)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasGroup));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<CanvasGroup>();
        }
    }
}
