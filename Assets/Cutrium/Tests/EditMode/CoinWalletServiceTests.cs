using System.Threading.Tasks;
using Cutrium.Gameplay.Economy;
using Cutrium.Unity.Services;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class CoinWalletServiceTests
    {
        [Test]
        public void SuccessfulMutationsPersistAndSurviveServiceRecreation()
        {
            var store = new FakeCoinBalanceStore();
            var firstSession = new CoinWalletService(store);

            CoinTransactionResult added = firstSession.AddCoins(
                90,
                "test_reward");
            CoinTransactionResult spent = firstSession.TrySpendCoins(
                35,
                "test_purchase");
            firstSession.Dispose();

            var secondSession = new CoinWalletService(store);
            try
            {
                Assert.That(added.Succeeded, Is.True);
                Assert.That(spent.Succeeded, Is.True);
                Assert.That(store.SaveCount, Is.EqualTo(2));
                Assert.That(secondSession.Balance, Is.EqualTo(55));
            }
            finally
            {
                secondSession.Dispose();
            }
        }

        [Test]
        public void FailedSpendDoesNotPersistOrChangeBalance()
        {
            var store = new FakeCoinBalanceStore
            {
                HasLocalBalance = true,
                LocalBalance = 25,
            };
            var service = new CoinWalletService(store);
            try
            {
                CoinTransactionResult result = service.TrySpendCoins(
                    26,
                    "too_expensive");

                Assert.That(result.Succeeded, Is.False);
                Assert.That(service.Balance, Is.EqualTo(25));
                Assert.That(store.SaveCount, Is.Zero);
            }
            finally
            {
                service.Dispose();
            }
        }

        [Test]
        public async Task FreshDeviceImportsCloudBalanceAndNotifiesUi()
        {
            var store = new FakeCoinBalanceStore
            {
                CloudBalance = 325,
            };
            var service = new CoinWalletService(store);
            CoinBalanceChangedEvent observed = default;
            int eventCount = 0;
            service.BalanceChanged += change =>
            {
                observed = change;
                eventCount++;
            };

            try
            {
                await service.SynchronizeWithCloudAsync();

                Assert.That(service.Balance, Is.EqualTo(325));
                Assert.That(store.HasLocalBalance, Is.True);
                Assert.That(store.LocalBalance, Is.EqualTo(325));
                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(
                    observed.MutationKind,
                    Is.EqualTo(CoinMutationKind.Restored));
                Assert.That(
                    observed.Context.Reason,
                    Is.EqualTo("cloud_restore"));
            }
            finally
            {
                service.Dispose();
            }
        }

        [Test]
        public async Task ExistingLocalBalancePushesInsteadOfUsingCloudMaximum()
        {
            var store = new FakeCoinBalanceStore
            {
                HasLocalBalance = true,
                LocalBalance = 40,
                CloudBalance = 500,
            };
            var service = new CoinWalletService(store);
            try
            {
                await service.SynchronizeWithCloudAsync();

                Assert.That(service.Balance, Is.EqualTo(40));
                Assert.That(store.CloudLoadCount, Is.Zero);
                Assert.That(store.LastCloudPush, Is.EqualTo(40));
            }
            finally
            {
                service.Dispose();
            }
        }

        [Test]
        public async Task LocalMutationDuringCloudPullWinsTheRace()
        {
            var store = new FakeCoinBalanceStore();
            store.DeferCloudLoad();
            var service = new CoinWalletService(store);

            try
            {
                Task syncTask = service.SynchronizeWithCloudAsync();
                service.AddCoins(70, "offline_reward");
                store.CompleteCloudLoad(900);
                await syncTask;

                Assert.That(service.Balance, Is.EqualTo(70));
                Assert.That(store.LocalBalance, Is.EqualTo(70));
                Assert.That(store.LastCloudPush, Is.EqualTo(70));
            }
            finally
            {
                service.Dispose();
            }
        }

        private sealed class FakeCoinBalanceStore : ICoinBalanceStore
        {
            private TaskCompletionSource<int?> _deferredCloudLoad;

            public bool HasLocalBalance { get; set; }
            public int LocalBalance { get; set; }
            public int? CloudBalance { get; set; }
            public int? LastCloudPush { get; private set; }
            public int SaveCount { get; private set; }
            public int CloudLoadCount { get; private set; }

            public bool TryLoadLocalCoinBalance(out int balance)
            {
                balance = LocalBalance;
                return HasLocalBalance;
            }

            public void SaveCoinBalance(int balance)
            {
                HasLocalBalance = true;
                LocalBalance = balance;
                LastCloudPush = balance;
                SaveCount++;
            }

            public Task PushCoinBalanceToCloudAsync(int balance)
            {
                LastCloudPush = balance;
                return Task.CompletedTask;
            }

            public Task<int?> TryLoadCloudCoinBalanceAsync()
            {
                CloudLoadCount++;
                return _deferredCloudLoad != null
                    ? _deferredCloudLoad.Task
                    : Task.FromResult(CloudBalance);
            }

            public void DeferCloudLoad()
            {
                _deferredCloudLoad = new TaskCompletionSource<int?>();
            }

            public void CompleteCloudLoad(int? balance)
            {
                CloudBalance = balance;
                _deferredCloudLoad.SetResult(balance);
            }
        }
    }
}
