using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

namespace Cutrium.Unity.Services
{
    /// Persists player progress and preferences. The local `PlayerPrefs`
    /// mirror is always the synchronous source of truth for what loads at
    /// boot -- the game must never wait on a network call to start --
    /// while Cloud Save is written best-effort in the background so a
    /// signed-in player's progress and settings can eventually follow them
    /// across devices (see Milestone 4 in
    /// .agent/plans/013-cloud-login-and-progress.md for account linking).
    ///
    /// For the four existing preferences (sound/music/haptic/language),
    /// `PlayerPrefs` reads/writes stay owned by their existing callers
    /// (`SettingsPanelPresenter`, `LocalizationService`) exactly as
    /// before -- this store only mirrors their already-decided value to
    /// Cloud Save, it is not the source of truth for them.
    public sealed class PlayerProgressStore : ICoinBalanceStore
    {
        private const string CurrentLevelIndexCloudKey = "CurrentLevelIndex";
        private const string CurrentLevelIndexPrefsKey =
            "Cutrium.Progress.CurrentLevelIndex";
        private const string HighestUnlockedLevelIndexCloudKey =
            "HighestUnlockedLevelIndex";
        private const string HighestUnlockedLevelIndexPrefsKey =
            "Cutrium.Progress.HighestUnlockedLevelIndex";
        private const string SoundEnabledCloudKey = "SoundEnabled";
        private const string MusicEnabledCloudKey = "MusicEnabled";
        private const string HapticEnabledCloudKey = "HapticEnabled";
        private const string LanguageCloudKey = "Language";
        private const string CoinBalanceCloudKey = "CoinBalance";
        private const string CoinBalancePrefsKey =
            "Cutrium.Economy.CoinBalance";

        // Coin mutations may happen several times in one frame. Serializing
        // their Cloud Save writes prevents an older request from completing
        // after a newer one and leaving the remote mirror stale.
        private readonly object _coinCloudWriteLock = new object();
        private Task _coinCloudWriteTail = Task.CompletedTask;

        /// Synchronous, offline-safe. Call at boot before the level catalog
        /// needs an index to load. Deliberately a no-op (always 0) while
        /// the Unity Test Framework is running a test -- otherwise a real
        /// play session's saved progress would leak into the Editor's
        /// PlayerPrefs and make PlayMode tests non-deterministic across
        /// runs (they assume a fresh level index 0 every time).
        public int LoadLocalCurrentLevelIndex()
        {
            if (TestModeDetector.IsRunningTests)
            {
                return 0;
            }

            return PlayerPrefs.GetInt(CurrentLevelIndexPrefsKey, 0);
        }

        /// Writes the local mirror synchronously (so progress is never lost
        /// even fully offline), then fires an unawaited, best-effort Cloud
        /// Save write. Never throws and never blocks the caller. No-op
        /// under the Test Framework -- see `LoadLocalCurrentLevelIndex`.
        public void SaveCurrentLevelIndex(int levelIndex)
        {
            if (TestModeDetector.IsRunningTests)
            {
                return;
            }

            PlayerPrefs.SetInt(CurrentLevelIndexPrefsKey, levelIndex);
            PlayerPrefs.Save();
            PushToCloud(CurrentLevelIndexCloudKey, levelIndex);
        }

        /// Synchronous, offline-safe. Distinct from
        /// `LoadLocalCurrentLevelIndex` (the resume point, which moves
        /// backward when the player replays an earlier level) -- this is
        /// the furthest level index ever unlocked, and only ever moves
        /// forward. Falls back to the resume index for saves written
        /// before this field existed, since under the old single-index
        /// scheme that value was also the furthest reached level as long
        /// as the player never replayed backward.
        public int LoadLocalHighestUnlockedLevelIndex()
        {
            if (TestModeDetector.IsRunningTests)
            {
                return 0;
            }

            int stored = PlayerPrefs.GetInt(
                HighestUnlockedLevelIndexPrefsKey,
                -1);
            return stored >= 0 ? stored : LoadLocalCurrentLevelIndex();
        }

