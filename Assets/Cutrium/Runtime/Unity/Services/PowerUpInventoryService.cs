using System;
using System.Threading.Tasks;
using Cutrium.Gameplay.Economy;

namespace Cutrium.Unity.Services
{
    /// Application-level inventory API. It keeps local persistence
    /// synchronous and imports Cloud state only on a genuinely fresh device.
    public sealed class PowerUpInventoryService : IDisposable
    {
        private readonly object _coordinationLock = new object();
        private readonly IPowerUpInventoryStore _progressStore;
        private readonly PowerUpInventory _inventory;
        private bool _hasLocalSave;
        private bool _isRestoringFromCloud;
        private bool _disposed;
        private Task _cloudSyncTask;

        public PowerUpInventoryService(IPowerUpInventoryStore progressStore)
        {
            _progressStore = progressStore
                ?? throw new ArgumentNullException(nameof(progressStore));
            _hasLocalSave = _progressStore.TryLoadLocalPowerUpInventory(
                out PowerUpInventorySnapshot localInventory);
            _inventory = new PowerUpInventory(
                _hasLocalSave ? localInventory : default);
            _inventory.InventoryChanged += OnInventoryChanged;
        }

        public event Action<PowerUpInventoryChangedEvent> InventoryChanged;

        public PowerUpInventorySnapshot Snapshot => _inventory.Snapshot;

        public int GetCount(PowerUpKind kind) => _inventory.GetCount(kind);

        public bool CanAdd(PowerUpKind kind, int amount) =>
            _inventory.CanAdd(kind, amount);

        public PowerUpInventoryTransactionResult Add(
            PowerUpKind kind,
            int amount,
            string reason,
            string sourceId = null)
        {
            lock (_coordinationLock)
            {
                ThrowIfDisposed();
                return _inventory.TryAdd(
                    kind,
                    amount,
                    new PowerUpInventoryMutationContext(reason, sourceId));
            }
        }

        public PowerUpInventoryTransactionResult TryConsume(
            PowerUpKind kind,
            int amount,
            string reason,
            string sourceId = null)
        {
            lock (_coordinationLock)
            {
                ThrowIfDisposed();
                return _inventory.TryConsume(
                    kind,
                    amount,
                    new PowerUpInventoryMutationContext(reason, sourceId));
            }
        }

        public Task SynchronizeWithCloudAsync()
        {
            lock (_coordinationLock)
            {
                ThrowIfDisposed();
                if (_cloudSyncTask != null && !_cloudSyncTask.IsCompleted)
                {
                    return _cloudSyncTask;
                }

                _cloudSyncTask = SynchronizeWithCloudCoreAsync();
                return _cloudSyncTask;
            }
        }

        public void Dispose()
        {
            lock (_coordinationLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _inventory.InventoryChanged -= OnInventoryChanged;
                InventoryChanged = null;
            }
        }

        private async Task SynchronizeWithCloudCoreAsync()
        {
            bool hasLocalSave;
            PowerUpInventorySnapshot localInventory;
            lock (_coordinationLock)
            {
                hasLocalSave = _hasLocalSave;
                localInventory = _inventory.Snapshot;
            }

            if (hasLocalSave)
            {
                await _progressStore.PushPowerUpInventoryToCloudAsync(
                    localInventory);
                return;
            }

            PowerUpInventorySnapshot? cloudInventory = await _progressStore
                .TryLoadCloudPowerUpInventoryAsync();
            bool localMutationWon;
            bool importedCloudInventory = false;
            PowerUpInventorySnapshot inventoryToPersist;
            lock (_coordinationLock)
            {
                if (_disposed)
                {
                    return;
                }

                localMutationWon = _hasLocalSave;
                if (!localMutationWon && cloudInventory.HasValue)
                {
                    _isRestoringFromCloud = true;
                    try
                    {
                        _inventory.Restore(
                            cloudInventory.Value,
                            new PowerUpInventoryMutationContext(
                                "cloud_restore",
                                "unity-cloud-save"));
                    }
                    finally
                    {
                        _isRestoringFromCloud = false;
                    }

                    _hasLocalSave = true;
                    importedCloudInventory = true;
                }

                inventoryToPersist = _inventory.Snapshot;
            }

            if (localMutationWon)
            {
                await _progressStore.PushPowerUpInventoryToCloudAsync(
                    inventoryToPersist);
            }
            else if (importedCloudInventory)
            {
                // Creates the local presence marker even for an all-zero
                // snapshot, which may produce no inventory change events.
                _progressStore.SavePowerUpInventory(inventoryToPersist);
            }
        }

        private void OnInventoryChanged(PowerUpInventoryChangedEvent change)
        {
            if (!_isRestoringFromCloud)
            {
                _hasLocalSave = true;
                _progressStore.SavePowerUpInventory(_inventory.Snapshot);
            }

            InventoryChanged?.Invoke(change);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(PowerUpInventoryService));
            }
        }
    }
}
