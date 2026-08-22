using System;
using Cutrium.Presentation.Localization;
using UnityEngine;

namespace Cutrium.Presentation.Landmark
{
    [CreateAssetMenu(
        fileName = "LandmarkDefinition",
        menuName = "Cutrium/Landmark Definition")]
    public sealed class LandmarkDefinition : ScriptableObject
    {
        [SerializeField] private string _landmarkId = "landmark";

        [Header("English")]
        [SerializeField] private string _displayTitle = "Untitled Landmark";

        [SerializeField]
        [TextArea(2, 5)]
        private string _shortDescription = string.Empty;

        [SerializeField] private string _sector = string.Empty;

        [Header("Turkish Localization")]
        [SerializeField] private string _displayTitleTurkish = string.Empty;

        [SerializeField]
        [TextArea(2, 5)]
        private string _shortDescriptionTurkish = string.Empty;

        [SerializeField] private string _sectorTurkish = string.Empty;

        [SerializeField] private Sprite _artwork;

        public string LandmarkId => _landmarkId;
        public string DisplayTitle => _displayTitle;
        public string ShortDescription => _shortDescription;
        public string Sector => _sector;
        public Sprite Artwork => _artwork;

        public string GetDisplayTitle(SupportedLanguage language) =>
            SelectLocalizedValue(
                language,
                _displayTitle,
                _displayTitleTurkish);

        public string GetShortDescription(SupportedLanguage language) =>
            SelectLocalizedValue(
                language,
                _shortDescription,
                _shortDescriptionTurkish);

        public string GetSector(SupportedLanguage language) =>
            SelectLocalizedValue(language, _sector, _sectorTurkish);

        public void ConfigureForSetup(
            string landmarkId,
            string displayTitle,
            string shortDescription,
            string sector,
            Sprite artwork)
        {
            if (string.IsNullOrWhiteSpace(landmarkId))
            {
                throw new ArgumentException(
                    "A landmark needs a stable ID.",
                    nameof(landmarkId));
            }

            if (string.IsNullOrWhiteSpace(displayTitle))
            {
                throw new ArgumentException(
                    "A landmark needs a display title.",
                    nameof(displayTitle));
            }

            _landmarkId = landmarkId;
            _displayTitle = displayTitle;
            _shortDescription = shortDescription ?? string.Empty;
            _sector = sector ?? string.Empty;
            _displayTitleTurkish = string.Empty;
            _shortDescriptionTurkish = string.Empty;
            _sectorTurkish = string.Empty;
            _artwork = artwork;
        }

        public void ConfigureLocalizedForSetup(
            string landmarkId,
            string displayTitleEnglish,
            string shortDescriptionEnglish,
            string sectorEnglish,
            string displayTitleTurkish,
            string shortDescriptionTurkish,
            string sectorTurkish,
            Sprite artwork)
        {
            string validatedTurkishTitle = RequireLocalizedTitle(
                displayTitleTurkish,
                nameof(displayTitleTurkish));
            ConfigureForSetup(
                landmarkId,
                displayTitleEnglish,
                shortDescriptionEnglish,
                sectorEnglish,
                artwork);
            _displayTitleTurkish = validatedTurkishTitle;
            _shortDescriptionTurkish =
                shortDescriptionTurkish ?? string.Empty;
            _sectorTurkish = sectorTurkish ?? string.Empty;
        }

        private static string SelectLocalizedValue(
            SupportedLanguage language,
            string english,
            string turkish)
        {
            if (language == SupportedLanguage.Turkish
                && !string.IsNullOrEmpty(turkish))
            {
                return turkish;
            }

            return english ?? string.Empty;
        }

        private static string RequireLocalizedTitle(
            string title,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(
                    "A localized landmark needs a display title.",
                    parameterName);
            }

            return title;
        }
    }
}
