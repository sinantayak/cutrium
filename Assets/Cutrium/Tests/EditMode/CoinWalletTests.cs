using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Economy;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class CoinWalletTests
    {
        [Test]
        public void Constructor_RejectsNegativeBalance()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CoinWallet(-1));
        }

        [Test]
        public void AddCoins_UpdatesBalanceAndCarriesMutationContext()
        {
            var wallet = new CoinWallet();
            CoinBalanceChangedEvent observed = default;
            int eventCount = 0;
            wallet.BalanceChanged += change =>
            {
                observed = change;
                eventCount++;
            };

            CoinTransactionResult result = wallet.AddCoins(
                125,
                "test_reward",
                "level-01");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Failure, Is.EqualTo(CoinTransactionFailure.None));
            Assert.That(wallet.Balance, Is.EqualTo(125));
            Assert.That(eventCount, Is.EqualTo(1));
            Assert.That(observed.PreviousBalance, Is.Zero);
            Assert.That(observed.CurrentBalance, Is.EqualTo(125));
            Assert.That(observed.Delta, Is.EqualTo(125));
            Assert.That(observed.MutationKind, Is.EqualTo(CoinMutationKind.Added));
            Assert.That(observed.Context.Reason, Is.EqualTo("test_reward"));
            Assert.That(observed.Context.SourceId, Is.EqualTo("level-01"));
        }

        [Test]
        public void MultipleMutationsInSameCallStack_AreAllAppliedInOrder()
        {
            var wallet = new CoinWallet(10);
            var balances = new List<int>();
            wallet.BalanceChanged += change =>
                balances.Add(change.CurrentBalance);

            CoinTransactionResult first = wallet.AddCoins(20, "first");
            CoinTransactionResult second = wallet.AddCoins(30, "second");
            CoinTransactionResult third = wallet.TrySpendCoins(15, "third");

            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.True);
            Assert.That(third.Succeeded, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(45));
            Assert.That(balances, Is.EqualTo(new[] { 30, 60, 45 }));
        }

        [Test]
        public void SpendExactlyCurrentBalance_SucceedsAtZero()
        {
            var wallet = new CoinWallet(250);

            CoinTransactionResult result = wallet.TrySpendCoins(
                250,
                "test_purchase",
                "offer-01");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Delta, Is.EqualTo(-250));
            Assert.That(wallet.Balance, Is.Zero);
            Assert.That(wallet.CanAfford(0), Is.True);
            Assert.That(wallet.CanAfford(1), Is.False);
        }

        [Test]
        public void SpendAboveCurrentBalance_FailsWithoutChangeEvent()
        {
            var wallet = new CoinWallet(99);
            int eventCount = 0;
            wallet.BalanceChanged += _ => eventCount++;

            CoinTransactionResult result = wallet.TrySpendCoins(
                100,
                "test_purchase");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(CoinTransactionFailure.InsufficientBalance));
            Assert.That(result.PreviousBalance, Is.EqualTo(99));
            Assert.That(result.CurrentBalance, Is.EqualTo(99));
            Assert.That(wallet.Balance, Is.EqualTo(99));
            Assert.That(eventCount, Is.Zero);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonPositiveMutationAmounts_AreRejected(int amount)
        {
            var wallet = new CoinWallet(100);
            int eventCount = 0;
            wallet.BalanceChanged += _ => eventCount++;

            CoinTransactionResult add = wallet.AddCoins(amount, "invalid");
            CoinTransactionResult spend = wallet.TrySpendCoins(
                amount,
                "invalid");

            Assert.That(add.Succeeded, Is.False);
            Assert.That(
                add.Failure,
                Is.EqualTo(CoinTransactionFailure.InvalidAmount));
            Assert.That(spend.Succeeded, Is.False);
            Assert.That(
                spend.Failure,
                Is.EqualTo(CoinTransactionFailure.InvalidAmount));
            Assert.That(wallet.Balance, Is.EqualTo(100));
            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void AddThatWouldOverflow_FailsWithoutChangingBalance()
        {
            var wallet = new CoinWallet(int.MaxValue - 5);

            CoinTransactionResult result = wallet.AddCoins(6, "overflow");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.Failure,
                Is.EqualTo(CoinTransactionFailure.BalanceOverflow));
            Assert.That(wallet.Balance, Is.EqualTo(int.MaxValue - 5));
        }

        [Test]
        public void RestoreBalance_RefreshesListenersAndRejectsNegativeData()
        {
            var wallet = new CoinWallet();
            CoinBalanceChangedEvent observed = default;
            int eventCount = 0;
            wallet.BalanceChanged += change =>
            {
                observed = change;
                eventCount++;
            };

            CoinTransactionResult restored = wallet.RestoreBalance(
                480,
                new CoinMutationContext("cloud_restore", "cloud-save"));
            CoinTransactionResult invalid = wallet.RestoreBalance(
                -1,
                new CoinMutationContext("corrupt_save"));

            Assert.That(restored.Succeeded, Is.True);
            Assert.That(observed.MutationKind, Is.EqualTo(CoinMutationKind.Restored));
            Assert.That(observed.CurrentBalance, Is.EqualTo(480));
            Assert.That(observed.Context.Reason, Is.EqualTo("cloud_restore"));
            Assert.That(invalid.Succeeded, Is.False);
            Assert.That(
                invalid.Failure,
                Is.EqualTo(CoinTransactionFailure.InvalidRestoredBalance));
            Assert.That(wallet.Balance, Is.EqualTo(480));
            Assert.That(eventCount, Is.EqualTo(1));
        }

        [Test]
        public void EmptyContext_UsesStableReasonForDiagnostics()
        {
            var wallet = new CoinWallet();

            CoinTransactionResult result = wallet.AddCoins(
                1,
                default(CoinMutationContext));

            Assert.That(result.Context.Reason, Is.EqualTo("unspecified"));
            Assert.That(result.Context.SourceId, Is.Empty);
        }
    }
}