        /// Writes the local mirror synchronously, then fires an unawaited,
        /// best-effort Cloud Save write -- see `SaveCurrentLevelIndex`.
        /// Callers must only ever pass a value at or above the previously
        /// saved one; this store does not itself enforce monotonicity.
        public void SaveHighestUnlockedLevelIndex(int levelIndex)
        {
            if (TestModeDetector.IsRunningTests)
            {
                return;
            }

            PlayerPrefs.SetInt(HighestUnlockedLevelIndexPrefsKey, levelIndex);
            PlayerPrefs.Save();
            PushToCloud(HighestUnlockedLevelIndexCloudKey, levelIndex);
        }

        /// Reads the local Coin mirror without creating a key for a legacy
        /// or fresh install. Returning `false` is important for cloud
        /// reconciliation: only a device that has never stored a Coin value
        /// may import a pre-existing cloud balance. Corrupt negative values
        /// are repaired to the safe legacy default of zero.
        public bool TryLoadLocalCoinBalance(out int balance)
        {
            balance = 0;
            if (TestModeDetector.IsRunningTests
                || !PlayerPrefs.HasKey(CoinBalancePrefsKey))
            {
                return false;
            }

            int storedBalance = PlayerPrefs.GetInt(CoinBalancePrefsKey, 0);
            if (storedBalance >= 0)
            {
                balance = storedBalance;
                return true;
            }

            Debug.LogWarning(
                "Stored Coin balance was negative and has been repaired "
                + "to zero.");
            PlayerPrefs.SetInt(CoinBalancePrefsKey, 0);
            PlayerPrefs.Save();
            return true;
        }

        /// Saves the local mirror synchronously, then mirrors the same value
        /// to Cloud Save in the background. CoinWalletService is the only
        /// normal caller and has already enforced all transaction rules.
        public void SaveCoinBalance(int balance)
        {
            ValidateCoinBalance(balance);
            if (TestModeDetector.IsRunningTests)
            {
                return;
            }

            PlayerPrefs.SetInt(CoinBalancePrefsKey, balance);
            PlayerPrefs.Save();
            _ = QueueCoinBalanceCloudPush(balance);
        }

        /// Used after sign-in when the device already has authoritative
        /// local Coin data. This covers mutations made while offline or
        /// before Unity Authentication completed during this boot.
        public Task PushCoinBalanceToCloudAsync(int balance)
        {
            ValidateCoinBalance(balance);
            return TestModeDetector.IsRunningTests
                ? Task.CompletedTask
                : QueueCoinBalanceCloudPush(balance);
        }

        /// Attempts to read a Coin balance for a device that has no local
        /// Coin key. Missing data and network/service failures return null,
        /// following this store's existing best-effort convention. A corrupt
        /// negative cloud value is repaired to the safe default in memory;
        /// the service's subsequent local save mirrors that repair back.
        public async Task<int?> TryLoadCloudCoinBalanceAsync()
        {
            try
            {
                if (TestModeDetector.IsRunningTests || !IsSignedIn())
                {
                    return null;
                }

                var keys = new HashSet<string> { CoinBalanceCloudKey };
                Dictionary<string, Item> results = await CloudSaveService
                    .Instance.Data.Player.LoadAsync(keys);
                if (!results.TryGetValue(CoinBalanceCloudKey, out Item item))
                {
                    return null;
                }

                int cloudBalance = item.Value.GetAs<int>();
                if (cloudBalance >= 0)
                {
                    return cloudBalance;
                }

                Debug.LogWarning(
                    "Cloud Coin balance was negative and has been repaired "
                    + "to zero.");
                return 0;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Cloud Coin balance pull failed; local balance "
                    + "unchanged. " + exception);
                return null;
            }
        }

        /// Mirrors an already-locally-saved sound preference to Cloud
        /// Save. Call alongside the existing `PlayerPrefs` write in
        /// `SettingsPanelPresenter` -- this does not touch local storage.
        public void SaveSoundEnabled(bool enabled) =>
            PushToCloud(SoundEnabledCloudKey, enabled);

        /// See `SaveSoundEnabled`.
        public void SaveMusicEnabled(bool enabled) =>
            PushToCloud(MusicEnabledCloudKey, enabled);

        /// See `SaveSoundEnabled`.
        public void SaveHapticEnabled(bool enabled) =>
            PushToCloud(HapticEnabledCloudKey, enabled);

