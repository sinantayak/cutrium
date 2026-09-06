using System;
using Cutrium.Gameplay.Economy;
using Cutrium.Gameplay.Session;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class PerformanceCoinRewardTests
    {
        private static readonly PerformanceCoinRewardConfiguration Tuning =
            new PerformanceCoinRewardConfiguration(10, 20, 30, 30);

        [Test]
        public void Configuration_RejectsNegativeAmounts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PerformanceCoinRewardConfiguration(-1, 20, 30, 30));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PerformanceCoinRewardConfiguration(10, -1, 30, 30));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PerformanceCoinRewardConfiguration(10, 20, -1, 30));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PerformanceCoinRewardConfiguration(10, 20, 30, -1));
        }

        [Test]
        public void Configuration_DefaultMatchesRoadmapInitialCandidates()
        {
            PerformanceCoinRewardConfiguration defaults =
                PerformanceCoinRewardConfiguration.Default;
            Assert.That(defaults.NearMissCoinsPerOccurrence, Is.EqualTo(10));
            Assert.That(defaults.PerfectCutCoinsPerOccurrence, Is.EqualTo(20));
            Assert.That(defaults.NoLifeLostCoins, Is.EqualTo(30));
            Assert.That(defaults.NoPowerUpUsedCoins, Is.EqualTo(30));
        }

        [Test]
        public void Calculate_NoSignalsEarnsNoBonus()
        {
            PerformanceCoinRewardBreakdown breakdown =
                PerformanceCoinRewardCalculator.Calculate(
                    0, 0, false, true, true, Tuning);

            Assert.That(breakdown.Lines, Is.Empty);
            Assert.That(breakdown.TotalCoinAmount, Is.Zero);
        }

        [Test]
        public void Calculate_NearMissAndPerfectCutScaleWithOccurrenceCount()
        {
            PerformanceCoinRewardBreakdown breakdown =
                PerformanceCoinRewardCalculator.Calculate(
                    3, 2, false, false, false, Tuning);

            Assert.That(breakdown.Lines.Count, Is.EqualTo(2));
            Assert.That(breakdown.Lines[0].Kind,
                Is.EqualTo(PerformanceCoinRewardKind.NearMiss));
            Assert.That(breakdown.Lines[0].OccurrenceCount, Is.EqualTo(3));
            Assert.That(breakdown.Lines[0].CoinAmount, Is.EqualTo(30));
            Assert.That(breakdown.Lines[1].Kind,
                Is.EqualTo(PerformanceCoinRewardKind.PerfectCut));
            Assert.That(breakdown.Lines[1].OccurrenceCount, Is.EqualTo(2));
            Assert.That(breakdown.Lines[1].CoinAmount, Is.EqualTo(40));
            Assert.That(breakdown.TotalCoinAmount, Is.EqualTo(70));
        }

        [Test]
        public void Calculate_NoLifeLostOnlyEarnedWhenTrue()
        {
            PerformanceCoinRewardBreakdown earned =
                PerformanceCoinRewardCalculator.Calculate(
                    0, 0, true, false, false, Tuning);
            Assert.That(earned.Lines.Count, Is.EqualTo(1));
            Assert.That(earned.Lines[0].Kind,
                Is.EqualTo(PerformanceCoinRewardKind.NoLifeLost));
            Assert.That(earned.Lines[0].OccurrenceCount, Is.EqualTo(1));
            Assert.That(earned.TotalCoinAmount, Is.EqualTo(30));

            PerformanceCoinRewardBreakdown notEarned =
                PerformanceCoinRewardCalculator.Calculate(
                    0, 0, false, false, false, Tuning);
            Assert.That(notEarned.Lines, Is.Empty);
        }

        [Test]
        public void Calculate_NoPowerUpUsedRequiresEligibilityAndAbstinence()
        {
            // Eligible (the level configured a power) and never used it.
            PerformanceCoinRewardBreakdown earned =
                PerformanceCoinRewardCalculator.Calculate(
                    0, 0, false, true, false, Tuning);
            Assert.That(earned.Lines.Count, Is.EqualTo(1));
            Assert.That(earned.Lines[0].Kind,
                Is.EqualTo(PerformanceCoinRewardKind.NoPowerUpUsed));
            Assert.That(earned.TotalCoinAmount, Is.EqualTo(30));

            // Eligible but used -- no bonus.
            PerformanceCoinRewardBreakdown used =
                PerformanceCoinRewardCalculator.Calculate(
                    0, 0, false, true, true, Tuning);
            Assert.That(used.Lines, Is.Empty);

            // Not eligible at all (level has no power configured): never a
            // real bonus even though "used" is trivially false, since
            // there was nothing meaningful to withhold.
            PerformanceCoinRewardBreakdown ineligible =
                PerformanceCoinRewardCalculator.Calculate(
                    0, 0, false, false, false, Tuning);
            Assert.That(ineligible.Lines, Is.Empty);
        }

        [Test]
        public void Calculate_CombinesEveryEarnedBonusIntoOneTotal()
        {
            PerformanceCoinRewardBreakdown breakdown =
                PerformanceCoinRewardCalculator.Calculate(
                    2, 1, true, true, false, Tuning);

            Assert.That(breakdown.Lines.Count, Is.EqualTo(4));
            Assert.That(breakdown.TotalCoinAmount,
                Is.EqualTo(20 + 20 + 30 + 30));
        }

        [Test]
        public void Calculate_ZeroConfiguredAmountSuppressesThatBonusLine()
        {
            var zeroedNearMiss = new PerformanceCoinRewardConfiguration(
                0, 20, 30, 30);
            PerformanceCoinRewardBreakdown breakdown =
                PerformanceCoinRewardCalculator.Calculate(
                    5, 0, false, false, false, zeroedNearMiss);

            Assert.That(breakdown.Lines, Is.Empty);
            Assert.That(breakdown.TotalCoinAmount, Is.Zero);
        }

        [Test]
        public void Breakdown_EmptyAndDefaultBothExposeAnEmptyLineList()
        {
            Assert.That(
                PerformanceCoinRewardBreakdown.Empty.Lines,
                Is.Empty);
            Assert.That(
                PerformanceCoinRewardBreakdown.Empty.TotalCoinAmount,
                Is.Zero);

            // The default(struct) value (e.g. an uninitialized field) must
            // stay safe to read -- callers should never need a null check.
            var defaultValue = default(PerformanceCoinRewardBreakdown);
            Assert.That(defaultValue.Lines, Is.Not.Null);
            Assert.That(defaultValue.Lines, Is.Empty);
        }

        [Test]
        public void Calculate_ThrowsOnNegativeOccurrenceCounts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PerformanceCoinRewardCalculator.Calculate(
                    -1, 0, false, false, false, Tuning));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PerformanceCoinRewardCalculator.Calculate(
                    0, -1, false, false, false, Tuning));
        }

        [TestCase(false, 0, 2, 3, 0)]
        [TestCase(true, 1, 2, 3, 1)]
        [TestCase(true, 0, 4, 3, 2)]
        [TestCase(true, 0, 3, 3, 3)]
        [TestCase(true, 0, 2, 0, 2)]
        public void StarRating_UsesCumulativeRoadmapConditions(
            bool completed,
            int failedBarriers,
            int acceptedCuts,
            int expectedCuts,
            int expectedStars)
        {
            Assert.That(
                LevelStarRatingCalculator.Calculate(
                    completed,
                    failedBarriers,
                    acceptedCuts,
                    expectedCuts),
                Is.EqualTo(expectedStars));
        }

        [Test]
        public void StarRating_RejectsNegativeMetricsOrThreshold()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LevelStarRatingCalculator.Calculate(true, -1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LevelStarRatingCalculator.Calculate(true, 0, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LevelStarRatingCalculator.Calculate(true, 0, 1, -1));
        }

        [Test]
        public void StarRating_PreserveBestNeverLowersStoredResult()
        {
            Assert.That(
                LevelStarRatingCalculator.PreserveBest(3, 1),
                Is.EqualTo(3));
            Assert.That(
                LevelStarRatingCalculator.PreserveBest(1, 3),
                Is.EqualTo(3));
            Assert.That(
                LevelStarRatingCalculator.PreserveBest(2, 2),
                Is.EqualTo(2));
        }

        [TestCase(0, 0)]
        [TestCase(1, 50)]
        [TestCase(2, 75)]
        [TestCase(3, 100)]
        public void StarCoinReward_DefaultScalesConfiguredMaximum(
            int starRating,
            int expectedCoins)
        {
            Assert.That(
                LevelStarCoinRewardCalculator.Calculate(
                    100,
                    starRating,
                    LevelStarCoinRewardConfiguration.Default),
                Is.EqualTo(expectedCoins));
        }

        [Test]
        public void StarCoinReward_RoundsAndPreservesPositiveSmallTiers()
        {
            Assert.That(
                LevelStarCoinRewardCalculator.Calculate(
                    101,
                    2,
                    LevelStarCoinRewardConfiguration.Default),
                Is.EqualTo(76));
            Assert.That(
                LevelStarCoinRewardCalculator.Calculate(
                    1,
                    1,
                    LevelStarCoinRewardConfiguration.Default),
                Is.EqualTo(1));
        }

        [Test]
        public void StarCoinReward_RejectsInvalidInputsAndDecreasingTiers()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LevelStarCoinRewardCalculator.Calculate(
                    -1,
                    1,
                    LevelStarCoinRewardConfiguration.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                LevelStarCoinRewardCalculator.Calculate(
                    100,
                    4,
                    LevelStarCoinRewardConfiguration.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new LevelStarCoinRewardConfiguration(-1, 75, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new LevelStarCoinRewardConfiguration(50, 75, 101));
            Assert.Throws<ArgumentException>(() =>
                new LevelStarCoinRewardConfiguration(75, 50, 100));
        }
    }
}
