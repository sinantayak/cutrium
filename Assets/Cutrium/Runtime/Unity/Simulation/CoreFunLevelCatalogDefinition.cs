using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Session;
using UnityEngine;

namespace Cutrium.Unity.Simulation
{
    [CreateAssetMenu(
        fileName = "GameplayLevelCatalog",
        menuName = "Cutrium/Gameplay Level Catalog")]
    public sealed class CoreFunLevelCatalogDefinition : ScriptableObject
    {
        [SerializeField]
        private CoreFunLevelDefinition[] _levels =
            Array.Empty<CoreFunLevelDefinition>();

        public IReadOnlyList<CoreFunLevelDefinition> Levels => _levels;

        public bool IsChapterOneCatalog =>
            _levels != null
            && _levels.Length == FirstTwelveGameplayProgression.LevelCount
            && _levels[0] != null
            && _levels[11] != null
            && string.Equals(
                _levels[0].StableId,
                "learn-the-cut",
                StringComparison.Ordinal)
            && string.Equals(
                _levels[11].StableId,
                "first-twelve-mastery",
                StringComparison.Ordinal);

        public bool IsSupersededFirstTwelveCatalog =>
            _levels != null
            && _levels.Length == FirstTwelveGameplayProgression.LevelCount
            && _levels[0] != null
            && _levels[3] != null
            && _levels[4] != null
            && _levels[11] != null
            && string.Equals(
                _levels[0].StableId,
                "learn-the-cut",
                StringComparison.Ordinal)
            && string.Equals(
                _levels[11].StableId,
                "first-twelve-mastery",
                StringComparison.Ordinal)
            && _levels[3].MaximumAcceptedCuts == 0
            && _levels[4].Threats.Count == 1
            && _levels[4].Threats[0].Behavior != null
            && _levels[4].Threats[0].Behavior.Kind
                == CoreFunThreatBehaviorKind.Hunter
            && _levels[4].Threats[0].Behavior.HunterSteerFactor < 0.5f;

        public IReadOnlyList<CoreFunLevelDefinition> EffectiveLevels =>
            IsChapterOneCatalog
                ? MainGameplayProgression.CreateDefinitions()
                : _levels;

        public CoreFunLevelCatalog BuildRuntimeCatalog()
        {
            IReadOnlyList<CoreFunLevelDefinition> effective = EffectiveLevels;
            if (effective == null || effective.Count == 0)
            {
                throw new InvalidOperationException(
                    "A gameplay level catalog needs at least one level.");
            }

            var configurations =
                new CoreFunLevelConfiguration[effective.Count];
            for (int index = 0; index < effective.Count; index++)
            {
                CoreFunLevelDefinition level = effective[index]
                    ?? throw new InvalidOperationException(
                        "Gameplay level catalogs cannot contain null entries.");
                configurations[index] = level.ToRuntimeConfiguration();
            }

            return new CoreFunLevelCatalog(configurations);
        }

        public void ConfigureForSetup(
            IReadOnlyList<CoreFunLevelDefinition> levels)
        {
            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            var copy = new CoreFunLevelDefinition[levels.Count];
            for (int index = 0; index < levels.Count; index++)
            {
                copy[index] = levels[index]
                    ?? throw new ArgumentException(
                        "Gameplay level catalogs cannot contain null entries.",
                        nameof(levels));
            }

            _levels = copy;
            _ = BuildRuntimeCatalog();
        }
    }
}
