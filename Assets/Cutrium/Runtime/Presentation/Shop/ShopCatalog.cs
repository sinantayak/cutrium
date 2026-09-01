using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cutrium.Presentation.Shop
{
    [Serializable]
    public sealed class ShopBundleSkillEntry
    {
        [SerializeField] private Sprite _icon;
        [SerializeField] private int _quantity = 1;
        [SerializeField] private Color _accentColor = Color.white;

        public ShopBundleSkillEntry(Sprite icon, int quantity, Color accentColor)
        {
            _icon = icon;
            _quantity = quantity;
            _accentColor = accentColor;
        }

        public Sprite Icon => _icon;
        public int Quantity => _quantity;

        /// Small color-coded background behind the quantity badge (e.g.
        /// blue/red/purple per skill) so the 3 skills read apart at a
        /// glance, matching the shop mockup.
        public Color AccentColor => _accentColor;
    }

    [Serializable]
    public sealed class ShopBundleOffer
    {
        [SerializeField] private int _coinAmount;
        [SerializeField, Range(0, 100)] private int _discountPercent;
        [SerializeField] private string _priceLabel;
        [SerializeField] private string _originalPriceLabel;
        [SerializeField] private ShopBundleSkillEntry[] _skills;

        public ShopBundleOffer(
            int coinAmount,
            int discountPercent,
            string priceLabel,
            string originalPriceLabel,
            ShopBundleSkillEntry[] skills)
        {
            _coinAmount = coinAmount;
            _discountPercent = discountPercent;
            _priceLabel = priceLabel;
            _originalPriceLabel = originalPriceLabel;
            _skills = skills ?? Array.Empty<ShopBundleSkillEntry>();
        }

        public int CoinAmount => _coinAmount;
        public int DiscountPercent => _discountPercent;
        public bool HasDiscount => _discountPercent > 0;
        public string PriceLabel => _priceLabel;
        public string OriginalPriceLabel => _originalPriceLabel;
        public IReadOnlyList<ShopBundleSkillEntry> Skills => _skills;
    }

    [Serializable]
    public sealed class ShopGoldOffer
    {
        [SerializeField] private int _coinAmount;
        [SerializeField] private Sprite _coinStackIcon;
        [SerializeField] private string _priceLabel;
        [SerializeField] private bool _isRewardedAd;

        public ShopGoldOffer(
            int coinAmount,
            Sprite coinStackIcon,
            string priceLabel,
            bool isRewardedAd)
        {
            _coinAmount = coinAmount;
            _coinStackIcon = coinStackIcon;
            _priceLabel = priceLabel;
            _isRewardedAd = isRewardedAd;
        }

        public int CoinAmount => _coinAmount;
        public Sprite CoinStackIcon => _coinStackIcon;
        public string PriceLabel => _priceLabel;
        public bool IsRewardedAd => _isRewardedAd;
    }

    /// UI-facing shop content (prices are placeholders until real store
    /// integration lands): the Remove Ads offer, coin/power bundles, and
    /// gold packs. Rebuilding the Shop page via ShopContentSceneSetup
    /// re-reads this asset, so prices/discounts/icons can be retuned here
    /// without touching scene or presenter code.
    [CreateAssetMenu(
        fileName = "ShopCatalog",
        menuName = "Cutrium/Shop/Shop Catalog")]
    public sealed class ShopCatalog : ScriptableObject
    {
        [SerializeField] private string _removeAdsPriceLabel = "$0.00";
        [SerializeField]
        private ShopBundleOffer[] _bundles = Array.Empty<ShopBundleOffer>();
        [SerializeField]
        private ShopGoldOffer[] _goldOffers = Array.Empty<ShopGoldOffer>();

        public string RemoveAdsPriceLabel => _removeAdsPriceLabel;
        public IReadOnlyList<ShopBundleOffer> Bundles => _bundles;
        public IReadOnlyList<ShopGoldOffer> GoldOffers => _goldOffers;

        public void ConfigureForSetup(
            string removeAdsPriceLabel,
            ShopBundleOffer[] bundles,
            ShopGoldOffer[] goldOffers)
        {
            _removeAdsPriceLabel = removeAdsPriceLabel;
            _bundles = bundles ?? Array.Empty<ShopBundleOffer>();
            _goldOffers = goldOffers ?? Array.Empty<ShopGoldOffer>();
        }
    }
}
