using System;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using UnityEngine;

namespace Cutrium.Unity.Simulation
{
    [DisallowMultipleComponent]
    public sealed class FirstPlayableController : MonoBehaviour
    {
        public const float SimulationStep = 1f / 60f;

        [Header("Threat Motion")]
        [SerializeField]
        private Vector2 _initialPosition = new Vector2(5f, 8f);

        [SerializeField]
        private Vector2 _initialDirection = new Vector2(0.8f, 0.6f);

        [SerializeField]
        private float _threatSpeed = 3f;

        [SerializeField]
        private float _threatRadius = 0.35f;

        [SerializeField]
        private int _maximumImpactsPerTick = 8;

        [Header("Fixed Step")]
        [SerializeField]
        private int _maximumCatchUpTicks = 8;

        [Header("Geometry Tolerances")]
        [SerializeField]
        private float _distanceTolerance = 0.0001f;

        [SerializeField]
        private float _timeTolerance = 0.00001f;

        [SerializeField]
        private float _cornerTimeTolerance = 0.0001f;

        [SerializeField]
        private float _areaTolerance = 0.001f;

        private Action<float> _tickAction;
        private FixedStepAccumulator _accumulator;

        public ThreatMotionSession Session { get; private set; }

        public GeometryTolerancePolicy Tolerance { get; private set; }

        public LogicalRect BoardBounds =>
            Session != null
                ? Session.InitialRoom.Bounds
                : new LogicalRect(0f, 0f, 10f, 16f);

        public float ThreatRadius => _threatRadius;

        public float ThreatSpeed => _threatSpeed;

        public Vector2 InitialPosition => _initialPosition;

        public Vector2 InitialDirection => _initialDirection;

        public int MaximumCatchUpTicks => _maximumCatchUpTicks;

        public int InitializationCount { get; private set; }

        public int CappedCatchUpCount { get; private set; }

        public float DroppedSimulationTime { get; private set; }

        private void Awake()
        {
            InitializeOnce();
        }

        private void Update()
        {
            AdvanceSimulation(Time.deltaTime);
        }

        public FixedStepAdvanceResult AdvanceSimulation(float renderDeltaTime)
        {
            InitializeOnce();
            FixedStepAdvanceResult result = _accumulator.Advance(
                renderDeltaTime,
                _tickAction);
            if (result.WasCatchUpCapped)
            {
                CappedCatchUpCount++;
                DroppedSimulationTime += result.DroppedTime;
            }

            return result;
        }

        public void ConfigureForSetup(
            Vector2 initialPosition,
            Vector2 initialDirection,
            float threatSpeed,
            float threatRadius,
            int maximumImpactsPerTick,
            int maximumCatchUpTicks)
        {
            _initialPosition = initialPosition;
            _initialDirection = initialDirection;
            _threatSpeed = threatSpeed;
            _threatRadius = threatRadius;
            _maximumImpactsPerTick = maximumImpactsPerTick;
            _maximumCatchUpTicks = maximumCatchUpTicks;
        }

        private void InitializeOnce()
        {
            if (Session != null)
            {
                return;
            }

            Tolerance = new GeometryTolerancePolicy(
                _distanceTolerance,
                _timeTolerance,
                _cornerTimeTolerance,
                _areaTolerance);
            var configuration = new ThreatMotionConfiguration(
                new LogicalRect(0f, 0f, 10f, 16f),
                new LogicalPoint(_initialPosition.x, _initialPosition.y),
                new LogicalVector(_initialDirection.x, _initialDirection.y),
                _threatSpeed,
                _threatRadius,
                _maximumImpactsPerTick);
            Session = new ThreatMotionSession(configuration, Tolerance);
            _accumulator = new FixedStepAccumulator(
                SimulationStep,
                _maximumCatchUpTicks,
                Tolerance);
            _tickAction = TickSession;
            InitializationCount++;
        }

        private void TickSession(float elapsedTime)
        {
            Session.Tick(elapsedTime);
        }
    }
}
