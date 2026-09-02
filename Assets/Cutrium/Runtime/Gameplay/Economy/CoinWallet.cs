using System;

namespace Cutrium.Gameplay.Economy
{
    public enum CoinMutationKind
    {
        Added,
        Spent,
        Restored,
    }

    public enum CoinTransactionFailure
    {
        None,
        InvalidAmount,
        InsufficientBalance,
        BalanceOverflow,
        InvalidRestoredBalance,
    }

    /// Identifies why a Coin mutation happened and, when useful, the
    /// concrete source that initiated it. Both values are presentation-
    /// agnostic so future analytics can consume them without coupling the
    /// wallet to an analytics SDK.
    public readonly struct CoinMutationContext
    {
        private readonly string _reason;
        private readonly string _sourceId;

        public CoinMutationContext(string reason, string sourceId = null)
        {
            _reason = string.IsNullOrWhiteSpace(reason)
                ? "unspecified"
                : reason.Trim();
            _sourceId = string.IsNullOrWhiteSpace(sourceId)
                ? string.Empty
                : sourceId.Trim();
        }

        public string Reason => string.IsNullOrWhiteSpace(_reason)
            ? "unspecified"
            : _reason;

        public string SourceId => _sourceId ?? string.Empty;
    }

    /// Immutable data emitted once for every successful balance change.
    /// UI code can subscribe to this record and refresh directly from
    /// `CurrentBalance`; no polling or presentation dependency is needed.
    public readonly struct CoinBalanceChangedEvent
    {
        public CoinBalanceChangedEvent(
            int previousBalance,
            int currentBalance,
            CoinMutationKind mutationKind,
            CoinMutationContext context)
        {
            PreviousBalance = previousBalance;
            CurrentBalance = currentBalance;
            MutationKind = mutationKind;
            Context = context;
        }

        public int PreviousBalance { get; }
        public int CurrentBalance { get; }
        public int Delta => CurrentBalance - PreviousBalance;
        public CoinMutationKind MutationKind { get; }
        public CoinMutationContext Context { get; }
    }

    /// Result returned by every requested mutation. Failed transactions
    /// always report the unchanged balance and never emit a change event.
    public readonly struct CoinTransactionResult
    {
        private CoinTransactionResult(
            bool succeeded,
            CoinTransactionFailure failure,
            int previousBalance,
            int currentBalance,
            CoinMutationKind mutationKind,
            CoinMutationContext context)
        {
            Succeeded = succeeded;
            Failure = failure;
            PreviousBalance = previousBalance;
            CurrentBalance = currentBalance;
            MutationKind = mutationKind;
            Context = context;
        }

        public bool Succeeded { get; }
        public CoinTransactionFailure Failure { get; }
        public int PreviousBalance { get; }
        public int CurrentBalance { get; }
        public int Delta => CurrentBalance - PreviousBalance;
        public CoinMutationKind MutationKind { get; }
        public CoinMutationContext Context { get; }

        internal static CoinTransactionResult Success(
            int previousBalance,
            int currentBalance,
            CoinMutationKind mutationKind,
            CoinMutationContext context) =>
            new CoinTransactionResult(
                true,
                CoinTransactionFailure.None,
                previousBalance,
                currentBalance,
                mutationKind,
                context);

        internal static CoinTransactionResult Failed(
            CoinTransactionFailure failure,
            int unchangedBalance,
            CoinMutationKind mutationKind,
            CoinMutationContext context) =>
            new CoinTransactionResult(
                false,
                failure,
                unchangedBalance,
                unchangedBalance,
                mutationKind,
                context);
    }

    /// Engine-free source of truth for one soft currency: Coins.
    /// Persistence and presentation are deliberately owned by higher layers.
    public sealed class CoinWallet
    {
        private readonly object _stateLock = new object();
        private int _balance;

        public CoinWallet(int initialBalance = 0)
        {
            if (initialBalance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialBalance),
                    "A Coin balance cannot be negative.");
            }

