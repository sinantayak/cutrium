using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using UnityEngine;

namespace Cutrium.Unity.Services
{
    public enum SocialSignInProvider
    {
        GooglePlayGames,
        Apple,
    }

    /// Links the current anonymous session to a Google Play Games or Apple
    /// account, so progress can follow the player across devices. Only
    /// ever called from an explicit player action (e.g. a Settings panel
    /// "Link Account" button) -- never automatically at boot.
    ///
    /// Both providers need Dashboard/store-console configuration the
    /// project owner has not completed yet (see Milestone 4 in
    /// .agent/plans/013-cloud-login-and-progress.md): a Google Play Games
    /// OAuth Web Client ID from Google Play Console, and a Sign In with
    /// Apple Services ID/Team ID/Key from an active Apple Developer
    /// Program membership. Until those exist, these calls fail with a
    /// readable reason instead of throwing an unhandled exception --
    /// this class is safe to wire into UI now and will start working the
    /// moment that Dashboard setup is done, with no code changes needed
    /// here.
    ///
    /// The caller is responsible for obtaining the native platform token
    /// first (a Google Play Games auth code from the Google Play Games
    /// SDK, or an Apple identity token from Sign In with Apple on iOS) --
    /// this class only exchanges that token with Unity Authentication.
    public sealed class SocialSignInLinker
    {
        public event Action<SocialSignInProvider> LinkSucceeded;
        public event Action<SocialSignInProvider, Exception> LinkFailed;

        public async Task<bool> LinkGooglePlayGamesAsync(string authCode)
        {
            try
            {
                await AuthenticationService.Instance
                    .LinkWithGooglePlayGamesAsync(authCode);
                LinkSucceeded?.Invoke(SocialSignInProvider.GooglePlayGames);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Google Play Games account link failed: " + exception);
                LinkFailed?.Invoke(
                    SocialSignInProvider.GooglePlayGames,
                    exception);
                return false;
            }
        }

        public async Task<bool> LinkAppleAsync(string identityToken)
        {
            try
            {
                await AuthenticationService.Instance
                    .LinkWithAppleAsync(identityToken);
                LinkSucceeded?.Invoke(SocialSignInProvider.Apple);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Apple account link failed: " + exception);
                LinkFailed?.Invoke(SocialSignInProvider.Apple, exception);
                return false;
            }
        }
    }
}
