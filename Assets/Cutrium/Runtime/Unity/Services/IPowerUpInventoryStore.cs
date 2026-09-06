using System.Threading.Tasks;
using Cutrium.Gameplay.Economy;

namespace Cutrium.Unity.Services
{
    /// Persistence boundary for the three spendable power-up quantities.
    public interface IPowerUpInventoryStore
    {
        bool TryLoadLocalPowerUpInventory(
            out PowerUpInventorySnapshot inventory);

        void SavePowerUpInventory(PowerUpInventorySnapshot inventory);

        Task PushPowerUpInventoryToCloudAsync(
            PowerUpInventorySnapshot inventory);

        Task<PowerUpInventorySnapshot?>
            TryLoadCloudPowerUpInventoryAsync();
    }
}
