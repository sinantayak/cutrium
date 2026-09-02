using System.Threading.Tasks;

namespace Cutrium.Unity.Services
{
    /// Persistence boundary used by CoinWalletService. The production
    /// implementation is PlayerProgressStore; the interface keeps economy
    /// coordination testable without touching real PlayerPrefs or UGS data.
    public interface ICoinBalanceStore
    {
        bool TryLoadLocalCoinBalance(out int balance);

        void SaveCoinBalance(int balance);

        Task PushCoinBalanceToCloudAsync(int balance);

        Task<int?> TryLoadCloudCoinBalanceAsync();
    }
}
