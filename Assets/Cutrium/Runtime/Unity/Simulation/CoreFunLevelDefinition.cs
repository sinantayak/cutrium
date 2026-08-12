using System;
using System.Collections.Generic;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Gameplay.Threats;
using UnityEngine;

namespace Cutrium.Unity.Simulation
{
    public enum CoreFunThreatBehaviorKind
    {
        Normal = 0,
        Hunter = 1,
        Pulse = 2
    }

    [Serializable]
    public sealed class CoreFunThreatBehaviorDefinition
    {
        [SerializeField]
        private CoreFunThreatBehaviorKind _kind = CoreFunThreatBehaviorKind.Normal;

        [Header("Hunter")]
        [SerializeField]
        [Range(0.01f, 1f)]
        private float _hunterSteerFactor = 0.25f;

        [Header("Pulse")]
        [SerializeField]
        private float _pulseSlowSpeedMultiplier = 0.5f;

        [SerializeField]
        private float _pulseFastSpeedMultiplier = 1.5f;

        [SerializeField]
        private float _pulseSlowDurationSeconds = 1.2f;

        [SerializeField]
        private float _pulseFastDurationSeconds = 0.8f;

        public CoreFunThreatBehaviorDefinition()
        {
        }

        public CoreFunThreatBehaviorDefinition(float hunterSteerFactor)
        {
            _kind = CoreFunThreatBehaviorKind.Hunter;
            _hunterSteerFactor = hunterSteerFactor;
        }

        public CoreFunThreatBehaviorDefinition(
            float pulseSlowSpeedMultiplier,
            float pulseFastSpeedMultiplier,
            float pulseSlowDurationSeconds,
            float pulseFastDurationSeconds)
        {
            _kind = CoreFunThreatBehaviorKind.Pulse;
            _pulseSlowSpeedMultiplier = pulseSlowSpeedMultiplier;
            _pulseFastSpeedMultiplier = pulseFastSpeedMultiplier;
            _pulseSlowDurationSeconds = pulseSlowDurationSeconds;
            _pulseFastDurationSeconds = pulseFastDurationSeconds;
        }

        public CoreFunThreatBehaviorKind Kind => _kind;
        public float HunterSteerFactor => _hunterSteerFactor;
        public float PulseSlowSpeedMultiplier => _pulseSlowSpeedMultiplier;
        public float PulseFastSpeedMultiplier => _pulseFastSpeedMultiplier;
        public float PulseSlowDurationSeconds => _pulseSlowDurationSeconds;
        public float PulseFastDurationSeconds => _pulseFastDurationSeconds;

        public ThreatBehaviorConfiguration ToRuntimeConfiguration()
        {
            switch (_kind)
            {
                case CoreFunThreatBehaviorKind.Hunter:
                    return ThreatBehaviorConfiguration.CreateHunter(
                        _hunterSteerFactor);
                case CoreFunThreatBehaviorKind.Pulse:
                    return ThreatBehaviorConfiguration.CreatePulse(
                        _pulseSlowSpeedMultiplier,
                        _pulseFastSpeedMultiplier,
                        _pulseSlowDurationSeconds,
                        _pulseFastDurationSeconds);
                default:
                    return ThreatBehaviorConfiguration.Normal;
            }
        }
    }

    [Serializable]
    public sealed class CoreFunThreatDefinition
    {
        [SerializeField]
        private Vector2 _initialPosition;

        [SerializeField]
        private Vector2 _initialDirection;

        [SerializeField]
        private float _speed;

        [SerializeField]
        private float _radius;

        [SerializeField]
        private int _maximumImpactsPerTick;

        [SerializeField]
        private CoreFunThreatBehaviorDefinition _behavior;

        public CoreFunThreatDefinition(
            Vector2 initialPosition,
            Vector2 initialDirection,
            float speed,
            float radius,
            int maximumImpactsPerTick)
            : this(
                initialPosition,
                initialDirection,
                speed,
                radius,
                maximumImpactsPerTick,
                null)
        {
        }

