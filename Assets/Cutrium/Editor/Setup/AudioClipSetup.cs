using System;
using Cutrium.Presentation.Feedback;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Landmark;
using Cutrium.Presentation.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cutrium.Editor.Setup
{
    public static class AudioClipSetup
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";
        private const string SoundsFolder =
            "Assets/Cutrium/Content/Sounds/";

        [MenuItem("Cutrium/Setup/Apply Audio Clips")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before applying audio clips.");
            }

            Scene scene = OpenVerticalSliceScene();
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            FeedbackAudioPresenter feedbackAudio = root
                .GetComponentInChildren<FeedbackAudioPresenter>(true);
            if (feedbackAudio == null)
            {
                throw new InvalidOperationException(
                    "Audio clip setup requires an existing " +
                    "FeedbackAudioPresenter in the scene.");
            }

            Transform musicSourceTransform =
                feedbackAudio.transform.Find("MusicSource");
            AudioSource musicSource = musicSourceTransform != null
                ? musicSourceTransform.GetComponent<AudioSource>()
                : null;
            if (musicSource == null)
            {
                throw new InvalidOperationException(
                    "Audio clip setup requires the MusicSource AudioSource " +
                    "created by 'Cutrium/Setup/Apply Settings Panel'.");
            }

            AudioSource barrierLoopSource = EnsureLoopSource(
                feedbackAudio.transform,
                "BarrierGrowLoopSource");
            AudioSource gravityWellLoopSource = EnsureLoopSource(
                feedbackAudio.transform,
                "GravityWellLoopSource");
            feedbackAudio.ConfigureLoopSources(
                barrierLoopSource,
                gravityWellLoopSource);

            AudioClip outOfCutsClip = RequireClip("SFX_OutOfCuts.wav");
            var clips = new FeedbackAudioClipSet
            {
                StartClip = RequireClip("SFX_BarrierStart.wav"),
                GrowClip = RequireClip("SFX_BarrierGrow.wav"),
                LockClip = RequireClip("SFX_BarrierLock.wav"),
                BreakClip = RequireClip("SFX_BarrierBreak.wav"),
                FillClip = RequireClip("SFX_RegionCaptured.wav"),
                // Optional: stays null (and safely silent) if not present.
                SandFillClip = TryLoadClip("SFX_SandFill.wav"),
                LargeCaptureClip = RequireClip("SFX_LargeCapture.wav"),
                NearMissClip = RequireClip("SFX_NearMiss.wav"),
                ComboClip = RequireClip("SFX_ComboChanged.wav"),
                CompleteClip = RequireClip("SFX_LevelComplete.wav"),
                UiClip = RequireClip("SFX_Button.wav"),
                PowerFreezeActivateClip =
                    RequireClip("SFX_PowerFreezeActivate.wav"),
                PowerInstantBarrierArmClip =
                    RequireClip("SFX_PowerInstantBarrierArm.wav"),
                PowerInstantBarrierConsumeClip =
                    RequireClip("SFX_PowerInstantBarrierConsume.wav"),
                PowerGravityWellActivateClip =
                    RequireClip("SFX_PowerGravityWellActivate.wav"),
                PowerUnavailableClip =
                    RequireClip("SFX_PowerUnavailable.wav"),
                HunterReactClip = RequireClip("SFX_HunterReact.wav"),
                // Out Of Lives has no dedicated recording yet; it
                // intentionally shares Out Of Cuts per product direction.
                OutOfCutsClip = outOfCutsClip,
                OutOfLivesClip = outOfCutsClip,
            };
            feedbackAudio.ConfigureClips(clips);

            AudioClip musicClip = RequireClip("SFX_Music.mp3");
            musicSource.clip = musicClip;

            Validate(
                feedbackAudio,
                musicSource,
                barrierLoopSource,
                gravityWellLoopSource,
                clips,
                musicClip);

            PreLevelIntroPresenter preLevelIntro = root
                .GetComponentInChildren<PreLevelIntroPresenter>(true);
            if (preLevelIntro == null)
            {
                throw new InvalidOperationException(
                    "Audio clip setup requires an existing " +
                    "PreLevelIntroPresenter in the scene.");
            }

            AudioSource preLevelIntroAudioSource = EnsureOneShotSource(
                preLevelIntro.transform,
                "PreLevelIntroAudioSource");
            preLevelIntro.ConfigureAudio(
                preLevelIntroAudioSource,
                // All five are optional/new -- stay null (and safely
                // silent) until the matching files are added.
                TryLoadClip("SFX_LevelReveal.wav"),
                TryLoadClip("SFX_TargetReveal.wav"),
                TryLoadClip("SFX_CutReveal.wav"),
                TryLoadClip("SFX_InfoReveal.wav"),
                TryLoadClip("SFX_IntroFlightLand.wav"));

            LandmarkRevealPresenter landmarkReveal = root
                .GetComponentInChildren<LandmarkRevealPresenter>(true);
            if (landmarkReveal == null)
            {
                throw new InvalidOperationException(
                    "Audio clip setup requires an existing " +
                    "LandmarkRevealPresenter in the scene.");
            }

            AudioSource landmarkRevealAudioSource = EnsureOneShotSource(
                landmarkReveal.transform,
                "LandmarkRevealAudioSource");
            landmarkReveal.ConfigureAudio(
                landmarkRevealAudioSource,
                // Optional/new -- stays null (and safely silent) until the
                // matching file is added.
                TryLoadClip("SFX_LandmarkPhotoReveal.wav"));

            SettingsPanelPresenter settingsPanel = root
                .GetComponentInChildren<SettingsPanelPresenter>(true);
            settingsPanel?.ConfigurePreLevelIntro(preLevelIntro);
            settingsPanel?.ConfigureLandmarkReveal(landmarkReveal);

            EditorUtility.SetDirty(feedbackAudio);
            EditorUtility.SetDirty(musicSource);
            EditorUtility.SetDirty(barrierLoopSource);
            EditorUtility.SetDirty(gravityWellLoopSource);
            EditorUtility.SetDirty(preLevelIntro);
            EditorUtility.SetDirty(preLevelIntroAudioSource);
            EditorUtility.SetDirty(landmarkReveal);
            EditorUtility.SetDirty(landmarkRevealAudioSource);
            if (settingsPanel != null)
            {
                EditorUtility.SetDirty(settingsPanel);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the audio clip setup.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Audio clips wired: 17 gameplay/UI SFX clips (Out Of Lives " +
                "shares Out Of Cuts), background music, the sustained " +
                "barrier-grow/gravity-well loop sources, and the (optional, " +
                "may still be silent) pre-level intro and landmark-reveal " +
                "cues.");
        }

        private static AudioSource EnsureLoopSource(
            Transform parent,
            string name)
        {
            AudioSource source = EnsureOneShotSource(parent, name);
            source.loop = true;
            return source;
        }

        private static AudioSource EnsureOneShotSource(
            Transform parent,
            string name)
        {
            GameObject sourceObject = GetOrCreateChild(parent, name);
            AudioSource source = GetOrAddComponent<AudioSource>(sourceObject);
            source.playOnAwake = false;
            source.loop = false;
            source.mute = false;
            source.spatialBlend = 0f;
            return source;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null
                ? component
                : gameObject.AddComponent<T>();
        }

        private static AudioClip RequireClip(string fileName)
        {
            string path = SoundsFolder + fileName;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            return clip != null
                ? clip
                : throw new InvalidOperationException(
                    $"Missing required audio asset at '{path}'.");
        }

        private static AudioClip TryLoadClip(string fileName) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>(SoundsFolder + fileName);

        private static void Validate(
            FeedbackAudioPresenter feedbackAudio,
            AudioSource musicSource,
            AudioSource barrierLoopSource,
            AudioSource gravityWellLoopSource,
            FeedbackAudioClipSet clips,
            AudioClip musicClip)
        {
            if (feedbackAudio.BarrierLoopSource != barrierLoopSource
                || feedbackAudio.GravityWellLoopSource != gravityWellLoopSource
                || !barrierLoopSource.loop
                || barrierLoopSource.playOnAwake
                || barrierLoopSource.mute
                || !gravityWellLoopSource.loop
                || gravityWellLoopSource.playOnAwake
                || gravityWellLoopSource.mute
                || feedbackAudio.StartClip != clips.StartClip
                || feedbackAudio.GrowClip != clips.GrowClip
                || feedbackAudio.LockClip != clips.LockClip
                || feedbackAudio.BreakClip != clips.BreakClip
                || feedbackAudio.FillClip != clips.FillClip
                || feedbackAudio.SandFillClip != clips.SandFillClip
                || feedbackAudio.LargeCaptureClip != clips.LargeCaptureClip
                || feedbackAudio.NearMissClip != clips.NearMissClip
                || feedbackAudio.ComboClip != clips.ComboClip
                || feedbackAudio.CompleteClip != clips.CompleteClip
                || feedbackAudio.UiClip != clips.UiClip
                || feedbackAudio.PowerFreezeActivateClip
                    != clips.PowerFreezeActivateClip
                || feedbackAudio.PowerInstantBarrierArmClip
                    != clips.PowerInstantBarrierArmClip
                || feedbackAudio.PowerInstantBarrierConsumeClip
                    != clips.PowerInstantBarrierConsumeClip
                || feedbackAudio.PowerGravityWellActivateClip
                    != clips.PowerGravityWellActivateClip
                || feedbackAudio.PowerUnavailableClip
                    != clips.PowerUnavailableClip
                || feedbackAudio.HunterReactClip != clips.HunterReactClip
                || feedbackAudio.OutOfCutsClip != clips.OutOfCutsClip
                || feedbackAudio.OutOfLivesClip != clips.OutOfLivesClip
                || feedbackAudio.OutOfLivesClip != feedbackAudio.OutOfCutsClip
                || musicSource.clip != musicClip)
            {
                throw new InvalidOperationException(
                    "One or more audio clips did not bind correctly.");
            }
        }

        private static Scene OpenVerticalSliceScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path == ScenePath)
            {
                return scene;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException(
                    "Audio clip setup cancelled before opening the scene.");
            }

            return EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == name)
                {
                    return rootObject;
                }
            }

            throw new InvalidOperationException(
                $"Scene does not contain required root '{name}'.");
        }
    }
}
