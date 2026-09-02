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

        private CoinWalletService _subscribedWallet;
        private bool _displayHeld;
        private int _heldBalance;

        public CloudServicesBootstrap CloudServices => _cloudServices;
        public Image CoinIcon => _coinIcon;
        public TMP_Text BalanceText => _balanceText;
        public RectTransform FlightTarget =>
            _coinIcon != null ? _coinIcon.rectTransform : null;
        public bool DisplayHeld => _displayHeld;
        public int DisplayedBalance { get; private set; }

        public void ConfigureForSetup(
            CloudServicesBootstrap cloudServices,
            Image coinIcon,
            TMP_Text balanceText)
        {
            Unsubscribe();
            _cloudServices = cloudServices
                ?? throw new ArgumentNullException(nameof(cloudServices));
            _coinIcon = coinIcon
                ?? throw new ArgumentNullException(nameof(coinIcon));
            _balanceText = balanceText
                ?? throw new ArgumentNullException(nameof(balanceText));
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
            Render(balance);
        }

        public void ReleaseDisplayedBalance()
        {
            _displayHeld = false;
            RefreshNow();
        }

        public void RefreshNow()
        {
            if (_displayHeld)
            {
                Render(_heldBalance);
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
            if (!_displayHeld)
            {
                Render(change.CurrentBalance);
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