        public CoreFunThreatDefinition(
            Vector2 initialPosition,
            Vector2 initialDirection,
            float speed,
            float radius,
            int maximumImpactsPerTick,
            CoreFunThreatBehaviorDefinition behavior)
        {
            _initialPosition = initialPosition;
            _initialDirection = initialDirection;
            _speed = speed;
            _radius = radius;
            _maximumImpactsPerTick = maximumImpactsPerTick;
            _behavior = behavior;
        }

        public Vector2 InitialPosition => _initialPosition;
        public Vector2 InitialDirection => _initialDirection;
        public float Speed => _speed;
        public float Radius => _radius;
        public int MaximumImpactsPerTick => _maximumImpactsPerTick;
        public CoreFunThreatBehaviorDefinition Behavior => _behavior;

        public ThreatMotionConfiguration ToRuntimeConfiguration() =>
            new ThreatMotionConfiguration(
                CoreFunLevelConfiguration.FixedBoardBounds,
                new LogicalPoint(_initialPosition.x, _initialPosition.y),
                new LogicalVector(_initialDirection.x, _initialDirection.y),
                _speed,
                _radius,
                _maximumImpactsPerTick,
                _behavior != null
                    ? _behavior.ToRuntimeConfiguration()
                    : ThreatBehaviorConfiguration.Normal);
    }

    [Serializable]
    public sealed class CoreFunPowerDefinition
    {
        [Header("Freeze Pulse")]
        [SerializeField]
        [Min(0)]
        private int _freezePulseCharges;

        [SerializeField]
        private float _freezePulseDurationSeconds = 3f;

        [SerializeField]
        [Range(0.01f, 0.99f)]
        private float _freezePulseSpeedMultiplier = 0.12f;

        [Header("Instant Barrier")]
        [SerializeField]
        [Min(0)]
        private int _instantBarrierCharges;

        [SerializeField]
        private float _instantBarrierGrowthSpeed = 600f;

        public CoreFunPowerDefinition()
        {
        }

        public CoreFunPowerDefinition(
            int freezePulseCharges,
            float freezePulseDurationSeconds,
            float freezePulseSpeedMultiplier,
            int instantBarrierCharges,
            float instantBarrierGrowthSpeed)
        {
            _freezePulseCharges = freezePulseCharges;
            _freezePulseDurationSeconds = freezePulseDurationSeconds;
            _freezePulseSpeedMultiplier = freezePulseSpeedMultiplier;
            _instantBarrierCharges = instantBarrierCharges;
            _instantBarrierGrowthSpeed = instantBarrierGrowthSpeed;
        }

        public int FreezePulseCharges => _freezePulseCharges;
        public float FreezePulseDurationSeconds => _freezePulseDurationSeconds;

        public float FreezePulseSpeedMultiplier =>
            _freezePulseSpeedMultiplier;

        public int InstantBarrierCharges => _instantBarrierCharges;
        public float InstantBarrierGrowthSpeed => _instantBarrierGrowthSpeed;

        public PowerConfiguration ToRuntimeConfiguration() =>
            new PowerConfiguration(
                _freezePulseCharges,
                _freezePulseDurationSeconds,
                _freezePulseSpeedMultiplier,
                _instantBarrierCharges,
                _instantBarrierGrowthSpeed);
    }

    [Serializable]
    public sealed class CoreFunLevelDefinition
    {
        [SerializeField]
        private string _stableId;

        [SerializeField]
        private int _displayNumber;

        [SerializeField]
        private CoreFunThreatDefinition[] _threats;

        [SerializeField]
        private float _targetCapturedFraction;

        [SerializeField]
        private float _barrierGrowthSpeed;

        [SerializeField]
        private float _barrierCollisionHalfWidth;

        [SerializeField]
        private float _minimumCutMargin;

        [SerializeField]
        private int _maximumBarrierSolverIterations;

        [SerializeField]
        private int _maximumCatchUpTicks;

        [SerializeField]
        private string _developmentNote;

        [SerializeField]
        private string _purposeLine;

