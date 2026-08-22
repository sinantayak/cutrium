using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cutrium.Presentation.Localization
{
    [Serializable]
    public sealed class LocalizationEntry
    {
        [SerializeField] private string _english = string.Empty;
        [SerializeField] private string _turkish = string.Empty;

        public LocalizationEntry(string english, string turkish)
        {
            _english = english ?? string.Empty;
            _turkish = turkish ?? string.Empty;
        }

        public string English => _english;
        public string Turkish => _turkish;
    }

    [CreateAssetMenu(
        fileName = "LocalizationTable",
        menuName = "Cutrium/Localization Table")]
    public sealed class LocalizationTable : ScriptableObject
    {
        [SerializeField]
        private LocalizationEntry[] _entries =
            Array.Empty<LocalizationEntry>();

        public IReadOnlyList<LocalizationEntry> Entries => _entries;

        public void ConfigureForSetup(LocalizationEntry[] entries)
        {
            _entries = entries != null
                ? (LocalizationEntry[])entries.Clone()
                : Array.Empty<LocalizationEntry>();
        }
    }
}
