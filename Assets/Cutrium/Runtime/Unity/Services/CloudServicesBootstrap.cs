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

        public event Action SignedIn;
        public event Action<Exception> SignInFailed;

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

        private void OnEnable()
        {
            if (_started
                || !Application.isPlaying
                || TestModeDetector.IsRunningTests)
            {
                return;
            }

            _started = true;
            _signInTask = InitializeAndSignInAsync();
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
    }
}