        [SerializeField]
        private float _maximumExpectedCompletionSeconds;

        [SerializeField]
        private CoreFunPowerDefinition _power;

        [SerializeField]
        [TextArea(2, 4)]
        private string _intendedDecision;

        [SerializeField]
        [Range(1, 5)]
        private int _difficultyRating = 1;

        public CoreFunLevelDefinition(
            string stableId,
            int displayNumber,
            Vector2 initialPosition,
            Vector2 initialDirection,
            float threatSpeed,
            float threatRadius,
            float targetCapturedFraction,
            float barrierGrowthSpeed,
            float barrierCollisionHalfWidth,
            float minimumCutMargin,
            int maximumThreatImpactsPerTick,
            int maximumBarrierSolverIterations,
            int maximumCatchUpTicks,
            string developmentNote,
            float maximumExpectedCompletionSeconds)
            : this(
                stableId,
                displayNumber,
                new[]
                {
                    new CoreFunThreatDefinition(
                        initialPosition,
                        initialDirection,
                        threatSpeed,
                        threatRadius,
                        maximumThreatImpactsPerTick),
                },
                targetCapturedFraction,
                barrierGrowthSpeed,
                barrierCollisionHalfWidth,
                minimumCutMargin,
                maximumBarrierSolverIterations,
                maximumCatchUpTicks,
                developmentNote,
                maximumExpectedCompletionSeconds,
                string.Empty)
        {
        }

        public CoreFunLevelDefinition(
            string stableId,
            int displayNumber,
            IReadOnlyList<CoreFunThreatDefinition> threats,
            float targetCapturedFraction,
            float barrierGrowthSpeed,
            float barrierCollisionHalfWidth,
            float minimumCutMargin,
            int maximumBarrierSolverIterations,
            int maximumCatchUpTicks,
            string developmentNote,
            float maximumExpectedCompletionSeconds,
            string purposeLine)
            : this(
                stableId,
                displayNumber,
                threats,
                targetCapturedFraction,
                barrierGrowthSpeed,
                barrierCollisionHalfWidth,
                minimumCutMargin,
                maximumBarrierSolverIterations,
                maximumCatchUpTicks,
                developmentNote,
                maximumExpectedCompletionSeconds,
                purposeLine,
                null)
        {
        }

        public CoreFunLevelDefinition(
            string stableId,
            int displayNumber,
            IReadOnlyList<CoreFunThreatDefinition> threats,
            float targetCapturedFraction,
            float barrierGrowthSpeed,
            float barrierCollisionHalfWidth,
            float minimumCutMargin,
            int maximumBarrierSolverIterations,
            int maximumCatchUpTicks,
            string developmentNote,
            float maximumExpectedCompletionSeconds,
            string purposeLine,
            CoreFunPowerDefinition power,
            string intendedDecision = "",
            int difficultyRating = 1)
        {
            _stableId = stableId;
            _displayNumber = displayNumber;
            _threats = threats?.ToArray()
                ?? throw new ArgumentNullException(nameof(threats));
            _targetCapturedFraction = targetCapturedFraction;
            _barrierGrowthSpeed = barrierGrowthSpeed;
            _barrierCollisionHalfWidth = barrierCollisionHalfWidth;
            _minimumCutMargin = minimumCutMargin;
            _maximumBarrierSolverIterations = maximumBarrierSolverIterations;
            _maximumCatchUpTicks = maximumCatchUpTicks;
            _developmentNote = developmentNote;
            _purposeLine = purposeLine;
            _maximumExpectedCompletionSeconds =
                maximumExpectedCompletionSeconds;
            _power = power;
            _intendedDecision = intendedDecision;
            _difficultyRating = difficultyRating;
        }

