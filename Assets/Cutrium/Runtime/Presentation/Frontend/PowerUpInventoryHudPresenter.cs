using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Economy;
using Cutrium.Unity.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Frontend
{
    /// Keeps Home's compact skill stack synchronized with the persistent
    /// inventory service. It displays owned quantities, not level free grants.
    [DisallowMultipleComponent]
    public sealed class PowerUpInventoryHudPresenter : MonoBehaviour
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private PowerUpKind _kind;
            [SerializeField] private TMP_Text _quantityText;
            [SerializeField] private Button _clickButton;

            public Entry(
                PowerUpKind kind,
                TMP_Text quantityText,
                Button clickButton)
            {
                PowerUpInventory.ValidateKind(kind);
                _kind = kind;
                _quantityText = quantityText;
                _clickButton = clickButton;
            }

            public PowerUpKind Kind => _kind;
            public TMP_Text QuantityText => _quantityText;
            public Button ClickButton => _clickButton;
        }

        [SerializeField] private CloudServicesBootstrap _cloudServices;
        [SerializeField] private FrontEndPresenter _frontEndPresenter;
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        private PowerUpInventoryService _subscribedInventory;

        public CloudServicesBootstrap CloudServices => _cloudServices;
        public FrontEndPresenter FrontEndPresenter => _frontEndPresenter;
        public IReadOnlyList<Entry> Entries => _entries;

        public void ConfigureForSetup(
            CloudServicesBootstrap cloudServices,
            FrontEndPresenter frontEndPresenter,
            Entry[] entries)
        {
            Unsubscribe();
            _cloudServices = cloudServices
                ?? throw new ArgumentNullException(nameof(cloudServices));
            _frontEndPresenter = frontEndPresenter
                ?? throw new ArgumentNullException(nameof(frontEndPresenter));
            _entries = entries ?? Array.Empty<Entry>();
            ValidateEntries(_entries);
            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
            }

            RefreshNow();
        }

        public void RefreshNow()
        {
            PowerUpInventoryService inventory =
                Application.isPlaying && _cloudServices != null
                    ? _cloudServices.PowerUps
                    : null;
            for (int index = 0; index < _entries.Length; index++)
            {
                Entry entry = _entries[index];
                if (entry?.QuantityText == null)
                {
                    continue;
                }

                int quantity = inventory != null
                    ? inventory.GetCount(entry.Kind)
                    : 0;
                entry.QuantityText.text = $"x{quantity:N0}";
            }
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
            if (_subscribedInventory != null || _cloudServices == null)
            {
                return;
            }

            _subscribedInventory = _cloudServices.PowerUps;
            _subscribedInventory.InventoryChanged += OnInventoryChanged;
            for (int index = 0; index < _entries.Length; index++)
            {
                _entries[index]?.ClickButton?.onClick.AddListener(
                    OnEntryClicked);
            }
        }

        private void Unsubscribe()
        {
            if (_subscribedInventory != null)
            {
                _subscribedInventory.InventoryChanged -= OnInventoryChanged;
                _subscribedInventory = null;
            }

            for (int index = 0; index < _entries.Length; index++)
            {
                _entries[index]?.ClickButton?.onClick.RemoveListener(
                    OnEntryClicked);
            }
        }

        private void OnInventoryChanged(PowerUpInventoryChangedEvent change) =>
            RefreshNow();

        // Any skill icon jumps to the Shop -- the player almost always taps
        // it wanting to buy more, not to inspect which one is which.
        private void OnEntryClicked() => _frontEndPresenter?.GoToShopTab();

        private static void ValidateEntries(Entry[] entries)
        {
            var seen = new HashSet<PowerUpKind>();
            for (int index = 0; index < entries.Length; index++)
            {
                Entry entry = entries[index]
                    ?? throw new ArgumentException(
                        "Inventory HUD entries cannot be null.",
                        nameof(entries));
                PowerUpInventory.ValidateKind(entry.Kind);
                if (entry.QuantityText == null
                    || entry.ClickButton == null
                    || !seen.Add(entry.Kind))
                {
                    throw new ArgumentException(
                        "Inventory HUD entries require one quantity label "
                        + "and one click button per unique power-up.",
                        nameof(entries));
                }
            }
        }
    }
}
