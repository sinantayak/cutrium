using Cutrium.Unity.Simulation;
using UnityEditor;
using UnityEngine;

namespace Cutrium.Editor
{
    public sealed class GameplayLevelNavigatorWindow : EditorWindow
    {
        private int _jumpLevel = 1;

        [MenuItem("Cutrium/Playtest/Level Navigator")]
        public static void Open()
        {
            var window = GetWindow<GameplayLevelNavigatorWindow>();
            window.titleContent = new GUIContent("Level Navigator");
            window.minSize = new Vector2(390f, 285f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "First 12 Gameplay Review",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Editor-only controls. They reload the active level inside " +
                "the persistent gameplay scene and never add player HUD.",
                MessageType.Info);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode in VerticalSlice, then use these controls.",
                    MessageType.None);
                return;
            }

            FirstPlayableController controller = FindController();
            if (controller == null)
            {
                EditorGUILayout.HelpBox(
                    "No active FirstPlayableController was found.",
                    MessageType.Warning);
                return;
            }

            CoreFunLevelDefinition definition =
                controller.LevelDefinitions[controller.CurrentLevelIndex];
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                $"Level {controller.CurrentLevelNumber}/{controller.LevelCount}: " +
                controller.CurrentLevelId,
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Purpose", definition.DevelopmentNote);
            EditorGUILayout.LabelField(
                "Decision",
                definition.IntendedDecision,
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField(
                "Target",
                $"{definition.TargetCapturedFraction:P0}");
            EditorGUILayout.LabelField(
                "Expected / Difficulty",
                $"{definition.ExpectedHumanCompletionSeconds:0}s / " +
                $"{definition.DifficultyRating} of 5");

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = controller.CurrentLevelIndex > 0;
                if (GUILayout.Button("Previous"))
                {
                    controller.TryGoToPreviousLevelForDevelopment();
                }

                GUI.enabled = true;
                if (GUILayout.Button("Retry"))
                {
                    controller.RetryLevel();
                }

                GUI.enabled = controller.CurrentLevelIndex + 1
                    < controller.LevelCount;
                if (GUILayout.Button("Next"))
                {
                    controller.TryGoToNextLevelForDevelopment();
                }

                GUI.enabled = true;
                if (GUILayout.Button("Reset Sequence"))
                {
                    controller.RestartSequence();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _jumpLevel = EditorGUILayout.IntSlider(
                    "Jump",
                    _jumpLevel,
                    1,
                    controller.LevelCount);
                if (GUILayout.Button("Go", GUILayout.Width(55f)))
                {
                    controller.TryJumpToLevelForDevelopment(_jumpLevel);
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(
                "Power Review",
                EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = controller.FreezePulseChargesRemaining > 0;
                if (GUILayout.Button(
                        $"Freeze ({controller.FreezePulseChargesRemaining})"))
                {
                    controller.TryActivateFreezePulse();
                }

                GUI.enabled = controller.InstantBarrierChargesRemaining > 0
                    && !controller.InstantBarrierArmed;
                if (GUILayout.Button(
                        $"Instant ({controller.InstantBarrierChargesRemaining})"))
                {
                    controller.TryArmInstantBarrier();
                }

                GUI.enabled = true;
            }
        }

        private static FirstPlayableController FindController()
        {
            FirstPlayableController[] controllers =
                Object.FindObjectsByType<FirstPlayableController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            return controllers.Length > 0 ? controllers[0] : null;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Repaint();
        }
    }
}
