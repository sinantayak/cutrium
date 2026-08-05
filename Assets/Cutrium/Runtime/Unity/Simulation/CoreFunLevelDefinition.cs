using System;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using UnityEngine;

namespace Cutrium.Unity.Simulation
{
    [Serializable]
    public sealed class CoreFunLevelDefinition
    {
        [SerializeField]
        private string _stableId;

        [SerializeField]
        private int _displayNumber;

        [SerializeField]
        private Vector2 _initialPosition;

        [SerializeField]
        private Vector2 _initialDirection;

        [SerializeField]
        private float _threatSpeed;

        [SerializeField]
        private float _threatRadius;

        [SerializeField]
        private float _targetCapturedFraction;

        [SerializeField]
        private float _barrierGrowthSpeed;

        [SerializeField]
        private float _barrierCollisionHalfWidth;

        [SerializeField]
        private float _minimumCutMargin;

        [SerializeField]
        private int _maximumThreatImpactsPerTick;

        [SerializeField]
        private int _maximumBarrierSolverIterations;

        [SerializeField]
        private int _maximumCatchUpTicks;

        [SerializeField]
        private string _developmentNote;

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
        {
            _stableId = stableId;
            _displayNumber = displayNumber;
            _initialPosition = initialPosition;
            _initialDirection = initialDirection;
            _threatSpeed = threatSpeed;
            _threatRadius = threatRadius;
            _targetCapturedFraction = targetCapturedFraction;
            _barrierGrowthSpeed = barrierGrowthSpeed;
            _barrierCollisionHalfWidth = barrierCollisionHalfWidth;
            _minimumCutMargin = minimumCutMargin;
            _maximumThreatImpactsPerTick = maximumThreatImpactsPerTick;
            _maximumBarrierSolverIterations = maximumBarrierSolverIterations;
            _maximumCatchUpTicks = maximumCatchUpTicks;
            _developmentNote = developmentNote;
            _maximumExpectedCompletionSeconds =
                maximumExpectedCompletionSeconds;
        }

        public string StableId => _stableId;
        public int DisplayNumber => _displayNumber;
        public Vector2 InitialPosition => _initialPosition;
        public Vector2 InitialDirection => _initialDirection;
        public float ThreatSpeed => _threatSpeed;
        public float ThreatRadius => _threatRadius;
        public float TargetCapturedFraction => _targetCapturedFraction;
        public float BarrierGrowthSpeed => _barrierGrowthSpeed;
        public float BarrierCollisionHalfWidth => _barrierCollisionHalfWidth;
        public float MinimumCutMargin => _minimumCutMargin;
        public int MaximumThreatImpactsPerTick =>
            _maximumThreatImpactsPerTick;
        public int MaximumBarrierSolverIterations =>
            _maximumBarrierSolverIterations;
        public int MaximumCatchUpTicks => _maximumCatchUpTicks;
        public string DevelopmentNote => _developmentNote;
        public float MaximumExpectedCompletionSeconds =>
            _maximumExpectedCompletionSeconds;

        public CoreFunLevelConfiguration ToRuntimeConfiguration()
        {
            var threat = new ThreatMotionConfiguration(
                CoreFunLevelConfiguration.FixedBoardBounds,
                new LogicalPoint(_initialPosition.x, _initialPosition.y),
                new LogicalVector(_initialDirection.x, _initialDirection.y),
                _threatSpeed,
                _threatRadius,
                _maximumThreatImpactsPerTick);
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
                threat,
                barrier,
                capture,
                _maximumCatchUpTicks,
                _developmentNote,
                _maximumExpectedCompletionSeconds);
        }

        public static CoreFunLevelDefinition[] CreateMilestone3Defaults() =>
            new[]
            {
                new CoreFunLevelDefinition(
                    "learn-the-cut",
                    1,
                    new Vector2(5f, 8f),
                    new Vector2(0.8f, 0.6f),
                    2.6f,
                    0.35f,
                    0.625f,
                    9.5f,
                    0.08f,
                    0.75f,
                    8,
                    16,
                    8,
                    "Readable safe cuts and a generous growth race.",
                    45f),
                new CoreFunLevelDefinition(
                    "timing-and-failure",
                    2,
                    new Vector2(6.5f, 5f),
                    new Vector2(-0.65f, 0.76f),
                    3.2f,
                    0.35f,
                    0.7f,
                    8f,
                    0.08f,
                    0.6f,
                    8,
                    16,
                    8,
                    "Slower barriers expose the vulnerable growth window.",
                    45f),
                new CoreFunLevelDefinition(
                    "confident-capture",
                    3,
                    new Vector2(5f, 8f),
                    new Vector2(0.92f, 0.38f),
                    3.6f,
                    0.35f,
                    0.75f,
                    7.5f,
                    0.08f,
                    0.8f,
                    8,
                    16,
                    8,
                    "Higher target and slower growth reward deliberate cuts.",
                    45f),
            };
    }
}
