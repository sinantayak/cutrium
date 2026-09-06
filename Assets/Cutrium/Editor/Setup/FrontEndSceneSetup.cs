using System;
using System.Collections.Generic;
using Cutrium.Presentation.Economy;
using Cutrium.Presentation.Frontend;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Layout;
using Cutrium.Unity.Services;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cutrium.Editor.Setup
{
    public static class FrontEndSceneSetup
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";
        private const string ShopIconPath =
            "Assets/Cutrium/Content/Gui/ShopIcon.png";
        private const string HomeIconPath =
            "Assets/Cutrium/Content/Gui/HomeIcon.png";
        private const string ChallengeIconPath =
            "Assets/Cutrium/Content/Gui/ChallangeIcon.png";
        private const string NodeSpritePath =
            "Assets/Cutrium/Content/Gui/NodeAsset.png";
        private const string NodeLockSpritePath =
            "Assets/Cutrium/Content/Gui/NodeLockAsset.png";
        private const string PlayButtonSpritePath =
            "Assets/Cutrium/Content/Gui/GeneralButtonBackground.png";
        private const string HomeBackgroundPath =
            "Assets/Cutrium/Content/Gui/HomeBackground.png";
        private const string HomeLogoPath =
            "Assets/Cutrium/Content/Gui/CutriumAmblem.png";

        private const float NavigationHeight = 176f;
        // Insets ShopPage/HomePage from the top by the same real mask
        // boundary the bottom nav already gets from NavigationHeight --
        // covers the shared Coin/Settings HUD's own footprint (deepest is
        // the coin slot: 16 top offset + 56 height) plus a margin, so
        // scrolled content is actually clipped there instead of merely
        // being drawn underneath the HUD icons and peeking through the
        // gaps around them while scrolling.
        private const float HudZoneHeight = 96f;
        private const float NodeSpacing = 226f;
        private const float NodeBottomPadding = 118f;
        private const float NodeTopPadding = 360f;
        private const float ChallengePlayBottomGap = 58f;
        private const float ChallengeMapPlayGap = 56f;
        private const float SelectedTabTopExtension = 30f;
        private const float NavigationCornerRadius = 34f;

        private static readonly Color FrontEndBackground =
            new Color32(31, 14, 7, 255);
        private static readonly Color NavigationBackground =
            new Color32(38, 14, 5, 255);
        private static readonly Color PrimaryText =
            new Color32(255, 230, 191, 255);
        private static readonly Color SecondaryText =
            new Color32(188, 126, 83, 255);
        internal static readonly Color BrownLabelText =
            new Color32(97, 48, 24, 255);
        // Matches gameplay's TopHUD CoinBalanceSlot exactly (measured from
        // the live scene: SafeAreaRoot > TopHUD > GameplayHudRow's own
        // padding nets out to this offset from the safe area's own top
        // corners) so the HUD sits in the identical spot on every screen.
        internal static readonly Vector2 SharedHudCoinAnchoredPosition =
            new Vector2(30f, -16f);
        internal static readonly Vector2 SharedHudSettingsAnchoredPosition =
            new Vector2(-30f, -16f);

        [MenuItem("Cutrium/Setup/Apply Frontend Home and Level Map")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before applying frontend setup.");
            }

            Scene scene = OpenVerticalSliceScene();
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            Transform canvas = RequireChild(root.transform, "Canvas");
            ScreenTransitionPresenter screenTransition =
                LandmarkRevealPresentationSetup
                    .ConfigureScreenTransitionForSetup(canvas);
            RequireChild(canvas, "SafeAreaRoot");
            FirstPlayableController controller = root
                .GetComponentInChildren<FirstPlayableController>(true);
            PreLevelIntroPresenter preLevelIntro = root
                .GetComponentInChildren<PreLevelIntroPresenter>(true);
            if (controller == null || preLevelIntro == null)
            {
                throw new InvalidOperationException(
                    "Frontend setup requires FirstPlayableController and " +
                    "PreLevelIntroPresenter in VerticalSliceRoot.");
            }

            int levelCount = controller.LevelDefinitions.Count;
            if (levelCount <= 0)
            {
                throw new InvalidOperationException(
                    "Frontend setup requires a non-empty level catalog.");
            }

            Sprite shopIcon = EnsureUiSprite(ShopIconPath);
            Sprite homeIcon = EnsureUiSprite(HomeIconPath);
            Sprite challengeIcon = EnsureUiSprite(ChallengeIconPath);
            Sprite nodeSprite = EnsureUiSprite(NodeSpritePath);
            Sprite nodeLockSprite = EnsureUiSprite(NodeLockSpritePath);
            Sprite filledStarSprite = EnsureUiSprite(
                LandmarkRevealPresentationSetup.YellowStarPath);
            Sprite emptyStarSprite = EnsureUiSprite(
                LandmarkRevealPresentationSetup.GrayStarPath);
            Sprite playButtonSprite = EnsureUiSprite(PlayButtonSpritePath);
            Sprite homeBackground = EnsureUiSprite(HomeBackgroundPath);
            Sprite homeLogo = EnsureUiSprite(HomeLogoPath);
            TMP_FontAsset font =
                LandmarkRevealPresentationSetup.LoadTmpUiFontForSetup();

            RectTransform frontEndRoot = GetOrCreateUiChild(
                canvas,
                "FrontEndRoot");
            Stretch(frontEndRoot);
            frontEndRoot.SetAsLastSibling();
            Image rootBackground = GetOrAddComponent<Image>(
                frontEndRoot.gameObject);
            rootBackground.sprite = null;
            rootBackground.color = FrontEndBackground;
            rootBackground.raycastTarget = true;
            CanvasGroup rootGroup = GetOrAddComponent<CanvasGroup>(
                frontEndRoot.gameObject);

            Image backgroundArtwork = ConfigureBackgroundArtwork(
                frontEndRoot,
                homeBackground);

            RectTransform frontEndSafeArea = GetOrCreateUiChild(
                frontEndRoot,
                "SafeAreaContent");
            Stretch(frontEndSafeArea);
            SafeAreaFitter safeAreaFitter = GetOrAddComponent<SafeAreaFitter>(
                frontEndSafeArea.gameObject);
            safeAreaFitter.Configure(frontEndSafeArea);

            CanvasGroup shopPage = ConfigurePage(
                frontEndSafeArea,
                "ShopPage");
            CanvasGroup homePage = ConfigurePage(
                frontEndSafeArea,
                "HomePage");
            CanvasGroup challengePage = ConfigureFullBleedChallengePage(
                frontEndRoot,
                frontEndSafeArea);

            ConfigureShopPage(shopPage, font);
            Button homePlayButton = ConfigureHomePage(
                homePage,
                font,
                playButtonSprite,
                homeLogo);
            Image homeLogoImage = RequireChild(
                    homePage.transform,
                    "CutriumLogo")
                .GetComponent<Image>();

            ChallengeSetupResult challenge = ConfigureChallengePage(
                challengePage,
                font,
                playButtonSprite,
                nodeSprite,
                nodeLockSprite,
                filledStarSprite,
                emptyStarSprite,
                levelCount);

            NavigationSetupResult navigation = ConfigureBottomNavigation(
                frontEndSafeArea,
                frontEndRoot,
                font,
                shopIcon,
                homeIcon,
                challengeIcon);

            // A direct child of the shared safe area (a sibling of
            // ShopPage/HomePage, not nested inside either one) so the
            // balance reads identically no matter which tab is active --
            // same reasoning as the Settings gear added in
            // SettingsPanelSceneSetup.ConfigureSettingsEntryPoints.
            CloudServicesBootstrap cloudServices = root
                .GetComponentInChildren<CloudServicesBootstrap>(true);
            if (cloudServices == null)
            {
                throw new InvalidOperationException(
                    "Frontend setup requires CloudServicesBootstrap for " +
                    "the Coin balance display.");
            }

            Undo.RecordObject(controller, "Wire Star Progress Cloud Save");
            controller.ConfigureProgressCloudForSetup(cloudServices);

            CoinBalanceHudPresenter coinBalance =
                LandmarkRevealPresentationSetup.ConfigureCoinBalanceSlot(
                    frontEndSafeArea,
                    EnsureUiSprite(LandmarkRevealPresentationSetup
                        .CoinStackL1Path),
                    font,
                    cloudServices,
                    SharedHudCoinAnchoredPosition,
                    BrownLabelText);

            FrontEndPresenter presenter = GetOrAddComponent<FrontEndPresenter>(
                frontEndRoot.gameObject);
            presenter.ConfigureForSetup(
                controller,
                preLevelIntro,
                rootGroup,
                shopPage,
                homePage,
                challengePage,
                backgroundArtwork,
                homeLogoImage,
                navigation.Buttons[0],
                navigation.Buttons[1],
                navigation.Buttons[2],
                navigation.Plates,
                navigation.Icons,
                navigation.Labels,
                homePlayButton,
                challenge.PlayButton,
                challenge.PlayLabel,
                challenge.ScrollRect,
                challenge.Nodes,
                challenge.Connectors,
                screenTransition);
            LandmarkRevealPresentationSetup
                .WireScreenTransitionConsumersForSetup(
                    root,
                    screenTransition);

            Validate(
                frontEndRoot,
                presenter,
                levelCount,
                shopIcon,
                homeIcon,
                challengeIcon,
                nodeSprite,
                nodeLockSprite,
                filledStarSprite,
                emptyStarSprite,
                homeBackground,
                homeLogo);
            if (coinBalance.CloudServices != cloudServices
                || coinBalance.transform.parent != frontEndSafeArea)
            {
                throw new InvalidOperationException(
                    "The shared Coin balance display is not wired correctly.");
            }

            EditorUtility.SetDirty(coinBalance);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(presenter);
            EditorUtility.SetDirty(rootGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the frontend scene setup.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Frontend ready: Home, Shop placeholder, and {levelCount} " +
                "real Challenge level nodes wired in VerticalSlice.unity.");
        }

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
                    "Frontend setup cancelled before opening the scene.");
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static Image ConfigureBackgroundArtwork(
            RectTransform root,
            Sprite sprite)
        {
            RectTransform rect = GetOrCreateUiChild(
                root,
                "BackgroundArtwork");
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.SetAsFirstSibling();

            Image image = GetOrAddComponent<Image>(rect.gameObject);
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = false;
            image.raycastTarget = false;

            AspectRatioFitter aspect = GetOrAddComponent<AspectRatioFitter>(
                rect.gameObject);
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = sprite.rect.width / sprite.rect.height;
            return image;
        }

        private static void ConfigureShopPage(
            CanvasGroup page,
            TMP_FontAsset font)
        {
            DestroyUiChildIfPresent(page.transform, "ShopTitle");
            DestroyUiChildIfPresent(page.transform, "ShopSubtitle");
            DestroyUiChildIfPresent(page.transform, "FutureShopContent");
            ShopContentSceneSetup.Configure((RectTransform)page.transform, font);
        }

        private static Button ConfigureHomePage(
            CanvasGroup page,
            TMP_FontAsset font,
            Sprite playButtonSprite,
            Sprite homeLogo)
        {
            RectTransform futureContent = GetOrCreateUiChild(
                page.transform,
                "FutureHomeContent");
            Stretch(futureContent);
            futureContent.offsetMin = new Vector2(40f, 620f);
            futureContent.offsetMax = new Vector2(-40f, -40f);
            futureContent.SetAsFirstSibling();

            RectTransform quickAccess = GetOrCreateUiChild(
                futureContent,
                "FutureQuickAccessRow");
            quickAccess.anchorMin = new Vector2(0f, 0f);
            quickAccess.anchorMax = new Vector2(1f, 0f);
            quickAccess.pivot = new Vector2(0.5f, 0f);
            quickAccess.anchoredPosition = Vector2.zero;
            quickAccess.sizeDelta = new Vector2(0f, 160f);

            RectTransform logoRect = GetOrCreateUiChild(
                page.transform,
                "CutriumLogo");
            Anchor(
                logoRect,
                new Vector2(0.5f, 0.64f),
                new Vector2(610f, 590f));
            logoRect.anchoredPosition = new Vector2(0f, 30f);
            Image logo = GetOrAddComponent<Image>(logoRect.gameObject);
            logo.sprite = homeLogo;
            logo.color = Color.white;
            logo.preserveAspect = true;
            logo.raycastTarget = false;

            return ConfigurePlayButton(
                page.transform,
                "HomePlayButton",
                "PLAY",
                font,
                playButtonSprite,
                new Vector2(0.5f, 0.31f),
                new Vector2(420f, 172f));
        }

        private static ChallengeSetupResult ConfigureChallengePage(
            CanvasGroup page,
            TMP_FontAsset font,
            Sprite playButtonSprite,
            Sprite nodeSprite,
            Sprite nodeLockSprite,
            Sprite filledStarSprite,
            Sprite emptyStarSprite,
            int levelCount)
        {
            DestroyUiChildIfPresent(page.transform, "ChallengeHeader");

            RectTransform scrollRectTransform = GetOrCreateUiChild(
                page.transform,
                "LevelMapScroll");
            Stretch(scrollRectTransform);
            scrollRectTransform.offsetMin = new Vector2(
                0f,
                NavigationHeight
                    + ChallengePlayBottomGap
                    + 172f
                    + ChallengeMapPlayGap);
            Image scrollSurface = GetOrAddComponent<Image>(
                scrollRectTransform.gameObject);
            scrollSurface.sprite = null;
            scrollSurface.color = Color.clear;
            scrollSurface.raycastTarget = true;

            RectTransform viewport = GetOrCreateUiChild(
                scrollRectTransform,
                "Viewport");
            Stretch(viewport);
            Image viewportImage = GetOrAddComponent<Image>(viewport.gameObject);
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImage.raycastTarget = true;
            RectMask2D mask = GetOrAddComponent<RectMask2D>(viewport.gameObject);
            mask.padding = Vector4.zero;

            RectTransform content = GetOrCreateUiChild(viewport, "Content");
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(1f, 0f);
            content.pivot = new Vector2(0.5f, 0f);
            content.anchoredPosition = Vector2.zero;
            float contentHeight = NodeBottomPadding
                + (levelCount - 1) * NodeSpacing
                + NodeTopPadding;
            content.sizeDelta = new Vector2(0f, contentHeight);

            RectTransform pathRoot = GetOrCreateUiChild(content, "PathRoot");
            Stretch(pathRoot);
            ClearGeneratedChildren(pathRoot);
            pathRoot.SetAsFirstSibling();

            RectTransform nodesRoot = GetOrCreateUiChild(content, "NodesRoot");
            Stretch(nodesRoot);
            ClearGeneratedChildren(nodesRoot);
            nodesRoot.SetAsLastSibling();

            Vector2[] positions = BuildNodePositions(levelCount);
            Image[] connectors = BuildConnectors(pathRoot, positions);
            FrontEndLevelNodeView[] nodes = BuildNodes(
                nodesRoot,
                positions,
                font,
                nodeSprite,
                nodeLockSprite,
                filledStarSprite,
                emptyStarSprite);

            ScrollRect scrollRect = GetOrAddComponent<ScrollRect>(
                scrollRectTransform.gameObject);
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 72f;
            scrollRect.verticalNormalizedPosition = 0f;

            Button playButton = ConfigurePlayButton(
                page.transform,
                "ChallengePlayButton",
                "PLAY LEVEL 1",
                font,
                playButtonSprite,
                new Vector2(0.5f, 0f),
                new Vector2(420f, 172f));
            RectTransform playRect = (RectTransform)playButton.transform;
            playRect.pivot = new Vector2(0.5f, 0f);
            playRect.anchoredPosition = new Vector2(
                0f,
                NavigationHeight + ChallengePlayBottomGap);
            TMP_Text playLabel = playRect
                .Find("Label")
                .GetComponent<TMP_Text>();
            playLabel.fontSize = 46f;
            playLabel.fontSizeMin = 28f;
            playLabel.fontSizeMax = 46f;

            return new ChallengeSetupResult(
                scrollRect,
                playButton,
                playLabel,
                nodes,
                connectors);
        }

        private static Vector2[] BuildNodePositions(int count)
        {
            float[] zigzag = { -224f, -92f, 104f, 230f, 94f, -108f };
            var positions = new Vector2[count];
            for (int index = 0; index < count; index++)
            {
                positions[index] = new Vector2(
                    zigzag[index % zigzag.Length],
                    NodeBottomPadding + index * NodeSpacing);
            }

            return positions;
        }

        private static Image[] BuildConnectors(
            RectTransform pathRoot,
            IReadOnlyList<Vector2> positions)
        {
            var connectors = new Image[Mathf.Max(0, positions.Count - 1)];
            for (int index = 0; index < connectors.Length; index++)
            {
                Vector2 start = positions[index];
                Vector2 end = positions[index + 1];
                Vector2 delta = end - start;
                float length = Mathf.Max(24f, delta.magnitude - 116f);
                RectTransform rect = CreateUiChild(
                    pathRoot,
                    $"Connector_{index + 1:00}_{index + 2:00}");
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = (start + end) * 0.5f;
                rect.sizeDelta = new Vector2(length, 14f);
                rect.localEulerAngles = new Vector3(
                    0f,
                    0f,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                Image image = rect.gameObject.AddComponent<Image>();
                image.color = new Color32(80, 43, 27, 190);
                image.raycastTarget = false;
                connectors[index] = image;
            }

            return connectors;
        }

        private static FrontEndLevelNodeView[] BuildNodes(
            RectTransform nodesRoot,
            IReadOnlyList<Vector2> positions,
            TMP_FontAsset font,
            Sprite nodeSprite,
            Sprite nodeLockSprite,
            Sprite filledStarSprite,
            Sprite emptyStarSprite)
        {
            var nodes = new FrontEndLevelNodeView[positions.Count];
            for (int index = 0; index < positions.Count; index++)
            {
                int levelNumber = index + 1;
                RectTransform root = CreateUiChild(
                    nodesRoot,
                    $"LevelNode_{levelNumber:00}");
                root.anchorMin = new Vector2(0.5f, 0f);
                root.anchorMax = new Vector2(0.5f, 0f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.anchoredPosition = positions[index];
                root.sizeDelta = new Vector2(156f, 156f);

                RectTransform glowRect = CreateUiChild(root, "SelectionGlow");
                Anchor(glowRect, new Vector2(0.5f, 0.5f), new Vector2(184f, 184f));
                Image glow = glowRect.gameObject.AddComponent<Image>();
                glow.sprite = nodeSprite;
                glow.preserveAspect = true;
                glow.color = new Color32(255, 100, 24, 199);
                glow.raycastTarget = false;
                glow.gameObject.SetActive(false);

                RectTransform visualRect = CreateUiChild(root, "NodeVisual");
                Stretch(visualRect);
                Image visual = visualRect.gameObject.AddComponent<Image>();
                visual.sprite = nodeSprite;
                visual.preserveAspect = true;
                visual.color = levelNumber == 1
                    ? Color.white
                    : new Color32(120, 76, 57, 255);
                visual.raycastTarget = true;

                RectTransform labelRect = CreateUiChild(root, "Number");
                Anchor(
                    labelRect,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(112f, 112f));
                labelRect.anchoredPosition = Vector2.zero;
                TMP_Text label = ConfigureText(
                    labelRect,
                    levelNumber.ToString(),
                    font,
                    58f,
                    PrimaryText,
                    TextAlignmentOptions.Center);

                RectTransform lockRect = CreateUiChild(root, "LockIcon");
                Stretch(lockRect);
                Image lockImage = lockRect.gameObject.AddComponent<Image>();
                lockImage.sprite = nodeLockSprite;
                lockImage.preserveAspect = true;
                lockImage.color = Color.white;
                lockImage.raycastTarget = false;
                lockImage.gameObject.SetActive(false);

                RectTransform starsRoot = CreateUiChild(root, "Stars");
                Anchor(
                    starsRoot,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(138f, 46f));
                starsRoot.anchoredPosition = new Vector2(0f, 104f);
                var starImages = new Image[3];
                for (int starIndex = 0; starIndex < starImages.Length;
                    starIndex++)
                {
                    RectTransform starRect = CreateUiChild(
                        starsRoot,
                        $"Star{starIndex + 1}");
                    Anchor(
                        starRect,
                        new Vector2(0.5f, 0.5f),
                        new Vector2(46f, 46f));
                    starRect.anchoredPosition = new Vector2(
                        (starIndex - 1) * 44f,
                        0f);
                    Image starImage = starRect.gameObject.AddComponent<Image>();
                    starImage.sprite = emptyStarSprite;
                    starImage.preserveAspect = true;
                    starImage.color = Color.white;
                    starImage.raycastTarget = false;
                    starImages[starIndex] = starImage;
                }

                Button button = root.gameObject.AddComponent<Button>();
                button.targetGraphic = visual;
                button.transition = Selectable.Transition.ColorTint;
                button.colors = CreateButtonColors();
                button.navigation = new Navigation
                {
                    mode = Navigation.Mode.None,
                };

                FrontEndLevelNodeView node =
                    root.gameObject.AddComponent<FrontEndLevelNodeView>();
                node.ConfigureForSetup(
                    levelNumber,
                    button,
                    visual,
                    glow,
                    label,
                    lockImage,
                    starImages,
                    filledStarSprite,
                    emptyStarSprite);
                nodes[index] = node;
            }

            return nodes;
        }

        private static void ConfigureNavigationUnderlay(
            RectTransform frontEndRoot,
            RectTransform navigation)
        {
            RectTransform underlay = GetOrCreateUiChild(
                frontEndRoot,
                "BottomNavigationUnderlay");
            underlay.anchorMin = Vector2.zero;
            underlay.anchorMax = new Vector2(1f, 0f);
            underlay.pivot = new Vector2(0.5f, 0f);
            underlay.anchoredPosition = Vector2.zero;
            underlay.sizeDelta = new Vector2(0f, NavigationHeight + 64f);
            navigation.parent.SetAsLastSibling();
            underlay.SetSiblingIndex(Mathf.Max(
                0,
                navigation.parent.GetSiblingIndex() - 1));

            FrontEndRoundedRectangleGraphic rounded =
                GetOrAddComponent<FrontEndRoundedRectangleGraphic>(
                    underlay.gameObject);
            rounded.ConfigureForSetup(
                NavigationBackground,
                NavigationCornerRadius,
                false);
        }

        private static NavigationSetupResult ConfigureBottomNavigation(
            RectTransform root,
            RectTransform frontEndRoot,
            TMP_FontAsset font,
            Sprite shopIcon,
            Sprite homeIcon,
            Sprite challengeIcon)
        {
            RectTransform navigationRect = GetOrCreateUiChild(
                root,
                "BottomNavigation");
            navigationRect.anchorMin = Vector2.zero;
            navigationRect.anchorMax = new Vector2(1f, 0f);
            navigationRect.pivot = new Vector2(0.5f, 0f);
            navigationRect.anchoredPosition = Vector2.zero;
            navigationRect.sizeDelta = new Vector2(0f, NavigationHeight);
            navigationRect.SetAsLastSibling();
            Image background = GetOrAddComponent<Image>(
                navigationRect.gameObject);
            background.color = Color.clear;
            background.raycastTarget = true;
            ConfigureNavigationUnderlay(frontEndRoot, navigationRect);
            HorizontalLayoutGroup layout =
                GetOrAddComponent<HorizontalLayoutGroup>(
                    navigationRect.gameObject);
            layout.padding = new RectOffset(22, 22, 10, 10);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            string[] names = { "Shop", "Home", "Challenge" };
            string[] labels = { "SHOP", "HOME", "CHALLENGE" };
            Sprite[] sprites = { shopIcon, homeIcon, challengeIcon };
            var buttons = new Button[3];
            var plates = new Image[3];
            var icons = new Image[3];
            var texts = new TMP_Text[3];
            for (int index = 0; index < names.Length; index++)
            {
                RectTransform tab = GetOrCreateUiChild(
                    navigationRect,
                    $"{names[index]}Tab");
                LayoutElement tabLayout = GetOrAddComponent<LayoutElement>(
                    tab.gameObject);
                tabLayout.flexibleWidth = 1f;
                tabLayout.minHeight = NavigationHeight - 20f;
                Image target = GetOrAddComponent<Image>(tab.gameObject);
                target.color = new Color(1f, 1f, 1f, 0.001f);
                target.raycastTarget = true;
                Button button = GetOrAddComponent<Button>(tab.gameObject);
                button.targetGraphic = target;
                button.transition = Selectable.Transition.ColorTint;
                button.colors = CreateButtonColors();
                button.navigation = new Navigation
                {
                    mode = Navigation.Mode.None,
                };

                RectTransform plateRect = GetOrCreateUiChild(tab, "ActivePlate");
                Stretch(plateRect);
                bool active = index == 1;
                plateRect.offsetMin = active
                    ? new Vector2(6f, -4f)
                    : new Vector2(14f, 4f);
                plateRect.offsetMax = active
                    ? new Vector2(-6f, SelectedTabTopExtension)
                    : new Vector2(-14f, -4f);
                Image plate = GetOrAddComponent<Image>(plateRect.gameObject);
                plate.sprite = null;
                plate.preserveAspect = false;
                plate.color = Color.clear;
                plate.raycastTarget = false;

                RectTransform roundedFillRect = GetOrCreateUiChild(
                    plateRect,
                    "RoundedFill");
                Stretch(roundedFillRect);
                roundedFillRect.SetAsFirstSibling();
                GetOrAddComponent<FrontEndRoundedRectangleGraphic>(
                    roundedFillRect.gameObject)
                    .ConfigureForSetup(
                        active
                            ? new Color32(89, 34, 10, 245)
                            : Color.clear,
                        24f,
                        true);
                roundedFillRect.gameObject.SetActive(true);

                RectTransform iconRect = GetOrCreateUiChild(tab, "Icon");
                Anchor(
                    iconRect,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(76f, 76f));
                iconRect.anchoredPosition = new Vector2(0f, 28f);
                if (active)
                {
                    iconRect.anchoredPosition = new Vector2(0f, 34f);
                }
                Image icon = GetOrAddComponent<Image>(iconRect.gameObject);
                icon.sprite = sprites[index];
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                RectTransform labelRect = GetOrCreateUiChild(tab, "Label");
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 0f);
                labelRect.pivot = new Vector2(0.5f, 0f);
                labelRect.anchoredPosition = new Vector2(0f, 8f);
                if (active)
                {
                    labelRect.anchoredPosition = new Vector2(0f, 12f);
                }
                labelRect.sizeDelta = new Vector2(0f, 44f);
                TMP_Text text = ConfigureText(
                    labelRect,
                    labels[index],
                    font,
                    27f,
                    index == 1 ? Color.white : SecondaryText,
                    TextAlignmentOptions.Center);

                buttons[index] = button;
                plates[index] = plate;
                icons[index] = icon;
                texts[index] = text;
            }

            return new NavigationSetupResult(buttons, plates, icons, texts);
        }

        private static Button ConfigurePlayButton(
            Transform parent,
            string name,
            string label,
            TMP_FontAsset font,
            Sprite sprite,
            Vector2 anchor,
            Vector2 size)
        {
            RectTransform rect = GetOrCreateUiChild(parent, name);
            Anchor(rect, anchor, size);
            Image image = GetOrAddComponent<Image>(rect.gameObject);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;
            Button button = GetOrAddComponent<Button>(rect.gameObject);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = CreateButtonColors();
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.None,
            };

            RectTransform labelRect = GetOrCreateUiChild(rect, "Label");
            Stretch(labelRect);
            labelRect.offsetMin = new Vector2(30f, 24f);
            labelRect.offsetMax = new Vector2(-30f, -24f);
            ConfigureText(
                labelRect,
                label,
                font,
                56f,
                PrimaryText,
                TextAlignmentOptions.Center);
            FrontEndPulseAnimator oldButtonPulse =
                rect.GetComponent<FrontEndPulseAnimator>();
            if (oldButtonPulse != null)
            {
                Undo.DestroyObjectImmediate(oldButtonPulse);
            }

            FrontEndPulseAnimator pulse =
                GetOrAddComponent<FrontEndPulseAnimator>(
                    labelRect.gameObject);
            pulse.ConfigureForSetup(
                labelRect,
                null,
                0.78f,
                0.045f,
                1f,
                1f,
                name == "ChallengePlayButton" ? 0.5f : 0f);
            return button;
        }

        private static CanvasGroup ConfigurePage(
            RectTransform root,
            string name)
        {
            RectTransform page = GetOrCreateUiChild(root, name);
            page.anchorMin = Vector2.zero;
            page.anchorMax = Vector2.one;
            page.offsetMin = new Vector2(0f, NavigationHeight);
            // Real mask boundary below the shared HUD (mirrors the bottom
            // nav inset above) -- without this, ShopPage's own RectMask2D
            // extended all the way to the safe area's top edge, so
            // scrolled cards stayed visible in the gaps around the HUD
            // icons instead of being clipped before reaching them.
            page.offsetMax = new Vector2(0f, -HudZoneHeight);
            CanvasGroup group = GetOrAddComponent<CanvasGroup>(page.gameObject);
            group.alpha = name == "HomePage" ? 1f : 0f;
            group.interactable = name == "HomePage";
            group.blocksRaycasts = name == "HomePage";
            return group;
        }

        private static CanvasGroup ConfigureFullBleedChallengePage(
            RectTransform root,
            RectTransform previousParent)
        {
            Transform existing = root.Find("ChallengePage");
            if (existing == null)
            {
                existing = previousParent.Find("ChallengePage");
                if (existing != null)
                {
                    Undo.SetTransformParent(
                        existing,
                        root,
                        "Move Challenge Page Outside Safe Area");
                }
            }

            RectTransform page;
            if (existing == null)
            {
                page = CreateUiChild(root, "ChallengePage");
            }
            else if (existing is RectTransform existingRect)
            {
                page = existingRect;
            }
            else
            {
                throw new InvalidOperationException(
                    "Existing ChallengePage is not a UI RectTransform.");
            }
            Stretch(page);
            Transform background = root.Find("BackgroundArtwork");
            page.SetSiblingIndex(background != null ? 1 : 0);
            previousParent.SetAsLastSibling();

            CanvasGroup group = GetOrAddComponent<CanvasGroup>(
                page.gameObject);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        internal static TMP_Text ConfigureText(
            RectTransform rect,
            string value,
            TMP_FontAsset font,
            float size,
            Color color,
            TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(
                rect.gameObject);
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(18f, size * 0.62f);
            text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        internal static ColorBlock CreateButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(255, 225, 190, 255);
            colors.pressedColor = new Color32(220, 142, 84, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(90, 63, 50, 150);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            return colors;
        }

        internal static Sprite EnsureUiSprite(string path)
        {
            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path)
                as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"Frontend texture is missing or invalid: {path}");
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
                    $"Unity could not load frontend Sprite: {path}");
        }

        private static void Validate(
            RectTransform root,
            FrontEndPresenter presenter,
            int levelCount,
            Sprite shopIcon,
            Sprite homeIcon,
            Sprite challengeIcon,
            Sprite nodeSprite,
            Sprite nodeLockSprite,
            Sprite filledStarSprite,
            Sprite emptyStarSprite,
            Sprite homeBackground,
            Sprite homeLogo)
        {
            if (root.parent == null
                || root.GetSiblingIndex() != root.parent.childCount - 1
                || presenter.Controller == null
                || presenter.Controller.ProgressCloudServices == null
                || presenter.ScreenTransition == null
                || presenter.ScreenTransition.OverlayCanvasGroup == null
                || presenter.FrontEndCanvasGroup == null
                || presenter.HomePage == null
                || presenter.ShopPage == null
                || presenter.ChallengePage == null
                || presenter.BackgroundArtwork == null
                || presenter.BackgroundArtwork.sprite != homeBackground
                || presenter.HomeLogo == null
                || presenter.HomeLogo.sprite != homeLogo
                || presenter.HomePlayButton == null
                || presenter.ChallengePlayButton == null
                || presenter.HomePlayButton
                    .transform.Find("Label")
                    .GetComponent<FrontEndPulseAnimator>() == null
                || presenter.ChallengePlayButton
                    .transform.Find("Label")
                    .GetComponent<FrontEndPulseAnimator>() == null
                || presenter.ChallengeScrollRect == null
                || presenter.LevelNodes.Count != levelCount
                || presenter.PathConnectors.Count != levelCount - 1)
            {
                throw new InvalidOperationException(
                    "Frontend hierarchy or serialized references are incomplete.");
            }

            if (presenter.ChallengePage.transform.parent != root
                || presenter.ChallengePage.transform
                    .Find("ChallengeHeader") != null)
            {
                throw new InvalidOperationException(
                    "Challenge must be full-bleed and header-free.");
            }

            Transform navigationUnderlay = root.Find(
                "BottomNavigationUnderlay");
            if (navigationUnderlay == null
                || navigationUnderlay
                    .GetComponent<FrontEndRoundedRectangleGraphic>() == null)
            {
                throw new InvalidOperationException(
                    "Bottom navigation full-bleed rounded underlay is missing.");
            }

            Sprite[] expectedIcons = { shopIcon, homeIcon, challengeIcon };
            Transform navigation = RequireChild(
                root,
                "SafeAreaContent/BottomNavigation");
            string[] tabNames = { "ShopTab", "HomeTab", "ChallengeTab" };
            for (int index = 0; index < tabNames.Length; index++)
            {
                Image icon = RequireChild(
                        navigation,
                        $"{tabNames[index]}/Icon")
                    .GetComponent<Image>();
                Image plate = RequireChild(
                        navigation,
                        $"{tabNames[index]}/ActivePlate")
                    .GetComponent<Image>();
                if (icon == null
                    || icon.sprite != expectedIcons[index]
                    || plate == null
                    || plate.sprite != null)
                {
                    throw new InvalidOperationException(
                        $"{tabNames[index]} artwork is not wired correctly.");
                }
            }

            for (int index = 0; index < presenter.LevelNodes.Count; index++)
            {
                FrontEndLevelNodeView node = presenter.LevelNodes[index];
                if (node == null
                    || node.LevelNumber != index + 1
                    || node.NodeImage == null
                    || node.NodeImage.sprite != nodeSprite
                    || node.SelectionGlow == null
                    || node.SelectionGlow
                        .GetComponent<FrontEndPulseAnimator>() != null
                    || node.LockImage == null
                    || node.LockImage.sprite != nodeLockSprite
                    || node.StarImages.Count != 3
                    || node.FilledStarSprite != filledStarSprite
                    || node.EmptyStarSprite != emptyStarSprite)
                {
                    throw new InvalidOperationException(
                        $"Challenge node {index + 1} is not wired correctly.");
                }
            }
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

        internal static RectTransform GetOrCreateUiChild(
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
                    $"Existing frontend object '{name}' is not UI.");
            }

            return CreateUiChild(parent, name);
        }

        internal static RectTransform CreateUiChild(
            Transform parent,
            string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Frontend UI");
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        internal static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null
                ? component
                : Undo.AddComponent<T>(gameObject);
        }

        internal static void ClearGeneratedChildren(Transform root)
        {
            while (root.childCount > 0)
            {
                GameObject child = root.GetChild(0).gameObject;
                // Disabling Graphics before the whole-hierarchy destroy
                // lets each one unregister from any RectMask2D ancestor
                // while its CanvasRenderer is still intact. Skipping this
                // let Undo.DestroyObjectImmediate tear down a Graphic and
                // its required CanvasRenderer in an order where the
                // Graphic's OnDisable ran after the CanvasRenderer was
                // already gone, throwing MissingComponentException.
                foreach (UnityEngine.UI.Graphic graphic
                    in child.GetComponentsInChildren<UnityEngine.UI.Graphic>(
                        true))
                {
                    // Earlier interrupted Shop setup passes could leave a
                    // custom quantity-pill Graphic without its required
                    // CanvasRenderer. Repair that malformed intermediate
                    // object before disabling it, otherwise even cleanup
                    // throws MissingComponentException and the idempotent
                    // rebuild can never recover.
                    if (graphic.GetComponent<CanvasRenderer>() == null)
                    {
                        Undo.AddComponent<CanvasRenderer>(graphic.gameObject);
                    }

                    graphic.enabled = false;
                }

                Undo.DestroyObjectImmediate(child);
            }
        }

        internal static void DestroyUiChildIfPresent(
            Transform parent,
            string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void Anchor(
            RectTransform rect,
            Vector2 anchor,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private readonly struct ChallengeSetupResult
        {
            public ChallengeSetupResult(
                ScrollRect scrollRect,
                Button playButton,
                TMP_Text playLabel,
                FrontEndLevelNodeView[] nodes,
                Image[] connectors)
            {
                ScrollRect = scrollRect;
                PlayButton = playButton;
                PlayLabel = playLabel;
                Nodes = nodes;
                Connectors = connectors;
            }

            public ScrollRect ScrollRect { get; }
            public Button PlayButton { get; }
            public TMP_Text PlayLabel { get; }
            public FrontEndLevelNodeView[] Nodes { get; }
            public Image[] Connectors { get; }
        }

        private readonly struct NavigationSetupResult
        {
            public NavigationSetupResult(
                Button[] buttons,
                Image[] plates,
                Image[] icons,
                TMP_Text[] labels)
            {
                Buttons = buttons;
                Plates = plates;
                Icons = icons;
                Labels = labels;
            }

            public Button[] Buttons { get; }
            public Image[] Plates { get; }
            public Image[] Icons { get; }
            public TMP_Text[] Labels { get; }
        }
    }
}