            _balance = initialBalance;
        }

        public event Action<CoinBalanceChangedEvent> BalanceChanged;

        public int Balance
        {
            get
            {
                lock (_stateLock)
                {
                    return _balance;
                }
            }
        }

        public bool CanAfford(int amount)
        {
            if (amount < 0)
            {
                return false;
            }

            lock (_stateLock)
            {
                return amount <= _balance;
            }
        }

        public CoinTransactionResult AddCoins(
            int amount,
            string reason,
            string sourceId = null) =>
            AddCoins(amount, new CoinMutationContext(reason, sourceId));

        public CoinTransactionResult AddCoins(
            int amount,
            CoinMutationContext context)
        {
            CoinBalanceChangedEvent change;
            CoinTransactionResult result;

            lock (_stateLock)
            {
                if (amount <= 0)
                {
                    return CoinTransactionResult.Failed(
                        CoinTransactionFailure.InvalidAmount,
                        _balance,
                        CoinMutationKind.Added,
                        context);
                }

                if (amount > int.MaxValue - _balance)
                {
                    return CoinTransactionResult.Failed(
                        CoinTransactionFailure.BalanceOverflow,
                        _balance,
                        CoinMutationKind.Added,
                        context);
                }

                int previousBalance = _balance;
                _balance += amount;
                change = new CoinBalanceChangedEvent(
                    previousBalance,
                    _balance,
                    CoinMutationKind.Added,
                    context);
                result = CoinTransactionResult.Success(
                    previousBalance,
                    _balance,
                    CoinMutationKind.Added,
                    context);
            }

            BalanceChanged?.Invoke(change);
            return result;
        }

        public CoinTransactionResult TrySpendCoins(
            int amount,
            string reason,
            string sourceId = null) =>
            TrySpendCoins(
                amount,
                new CoinMutationContext(reason, sourceId));

        public CoinTransactionResult TrySpendCoins(
            int amount,
            CoinMutationContext context)
        {
            CoinBalanceChangedEvent change;
            CoinTransactionResult result;

            lock (_stateLock)
            {
                if (amount <= 0)
                {
                    return CoinTransactionResult.Failed(
                        CoinTransactionFailure.InvalidAmount,
                        _balance,
                        CoinMutationKind.Spent,
                        context);
                }

                if (amount > _balance)
                {
                    return CoinTransactionResult.Failed(
                        CoinTransactionFailure.InsufficientBalance,
                        _balance,
                        CoinMutationKind.Spent,
                        context);
                }

                int previousBalance = _balance;
                _balance -= amount;
                change = new CoinBalanceChangedEvent(
                    previousBalance,
                    _balance,
                    CoinMutationKind.Spent,
                    context);
                result = CoinTransactionResult.Success(
                    previousBalance,
                    _balance,
                    CoinMutationKind.Spent,
                    context);
            }

            BalanceChanged?.Invoke(change);
            return result;
        }

        /// Applies a trusted persistence value. Normal game features should
        /// use `AddCoins` or `TrySpendCoins`; this entry point exists so the
        /// application service can import a fresh-device Cloud Save value.
        public CoinTransactionResult RestoreBalance(
            int restoredBalance,
            CoinMutationContext context)
        {
            CoinBalanceChangedEvent change = default;
            CoinTransactionResult result;
            bool changed;

            lock (_stateLock)
            {
                if (restoredBalance < 0)
                {
                    return CoinTransactionResult.Failed(
                        CoinTransactionFailure.InvalidRestoredBalance,
                        _balance,
                        CoinMutationKind.Restored,
                        context);
                }

                int previousBalance = _balance;
                changed = previousBalance != restoredBalance;
                _balance = restoredBalance;
                if (changed)
                {
                    change = new CoinBalanceChangedEvent(
                        previousBalance,
                        _balance,
                        CoinMutationKind.Restored,
                        context);
                }

                result = CoinTransactionResult.Success(
                    previousBalance,
                    _balance,
                    CoinMutationKind.Restored,
                    context);
            }

            if (changed)
            {
                BalanceChanged?.Invoke(change);
            }

            return result;
        }
    }
}
