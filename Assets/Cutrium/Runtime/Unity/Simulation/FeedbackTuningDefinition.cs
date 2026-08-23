using System;
using Cutrium.Gameplay.Feedback;
using UnityEngine;

namespace Cutrium.Unity.Simulation
{
    [CreateAssetMenu(
        fileName = "FeedbackTuning",
        menuName = "Cutrium/Feedback Tuning")]
    public sealed class FeedbackTuningDefinition : ScriptableObject
    {
        [Header("Logical Evaluation")]
        [SerializeField]
        [Min(0.01f)]
        private float _nearMissDistance = 0.45f;

        [SerializeField]
        [Min(0.01f)]
        private float _nearMissWindowSeconds = 0.75f;

        [SerializeField]
        [Range(0.01f, 1f)]
        private float _largeCaptureFraction = 0.2f;

        [SerializeField]
        [Min(0.01f)]
        private float _comboTimeoutSeconds = 3f;

        [Header("Presentation Timing")]
        [SerializeField]
        [Min(0f)]
        private float _captureRevealSeconds = 0.18f;

        [SerializeField]
        [Min(0f)]
        private float _percentageAnimationSeconds = 0.22f;

        [SerializeField]
        [Min(0f)]
        private float _labelSeconds = 0.65f;

        public float NearMissDistance => _nearMissDistance;

        public float NearMissWindowSeconds => _nearMissWindowSeconds;

        public float LargeCaptureFraction => _largeCaptureFraction;

        public float ComboTimeoutSeconds => _comboTimeoutSeconds;

        public float CaptureRevealSeconds => _captureRevealSeconds;

        public float PercentageAnimationSeconds =>
            _percentageAnimationSeconds;

        public float LabelSeconds => _labelSeconds;

        public FeedbackTuningConfiguration ToRuntimeConfiguration() =>
            new FeedbackTuningConfiguration(
                _nearMissDistance,
                _nearMissWindowSeconds,
                _largeCaptureFraction,
                _comboTimeoutSeconds);

        public void ConfigureForSetup(
            float nearMissDistance,
            float nearMissWindowSeconds,
            float largeCaptureFraction,
            float comboTimeoutSeconds,
            float captureRevealSeconds,
            float percentageAnimationSeconds,
            float labelSeconds)
        {
            _nearMissDistance = nearMissDistance;
            _nearMissWindowSeconds = nearMissWindowSeconds;
            _largeCaptureFraction = largeCaptureFraction;
            _comboTimeoutSeconds = ValidateNonNegative(
                comboTimeoutSeconds,
                nameof(comboTimeoutSeconds));
            _captureRevealSeconds = ValidateNonNegative(
                captureRevealSeconds,
                nameof(captureRevealSeconds));
            _percentageAnimationSeconds = ValidateNonNegative(
                percentageAnimationSeconds,
                nameof(percentageAnimationSeconds));
            _labelSeconds = ValidateNonNegative(
                labelSeconds,
                nameof(labelSeconds));
            _ = ToRuntimeConfiguration();
        }

        private static float ValidateNonNegative(float value, string name)
        {
            if (float.IsNaN(value)
                || float.IsInfinity(value)
                || value < 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }

            return value;
        }
    }
}
