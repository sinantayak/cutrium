using System.Threading.Tasks;
using Cutrium.Gameplay.Economy;
using Cutrium.Unity.Services;
using NUnit.Framework;

namespace Cutrium.Gameplay.EditModeTests
{
    public sealed class PowerUpInventoryEconomyTests
    {
        [Test]
        public void InventoryAddsConsumesAndRejectsUnderflowWithoutMutation()
        {
            var inventory = new PowerUpInventory();
            int eventCount = 0;
            inventory.InventoryChanged += change => eventCount++;

            PowerUpInventoryTransactionResult added = inventory.TryAdd(
                PowerUpKind.FreezePulse,
                2,
                new PowerUpInventoryMutationContext("test_add"));
            PowerUpInventoryTransactionResult consumed = inventory.TryConsume(
                PowerUpKind.FreezePulse,
                1,
                new PowerUpInventoryMutationContext("test_consume"));
            PowerUpInventoryTransactionResult rejected = inventory.TryConsume(
                PowerUpKind.FreezePulse,
                2,
                new PowerUpInventoryMutationContext("test_underflow"));

            Assert.That(added.Succeeded, Is.True);
            Assert.That(consumed.Succeeded, Is.True);
            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(
                rejected.Failure,
                Is.EqualTo(PowerUpInventoryFailure.InsufficientQuantity));
            Assert.That(
                inventory.GetCount(PowerUpKind.FreezePulse),
                Is.EqualTo(1));
            Assert.That(eventCount, Is.EqualTo(2));
        }

        [Test]
        public void SuccessfulInventoryMutationsPersistAcrossServiceRecreation()
        {
            var store = new MemoryEconomyStore();
            using (var first = new PowerUpInventoryService(store))
            {
                first.Add(PowerUpKind.FreezePulse, 3, "test_add");
                first.Add(PowerUpKind.GravityWell, 2, "test_add");
                first.TryConsume(
                    PowerUpKind.FreezePulse,
                    1,
                    "test_consume");
            }

            using var restored = new PowerUpInventoryService(store);
            Assert.That(store.InventorySaveCount, Is.EqualTo(3));
            Assert.That(restored.GetCount(PowerUpKind.FreezePulse), Is.EqualTo(2));
            Assert.That(restored.GetCount(PowerUpKind.InstantBarrier), Is.Zero);
            Assert.That(restored.GetCount(PowerUpKind.GravityWell), Is.EqualTo(2));
        }

