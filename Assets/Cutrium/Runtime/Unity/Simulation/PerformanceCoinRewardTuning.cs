using System;
using Cutrium.Gameplay.Economy;
using UnityEngine;

namespace Cutrium.Unity.Simulation
{
    /// Data-driven Coin amounts for Task 03's performance bonuses and Task
    /// 12's star-scaled completion base. Mirrors FeedbackTuningDefinition's
    /// asset/runtime-configuration split so designers retune these without
    /// touching code.
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

        [Header("Star-Scaled Completion Reward")]
        [SerializeField]
        [Range(0, 100)]
        private int _oneStarRewardPercent = 50;

        [SerializeField]
        [Range(0, 100)]
        private int _twoStarRewardPercent = 75;

        [SerializeField]
        [Range(0, 100)]
        private int _threeStarRewardPercent = 100;

        public int NearMissCoinsPerOccurrence => _nearMissCoinsPerOccurrence;

        public int PerfectCutCoinsPerOccurrence =>
            _perfectCutCoinsPerOccurrence;

        public int NoLifeLostCoins => _noLifeLostCoins;

        public int NoPowerUpUsedCoins => _noPowerUpUsedCoins;

        public int OneStarRewardPercent => _oneStarRewardPercent;

        public int TwoStarRewardPercent => _twoStarRewardPercent;

        public int ThreeStarRewardPercent => _threeStarRewardPercent;

        public PerformanceCoinRewardConfiguration ToRuntimeConfiguration() =>
            new PerformanceCoinRewardConfiguration(
                _nearMissCoinsPerOccurrence,
                _perfectCutCoinsPerOccurrence,
                _noLifeLostCoins,
                _noPowerUpUsedCoins);

        public LevelStarCoinRewardConfiguration ToStarRewardConfiguration() =>
            new LevelStarCoinRewardConfiguration(
                _oneStarRewardPercent,
                _twoStarRewardPercent,
                _threeStarRewardPercent);

        public void ConfigureForSetup(
            int nearMissCoinsPerOccurrence,
            int perfectCutCoinsPerOccurrence,
            int noLifeLostCoins,
            int noPowerUpUsedCoins,
            int oneStarRewardPercent = 50,
            int twoStarRewardPercent = 75,
            int threeStarRewardPercent = 100)
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
            LevelStarCoinRewardConfiguration starConfiguration =
                new LevelStarCoinRewardConfiguration(
                    oneStarRewardPercent,
                    twoStarRewardPercent,
                    threeStarRewardPercent);
            _oneStarRewardPercent = starConfiguration.OneStarPercent;
            _twoStarRewardPercent = starConfiguration.TwoStarPercent;
            _threeStarRewardPercent = starConfiguration.ThreeStarPercent;
            _ = ToRuntimeConfiguration();
            _ = ToStarRewardConfiguration();
        }

        private void OnValidate()
        {
            _nearMissCoinsPerOccurrence = Mathf.Max(
                0,
                _nearMissCoinsPerOccurrence);
            _perfectCutCoinsPerOccurrence = Mathf.Max(
                0,
                _perfectCutCoinsPerOccurrence);
            _noLifeLostCoins = Mathf.Max(0, _noLifeLostCoins);
            _noPowerUpUsedCoins = Mathf.Max(0, _noPowerUpUsedCoins);
            _oneStarRewardPercent = Mathf.Clamp(
                _oneStarRewardPercent,
                0,
                100);
            _twoStarRewardPercent = Mathf.Clamp(
                _twoStarRewardPercent,
                _oneStarRewardPercent,
                100);
            _threeStarRewardPercent = Mathf.Clamp(
                _threeStarRewardPercent,
                _twoStarRewardPercent,
                100);
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
