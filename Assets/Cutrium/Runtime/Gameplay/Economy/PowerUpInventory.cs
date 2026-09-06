using System;
using System.Collections.Generic;

namespace Cutrium.Gameplay.Economy
{
    public enum PowerUpKind
    {
        FreezePulse = 0,
        InstantBarrier = 1,
        GravityWell = 2,
    }

    public enum PowerUpInventoryMutationKind
    {
        Added,
        Consumed,
        Restored,
    }

    public enum PowerUpInventoryFailure
    {
        None,
        InvalidAmount,
        InsufficientQuantity,
        QuantityOverflow,
    }

    public readonly struct PowerUpInventorySnapshot : IEquatable<
        PowerUpInventorySnapshot>
    {
        public PowerUpInventorySnapshot(
            int freezePulse,
            int instantBarrier,
            int gravityWell)
        {
            ValidateCount(freezePulse, nameof(freezePulse));
            ValidateCount(instantBarrier, nameof(instantBarrier));
            ValidateCount(gravityWell, nameof(gravityWell));
            FreezePulse = freezePulse;
            InstantBarrier = instantBarrier;
            GravityWell = gravityWell;
        }

        public int FreezePulse { get; }
        public int InstantBarrier { get; }
        public int GravityWell { get; }

        public int GetCount(PowerUpKind kind) => kind switch
        {
            PowerUpKind.FreezePulse => FreezePulse,
            PowerUpKind.InstantBarrier => InstantBarrier,
            PowerUpKind.GravityWell => GravityWell,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        public bool Equals(PowerUpInventorySnapshot other) =>
            FreezePulse == other.FreezePulse
            && InstantBarrier == other.InstantBarrier
            && GravityWell == other.GravityWell;

        public override bool Equals(object obj) =>
            obj is PowerUpInventorySnapshot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = FreezePulse;
                hashCode = (hashCode * 397) ^ InstantBarrier;
                hashCode = (hashCode * 397) ^ GravityWell;
                return hashCode;
            }
        }

        public static bool operator ==(
            PowerUpInventorySnapshot left,
            PowerUpInventorySnapshot right) => left.Equals(right);

        public static bool operator !=(
            PowerUpInventorySnapshot left,
            PowerUpInventorySnapshot right) => !left.Equals(right);

