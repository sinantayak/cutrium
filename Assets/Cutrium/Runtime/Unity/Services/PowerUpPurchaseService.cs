using System;
using Cutrium.Gameplay.Economy;

namespace Cutrium.Unity.Services
{
    public enum PowerUpPurchaseStatus
    {
        Purchased,
        InvalidPrice,
        InvalidQuantity,
        InsufficientCoins,
        InventoryOverflow,
        CoinSpendRejected,
        InventoryMutationRejected,
    }

    public readonly struct PowerUpPurchaseResult
    {
        public PowerUpPurchaseResult(
            PowerUpPurchaseStatus status,
            PowerUpKind kind,
            int quantity,
            int price,
            int balance,
            int ownedQuantity)
        {
            Status = status;
            Kind = kind;
            Quantity = quantity;
            Price = price;
            Balance = balance;
            OwnedQuantity = ownedQuantity;
        }

        public PowerUpPurchaseStatus Status { get; }
        public PowerUpKind Kind { get; }
        public int Quantity { get; }
        public int Price { get; }
        public int Balance { get; }
        public int OwnedQuantity { get; }
        public bool Purchased => Status == PowerUpPurchaseStatus.Purchased;
    }

    /// Coordinates the central Coin wallet and inventory as one validated
    /// purchase operation. UI code receives a result but never edits either
    /// balance directly.
    public sealed class PowerUpPurchaseService
    {
        public const string PurchaseMutationReason = "power_up_purchase";
        public const string RollbackMutationReason =
            "power_up_purchase_rollback";

        private readonly object _transactionLock = new object();
        private readonly CoinWalletService _coins;
        private readonly PowerUpInventoryService _inventory;

        public PowerUpPurchaseService(
            CoinWalletService coins,
            PowerUpInventoryService inventory)
        {
            _coins = coins ?? throw new ArgumentNullException(nameof(coins));
            _inventory = inventory
                ?? throw new ArgumentNullException(nameof(inventory));
        }

        public PowerUpPurchaseResult TryPurchase(
            PowerUpKind kind,
            int quantity,
            int price)
        {
            PowerUpInventory.ValidateKind(kind);
            lock (_transactionLock)
            {
                if (price <= 0)
                {
                    return Result(
                        PowerUpPurchaseStatus.InvalidPrice,
                        kind,
                        quantity,
                        price);
                }

                if (quantity <= 0)
                {
                    return Result(
                        PowerUpPurchaseStatus.InvalidQuantity,
                        kind,
                        quantity,
                        price);
                }

                if (!_inventory.CanAdd(kind, quantity))
                {
                    return Result(
                        PowerUpPurchaseStatus.InventoryOverflow,
                        kind,
                        quantity,
                        price);
                }

                if (!_coins.CanAfford(price))
                {
                    return Result(
                        PowerUpPurchaseStatus.InsufficientCoins,
                        kind,
                        quantity,
                        price);
                }

                string sourceId = GetSourceId(kind);
                CoinTransactionResult spend = _coins.TrySpendCoins(
                    price,
                    PurchaseMutationReason,
                    sourceId);
                if (!spend.Succeeded)
                {
                    PowerUpPurchaseStatus status = spend.Failure
                        == CoinTransactionFailure.InsufficientBalance
                            ? PowerUpPurchaseStatus.InsufficientCoins
                            : PowerUpPurchaseStatus.CoinSpendRejected;
                    return Result(status, kind, quantity, price);
                }

                PowerUpInventoryTransactionResult added = _inventory.Add(
                    kind,
                    quantity,
                    PurchaseMutationReason,
                    sourceId);
                if (!added.Succeeded)
                {
                    _coins.AddCoins(
                        price,
                        RollbackMutationReason,
                        sourceId);
                    return Result(
                        PowerUpPurchaseStatus.InventoryMutationRejected,
                        kind,
                        quantity,
                        price);
                }

                return Result(
                    PowerUpPurchaseStatus.Purchased,
                    kind,
                    quantity,
                    price);
            }
        }

        private PowerUpPurchaseResult Result(
            PowerUpPurchaseStatus status,
            PowerUpKind kind,
            int quantity,
            int price) => new PowerUpPurchaseResult(
                status,
                kind,
                quantity,
                price,
                _coins.Balance,
                _inventory.GetCount(kind));

        private static string GetSourceId(PowerUpKind kind) => kind switch
        {
            PowerUpKind.FreezePulse => "freeze-pulse",
            PowerUpKind.InstantBarrier => "instant-barrier",
            PowerUpKind.GravityWell => "gravity-well",
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}
