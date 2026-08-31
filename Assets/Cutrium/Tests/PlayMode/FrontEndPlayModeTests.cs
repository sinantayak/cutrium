using System.Collections;
using System.Linq;
using Cutrium.Presentation.Frontend;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class FrontEndPlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        [UnityTest]
        public IEnumerator ConfiguredSceneStartsOnHomeWithCatalogBackedMap()
        {
            yield return SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
            yield return null;

            GameObject root = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            FrontEndPresenter presenter = root
                .GetComponentInChildren<FrontEndPresenter>(true);
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);

            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.FrontEndVisible, Is.True);
            Assert.That(presenter.ActiveTab, Is.EqualTo(FrontEndTab.Home));
            Assert.That(presenter.FrontEndCanvasGroup.blocksRaycasts, Is.True);
            Assert.That(presenter.LevelNodes.Count,
                Is.EqualTo(controller.LevelDefinitions.Count));
            Assert.That(presenter.LevelNodes.Count, Is.EqualTo(24));
            Assert.That(presenter.PathConnectors.Count, Is.EqualTo(23));
            Assert.That(
                presenter.LevelNodes.Select(node => node.LevelNumber),
                Is.EqualTo(Enumerable.Range(1, 24)));
            Assert.That(
                presenter.LevelNodes.All(
                    node => node.NodeImage.sprite != null),
                Is.True);
            Assert.That(
                presenter.LevelNodes.All(
                    node => node.NodeImage.color.a == 1f),
                Is.True);

            RectTransform frontEndRoot =
                (RectTransform)presenter.FrontEndCanvasGroup.transform;
            Assert.That(presenter.BackgroundArtwork, Is.Not.Null);
            Assert.That(presenter.BackgroundArtwork.sprite, Is.Not.Null);
            Assert.That(
                presenter.BackgroundArtwork.sprite.texture.name,
                Is.EqualTo("HomeBackground"));
            Assert.That(presenter.HomeLogo, Is.Not.Null);
            Assert.That(presenter.HomeLogo.sprite, Is.Not.Null);
            Assert.That(
                presenter.HomeLogo.sprite.texture.name,
                Is.EqualTo("CutriumAmblem"));
            Image mapBackground = presenter.ChallengeScrollRect
                .GetComponent<Image>();
            Assert.That(mapBackground.color.a, Is.Zero);
            Assert.That(presenter.ChallengePage.transform.parent,
                Is.EqualTo(frontEndRoot));
            RectTransform mapRect =
                (RectTransform)presenter.ChallengeScrollRect.transform;
            Assert.That(mapRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(mapRect.anchorMax, Is.EqualTo(Vector2.one));
            Transform challengeHeader = presenter.ChallengePage.transform
                .Find("ChallengeHeader");
            Assert.That(
                challengeHeader == null || !challengeHeader.gameObject.activeSelf,
                Is.True);

            Transform safeAreaContent = frontEndRoot.Find("SafeAreaContent");
            Image homeTabBackground = safeAreaContent
                .Find("BottomNavigation/HomeTab/ActivePlate")
                .GetComponent<Image>();
            Assert.That(homeTabBackground.sprite, Is.Null);
            RectTransform homeTabPlate =
                (RectTransform)homeTabBackground.transform;
            Assert.That(homeTabPlate.offsetMax.y, Is.EqualTo(30f));
            Assert.That(
                homeTabPlate.Find("RoundedFill")
                    .gameObject.activeSelf,
                Is.True);

            RectTransform navigationUnderlay = frontEndRoot
                .Find("BottomNavigationUnderlay") as RectTransform;
            Assert.That(navigationUnderlay, Is.Not.Null);
            Assert.That(navigationUnderlay.anchorMin.y, Is.Zero);
            Assert.That(navigationUnderlay.anchoredPosition.y, Is.Zero);
            Assert.That(
                navigationUnderlay
                    .GetComponent<FrontEndRoundedRectangleGraphic>(),
                Is.Not.Null);

            RectTransform homePlay =
                (RectTransform)presenter.HomePlayButton.transform;
            RectTransform challengePlay =
                (RectTransform)presenter.ChallengePlayButton.transform;
            Assert.That(homePlay.sizeDelta, Is.EqualTo(new Vector2(420f, 172f)));
            Assert.That(
                challengePlay.sizeDelta,
                Is.EqualTo(new Vector2(420f, 172f)));
            Assert.That(
                presenter.HomePlayButton.transform.Find("Label")
                    .GetComponent<FrontEndPulseAnimator>(),
                Is.Not.Null);
            Assert.That(
                presenter.ChallengePlayButton.transform.Find("Label")
                    .GetComponent<FrontEndPulseAnimator>(),
                Is.Not.Null);
            FrontEndPulseAnimator oldChallengeButtonPulse =
                presenter.ChallengePlayButton
                    .GetComponent<FrontEndPulseAnimator>();
            Assert.That(
                oldChallengeButtonPulse == null
                    || !oldChallengeButtonPulse.enabled,
                Is.True);
            Assert.That(
                homePlay.Find("Label").GetComponent<TMP_Text>().fontSizeMax,
                Is.EqualTo(56f));
            Assert.That(
                challengePlay.Find("Label")
                    .GetComponent<TMP_Text>().fontSizeMax,
                Is.EqualTo(46f));
            Assert.That(
                presenter.LevelNodes.All(
                    node => node.NumberLabel.fontSizeMax == 58f),
                Is.True);
            Assert.That(
                presenter.LevelNodes.All(node =>
                    ((RectTransform)node.NumberLabel.transform)
                        .anchoredPosition == Vector2.zero),
                Is.True);

            presenter.ShowTab(FrontEndTab.Challenge);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(
                presenter.ChallengeScrollRect.verticalNormalizedPosition,
                Is.Zero.Within(0.001f));
            Assert.That(
                ((RectTransform)presenter.LevelNodes[0].transform)
                    .anchoredPosition.y,
                Is.EqualTo(118f));
            Assert.That(
                ((RectTransform)presenter.LevelNodes[0].transform)
                    .anchoredPosition.y,
                Is.LessThan(
                    ((RectTransform)presenter.LevelNodes[1].transform)
                        .anchoredPosition.y));
            Assert.That(
                presenter.LevelNodes[0].SelectionGlow.gameObject.activeSelf,
                Is.True);
            FrontEndPulseAnimator selectedGlowPulse =
                presenter.LevelNodes[0].SelectionGlow
                    .GetComponent<FrontEndPulseAnimator>();
            Assert.That(
                selectedGlowPulse == null || !selectedGlowPulse.enabled,
                Is.True);
            Assert.That(
                presenter.LevelNodes[0].NumberLabel.gameObject.activeSelf,
                Is.True);
            Assert.That(
                presenter.LevelNodes[0].LockImage.gameObject.activeSelf,
                Is.False);
            Assert.That(
                presenter.LevelNodes[1].LockImage, Is.Not.Null);
            Assert.That(
                presenter.LevelNodes[1].LockImage.sprite, Is.Not.Null);
            Assert.That(
                presenter.LevelNodes[1].LockImage.gameObject.activeSelf,
                Is.True);
            Assert.That(
                presenter.LevelNodes[1].NumberLabel.gameObject.activeSelf,
                Is.False);

            var mapCorners = new Vector3[4];
            var playCorners = new Vector3[4];
            mapRect.GetWorldCorners(mapCorners);
            challengePlay.GetWorldCorners(playCorners);
            Assert.That(
                mapCorners[0].y,
                Is.GreaterThanOrEqualTo(playCorners[1].y + 50f));
            float stableMapBottom = mapRect.offsetMin.y;
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(mapRect.offsetMin.y,
                Is.EqualTo(stableMapBottom).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator HomeOwnsItsHoldUntilPlayWithoutReleasingIntroHold()
        {
            var rig = new FrontEndRig();
            try
            {
                rig.Activate();
                yield return null;

                Assert.That(rig.Presenter.FrontEndVisible, Is.True);
                Assert.That(rig.Presenter.ActiveTab, Is.EqualTo(FrontEndTab.Home));
                Assert.That(rig.HomePage.alpha, Is.EqualTo(1f));
                Assert.That(rig.ShopPage.alpha, Is.Zero);
                Assert.That(rig.ChallengePage.alpha, Is.Zero);
                Assert.That(
                    rig.Controller.HasSimulationHold(
                        SimulationHoldReason.FrontEnd),
                    Is.True);

                rig.Controller.SetSimulationHold(
                    SimulationHoldReason.PreLevelIntro,
                    true);
                rig.HomePlayButton.onClick.Invoke();

                Assert.That(rig.Presenter.FrontEndVisible, Is.False);
                Assert.That(rig.Controller.CurrentLevelIndex, Is.Zero);
                Assert.That(
                    rig.Controller.HasSimulationHold(
                        SimulationHoldReason.FrontEnd),
                    Is.False);
                Assert.That(
                    rig.Controller.HasSimulationHold(
                        SimulationHoldReason.PreLevelIntro),
                    Is.True);
                Assert.That(rig.Controller.SimulationHeld, Is.True);
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ChallengeNodeSelectsAndStartsMatchingCatalogLevel()
        {
            var rig = new FrontEndRig();
            try
            {
                rig.Activate();
                yield return null;

                // Simulate progress already reaching level 2 (but not 3),
                // so selecting/playing level 2 exercises the normal,
                // already-unlocked path.
                Assert.That(
                    rig.Controller.TryJumpToLevelForDevelopment(2),
                    Is.True);

                rig.ChallengeTabButton.onClick.Invoke();
                Assert.That(
                    rig.Presenter.ActiveTab,
                    Is.EqualTo(FrontEndTab.Challenge));
                Assert.That(rig.ChallengePage.alpha, Is.EqualTo(1f));

                rig.Nodes[1].Button.onClick.Invoke();
                Assert.That(rig.Presenter.SelectedLevelNumber, Is.EqualTo(2));
                Assert.That(
                    rig.Nodes[1].State,
                    Is.EqualTo(FrontEndLevelNodeState.Selected));
                Assert.That(rig.ChallengePlayLabel.text, Is.EqualTo("PLAY LEVEL 2"));

                rig.ChallengePlayButton.onClick.Invoke();
                Assert.That(rig.Controller.CurrentLevelIndex, Is.EqualTo(1));
                Assert.That(rig.Controller.CurrentLevelNumber, Is.EqualTo(2));
                Assert.That(rig.Presenter.FrontEndVisible, Is.False);
                Assert.That(
                    rig.Controller.HasSimulationHold(
                        SimulationHoldReason.FrontEnd),
                    Is.False);
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ChallengeNode_LockedLevelCannotBeSelectedOrPlayed()
        {
            var rig = new FrontEndRig();
            try
            {
                rig.Activate();
                yield return null;

                rig.ChallengeTabButton.onClick.Invoke();
                Assert.That(rig.Controller.CurrentLevelIndex, Is.Zero);
                Assert.That(rig.Nodes[2].Button.interactable, Is.False);

                rig.Nodes[2].Button.onClick.Invoke();
                Assert.That(
                    rig.Presenter.SelectedLevelNumber,
                    Is.Not.EqualTo(3),
                    "Clicking a locked node must not select it.");
                Assert.That(
                    rig.Nodes[2].State,
                    Is.EqualTo(FrontEndLevelNodeState.Locked));
                Assert.That(
                    rig.ChallengePlayLabel.text,
                    Is.Not.EqualTo("PLAY LEVEL 3"));

                // Even a direct attempt to start it is refused by the
                // controller itself — the authoritative gate, independent
                // of any UI state.
                Assert.That(rig.Controller.TryStartLevel(3), Is.False);
                Assert.That(rig.Controller.CurrentLevelIndex, Is.Zero);
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void IndependentSimulationHoldReasonsCompose()
        {
            var rig = new FrontEndRig();
            try
            {
                rig.Controller.SetSimulationHold(
                    SimulationHoldReason.FrontEnd,
                    true);
                rig.Controller.SetSimulationHold(
                    SimulationHoldReason.PreLevelIntro,
                    true);
                rig.Controller.SetSimulationHold(
                    SimulationHoldReason.FrontEnd,
                    false);

                Assert.That(rig.Controller.SimulationHeld, Is.True);
                Assert.That(
                    rig.Controller.ActiveSimulationHolds,
                    Is.EqualTo(SimulationHoldReason.PreLevelIntro));

                rig.Controller.SetSimulationHold(
                    SimulationHoldReason.PreLevelIntro,
                    false);
                Assert.That(rig.Controller.SimulationHeld, Is.False);
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator AttentionPulseContinuesWhileGameplayTimeIsPaused()
        {
            var pulseObject = new GameObject(
                "FrontEndPulseTest",
                typeof(RectTransform));
            pulseObject.SetActive(false);
            RectTransform target = (RectTransform)pulseObject.transform;
            CanvasGroup canvasGroup = pulseObject.AddComponent<CanvasGroup>();
            FrontEndPulseAnimator pulse =
                pulseObject.AddComponent<FrontEndPulseAnimator>();
            pulse.ConfigureForSetup(
                target,
                canvasGroup,
                0.8f,
                0.1f,
                0.4f,
                1f);

            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                pulseObject.SetActive(true);
                float initialScale = target.localScale.x;
                yield return new WaitForSecondsRealtime(0.25f);

                Assert.That(target.localScale.x,
                    Is.GreaterThan(initialScale + 0.01f));
                Assert.That(canvasGroup.alpha, Is.GreaterThan(0.4f));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
                Object.DestroyImmediate(pulseObject);
            }
        }

        private sealed class FrontEndRig
        {
            private readonly GameObject _root;

            public FrontEndRig()
            {
                _root = new GameObject("FrontEndTestRig");
                _root.SetActive(false);

                GameObject controllerObject = CreateChild("Controller");
                Controller = controllerObject.AddComponent<FirstPlayableController>();
                Controller.ConfigureLevelsForSetup(
                    CoreFunLevelDefinition.CreateMilestone3Defaults());

                CanvasGroup frontEndGroup = CreateGroup("FrontEnd");
                ShopPage = CreateGroup("ShopPage");
                HomePage = CreateGroup("HomePage");
                ChallengePage = CreateGroup("ChallengePage");

                Button shopTab = CreateButton("ShopTab");
                Button homeTab = CreateButton("HomeTab");
                ChallengeTabButton = CreateButton("ChallengeTab");
                HomePlayButton = CreateButton("HomePlay");
                ChallengePlayButton = CreateButton("ChallengePlay");
                ChallengePlayLabel = CreateText("ChallengePlayLabel");

                var tabPlates = new Image[3];
                var tabIcons = new Image[3];
                var tabLabels = new TMP_Text[3];
                for (int index = 0; index < 3; index++)
                {
                    tabPlates[index] = CreateImage($"TabPlate{index}");
                    tabIcons[index] = CreateImage($"TabIcon{index}");
                    tabLabels[index] = CreateText($"TabLabel{index}");
                }

                ScrollRect scrollRect = CreateChild("ScrollRect")
                    .AddComponent<ScrollRect>();
                // ScrollRect.verticalNormalizedPosition's setter
                // (ScrollToSelectedLevel) dereferences `content` with no
                // null guard of its own, so an unset content throws a
                // NullReferenceException the moment the Challenge tab is
                // opened.
                scrollRect.content = CreateChild("ScrollContent")
                    .GetComponent<RectTransform>();
                Nodes = new FrontEndLevelNodeView[3];
                for (int index = 0; index < Nodes.Length; index++)
                {
                    GameObject nodeObject = CreateChild($"Node{index + 1}");
                    Button nodeButton = nodeObject.AddComponent<Button>();
                    Image nodeImage = nodeObject.AddComponent<Image>();
                    nodeButton.targetGraphic = nodeImage;
                    Image glow = CreateImage($"Glow{index + 1}");
                    TMP_Text number = CreateText($"Number{index + 1}");
                    Nodes[index] = nodeObject
                        .AddComponent<FrontEndLevelNodeView>();
                    Nodes[index].ConfigureForSetup(
                        index + 1,
                        nodeButton,
                        nodeImage,
                        glow,
                        number);
                }

                var connectors = new Image[2];
                connectors[0] = CreateImage("Connector1");
                connectors[1] = CreateImage("Connector2");

                GameObject presenterObject = CreateChild("Presenter");
                Presenter = presenterObject.AddComponent<FrontEndPresenter>();
                Presenter.ConfigureForSetup(
                    Controller,
                    null,
                    frontEndGroup,
                    ShopPage,
                    HomePage,
                    ChallengePage,
                    null,
                    null,
                    shopTab,
                    homeTab,
                    ChallengeTabButton,
                    tabPlates,
                    tabIcons,
                    tabLabels,
                    HomePlayButton,
                    ChallengePlayButton,
                    ChallengePlayLabel,
                    scrollRect,
                    Nodes,
                    connectors);
            }

            public FirstPlayableController Controller { get; }
            public FrontEndPresenter Presenter { get; }
            public CanvasGroup ShopPage { get; }
            public CanvasGroup HomePage { get; }
            public CanvasGroup ChallengePage { get; }
            public Button ChallengeTabButton { get; }
            public Button HomePlayButton { get; }
            public Button ChallengePlayButton { get; }
            public TMP_Text ChallengePlayLabel { get; }
            public FrontEndLevelNodeView[] Nodes { get; }

            public void Activate()
            {
                _root.SetActive(true);
            }

            public void Dispose()
            {
                Object.DestroyImmediate(_root);

                // TestModeDetector.IsRunningTests only recognizes the
                // documented `-batchmode -runTests` CLI invocation; a test
                // driven live (e.g. via an Editor MCP tool) does not set
                // that flag, so FirstPlayableController's progress calls
                // read/write the developer's real local PlayerPrefs instead
                // of no-op'ing. Any rig that advances CurrentLevelIndex
                // (TryStartLevel/TryAdvanceToNextLevel/RestartSequence/
                // TryJumpToLevelForDevelopment) must undo that on disposal
                // so it can't leak into another test or the developer's own
                // manual Play Mode session.
                PlayerPrefs.DeleteKey("Cutrium.Progress.CurrentLevelIndex");
            }

            private GameObject CreateChild(string name)
            {
                var gameObject = new GameObject(name, typeof(RectTransform));
                gameObject.transform.SetParent(_root.transform, false);
                return gameObject;
            }

            private CanvasGroup CreateGroup(string name) =>
                CreateChild(name).AddComponent<CanvasGroup>();

            private Image CreateImage(string name) =>
                CreateChild(name).AddComponent<Image>();

            private Button CreateButton(string name)
            {
                GameObject gameObject = CreateChild(name);
                Image image = gameObject.AddComponent<Image>();
                Button button = gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                return button;
            }

            private TMP_Text CreateText(string name) =>
                CreateChild(name).AddComponent<TextMeshProUGUI>();
        }
    }
}
