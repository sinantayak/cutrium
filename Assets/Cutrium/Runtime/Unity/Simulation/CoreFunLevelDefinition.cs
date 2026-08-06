using System;
using System.Collections.Generic;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using UnityEngine;

namespace Cutrium.Unity.Simulation
{
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

        public CoreFunThreatDefinition(
            Vector2 initialPosition,
            Vector2 initialDirection,
            float speed,
            float radius,
            int maximumImpactsPerTick)
        {
            _initialPosition = initialPosition;
            _initialDirection = initialDirection;
            _speed = speed;
            _radius = radius;
            _maximumImpactsPerTick = maximumImpactsPerTick;
        }

        public Vector2 InitialPosition => _initialPosition;
        public Vector2 InitialDirection => _initialDirection;
        public float Speed => _speed;
        public float Radius => _radius;
        public int MaximumImpactsPerTick => _maximumImpactsPerTick;

        public ThreatMotionConfiguration ToRuntimeConfiguration() =>
            new ThreatMotionConfiguration(
                CoreFunLevelConfiguration.FixedBoardBounds,
                new LogicalPoint(_initialPosition.x, _initialPosition.y),
                new LogicalVector(_initialDirection.x, _initialDirection.y),
                _speed,
                _radius,
                _maximumImpactsPerTick);
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
                _purposeLine);
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
    }
}
