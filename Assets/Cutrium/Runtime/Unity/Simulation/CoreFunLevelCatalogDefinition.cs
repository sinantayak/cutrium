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

        public CoreFunLevelCatalog BuildRuntimeCatalog()
        {
            if (_levels == null || _levels.Length == 0)
            {
                throw new InvalidOperationException(
                    "A gameplay level catalog needs at least one level.");
            }

            var configurations =
                new CoreFunLevelConfiguration[_levels.Length];
            for (int index = 0; index < _levels.Length; index++)
            {
                CoreFunLevelDefinition level = _levels[index]
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
