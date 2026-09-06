using Cutrium.Gameplay.Economy;
using Cutrium.Presentation.Feedback;
using Cutrium.Presentation.Frontend;
using Cutrium.Presentation.Shop;
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
    /// Builds the Shop tab's visual content and wires a-la-carte power-up
    /// purchases to the central Coin wallet and persistent inventory.
    internal static class ShopContentSceneSetup
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";
        private const string CatalogPath =
            "Assets/Cutrium/Content/Shop/ShopCatalog.asset";

        private const string RemoveAdsBackgroundPath =
            "Assets/Cutrium/Content/Gui/ADS-Remove-Background.png";
        private const string RemoveAdsIconPath =
            "Assets/Cutrium/Content/Gui/ADS-Remove.png";
        private const string BundleBackgroundPath =
            "Assets/Cutrium/Content/Gui/BundleBackground.png";
        private const string SaleBadgePath =
            "Assets/Cutrium/Content/Gui/SaleBadge.png";
        private const string GoldBackgroundPath =
            "Assets/Cutrium/Content/Gui/GoldBackground.png";
        internal const string FreezeSkillPath =
            "Assets/Cutrium/Content/Gui/FreezeSkill.png";
        internal const string InstantBarrierSkillPath =
            "Assets/Cutrium/Content/Gui/InstantBarrierSkill.png";
        internal const string GravityWellSkillPath =
            "Assets/Cutrium/Content/Gui/GravityWellSkill.png";
        private const string ButtonBackgroundPath =
            "Assets/Cutrium/Content/Gui/GeneralButtonBackground.png";
        private const string WatchAdsCameraPath =
            "Assets/Cutrium/Content/Gui/WatchADSCamera.png";
        private const string CoinStackPathFormat =
            "Assets/Cutrium/Content/Gui/CoinStackL{0}.png";

        private const int GoldColumnCount = 3;
        private const float GoldRowSpacing = 16f;
        private const int GoldRowVerticalInset = 8;
        private const float BundleHorizontalInset = 18f;
        private const float RemoveAdsHorizontalInset = 18f;
        private const float RemoveAdsVerticalInset = 8f;
        private const float GoldArtworkInset = 6f;
        private const float SectionLabelHeight = 68f;
        private const float ContentSpacing = 24f;
        private const float ContentSidePadding = 28f;
        // Clearing the shared HUD is now the ScrollRect's own mask
        // boundary (ShopPage is inset from the top by
        // FrontEndSceneSetup.HudZoneHeight) so scrolled cards are actually
        // clipped there, not just drawn underneath the HUD icons. This is
        // just breathing room inside that already-safe viewport.
        private const float ContentTopPadding = 24f;
        private const float ContentBottomPadding = 52f;
        private const int SkillColumnCount = 3;
        private const float SkillRowSpacing = 16f;
        private const int SkillRowVerticalInset = 8;
        private const float ShopFeedbackHeight = 48f;

        private static readonly Color PrimaryText =
            new Color32(255, 230, 191, 255);
        private static readonly Color SecondaryText =
            new Color32(188, 126, 83, 255);
        private static readonly Color SectionText =
            new Color32(97, 48, 24, 255);
        private static readonly Color AmountFillColor =
            Color.white;
        private static readonly Color AmountStrokeColor =
            new Color32(104, 48, 17, 235);
        private static readonly Color AmountShadowColor =
            new Color32(0, 0, 0, 150);
        private static readonly Color OriginalPriceColor =
            new Color32(214, 176, 150, 220);
        private static readonly Color SaleBadgeTextColor = Color.white;
        private static readonly Color RewardedGoldTint =
            new Color32(255, 220, 180, 255);
        private static readonly Color RewardedGlowColor =
            new Color32(255, 155, 55, 255);

        [MenuItem("Cutrium/Setup/Apply Shop Visual Parity")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new System.InvalidOperationException(
                    "Exit Play Mode before applying Shop visual setup.");
            }

            Scene scene = OpenVerticalSliceScene();
            FrontEndPresenter presenter = null;
            GameObject verticalSliceRoot = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "VerticalSliceRoot")
                {
                    verticalSliceRoot = root;
                }

                presenter = root.GetComponentInChildren<FrontEndPresenter>(
                    true);
                if (presenter != null)
                {
                    break;
                }
            }

            TMP_FontAsset font = presenter?.HomePlayButton
                ?.transform.Find("Label")
                ?.GetComponent<TMP_Text>()
                ?.font;
            if (presenter?.ShopPage == null || font == null)
            {
                throw new System.InvalidOperationException(
                    "Shop setup requires the configured frontend and its font.");
            }

            CloudServicesBootstrap cloudServices = verticalSliceRoot
                ?.GetComponentInChildren<CloudServicesBootstrap>(true);
            FirstPlayableController controller = verticalSliceRoot
                ?.GetComponentInChildren<FirstPlayableController>(true);
            FeedbackAudioPresenter feedbackAudio = verticalSliceRoot
                ?.GetComponentInChildren<FeedbackAudioPresenter>(true);
            if (cloudServices == null || controller == null
                || feedbackAudio == null)
            {
                throw new System.InvalidOperationException(
                    "Shop setup requires Cloud services, the gameplay "
                    + "controller, and FeedbackAudioPresenter.");
            }

            Configure(
                (RectTransform)presenter.ShopPage.transform,
                font,
                cloudServices,
                controller,
                feedbackAudio);
            PowerUpInventoryHudPresenter inventoryHud =
                FrontEndSceneSetup.ConfigureHomePowerInventoryForSetup(
                    (RectTransform)presenter.HomePage.transform,
                    font,
                    cloudServices,
                    presenter);
            EditorUtility.SetDirty(presenter.ShopPage);
            EditorUtility.SetDirty(inventoryHud);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException(
                    "Unity could not save the Shop visual setup.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Shop power-up purchases and Home inventory HUD applied "
                + "alongside the existing responsive offers.");
        }

        [MenuItem("Cutrium/Setup/Apply Task 04 Power-Up Inventory")]
        private static void ApplyTask04PowerUpInventory() => Apply();

        public static void Configure(
            RectTransform shopPage,
            TMP_FontAsset font,
            CloudServicesBootstrap cloudServices,
            FirstPlayableController controller,
            FeedbackAudioPresenter feedbackAudio)
        {
            if (shopPage == null || font == null || cloudServices == null)
            {
                throw new System.ArgumentNullException(
                    "Shop setup dependencies cannot be null.");
            }

            ShopCatalog catalog = EnsureCatalog();

            Sprite removeAdsBackground = FrontEndSceneSetup.EnsureUiSprite(
                RemoveAdsBackgroundPath);
            Sprite removeAdsIcon = FrontEndSceneSetup.EnsureUiSprite(
                RemoveAdsIconPath);
            Sprite bundleBackground = FrontEndSceneSetup.EnsureUiSprite(
                BundleBackgroundPath);
            Sprite saleBadge = FrontEndSceneSetup.EnsureUiSprite(SaleBadgePath);
            Sprite goldBackground = FrontEndSceneSetup.EnsureUiSprite(
                GoldBackgroundPath);
            Sprite buttonBackground = FrontEndSceneSetup.EnsureUiSprite(
                ButtonBackgroundPath);
            Sprite watchAdsCamera = FrontEndSceneSetup.EnsureUiSprite(
                WatchAdsCameraPath);
            Sprite coinIcon = FrontEndSceneSetup.EnsureUiSprite(
                string.Format(CoinStackPathFormat, 1));

            RectTransform scrollRoot = FrontEndSceneSetup.GetOrCreateUiChild(
                shopPage,
                "ShopScroll");
            FrontEndSceneSetup.Stretch(scrollRoot);
            Image scrollSurface = FrontEndSceneSetup.GetOrAddComponent<Image>(
                scrollRoot.gameObject);
            scrollSurface.sprite = null;
            scrollSurface.color = Color.clear;
            scrollSurface.raycastTarget = true;

            RectTransform viewport = FrontEndSceneSetup.GetOrCreateUiChild(
                scrollRoot,
                "Viewport");
            FrontEndSceneSetup.Stretch(viewport);
            Image viewportImage = FrontEndSceneSetup.GetOrAddComponent<Image>(
                viewport.gameObject);
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImage.raycastTarget = true;
            RectMask2D mask = FrontEndSceneSetup.GetOrAddComponent<RectMask2D>(
                viewport.gameObject);
            mask.padding = Vector4.zero;

            RectTransform content = FrontEndSceneSetup.GetOrCreateUiChild(
                viewport,
                "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
            FrontEndSceneSetup.ClearGeneratedChildren(content);

            VerticalLayoutGroup layout =
                FrontEndSceneSetup.GetOrAddComponent<VerticalLayoutGroup>(
                    content.gameObject);
            layout.padding = new RectOffset(
                (int)ContentSidePadding,
                (int)ContentSidePadding,
                (int)ContentTopPadding,
                (int)ContentBottomPadding);
            layout.spacing = ContentSpacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter =
                FrontEndSceneSetup.GetOrAddComponent<ContentSizeFitter>(
                    content.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildRemoveAdsCard(
                content,
                font,
                removeAdsBackground,
                removeAdsIcon,
                buttonBackground,
                catalog.RemoveAdsPriceLabel);

            BuildSectionLabel(content, "BundlesLabel", "BUNDLES", font);
            for (int index = 0; index < catalog.Bundles.Count; index++)
            {
                ShopBundleOffer offer = catalog.Bundles[index];
                Sprite bundleCoin = FrontEndSceneSetup.EnsureUiSprite(
                    string.Format(
                        CoinStackPathFormat,
                        GetBundleCoinStackLevel(offer.CoinAmount)));
                BuildBundleCard(
                    content,
                    $"BundleCard_{index + 1:00}",
                    font,
                    bundleBackground,
                    bundleCoin,
                    saleBadge,
                    buttonBackground,
                    offer);
            }

            // An older pass built one full-width card per skill, directly
            // under content. Skills now live in a 3-per-row grid (like Gold)
            // nested under SkillRow_XX instead -- remove any leftover
            // top-level card so it doesn't sit around as a stale duplicate.
            for (int index = 0; index < catalog.PowerUpOffers.Count; index++)
            {
                FrontEndSceneSetup.DestroyUiChildIfPresent(
                    content,
                    $"PowerUpCard_{catalog.PowerUpOffers[index].Kind}");
            }

            BuildSectionLabel(content, "SkillsLabel", "SKILLS", font);
            PowerUpShopPresenter.ItemView[] powerUpViews = BuildSkillGrid(
                content,
                font,
                goldBackground,
                buttonBackground,
                coinIcon,
                catalog);

            RectTransform feedbackRect =
                FrontEndSceneSetup.GetOrCreateUiChild(
                    content,
                    "SkillsFeedback");
            feedbackRect.sizeDelta = new Vector2(
                feedbackRect.sizeDelta.x,
                ShopFeedbackHeight);
            LayoutElement feedbackLayout =
                FrontEndSceneSetup.GetOrAddComponent<LayoutElement>(
                    feedbackRect.gameObject);
            feedbackLayout.preferredHeight = ShopFeedbackHeight;
            feedbackLayout.flexibleWidth = 1f;
            TMP_Text feedbackText = FrontEndSceneSetup.ConfigureText(
                feedbackRect,
                string.Empty,
                font,
                30f,
                PrimaryText,
                TextAlignmentOptions.Center);

            BuildSectionLabel(content, "GoldLabel", "GOLD", font);
            BuildGoldGrid(
                content,
                font,
                goldBackground,
                buttonBackground,
                watchAdsCamera,
                catalog);

            // Skills purchases move to the very end of the scroll content, so
            // browsing starts with the higher-intent Bundles/Gold offers. Move
            // every already-existing Skills element there explicitly -- an
            // idempotent re-run finds these objects at whatever sibling index
            // an earlier setup pass left them at and would otherwise leave
            // them in their old position ahead of Gold.
            content.Find("GoldLabel")?.SetAsLastSibling();
            for (int row = 0; ; row++)
            {
                Transform goldRow = content.Find($"GoldRow_{row + 1:00}");
                if (goldRow == null)
                {
                    break;
                }

                goldRow.SetAsLastSibling();
            }

            content.Find("SkillsLabel")?.SetAsLastSibling();
            for (int row = 0; ; row++)
            {
                Transform skillRow = content.Find($"SkillRow_{row + 1:00}");
                if (skillRow == null)
                {
                    break;
                }

                skillRow.SetAsLastSibling();
            }

            feedbackRect.SetAsLastSibling();

            ScrollRect scrollRect =
                FrontEndSceneSetup.GetOrAddComponent<ScrollRect>(
                    scrollRoot.gameObject);
            scrollRect.content = content;
            scrollRect.viewport = viewport;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 72f;
            scrollRect.verticalNormalizedPosition = 1f;

            PowerUpShopPresenter powerUpShop =
                FrontEndSceneSetup.GetOrAddComponent<PowerUpShopPresenter>(
                    scrollRoot.gameObject);
            powerUpShop.ConfigureForSetup(
                catalog,
                cloudServices,
                controller,
                feedbackAudio,
                feedbackText,
                powerUpViews);

            Validate(
                scrollRect,
                content,
                catalog,
                powerUpShop,
                removeAdsBackground,
                bundleBackground,
                goldBackground);
            EditorUtility.SetDirty(powerUpShop);
        }

        private static ShopCatalog EnsureCatalog()
        {
            ShopCatalog catalog =
                AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            bool created = catalog == null;
            if (created)
            {
                string folder = System.IO.Path.GetDirectoryName(CatalogPath)
                    ?.Replace('\\', '/');
                EnsureFolder(folder);
                catalog = ScriptableObject.CreateInstance<ShopCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            Sprite freeze = FrontEndSceneSetup.EnsureUiSprite(FreezeSkillPath);
            Sprite instant = FrontEndSceneSetup.EnsureUiSprite(
                InstantBarrierSkillPath);
            Sprite gravity = FrontEndSceneSetup.EnsureUiSprite(
                GravityWellSkillPath);

            var freezeAccent = new Color32(64, 158, 214, 255);
            var instantAccent = new Color32(214, 84, 46, 255);
            var gravityAccent = new Color32(150, 78, 199, 255);

            var bundles = new[]
            {
                new ShopBundleOffer(
                    1500,
                    35,
                    "$4.99",
                    "$7.99",
                    new[]
                    {
                        new ShopBundleSkillEntry(freeze, 2, freezeAccent),
                        new ShopBundleSkillEntry(instant, 2, instantAccent),
                        new ShopBundleSkillEntry(gravity, 1, gravityAccent),
                    }),
                new ShopBundleOffer(
                    4000,
                    0,
                    "$9.99",
                    "$9.99",
                    new[]
                    {
                        new ShopBundleSkillEntry(freeze, 5, freezeAccent),
                        new ShopBundleSkillEntry(instant, 5, instantAccent),
                        new ShopBundleSkillEntry(gravity, 3, gravityAccent),
                    }),
            };

            var goldOffers = new[]
            {
                new ShopGoldOffer(
                    100,
                    FrontEndSceneSetup.EnsureUiSprite(
                        string.Format(CoinStackPathFormat, 1)),
                    string.Empty,
                    true),
                new ShopGoldOffer(
                    200,
                    FrontEndSceneSetup.EnsureUiSprite(
                        string.Format(CoinStackPathFormat, 2)),
                    "$2.99",
                    false),
                new ShopGoldOffer(
                    500,
                    FrontEndSceneSetup.EnsureUiSprite(
                        string.Format(CoinStackPathFormat, 3)),
                    "$3.99",
                    false),
                new ShopGoldOffer(
                    1000,
                    FrontEndSceneSetup.EnsureUiSprite(
                        string.Format(CoinStackPathFormat, 4)),
                    "$5.99",
                    false),
                new ShopGoldOffer(
                    2000,
                    FrontEndSceneSetup.EnsureUiSprite(
                        string.Format(CoinStackPathFormat, 5)),
                    "$9.99",
                    false),
                new ShopGoldOffer(
                    5000,
                    FrontEndSceneSetup.EnsureUiSprite(
                        string.Format(CoinStackPathFormat, 6)),
                    "$14.99",
                    false),
            };

            var powerUpOffers = new[]
            {
                new ShopPowerUpOffer(
                    PowerUpKind.FreezePulse,
                    "FREEZE PULSE",
                    freeze,
                    1,
                    200,
                    freezeAccent),
                new ShopPowerUpOffer(
                    PowerUpKind.InstantBarrier,
                    "INSTANT BARRIER",
                    instant,
                    1,
                    250,
                    instantAccent),
                new ShopPowerUpOffer(
                    PowerUpKind.GravityWell,
                    "GRAVITY WELL",
                    gravity,
                    1,
                    250,
                    gravityAccent),
            };

            if (created)
            {
                catalog.ConfigureForSetup(
                    "$25.99",
                    bundles,
                    goldOffers,
                    powerUpOffers);
            }
            else if (catalog.PowerUpOffers.Count == 0)
            {
                catalog.ConfigurePowerUpsForSetup(powerUpOffers);
            }

            if (created || catalog.PowerUpOffers.Count == powerUpOffers.Length)
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
            }

            return catalog;
        }

        private static void BuildRemoveAdsCard(
            Transform parent,
            TMP_FontAsset font,
            Sprite background,
            Sprite icon,
            Sprite buttonSprite,
            string priceLabel)
        {
            RectTransform card = FrontEndSceneSetup.GetOrCreateUiChild(
                parent,
                "RemoveAdsCard");
            ConfigureResponsiveHeight(
                card,
                background,
                horizontalPadding: RemoveAdsHorizontalInset * 2f,
                verticalPadding: RemoveAdsVerticalInset * 2f);
            FrontEndSceneSetup.ClearGeneratedChildren(card);
            RemoveComponentIfPresent<Image>(card.gameObject);

            RectTransform visual = FrontEndSceneSetup.GetOrCreateUiChild(
                card,
                "Visual");
            visual.anchorMin = Vector2.zero;
            visual.anchorMax = Vector2.one;
            visual.pivot = new Vector2(0.5f, 0.5f);
            visual.offsetMin = new Vector2(
                RemoveAdsHorizontalInset,
                RemoveAdsVerticalInset);
            visual.offsetMax = new Vector2(
                -RemoveAdsHorizontalInset,
                -RemoveAdsVerticalInset);
            Image backgroundImage = FrontEndSceneSetup.GetOrAddComponent<Image>(
                visual.gameObject);
            backgroundImage.sprite = background;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
            backgroundImage.color = Color.white;
            MakeCardClickable(visual);

            RectTransform iconRect = FrontEndSceneSetup.GetOrCreateUiChild(
                visual,
                "Icon");
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(28f, 0f);
            iconRect.sizeDelta = new Vector2(148f, 148f);
            Image iconImage = FrontEndSceneSetup.GetOrAddComponent<Image>(
                iconRect.gameObject);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            RectTransform titleRect = FrontEndSceneSetup.GetOrCreateUiChild(
                visual,
                "Title");
            titleRect.anchorMin = new Vector2(0.18f, 0.5f);
            titleRect.anchorMax = new Vector2(0.72f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 26f);
            titleRect.sizeDelta = new Vector2(0f, 58f);
            TMP_Text title = FrontEndSceneSetup.ConfigureText(
                titleRect,
                "REMOVE ADS",
                font,
                46f,
                PrimaryText,
                TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;

            RectTransform durationRect =
                FrontEndSceneSetup.GetOrCreateUiChild(
                    visual,
                    "Duration");
            durationRect.anchorMin = new Vector2(0.18f, 0.5f);
            durationRect.anchorMax = new Vector2(0.72f, 0.5f);
            durationRect.pivot = new Vector2(0.5f, 0.5f);
            durationRect.anchoredPosition = new Vector2(0f, -30f);
            durationRect.sizeDelta = new Vector2(0f, 42f);
            TMP_Text duration = FrontEndSceneSetup.ConfigureText(
                durationRect,
                "FOR 24 HOURS",
                font,
                28f,
                SecondaryText,
                TextAlignmentOptions.Center);
            duration.fontStyle = FontStyles.Bold;

            BuildPriceButton(
                visual,
                "PriceButton",
                priceLabel,
                font,
                buttonSprite,
                new Vector2(1f, 0.5f),
                new Vector2(-28f, 0f),
                new Vector2(210f, 88f),
                38f);
        }

        private static void BuildBundleCard(
            Transform parent,
            string name,
            TMP_FontAsset font,
            Sprite background,
            Sprite coinSprite,
            Sprite saleBadgeSprite,
            Sprite buttonSprite,
            ShopBundleOffer offer)
        {
            RectTransform card = FrontEndSceneSetup.GetOrCreateUiChild(
                parent,
                name);
            ConfigureResponsiveHeight(
                card,
                background,
                horizontalPadding: BundleHorizontalInset * 2f);
            FrontEndSceneSetup.ClearGeneratedChildren(card);
            RemoveComponentIfPresent<Image>(card.gameObject);

            RectTransform visual = FrontEndSceneSetup.GetOrCreateUiChild(
                card,
                "Visual");
            visual.anchorMin = Vector2.zero;
            visual.anchorMax = Vector2.one;
            visual.pivot = new Vector2(0.5f, 0.5f);
            visual.offsetMin = new Vector2(BundleHorizontalInset, 0f);
            visual.offsetMax = new Vector2(-BundleHorizontalInset, 0f);
            Image backgroundImage = FrontEndSceneSetup.GetOrAddComponent<Image>(
                visual.gameObject);
            backgroundImage.sprite = background;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
            backgroundImage.color = Color.white;
            MakeCardClickable(visual);

            // The coin stack (sized to the offer's amount, see
            // GetBundleCoinStackLevel) owns the left half of the offer. The
            // amount is a true overlay so the coin and value read as one
            // visual unit.
            RectTransform coinIconRect = FrontEndSceneSetup.GetOrCreateUiChild(
                visual,
                "CoinIcon");
            coinIconRect.anchorMin = new Vector2(0.015f, 0.04f);
            coinIconRect.anchorMax = new Vector2(0.5f, 0.96f);
            coinIconRect.pivot = new Vector2(0.5f, 0.5f);
            coinIconRect.anchoredPosition = Vector2.zero;
            coinIconRect.sizeDelta = Vector2.zero;
            Image coinIcon = FrontEndSceneSetup.GetOrAddComponent<Image>(
                coinIconRect.gameObject);
            coinIcon.sprite = coinSprite;
            coinIcon.preserveAspect = true;
            coinIcon.raycastTarget = false;

            RectTransform amountRect = FrontEndSceneSetup.GetOrCreateUiChild(
                visual,
                "Amount");
            amountRect.anchorMin = coinIconRect.anchorMin;
            amountRect.anchorMax = coinIconRect.anchorMax;
            amountRect.pivot = new Vector2(0.5f, 0.5f);
            amountRect.anchoredPosition = Vector2.zero;
            amountRect.sizeDelta = Vector2.zero;
            BuildStrokedAmountLabel(
                amountRect,
                offer.CoinAmount.ToString(),
                font,
                60f,
                TextAlignmentOptions.Center);

            // Stretch from the card's center line to the right edge. Each
            // skill receives an equal share instead of staying at 80 px.
            RectTransform skillRow = FrontEndSceneSetup.GetOrCreateUiChild(
                visual,
                "SkillRow");
            skillRow.anchorMin = new Vector2(0.5f, 1f);
            skillRow.anchorMax = new Vector2(1f, 1f);
            skillRow.pivot = new Vector2(1f, 1f);
            skillRow.offsetMin = new Vector2(8f, -176f);
            skillRow.offsetMax = new Vector2(-28f, -28f);
            HorizontalLayoutGroup skillLayout =
                FrontEndSceneSetup.GetOrAddComponent<HorizontalLayoutGroup>(
                    skillRow.gameObject);
            skillLayout.childAlignment = TextAnchor.MiddleCenter;
            skillLayout.spacing = 20f;
            skillLayout.childControlWidth = true;
            skillLayout.childControlHeight = true;
            skillLayout.childForceExpandWidth = true;
            skillLayout.childForceExpandHeight = true;

            FrontEndSceneSetup.ClearGeneratedChildren(skillRow);
            for (int index = 0; index < offer.Skills.Count; index++)
            {
                ShopBundleSkillEntry skill = offer.Skills[index];
                BuildSkillEntry(
                    skillRow,
                    $"Skill_{index + 1}",
                    font,
                    skill,
                    index);
            }

            if (offer.HasDiscount)
            {
                RectTransform originalPriceRect =
                    FrontEndSceneSetup.GetOrCreateUiChild(
                        visual,
                        "OriginalPrice");
                originalPriceRect.anchorMin = new Vector2(1f, 0f);
                originalPriceRect.anchorMax = new Vector2(1f, 0f);
                originalPriceRect.pivot = new Vector2(1f, 0f);
                originalPriceRect.anchoredPosition = new Vector2(-260f, 55f);
                originalPriceRect.sizeDelta = new Vector2(116f, 34f);
                TMP_Text originalPrice = FrontEndSceneSetup.ConfigureText(
                    originalPriceRect,
                    offer.OriginalPriceLabel,
                    font,
                    28f,
                    OriginalPriceColor,
                    TextAlignmentOptions.MidlineRight);
                originalPrice.fontStyle |= FontStyles.Strikethrough;
                originalPriceRect.gameObject.SetActive(true);
            }
            else
            {
                FrontEndSceneSetup.DestroyUiChildIfPresent(
                    visual,
                    "OriginalPrice");
            }

            BuildPriceButton(
                visual,
                "PriceButton",
                offer.PriceLabel,
                font,
                buttonSprite,
                new Vector2(1f, 0f),
                new Vector2(-28f, 28f),
                new Vector2(220f, 88f),
                38f);

            // Bigger and nudged up-left so it reads as a corner badge
            // pinned to the card rather than an icon sitting flush inside
            // it -- a small overflow past the card's own top-left edge is
            // fine here since ContentSpacing (24px) leaves room above it.
            RectTransform badgeRect = FrontEndSceneSetup.GetOrCreateUiChild(
                visual,
                "SaleBadge");
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = new Vector2(-14f, 14f);
            badgeRect.sizeDelta = new Vector2(136f, 136f);
            badgeRect.SetAsLastSibling();
            Image badgeImage = FrontEndSceneSetup.GetOrAddComponent<Image>(
                badgeRect.gameObject);
            badgeImage.sprite = saleBadgeSprite;
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;
            badgeRect.gameObject.SetActive(offer.HasDiscount);

            RectTransform badgeLabelRect =
                FrontEndSceneSetup.GetOrCreateUiChild(badgeRect, "Label");
            FrontEndSceneSetup.Anchor(
                badgeLabelRect,
                new Vector2(0.5f, 0.42f),
                new Vector2(116f, 96f));
            TMP_Text badgeLabel = FrontEndSceneSetup.ConfigureText(
                badgeLabelRect,
                $"-{offer.DiscountPercent}%\nOFF",
                font,
                30f,
                SaleBadgeTextColor,
                TextAlignmentOptions.Center);
            badgeLabel.textWrappingMode = TextWrappingModes.Normal;
            badgeLabel.lineSpacing = -14f;
        }

        private static void BuildSkillEntry(
            Transform parent,
            string name,
            TMP_FontAsset font,
            ShopBundleSkillEntry skill,
            int index)
        {
            RectTransform root = FrontEndSceneSetup.CreateUiChild(parent, name);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.zero;
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(128f, 146f);
            LayoutElement layoutElement =
                FrontEndSceneSetup.GetOrAddComponent<LayoutElement>(
                    root.gameObject);
            layoutElement.minWidth = 96f;
            layoutElement.preferredWidth = 128f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.minHeight = 128f;
            layoutElement.preferredHeight = 146f;
            layoutElement.flexibleHeight = 1f;

            RectTransform iconRect = FrontEndSceneSetup.GetOrCreateUiChild(
                root,
                "Icon");
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;
            Image icon = FrontEndSceneSetup.GetOrAddComponent<Image>(
                iconRect.gameObject);
            icon.sprite = skill.Icon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // root isn't guaranteed square (its layout element is stretched
            // by the row's HorizontalLayoutGroup), so an AspectRatioFitter
            // keeps Icon's own rect an exact centered square matching the
            // 1:1 skill artwork -- otherwise preserveAspect just letterboxes
            // inside a non-square rect and the badge below drifts off the
            // visible art depending on how much that rect got stretched.
            AspectRatioFitter iconAspect =
                FrontEndSceneSetup.GetOrAddComponent<AspectRatioFitter>(
                    iconRect.gameObject);
            iconAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            iconAspect.aspectRatio = 1f;

            // Clips the shine sweep below to Icon's own square bounds so it
            // never spills past the art's edges while sweeping in/out.
            RectMask2D iconMask = FrontEndSceneSetup.GetOrAddComponent<
                RectMask2D>(iconRect.gameObject);
            iconMask.padding = Vector4.zero;

            // The skill artwork already paints its own folded-corner badge
            // shape at bottom-right (a flat brown plate meant to hold a
            // label) -- no separate colored pill is needed on top of it,
            // just a plain white label sized and centered onto that shape.
            // Parented under Icon (not root) so the anchor fraction lands on
            // the icon's own corner regardless of how root got stretched.
            RectTransform quantityRect = FrontEndSceneSetup.GetOrCreateUiChild(
                iconRect,
                "QuantityLabel");
            FrontEndSceneSetup.Anchor(
                quantityRect,
                new Vector2(0.76f, 0.21f),
                new Vector2(58f, 54f));
            TMP_Text quantityLabel = FrontEndSceneSetup.ConfigureText(
                quantityRect,
                $"x{skill.Quantity}",
                font,
                30f,
                Color.white,
                TextAlignmentOptions.Center);
            quantityLabel.fontSizeMin = 20f;

            // The grow/shrink pulse read as distracting at this size; a
            // sun-glint sweep across the icon reads as "alive" without it.
            RemoveComponentIfPresent<FrontEndPulseAnimator>(root.gameObject);

            RectTransform shineRect = FrontEndSceneSetup.GetOrCreateUiChild(
                iconRect,
                "Shine");
            FrontEndSceneSetup.Stretch(shineRect);
            FrontEndSceneSetup.GetOrAddComponent<CanvasRenderer>(
                shineRect.gameObject);
            FrontEndShineSweepGraphic shineGraphic = FrontEndSceneSetup
                .GetOrAddComponent<FrontEndShineSweepGraphic>(
                    shineRect.gameObject);
            shineGraphic.ConfigureForSetup(
                new Color(1f, 1f, 1f, 0.55f),
                22f,
                0.16f);
            FrontEndShineSweepAnimator shineAnimator = FrontEndSceneSetup
                .GetOrAddComponent<FrontEndShineSweepAnimator>(
                    shineRect.gameObject);
            shineAnimator.ConfigureForSetup(
                shineGraphic,
                1.1f,
                4.9f,
                index * 0.33f);
            shineAnimator.enabled = true;
        }

        // Mirrors BuildGoldGrid's own row layout so Skills reads as a third
        // "3 tiles per row" section, matching Bundles/Gold instead of one
        // full-width card per skill.
        private static PowerUpShopPresenter.ItemView[] BuildSkillGrid(
            Transform parent,
            TMP_FontAsset font,
            Sprite background,
            Sprite buttonSprite,
            Sprite coinSprite,
            ShopCatalog catalog)
        {
            int count = catalog.PowerUpOffers.Count;
            int rowCount = Mathf.CeilToInt(count / (float)SkillColumnCount);
            var views = new PowerUpShopPresenter.ItemView[count];
            for (int row = 0; row < rowCount; row++)
            {
                RectTransform rowRect = FrontEndSceneSetup.GetOrCreateUiChild(
                    parent,
                    $"SkillRow_{row + 1:00}");
                ConfigureResponsiveHeight(
                    rowRect,
                    background,
                    SkillColumnCount,
                    SkillRowSpacing,
                    verticalPadding: SkillRowVerticalInset * 2f);

                HorizontalLayoutGroup rowLayout =
                    FrontEndSceneSetup.GetOrAddComponent<HorizontalLayoutGroup>(
                        rowRect.gameObject);
                rowLayout.spacing = SkillRowSpacing;
                rowLayout.padding = new RectOffset(
                    0,
                    0,
                    SkillRowVerticalInset,
                    SkillRowVerticalInset);
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = true;

                FrontEndSceneSetup.ClearGeneratedChildren(rowRect);
                int firstIndex = row * SkillColumnCount;
                int lastIndex = Mathf.Min(
                    firstIndex + SkillColumnCount - 1,
                    count - 1);
                for (int index = firstIndex; index <= lastIndex; index++)
                {
                    ShopPowerUpOffer offer = catalog.PowerUpOffers[index];
                    views[index] = BuildPowerUpCard(
                        rowRect,
                        $"PowerUpCard_{offer.Kind}",
                        font,
                        background,
                        buttonSprite,
                        coinSprite,
                        offer);
                }
            }

            for (int row = rowCount; ; row++)
            {
                Transform stale = parent.Find($"SkillRow_{row + 1:00}");
                if (stale == null)
                {
                    break;
                }

                FrontEndSceneSetup.DestroyUiChildIfPresent(
                    parent,
                    $"SkillRow_{row + 1:00}");
            }

            return views;
        }

        private static PowerUpShopPresenter.ItemView BuildPowerUpCard(
            Transform parent,
            string name,
            TMP_FontAsset font,
            Sprite background,
            Sprite buttonSprite,
            Sprite coinSprite,
            ShopPowerUpOffer offer)
        {
            RectTransform card = FrontEndSceneSetup.GetOrCreateUiChild(
                parent,
                name);
            RemoveComponentIfPresent<LayoutElement>(card.gameObject);
            RemoveComponentIfPresent<Image>(card.gameObject);

            RectTransform artwork = FrontEndSceneSetup.GetOrCreateUiChild(
                card,
                "Artwork");
            artwork.anchorMin = Vector2.zero;
            artwork.anchorMax = Vector2.one;
            artwork.pivot = new Vector2(0.5f, 0.5f);
            artwork.offsetMin = new Vector2(GoldArtworkInset, GoldArtworkInset);
            artwork.offsetMax = new Vector2(
                -GoldArtworkInset,
                -GoldArtworkInset);
            Image backgroundImage = FrontEndSceneSetup.GetOrAddComponent<Image>(
                artwork.gameObject);
            backgroundImage.sprite = background;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
            backgroundImage.color = Color.white;
            Button purchaseButton = MakeCardClickable(artwork);

            // Clean up elements the older one-card-per-skill layout built,
            // which no longer have a place in this compact grid tile.
            FrontEndSceneSetup.DestroyUiChildIfPresent(artwork, "IconPlate");
            FrontEndSceneSetup.DestroyUiChildIfPresent(artwork, "Title");
            FrontEndSceneSetup.DestroyUiChildIfPresent(
                artwork,
                "Description");

            // A square box (the tile itself is always square -- both Gold
            // and Skill rows share the same 3-column, square-background
            // grid) so preserveAspect renders the 1:1 skill artwork with no
            // letterboxing. Smaller than Gold's own coin box because the
            // skill art is full-bleed (no internal padding), so the same
            // bounding box would read as visibly bigger than Gold's coin.
            RectTransform iconRect = FrontEndSceneSetup.GetOrCreateUiChild(
                artwork,
                "Icon");
            iconRect.anchorMin = new Vector2(0.28f, 0.46f);
            iconRect.anchorMax = new Vector2(0.72f, 0.90f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;
            Image icon = FrontEndSceneSetup.GetOrAddComponent<Image>(
                iconRect.gameObject);
            icon.sprite = offer.Icon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            // Matches the bundle skill row / Home inventory stack's own
            // "xN" plate convention -- the icon art already paints a folded
            // badge shape at its own bottom-right corner for this to sit on.
            // Parented under the icon itself (not the artwork) so the
            // anchor fraction lands on the icon's own corner regardless of
            // how big the icon box is scaled -- anchoring it to the wider
            // artwork rect is what made the badge drift off the icon.
            RectTransform quantityRect = FrontEndSceneSetup.GetOrCreateUiChild(
                iconRect,
                "QuantityLabel");
            FrontEndSceneSetup.Anchor(
                quantityRect,
                new Vector2(0.76f, 0.21f),
                new Vector2(58f, 54f));
            TMP_Text quantityLabel = FrontEndSceneSetup.ConfigureText(
                quantityRect,
                $"{offer.Quantity}x",
                font,
                26f,
                Color.white,
                TextAlignmentOptions.Center);
            quantityLabel.fontSizeMin = 18f;

            RectTransform ownedRect = FrontEndSceneSetup.GetOrCreateUiChild(
                card,
                "Owned");
            ownedRect.anchorMin = new Vector2(0f, 0.32f);
            ownedRect.anchorMax = new Vector2(1f, 0.44f);
            ownedRect.pivot = new Vector2(0.5f, 0.5f);
            ownedRect.anchoredPosition = Vector2.zero;
            ownedRect.sizeDelta = Vector2.zero;
            TMP_Text ownedText = FrontEndSceneSetup.ConfigureText(
                ownedRect,
                "OWNED  x0",
                font,
                22f,
                SecondaryText,
                TextAlignmentOptions.Center);
            ownedText.fontSizeMin = 16f;

            // Same size/position as Gold's own PriceButton so every Shop
            // tile's buy button reads as one consistent control.
            RectTransform priceButton = BuildPriceButton(
                card,
                "PriceButton",
                offer.CoinPrice.ToString("N0"),
                font,
                buttonSprite,
                new Vector2(0.5f, 0f),
                new Vector2(0f, 12f),
                new Vector2(190f, 82f),
                34f);
            RectTransform coinRect = FrontEndSceneSetup.GetOrCreateUiChild(
                priceButton,
                "CoinIcon");
            FrontEndSceneSetup.Anchor(
                coinRect,
                new Vector2(0f, 0.5f),
                new Vector2(40f, 40f));
            coinRect.pivot = new Vector2(0f, 0.5f);
            coinRect.anchoredPosition = new Vector2(16f, 0f);
            Image coin = FrontEndSceneSetup.GetOrAddComponent<Image>(
                coinRect.gameObject);
            coin.sprite = coinSprite;
            coin.preserveAspect = true;
            coin.raycastTarget = false;

            TMP_Text priceText = priceButton.Find("Label")
                ?.GetComponent<TMP_Text>();
            if (priceText == null)
            {
                throw new System.InvalidOperationException(
                    $"Power-up price label is missing for {offer.Kind}.");
            }

            RectTransform priceLabelRect = (RectTransform)priceText.transform;
            priceLabelRect.offsetMin = new Vector2(
                56f,
                priceLabelRect.offsetMin.y);
            return new PowerUpShopPresenter.ItemView(
                offer.Kind,
                purchaseButton,
                ownedText,
                priceText);
        }

        private static void BuildGoldGrid(
            Transform parent,
            TMP_FontAsset font,
            Sprite background,
            Sprite buttonSprite,
            Sprite watchAdsCamera,
            ShopCatalog catalog)
        {
            int count = catalog.GoldOffers.Count;
            int rowCount = Mathf.CeilToInt(
                count / (float)GoldColumnCount);
            for (int row = 0; row < rowCount; row++)
            {
                RectTransform rowRect = FrontEndSceneSetup.GetOrCreateUiChild(
                    parent,
                    $"GoldRow_{row + 1:00}");
                ConfigureResponsiveHeight(
                    rowRect,
                    background,
                    GoldColumnCount,
                    GoldRowSpacing,
                    verticalPadding: GoldRowVerticalInset * 2f);

                HorizontalLayoutGroup rowLayout =
                    FrontEndSceneSetup.GetOrAddComponent<HorizontalLayoutGroup>(
                        rowRect.gameObject);
                rowLayout.spacing = GoldRowSpacing;
                rowLayout.padding = new RectOffset(
                    0,
                    0,
                    GoldRowVerticalInset,
                    GoldRowVerticalInset);
                rowLayout.childAlignment = TextAnchor.MiddleCenter;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = true;

                FrontEndSceneSetup.ClearGeneratedChildren(rowRect);
                int firstIndex = row * GoldColumnCount;
                int lastIndex = Mathf.Min(
                    firstIndex + GoldColumnCount - 1,
                    count - 1);
                for (int index = firstIndex; index <= lastIndex; index++)
                {
                    BuildGoldTile(
                        rowRect,
                        $"GoldTile_{index + 1:00}",
                        font,
                        background,
                        buttonSprite,
                        watchAdsCamera,
                        catalog.GoldOffers[index]);
                }
            }

            for (int row = rowCount; ; row++)
            {
                Transform stale = parent.Find($"GoldRow_{row + 1:00}");
                if (stale == null)
                {
                    break;
                }

                FrontEndSceneSetup.DestroyUiChildIfPresent(
                    parent,
                    $"GoldRow_{row + 1:00}");
            }
        }

        private static void BuildGoldTile(
            Transform parent,
            string name,
            TMP_FontAsset font,
            Sprite background,
            Sprite buttonSprite,
            Sprite watchAdsCamera,
            ShopGoldOffer offer)
        {
            RectTransform card = FrontEndSceneSetup.GetOrCreateUiChild(
                parent,
                name);
            RemoveComponentIfPresent<LayoutElement>(card.gameObject);
            RemoveComponentIfPresent<Image>(card.gameObject);

            RectTransform artwork = FrontEndSceneSetup.GetOrCreateUiChild(
                card,
                "Artwork");
            artwork.anchorMin = Vector2.zero;
            artwork.anchorMax = Vector2.one;
            artwork.pivot = new Vector2(0.5f, 0.5f);
            artwork.offsetMin = new Vector2(
                GoldArtworkInset,
                GoldArtworkInset);
            artwork.offsetMax = new Vector2(
                -GoldArtworkInset,
                -GoldArtworkInset);
            Image backgroundImage = FrontEndSceneSetup.GetOrAddComponent<Image>(
                artwork.gameObject);
            backgroundImage.sprite = background;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
            backgroundImage.color = offer.IsRewardedAd
                ? RewardedGoldTint
                : Color.white;
            MakeCardClickable(artwork);

            if (offer.IsRewardedAd)
            {
                BuildRewardedGlow(artwork);
            }
            else
            {
                FrontEndSceneSetup.DestroyUiChildIfPresent(
                    artwork,
                    "RewardedGlow");
            }

            RectTransform coinIconRect = FrontEndSceneSetup.GetOrCreateUiChild(
                card,
                "CoinIcon");
            coinIconRect.anchorMin = new Vector2(0.08f, 0.33f);
            coinIconRect.anchorMax = new Vector2(0.92f, 0.94f);
            coinIconRect.pivot = new Vector2(0.5f, 0.5f);
            coinIconRect.anchoredPosition = Vector2.zero;
            coinIconRect.sizeDelta = Vector2.zero;
            Image coinIcon = FrontEndSceneSetup.GetOrAddComponent<Image>(
                coinIconRect.gameObject);
            coinIcon.sprite = offer.CoinStackIcon;
            coinIcon.preserveAspect = true;
            coinIcon.raycastTarget = false;

            RectTransform amountRect = FrontEndSceneSetup.GetOrCreateUiChild(
                card,
                "Amount");
            amountRect.anchorMin = coinIconRect.anchorMin;
            amountRect.anchorMax = coinIconRect.anchorMax;
            amountRect.pivot = new Vector2(0.5f, 0.5f);
            amountRect.anchoredPosition = Vector2.zero;
            amountRect.sizeDelta = Vector2.zero;
            BuildStrokedAmountLabel(
                amountRect,
                offer.CoinAmount.ToString(),
                font,
                48f,
                TextAlignmentOptions.Center);

            if (offer.IsRewardedAd)
            {
                RectTransform buttonRect = BuildPriceButton(
                    card,
                    "PriceButton",
                    "GET",
                    font,
                    buttonSprite,
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, 12f),
                    new Vector2(190f, 82f),
                    34f);

                RectTransform cameraRect =
                    FrontEndSceneSetup.GetOrCreateUiChild(
                        buttonRect,
                        "CameraIcon");
                cameraRect.anchorMin = new Vector2(0f, 0.5f);
                cameraRect.anchorMax = new Vector2(0f, 0.5f);
                cameraRect.pivot = new Vector2(0f, 0.5f);
                cameraRect.anchoredPosition = new Vector2(24f, 0f);
                cameraRect.sizeDelta = new Vector2(36f, 36f);
                Image cameraIcon = FrontEndSceneSetup.GetOrAddComponent<Image>(
                    cameraRect.gameObject);
                cameraIcon.sprite = watchAdsCamera;
                cameraIcon.preserveAspect = true;
                cameraIcon.raycastTarget = false;

                TMP_Text label = buttonRect.Find("Label")
                    ?.GetComponent<TMP_Text>();
                if (label != null)
                {
                    RectTransform labelRect = (RectTransform)label.transform;
                    labelRect.offsetMin = new Vector2(68f, labelRect.offsetMin.y);
                }
            }
            else
            {
                FrontEndSceneSetup.DestroyUiChildIfPresent(
                    card,
                    "PriceButton");
                BuildPriceButton(
                    card,
                    "PriceButton",
                    offer.PriceLabel,
                    font,
                    buttonSprite,
                    new Vector2(0.5f, 0f),
                    new Vector2(0f, 12f),
                    new Vector2(190f, 82f),
                    34f);
            }
        }

        private static void BuildRewardedGlow(RectTransform card)
        {
            RectTransform glowRect = FrontEndSceneSetup.GetOrCreateUiChild(
                card,
                "RewardedGlow");
            glowRect.anchorMin = new Vector2(0.075f, 0.015f);
            glowRect.anchorMax = new Vector2(0.945f, 0.995f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = Vector2.zero;
            glowRect.sizeDelta = Vector2.zero;
            glowRect.SetAsFirstSibling();

            FrontEndSceneSetup.GetOrAddComponent<CanvasRenderer>(
                glowRect.gameObject);
            FrontEndRoundedRectangleGraphic glow =
                FrontEndSceneSetup.GetOrAddComponent<
                    FrontEndRoundedRectangleGraphic>(glowRect.gameObject);
            glow.ConfigureForSetup(RewardedGlowColor, 28f, true);

            CanvasGroup glowGroup =
                FrontEndSceneSetup.GetOrAddComponent<CanvasGroup>(
                    glowRect.gameObject);
            glowGroup.alpha = 0.08f;
            glowGroup.interactable = false;
            glowGroup.blocksRaycasts = false;

            FrontEndPulseAnimator pulse =
                FrontEndSceneSetup.GetOrAddComponent<FrontEndPulseAnimator>(
                    glowRect.gameObject);
            pulse.ConfigureForSetup(
                glowRect,
                glowGroup,
                0.18f,
                0f,
                0.08f,
                0.34f);
            pulse.enabled = true;
        }

        private static void BuildStrokedAmountLabel(
            RectTransform rect,
            string value,
            TMP_FontAsset font,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            TMP_Text label = FrontEndSceneSetup.ConfigureText(
                rect,
                value,
                font,
                fontSize,
                AmountFillColor,
                alignment);

            // UnityEngine.UI.Outline/Shadow are IMeshModifier effects that
            // Graphic.UpdateGeometry() applies after OnPopulateMesh -- but
            // TMP_Text overrides UpdateGeometry itself and never runs that
            // pipeline, so those components silently render nothing on a
            // TextMeshProUGUI. The stroke and shadow both have to come from
            // the SDF font material's own Outline/Underlay features instead.
            RemoveComponentIfPresent<Outline>(rect.gameObject);
            RemoveComponentIfPresent<Shadow>(rect.gameObject);

            Material material = label.fontMaterial;
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetColor(ShaderUtilities.ID_OutlineColor, AmountStrokeColor);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);

            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, AmountShadowColor);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 1f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -1f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.5f);
            label.fontMaterial = material;
        }

        private static RectTransform BuildPriceButton(
            Transform parent,
            string name,
            string label,
            TMP_FontAsset font,
            Sprite sprite,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize)
        {
            RectTransform rect = FrontEndSceneSetup.GetOrCreateUiChild(
                parent,
                name);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            // Purely visual now -- the whole card is the actual button (see
            // MakeCardClickable), so this stays out of the raycast path
            // entirely rather than being a second, overlapping Selectable
            // that could fight the card's own press/click handling.
            Image image = FrontEndSceneSetup.GetOrAddComponent<Image>(
                rect.gameObject);
            image.sprite = sprite;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = false;
            RemoveComponentIfPresent<Button>(rect.gameObject);

            RectTransform labelRect = FrontEndSceneSetup.GetOrCreateUiChild(
                rect,
                "Label");
            FrontEndSceneSetup.Stretch(labelRect);
            labelRect.offsetMin = new Vector2(12f, 6f);
            labelRect.offsetMax = new Vector2(-12f, -6f);
            FrontEndSceneSetup.ConfigureText(
                labelRect,
                label,
                font,
                fontSize,
                PrimaryText,
                TextAlignmentOptions.Center);
            return rect;
        }

        private static void BuildSectionLabel(
            Transform parent,
            string name,
            string text,
            TMP_FontAsset font)
        {
            RectTransform rect = FrontEndSceneSetup.GetOrCreateUiChild(
                parent,
                name);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, SectionLabelHeight);
            LayoutElement layoutElement =
                FrontEndSceneSetup.GetOrAddComponent<LayoutElement>(
                    rect.gameObject);
            layoutElement.preferredHeight = SectionLabelHeight;
            layoutElement.flexibleWidth = 1f;
            FrontEndSceneSetup.ConfigureText(
                rect,
                text,
                font,
                44f,
                SectionText,
                TextAlignmentOptions.Center);
        }

        private static void ConfigureResponsiveHeight(
            RectTransform rect,
            Sprite sourceSprite,
            int columnCount = 1,
            float columnSpacing = 0f,
            float horizontalPadding = 0f,
            float verticalPadding = 0f)
        {
            RemoveComponentIfPresent<LayoutElement>(rect.gameObject);
            ShopResponsiveLayoutElement responsive =
                FrontEndSceneSetup.GetOrAddComponent<
                    ShopResponsiveLayoutElement>(rect.gameObject);
            responsive.ConfigureForSetup(
                GetTextureAspect(sourceSprite),
                columnCount,
                columnSpacing,
                horizontalPadding,
                verticalPadding);
            EditorUtility.SetDirty(responsive);
        }

        // Mirrors the Gold grid's own amount-to-stack-size convention
        // (100->L1 ... 5000->L6) so a bundle's leading icon reads as the
        // same size of coin pile a la carte purchase would show.
        private static int GetBundleCoinStackLevel(int coinAmount)
        {
            if (coinAmount < 2000)
            {
                return 4;
            }

            return coinAmount < 5000 ? 5 : 6;
        }

        // Makes an entire card/tile tappable (any part of it triggers the
        // purchase), with the press feedback (a slight color tint) reading
        // across the whole card rather than just its price label -- this is
        // the ONLY interactive Selectable per card. PriceButton and the
        // other visuals stay raycastTarget=false so nothing overlaps it.
        private static Button MakeCardClickable(RectTransform visual)
        {
            Image background = FrontEndSceneSetup.GetOrAddComponent<Image>(
                visual.gameObject);
            background.raycastTarget = true;
            Button button = FrontEndSceneSetup.GetOrAddComponent<Button>(
                visual.gameObject);
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = FrontEndSceneSetup.CreateButtonColors();
            colors.pressedColor = new Color32(214, 176, 150, 255);
            colors.highlightedColor = Color.white;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private static float GetTextureAspect(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                throw new System.InvalidOperationException(
                    "Shop card artwork must resolve to a texture-backed Sprite.");
            }

            return sprite.texture.width / (float)sprite.texture.height;
        }

        private static void RemoveComponentIfPresent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            if (component != null)
            {
                Undo.DestroyObjectImmediate(component);
            }
        }

        private static void Validate(
            ScrollRect scrollRect,
            RectTransform content,
            ShopCatalog catalog,
            PowerUpShopPresenter powerUpShop,
            Sprite removeAdsBackground,
            Sprite bundleBackground,
            Sprite goldBackground)
        {
            if (scrollRect.content != content
                || scrollRect.viewport == null
                || scrollRect.horizontal
                || !scrollRect.vertical)
            {
                throw new System.InvalidOperationException(
                    "Shop ScrollRect is not configured for vertical content.");
            }

            RectTransform removeAdsCard = RequireChild(
                content,
                "RemoveAdsCard");
            ValidateResponsiveArtwork(
                removeAdsCard,
                removeAdsBackground,
                1,
                0f,
                RemoveAdsHorizontalInset * 2f,
                RemoveAdsVerticalInset * 2f,
                validateImage: false);
            RectTransform removeAdsVisual = RequireChild(
                removeAdsCard,
                "Visual");
            Image removeAdsImage = removeAdsVisual.GetComponent<Image>();
            if (removeAdsImage == null
                || removeAdsImage.sprite != removeAdsBackground
                || Mathf.Abs(
                    removeAdsVisual.offsetMin.x - RemoveAdsHorizontalInset)
                    > 0.01f
                || Mathf.Abs(
                    removeAdsVisual.offsetMin.y - RemoveAdsVerticalInset)
                    > 0.01f
                || Mathf.Abs(
                    removeAdsVisual.offsetMax.x + RemoveAdsHorizontalInset)
                    > 0.01f
                || Mathf.Abs(
                    removeAdsVisual.offsetMax.y + RemoveAdsVerticalInset)
                    > 0.01f
                || removeAdsVisual.Find("Title") == null
                || removeAdsVisual.Find("Duration") == null
                || removeAdsVisual.Find("PriceButton") == null)
            {
                throw new System.InvalidOperationException(
                    "Remove Ads card is missing its offer message or action.");
            }

            for (int index = 0; index < catalog.Bundles.Count; index++)
            {
                RectTransform card = RequireChild(
                    content,
                    $"BundleCard_{index + 1:00}");
                ValidateResponsiveArtwork(
                    card,
                    bundleBackground,
                    1,
                    0f,
                    BundleHorizontalInset * 2f,
                    validateImage: false);
                RectTransform visual = RequireChild(card, "Visual");
                Image visualBackground = visual.GetComponent<Image>();
                Transform skillRow = visual.Find("SkillRow");
                if (visualBackground == null
                    || visualBackground.sprite != bundleBackground
                    || Mathf.Abs(visual.offsetMin.x - BundleHorizontalInset)
                        > 0.01f
                    || Mathf.Abs(visual.offsetMax.x + BundleHorizontalInset)
                        > 0.01f
                    || skillRow == null
                    || skillRow.childCount != catalog.Bundles[index].Skills.Count
                    || visual.Find("PriceButton") == null)
                {
                    throw new System.InvalidOperationException(
                        $"Bundle card {index + 1} is incomplete.");
                }
            }

            if (powerUpShop == null
                || powerUpShop.Catalog != catalog
                || powerUpShop.ItemViews.Count
                    != catalog.PowerUpOffers.Count
                || powerUpShop.FeedbackText == null)
            {
                throw new System.InvalidOperationException(
                    "Power-up Shop presenter is not wired correctly.");
            }

            int skillRowCount = Mathf.CeilToInt(
                catalog.PowerUpOffers.Count / (float)SkillColumnCount);
            int skillOfferIndex = 0;
            for (int row = 0; row < skillRowCount; row++)
            {
                RectTransform rowRect = RequireChild(
                    content,
                    $"SkillRow_{row + 1:00}");
                ValidateResponsiveArtwork(
                    rowRect,
                    goldBackground,
                    SkillColumnCount,
                    SkillRowSpacing,
                    0f,
                    SkillRowVerticalInset * 2f,
                    validateImage: false);

                int expectedChildren = Mathf.Min(
                    SkillColumnCount,
                    catalog.PowerUpOffers.Count - skillOfferIndex);
                if (rowRect.childCount != expectedChildren)
                {
                    throw new System.InvalidOperationException(
                        $"Skill row {row + 1} has the wrong offer count.");
                }

                for (int child = 0; child < expectedChildren; child++)
                {
                    ShopPowerUpOffer offer =
                        catalog.PowerUpOffers[skillOfferIndex];
                    RectTransform tile =
                        (RectTransform)rowRect.GetChild(child);
                    RectTransform artwork = RequireChild(tile, "Artwork");
                    Image tileImage = artwork.GetComponent<Image>();
                    PowerUpShopPresenter.ItemView view =
                        powerUpShop.ItemViews[skillOfferIndex];
                    Transform iconTransform = artwork.Find("Icon");
                    TMP_Text quantityLabel = iconTransform
                        ?.Find("QuantityLabel")
                        ?.GetComponent<TMP_Text>();
                    if (tileImage == null
                        || tileImage.sprite != goldBackground
                        || view.Kind != offer.Kind
                        || view.PurchaseButton == null
                        || view.PurchaseButton.transform != artwork
                        || view.OwnedText == null
                        || view.PriceText == null
                        || iconTransform?.GetComponent<Image>()
                            ?.sprite != offer.Icon
                        || tile.Find("PriceButton/CoinIcon") == null
                        || artwork.Find("IconPlate") != null
                        || artwork.Find("Title") != null
                        || artwork.Find("Description") != null
                        || quantityLabel == null
                        || quantityLabel.text != $"{offer.Quantity}x")
                    {
                        throw new System.InvalidOperationException(
                            $"Power-up offer {offer.Kind} is incomplete.");
                    }

                    skillOfferIndex++;
                }
            }

            int goldRowCount = Mathf.CeilToInt(
                catalog.GoldOffers.Count / (float)GoldColumnCount);
            int goldOfferIndex = 0;
            for (int row = 0; row < goldRowCount; row++)
            {
                RectTransform rowRect = RequireChild(
                    content,
                    $"GoldRow_{row + 1:00}");
                ValidateResponsiveArtwork(
                    rowRect,
                    goldBackground,
                    GoldColumnCount,
                    GoldRowSpacing,
                    0f,
                    GoldRowVerticalInset * 2f,
                    validateImage: false);

                int expectedChildren = Mathf.Min(
                    GoldColumnCount,
                    catalog.GoldOffers.Count - goldOfferIndex);
                if (rowRect.childCount != expectedChildren)
                {
                    throw new System.InvalidOperationException(
                        $"Gold row {row + 1} has the wrong offer count.");
                }

                for (int child = 0; child < expectedChildren; child++)
                {
                    RectTransform tile = (RectTransform)rowRect.GetChild(child);
                    RectTransform artwork = RequireChild(tile, "Artwork");
                    Image tileImage = artwork.GetComponent<Image>();
                    RectTransform priceButton = tile.Find("PriceButton")
                        as RectTransform;
                    bool isRewarded =
                        catalog.GoldOffers[goldOfferIndex].IsRewardedAd;
                    Transform rewardedGlow = artwork.Find("RewardedGlow");
                    if (tileImage == null
                        || tileImage.sprite != goldBackground
                        || Mathf.Abs(artwork.offsetMin.x - GoldArtworkInset)
                            > 0.01f
                        || Mathf.Abs(artwork.offsetMin.y - GoldArtworkInset)
                            > 0.01f
                        || Mathf.Abs(artwork.offsetMax.x + GoldArtworkInset)
                            > 0.01f
                        || Mathf.Abs(artwork.offsetMax.y + GoldArtworkInset)
                            > 0.01f
                        || priceButton == null
                        || priceButton.sizeDelta.y < 80f
                        || (isRewarded && rewardedGlow == null)
                        || (!isRewarded && rewardedGlow != null))
                    {
                        throw new System.InvalidOperationException(
                            $"Gold offer {goldOfferIndex + 1} is incomplete.");
                    }

                    goldOfferIndex++;
                }
            }
        }

        private static void ValidateResponsiveArtwork(
            RectTransform rect,
            Sprite sourceSprite,
            int expectedColumns,
            float expectedSpacing,
            float expectedHorizontalPadding = 0f,
            float expectedVerticalPadding = 0f,
            bool validateImage = true)
        {
            ShopResponsiveLayoutElement responsive =
                rect.GetComponent<ShopResponsiveLayoutElement>();
            Image image = rect.GetComponent<Image>();
            float expectedAspect = GetTextureAspect(sourceSprite);
            if (responsive == null
                || responsive.ColumnCount != expectedColumns
                || Mathf.Abs(responsive.ColumnSpacing - expectedSpacing) > 0.01f
                || Mathf.Abs(
                    responsive.HorizontalPadding
                    - expectedHorizontalPadding) > 0.01f
                || Mathf.Abs(
                    responsive.VerticalPadding
                    - expectedVerticalPadding) > 0.01f
                || Mathf.Abs(responsive.ItemAspectRatio - expectedAspect) > 0.001f
                || (validateImage
                    && (image == null || image.sprite != sourceSprite)))
            {
                throw new System.InvalidOperationException(
                    $"Shop artwork layout is invalid at '{rect.name}'.");
            }
        }

        private static RectTransform RequireChild(
            Transform parent,
            string name)
        {
            Transform child = parent.Find(name);
            return child as RectTransform
                ?? throw new System.InvalidOperationException(
                    $"Shop hierarchy is missing '{name}'.");
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
                throw new System.OperationCanceledException(
                    "Shop setup cancelled before opening the scene.");
            }

            return EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)
                || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath)
                ?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(
                parent,
                System.IO.Path.GetFileName(folderPath));
        }
    }
}
