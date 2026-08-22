using System.Collections;
using System.Linq;
using Cutrium.Presentation.Feedback;
using Cutrium.Presentation.Frontend;
using Cutrium.Presentation.Settings;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class SettingsPanelPlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        [UnityTest]
        public IEnumerator Scene_HasResponsiveAssetBackedSettingsPanel()
        {
            yield return SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
            yield return null;

            GameObject root = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            SettingsPanelPresenter presenter = root
                .GetComponentInChildren<SettingsPanelPresenter>(true);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.Controller, Is.Not.Null);
            Assert.That(presenter.PanelCanvasGroup, Is.Not.Null);
            Assert.That(presenter.PanelCanvasGroup.alpha, Is.Zero);
            Assert.That(presenter.PanelCanvasGroup.interactable, Is.False);
            Assert.That(presenter.PanelCanvasGroup.blocksRaycasts, Is.False);
            Assert.That(presenter.OpenButton, Is.Not.Null);
            Assert.That(presenter.OpenButton.interactable, Is.True);

            Transform settingsRoot = root.transform.Find(
                "Canvas/SettingsPanelRoot");
            Assert.That(settingsRoot, Is.Not.Null);
            Assert.That(
                settingsRoot.GetSiblingIndex(),
                Is.EqualTo(settingsRoot.parent.childCount - 1));
            Assert.That(
                settingsRoot.Find("SafeAreaContent")
                    .GetComponent<Cutrium.Unity.Layout.SafeAreaFitter>(),
                Is.Not.Null);

            RectTransform bounds = (RectTransform)settingsRoot.Find(
                "SafeAreaContent/SettingsPanelBounds");
            Assert.That(bounds.anchorMin, Is.EqualTo(new Vector2(0.12f, 0.10f)));
            Assert.That(bounds.anchorMax, Is.EqualTo(new Vector2(0.88f, 0.90f)));
            Transform panel = bounds.Find("SettingsPanel");
            Assert.That(panel.GetComponent<Image>().sprite.name,
                Does.StartWith("GeneralPanelBackground"));
            Assert.That(
                panel.GetComponent<AspectRatioFitter>().aspectMode,
                Is.EqualTo(AspectRatioFitter.AspectMode.FitInParent));

            AssertIcon(panel, "SoundToggleBounds/SoundToggle/Icon", "SoundIcon");
            AssertIcon(panel, "MusicToggleBounds/MusicToggle/Icon", "MusicIcon");
            AssertIcon(panel, "HapticToggleBounds/HapticToggle/Icon", "HapticIcon");
            AssertIcon(
                panel,
                "CloseButtonBounds/CloseButton/Icon",
                "CloseIcon");
            Assert.That(
                ((RectTransform)panel.Find(
                    "SoundToggleBounds/SoundToggle/Icon")).anchorMax.x
                - ((RectTransform)panel.Find(
                    "SoundToggleBounds/SoundToggle/Icon")).anchorMin.x,
                Is.EqualTo(0.48f).Within(0.001f));
            Assert.That(
                ((RectTransform)panel.Find(
                    "CloseButtonBounds/CloseButton/Icon")).anchorMax.x
                - ((RectTransform)panel.Find(
                    "CloseButtonBounds/CloseButton/Icon")).anchorMin.x,
                Is.EqualTo(0.56f).Within(0.001f));
            AssertInspectorOffsets(
                panel,
                "SoundToggleBounds/SoundToggle/Icon");
            AssertInspectorOffsets(
                panel,
                "MusicToggleBounds/MusicToggle/Icon");
            AssertInspectorOffsets(
                panel,
                "HapticToggleBounds/HapticToggle/Icon");
            AssertInspectorOffsets(
                panel,
                "CloseButtonBounds/CloseButton/Icon");
            Assert.That(
                panel.Find("SoundToggleBounds/SoundToggle/StateLabel"),
                Is.Null);
            Assert.That(
                panel.Find("MusicToggleBounds/MusicToggle/StateLabel"),
                Is.Null);
            Assert.That(
                panel.Find("HapticToggleBounds/HapticToggle/StateLabel"),
                Is.Null);
            AssertAction(panel, "LanguageButton", "English");
            AssertAction(panel, "HomeButton", "Home");
            AssertAction(panel, "ExitButton", "Exit");
        }

        [Test]
        public void OpenCloseAndIndependentHoldOwnershipCompose()
        {
            using var rig = new SettingsRig();
            rig.ActivateWithoutFrontend();

            rig.OpenButton.onClick.Invoke();
            Assert.That(rig.Presenter.IsOpen, Is.True);
            Assert.That(rig.PanelGroup.alpha, Is.EqualTo(1f));
            Assert.That(rig.PanelGroup.interactable, Is.True);
            Assert.That(rig.PanelGroup.blocksRaycasts, Is.True);
            Assert.That(
                rig.Controller.HasSimulationHold(
                    SimulationHoldReason.Settings),
                Is.True);

            rig.Controller.SetSimulationHold(
                SimulationHoldReason.PreLevelIntro,
                true);
            rig.CloseButton.onClick.Invoke();
            Assert.That(rig.Presenter.IsOpen, Is.False);
            Assert.That(
                rig.Controller.HasSimulationHold(
                    SimulationHoldReason.Settings),
                Is.False);
            Assert.That(
                rig.Controller.HasSimulationHold(
                    SimulationHoldReason.PreLevelIntro),
                Is.True);
            Assert.That(rig.Controller.SimulationHeld, Is.True);
        }

        [Test]
        public void TogglesApplyToFeedbackMusicAndVisibleState()
        {
            using var rig = new SettingsRig();
            rig.ActivateWithoutFrontend();

            rig.SoundButton.onClick.Invoke();
            Assert.That(rig.Presenter.SoundEnabled, Is.False);
            Assert.That(rig.Audio.EffectsEnabled, Is.False);
            Assert.That(rig.SoundState.text, Is.EqualTo("OFF"));

            rig.MusicButton.onClick.Invoke();
            Assert.That(rig.Presenter.MusicEnabled, Is.False);
            Assert.That(rig.MusicSource.mute, Is.True);
            Assert.That(rig.MusicState.text, Is.EqualTo("OFF"));

            rig.HapticButton.onClick.Invoke();
            Assert.That(rig.Presenter.HapticEnabled, Is.False);
            Assert.That(rig.Haptics.HapticsEnabled, Is.False);
            Assert.That(rig.HapticState.text, Is.EqualTo("OFF"));
            int cueCount = rig.Haptics.HandledCueCount;
            rig.Haptics.PlayUi();
            Assert.That(rig.Haptics.HandledCueCount, Is.EqualTo(cueCount));

            rig.SoundButton.onClick.Invoke();
            rig.MusicButton.onClick.Invoke();
            rig.HapticButton.onClick.Invoke();
            Assert.That(rig.Audio.EffectsEnabled, Is.True);
            Assert.That(rig.MusicSource.mute, Is.False);
            Assert.That(rig.Haptics.HapticsEnabled, Is.True);
            Assert.That(rig.SoundState.text, Is.EqualTo("ON"));
            Assert.That(rig.MusicState.text, Is.EqualTo("ON"));
            Assert.That(rig.HapticState.text, Is.EqualTo("ON"));
        }

        [Test]
        public void HomeTransfersHoldToFrontendAndExitRaisesRequest()
        {
            using var rig = new SettingsRig();
            rig.ActivateWithoutFrontend();
            bool exitRequested = false;
            rig.Presenter.ExitRequested += () => exitRequested = true;

            rig.OpenButton.onClick.Invoke();
            rig.HomeButton.onClick.Invoke();
            Assert.That(rig.Presenter.IsOpen, Is.False);
            Assert.That(rig.FrontEnd.FrontEndVisible, Is.True);
            Assert.That(rig.FrontEnd.ActiveTab, Is.EqualTo(FrontEndTab.Home));
            Assert.That(
                rig.Controller.HasSimulationHold(
                    SimulationHoldReason.Settings),
                Is.False);
            Assert.That(
                rig.Controller.HasSimulationHold(
                    SimulationHoldReason.FrontEnd),
                Is.True);

            rig.ExitButton.onClick.Invoke();
            Assert.That(exitRequested, Is.True);
        }

        private static void AssertIcon(
            Transform panel,
            string path,
            string expectedSpriteName)
        {
            Image image = panel.Find(path).GetComponent<Image>();
            Assert.That(image, Is.Not.Null, path);
            Assert.That(image.sprite, Is.Not.Null, path);
            Assert.That(image.sprite.name, Does.StartWith(expectedSpriteName));
        }

        private static void AssertInspectorOffsets(
            Transform panel,
            string path)
        {
            RectTransform icon = (RectTransform)panel.Find(path);
            Assert.That(icon.offsetMin.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(-icon.offsetMax.y, Is.EqualTo(-10f).Within(0.001f));
        }

        private static void AssertAction(
            Transform panel,
            string path,
            string expectedLabel)
        {
            Transform action = panel.Find(path);
            Assert.That(action, Is.Not.Null, path);
            Assert.That(action.GetComponent<Button>(), Is.Not.Null, path);
            Assert.That(action.GetComponent<Image>().sprite.name,
                Does.StartWith("GeneralButtonBackground_2"));
            Assert.That(action.Find("Label").GetComponent<TMP_Text>().text,
                Is.EqualTo(expectedLabel));
        }

        private sealed class SettingsRig : System.IDisposable
        {
            private readonly GameObject _root;

            public SettingsRig()
            {
                _root = new GameObject("SettingsTestRig");
                _root.SetActive(false);

                Controller = CreateChild("Controller")
                    .AddComponent<FirstPlayableController>();
                Controller.ConfigureLevelsForSetup(
                    CoreFunLevelDefinition.CreateMilestone3Defaults());

                AudioSource effectSource = CreateChild("EffectSource")
                    .AddComponent<AudioSource>();
                Audio = CreateChild("Audio")
                    .AddComponent<FeedbackAudioPresenter>();
                Audio.Configure(Controller, effectSource);
                Haptics = CreateChild("Haptics")
                    .AddComponent<FeedbackHapticPresenter>();
                Haptics.Configure(Controller);
                MusicSource = CreateChild("Music")
                    .AddComponent<AudioSource>();
                MusicSource.loop = true;

                CanvasGroup frontEndGroup = CreateGroup("FrontEndGroup");
                CanvasGroup shopPage = CreateGroup("ShopPage");
                CanvasGroup homePage = CreateGroup("HomePage");
                CanvasGroup challengePage = CreateGroup("ChallengePage");
                FrontEnd = CreateChild("FrontEnd")
                    .AddComponent<FrontEndPresenter>();
                FrontEnd.ConfigureForSetup(
                    Controller,
                    null,
                    frontEndGroup,
                    shopPage,
                    homePage,
                    challengePage,
                    null,
                    null,
                    CreateButton("ShopTab"),
                    CreateButton("HomeTab"),
                    CreateButton("ChallengeTab"),
                    new Image[3],
                    new Image[3],
                    new TMP_Text[3],
                    CreateButton("HomePlay"),
                    CreateButton("ChallengePlay"),
                    CreateText("ChallengePlayLabel"),
                    CreateChild("ScrollRect").AddComponent<ScrollRect>(),
                    new FrontEndLevelNodeView[0],
                    new Image[0]);

                PanelGroup = CreateGroup("PanelGroup");
                OpenButton = CreateButton("OpenButton");
                CloseButton = CreateButton("CloseButton");
                SoundButton = CreateButton("SoundButton");
                MusicButton = CreateButton("MusicButton");
                HapticButton = CreateButton("HapticButton");
                Button languageButton = CreateButton("LanguageButton");
                HomeButton = CreateButton("SettingsHomeButton");
                ExitButton = CreateButton("ExitButton");
                Image soundBackground = SoundButton.GetComponent<Image>();
                Image musicBackground = MusicButton.GetComponent<Image>();
                Image hapticBackground = HapticButton.GetComponent<Image>();
                Image soundIcon = CreateImage("SoundIcon");
                Image musicIcon = CreateImage("MusicIcon");
                Image hapticIcon = CreateImage("HapticIcon");
                SoundState = CreateText("SoundState");
                MusicState = CreateText("MusicState");
                HapticState = CreateText("HapticState");

                Presenter = CreateChild("SettingsPresenter")
                    .AddComponent<SettingsPanelPresenter>();
                Presenter.ConfigureForSetup(
                    Controller,
                    FrontEnd,
                    Audio,
                    Haptics,
                    PanelGroup,
                    OpenButton,
                    CloseButton,
                    SoundButton,
                    MusicButton,
                    HapticButton,
                    languageButton,
                    HomeButton,
                    ExitButton,
                    soundBackground,
                    musicBackground,
                    hapticBackground,
                    soundIcon,
                    musicIcon,
                    hapticIcon,
                    SoundState,
                    MusicState,
                    HapticState,
                    new[] { MusicSource },
                    false);
            }

            public FirstPlayableController Controller { get; }
            public FeedbackAudioPresenter Audio { get; }
            public FeedbackHapticPresenter Haptics { get; }
            public AudioSource MusicSource { get; }
            public FrontEndPresenter FrontEnd { get; }
            public SettingsPanelPresenter Presenter { get; }
            public CanvasGroup PanelGroup { get; }
            public Button OpenButton { get; }
            public Button CloseButton { get; }
            public Button SoundButton { get; }
            public Button MusicButton { get; }
            public Button HapticButton { get; }
            public Button HomeButton { get; }
            public Button ExitButton { get; }
            public TMP_Text SoundState { get; }
            public TMP_Text MusicState { get; }
            public TMP_Text HapticState { get; }

            public void ActivateWithoutFrontend()
            {
                _root.SetActive(true);
                FrontEnd.SkipForTesting();
            }

            public void Dispose()
            {
                Object.DestroyImmediate(_root);
            }

            private GameObject CreateChild(string name)
            {
                var gameObject = new GameObject(name, typeof(RectTransform));
                gameObject.transform.SetParent(_root.transform, false);
                return gameObject;
            }

            private CanvasGroup CreateGroup(string name) =>
                CreateChild(name).AddComponent<CanvasGroup>();

            private Button CreateButton(string name)
            {
                GameObject gameObject = CreateChild(name);
                Image image = gameObject.AddComponent<Image>();
                Button button = gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                return button;
            }

            private Image CreateImage(string name) =>
                CreateChild(name).AddComponent<Image>();

            private TMP_Text CreateText(string name) =>
                CreateChild(name).AddComponent<TextMeshProUGUI>();
        }
    }
}
