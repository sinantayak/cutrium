using System;
using UnityEngine;

namespace Cutrium.Presentation.Landmark
{
    [CreateAssetMenu(
        fileName = "LandmarkDefinition",
        menuName = "Cutrium/Landmark Definition")]
    public sealed class LandmarkDefinition : ScriptableObject
    {
        [SerializeField] private string _landmarkId = "landmark";

        [SerializeField] private string _displayTitle = "Untitled Landmark";

        [SerializeField]
        [TextArea(2, 5)]
        private string _shortDescription = string.Empty;

        [SerializeField] private string _sector = string.Empty;

        [SerializeField] private Sprite _artwork;

        public string LandmarkId => _landmarkId;
        public string DisplayTitle => _displayTitle;
        public string ShortDescription => _shortDescription;
        public string Sector => _sector;
        public Sprite Artwork => _artwork;

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
            _artwork = artwork;
        }
    }
}
