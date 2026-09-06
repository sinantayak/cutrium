using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Cutrium.Unity.Services
{
    /// Initializes Unity Gaming Services and signs the player in
    /// anonymously at startup. This must never block or break gameplay if
    /// it fails -- offline, or Authentication not yet enabled for this
    /// project on the Unity Dashboard are both expected, recoverable
    /// states -- so every failure is caught and logged rather than thrown.
    [DisallowMultipleComponent]
    public sealed class CloudServicesBootstrap : MonoBehaviour
    {
        private bool _started;
        private Task _signInTask;
        private CoinWalletService _coins;
        private LevelCoinRewardService _levelRewards;
        private PowerUpInventoryService _powerUps;
        private PowerUpPurchaseService _powerUpPurchases;
        private PlayerProgressStore _playerProgressStore;

        public event Action SignedIn;
        public event Action<Exception> SignInFailed;

        /// Single application-level Coin service for the current game
        /// session. Future UI/gameplay composition should receive this
        /// reference rather than creating another wallet or searching the
        /// scene at runtime.
        public CoinWalletService Coins => EnsureCoinService();

        public LevelCoinRewardService LevelRewards =>
            _levelRewards ??= new LevelCoinRewardService(EnsureCoinService());

        public PowerUpInventoryService PowerUps =>
            EnsurePowerUpInventoryService();

        public PowerUpPurchaseService PowerUpPurchases =>
            _powerUpPurchases ??= new PowerUpPurchaseService(
                EnsureCoinService(),
                EnsurePowerUpInventoryService());

        public bool IsSignedIn
        {
            get
            {
                try
                {
                    return AuthenticationService.Instance != null
                        && AuthenticationService.Instance.IsSignedIn;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public string PlayerId
        {
            get
            {
                try
                {
                    return AuthenticationService.Instance?.PlayerId;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private void Awake()
        {
            EnsureCoinService();
            EnsurePowerUpInventoryService();
        }

        private void OnEnable()
        {
            EnsureCoinService();
            EnsurePowerUpInventoryService();
            if (_started
                || !Application.isPlaying
                || TestModeDetector.IsRunningTests)
            {
                return;
            }

            _started = true;
            _signInTask = InitializeAndSignInAsync();
        }

        private void OnDestroy()
        {
            _coins?.Dispose();
            _powerUps?.Dispose();
            _coins = null;
            _levelRewards = null;
            _powerUps = null;
            _powerUpPurchases = null;
            _playerProgressStore = null;
        }

        /// Idempotent: safe to call again (e.g. after linking a social
        /// account changes sign-in state) -- returns the same in-flight
        /// task if one is already running.
        public Task InitializeAndSignInAsync()
        {
            if (_signInTask != null && !_signInTask.IsCompleted)
            {
                return _signInTask;
            }

            _signInTask = RunAsync();
            return _signInTask;
        }

        private async Task RunAsync()
        {
            try
            {
                if (UnityServices.State
                    == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance
                        .SignInAnonymouslyAsync();
                }

                await Task.WhenAll(
                    Coins.SynchronizeWithCloudAsync(),
                    PowerUps.SynchronizeWithCloudAsync());
                SignedIn?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Cloud services sign-in failed; continuing offline. "
                    + "This is expected until Authentication is enabled "
                    + "for this project on the Unity Dashboard. "
                    + exception,
                    this);
                SignInFailed?.Invoke(exception);
            }
        }

        private CoinWalletService EnsureCoinService()
        {
            if (_coins == null)
            {
                _coins = new CoinWalletService(
                    EnsurePlayerProgressStore(),
                    CoinWalletService.DefaultStartingBalance);
            }

            return _coins;
        }

        private PowerUpInventoryService EnsurePowerUpInventoryService()
        {
            if (_powerUps == null)
            {
                _powerUps = new PowerUpInventoryService(
                    EnsurePlayerProgressStore());
            }

            return _powerUps;
        }

        private PlayerProgressStore EnsurePlayerProgressStore() =>
            _playerProgressStore ??= new PlayerProgressStore();
    }
}