        [Test]
        public async Task FreshDeviceImportsCloudInventory()
        {
            var store = new MemoryEconomyStore
            {
                CloudInventory = new PowerUpInventorySnapshot(4, 5, 6),
            };
            using var service = new PowerUpInventoryService(store);

            await service.SynchronizeWithCloudAsync();

            Assert.That(
                service.Snapshot,
                Is.EqualTo(new PowerUpInventorySnapshot(4, 5, 6)));
            Assert.That(store.HasLocalInventory, Is.True);
            Assert.That(store.InventorySaveCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ExistingLocalInventoryWinsOverCloudInventory()
        {
            var store = new MemoryEconomyStore
            {
                HasLocalInventory = true,
                LocalInventory = new PowerUpInventorySnapshot(1, 2, 3),
                CloudInventory = new PowerUpInventorySnapshot(9, 9, 9),
            };
            using var service = new PowerUpInventoryService(store);

            await service.SynchronizeWithCloudAsync();

            Assert.That(
                service.Snapshot,
                Is.EqualTo(new PowerUpInventorySnapshot(1, 2, 3)));
            Assert.That(store.InventoryCloudLoadCount, Is.Zero);
            Assert.That(
                store.LastInventoryCloudPush,
                Is.EqualTo(new PowerUpInventorySnapshot(1, 2, 3)));
        }

        [Test]
        public async Task LocalMutationDuringCloudPullWinsTheRace()
        {
            var store = new MemoryEconomyStore();
            store.DeferInventoryCloudLoad();
            using var service = new PowerUpInventoryService(store);

            Task sync = service.SynchronizeWithCloudAsync();
            service.Add(PowerUpKind.InstantBarrier, 2, "offline_purchase");
            store.CompleteInventoryCloudLoad(
                new PowerUpInventorySnapshot(8, 8, 8));
            await sync;

            Assert.That(
                service.Snapshot,
                Is.EqualTo(new PowerUpInventorySnapshot(0, 2, 0)));
            Assert.That(
                store.LastInventoryCloudPush,
                Is.EqualTo(new PowerUpInventorySnapshot(0, 2, 0)));
        }

        [TestCase(PowerUpKind.FreezePulse, 200)]
        [TestCase(PowerUpKind.InstantBarrier, 250)]
        [TestCase(PowerUpKind.GravityWell, 250)]
        public void PurchaseSpendsCoinsAndAddsConfiguredPowerUp(
            PowerUpKind kind,
            int price)
        {
            var store = new MemoryEconomyStore
            {
                HasLocalCoins = true,
                LocalCoins = 500,
            };
            using var coins = new CoinWalletService(store);
            using var inventory = new PowerUpInventoryService(store);
            var purchases = new PowerUpPurchaseService(coins, inventory);

            PowerUpPurchaseResult result = purchases.TryPurchase(
                kind,
                1,
                price);

            Assert.That(result.Purchased, Is.True);
            Assert.That(coins.Balance, Is.EqualTo(500 - price));
            Assert.That(inventory.GetCount(kind), Is.EqualTo(1));
            Assert.That(store.CoinSaveCount, Is.EqualTo(1));
            Assert.That(store.InventorySaveCount, Is.EqualTo(1));
        }

        [Test]
        public void InsufficientCoinsLeavesWalletAndInventoryUnchanged()
        {
            var store = new MemoryEconomyStore
            {
                HasLocalCoins = true,
                LocalCoins = 199,
            };
            using var coins = new CoinWalletService(store);
            using var inventory = new PowerUpInventoryService(store);
            var purchases = new PowerUpPurchaseService(coins, inventory);

            PowerUpPurchaseResult result = purchases.TryPurchase(
                PowerUpKind.FreezePulse,
                1,
                200);

            Assert.That(
                result.Status,
                Is.EqualTo(PowerUpPurchaseStatus.InsufficientCoins));
            Assert.That(coins.Balance, Is.EqualTo(199));
            Assert.That(inventory.GetCount(PowerUpKind.FreezePulse), Is.Zero);
            Assert.That(store.CoinSaveCount, Is.Zero);
            Assert.That(store.InventorySaveCount, Is.Zero);
        }

        private sealed class MemoryEconomyStore :
            ICoinBalanceStore,
            IPowerUpInventoryStore
        {
            private TaskCompletionSource<PowerUpInventorySnapshot?>
                _deferredInventoryCloudLoad;

            public bool HasLocalCoins { get; set; }
            public int LocalCoins { get; set; }
            public int CoinSaveCount { get; private set; }
            public bool HasLocalInventory { get; set; }
            public PowerUpInventorySnapshot LocalInventory { get; set; }
            public PowerUpInventorySnapshot? CloudInventory { get; set; }
            public PowerUpInventorySnapshot? LastInventoryCloudPush
            {
                get;
                private set;
            }
            public int InventorySaveCount { get; private set; }
            public int InventoryCloudLoadCount { get; private set; }

            public bool TryLoadLocalCoinBalance(out int balance)
            {
                balance = LocalCoins;
                return HasLocalCoins;
            }

            public void SaveCoinBalance(int balance)
            {
                HasLocalCoins = true;
                LocalCoins = balance;
                CoinSaveCount++;
            }

            public Task PushCoinBalanceToCloudAsync(int balance) =>
                Task.CompletedTask;

            public Task<int?> TryLoadCloudCoinBalanceAsync() =>
                Task.FromResult<int?>(null);

            public bool TryLoadLocalPowerUpInventory(
                out PowerUpInventorySnapshot inventory)
            {
                inventory = LocalInventory;
                return HasLocalInventory;
            }

            public void SavePowerUpInventory(
                PowerUpInventorySnapshot inventory)
            {
                HasLocalInventory = true;
                LocalInventory = inventory;
                InventorySaveCount++;
            }

            public Task PushPowerUpInventoryToCloudAsync(
                PowerUpInventorySnapshot inventory)
            {
                LastInventoryCloudPush = inventory;
                return Task.CompletedTask;
            }

            public Task<PowerUpInventorySnapshot?>
                TryLoadCloudPowerUpInventoryAsync()
            {
                InventoryCloudLoadCount++;
                return _deferredInventoryCloudLoad != null
                    ? _deferredInventoryCloudLoad.Task
                    : Task.FromResult(CloudInventory);
            }

            public void DeferInventoryCloudLoad()
            {
                _deferredInventoryCloudLoad =
                    new TaskCompletionSource<PowerUpInventorySnapshot?>();
            }

            public void CompleteInventoryCloudLoad(
                PowerUpInventorySnapshot? inventory)
            {
                CloudInventory = inventory;
                _deferredInventoryCloudLoad.SetResult(inventory);
            }
        }
    }
}
