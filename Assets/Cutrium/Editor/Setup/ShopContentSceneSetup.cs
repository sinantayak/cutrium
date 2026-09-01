using Cutrium.Presentation.Frontend;
using Cutrium.Presentation.Shop;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cutrium.Editor.Setup
{
    /// Builds the Shop tab's visual content (Remove Ads card, Bundles
    /// section, Gold section) inside a vertical ScrollRect matching the
    /// Challenge level-map's scroll pattern (see FrontEndSceneSetup). This
    /// is presentation only -- no purchase/ad/IAP wiring yet; prices and
    /// bundle contents live in a ShopCatalog asset so they can be retuned
    /// here without touching scene structure.
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
        private const string FreezeSkillPath =
            "Assets/Cutrium/Content/Gui/FreezeSkill.png";
        private const string InstantBarrierSkillPath =
            "Assets/Cutrium/Content/Gui/InstantBarrierSkill.png";
        private const string GravityWellSkillPath =
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
        private const float ContentTopPadding = 28f;
        private const float ContentBottomPadding = 52f;

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
            foreach (GameObject root in scene.GetRootGameObjects())
            {
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

            Configure((RectTransform)presenter.ShopPage.transform, font);
            EditorUtility.SetDirty(presenter.ShopPage);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException(
                    "Unity could not save the Shop visual setup.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Shop visual parity applied: responsive Remove Ads, bundles, " +
                "and three-column gold offers saved in VerticalSlice.unity.");
        }

        public static void Configure(RectTransform shopPage, TMP_FontAsset font)
        {
            ShopCatalog catalog = EnsureCatalog();

            Sprite removeAdsBackground = FrontEndSceneSetup.EnsureUiSprite(
                RemoveAdsBackgroundPath);
            Sprite removeAdsIcon = FrontEndSceneSetup.EnsureUiSprite(
                RemoveAdsIconPath);
            Sprite bundleBackground = FrontEndSceneSetup.EnsureUiSprite(
                BundleBackgroundPath);
            Sprite bundleCoin = FrontEndSceneSetup.EnsureUiSprite(
                string.Format(CoinStackPathFormat, 2));
            Sprite saleBadge = FrontEndSceneSetup.EnsureUiSprite(SaleBadgePath);
            Sprite goldBackground = FrontEndSceneSetup.EnsureUiSprite(
                GoldBackgroundPath);
            Sprite buttonBackground = FrontEndSceneSetup.EnsureUiSprite(
                ButtonBackgroundPath);
            Sprite watchAdsCamera = FrontEndSceneSetup.EnsureUiSprite(
                WatchAdsCameraPath);

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
                BuildBundleCard(
                    content,
                    $"BundleCard_{index + 1:00}",
                    font,
                    bundleBackground,
                    bundleCoin,
                    saleBadge,
                    buttonBackground,
                    catalog.Bundles[index]);
            }

            BuildSectionLabel(content, "GoldLabel", "GOLD", font);
            BuildGoldGrid(
                content,
                font,
                goldBackground,
                buttonBackground,
                watchAdsCamera,
                catalog);

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

            Validate(
                scrollRect,
                content,
                catalog,
                removeAdsBackground,
                bundleBackground,
                goldBackground);
        }

        private static ShopCatalog EnsureCatalog()
        {
            ShopCatalog catalog =
                AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            if (catalog == null)
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

            catalog.ConfigureForSetup("$25.99", bundles, goldOffers);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
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
            backgroundImage.raycastTarget = false;

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
                34f);
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
            backgroundImage.raycastTarget = false;

            // The L2 stack owns the left half of the offer. The amount is a
            // true overlay so the coin and value read as one visual unit.
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
            skillLayout.spacing = 12f;
            skillLayout.childControlWidth = true;
            skillLayout.childControlHeight = true;
            skillLayout.childForceExpandWidth = true;
            skillLayout.childForceExpandHeight = true;

            FrontEndSceneSetup.ClearGeneratedChildren(skillRow);
            for (int index = 0; index < offer.Skills.Count; index++)
            {
                ShopBundleSkillEntry skill = offer.Skills[index];
                BuildSkillEntry(skillRow, $"Skill_{index + 1}", font, skill);
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
                34f);

            // Keep the sale badge inside the visual bounds so the viewport
            // mask cannot crop it during normal scrolling.
            RectTransform badgeRect = FrontEndSceneSetup.GetOrCreateUiChild(
                visual,
                "SaleBadge");
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = Vector2.zero;
            badgeRect.sizeDelta = new Vector2(108f, 108f);
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
                new Vector2(92f, 76f));
            TMP_Text badgeLabel = FrontEndSceneSetup.ConfigureText(
                badgeLabelRect,
                $"-{offer.DiscountPercent}%\nOFF",
                font,
                24f,
                SaleBadgeTextColor,
                TextAlignmentOptions.Center);
            badgeLabel.textWrappingMode = TextWrappingModes.Normal;
            badgeLabel.lineSpacing = -14f;
        }

        private static void BuildSkillEntry(
            Transform parent,
            string name,
            TMP_FontAsset font,
            ShopBundleSkillEntry skill)
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

            // Small color-coded quantity pill overlaid on the icon's
            // bottom-right corner, per spec ("sağ altlarında x1 x2 gibi
            // miktarları").
            RectTransform pillRect = FrontEndSceneSetup.GetOrCreateUiChild(
                root,
                "QuantityPill");
            pillRect.anchorMin = new Vector2(1f, 0f);
            pillRect.anchorMax = new Vector2(1f, 0f);
            pillRect.pivot = new Vector2(1f, 0f);
            pillRect.anchoredPosition = new Vector2(-2f, 2f);
            pillRect.sizeDelta = new Vector2(50f, 34f);
            // Explicit CanvasRenderer before the Graphic: relying on
            // MaskableGraphic's [RequireComponent] to add it automatically
            // left the object without one when built from this batch/editor
            // setup path, throwing MissingComponentException on every
            // canvas rebuild.
            FrontEndSceneSetup.GetOrAddComponent<CanvasRenderer>(
                pillRect.gameObject);
            FrontEndRoundedRectangleGraphic pill =
                FrontEndSceneSetup.GetOrAddComponent<
                    FrontEndRoundedRectangleGraphic>(pillRect.gameObject);
            pill.ConfigureForSetup(skill.AccentColor, 11f, true);

            RectTransform quantityRect = FrontEndSceneSetup.GetOrCreateUiChild(
                pillRect,
                "Label");
            FrontEndSceneSetup.Stretch(quantityRect);
            FrontEndSceneSetup.ConfigureText(
                quantityRect,
                $"x{skill.Quantity}",
                font,
                20f,
                Color.white,
                TextAlignmentOptions.Center);
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
            backgroundImage.raycastTarget = false;

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
                    30f);

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
                    30f);
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
            FrontEndSceneSetup.ConfigureText(
                rect,
                value,
                font,
                fontSize,
                AmountFillColor,
                alignment);
            Outline stroke = FrontEndSceneSetup.GetOrAddComponent<Outline>(
                rect.gameObject);
            stroke.effectColor = AmountStrokeColor;
            stroke.effectDistance = new Vector2(3f, -3f);
            stroke.useGraphicAlpha = true;
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

            Image image = FrontEndSceneSetup.GetOrAddComponent<Image>(
                rect.gameObject);
            image.sprite = sprite;
            image.preserveAspect = false;
            image.color = Color.white;
            image.raycastTarget = true;

            Button button = FrontEndSceneSetup.GetOrAddComponent<Button>(
                rect.gameObject);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = FrontEndSceneSetup.CreateButtonColors();
            button.navigation = new Navigation { mode = Navigation.Mode.None };

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
