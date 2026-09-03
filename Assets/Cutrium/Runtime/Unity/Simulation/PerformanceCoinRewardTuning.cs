using System;
using Cutrium.Gameplay.Economy;
using UnityEngine;

namespace Cutrium.Unity.Simulation
{
    /// Data-driven Coin amounts for Task 03's performance bonuses. Mirrors
    /// FeedbackTuningDefinition's own asset/runtime-configuration split so
    /// designers retune these without touching code.
    [CreateAssetMenu(
        fileName = "PerformanceCoinRewardTuning",
        menuName = "Cutrium/Performance Coin Reward Tuning")]
    public sealed class PerformanceCoinRewardTuning : ScriptableObject
    {
        [Header("Per-Occurrence Bonuses")]
        [SerializeField]
        [Min(0)]
        private int _nearMissCoinsPerOccurrence = 10;

        [SerializeField]
        [Min(0)]
        private int _perfectCutCoinsPerOccurrence = 20;

        [Header("Whole-Completion Bonuses")]
        [SerializeField]
        [Min(0)]
        private int _noLifeLostCoins = 30;

        [SerializeField]
        [Min(0)]
        private int _noPowerUpUsedCoins = 30;

        public int NearMissCoinsPerOccurrence => _nearMissCoinsPerOccurrence;

        public int PerfectCutCoinsPerOccurrence =>
            _perfectCutCoinsPerOccurrence;

        public int NoLifeLostCoins => _noLifeLostCoins;

        public int NoPowerUpUsedCoins => _noPowerUpUsedCoins;

        public PerformanceCoinRewardConfiguration ToRuntimeConfiguration() =>
            new PerformanceCoinRewardConfiguration(
                _nearMissCoinsPerOccurrence,
                _perfectCutCoinsPerOccurrence,
                _noLifeLostCoins,
                _noPowerUpUsedCoins);

        public void ConfigureForSetup(
            int nearMissCoinsPerOccurrence,
            int perfectCutCoinsPerOccurrence,
            int noLifeLostCoins,
            int noPowerUpUsedCoins)
        {
            _nearMissCoinsPerOccurrence = ValidateNonNegative(
                nearMissCoinsPerOccurrence,
                nameof(nearMissCoinsPerOccurrence));
            _perfectCutCoinsPerOccurrence = ValidateNonNegative(
                perfectCutCoinsPerOccurrence,
                nameof(perfectCutCoinsPerOccurrence));
            _noLifeLostCoins = ValidateNonNegative(
                noLifeLostCoins,
                nameof(noLifeLostCoins));
            _noPowerUpUsedCoins = ValidateNonNegative(
                noPowerUpUsedCoins,
                nameof(noPowerUpUsedCoins));
            _ = ToRuntimeConfiguration();
        }

        private static int ValidateNonNegative(int value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }
    }
}
