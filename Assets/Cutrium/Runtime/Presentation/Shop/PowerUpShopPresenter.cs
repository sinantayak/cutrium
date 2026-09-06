using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Economy;
using Cutrium.Presentation.Feedback;
using Cutrium.Unity.Services;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cutrium.Presentation.Shop
{
    /// Binds the catalog's a-la-carte power offers to the central Coin and
    /// inventory services. Card visuals remain replaceable setup content.
    [DisallowMultipleComponent]
    public sealed class PowerUpShopPresenter : MonoBehaviour
    {
        [Serializable]
        public sealed class ItemView
        {
            [SerializeField] private PowerUpKind _kind;
            [SerializeField] private Button _purchaseButton;
            [SerializeField] private TMP_Text _ownedText;
            [SerializeField] private TMP_Text _priceText;

            public ItemView(
                PowerUpKind kind,
                Button purchaseButton,
                TMP_Text ownedText,
                TMP_Text priceText)
            {
                PowerUpInventory.ValidateKind(kind);
                _kind = kind;
                _purchaseButton = purchaseButton;
                _ownedText = ownedText;
                _priceText = priceText;
            }

            public PowerUpKind Kind => _kind;
            public Button PurchaseButton => _purchaseButton;
            public TMP_Text OwnedText => _ownedText;
            public TMP_Text PriceText => _priceText;
        }

        private sealed class ButtonSubscription
        {
            public Button Button;
            public UnityAction Action;
        }

        private static readonly Color AffordablePriceColor =
            new Color32(255, 230, 191, 255);
        private static readonly Color UnaffordablePriceColor =
            new Color32(255, 132, 96, 255);
        private static readonly Color SuccessColor =
            new Color32(108, 236, 130, 255);
        private static readonly Color FailureColor =
            new Color32(255, 132, 96, 255);

        [SerializeField] private ShopCatalog _catalog;
        [SerializeField] private CloudServicesBootstrap _cloudServices;
        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private FeedbackAudioPresenter _feedbackAudio;
        [SerializeField] private TMP_Text _feedbackText;
        [SerializeField] private ItemView[] _itemViews = Array.Empty<ItemView>();
        [SerializeField] [Min(0f)] private float _feedbackSeconds = 1.5f;

        private readonly List<ButtonSubscription> _buttonSubscriptions =
            new List<ButtonSubscription>(3);
        private CoinWalletService _subscribedCoins;
        private PowerUpInventoryService _subscribedInventory;
        private float _clearFeedbackAt;

        public ShopCatalog Catalog => _catalog;
        public CloudServicesBootstrap CloudServices => _cloudServices;
        public IReadOnlyList<ItemView> ItemViews => _itemViews;
        public TMP_Text FeedbackText => _feedbackText;
        public PowerUpPurchaseResult LastPurchaseResult { get; private set; }

        public void ConfigureForSetup(
            ShopCatalog catalog,
            CloudServicesBootstrap cloudServices,
            FirstPlayableController controller,
            FeedbackAudioPresenter feedbackAudio,
            TMP_Text feedbackText,
            ItemView[] itemViews,
            float feedbackSeconds = 1.5f)
        {
            if (float.IsNaN(feedbackSeconds)
                || float.IsInfinity(feedbackSeconds)
                || feedbackSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(feedbackSeconds));
            }

            Unsubscribe();
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _cloudServices = cloudServices
                ?? throw new ArgumentNullException(nameof(cloudServices));
            _controller = controller;
            _feedbackAudio = feedbackAudio;
            _feedbackText = feedbackText
                ?? throw new ArgumentNullException(nameof(feedbackText));
            _itemViews = itemViews ?? Array.Empty<ItemView>();
            _feedbackSeconds = feedbackSeconds;
            ValidateViews();
            ClearFeedback();
            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
            }

            RefreshNow();
        }

        public void RefreshNow()
        {
            bool playing = Application.isPlaying;
            CoinWalletService coins = playing && _cloudServices != null
                ? _cloudServices.Coins
                : null;
            PowerUpInventoryService inventory =
                playing && _cloudServices != null
                    ? _cloudServices.PowerUps
                    : null;
            for (int index = 0; index < _itemViews.Length; index++)
            {
                ItemView view = _itemViews[index];
                ShopPowerUpOffer offer = FindOffer(view.Kind);
                int owned = inventory != null
                    ? inventory.GetCount(view.Kind)
                    : 0;
                view.OwnedText.text = $"OWNED  x{owned:N0}";
                view.PriceText.text = offer.CoinPrice.ToString("N0");
                view.PriceText.color = coins == null
                    || coins.CanAfford(offer.CoinPrice)
                        ? AffordablePriceColor
                        : UnaffordablePriceColor;
                view.PurchaseButton.interactable = true;
            }
        }

        public PowerUpPurchaseResult Purchase(PowerUpKind kind)
        {
            ShopPowerUpOffer offer = FindOffer(kind);
            _controller?.NotifyUiFeedback();
            LastPurchaseResult = _cloudServices.PowerUpPurchases.TryPurchase(
                kind,
                offer.Quantity,
                offer.CoinPrice);
            if (LastPurchaseResult.Purchased)
            {
                _feedbackAudio?.PlayCoinSpend();
                ShowFeedback(
                    $"PURCHASED  +{offer.Quantity:N0}",
                    SuccessColor);
            }
            else
            {
                string message = LastPurchaseResult.Status switch
                {
                    PowerUpPurchaseStatus.InsufficientCoins =>
                        "NOT ENOUGH COINS",
                    PowerUpPurchaseStatus.InventoryOverflow =>
                        "INVENTORY FULL",
                    _ => "PURCHASE FAILED",
                };
                ShowFeedback(message, FailureColor);
            }

            RefreshNow();
            return LastPurchaseResult;
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
            if (_feedbackText != null
                && !string.IsNullOrEmpty(_feedbackText.text)
                && Time.unscaledTime >= _clearFeedbackAt)
            {
                ClearFeedback();
            }
        }

        private void Subscribe()
        {
            if (_cloudServices == null || _subscribedCoins != null)
            {
                return;
            }

            _subscribedCoins = _cloudServices.Coins;
            _subscribedInventory = _cloudServices.PowerUps;
            _subscribedCoins.BalanceChanged += OnCoinBalanceChanged;
            _subscribedInventory.InventoryChanged += OnInventoryChanged;
            for (int index = 0; index < _itemViews.Length; index++)
            {
                ItemView view = _itemViews[index];
                PowerUpKind kind = view.Kind;
                UnityAction action = () => Purchase(kind);
                view.PurchaseButton.onClick.AddListener(action);
                _buttonSubscriptions.Add(new ButtonSubscription
                {
                    Button = view.PurchaseButton,
                    Action = action,
                });
            }
        }

        private void Unsubscribe()
        {
            if (_subscribedCoins != null)
            {
                _subscribedCoins.BalanceChanged -= OnCoinBalanceChanged;
                _subscribedCoins = null;
            }

            if (_subscribedInventory != null)
            {
                _subscribedInventory.InventoryChanged -= OnInventoryChanged;
                _subscribedInventory = null;
            }

            for (int index = 0; index < _buttonSubscriptions.Count; index++)
            {
                ButtonSubscription subscription = _buttonSubscriptions[index];
                if (subscription.Button != null)
                {
                    subscription.Button.onClick.RemoveListener(
                        subscription.Action);
                }
            }

            _buttonSubscriptions.Clear();
        }

        private void OnCoinBalanceChanged(CoinBalanceChangedEvent change) =>
            RefreshNow();

        private void OnInventoryChanged(PowerUpInventoryChangedEvent change) =>
            RefreshNow();

        private void ShowFeedback(string message, Color color)
        {
            if (_feedbackText == null)
            {
                return;
            }

            _feedbackText.text = message;
            _feedbackText.color = color;
            _clearFeedbackAt = Time.unscaledTime + _feedbackSeconds;
        }

        private void ClearFeedback()
        {
            if (_feedbackText == null)
            {
                return;
            }

            _feedbackText.text = string.Empty;
            _clearFeedbackAt = float.PositiveInfinity;
        }

        private ShopPowerUpOffer FindOffer(PowerUpKind kind)
        {
            if (_catalog != null)
            {
                for (int index = 0; index < _catalog.PowerUpOffers.Count;
                    index++)
                {
                    ShopPowerUpOffer offer = _catalog.PowerUpOffers[index];
                    if (offer != null && offer.Kind == kind)
                    {
                        return offer;
                    }
                }
            }

            throw new InvalidOperationException(
                $"Shop catalog has no offer for {kind}.");
        }

        private void ValidateViews()
        {
            var seen = new HashSet<PowerUpKind>();
            for (int index = 0; index < _itemViews.Length; index++)
            {
                ItemView view = _itemViews[index]
                    ?? throw new ArgumentException(
                        "Shop item views cannot be null.",
                        nameof(_itemViews));
                PowerUpInventory.ValidateKind(view.Kind);
                if (view.PurchaseButton == null
                    || view.OwnedText == null
                    || view.PriceText == null
                    || !seen.Add(view.Kind))
                {
                    throw new ArgumentException(
                        "Each power-up needs one complete, unique Shop view.",
                        nameof(_itemViews));
                }

                _ = FindOffer(view.Kind);
            }

            if (seen.Count != _catalog.PowerUpOffers.Count)
            {
                throw new ArgumentException(
                    "Shop views must match every configured power-up offer.",
                    nameof(_itemViews));
            }
        }
    }
}
