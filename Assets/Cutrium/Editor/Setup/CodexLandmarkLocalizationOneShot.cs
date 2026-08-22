using System;
using UnityEditor;
using UnityEngine;

namespace Cutrium.Editor.Setup
{
    [InitializeOnLoad]
    internal static class CodexLandmarkLocalizationOneShot
    {
        private const string CompletedKey =
            "Cutrium.Codex.LandmarkLocalization.20260823.Completed";

        static CodexLandmarkLocalizationOneShot()
        {
            if (!SessionState.GetBool(CompletedKey, false))
            {
                EditorApplication.delayCall += Apply;
            }
        }

        private static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += Apply;
                return;
            }

            try
            {
                SettingsPanelSceneSetup.Apply();
                SessionState.SetBool(CompletedKey, true);
                Debug.Log("CODEX_LANDMARK_LOCALIZATION_SETUP_COMPLETE");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
