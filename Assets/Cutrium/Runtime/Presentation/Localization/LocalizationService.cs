using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Cutrium.Presentation.Localization
{
    [DisallowMultipleComponent]
    public sealed class LocalizationService : MonoBehaviour
    {
        public const string LanguagePreferenceKey =
            "Cutrium.Settings.Language";

        private static readonly Regex PlayLevelPattern = new Regex(
            @"^PLAY LEVEL (\d+)$",
            RegexOptions.CultureInvariant);
        private static readonly Regex LevelPattern = new Regex(
            @"^LEVEL (\d+)$",
            RegexOptions.CultureInvariant);
        private static readonly Regex TargetUpperPattern = new Regex(
            @"^TARGET (\d+)%$",
            RegexOptions.CultureInvariant);
        private static readonly Regex TargetTitlePattern = new Regex(
            @"^Target (\d+)%$",
            RegexOptions.CultureInvariant);
        private static readonly Regex CapturedTitlePattern = new Regex(
            @"^Captured (\d+)%$",
            RegexOptions.CultureInvariant);
        private static readonly Regex CutLimitPattern = new Regex(
            @"^CUT: (\d+)/(\d+)$",
            RegexOptions.CultureInvariant);
        private static readonly Regex ComboPattern = new Regex(
            @"^COMBO x(\d+)$",
            RegexOptions.CultureInvariant);
        private static readonly Regex CutsPattern = new Regex(
            @"^(\d+) CUTS?$",
            RegexOptions.CultureInvariant);
        private static readonly Regex CompletionLevelPattern = new Regex(
            @"LEVEL (\d+) COMPLETE",
            RegexOptions.CultureInvariant);
        private static readonly Regex CompletionCapturedPattern = new Regex(
            @"CAPTURED (\d+)%",
            RegexOptions.CultureInvariant);
        private static readonly Regex CompletionCapturedTitlePattern =
            new Regex(
                @"Captured (\d+)%",
                RegexOptions.CultureInvariant);
        private static readonly Regex CompletionCutsPattern = new Regex(
            @"(\d+) CUTS?",
            RegexOptions.CultureInvariant);
        private static readonly Regex CompletionTimePattern = new Regex(
            @"TIME ([0-9.,]+)s",
            RegexOptions.CultureInvariant);
        private static readonly Regex CompletionBrokenPattern = new Regex(
            @"BROKEN (\d+)",
            RegexOptions.CultureInvariant);
        private static readonly Regex CompletionTimeTitlePattern = new Regex(
            @"Time ([0-9.,]+)s",
            RegexOptions.CultureInvariant);
        private static readonly Regex CompletionAttemptsPattern = new Regex(
            @"Attempts (\d+)",
            RegexOptions.CultureInvariant);
        private static readonly Regex CompletionBreaksPattern = new Regex(
            @"Breaks (\d+)",
            RegexOptions.CultureInvariant);

        [SerializeField] private LocalizationTable _table;
        [SerializeField] private SupportedLanguage _defaultLanguage =
            SupportedLanguage.English;
        [SerializeField] private bool _persistPreference = true;

        private readonly Dictionary<string, string> _englishLookup =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _turkishLookup =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private bool _initialized;

        public LocalizationTable Table => _table;
        public SupportedLanguage CurrentLanguage { get; private set; } =
            SupportedLanguage.English;
        public bool PersistsPreference => _persistPreference;

        public event Action<SupportedLanguage> LanguageChanged;

        public void ConfigureForSetup(
            LocalizationTable table,
            bool persistPreference = true,
            SupportedLanguage defaultLanguage = SupportedLanguage.English)
        {
            _table = table;
            _persistPreference = persistPreference;
            _defaultLanguage = defaultLanguage;
            _initialized = false;
            CurrentLanguage = defaultLanguage;
            if (Application.isPlaying)
            {
                Initialize();
            }
        }

        public string Localize(string source)
        {
            Initialize();
            if (string.IsNullOrEmpty(source))
            {
                return source ?? string.Empty;
            }

            Dictionary<string, string> lookup = CurrentLanguage
                == SupportedLanguage.Turkish
                    ? _turkishLookup
                    : _englishLookup;
            if (lookup.TryGetValue(source, out string localized))
            {
                return localized;
            }

            return CurrentLanguage == SupportedLanguage.Turkish
                ? LocalizeDynamicTurkish(source)
                : source;
        }

        public void SetLanguage(
            SupportedLanguage language,
            bool savePreference = true)
        {
            Initialize();
            if (!Enum.IsDefined(typeof(SupportedLanguage), language))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(language),
                    language,
                    "Unsupported Cutrium language.");
            }

            bool changed = CurrentLanguage != language;
            CurrentLanguage = language;
            if (savePreference && _persistPreference)
            {
                PlayerPrefs.SetInt(LanguagePreferenceKey, (int)language);
                PlayerPrefs.Save();
            }

            if (changed)
            {
                LanguageChanged?.Invoke(language);
            }
        }

        public void ToggleLanguage()
        {
            SetLanguage(CurrentLanguage == SupportedLanguage.English
                ? SupportedLanguage.Turkish
                : SupportedLanguage.English);
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _englishLookup.Clear();
            _turkishLookup.Clear();
            if (_table != null)
            {
                foreach (LocalizationEntry entry in _table.Entries)
                {
                    AddEntry(entry);
                }
            }

            CurrentLanguage = _defaultLanguage;
            if (_persistPreference
                && PlayerPrefs.HasKey(LanguagePreferenceKey))
            {
                int stored = PlayerPrefs.GetInt(
                    LanguagePreferenceKey,
                    (int)_defaultLanguage);
                if (Enum.IsDefined(typeof(SupportedLanguage), stored))
                {
                    CurrentLanguage = (SupportedLanguage)stored;
                }
            }

            _initialized = true;
        }

        private void AddEntry(LocalizationEntry entry)
        {
            if (entry == null
                || string.IsNullOrEmpty(entry.English)
                || string.IsNullOrEmpty(entry.Turkish))
            {
                return;
            }

            AddLookupValue(_englishLookup, entry.English, entry.English);
            AddLookupValue(_englishLookup, entry.Turkish, entry.English);
            AddLookupValue(_turkishLookup, entry.English, entry.Turkish);
            AddLookupValue(_turkishLookup, entry.Turkish, entry.Turkish);
        }

        private static void AddLookupValue(
            IDictionary<string, string> lookup,
            string source,
            string localized)
        {
            if (!lookup.ContainsKey(source))
            {
                lookup.Add(source, localized);
            }
        }

        private static string LocalizeDynamicTurkish(string source)
        {
            Match match = PlayLevelPattern.Match(source);
            if (match.Success)
            {
                return $"SEVİYE {match.Groups[1].Value} OYNA";
            }

            match = LevelPattern.Match(source);
            if (match.Success)
            {
                return $"SEVİYE {match.Groups[1].Value}";
            }

            match = TargetUpperPattern.Match(source);
            if (match.Success)
            {
                return $"HEDEF %{match.Groups[1].Value}";
            }

            match = TargetTitlePattern.Match(source);
            if (match.Success)
            {
                return $"Hedef %{match.Groups[1].Value}";
            }

            match = CapturedTitlePattern.Match(source);
            if (match.Success)
            {
                return $"Kaplanan %{match.Groups[1].Value}";
            }

            match = CutLimitPattern.Match(source);
            if (match.Success)
            {
                return $"KESİM: {match.Groups[1].Value}/" +
                    match.Groups[2].Value;
            }

            match = ComboPattern.Match(source);
            if (match.Success)
            {
                return $"KOMBO x{match.Groups[1].Value}";
            }

            match = CutsPattern.Match(source);
            if (match.Success)
            {
                return $"{match.Groups[1].Value} KESİM";
            }

            if (CompletionLevelPattern.IsMatch(source))
            {
                string localized = CompletionLevelPattern.Replace(
                    source,
                    "SEVİYE $1 TAMAMLANDI");
                localized = CompletionCapturedPattern.Replace(
                    localized,
                    "KAPLANAN %$1");
                localized = CompletionCapturedTitlePattern.Replace(
                    localized,
                    "Kaplanan %$1");
                localized = CompletionCutsPattern.Replace(
                    localized,
                    "$1 KESİM");
                localized = CompletionTimePattern.Replace(
                    localized,
                    "SÜRE $1sn");
                localized = CompletionBrokenPattern.Replace(
                    localized,
                    "KIRILAN $1");
                localized = CompletionTimeTitlePattern.Replace(
                    localized,
                    "Süre $1sn");
                localized = CompletionAttemptsPattern.Replace(
                    localized,
                    "Deneme $1");
                return CompletionBreaksPattern.Replace(
                    localized,
                    "Kırılan $1");
            }

            return source;
        }
    }
}