        /// Mirrors an already-locally-saved language preference (the raw
        /// `SupportedLanguage` enum int, matching how `LocalizationService`
        /// already stores it in `PlayerPrefs`) to Cloud Save. Kept as a
        /// plain int here rather than the enum type itself so this
        /// `Cutrium.Unity` class never needs to reference
        /// `Cutrium.Presentation` (wrong layering direction).
        public void SaveLanguage(int languageCode) =>
            PushToCloud(LanguageCloudKey, languageCode);

        private static void PushToCloud(string key, object value)
        {
            if (TestModeDetector.IsRunningTests)
            {
                return;
            }

            _ = PushToCloudAsync(key, value);
        }

        private static async Task PushToCloudAsync(string key, object value)
        {
            try
            {
                if (!IsSignedIn())
                {
                    return;
                }

                var data = new Dictionary<string, object> { { key, value } };
                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Cloud save failed for '{key}'; kept locally only. "
                    + exception);
            }
        }

        /// Best-effort background pull, meant to be called once shortly
        /// after sign-in. Only ever *raises* the local level index (never
        /// lowers it) -- if the cloud has a higher level than this device
        /// has ever recorded, that becomes the local value used on the
        /// *next* boot, deliberately not hot-swapped into the current
        /// session to avoid an in-progress level being yanked away.
        public async Task PullCloudCurrentLevelIndexAsync()
        {
            try
            {
                if (TestModeDetector.IsRunningTests || !IsSignedIn())
                {
                    return;
                }

                var keys = new HashSet<string> { CurrentLevelIndexCloudKey };
                Dictionary<string, Item> results = await CloudSaveService
                    .Instance.Data.Player.LoadAsync(keys);
                if (!results.TryGetValue(
                        CurrentLevelIndexCloudKey,
                        out Item item))
                {
                    return;
                }

                int cloudLevelIndex = item.Value.GetAs<int>();
                if (cloudLevelIndex > LoadLocalCurrentLevelIndex())
                {
                    PlayerPrefs.SetInt(
                        CurrentLevelIndexPrefsKey,
                        cloudLevelIndex);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Cloud progress pull failed; local progress unchanged. "
                    + exception);
            }
        }

        /// Best-effort background pull, meant to be called once shortly
        /// after sign-in. Only ever *raises* the local value (never lowers
        /// it) -- see `PullCloudCurrentLevelIndexAsync`.
        public async Task PullCloudHighestUnlockedLevelIndexAsync()
        {
            try
            {
                if (TestModeDetector.IsRunningTests || !IsSignedIn())
                {
                    return;
                }

                var keys = new HashSet<string>
                {
                    HighestUnlockedLevelIndexCloudKey
                };
                Dictionary<string, Item> results = await CloudSaveService
                    .Instance.Data.Player.LoadAsync(keys);
                if (!results.TryGetValue(
                        HighestUnlockedLevelIndexCloudKey,
                        out Item item))
                {
                    return;
                }

                int cloudIndex = item.Value.GetAs<int>();
                if (cloudIndex > LoadLocalHighestUnlockedLevelIndex())
                {
                    PlayerPrefs.SetInt(
                        HighestUnlockedLevelIndexPrefsKey,
                        cloudIndex);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Cloud unlocked-level pull failed; local progress "
                    + "unchanged. " + exception);
            }
        }

        private static bool IsSignedIn()
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

        private Task QueueCoinBalanceCloudPush(int balance)
        {
            lock (_coinCloudWriteLock)
            {
                _coinCloudWriteTail = PushCoinBalanceAfterAsync(
                    _coinCloudWriteTail,
                    balance);
                return _coinCloudWriteTail;
            }
        }

        private static async Task PushCoinBalanceAfterAsync(
            Task previousPush,
            int balance)
        {
            try
            {
                await previousPush;
            }
            catch (Exception exception)
            {
                // PushToCloudAsync already catches service errors, but keep
                // the queue moving if an unexpected task failure occurs.
                Debug.LogWarning(
                    "Previous Coin cloud-save task failed; continuing with "
                    + "the latest balance. " + exception);
            }

            await PushToCloudAsync(CoinBalanceCloudKey, balance);
        }

        private static void ValidateCoinBalance(int balance)
        {
            if (balance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(balance),
                    "A Coin balance cannot be negative.");
            }
        }
    }
}
