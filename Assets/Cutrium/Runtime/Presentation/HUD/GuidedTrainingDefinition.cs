using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Geometry;
using UnityEngine;

namespace Cutrium.Presentation.HUD
{
    public enum GuidedTrainingStepKind
    {
        Observe = 0,
        Info = 1,
        Action = 2,
    }

    public enum GuidedTrainingActionKind
    {
        None = 0,
        HorizontalBarrier = 1,
        VerticalBarrier = 2,
        FreezePulse = 3,
        InstantBarrier = 4,
        GravityWell = 5,
        FreeBarrier = 6,
    }

    public enum GuidedTrainingHandMotion
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        Pulse = 3,
    }

    public enum GuidedTrainingFocusTarget
    {
        None = 0,
        Progress = 1,
        FreezePulse = 2,
        InstantBarrier = 3,
        GravityWell = 4,
        BarrierSpeed = 5,
        Lives = 6,
    }

    public enum GuidedTrainingCompletionGate
    {
        None = 0,
        ProgressSettled = 1,
    }

    [Serializable]
    public sealed class GuidedTrainingStep
    {
        [SerializeField]
        private GuidedTrainingStepKind _stepKind = GuidedTrainingStepKind.Action;

        [SerializeField]
        private GuidedTrainingActionKind _action;

        [SerializeField]
        private GuidedTrainingHandMotion _handMotion;

        [SerializeField]
        private bool _hasFixedOrigin;

        [SerializeField]
        private float _fixedOriginX;

        [SerializeField]
        private float _fixedOriginY;

        [SerializeField]
        private bool _freeze = true;

        [SerializeField]
        private bool _requiresLevelCompletion;

        [SerializeField]
        [TextArea(1, 3)]
        private string _promptEnglish = string.Empty;

        [SerializeField]
        [TextArea(1, 3)]
        private string _promptTurkish = string.Empty;

        [SerializeField]
        [TextArea(1, 3)]
        private string _resolvingEnglish = string.Empty;

        [SerializeField]
        [TextArea(1, 3)]
        private string _resolvingTurkish = string.Empty;

        [SerializeField]
        [TextArea(1, 3)]
        private string _successEnglish = string.Empty;

        [SerializeField]
        [TextArea(1, 3)]
        private string _successTurkish = string.Empty;

        [SerializeField]
        private GuidedTrainingFocusTarget _promptFocus;

        [SerializeField]
        private GuidedTrainingFocusTarget _successFocus;

        [SerializeField]
        private GuidedTrainingCompletionGate _completionGate;

        [SerializeField]
        [Min(0f)]
        private float _successSeconds = 1.25f;

        [SerializeField]
        [Min(0f)]
        private float _durationSeconds = 1.5f;

        public GuidedTrainingStepKind StepKind => _stepKind;
        public GuidedTrainingActionKind Action => _action;
        public GuidedTrainingHandMotion HandMotion => _handMotion;
        public LogicalPoint? FixedOrigin => _hasFixedOrigin
            ? new LogicalPoint(_fixedOriginX, _fixedOriginY)
            : (LogicalPoint?)null;
        public bool Freeze => _freeze;
        public bool RequiresLevelCompletion => _requiresLevelCompletion;
        public string PromptEnglish => _promptEnglish;
        public string PromptTurkish => _promptTurkish;
        public string ResolvingEnglish => _resolvingEnglish;
        public string ResolvingTurkish => _resolvingTurkish;
        public string SuccessEnglish => _successEnglish;
        public string SuccessTurkish => _successTurkish;
        public GuidedTrainingFocusTarget PromptFocus => _promptFocus;
        public GuidedTrainingFocusTarget SuccessFocus => _successFocus;
        public GuidedTrainingCompletionGate CompletionGate =>
            _completionGate;
        public float SuccessSeconds => _successSeconds;
        public float DurationSeconds => _durationSeconds;

        private GuidedTrainingStep()
        {
        }

        public static GuidedTrainingStep Observe(
            string promptEnglish,
            string promptTurkish,
            float durationSeconds)
        {
            ValidateCopy(promptEnglish, nameof(promptEnglish));
            ValidateCopy(promptTurkish, nameof(promptTurkish));
            ValidatePositive(durationSeconds, nameof(durationSeconds));

            return new GuidedTrainingStep
            {
                _stepKind = GuidedTrainingStepKind.Observe,
                _action = GuidedTrainingActionKind.None,
                _handMotion = GuidedTrainingHandMotion.None,
                _freeze = false,
                _promptEnglish = promptEnglish,
                _promptTurkish = promptTurkish,
                _promptFocus = GuidedTrainingFocusTarget.None,
                _durationSeconds = durationSeconds,
            };
        }

        /// <summary>
        /// An informational beat: frozen, highlights <paramref name="focus"/>,
        /// and waits for the player to tap anywhere to continue (see
        /// <see cref="GuidedTrainingPresenter"/>'s point-targeting use)
        /// rather than a timer, so nobody is rushed past it.
        /// </summary>
        public static GuidedTrainingStep Info(
            string promptEnglish,
            string promptTurkish,
            GuidedTrainingFocusTarget focus)
        {
            ValidateCopy(promptEnglish, nameof(promptEnglish));
            ValidateCopy(promptTurkish, nameof(promptTurkish));

            return new GuidedTrainingStep
            {
                _stepKind = GuidedTrainingStepKind.Info,
                _action = GuidedTrainingActionKind.None,
                _handMotion = GuidedTrainingHandMotion.None,
                _freeze = true,
                _promptEnglish = promptEnglish,
                _promptTurkish = promptTurkish,
                _promptFocus = focus,
                _durationSeconds = 1f,
            };
        }

        public static GuidedTrainingStep ActionStep(
            GuidedTrainingActionKind action,
            GuidedTrainingHandMotion handMotion,
            LogicalPoint? fixedOrigin,
            string promptEnglish,
            string promptTurkish,
            string resolvingEnglish,
            string resolvingTurkish,
            string successEnglish,
            string successTurkish,
            GuidedTrainingFocusTarget successFocus,
            GuidedTrainingCompletionGate completionGate,
            float successSeconds,
            bool freeze = true,
            bool requiresLevelCompletion = false,
            GuidedTrainingFocusTarget promptFocus = GuidedTrainingFocusTarget.None)
        {
            if (action == GuidedTrainingActionKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(action));
            }

            ValidateCopy(promptEnglish, nameof(promptEnglish));
            ValidateCopy(promptTurkish, nameof(promptTurkish));
            ValidateCopy(resolvingEnglish, nameof(resolvingEnglish));
            ValidateCopy(resolvingTurkish, nameof(resolvingTurkish));
            if (!requiresLevelCompletion)
            {
                ValidateCopy(successEnglish, nameof(successEnglish));
                ValidateCopy(successTurkish, nameof(successTurkish));
            }

            ValidateNonNegative(successSeconds, nameof(successSeconds));

            return new GuidedTrainingStep
            {
                _stepKind = GuidedTrainingStepKind.Action,
                _action = action,
                _handMotion = handMotion,
                _hasFixedOrigin = fixedOrigin.HasValue,
                _fixedOriginX = fixedOrigin?.X ?? 0f,
                _fixedOriginY = fixedOrigin?.Y ?? 0f,
                _freeze = freeze,
                _requiresLevelCompletion = requiresLevelCompletion,
                _promptEnglish = promptEnglish,
                _promptTurkish = promptTurkish,
                _resolvingEnglish = resolvingEnglish,
                _resolvingTurkish = resolvingTurkish,
                _successEnglish = successEnglish ?? string.Empty,
                _successTurkish = successTurkish ?? string.Empty,
                _promptFocus = promptFocus,
                _successFocus = successFocus,
                _completionGate = completionGate,
                _successSeconds = successSeconds,
            };
        }

        public string Prompt(bool turkish) =>
            turkish ? _promptTurkish : _promptEnglish;

        public string Resolving(bool turkish) =>
            turkish ? _resolvingTurkish : _resolvingEnglish;

        public string Success(bool turkish) =>
            turkish ? _successTurkish : _successEnglish;

        public void Validate()
        {
            ValidateCopy(_promptEnglish, nameof(_promptEnglish));
            ValidateCopy(_promptTurkish, nameof(_promptTurkish));

            if (_stepKind == GuidedTrainingStepKind.Action)
            {
                if (_action == GuidedTrainingActionKind.None)
                {
                    throw new InvalidOperationException(
                        "A guided-training action step needs an action.");
                }

                ValidateCopy(_resolvingEnglish, nameof(_resolvingEnglish));
                ValidateCopy(_resolvingTurkish, nameof(_resolvingTurkish));
                if (!_requiresLevelCompletion)
                {
                    ValidateCopy(_successEnglish, nameof(_successEnglish));
                    ValidateCopy(_successTurkish, nameof(_successTurkish));
                }

                ValidateNonNegative(_successSeconds, nameof(_successSeconds));
            }
            else
            {
                if (_action != GuidedTrainingActionKind.None)
                {
                    throw new InvalidOperationException(
                        "Observe/Info steps cannot declare an action.");
                }

                ValidatePositive(_durationSeconds, nameof(_durationSeconds));
            }
        }

        private static void ValidateCopy(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Guided-training copy cannot be empty.",
                    name);
            }
        }

        private static void ValidateNonNegative(float value, string name)
        {
            if (float.IsNaN(value)
                || float.IsInfinity(value)
                || value < 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static void ValidatePositive(float value, string name)
        {
            if (float.IsNaN(value)
                || float.IsInfinity(value)
                || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }

    [CreateAssetMenu(
        fileName = "GuidedTrainingDefinition",
        menuName = "Cutrium/Training/Guided Training Definition")]
    public sealed class GuidedTrainingDefinition : ScriptableObject
    {
        [SerializeField]
        private string _stableLevelId = string.Empty;

        [SerializeField]
        private GuidedTrainingStep[] _steps =
            Array.Empty<GuidedTrainingStep>();

        public string StableLevelId => _stableLevelId;
        public IReadOnlyList<GuidedTrainingStep> Steps => _steps;

        public void ConfigureForSetup(
            string stableLevelId,
            IReadOnlyList<GuidedTrainingStep> steps)
        {
            if (string.IsNullOrWhiteSpace(stableLevelId))
            {
                throw new ArgumentException(
                    "A guided-training definition needs a stable level ID.",
                    nameof(stableLevelId));
            }

            if (steps == null || steps.Count == 0)
            {
                throw new ArgumentException(
                    "A guided-training definition needs at least one step.",
                    nameof(steps));
            }

            var copy = new GuidedTrainingStep[steps.Count];
            for (int index = 0; index < steps.Count; index++)
            {
                GuidedTrainingStep step = steps[index]
                    ?? throw new ArgumentException(
                        "Training steps cannot contain null entries.",
                        nameof(steps));
                step.Validate();
                copy[index] = step;
            }

            _stableLevelId = stableLevelId;
            _steps = copy;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(_stableLevelId))
            {
                throw new InvalidOperationException(
                    "A guided-training definition needs a stable level ID.");
            }

            if (_steps == null || _steps.Length == 0)
            {
                throw new InvalidOperationException(
                    "A guided-training definition needs at least one step.");
            }

            for (int index = 0; index < _steps.Length; index++)
            {
                if (_steps[index] == null)
                {
                    throw new InvalidOperationException(
                        "Training steps cannot contain null entries.");
                }

                _steps[index].Validate();
            }
        }
    }
}
