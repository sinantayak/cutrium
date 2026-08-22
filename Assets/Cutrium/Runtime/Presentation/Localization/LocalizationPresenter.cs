using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Localization
{
    [Serializable]
    public sealed class LocalizationLabelBinding
    {
        [SerializeField] private Text _legacyText;
        [SerializeField] private TMP_Text _tmpText;
        [SerializeField] private string _source = string.Empty;

        [NonSerialized] private string _lastApplied;

        public LocalizationLabelBinding(Text label, string source)
        {
            _legacyText = label;
            _tmpText = null;
            _source = source ?? string.Empty;
        }

        public LocalizationLabelBinding(TMP_Text label, string source)
        {
            _legacyText = null;
            _tmpText = label;
            _source = source ?? string.Empty;
        }

        public Text LegacyText => _legacyText;
        public TMP_Text TmpText => _tmpText;
        public string Source => _source;
        public bool HasLabel => _legacyText != null || _tmpText != null;

        public void RefreshSourceIfChanged()
        {
            string current = Read();
            if (!string.Equals(current, _lastApplied, StringComparison.Ordinal))
            {
                _source = current;
            }
        }

        public void Apply(string value)
        {
            string safeValue = value ?? string.Empty;
            if (string.Equals(Read(), safeValue, StringComparison.Ordinal))
            {
                _lastApplied = safeValue;
                return;
            }

            if (_legacyText != null)
            {
                _legacyText.text = safeValue;
            }
            else if (_tmpText != null)
            {
                _tmpText.text = safeValue;
            }

            _lastApplied = safeValue;
        }

        private string Read()
        {
            if (_legacyText != null)
            {
                return _legacyText.text ?? string.Empty;
            }

            return _tmpText != null
                ? _tmpText.text ?? string.Empty
                : string.Empty;
        }
    }

    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class LocalizationPresenter : MonoBehaviour
    {
        [SerializeField] private LocalizationService _service;
        [SerializeField]
        private LocalizationLabelBinding[] _bindings =
            Array.Empty<LocalizationLabelBinding>();

        private bool _subscribed;

        public LocalizationService Service => _service;
        public int LabelCount => _bindings?.Length ?? 0;
        public IReadOnlyList<LocalizationLabelBinding> Bindings => _bindings;

        public void ConfigureForSetup(
            LocalizationService service,
            Text[] legacyLabels,
            TMP_Text[] tmpLabels)
        {
            Unsubscribe();
            _service = service;
            var bindings = new List<LocalizationLabelBinding>();
            if (legacyLabels != null)
            {
                foreach (Text label in legacyLabels)
                {
                    if (label != null)
                    {
                        bindings.Add(new LocalizationLabelBinding(
                            label,
                            label.text));
                    }
                }
            }

            if (tmpLabels != null)
            {
                foreach (TMP_Text label in tmpLabels)
                {
                    if (label != null)
                    {
                        bindings.Add(new LocalizationLabelBinding(
                            label,
                            label.text));
                    }
                }
            }

            _bindings = bindings.ToArray();
            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
                RefreshNow();
            }
        }

        public void RefreshNow()
        {
            if (_service == null || _bindings == null)
            {
                return;
            }

            foreach (LocalizationLabelBinding binding in _bindings)
            {
                if (binding == null || !binding.HasLabel)
                {
                    continue;
                }

                binding.RefreshSourceIfChanged();
                binding.Apply(_service.Localize(binding.Source));
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Subscribe();
            RefreshNow();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private void Subscribe()
        {
            if (_subscribed || _service == null)
            {
                return;
            }

            _service.LanguageChanged += OnLanguageChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_service != null)
            {
                _service.LanguageChanged -= OnLanguageChanged;
            }

            _subscribed = false;
        }

        private void OnLanguageChanged(SupportedLanguage language)
        {
            RefreshNow();
        }
    }
}
