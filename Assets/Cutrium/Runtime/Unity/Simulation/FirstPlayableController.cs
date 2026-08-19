using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Feedback;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Unity.Input;
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

        [Header("Barrier Interaction")]
        [SerializeField]
        private BarrierGestureAdapter _barrierGesture;

        [SerializeField]
        private float _barrierGrowthSpeed = 8f;

        [SerializeField]
        private float _barrierCollisionHalfWidth = 0.08f;

        [SerializeField]
        private float _barrierMinimumEdgeMargin = 0.6f;

        [SerializeField]
        private int _maximumBarrierSolverIterations = 16;

        [Header("Level Flow")]
        [SerializeField]
        [Range(0.01f, 1f)]
        private float _targetCapturedFraction = 0.75f;

        [SerializeField]
        private CoreFunLevelDefinition[] _levelDefinitions =
            Array.Empty<CoreFunLevelDefinition>();

        [SerializeField]
        private CoreFunLevelCatalogDefinition _levelCatalogDefinition;

        [Header("Feedback")]
        [SerializeField]
        private FeedbackTuningDefinition _feedbackTuning;

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
        private CoreFunLevelCatalog _levelCatalog;
        private IReadOnlyList<CoreFunLevelDefinition> _activeLevelDefinitions;
        private bool _completionReported;

        public bool GravityWellTargeting { get; private set; }

        public ThreatMotionSession Session { get; private set; }

        public bool SimulationHeld { get; private set; }

        public event Action<FeedbackEvent> FeedbackEventRaised;

        public GeometryTolerancePolicy Tolerance { get; private set; }

        public LogicalRect BoardBounds =>
            Session != null
                ? Session.InitialRoom.Bounds
                : new LogicalRect(0f, 0f, 10f, 16f);

        public float ThreatRadius => _levelCatalog != null
            ? CurrentLevelConfiguration.ThreatMotion.Radius
            : _threatRadius;

        public float ThreatSpeed => _levelCatalog != null
            ? CurrentLevelConfiguration.ThreatMotion.Speed
            : _threatSpeed;

        public int ThreatCount => Session != null
            ? Session.Threats.Count
            : _levelCatalog != null
                ? CurrentLevelConfiguration.ThreatMotions.Count
                : 1;

        public Vector2 InitialPosition => _levelCatalog != null
            ? new Vector2(
                CurrentLevelConfiguration.ThreatMotion.InitialPosition.X,
                CurrentLevelConfiguration.ThreatMotion.InitialPosition.Y)
            : _initialPosition;

        public Vector2 InitialDirection => _levelCatalog != null
            ? new Vector2(
                CurrentLevelConfiguration.ThreatMotion.InitialDirection.X,
                CurrentLevelConfiguration.ThreatMotion.InitialDirection.Y)
            : _initialDirection;

        public int MaximumCatchUpTicks => _levelCatalog != null
            ? CurrentLevelConfiguration.MaximumCatchUpTicks
            : _maximumCatchUpTicks;

        public BarrierGestureAdapter BarrierGesture => _barrierGesture;

        public float BarrierGrowthSpeed => _levelCatalog != null
            ? CurrentLevelConfiguration.Barrier.GrowthSpeed
            : _barrierGrowthSpeed;

        public float BarrierCollisionHalfWidth => _levelCatalog != null
            ? CurrentLevelConfiguration.Barrier.CollisionHalfWidth
            : _barrierCollisionHalfWidth;

        public float BarrierMinimumEdgeMargin => _levelCatalog != null
            ? CurrentLevelConfiguration.Barrier.MinimumEdgeMargin
            : _barrierMinimumEdgeMargin;

        public float TargetCapturedFraction => _levelCatalog != null
            ? CurrentLevelConfiguration.Capture.TargetCapturedFraction
            : _targetCapturedFraction;

        public IReadOnlyList<CoreFunLevelDefinition> LevelDefinitions
        {
            get
            {
                if (_activeLevelDefinitions != null)
                {
                    return _activeLevelDefinitions;
                }

                return _levelCatalogDefinition != null
                    ? _levelCatalogDefinition.EffectiveLevels
                    : _levelDefinitions;
            }
        }

        public CoreFunLevelCatalogDefinition LevelCatalogDefinition =>
            _levelCatalogDefinition;

        public CoreFunLevelConfiguration CurrentLevelConfiguration
        {
            get;
            private set;
        }

        public int CurrentLevelIndex { get; private set; }

        public int CurrentLevelNumber =>
            CurrentLevelConfiguration.DisplayNumber;

        public string CurrentLevelId => CurrentLevelConfiguration.StableId;

        public int LevelCount => _levelCatalog?.Count ?? 0;

        public bool HasNextLevel =>
            _levelCatalog != null && CurrentLevelIndex + 1 < _levelCatalog.Count;

        public CoreFunMetricsTracker Metrics { get; private set; }

        public BarrierStartResult LastBarrierStartResult { get; private set; }

        public int InitializationCount { get; private set; }

        public int CappedCatchUpCount { get; private set; }

        public float DroppedSimulationTime { get; private set; }

        public int RetryCount { get; private set; }

        public int LevelLoadCount { get; private set; }

        public int SequenceRestartCount { get; private set; }

        public int DevelopmentJumpCount { get; private set; }

        public int CompletionLogCount { get; private set; }

        public FeedbackTuningDefinition FeedbackTuning => _feedbackTuning;

        private void Awake()
        {
            InitializeOnce();
        }

        private void OnEnable()
        {
            if (_barrierGesture != null)
            {
                _barrierGesture.IntentCommitted += OnBarrierIntentCommitted;
                _barrierGesture.PointCommitted += OnPointCommitted;
            }
        }

        private void OnDisable()
        {
            if (_barrierGesture != null)
            {
                _barrierGesture.IntentCommitted -= OnBarrierIntentCommitted;
                _barrierGesture.PointCommitted -= OnPointCommitted;
            }
        }

        private void Update()
        {
            if (!SimulationHeld)
            {
                AdvanceSimulation(Time.deltaTime);
            }
        }

        // The pre-level intro sequence (PreLevelIntroPresenter) holds the
        // simulation and disables barrier input while its staged text plays
        // over a hidden board, so threats stay frozen and untouchable until
        // the level visibly starts.
        public void SetSimulationHold(bool held)
        {
            SimulationHeld = held;
            if (held && GravityWellTargeting)
            {
                CancelGravityWellTargeting();
            }

            if (_barrierGesture != null)
            {
                _barrierGesture.enabled = !held;
            }
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

        public void ConfigureBarrierForSetup(
            BarrierGestureAdapter barrierGesture,
            float growthSpeed,
            float collisionHalfWidth,
            float minimumEdgeMargin,
            int maximumSolverIterations)
        {
            _barrierGesture = barrierGesture;
            _barrierGrowthSpeed = growthSpeed;
            _barrierCollisionHalfWidth = collisionHalfWidth;
            _barrierMinimumEdgeMargin = minimumEdgeMargin;
            _maximumBarrierSolverIterations = maximumSolverIterations;
        }

        public void ConfigureCaptureForSetup(float targetCapturedFraction)
        {
            _targetCapturedFraction = targetCapturedFraction;
        }

        public void ConfigureFeedbackForSetup(
            FeedbackTuningDefinition feedbackTuning)
        {
            _feedbackTuning = feedbackTuning
                ?? throw new ArgumentNullException(nameof(feedbackTuning));
        }

        public void ConfigureLevelsForSetup(
            IReadOnlyList<CoreFunLevelDefinition> levelDefinitions)
        {
            if (levelDefinitions == null)
            {
                throw new ArgumentNullException(nameof(levelDefinitions));
            }

            var definitions = new CoreFunLevelDefinition[levelDefinitions.Count];
            var configurations =
                new CoreFunLevelConfiguration[levelDefinitions.Count];
            for (int index = 0; index < levelDefinitions.Count; index++)
            {
                CoreFunLevelDefinition definition = levelDefinitions[index]
                    ?? throw new ArgumentException(
                        "Level definitions cannot contain null entries.",
                        nameof(levelDefinitions));
                definitions[index] = definition;
                configurations[index] = definition.ToRuntimeConfiguration();
            }

            _ = new CoreFunLevelCatalog(configurations);
            _levelDefinitions = definitions;
            _levelCatalogDefinition = null;
            _activeLevelDefinitions = null;
        }

        public void ConfigureLevelCatalogForSetup(
            CoreFunLevelCatalogDefinition levelCatalogDefinition)
        {
            _levelCatalogDefinition = levelCatalogDefinition
                ?? throw new ArgumentNullException(
                    nameof(levelCatalogDefinition));
            _ = _levelCatalogDefinition.BuildRuntimeCatalog();
            _activeLevelDefinitions = null;
        }

        public BarrierStartResult SubmitBarrierIntent(BarrierIntent intent)
        {
            InitializeOnce();
            LastBarrierStartResult = Session.TryStartBarrier(intent);
            if (LastBarrierStartResult.Accepted)
            {
                Metrics.RecordBarrierAttempt();
            }

            DispatchFeedbackEvents();

            return LastBarrierStartResult;
        }

        public BarrierStartResult ValidateBarrierIntent(BarrierIntent intent)
        {
            InitializeOnce();
            return Session.ValidateBarrierStart(intent);
        }

        public bool TryActivateFreezePulse()
        {
            InitializeOnce();
            bool activated = Session.TryActivateFreezePulse();
            DispatchFeedbackEvents();
            return activated;
        }

        public bool TryArmInstantBarrier()
        {
            InitializeOnce();
            bool armed = Session.TryArmInstantBarrier();
            DispatchFeedbackEvents();
            return armed;
        }

        public bool ToggleGravityWellTargeting()
        {
            InitializeOnce();
            if (GravityWellTargeting)
            {
                CancelGravityWellTargeting();
                return false;
            }

            if (!Session.CanActivateGravityWell || _barrierGesture == null)
            {
                return false;
            }

            GravityWellTargeting = true;
            _barrierGesture.SetPointTargeting(true);
            return true;
        }

        public void CancelGravityWellTargeting()
        {
            GravityWellTargeting = false;
            _barrierGesture?.SetPointTargeting(false);
        }

        public bool TryPlaceGravityWell(LogicalPoint position)
        {
            InitializeOnce();
            if (!GravityWellTargeting)
            {
                return false;
            }

            bool activated = Session.TryActivateGravityWell(position);
            if (activated)
            {
                CancelGravityWellTargeting();
            }

            DispatchFeedbackEvents();
            return activated;
        }

        public int FreezePulseChargesRemaining =>
            Session?.FreezePulseChargesRemaining ?? 0;

        public float FreezePulseRemainingSeconds =>
            Session?.FreezePulseRemainingSeconds ?? 0f;

        public int InstantBarrierChargesRemaining =>
            Session?.InstantBarrierChargesRemaining ?? 0;

        public bool InstantBarrierArmed =>
            Session?.InstantBarrierArmed ?? false;

        public int GravityWellChargesRemaining =>
            Session?.GravityWellChargesRemaining ?? 0;

        public float GravityWellRemainingSeconds =>
            Session?.GravityWellRemainingSeconds ?? 0f;

        public LogicalPoint? GravityWellPosition =>
            Session?.GravityWellPosition;

        public bool GravityWellActive => Session?.GravityWellActive ?? false;

        public float GravityWellRadius => Session?.GravityWellRadius ?? 0f;

        public bool HasCutLimit => Session?.HasCutLimit ?? false;

        public int AcceptedCutCount => Session?.AcceptedCutCount ?? 0;

        public int MaximumAcceptedCuts =>
            Session?.MaximumAcceptedCuts ?? 0;

        public int CutsRemaining => Session?.HasCutLimit == true
            ? Session.CutsRemaining
            : 0;

        public void RetryLevel()
        {
            InitializeOnce();
            Metrics.RetryCurrentLevel();
            LoadCurrentLevel();
            RetryCount++;
        }

        public bool TryAdvanceToNextLevel()
        {
            InitializeOnce();
            if (Session.LevelStatus != CaptureLevelStatus.Completed
                || !HasNextLevel)
            {
                return false;
            }

            int nextIndex = CurrentLevelIndex + 1;
            Metrics.AdvanceTo(_levelCatalog[nextIndex]);
            CurrentLevelIndex = nextIndex;
            CurrentLevelConfiguration = _levelCatalog[nextIndex];
            LoadCurrentLevel();
            return true;
        }

        public bool RestartSequence()
        {
            InitializeOnce();
            bool completedSequence =
                !HasNextLevel
                && Session.LevelStatus == CaptureLevelStatus.Completed;
            if (completedSequence)
            {
                Metrics.CompleteSequenceAndRestart(_levelCatalog[0]);
            }
            else
            {
                Metrics.StartSequence(_levelCatalog[0]);
            }

            CurrentLevelIndex = 0;
            CurrentLevelConfiguration = _levelCatalog[0];
            LoadCurrentLevel();
            SequenceRestartCount++;
            return completedSequence;
        }

        public bool AdvanceLevelOrRestartSequence()
        {
            InitializeOnce();
            if (Session.LevelStatus != CaptureLevelStatus.Completed)
            {
                return false;
            }

            return HasNextLevel
                ? TryAdvanceToNextLevel()
                : RestartSequence();
        }

        public bool TryJumpToLevelForDevelopment(int oneBasedLevelNumber)
        {
            InitializeOnce();
            int index = oneBasedLevelNumber - 1;
            if (index < 0 || index >= _levelCatalog.Count)
            {
                return false;
            }

            Metrics.StartSequence(_levelCatalog[index]);
            CurrentLevelIndex = index;
            CurrentLevelConfiguration = _levelCatalog[index];
            LoadCurrentLevel();
            DevelopmentJumpCount++;
            return true;
        }

        public bool TryGoToPreviousLevelForDevelopment()
        {
            InitializeOnce();
            return CurrentLevelIndex > 0
                && TryJumpToLevelForDevelopment(CurrentLevelIndex);
        }

        public bool TryGoToNextLevelForDevelopment()
        {
            InitializeOnce();
            return CurrentLevelIndex + 1 < _levelCatalog.Count
                && TryJumpToLevelForDevelopment(CurrentLevelIndex + 2);
        }

        public void NotifyUiFeedback()
        {
            InitializeOnce();
            FeedbackEventRaised?.Invoke(new FeedbackEvent(
                FeedbackEventKind.Ui,
                default,
                0f,
                Session.CapturedFraction,
                float.PositiveInfinity,
                Session.ComboCount));
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
            _levelCatalog = BuildLevelCatalog();
            CurrentLevelIndex = 0;
            CurrentLevelConfiguration = _levelCatalog[0];
            Metrics = new CoreFunMetricsTracker();
            Metrics.StartSequence(CurrentLevelConfiguration);
            _tickAction = TickSession;
            InitializationCount++;
            LoadCurrentLevel();
        }

        private void TickSession(float elapsedTime)
        {
            if (Session.LevelStatus == CaptureLevelStatus.Playing)
            {
                Metrics.AdvanceTime(elapsedTime);
            }

            float capturedBefore = Session.CapturedFraction;
            Session.Tick(elapsedTime);
            DispatchFeedbackEvents();
            if (Session.LastBarrierEvent == BarrierSimulationEvent.Failed)
            {
                Metrics.RecordBarrierFailure(Session.CapturedFraction);
            }
            else if (Session.LastBarrierEvent == BarrierSimulationEvent.Locked)
            {
                Metrics.RecordBarrierSuccess(
                    Session.CapturedFraction - capturedBefore,
                    Session.CapturedFraction);
            }

            if (!_completionReported
                && Session.LevelStatus == CaptureLevelStatus.Completed)
            {
                _completionReported = true;
                Metrics.RecordCompletion(Session.CapturedFraction);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                CompletionLogCount++;
                CoreFunLevelMetrics metrics = Metrics.Current;
                Debug.Log(
                    $"CoreFun Level {metrics.LevelNumber} '{metrics.LevelId}' " +
                    $"complete: {metrics.ElapsedSeconds:0.00}s, " +
                    $"attempts={metrics.BarrierAttempts}, " +
                    $"failed={metrics.FailedBarriers}, " +
                    $"successful={metrics.SuccessfulBarriers}, " +
                    $"largest={metrics.LargestSingleCapturedFraction:P0}, " +
                    $"captured={metrics.FinalCapturedFraction:P0}, " +
                    $"retries={metrics.RetryCount}.");
#endif
            }
        }

        private CoreFunLevelCatalog BuildLevelCatalog()
        {
            if (_levelCatalogDefinition != null)
            {
                _activeLevelDefinitions =
                    _levelCatalogDefinition.EffectiveLevels;
                return _levelCatalogDefinition.BuildRuntimeCatalog();
            }

            // The checked-in scene predates the ScriptableObject catalog and
            // carries the exact three-level Milestone 3 gate inline. Promote
            // only that known legacy payload so the first-twelve progression
            // is playable before the focused Editor setup can serialize its
            // catalog asset. Custom/test catalogs are never replaced.
            if (HasLegacyThreeLevelGate(_levelDefinitions))
            {
                _activeLevelDefinitions =
                    MainGameplayProgression.CreateDefinitions();
                return BuildCatalog(_activeLevelDefinitions);
            }

            if (_levelDefinitions != null && _levelDefinitions.Length > 0)
            {
                _activeLevelDefinitions = _levelDefinitions;
                return BuildCatalog(_activeLevelDefinitions);
            }

            var legacyThreat = new ThreatMotionConfiguration(
                CoreFunLevelConfiguration.FixedBoardBounds,
                new LogicalPoint(_initialPosition.x, _initialPosition.y),
                new LogicalVector(_initialDirection.x, _initialDirection.y),
                _threatSpeed,
                _threatRadius,
                _maximumImpactsPerTick);
            var legacyBarrier = new BarrierConfiguration(
                _barrierGrowthSpeed,
                _barrierCollisionHalfWidth,
                _barrierMinimumEdgeMargin,
                _maximumBarrierSolverIterations);
            var legacyCapture = new CaptureLevelConfiguration(
                _targetCapturedFraction);
            return new CoreFunLevelCatalog(new[]
            {
                new CoreFunLevelConfiguration(
                    "legacy-first-playable",
                    1,
                    legacyThreat,
                    legacyBarrier,
                    legacyCapture,
                    _maximumCatchUpTicks,
                    string.Empty,
                    0f),
            });
        }

        private static CoreFunLevelCatalog BuildCatalog(
            IReadOnlyList<CoreFunLevelDefinition> definitions)
        {
            var configurations =
                new CoreFunLevelConfiguration[definitions.Count];
            for (int index = 0; index < definitions.Count; index++)
            {
                CoreFunLevelDefinition definition = definitions[index]
                    ?? throw new InvalidOperationException(
                        "Serialized level definitions cannot be null.");
                configurations[index] = definition.ToRuntimeConfiguration();
            }

            return new CoreFunLevelCatalog(configurations);
        }

        private static bool HasLegacyThreeLevelGate(
            IReadOnlyList<CoreFunLevelDefinition> definitions) =>
            definitions != null
            && definitions.Count == 3
            && definitions[0] != null
            && definitions[1] != null
            && definitions[2] != null
            && string.Equals(
                definitions[0].StableId,
                "learn-the-cut",
                StringComparison.Ordinal)
            && string.Equals(
                definitions[1].StableId,
                "timing-and-failure",
                StringComparison.Ordinal)
            && string.Equals(
                definitions[2].StableId,
                "confident-capture",
                StringComparison.Ordinal);

        private void LoadCurrentLevel()
        {
            GravityWellTargeting = false;
            Session = new ThreatMotionSession(
                CurrentLevelConfiguration.ThreatMotions,
                CurrentLevelConfiguration.Barrier,
                CurrentLevelConfiguration.Capture,
                _feedbackTuning != null
                    ? _feedbackTuning.ToRuntimeConfiguration()
                    : FeedbackTuningConfiguration.Default,
                CurrentLevelConfiguration.Power,
                Tolerance);
            _accumulator = new FixedStepAccumulator(
                SimulationStep,
                CurrentLevelConfiguration.MaximumCatchUpTicks,
                Tolerance);
            _barrierGesture?.ResetForRetry();
            _barrierGesture?.PointerInput?.ResetInteractionState();
            LastBarrierStartResult = default;
            CappedCatchUpCount = 0;
            DroppedSimulationTime = 0f;
            _completionReported = false;
            LevelLoadCount++;
            DispatchFeedbackEvents();
        }

        private void DispatchFeedbackEvents()
        {
            if (Session == null || FeedbackEventRaised == null)
            {
                return;
            }

            IReadOnlyList<FeedbackEvent> events = Session.FeedbackEvents;
            for (int index = 0; index < events.Count; index++)
            {
                FeedbackEventRaised.Invoke(events[index]);
            }
        }

        private void OnBarrierIntentCommitted(BarrierIntent intent)
        {
            SubmitBarrierIntent(intent);
        }

        private void OnPointCommitted(LogicalPoint point)
        {
            TryPlaceGravityWell(point);
        }
    }
}
