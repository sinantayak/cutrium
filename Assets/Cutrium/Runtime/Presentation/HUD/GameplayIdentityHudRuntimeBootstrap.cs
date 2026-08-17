using Cutrium.Unity.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.HUD
{
    /// Compatibility bootstrap for checked-in scenes created before the
    /// identity HUD was serialized. It adds only the compact cut counter and
    /// transient intro/failure presentation; the focused Editor setup later
    /// materializes the same named hierarchy idempotently.
    internal static class GameplayIdentityHudRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCurrentScenePresenter()
        {
            FirstPlayableController[] controllers =
                Object.FindObjectsByType<FirstPlayableController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int index = 0; index < controllers.Length; index++)
            {
                FirstPlayableController controller = controllers[index];
                if (controller.transform.root
                        .GetComponentInChildren<GameplayIdentityHudPresenter>(
                            true) != null)
                {
                    continue;
                }

                RectTransform safeArea = FindRect(
                    controller.transform.root,
                    "SafeAreaRoot");
                RectTransform bottomHud = FindRect(
                    controller.transform.root,
                    "BottomHUD");
                if (safeArea == null || bottomHud == null)
                {
                    continue;
                }

                Configure(safeArea, bottomHud, controller);
            }
        }

        private static void Configure(
            RectTransform safeArea,
            RectTransform bottomHud,
            FirstPlayableController controller)
        {
            GameplayIdentityHudPresenter presenter =
                safeArea.gameObject.AddComponent<GameplayIdentityHudPresenter>();
            Font font = ResolveUiFont(safeArea);
            TMP_Text cutCounter = TmpText(
                bottomHud,
                "CutLimitCounter",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -4f),
                new Vector2(260f, 30f),
                20);
            cutCounter.color = new Color(0.22f, 0.1f, 0.035f, 1f);
            cutCounter.raycastTarget = false;

            TMP_Text speedText = TmpText(
                bottomHud,
                "BarrierSpeedCounter",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -34f),
                new Vector2(140f, 26f),
                18);
            speedText.color = new Color(0.22f, 0.1f, 0.035f, 1f);
            speedText.raycastTarget = false;

            RectTransform failure = Rect(
                safeArea,
                "CutLimitFailureOverlay",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            CanvasGroup failureGroup =
                failure.gameObject.AddComponent<CanvasGroup>();
            Image scrim = failure.gameObject.AddComponent<Image>();
            scrim.color = new Color(0.08f, 0.035f, 0.02f, 0.86f);
            Text failureText = Text(
                failure,
                "FailureText",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 55f),
                new Vector2(520f, 120f),
                font,
                32);
            failureText.color = Color.white;
            RectTransform retryRect = Rect(
                failure,
                "RetryButton",
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -60f),
                new Vector2(240f, 64f));
            Image retryImage = retryRect.gameObject.AddComponent<Image>();
            retryImage.color = new Color(0.92f, 0.5f, 0.12f, 1f);
            Button retryButton = retryRect.gameObject.AddComponent<Button>();
            retryButton.targetGraphic = retryImage;
            Text retryText = Text(
                retryRect,
                "Label",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                font,
                26);
            retryText.text = "RETRY";
            retryText.color = new Color(0.2f, 0.08f, 0.02f, 1f);
            retryText.raycastTarget = false;
            failure.SetAsLastSibling();

            presenter.Configure(
                controller,
                cutCounter,
                speedText,
                null,
                null,
                failureGroup,
                failureText,
                retryButton);
        }

        private static Font ResolveUiFont(RectTransform safeArea)
        {
            Text[] existingTexts = safeArea.GetComponentsInChildren<Text>(true);
            for (int index = 0; index < existingTexts.Length; index++)
            {
                Font candidate = existingTexts[index].font;
                if (candidate != null && candidate.name == "LapsusPro-Bold")
                {
                    return candidate;
                }
            }

            for (int index = 0; index < existingTexts.Length; index++)
            {
                if (existingTexts[index].font != null)
                {
                    return existingTexts[index].font;
                }
            }

            throw new System.InvalidOperationException(
                "Gameplay identity HUD requires a serialized UI font.");
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            if (root.name == name)
            {
                return root as RectTransform;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                RectTransform found = FindRect(root.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static RectTransform Rect(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private static Text Text(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Font font,
            int fontSize)
        {
            RectTransform rect = Rect(
                parent,
                name,
                anchorMin,
                anchorMax,
                anchoredPosition,
                sizeDelta);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static TMP_Text TmpText(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            int fontSize)
        {
            RectTransform rect = Rect(
                parent,
                name,
                anchorMin,
                anchorMax,
                anchoredPosition,
                sizeDelta);
            TextMeshProUGUI text = rect.gameObject
                .AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10;
            text.fontSizeMax = fontSize;
            return text;
        }
    }
}