        public string StableId => _stableId;
        public int DisplayNumber => _displayNumber;
        public IReadOnlyList<CoreFunThreatDefinition> Threats => _threats;
        public Vector2 InitialPosition => _threats[0].InitialPosition;
        public Vector2 InitialDirection => _threats[0].InitialDirection;
        public float ThreatSpeed => _threats[0].Speed;
        public float ThreatRadius => _threats[0].Radius;
        public float TargetCapturedFraction => _targetCapturedFraction;
        public float BarrierGrowthSpeed => _barrierGrowthSpeed;
        public float BarrierCollisionHalfWidth => _barrierCollisionHalfWidth;
        public float MinimumCutMargin => _minimumCutMargin;
        public int MaximumThreatImpactsPerTick =>
            _threats[0].MaximumImpactsPerTick;
        public int MaximumBarrierSolverIterations =>
            _maximumBarrierSolverIterations;
        public int MaximumCatchUpTicks => _maximumCatchUpTicks;
        public string DevelopmentNote => _developmentNote;
        public string PurposeLine => _purposeLine;
        public float MaximumExpectedCompletionSeconds =>
            _maximumExpectedCompletionSeconds;
        public float ExpectedHumanCompletionSeconds =>
            _maximumExpectedCompletionSeconds;
        public CoreFunPowerDefinition Power => _power;
        public string IntendedDecision => _intendedDecision;
        public int DifficultyRating => _difficultyRating;

        public CoreFunLevelConfiguration ToRuntimeConfiguration()
        {
            if (_threats == null || _threats.Length == 0)
            {
                throw new InvalidOperationException(
                    "A serialized level needs at least one normal threat.");
            }

            ThreatMotionConfiguration[] threats = _threats
                .Select(threat => threat?.ToRuntimeConfiguration()
                    ?? throw new InvalidOperationException(
                        "Serialized threat definitions cannot be null."))
                .ToArray();
            var barrier = new BarrierConfiguration(
                _barrierGrowthSpeed,
                _barrierCollisionHalfWidth,
                _minimumCutMargin,
                _maximumBarrierSolverIterations);
            var capture = new CaptureLevelConfiguration(
                _targetCapturedFraction);
            return new CoreFunLevelConfiguration(
                _stableId,
                _displayNumber,
                threats,
                barrier,
                capture,
                _maximumCatchUpTicks,
                _developmentNote,
                _maximumExpectedCompletionSeconds,
                _purposeLine,
                _power != null
                    ? _power.ToRuntimeConfiguration()
                    : PowerConfiguration.None,
                _intendedDecision,
                _difficultyRating);
        }

        public static CoreFunLevelDefinition[] CreateMilestone3Defaults() =>
            new[]
            {
                new CoreFunLevelDefinition(
                    "learn-the-cut",
                    1,
                    new[]
                    {
                        new CoreFunThreatDefinition(
                            new Vector2(5f, 8f),
                            new Vector2(0.8f, 0.6f),
                            1.6f,
                            0.35f,
                            8),
                    },
                    0.825f,
                    3f,
                    0.08f,
                    3f,
                    16,
                    8,
                    "Two or more readable cuts teach empty-side capture.",
                    15f,
                    "LEARN THE CUT"),
                new CoreFunLevelDefinition(
                    "timing-and-failure",
                    2,
                    new[]
                    {
                        new CoreFunThreatDefinition(
                            new Vector2(4.5f, 3.5f),
                            new Vector2(0.45f, 0.89f),
                            3.1f,
                            0.38f,
                            8),
                    },
                    0.85f,
                    2.4f,
                    0.08f,
                    2.5f,
                    16,
                    8,
                    "A crossing trajectory makes careless growth timing break.",
                    30f,
                    "WATCH THE THREAT"),
                new CoreFunLevelDefinition(
                    "confident-capture",
                    3,
                    new[]
                    {
                        new CoreFunThreatDefinition(
                            new Vector2(3f, 5f),
                            new Vector2(0.9f, 0.44f),
                            2.7f,
                            0.35f,
                            8),
                        new CoreFunThreatDefinition(
                            new Vector2(7f, 11f),
                            new Vector2(-0.82f, -0.57f),
                            2.9f,
                            0.35f,
                            8),
                    },
                    0.9f,
                    2.8f,
                    0.08f,
                    1.8f,
                    16,
                    8,
                    "Separated threats make grouping the strategic constraint.",
                    45f,
                    "KEEP THEM TOGETHER"),
            };

