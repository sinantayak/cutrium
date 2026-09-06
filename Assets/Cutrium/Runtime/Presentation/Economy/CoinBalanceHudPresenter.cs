using System;
using System.Globalization;
using Cutrium.Gameplay.Economy;
using Cutrium.Unity.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Economy
{
    /// Keeps the gameplay Coin balance readable without making the wallet
    /// depend on UI. A completion reward may briefly hold the old displayed
    /// value while its already-persisted Coins fly into this target.
    [DisallowMultipleComponent]
    public sealed class CoinBalanceHudPresenter : MonoBehaviour
    {
        [SerializeField] private CloudServicesBootstrap _cloudServices;
        [SerializeField] private Image _coinIcon;
        [SerializeField] private TMP_Text _balanceText;
        [SerializeField] [Min(0f)] private float _balanceCountSeconds = 0.45f;

        private CoinWalletService _subscribedWallet;
        private bool _displayHeld;
        private int _heldBalance;
        private bool _displayAnimationActive;
        private int _animationStartBalance;
        private int _animationTargetBalance;
        private float _animationElapsed;

        public CloudServicesBootstrap CloudServices => _cloudServices;
        public Image CoinIcon => _coinIcon;
        public TMP_Text BalanceText => _balanceText;
        public RectTransform FlightTarget =>
            _coinIcon != null ? _coinIcon.rectTransform : null;
        public bool DisplayHeld => _displayHeld;
        public bool IsAnimatingDisplayedBalance => _displayAnimationActive;
        public float BalanceCountSeconds => _balanceCountSeconds;
        public int DisplayedBalance { get; private set; }

        public void ConfigureForSetup(
            CloudServicesBootstrap cloudServices,
            Image coinIcon,
            TMP_Text balanceText,
            float balanceCountSeconds = 0.45f)
        {
            ValidateDuration(balanceCountSeconds, nameof(balanceCountSeconds));
            Unsubscribe();
            _cloudServices = cloudServices
                ?? throw new ArgumentNullException(nameof(cloudServices));
            _coinIcon = coinIcon
                ?? throw new ArgumentNullException(nameof(coinIcon));
            _balanceText = balanceText
                ?? throw new ArgumentNullException(nameof(balanceText));
            _balanceCountSeconds = balanceCountSeconds;
            _displayAnimationActive = false;
            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
            }

            RefreshNow();
        }

        public void HoldDisplayedBalance(int balance)
        {
            if (balance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(balance));
            }

            _displayHeld = true;
            _heldBalance = balance;
            _displayAnimationActive = false;
            Render(balance);
        }

        public void ReleaseDisplayedBalance()
        {
            _displayHeld = false;
            _displayAnimationActive = false;
            RefreshNow();
        }

        /// Releases the presentation hold while counting only the visible
        /// label toward an already-authoritative wallet balance. This never
        /// mutates or delays persistence.
        public void ReleaseDisplayedBalanceAnimated(int targetBalance)
        {
            if (targetBalance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetBalance));
            }

            _displayHeld = false;
            if (_balanceCountSeconds <= 0f
                || targetBalance <= DisplayedBalance)
            {
                _displayAnimationActive = false;
                Render(targetBalance);
                return;
            }

            _animationStartBalance = DisplayedBalance;
            _animationTargetBalance = targetBalance;
            _animationElapsed = 0f;
            _displayAnimationActive = true;
        }

        public void AdvanceDisplayAnimation(float elapsedSeconds)
        {
            ValidateDuration(elapsedSeconds, nameof(elapsedSeconds));
            if (!_displayAnimationActive)
            {
                return;
            }

            _animationElapsed += elapsedSeconds;
            float progress = _balanceCountSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(_animationElapsed / _balanceCountSeconds);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            int displayed = Mathf.RoundToInt(Mathf.Lerp(
                _animationStartBalance,
                _animationTargetBalance,
                eased));
            Render(Mathf.Clamp(
                displayed,
                DisplayedBalance,
                _animationTargetBalance));
            if (progress >= 1f)
            {
                _displayAnimationActive = false;
                Render(_animationTargetBalance);
            }
        }

        public void RefreshNow()
        {
            if (_displayHeld)
            {
                Render(_heldBalance);
                return;
            }

            if (_displayAnimationActive)
            {
                return;
            }

            int balance = Application.isPlaying && _cloudServices != null
                ? _cloudServices.Coins.Balance
                : 0;
            Render(balance);
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Subscribe();
            }

            RefreshNow();
        }

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            AdvanceDisplayAnimation(Time.unscaledDeltaTime);
        }

        private void Subscribe()
        {
            if (_subscribedWallet != null || _cloudServices == null)
            {
                return;
            }

            _subscribedWallet = _cloudServices.Coins;
            _subscribedWallet.BalanceChanged += OnBalanceChanged;
        }

        private void Unsubscribe()
        {
            if (_subscribedWallet != null)
            {
                _subscribedWallet.BalanceChanged -= OnBalanceChanged;
                _subscribedWallet = null;
            }
        }

        private void OnBalanceChanged(CoinBalanceChangedEvent change)
        {
            if (_displayHeld)
            {
                return;
            }

            if (_displayAnimationActive)
            {
                if (change.CurrentBalance <= DisplayedBalance)
                {
                    _displayAnimationActive = false;
                    Render(change.CurrentBalance);
                }
                else
                {
                    _animationTargetBalance = change.CurrentBalance;
                }

                return;
            }

            Render(change.CurrentBalance);
        }

        private static void ValidateDuration(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private void Render(int balance)
        {
            DisplayedBalance = Math.Max(0, balance);
            if (_balanceText != null)
            {
                _balanceText.text = DisplayedBalance.ToString(
                    "N0",
                    CultureInfo.InvariantCulture);
            }
        }
    }
}
