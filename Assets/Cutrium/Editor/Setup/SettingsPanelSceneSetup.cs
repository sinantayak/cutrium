using System;
using System.Linq;
using Cutrium.Presentation.Feedback;
using Cutrium.Presentation.Frontend;
using Cutrium.Presentation.Localization;
using Cutrium.Presentation.Settings;
using Cutrium.Unity.Layout;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cutrium.Editor.Setup
{
    public static class SettingsPanelSceneSetup
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";
        private const string PanelBackgroundPath =
            "Assets/Cutrium/Content/Gui/GeneralPanelBackground.png";
        private const string ToggleBackgroundPath =
            "Assets/Cutrium/Content/Gui/SmallSquareButtonBackground_2.png";
        private const string ActionBackgroundPath =
            "Assets/Cutrium/Content/Gui/GeneralButtonBackground_2.png";
        private const string MusicIconPath =
            "Assets/Cutrium/Content/Gui/MusicIcon.png";
        private const string SoundIconPath =
            "Assets/Cutrium/Content/Gui/SoundIcon.png";
        private const string HapticIconPath =
            "Assets/Cutrium/Content/Gui/HapticIcon.png";
        private const string CloseIconPath =
            "Assets/Cutrium/Content/Gui/CloseIcon.png";
        private const string SettingsButtonScenePath =
            "Canvas/SafeAreaRoot/TopHUD/GameplayHudRow/" +
            "SettingsSlot/SettingsButton";
        private const float SettingsEntrySize = 48f;

        private static readonly Color ScrimColor =
            new Color(0.07f, 0.035f, 0.02f, 0.82f);
        private static readonly Color PrimaryText =
            new Color32(255, 239, 210, 255);

        [MenuItem("Cutrium/Setup/Apply Settings Panel")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before applying the Settings panel.");
            }

            Scene scene = OpenVerticalSliceScene();
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform canvas = RequireChild(root.transform, "Canvas");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            FrontEndPresenter frontEnd = root
                .GetComponentInChildren<FrontEndPresenter>(true);
            FeedbackAudioPresenter feedbackAudio = root
                .GetComponentInChildren<FeedbackAudioPresenter>(true);
            FeedbackHapticPresenter feedbackHaptics = root
                .GetComponentInChildren<FeedbackHapticPresenter>(true);
            Button openButton = RequireChild(
                    root.transform,
                    SettingsButtonScenePath)
                .GetComponent<Button>();
            if (controller == null
                || frontEnd == null
                || feedbackAudio == null
                || feedbackHaptics == null
                || openButton == null)
            {
                throw new InvalidOperationException(
                    "Settings setup requires the gameplay controller, " +
                    "frontend, feedback presenters, and HUD Settings button.");
            }

            SettingsArtwork artwork = LoadArtwork();
            TMP_FontAsset font =
                LandmarkRevealPresentationSetup.LoadTmpUiFontForSetup();
            Button homeOpenButton = ConfigureSettingsEntryPoints(
                frontEnd,
                openButton);

            RectTransform settingsRoot = GetOrCreateUiChild(
                canvas,
                "SettingsPanelRoot");
            Stretch(settingsRoot);
            settingsRoot.SetAsLastSibling();
            Image scrim = GetOrAddComponent<Image>(settingsRoot.gameObject);
            scrim.sprite = null;
            scrim.color = ScrimColor;
            scrim.raycastTarget = true;
            CanvasGroup panelGroup = GetOrAddComponent<CanvasGroup>(
                settingsRoot.gameObject);
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;

            RectTransform safeArea = GetOrCreateUiChild(
                settingsRoot,
                "SafeAreaContent");
            Stretch(safeArea);
            SafeAreaFitter safeAreaFitter = GetOrAddComponent<SafeAreaFitter>(
                safeArea.gameObject);
            safeAreaFitter.Configure(safeArea);

            RectTransform panelBounds = GetOrCreateUiChild(
                safeArea,
                "SettingsPanelBounds");
            SetAnchors(
                panelBounds,
                GameplayProgressionSetup.CompactModalPanelAnchorMin,
                GameplayProgressionSetup.CompactModalPanelAnchorMax);
            RectTransform panel = GetOrCreateUiChild(
                panelBounds,
                "SettingsPanel");
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(
                artwork.Panel.rect.width,
                artwork.Panel.rect.height);
            Image panelImage = GetOrAddComponent<Image>(panel.gameObject);
            panelImage.sprite = artwork.Panel;
            panelImage.type = Image.Type.Simple;
            panelImage.preserveAspect = true;
            panelImage.color = Color.white;
            panelImage.raycastTarget = true;
            AspectRatioFitter panelAspect =
                GetOrAddComponent<AspectRatioFitter>(panel.gameObject);
            panelAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            panelAspect.aspectRatio = artwork.Panel.rect.width
                / artwork.Panel.rect.height;

            ToggleSetupResult sound = ConfigureToggle(
                panel,
                "SoundToggle",
                new Vector2(0.145f, 0.62f),
                new Vector2(0.365f, 0.82f),
                artwork.ToggleBackground,
                artwork.SoundIcon);
            ToggleSetupResult music = ConfigureToggle(
                panel,
                "MusicToggle",
                new Vector2(0.39f, 0.62f),
                new Vector2(0.61f, 0.82f),
                artwork.ToggleBackground,
                artwork.MusicIcon);
            ToggleSetupResult haptic = ConfigureToggle(
                panel,
                "HapticToggle",
                new Vector2(0.635f, 0.62f),
                new Vector2(0.855f, 0.82f),
                artwork.ToggleBackground,
                artwork.HapticIcon);

            Button closeButton = ConfigureIconButton(
                panel,
                "CloseButton",
                new Vector2(0.825f, 0.84f),
                new Vector2(0.965f, 0.97f),
                artwork.CloseIcon);
            Button languageButton = ConfigureActionButton(
                panel,
                "LanguageButton",
                "English",
                new Vector2(0.15f, 0.455f),
                new Vector2(0.85f, 0.575f),
                artwork.ActionBackground,
                font);
            Button homeButton = ConfigureActionButton(
                panel,
                "HomeButton",
                "Home",
                new Vector2(0.15f, 0.305f),
                new Vector2(0.85f, 0.425f),
                artwork.ActionBackground,
                font);
            Button exitButton = ConfigureActionButton(
                panel,
                "ExitButton",
                "Exit",
                new Vector2(0.15f, 0.155f),
                new Vector2(0.85f, 0.275f),
                artwork.ActionBackground,
                font);

            RectTransform gameOverPanelBounds =
                GameplayProgressionSetup.ApplyCompactGameOverPanelBounds(
                    root.transform);

            openButton.interactable = true;
            GameObject musicServices = GetOrCreateChild(
                feedbackAudio.transform,
                "MusicSource");
            AudioSource musicSource =
                GetOrAddComponent<AudioSource>(musicServices);
            musicSource.playOnAwake = true;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.5f;
            AudioSource[] musicSources = root
                .GetComponentsInChildren<AudioSource>(true)
                .Where(source => source != feedbackAudio.AudioSource
                    && source.loop)
                .ToArray();
            LocalizationSceneSetup.LocalizationSetupResult localization =
                LocalizationSceneSetup.ApplyToScene(root);
            SettingsPanelPresenter presenter =
                GetOrAddComponent<SettingsPanelPresenter>(
                    settingsRoot.gameObject);
            presenter.ConfigureForSetup(
                controller,
                frontEnd,
                feedbackAudio,
                feedbackHaptics,
                panelGroup,
                openButton,
                closeButton,
                sound.Button,
                music.Button,
                haptic.Button,
                languageButton,
                homeButton,
                exitButton,
                sound.Background,
                music.Background,
                haptic.Background,
                sound.Icon,
                music.Icon,
                haptic.Icon,
                sound.StateLabel,
                music.StateLabel,
                haptic.StateLabel,
                musicSources,
                true,
                homeOpenButton,
                localization.Service);

            Validate(
                settingsRoot,
                panelBounds,
                panel,
                gameOverPanelBounds,
                presenter,
                artwork,
                localization);
            EditorUtility.SetDirty(musicSource);
            EditorUtility.SetDirty(openButton);
            EditorUtility.SetDirty(presenter);
            EditorUtility.SetDirty(panelGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the Settings panel setup.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Settings panel ready: shared Home/gameplay entry points, " +
                "Sound, Music, Haptic, EN/TR, Home, Exit, and Close are " +
                "wired in VerticalSlice.unity.");
        }

        private static Button ConfigureSettingsEntryPoints(
            FrontEndPresenter frontEnd,
            Button gameplayOpenButton)
        {
            if (frontEnd.HomePage == null)
            {
                throw new InvalidOperationException(
                    "The frontend Home page is required for Settings.");
            }

            RectTransform gameplayButtonRect =
                (RectTransform)gameplayOpenButton.transform;
            RectTransform gameplaySlot =
                gameplayButtonRect.parent as RectTransform;
            if (gameplaySlot == null)
            {
                throw new InvalidOperationException(
                    "The gameplay Settings button needs a UI slot.");
            }

            gameplaySlot.sizeDelta = new Vector2(
                SettingsEntrySize,
                SettingsEntrySize);
            Stretch(gameplayButtonRect);

            Image gameplayImage = gameplayOpenButton.targetGraphic as Image
                ?? gameplayOpenButton.GetComponent<Image>();
            if (gameplayImage == null || gameplayImage.sprite == null)
            {
                throw new InvalidOperationException(
                    "The gameplay Settings button needs its gear Sprite.");
            }

            RectTransform homeSlot = GetOrCreateUiChild(
                frontEnd.HomePage.transform,
                "SettingsSlot");
            homeSlot.gameObject.SetActive(true);
            homeSlot.anchorMin = Vector2.one;
            homeSlot.anchorMax = Vector2.one;
            homeSlot.pivot = Vector2.one;
            homeSlot.anchoredPosition = new Vector2(-20f, -20f);
            homeSlot.sizeDelta = new Vector2(
                SettingsEntrySize,
                SettingsEntrySize);
            homeSlot.SetAsLastSibling();

            RectTransform homeButtonRect = GetOrCreateUiChild(
                homeSlot,
                "SettingsButton");
            Stretch(homeButtonRect);
            Image homeImage = GetOrAddComponent<Image>(
                homeButtonRect.gameObject);
            homeImage.sprite = gameplayImage.sprite;
            homeImage.type = gameplayImage.type;
            homeImage.color = gameplayImage.color;
            homeImage.preserveAspect = gameplayImage.preserveAspect;
            homeImage.raycastTarget = true;

            Button homeButton = GetOrAddComponent<Button>(
                homeButtonRect.gameObject);
            homeButton.targetGraphic = homeImage;
            homeButton.transition = gameplayOpenButton.transition;
            homeButton.colors = gameplayOpenButton.colors;
            homeButton.spriteState = gameplayOpenButton.spriteState;
            homeButton.interactable = true;
            Navigation navigation = homeButton.navigation;
            navigation.mode = Navigation.Mode.None;
            homeButton.navigation = navigation;

            EditorUtility.SetDirty(gameplaySlot);
            EditorUtility.SetDirty(gameplayOpenButton);
            EditorUtility.SetDirty(homeImage);
            EditorUtility.SetDirty(homeButton);
            return homeButton;
        }

        private static ToggleSetupResult ConfigureToggle(
            RectTransform panel,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Sprite backgroundSprite,
            Sprite iconSprite)
        {
            RectTransform bounds = GetOrCreateUiChild(panel, name + "Bounds");
            SetAnchors(bounds, anchorMin, anchorMax);
            RectTransform buttonRect = GetOrCreateUiChild(bounds, name);
            Stretch(buttonRect);
            AspectRatioFitter aspect = GetOrAddComponent<AspectRatioFitter>(
                buttonRect.gameObject);
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 1f;

            Image background = GetOrAddComponent<Image>(buttonRect.gameObject);
            background.sprite = backgroundSprite;
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            background.color = Color.white;
            background.raycastTarget = true;
            Button button = GetOrAddComponent<Button>(buttonRect.gameObject);
            ConfigureButton(button, background);

            RectTransform iconRect = GetOrCreateUiChild(buttonRect, "Icon");
            SetAnchors(
                iconRect,
                new Vector2(0.26f, 0.27f),
                new Vector2(0.74f, 0.75f));
            ApplyIconAssetOffsets(iconRect);
            Image icon = GetOrAddComponent<Image>(iconRect.gameObject);
            icon.sprite = iconSprite;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.color = Color.white;
            icon.raycastTarget = false;

            DestroyUiChildIfPresent(buttonRect, "StateLabel");
            return new ToggleSetupResult(button, background, icon);
        }

        private static Button ConfigureIconButton(
            RectTransform panel,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Sprite iconSprite)
        {
            RectTransform bounds = GetOrCreateUiChild(panel, name + "Bounds");
            SetAnchors(bounds, anchorMin, anchorMax);
            RectTransform buttonRect = GetOrCreateUiChild(bounds, name);
            Stretch(buttonRect);
            AspectRatioFitter aspect = GetOrAddComponent<AspectRatioFitter>(
                buttonRect.gameObject);
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 1f;
            Image hitArea = GetOrAddComponent<Image>(buttonRect.gameObject);
            hitArea.sprite = null;
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;

            RectTransform iconRect = GetOrCreateUiChild(buttonRect, "Icon");
            SetAnchors(
                iconRect,
                new Vector2(0.22f, 0.22f),
                new Vector2(0.78f, 0.78f));
            ApplyIconAssetOffsets(iconRect);
            Image icon = GetOrAddComponent<Image>(iconRect.gameObject);
            icon.sprite = iconSprite;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.color = Color.white;
            icon.raycastTarget = false;
            Button button = GetOrAddComponent<Button>(buttonRect.gameObject);
            ConfigureButton(button, icon);
            return button;
        }

        private static Button ConfigureActionButton(
            RectTransform panel,
            string name,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Sprite backgroundSprite,
            TMP_FontAsset font)
        {
            RectTransform buttonRect = GetOrCreateUiChild(panel, name);
            SetAnchors(buttonRect, anchorMin, anchorMax);
            Image background = GetOrAddComponent<Image>(buttonRect.gameObject);
            background.sprite = backgroundSprite;
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            background.color = Color.white;
            background.raycastTarget = true;
            Button button = GetOrAddComponent<Button>(buttonRect.gameObject);
            ConfigureButton(button, background);

            RectTransform labelRect = GetOrCreateUiChild(buttonRect, "Label");
            SetAnchors(
                labelRect,
                new Vector2(0.12f, 0.14f),
                new Vector2(0.88f, 0.86f));
            ConfigureText(labelRect, label, font, 52f, PrimaryText);
            return button;
        }

        private static TMP_Text ConfigureText(
            RectTransform rect,
            string value,
            TMP_FontAsset font,
            float fontSize,
            Color color)
        {
            TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(
                rect.gameObject);
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(16f, fontSize * 0.65f);
            text.fontSizeMax = fontSize;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureButton(Button button, Graphic target)
        {
            button.targetGraphic = target;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = CreateButtonColors();
            button.interactable = true;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        private static ColorBlock CreateButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(255, 225, 190, 255);
            colors.pressedColor = new Color32(218, 139, 80, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(90, 63, 50, 150);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            return colors;
        }

        private static SettingsArtwork LoadArtwork() => new SettingsArtwork(
            EnsureUiSprite(PanelBackgroundPath),
            EnsureUiSprite(ToggleBackgroundPath),
            EnsureUiSprite(ActionBackgroundPath),
            EnsureUiSprite(MusicIconPath),
            EnsureUiSprite(SoundIconPath),
            EnsureUiSprite(HapticIconPath),
            EnsureUiSprite(CloseIconPath));

        private static Sprite EnsureUiSprite(string path)
        {
            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path)
                as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"Settings texture is missing or invalid: {path}");
            }

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency
                || importer.mipmapEnabled
                || importer.wrapMode != TextureWrapMode.Clamp;
            if (changed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return sprite != null
                ? sprite
                : throw new InvalidOperationException(
                    $"Unity could not load Settings Sprite: {path}");
        }

        private static void Validate(
            RectTransform settingsRoot,
            RectTransform panelBounds,
            RectTransform panel,
            RectTransform gameOverPanelBounds,
            SettingsPanelPresenter presenter,
            SettingsArtwork artwork,
            LocalizationSceneSetup.LocalizationSetupResult localization)
        {
            Image panelImage = panel.GetComponent<Image>();
            AspectRatioFitter panelAspect =
                panel.GetComponent<AspectRatioFitter>();
            if (settingsRoot.parent == null
                || settingsRoot.GetSiblingIndex()
                    != settingsRoot.parent.childCount - 1
                || panelBounds.anchorMin
                    != GameplayProgressionSetup.CompactModalPanelAnchorMin
                || panelBounds.anchorMax
                    != GameplayProgressionSetup.CompactModalPanelAnchorMax
                || gameOverPanelBounds == null
                || gameOverPanelBounds.anchorMin
                    != GameplayProgressionSetup.CompactModalPanelAnchorMin
                || gameOverPanelBounds.anchorMax
                    != GameplayProgressionSetup.CompactModalPanelAnchorMax
                || panelImage == null
                || panelImage.sprite != artwork.Panel
                || panelAspect == null
                || panelAspect.aspectMode
                    != AspectRatioFitter.AspectMode.FitInParent
                || presenter.Controller == null
                || presenter.PanelCanvasGroup == null
                || presenter.OpenButton == null
                || !presenter.OpenButton.interactable
                || presenter.HomeOpenButton == null
                || !presenter.HomeOpenButton.interactable
                || presenter.Localization != localization.Service
                || presenter.CloseButton == null
                || presenter.SoundButton == null
                || presenter.MusicButton == null
                || presenter.HapticButton == null
                || presenter.LanguageButton == null
                || presenter.HomeButton == null
                || presenter.ExitButton == null
                || presenter.MusicSources.Count < 1
                || presenter.MusicSources[0] == null
                || !presenter.MusicSources[0].loop
                || !presenter.MusicSources[0].playOnAwake)
            {
                throw new InvalidOperationException(
                    "Settings hierarchy or serialized references are incomplete.");
            }

            RectTransform gameplaySlot =
                presenter.OpenButton.transform.parent as RectTransform;
            RectTransform homeSlot =
                presenter.HomeOpenButton.transform.parent as RectTransform;
            Image gameplayGear = presenter.OpenButton.targetGraphic as Image;
            Image homeGear = presenter.HomeOpenButton.targetGraphic as Image;
            if (gameplaySlot == null
                || homeSlot == null
                || gameplaySlot.sizeDelta
                    != new Vector2(SettingsEntrySize, SettingsEntrySize)
                || homeSlot.sizeDelta
                    != new Vector2(SettingsEntrySize, SettingsEntrySize)
                || gameplayGear == null
                || homeGear == null
                || homeGear.sprite != gameplayGear.sprite)
            {
                throw new InvalidOperationException(
                    "Home and gameplay Settings gears must match at 48x48.");
            }

            LocalizationSceneSetup.Validate(
                settingsRoot.root.gameObject,
                localization);

            Image soundIcon = RequireChild(
                    panel,
                    "SoundToggleBounds/SoundToggle/Icon")
                .GetComponent<Image>();
            Image musicIcon = RequireChild(
                    panel,
                    "MusicToggleBounds/MusicToggle/Icon")
                .GetComponent<Image>();
            Image hapticIcon = RequireChild(
                    panel,
                    "HapticToggleBounds/HapticToggle/Icon")
                .GetComponent<Image>();
            Image closeIcon = RequireChild(
                    panel,
                    "CloseButtonBounds/CloseButton/Icon")
                .GetComponent<Image>();
            if (soundIcon.sprite != artwork.SoundIcon
                || musicIcon.sprite != artwork.MusicIcon
                || hapticIcon.sprite != artwork.HapticIcon
                || closeIcon.sprite != artwork.CloseIcon
                || !HasExpectedIconAssetOffsets(
                    (RectTransform)soundIcon.transform)
                || !HasExpectedIconAssetOffsets(
                    (RectTransform)musicIcon.transform)
                || !HasExpectedIconAssetOffsets(
                    (RectTransform)hapticIcon.transform)
                || !HasExpectedIconAssetOffsets(
                    (RectTransform)closeIcon.transform))
            {
                throw new InvalidOperationException(
                    "One or more Settings icons are not wired correctly.");
            }
        }

        private static bool HasExpectedIconAssetOffsets(
            RectTransform iconRect) =>
            Mathf.Approximately(iconRect.offsetMin.x, 10f)
            && Mathf.Approximately(-iconRect.offsetMax.y, -10f);

        private static Scene OpenVerticalSliceScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path == ScenePath)
            {
                return scene;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException(
                    "Settings setup cancelled before opening the scene.");
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            throw new InvalidOperationException(
                $"Scene does not contain required root '{name}'.");
        }

        private static Transform RequireChild(Transform parent, string path)
        {
            Transform child = parent.Find(path);
            return child != null
                ? child
                : throw new InvalidOperationException(
                    $"Missing required scene path '{path}' below " +
                    $"'{parent.name}'.");
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static RectTransform GetOrCreateUiChild(
            Transform parent,
            string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                if (existing is RectTransform existingRect)
                {
                    return existingRect;
                }

                throw new InvalidOperationException(
                    $"Existing Settings object '{name}' is not UI.");
            }

            var gameObject = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Settings UI");
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null
                ? component
                : Undo.AddComponent<T>(gameObject);
        }

        private static void DestroyUiChildIfPresent(
            Transform parent,
            string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        private static void ApplyIconAssetOffsets(RectTransform iconRect)
        {
            // With stretch anchors Unity exposes offsetMin.x as Left and
            // -offsetMax.y as Top in the RectTransform Inspector.
            iconRect.offsetMin = new Vector2(10f, 0f);
            iconRect.offsetMax = new Vector2(0f, 10f);
        }

        private static void Stretch(RectTransform rect)
        {
            SetAnchors(rect, Vector2.zero, Vector2.one);
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private readonly struct ToggleSetupResult
        {
            public ToggleSetupResult(
                Button button,
                Image background,
                Image icon)
            {
                Button = button;
                Background = background;
                Icon = icon;
                StateLabel = null;
            }

            public Button Button { get; }
            public Image Background { get; }
            public Image Icon { get; }
            public TMP_Text StateLabel { get; }
        }

        private readonly struct SettingsArtwork
        {
            public SettingsArtwork(
                Sprite panel,
                Sprite toggleBackground,
                Sprite actionBackground,
                Sprite musicIcon,
                Sprite soundIcon,
                Sprite hapticIcon,
                Sprite closeIcon)
            {
                Panel = panel;
                ToggleBackground = toggleBackground;
                ActionBackground = actionBackground;
                MusicIcon = musicIcon;
                SoundIcon = soundIcon;
                HapticIcon = hapticIcon;
                CloseIcon = closeIcon;
            }

            public Sprite Panel { get; }
            public Sprite ToggleBackground { get; }
            public Sprite ActionBackground { get; }
            public Sprite MusicIcon { get; }
            public Sprite SoundIcon { get; }
            public Sprite HapticIcon { get; }
            public Sprite CloseIcon { get; }
        }
    }
}