        public static CoreFunLevelDefinition[] CreateMilestone6Defaults() =>
            new[]
            {
                new CoreFunLevelDefinition(
                    "hunter-alone",
                    1,
                    new[]
                    {
                        new CoreFunThreatDefinition(
                            new Vector2(5f, 8f),
                            new Vector2(0.8f, 0.6f),
                            2f,
                            0.35f,
                            8,
                            new CoreFunThreatBehaviorDefinition(0.3f)),
                    },
                    0.8f,
                    2.6f,
                    0.08f,
                    3f,
                    16,
                    8,
                    "Hunter reacts once per barrier start; verify fairness " +
                    "and readable, bounded steering.",
                    30f,
                    "OUTSMART THE HUNTER",
                    null),
                new CoreFunLevelDefinition(
                    "pulse-alone",
                    2,
                    new[]
                    {
                        new CoreFunThreatDefinition(
                            new Vector2(4f, 10f),
                            new Vector2(0.6f, -0.8f),
                            2f,
                            0.35f,
                            8,
                            new CoreFunThreatBehaviorDefinition(
                                0.5f,
                                1.5f,
                                1.2f,
                                0.8f)),
                    },
                    0.8f,
                    2.6f,
                    0.08f,
                    3f,
                    16,
                    8,
                    "Pulse cycles slow/fast speed; verify peak-speed solver " +
                    "reliability and readable timing.",
                    30f,
                    "FEEL THE PULSE",
                    null),
                new CoreFunLevelDefinition(
                    "freeze-pulse-rescue",
                    3,
                    new[]
                    {
                        new CoreFunThreatDefinition(
                            new Vector2(5f, 8f),
                            new Vector2(0.7f, 0.71f),
                            2.4f,
                            0.35f,
                            8),
                    },
                    0.8f,
                    2.4f,
                    0.08f,
                    3f,
                    16,
                    8,
                    "One Freeze Pulse charge should rescue a risky cut " +
                    "without a permanent stuck threat.",
                    30f,
                    "FREEZE AND CUT",
                    new CoreFunPowerDefinition(1, 3f, 0.12f, 0, 600f)),
                new CoreFunLevelDefinition(
                    "instant-barrier-finish",
                    4,
                    new[]
                    {
                        new CoreFunThreatDefinition(
                            new Vector2(5f, 8f),
                            new Vector2(0.5f, 0.87f),
                            2.4f,
                            0.35f,
                            8),
                    },
                    0.8f,
                    2.4f,
                    0.08f,
                    3f,
                    16,
                    8,
                    "One Instant Barrier charge should complete within the " +
                    "same tick without changing lock/contact rules.",
                    30f,
                    "END IT INSTANTLY",
                    new CoreFunPowerDefinition(0, 3f, 0.12f, 1, 600f)),
                new CoreFunLevelDefinition(
                    "identity-mix",
                    5,
                    new[]
                    {
                        new CoreFunThreatDefinition(
                            new Vector2(3f, 5f),
                            new Vector2(0.9f, 0.44f),
                            2f,
                            0.35f,
                            8,
                            new CoreFunThreatBehaviorDefinition(0.25f)),
                        new CoreFunThreatDefinition(
                            new Vector2(7f, 11f),
                            new Vector2(-0.82f, -0.57f),
                            2f,
                            0.35f,
                            8,
                            new CoreFunThreatBehaviorDefinition(
                                0.5f,
                                1.5f,
                                1.2f,
                                0.8f)),
                    },
                    0.85f,
                    2.6f,
                    0.08f,
                    3f,
                    16,
                    8,
                    "Combines Hunter, Pulse, Freeze Pulse, and Instant " +
                    "Barrier for one identity-test pass.",
                    45f,
                    "CUTRIUM IDENTITY TEST",
                    new CoreFunPowerDefinition(1, 3f, 0.12f, 1, 600f)),
            };
    }
}
