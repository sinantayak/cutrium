using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Economy;

namespace Cutrium.Unity.Services
{
    public enum LevelCoinRewardClaimStatus
    {
        Awarded,
        AlreadyClaimed,
        InvalidReward,
        WalletRejected,
    }

    public readonly struct LevelCoinRewardClaimResult
    {
        public LevelCoinRewardClaimResult(
            LevelCoinRewardClaimStatus status,
            int rewardAmount,
            int balance,
            CoinTransactionResult transaction)
        {
            Status = status;
            RewardAmount = rewardAmount;
            Balance = balance;
            Transaction = transaction;
        }

        public LevelCoinRewardClaimStatus Status { get; }
        public int RewardAmount { get; }
        public int Balance { get; }
        public CoinTransactionResult Transaction { get; }
        public bool Awarded => Status == LevelCoinRewardClaimStatus.Awarded;
    }

    /// Enforces once-per-loaded-level-run reward credit while delegating all
    /// balance rules and persistence to Task 01's central Coin wallet.
    public sealed class LevelCoinRewardService
    {
        public const string MutationReason = "level_completion";

        private readonly object _claimLock = new object();
        private readonly HashSet<string> _claimedRunIds =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly CoinWalletService _coins;

        public LevelCoinRewardService(CoinWalletService coins)
        {
            _coins = coins ?? throw new ArgumentNullException(nameof(coins));
        }

        public LevelCoinRewardClaimResult Claim(
            string levelRunId,
            string levelId,
            int rewardAmount)
        {
            if (string.IsNullOrWhiteSpace(levelRunId))
            {
                throw new ArgumentException(
                    "A level reward requires a non-empty run ID.",
                    nameof(levelRunId));
            }

            if (rewardAmount <= 0)
            {
                return new LevelCoinRewardClaimResult(
                    LevelCoinRewardClaimStatus.InvalidReward,
                    rewardAmount,
                    _coins.Balance,
                    default);
            }

            string normalizedRunId = levelRunId.Trim();
            lock (_claimLock)
            {
                // Reserve before mutating the observable wallet. A balance
                // listener that re-enters this API for the same completion
                // therefore observes AlreadyClaimed instead of paying twice.
                if (!_claimedRunIds.Add(normalizedRunId))
                {
                    return new LevelCoinRewardClaimResult(
                        LevelCoinRewardClaimStatus.AlreadyClaimed,
                        rewardAmount,
                        _coins.Balance,
                        default);
                }

                string sourceId = string.IsNullOrWhiteSpace(levelId)
                    ? normalizedRunId
                    : levelId.Trim() + ":" + normalizedRunId;
                CoinTransactionResult transaction = _coins.AddCoins(
                    rewardAmount,
                    MutationReason,
                    sourceId);
                if (!transaction.Succeeded)
                {
                    _claimedRunIds.Remove(normalizedRunId);
                    return new LevelCoinRewardClaimResult(
                        LevelCoinRewardClaimStatus.WalletRejected,
                        rewardAmount,
                        transaction.CurrentBalance,
                        transaction);
                }

                return new LevelCoinRewardClaimResult(
                    LevelCoinRewardClaimStatus.Awarded,
                    rewardAmount,
                    transaction.CurrentBalance,
                    transaction);
            }
        }
    }
}
