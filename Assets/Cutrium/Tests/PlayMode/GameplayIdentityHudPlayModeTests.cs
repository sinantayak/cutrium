using System.Collections;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class GameplayIdentityHudPlayModeTests
    {
        [UnityTest]
        public IEnumerator LimitedLevel_ShowsCounterAndRetryResetsFailure()
        {
            var root = new GameObject("IdentityHudTestRoot");
            root.SetActive(false);
            FirstPlayableController controller =
                root.AddComponent<FirstPlayableController>();
            controller.ConfigureLevelsForSetup(new[]
            {
                LimitedDefinition(),
            });
            GameplayIdentityHudPresenter presenter =
                root.AddComponent<GameplayIdentityHudPresenter>();
            TMP_Text counter = TmpText(root.transform, "Counter");
            TMP_Text speed = TmpText(root.transform, "Speed");
            CanvasGroup failure = Group(root.transform, "Failure");
            Text failureText = Text(failure.transform, "Text");
            Button retry = Button(failure.transform, "Retry");
            presenter.Configure(
                controller,
                counter,
                speed,
                null,
                null,
                failure,
                failureText,
                retry);
            root.SetActive(true);
            yield return null;

            presenter.RefreshNow(0f);
            Assert.That(counter.gameObject.activeSelf, Is.True);
            Assert.That(counter.text, Is.EqualTo("CUT: 0/1"));
            Assert.That(speed.text, Is.EqualTo("8.0"));
            LayoutElement failureLayout =
                failure.GetComponent<LayoutElement>();
            Assert.That(failureLayout, Is.Not.Null);
            Assert.That(failureLayout.ignoreLayout, Is.True);
            var failureRect = (RectTransform)failure.transform;
            Assert.That(failureRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(failureRect.anchorMax, Is.EqualTo(Vector2.one));

            controller.SubmitBarrierIntent(new BarrierIntent(
                new LogicalPoint(5f, 8f),
                BarrierOrientation.Vertical));
            controller.AdvanceSimulation(1f / 60f);
            presenter.RefreshNow(0f);
            Assert.That(controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.OutOfCuts));
            Assert.That(counter.text, Is.EqualTo("CUT: 1/1"));
            Assert.That(failure.alpha, Is.EqualTo(1f));
            Assert.That(failure.blocksRaycasts, Is.True);

            retry.onClick.Invoke();
            presenter.RefreshNow(0f);
            Assert.That(controller.Session.LevelStatus,
                Is.EqualTo(CaptureLevelStatus.Playing));
            Assert.That(counter.text, Is.EqualTo("CUT: 0/1"));
            Assert.That(failure.alpha, Is.Zero);
            Assert.That(failure.blocksRaycasts, Is.False);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator UnlimitedLevel_HidesCutCounter()
        {
            var root = new GameObject("UnlimitedIdentityHudTestRoot");
            root.SetActive(false);
            FirstPlayableController controller =
                root.AddComponent<FirstPlayableController>();
            controller.ConfigureLevelsForSetup(new[]
            {
                UnlimitedDefinition(),
            });
            GameplayIdentityHudPresenter presenter =
                root.AddComponent<GameplayIdentityHudPresenter>();
            TMP_Text counter = TmpText(root.transform, "Counter");
            TMP_Text speed = TmpText(root.transform, "Speed");
            CanvasGroup failure = Group(root.transform, "Failure");
            presenter.Configure(
                controller,
                counter,
                speed,
                null,
                null,
                failure,
                Text(failure.transform, "Text"),
                Button(failure.transform, "Retry"));
            root.SetActive(true);
            yield return null;

            presenter.RefreshNow(0f);
            Assert.That(counter.gameObject.activeSelf, Is.False);
            Assert.That(speed.text, Is.EqualTo("3.0"));
            Object.Destroy(root);
        }

        private static CoreFunLevelDefinition LimitedDefinition() =>
            new CoreFunLevelDefinition(
                "limited-test",
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

        private static CoreFunLevelDefinition UnlimitedDefinition() =>
            new CoreFunLevelDefinition(
                "unlimited-test",
                1,
                new[]
                {
                    new CoreFunThreatDefinition(
                        new Vector2(5f, 8f),
                        new Vector2(1f, 0f),
                        2f,
                        0.35f,
                        8),
                },
                0.75f,
                3f,
                0.08f,
                0.6f,
                16,
                8,
                "Test",
                20f,
                "Test",
                null,
                "Test");

        private static Text Text(Transform parent, string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Text>();
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

        private static Button Button(Transform parent, string name)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Button>();
        }
    }
}
