using System;
using System.Collections.Generic;

namespace Cutrium.Gameplay.Session
{
    public sealed class CoreFunLevelCatalog
    {
        private readonly CoreFunLevelConfiguration[] _levels;

        public CoreFunLevelCatalog(
            IReadOnlyList<CoreFunLevelConfiguration> levels)
        {
            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            if (levels.Count == 0)
            {
                throw new ArgumentException(
                    "A level catalog cannot be empty.",
                    nameof(levels));
            }

            _levels = new CoreFunLevelConfiguration[levels.Count];
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < levels.Count; index++)
            {
                CoreFunLevelConfiguration level = levels[index];
                if (level.DisplayNumber != index + 1)
                {
                    throw new ArgumentException(
                        "Catalog display numbers must be ordered and contiguous.",
                        nameof(levels));
                }

                if (!stableIds.Add(level.StableId))
                {
                    throw new ArgumentException(
                        $"Duplicate stable level ID '{level.StableId}'.",
                        nameof(levels));
                }

                _levels[index] = level;
            }
        }

        public int Count => _levels.Length;

        public CoreFunLevelConfiguration this[int index] => _levels[index];
    }
}
