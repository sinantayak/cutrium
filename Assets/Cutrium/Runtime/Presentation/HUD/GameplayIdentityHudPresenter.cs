using System;
using Cutrium.Gameplay.Session;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class GameplayIdentityHudPresenter : MonoBehaviour
    {
        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private TMP_Text _cutCounterText;
        [SerializeField] private TMP_Text _speedText;
        [SerializeField] private Image _speedIconImage;
        [SerializeField] private Sprite[] _speedTierSprites = new Sprite[0];
        [SerializeField] private CanvasGroup _failureCanvasGroup;
        [SerializeField] private Text _failureText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _watchAdButton;

        private const string FailurePrompt =
            "Watch an AD\nto Continue!";

        private bool _retrySubscribed;

        public TMP_Text CutCounterText => _cutCounterText;
        public TMP_Text SpeedText => _speedText;
        public Image SpeedIconImage => _speedIconImage;
        public CanvasGroup FailureCanvasGroup => _failureCanvasGroup;
        public Button RetryButton => _retryButton;
        public Button WatchAdButton => _watchAdButton;

        public void Configure(
            FirstPlayableController controller,
            TMP_Text cutCounterText,
            TMP_Text speedText,
            Image speedIconImage,
            Sprite[] speedTierSprites,
            CanvasGroup failureCanvasGroup,
            Text failureText,
            Button retryButton,
            Button watchAdButton = null)
        {
            ConfigureForSetup(
                controller,
                cutCounterText,
                speedText,
                speedIconImage,
                speedTierSprites,
                failureCanvasGroup,
                failureText,
                retryButton,
                watchAdButton);
        }

        public void ConfigureForSetup(
            FirstPlayableController controller,
            TMP_Text cutCounterText,
            TMP_Text speedText,
            Image speedIconImage,
            Sprite[] speedTierSprites,
            CanvasGroup failureCanvasGroup,
            Text failureText,
            Button retryButton,
            Button watchAdButton = null)
        {
            UnsubscribeRetry();
            _controller = controller;
            _cutCounterText = cutCounterText;
            _speedText = speedText;
            _speedIconImage = speedIconImage;
            _speedTierSprites = speedTierSprites ?? new Sprite[0];
            _failureCanvasGroup = failureCanvasGroup;
            _failureText = failureText;
            _retryButton = retryButton;
            _watchAdButton = watchAdButton;
            if (_failureCanvasGroup != null)
            {
                EnsureFailureOverlayIgnoresLayout();
                _failureCanvasGroup.alpha = 0f;
                _failureCanvasGroup.interactable = false;
                _failureCanvasGroup.blocksRaycasts = false;
            }
            if (isActiveAndEnabled && Application.isPlaying)
            {
                SubscribeRetry();
            }

            RefreshNow(0f);
        }

        // The speedometer needle position is meaningful only relative to
        // the catalog's own floor/ceiling barrier speed. This is
        // recomputed on every call rather than cached on Configure: caching
        // here previously broke at runtime because ConfigureForSetup only
        // ever runs once, in the Editor at setup time -- plain (non-
        // [SerializeField]) fields computed then do not survive the
        // scene-save/Play-mode-load round trip, so the cached range read
        // back as (0, 0) during actual play and the icon never left tier 0.
        private void BarrierGrowthSpeedRange(out float minimum, out float maximum)
        {
            minimum = float.PositiveInfinity;
            maximum = float.NegativeInfinity;
            if (_controller == null)
            {
                return;
            }

            var definitions = _controller.LevelDefinitions;
            for (int index = 0; index < definitions.Count; index++)
            {
                float speed = definitions[index].BarrierGrowthSpeed;
                minimum = Mathf.Min(minimum, speed);
                maximum = Mathf.Max(maximum, speed);
            }
        }

        public void RefreshNow(float elapsedTime)
        {
            if (float.IsNaN(elapsedTime)
                || float.IsInfinity(elapsedTime)
                || elapsedTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedTime));
            }

            if (_controller == null || _controller.Session == null)
            {
                SetGroup(_failureCanvasGroup, false, 0f, true);
                return;
            }

            bool limited = _controller.Session.HasCutLimit;
            if (_cutCounterText != null)
            {
                _cutCounterText.gameObject.SetActive(limited);
                if (limited)
                {
                    int maximum = _controller.Session.MaximumAcceptedCuts;
                    int used = maximum - _controller.Session.CutsRemaining;
                    _cutCounterText.text = $"CUT: {used}/{maximum}";
                }

                // ShadowText is a decorative sibling, not wired to this
                // presenter -- it needs the same visibility toggle or an
                // unlimited level would leave its last baked placeholder
                // text floating in the TopHUD with nothing to back it.
                Transform shadow = _cutCounterText.transform.parent != null
                    ? _cutCounterText.transform.parent.Find("ShadowText")
                    : null;
                if (shadow != null)
                {
                    shadow.gameObject.SetActive(limited);
                }
            }

            // The growing-barrier speed is set per level (see
            // FirstTwelveGameplayProgression), not a fixed constant, so a
            // static placeholder here would silently disagree with what
            // the player actually sees while drawing a cut.
            if (_speedText != null)
            {
                _speedText.text = _controller.BarrierGrowthSpeed.ToString(
                    "0.0",
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            // The speedometer icon reads like a car's needle: as this
            // level's barrier growth speed climbs toward the catalog's own
            // fastest level, the icon steps through faster-looking stages.
            if (_speedIconImage != null && _speedTierSprites.Length > 0)
            {
                BarrierGrowthSpeedRange(
                    out float minSpeed,
                    out float maxSpeed);
                float range = maxSpeed - minSpeed;
                float t = range > 0f
                    ? Mathf.Clamp01(
                        (_controller.BarrierGrowthSpeed - minSpeed) / range)
                    : 0f;
                int tier = Mathf.Clamp(
                    Mathf.FloorToInt(t * _speedTierSprites.Length),
                    0,
                    _speedTierSprites.Length - 1);
                Sprite tierSprite = _speedTierSprites[tier];
                if (tierSprite != null)
                {
                    _speedIconImage.sprite = tierSprite;
                }
            }

            CaptureLevelStatus status = _controller.Session.LevelStatus;
            bool failed = status == CaptureLevelStatus.OutOfCuts
                || status == CaptureLevelStatus.OutOfLives;
            SetGroup(_failureCanvasGroup, failed, failed ? 1f : 0f, true);
            if (_failureText != null && failed)
            {
                _failureText.text = FailurePrompt;
            }
        }

        private void LateUpdate()
        {
            RefreshNow(Time.unscaledDeltaTime);
        }

        private void OnEnable()
        {
            EnsureFailureOverlayIgnoresLayout();
            if (Application.isPlaying)
            {
                SubscribeRetry();
            }
        }

        private void OnDisable()
        {
            UnsubscribeRetry();
        }

        private void SubscribeRetry()
        {
            if (_retrySubscribed || _retryButton == null)
            {
                return;
            }

            _retryButton.onClick.AddListener(OnRetryClicked);
            _retrySubscribed = true;
        }

        private void UnsubscribeRetry()
        {
            if (_retrySubscribed && _retryButton != null)
            {
                _retryButton.onClick.RemoveListener(OnRetryClicked);
            }

            _retrySubscribed = false;
        }

        private void OnRetryClicked()
        {
            _controller.NotifyUiFeedback();
            _controller.RetryLevel();
            RefreshNow(0f);
        }

        private void EnsureFailureOverlayIgnoresLayout()
        {
            if (_failureCanvasGroup == null)
            {
                return;
            }

            LayoutElement layout =
                _failureCanvasGroup.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = _failureCanvasGroup.gameObject
                    .AddComponent<LayoutElement>();
            }

            layout.ignoreLayout = true;
            if (_failureCanvasGroup.transform is RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private static void SetGroup(
            CanvasGroup group,
            bool visible,
            float alpha,
            bool blockWhenVisible)
        {
            if (group == null)
            {
                return;
            }

            if (!group.gameObject.activeSelf)
            {
                group.gameObject.SetActive(true);
            }

            group.alpha = alpha;
            group.interactable = visible && blockWhenVisible;
            group.blocksRaycasts = visible && blockWhenVisible;
        }
    }
}