        private static void ValidateCount(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "A power-up quantity cannot be negative.");
            }
        }
    }

    public readonly struct PowerUpInventoryMutationContext
    {
        private readonly string _reason;
        private readonly string _sourceId;

        public PowerUpInventoryMutationContext(
            string reason,
            string sourceId = null)
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

    public readonly struct PowerUpInventoryChangedEvent
    {
        public PowerUpInventoryChangedEvent(
            PowerUpKind kind,
            int previousQuantity,
            int currentQuantity,
            PowerUpInventoryMutationKind mutationKind,
            PowerUpInventoryMutationContext context)
        {
            Kind = kind;
            PreviousQuantity = previousQuantity;
            CurrentQuantity = currentQuantity;
            MutationKind = mutationKind;
            Context = context;
        }

        public PowerUpKind Kind { get; }
        public int PreviousQuantity { get; }
        public int CurrentQuantity { get; }
        public int Delta => CurrentQuantity - PreviousQuantity;
        public PowerUpInventoryMutationKind MutationKind { get; }
        public PowerUpInventoryMutationContext Context { get; }
    }

    public readonly struct PowerUpInventoryTransactionResult
    {
        private PowerUpInventoryTransactionResult(
            bool succeeded,
            PowerUpInventoryFailure failure,
            PowerUpKind kind,
            int requestedAmount,
            int previousQuantity,
            int currentQuantity)
        {
            Succeeded = succeeded;
            Failure = failure;
            Kind = kind;
            RequestedAmount = requestedAmount;
            PreviousQuantity = previousQuantity;
            CurrentQuantity = currentQuantity;
        }

        public bool Succeeded { get; }
        public PowerUpInventoryFailure Failure { get; }
        public PowerUpKind Kind { get; }
        public int RequestedAmount { get; }
        public int PreviousQuantity { get; }
        public int CurrentQuantity { get; }

        internal static PowerUpInventoryTransactionResult Success(
            PowerUpKind kind,
            int amount,
            int previousQuantity,
            int currentQuantity) => new PowerUpInventoryTransactionResult(
                true,
                PowerUpInventoryFailure.None,
                kind,
                amount,
                previousQuantity,
                currentQuantity);

        internal static PowerUpInventoryTransactionResult Failed(
            PowerUpInventoryFailure failure,
            PowerUpKind kind,
            int amount,
            int unchangedQuantity) => new PowerUpInventoryTransactionResult(
                false,
                failure,
                kind,
                amount,
                unchangedQuantity,
                unchangedQuantity);
    }

    /// Engine-free source of truth for the player's three consumable
    /// power-up quantities. Persistence and UI remain higher-layer concerns.
    public sealed class PowerUpInventory
    {
        private readonly object _stateLock = new object();
        private int _freezePulse;
        private int _instantBarrier;
        private int _gravityWell;

        public PowerUpInventory(PowerUpInventorySnapshot initial = default)
        {
            _freezePulse = initial.FreezePulse;
            _instantBarrier = initial.InstantBarrier;
            _gravityWell = initial.GravityWell;
        }

        public event Action<PowerUpInventoryChangedEvent> InventoryChanged;

        public PowerUpInventorySnapshot Snapshot
        {
            get
            {
                lock (_stateLock)
                {
                    return CreateSnapshot();
                }
            }
        }

        public int GetCount(PowerUpKind kind)
        {
            ValidateKind(kind);
            lock (_stateLock)
            {
                return GetCountUnsafe(kind);
            }
        }

        public bool CanAdd(PowerUpKind kind, int amount)
        {
            ValidateKind(kind);
            if (amount <= 0)
            {
                return false;
            }

            lock (_stateLock)
            {
                return GetCountUnsafe(kind) <= int.MaxValue - amount;
            }
        }

        public PowerUpInventoryTransactionResult TryAdd(
            PowerUpKind kind,
            int amount,
            PowerUpInventoryMutationContext context)
        {
            ValidateKind(kind);
            PowerUpInventoryChangedEvent change;
            PowerUpInventoryTransactionResult result;
            lock (_stateLock)
            {
                int previous = GetCountUnsafe(kind);
                if (amount <= 0)
                {
                    return PowerUpInventoryTransactionResult.Failed(
                        PowerUpInventoryFailure.InvalidAmount,
                        kind,
                        amount,
                        previous);
                }

                if (previous > int.MaxValue - amount)
                {
                    return PowerUpInventoryTransactionResult.Failed(
                        PowerUpInventoryFailure.QuantityOverflow,
                        kind,
                        amount,
                        previous);
                }

                int current = previous + amount;
                SetCountUnsafe(kind, current);
                change = new PowerUpInventoryChangedEvent(
                    kind,
                    previous,
                    current,
                    PowerUpInventoryMutationKind.Added,
                    context);
                result = PowerUpInventoryTransactionResult.Success(
                    kind,
                    amount,
                    previous,
                    current);
            }

            InventoryChanged?.Invoke(change);
            return result;
        }

        public PowerUpInventoryTransactionResult TryConsume(
            PowerUpKind kind,
            int amount,
            PowerUpInventoryMutationContext context)
        {
            ValidateKind(kind);
            PowerUpInventoryChangedEvent change;
            PowerUpInventoryTransactionResult result;
            lock (_stateLock)
            {
                int previous = GetCountUnsafe(kind);
                if (amount <= 0)
                {
                    return PowerUpInventoryTransactionResult.Failed(
                        PowerUpInventoryFailure.InvalidAmount,
                        kind,
                        amount,
                        previous);
                }

                if (amount > previous)
                {
                    return PowerUpInventoryTransactionResult.Failed(
                        PowerUpInventoryFailure.InsufficientQuantity,
                        kind,
                        amount,
                        previous);
                }

                int current = previous - amount;
                SetCountUnsafe(kind, current);
                change = new PowerUpInventoryChangedEvent(
                    kind,
                    previous,
                    current,
                    PowerUpInventoryMutationKind.Consumed,
                    context);
                result = PowerUpInventoryTransactionResult.Success(
                    kind,
                    amount,
                    previous,
                    current);
            }

            InventoryChanged?.Invoke(change);
            return result;
        }

        public void Restore(
            PowerUpInventorySnapshot restored,
            PowerUpInventoryMutationContext context)
        {
            List<PowerUpInventoryChangedEvent> changes = null;
            lock (_stateLock)
            {
                RestoreOne(
                    PowerUpKind.FreezePulse,
                    restored.FreezePulse,
                    context,
                    ref changes);
                RestoreOne(
                    PowerUpKind.InstantBarrier,
                    restored.InstantBarrier,
                    context,
                    ref changes);
                RestoreOne(
                    PowerUpKind.GravityWell,
                    restored.GravityWell,
                    context,
                    ref changes);
            }

            if (changes == null)
            {
                return;
            }

            for (int index = 0; index < changes.Count; index++)
            {
                InventoryChanged?.Invoke(changes[index]);
            }
        }

        public static void ValidateKind(PowerUpKind kind)
        {
            if (kind != PowerUpKind.FreezePulse
                && kind != PowerUpKind.InstantBarrier
                && kind != PowerUpKind.GravityWell)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private void RestoreOne(
            PowerUpKind kind,
            int current,
            PowerUpInventoryMutationContext context,
            ref List<PowerUpInventoryChangedEvent> changes)
        {
            int previous = GetCountUnsafe(kind);
            if (previous == current)
            {
                return;
            }

            SetCountUnsafe(kind, current);
            changes ??= new List<PowerUpInventoryChangedEvent>(3);
            changes.Add(new PowerUpInventoryChangedEvent(
                kind,
                previous,
                current,
                PowerUpInventoryMutationKind.Restored,
                context));
        }

        private PowerUpInventorySnapshot CreateSnapshot() =>
            new PowerUpInventorySnapshot(
                _freezePulse,
                _instantBarrier,
                _gravityWell);

        private int GetCountUnsafe(PowerUpKind kind) => kind switch
        {
            PowerUpKind.FreezePulse => _freezePulse,
            PowerUpKind.InstantBarrier => _instantBarrier,
            PowerUpKind.GravityWell => _gravityWell,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        private void SetCountUnsafe(PowerUpKind kind, int value)
        {
            switch (kind)
            {
                case PowerUpKind.FreezePulse:
                    _freezePulse = value;
                    break;
                case PowerUpKind.InstantBarrier:
                    _instantBarrier = value;
                    break;
                case PowerUpKind.GravityWell:
                    _gravityWell = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
