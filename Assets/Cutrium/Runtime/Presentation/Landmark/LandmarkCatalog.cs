using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cutrium.Presentation.Landmark
{
    [CreateAssetMenu(
        fileName = "LandmarkCatalog",
        menuName = "Cutrium/Landmark Catalog")]
    public sealed class LandmarkCatalog : ScriptableObject
    {
        [SerializeField]
        private LandmarkDefinition[] _landmarks =
            Array.Empty<LandmarkDefinition>();

        public IReadOnlyList<LandmarkDefinition> Landmarks => _landmarks;

        public int Count => _landmarks?.Length ?? 0;

        public LandmarkDefinition SelectForProgressionIndex(int index)
        {
            Validate();
            int wrappedIndex = index % _landmarks.Length;
            if (wrappedIndex < 0)
            {
                wrappedIndex += _landmarks.Length;
            }

            return _landmarks[wrappedIndex];
        }

        public void ConfigureForSetup(
            IReadOnlyList<LandmarkDefinition> landmarks)
        {
            if (landmarks == null)
            {
                throw new ArgumentNullException(nameof(landmarks));
            }

            _landmarks = new LandmarkDefinition[landmarks.Count];
            for (int index = 0; index < landmarks.Count; index++)
            {
                _landmarks[index] = landmarks[index];
            }

            Validate();
        }

        public void Validate()
        {
            if (_landmarks == null || _landmarks.Length == 0)
            {
                throw new InvalidOperationException(
                    "A landmark catalog needs at least one landmark.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < _landmarks.Length; index++)
            {
                LandmarkDefinition landmark = _landmarks[index]
                    ?? throw new InvalidOperationException(
                        "Landmark catalogs cannot contain null entries.");
                if (string.IsNullOrWhiteSpace(landmark.LandmarkId)
                    || !ids.Add(landmark.LandmarkId))
                {
                    throw new InvalidOperationException(
                        "Landmark catalogs require unique non-empty IDs.");
                }
            }
        }
    }
}
