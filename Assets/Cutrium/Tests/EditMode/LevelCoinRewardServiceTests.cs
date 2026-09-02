using System;
using System.Threading.Tasks;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Economy;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Unity.Services;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class LevelCoinRewardServiceTests
    {
        [Test]
        public void LevelConfigurationDefaultsCompletionRewardToOneHundred()
        {
            CoreFunLevelConfiguration configuration = CreateLevel();

            Assert.That(configuration.CompletionCoinReward, Is.EqualTo(100));
        }

        [Test]
        public void LevelConfigurationAcceptsCustomCompletionReward()
        {
            CoreFunLevelConfiguration configuration = CreateLevel(240);

            Assert.That(configuration.CompletionCoinReward, Is.EqualTo(240));
        }

        [Test]
        public void LevelConfigurationRejectsNegativeCompletionReward()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateLevel(-1));
        }

        [Test]
        public void SameLevelRunCreditsExactlyOnceAndPersists()
        {
            var store = new MemoryCoinBalanceStore();
            using var wallet = new CoinWalletService(store);
            var rewards = new LevelCoinRewardService(wallet);

            LevelCoinRewardClaimResult first = rewards.Claim(
                "run-a",
                "level-a",
                100);
            LevelCoinRewardClaimResult duplicate = rewards.Claim(
                "run-a",
                "level-a",
                100);

            Assert.That(first.Status,
                Is.EqualTo(LevelCoinRewardClaimStatus.Awarded));
            Assert.That(first.Transaction.Context.Reason,
                Is.EqualTo(LevelCoinRewardService.MutationReason));
            Assert.That(first.Transaction.Context.SourceId,
                Is.EqualTo("level-a:run-a"));
            Assert.That(duplicate.Status,
                Is.EqualTo(LevelCoinRewardClaimStatus.AlreadyClaimed));
            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(store.SavedBalance, Is.EqualTo(100));
            Assert.That(store.SaveCount, Is.EqualTo(1));

            using var restoredWallet = new CoinWalletService(store);
            Assert.That(restoredWallet.Balance, Is.EqualTo(100));
        }

        [Test]
        public void DistinctLevelRunsCanEachEarnTheirConfiguredReward()
        {
            var store = new MemoryCoinBalanceStore();
            using var wallet = new CoinWalletService(store);
            var rewards = new LevelCoinRewardService(wallet);

            rewards.Claim("run-a", "level-a", 100);
            LevelCoinRewardClaimResult second = rewards.Claim(
                "run-b",
                "level-a",
                175);

            Assert.That(second.Awarded, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(275));
            Assert.That(store.SaveCount, Is.EqualTo(2));
        }

        [Test]
        public void ReentrantListenerCannotCreditSameRunTwice()
        {
            var store = new MemoryCoinBalanceStore();
            using var wallet = new CoinWalletService(store);
            var rewards = new LevelCoinRewardService(wallet);
            LevelCoinRewardClaimResult nestedResult = default;
            wallet.BalanceChanged += _ =>
            {
                nestedResult = rewards.Claim("run-a", "level-a", 100);
            };

            LevelCoinRewardClaimResult result = rewards.Claim(
                "run-a",
                "level-a",
                100);

            Assert.That(result.Awarded, Is.True);
            Assert.That(nestedResult.Status,
                Is.EqualTo(LevelCoinRewardClaimStatus.AlreadyClaimed));
            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void RejectedWalletMutationReleasesRunForValidRetry()
        {
            var store = new MemoryCoinBalanceStore(int.MaxValue);
            using var wallet = new CoinWalletService(store);
            var rewards = new LevelCoinRewardService(wallet);

            LevelCoinRewardClaimResult rejected = rewards.Claim(
                "run-a",
                "level-a",
                1);
            wallet.TrySpendCoins(1, "test_space", "test");
            LevelCoinRewardClaimResult retried = rewards.Claim(
                "run-a",
                "level-a",
                1);

            Assert.That(rejected.Status,
                Is.EqualTo(LevelCoinRewardClaimStatus.WalletRejected));
            Assert.That(rejected.Transaction.Failure,
                Is.EqualTo(CoinTransactionFailure.BalanceOverflow));
            Assert.That(retried.Awarded, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(int.MaxValue));
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void NonPositiveRewardDoesNotMutateWallet(int reward)
        {
            var store = new MemoryCoinBalanceStore();
            using var wallet = new CoinWalletService(store);
            var rewards = new LevelCoinRewardService(wallet);

            LevelCoinRewardClaimResult result = rewards.Claim(
                "run-a",
                "level-a",
                reward);

            Assert.That(result.Status,
                Is.EqualTo(LevelCoinRewardClaimStatus.InvalidReward));
            Assert.That(wallet.Balance, Is.Zero);
            Assert.That(store.SaveCount, Is.Zero);
        }

        private static CoreFunLevelConfiguration CreateLevel(
            int completionCoinReward = 100) =>
            new CoreFunLevelConfiguration(
                "reward-test",
                1,
                new ThreatMotionConfiguration(
                    CoreFunLevelConfiguration.FixedBoardBounds,
                    new LogicalPoint(5f, 8f),
                    new LogicalVector(1f, 0f),
                    2f,
                    0.35f,
                    8),
                new BarrierConfiguration(3f, 0.08f, 1f, 16),
                new CaptureLevelConfiguration(0.8f),
                8,
                string.Empty,
                30f,
                completionCoinReward: completionCoinReward);

        private sealed class MemoryCoinBalanceStore : ICoinBalanceStore
        {
            private bool _hasBalance;

            public MemoryCoinBalanceStore()
            {
            }

            public MemoryCoinBalanceStore(int savedBalance)
            {
                _hasBalance = true;
                SavedBalance = savedBalance;
            }

            public int SavedBalance { get; private set; }
            public int SaveCount { get; private set; }

            public bool TryLoadLocalCoinBalance(out int balance)
            {
                balance = SavedBalance;
                return _hasBalance;
            }

            public void SaveCoinBalance(int balance)
            {
                _hasBalance = true;
                SavedBalance = balance;
                SaveCount++;
            }

            public Task<int?> TryLoadCloudCoinBalanceAsync() =>
                Task.FromResult<int?>(null);

            public Task PushCoinBalanceToCloudAsync(int balance) =>
                Task.CompletedTask;
        }
    }
}
