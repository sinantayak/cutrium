using System;
using System.Collections.Generic;

namespace Cutrium.Gameplay.Economy
{
    /// One kind of skillful-play bonus a level completion can earn. Every
    /// kind maps to a signal the simulation already tracks -- see
    /// PerformanceCoinRewardCalculator for exactly which one.
    public enum PerformanceCoinRewardKind
    {
        NearMiss = 0,
        PerfectCut = 1,
        NoLifeLost = 2,
        NoPowerUpUsed = 3,
    }

    /// Configurable Coin amount per bonus kind. NearMiss/PerfectCut scale
    /// with how many times they happened during the run; NoLifeLost/
    /// NoPowerUpUsed are pass/fail for the whole completion.
    public readonly struct PerformanceCoinRewardConfiguration
    {
        public PerformanceCoinRewardConfiguration(
            int nearMissCoinsPerOccurrence,
            int perfectCutCoinsPerOccurrence,
            int noLifeLostCoins,
            int noPowerUpUsedCoins)
        {
            NearMissCoinsPerOccurrence = ValidateNonNegative(
                nearMissCoinsPerOccurrence,
                nameof(nearMissCoinsPerOccurrence));
            PerfectCutCoinsPerOccurrence = ValidateNonNegative(
                perfectCutCoinsPerOccurrence,
                nameof(perfectCutCoinsPerOccurrence));
            NoLifeLostCoins = ValidateNonNegative(
                noLifeLostCoins,
                nameof(noLifeLostCoins));
            NoPowerUpUsedCoins = ValidateNonNegative(
                noPowerUpUsedCoins,
                nameof(noPowerUpUsedCoins));
        }

        public int NearMissCoinsPerOccurrence { get; }

        public int PerfectCutCoinsPerOccurrence { get; }

        public int NoLifeLostCoins { get; }

        public int NoPowerUpUsedCoins { get; }

        public static PerformanceCoinRewardConfiguration Default { get; } =
            new PerformanceCoinRewardConfiguration(10, 20, 30, 30);

        private static int ValidateNonNegative(int value, string name) =>
            value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(name);
    }

    /// One displayed/credited breakdown line, e.g. "NEAR MISS x2  +20".
    public readonly struct PerformanceCoinRewardLine
    {
        public PerformanceCoinRewardLine(
            PerformanceCoinRewardKind kind,
            int occurrenceCount,
            int coinAmount)
        {
            if (occurrenceCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(occurrenceCount));
            }

            if (coinAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(coinAmount));
            }

            Kind = kind;
            OccurrenceCount = occurrenceCount;
            CoinAmount = coinAmount;
        }

        public PerformanceCoinRewardKind Kind { get; }

        public int OccurrenceCount { get; }

        public int CoinAmount { get; }
    }

    /// The full set of bonus lines earned by one level-completion run, plus
    /// their combined total. An empty breakdown (Lines.Count == 0,
    /// TotalCoinAmount == 0) means no bonus was earned -- not an error.
    public readonly struct PerformanceCoinRewardBreakdown
    {
        private readonly IReadOnlyList<PerformanceCoinRewardLine> _lines;

        public PerformanceCoinRewardBreakdown(
            IReadOnlyList<PerformanceCoinRewardLine> lines,
            int totalCoinAmount)
        {
            if (totalCoinAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCoinAmount));
            }

            _lines = lines ?? Array.Empty<PerformanceCoinRewardLine>();
            TotalCoinAmount = totalCoinAmount;
        }

        public IReadOnlyList<PerformanceCoinRewardLine> Lines =>
            _lines ?? Array.Empty<PerformanceCoinRewardLine>();

        public int TotalCoinAmount { get; }

        public static PerformanceCoinRewardBreakdown Empty { get; } =
            new PerformanceCoinRewardBreakdown(
                Array.Empty<PerformanceCoinRewardLine>(),
                0);
    }

    /// Turns this run's already-tracked performance signals (near misses,
    /// large "perfect" captures, whether any cut broke, whether any power
    /// was used) into a Coin bonus breakdown. Every input here is a signal
    /// the deterministic simulation already records -- see
    /// CoreFunMetricsTracker -- so this never fabricates a statistic the
    /// gameplay itself doesn't produce.
    public static class PerformanceCoinRewardCalculator
    {
        public static PerformanceCoinRewardBreakdown Calculate(
            int nearMissCount,
            int perfectCutCount,
            bool noLifeLost,
            bool powerUpEligible,
            bool anyPowerUpUsed,
            PerformanceCoinRewardConfiguration configuration)
        {
            if (nearMissCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nearMissCount));
            }

            if (perfectCutCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(perfectCutCount));
            }

            var lines = new List<PerformanceCoinRewardLine>(4);
            int total = 0;

            AppendIfEarned(
                lines,
                ref total,
                PerformanceCoinRewardKind.NearMiss,
                nearMissCount,
                configuration.NearMissCoinsPerOccurrence);
            AppendIfEarned(
                lines,
                ref total,
                PerformanceCoinRewardKind.PerfectCut,
                perfectCutCount,
                configuration.PerfectCutCoinsPerOccurrence);
            AppendIfEarned(
                lines,
                ref total,
                PerformanceCoinRewardKind.NoLifeLost,
                noLifeLost ? 1 : 0,
                configuration.NoLifeLostCoins);
            // Only a real bonus when the level actually offered a power to
            // withhold -- a level with none configured would otherwise
            // always trivially "pass" this, rewarding nothing meaningful.
            AppendIfEarned(
                lines,
                ref total,
                PerformanceCoinRewardKind.NoPowerUpUsed,
                powerUpEligible && !anyPowerUpUsed ? 1 : 0,
                configuration.NoPowerUpUsedCoins);

            return new PerformanceCoinRewardBreakdown(lines, total);
        }

        private static void AppendIfEarned(
            List<PerformanceCoinRewardLine> lines,
            ref int total,
            PerformanceCoinRewardKind kind,
            int occurrenceCount,
            int coinsPerOccurrence)
        {
            if (occurrenceCount <= 0 || coinsPerOccurrence <= 0)
            {
                return;
            }

            int amount = occurrenceCount * coinsPerOccurrence;
            lines.Add(new PerformanceCoinRewardLine(
                kind,
                occurrenceCount,
                amount));
            total += amount;
        }
    }
}
