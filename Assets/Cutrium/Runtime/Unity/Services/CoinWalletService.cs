using System;
using System.Threading.Tasks;
using Cutrium.Gameplay.Economy;

namespace Cutrium.Unity.Services
{
    /// Application-level Coin API. It owns the single wallet used by the
    /// current game session and bridges successful domain mutations to the
    /// existing local/Cloud Save store.
    public sealed class CoinWalletService : IDisposable
    {
        public const int DefaultStartingBalance = 0;

        private readonly object _coordinationLock = new object();
        private readonly ICoinBalanceStore _progressStore;
        private readonly CoinWallet _wallet;
        private bool _hasLocalCoinSave;
        private bool _isRestoringFromCloud;
        private bool _disposed;
        private Task _cloudSyncTask;

        public CoinWalletService(
            ICoinBalanceStore progressStore,
            int startingBalance = DefaultStartingBalance)
        {
            _progressStore = progressStore
                ?? throw new ArgumentNullException(nameof(progressStore));
            if (startingBalance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingBalance),
                    "A Coin balance cannot be negative.");
            }

            _hasLocalCoinSave = _progressStore.TryLoadLocalCoinBalance(
                out int localBalance);
            _wallet = new CoinWallet(
                _hasLocalCoinSave ? localBalance : startingBalance);
            _wallet.BalanceChanged += OnWalletBalanceChanged;
        }

        public event Action<CoinBalanceChangedEvent> BalanceChanged;

        public int Balance => _wallet.Balance;

        public bool CanAfford(int amount) => _wallet.CanAfford(amount);

        public CoinTransactionResult AddCoins(
            int amount,
            string reason,
            string sourceId = null)
        {
            lock (_coordinationLock)
            {
                ThrowIfDisposed();
                return _wallet.AddCoins(amount, reason, sourceId);
            }
        }

        public CoinTransactionResult AddCoins(
            int amount,
            CoinMutationContext context)
        {
            lock (_coordinationLock)
            {
                ThrowIfDisposed();
                return _wallet.AddCoins(amount, context);
            }
        }

        public CoinTransactionResult TrySpendCoins(
            int amount,
            string reason,
            string sourceId = null)
        {
            lock (_coordinationLock)
            {
                ThrowIfDisposed();
                return _wallet.TrySpendCoins(amount, reason, sourceId);
            }
        }

        public CoinTransactionResult TrySpendCoins(
            int amount,
            CoinMutationContext context)
        {
            lock (_coordinationLock)
            {
                ThrowIfDisposed();
                return _wallet.TrySpendCoins(amount, context);
            }
        }

        /// Reconciles once authentication is available. Existing local data
        /// wins and is pushed; only a device with no local Coin key imports
        /// cloud data. A mutation that happens while the pull is in flight
        /// also wins, preventing a late cloud response from erasing it.
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
                _wallet.BalanceChanged -= OnWalletBalanceChanged;
                BalanceChanged = null;
            }
        }

        private async Task SynchronizeWithCloudCoreAsync()
        {
            bool hasLocalSave;
            int localBalance;
            lock (_coordinationLock)
            {
                hasLocalSave = _hasLocalCoinSave;
                localBalance = _wallet.Balance;
            }

            if (hasLocalSave)
            {
                await _progressStore.PushCoinBalanceToCloudAsync(
                    localBalance);
                return;
            }

            int? cloudBalance = await _progressStore
                .TryLoadCloudCoinBalanceAsync();
            bool localMutationWon;
            int balanceToPersist;
            bool importedCloudBalance = false;

            lock (_coordinationLock)
            {
                if (_disposed)
                {
                    return;
                }

                localMutationWon = _hasLocalCoinSave;
                if (!localMutationWon && cloudBalance.HasValue)
                {
                    _isRestoringFromCloud = true;
                    try
                    {
                        _wallet.RestoreBalance(
                            cloudBalance.Value,
                            new CoinMutationContext(
                                "cloud_restore",
                                "unity-cloud-save"));
                    }
                    finally
                    {
                        _isRestoringFromCloud = false;
                    }

                    _hasLocalCoinSave = true;
                    importedCloudBalance = true;
                }

                balanceToPersist = _wallet.Balance;
            }

            if (localMutationWon)
            {
                await _progressStore.PushCoinBalanceToCloudAsync(
                    balanceToPersist);
            }
            else if (importedCloudBalance)
            {
                // Also writes a local key when the imported value equals the
                // zero starting balance and therefore emitted no event.
                _progressStore.SaveCoinBalance(balanceToPersist);
            }
        }

        private void OnWalletBalanceChanged(CoinBalanceChangedEvent change)
        {
            if (!_isRestoringFromCloud)
            {
                _hasLocalCoinSave = true;
                _progressStore.SaveCoinBalance(change.CurrentBalance);
            }

            BalanceChanged?.Invoke(change);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CoinWalletService));
            }
        }
    }
}
