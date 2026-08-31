using System;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Feedback;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.Localization;
using Cutrium.Unity.Input;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.HUD
{
    public enum GuidedTrainingStage
    {
        Inactive = 0,
        WaitingForIntro = 1,
        Prompting = 2,
        ResolvingAction = 3,
        SuccessFeedback = 4,
        Complete = 5,
    }

    /// <summary>
    /// Runs presentation-authored preparation steps against authoritative
    /// gameplay input and feedback. It never starts barriers, captures rooms,
    /// activates powers, or advances logical progress itself.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuidedTrainingPresenter : MonoBehaviour
    {
        [SerializeField]
        private GuidedTrainingDefinition[] _definitions =
            Array.Empty<GuidedTrainingDefinition>();

        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private BarrierGestureAdapter _gesture;
        [SerializeField] private PreLevelIntroPresenter _preLevelIntro;
        [SerializeField] private LocalizationService _localization;
        [SerializeField] private SandProgressPresenter _sandProgress;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _handVisual;
        [SerializeField] private TMP_Text _instructionText;
        [SerializeField] private TMP_Text _tapToContinueText;
        [SerializeField]
        private TrainingFocusHighlightPresenter _focusHighlight;

        [Header("Focus Targets")]
        [SerializeField] private RectTransform _progressFocusTarget;
        [SerializeField] private RectTransform _freezePulseFocusTarget;
        [SerializeField] private RectTransform _instantBarrierFocusTarget;
        [SerializeField] private RectTransform _gravityWellFocusTarget;
        [SerializeField] private RectTransform _speedHudFocusTarget;
        [SerializeField] private RectTransform _livesHudFocusTarget;

        [Header("Motion")]
        [SerializeField] private float _travelDistance = 52f;
        [SerializeField] private float _cycleSeconds = 1.15f;
        [SerializeField] private float _fadeSeconds = 0.16f;

        [Header("Fixed Origin")]
        [SerializeField] [Min(0f)]
        private float _requiredOriginTolerance = 1.5f;

        private const string TapToContinueEnglish = "TAP TO CONTINUE";
        private const string TapToContinueTurkish = "DEVAM ETMEK İÇİN DOKUN";

        private ThreatMotionSession _observedSession;
        private GuidedTrainingDefinition _activeDefinition;
        private GuidedTrainingStage _stage = GuidedTrainingStage.Inactive;
        private int _stepIndex;
        private Vector2 _handRestPosition;
        private float _animationElapsed;
        private float _successElapsed;
        private float _passiveElapsed;
        private bool _subscribed;
        private bool _localizationSubscribed;
        private bool _hasPendingIntent;
        private BarrierIntent _pendingIntent;

        public GuidedTrainingDefinition[] Definitions => _definitions;
        public FirstPlayableController Controller => _controller;
        public BarrierGestureAdapter Gesture => _gesture;
        public PreLevelIntroPresenter PreLevelIntro => _preLevelIntro;
        public LocalizationService Localization => _localization;
        public SandProgressPresenter SandProgress => _sandProgress;
        public CanvasGroup CanvasGroup => _canvasGroup;
        public RectTransform HandVisual => _handVisual;
        public TMP_Text InstructionText => _instructionText;
        public TMP_Text TapToContinueText => _tapToContinueText;
        public TrainingFocusHighlightPresenter FocusHighlight =>
            _focusHighlight;
        public RectTransform ProgressFocusTarget => _progressFocusTarget;
        public RectTransform SpeedHudFocusTarget => _speedHudFocusTarget;
        public RectTransform LivesHudFocusTarget => _livesHudFocusTarget;
        public GuidedTrainingDefinition ActiveDefinition =>
            _activeDefinition;
        public GuidedTrainingStage Stage => _stage;
        public int StepIndex => _stepIndex;
        public bool IsComplete => _stage == GuidedTrainingStage.Complete;
        public bool IsVisible => _canvasGroup != null
            && _canvasGroup.alpha > 0f;

        private GuidedTrainingStep CurrentStep =>
            _activeDefinition != null
            && _stepIndex >= 0
            && _stepIndex < _activeDefinition.Steps.Count
                ? _activeDefinition.Steps[_stepIndex]
                : null;

        private void Awake()
        {
            _handRestPosition = _handVisual != null
                ? _handVisual.anchoredPosition
                : Vector2.zero;
            SetGroupImmediate(false);
            _focusHighlight?.Hide();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            ReleaseTrainingControl();
            SetGroupImmediate(false);
            _focusHighlight?.Hide();
        }

        private void LateUpdate()
        {
            RefreshNow(Time.unscaledDeltaTime);
        }

        public void ConfigureForSetup(
            GuidedTrainingDefinition[] definitions,
            FirstPlayableController controller,
            BarrierGestureAdapter gesture,
            PreLevelIntroPresenter preLevelIntro,
            LocalizationService localization,
            SandProgressPresenter sandProgress,
            CanvasGroup canvasGroup,
            RectTransform handVisual,
            TMP_Text instructionText,
            TMP_Text tapToContinueText,
            TrainingFocusHighlightPresenter focusHighlight,
            RectTransform progressFocusTarget,
            RectTransform freezePulseFocusTarget = null,
            RectTransform instantBarrierFocusTarget = null,
            RectTransform gravityWellFocusTarget = null,
            RectTransform speedHudFocusTarget = null,
            RectTransform livesHudFocusTarget = null,
            float travelDistance = 52f,
            float cycleSeconds = 1.15f,
            float fadeSeconds = 0.16f,
            float requiredOriginTolerance = 1.5f)
        {
            ValidateDefinitions(definitions);
            ValidatePositiveFinite(travelDistance, nameof(travelDistance));
            ValidatePositiveFinite(cycleSeconds, nameof(cycleSeconds));
            ValidateNonNegativeFinite(fadeSeconds, nameof(fadeSeconds));
            ValidateNonNegativeFinite(
                requiredOriginTolerance,
                nameof(requiredOriginTolerance));

            Unsubscribe();
            ReleaseTrainingControl();
            _definitions = definitions;
            _controller = controller;
            _gesture = gesture;
            _preLevelIntro = preLevelIntro;
            _localization = localization;
            _sandProgress = sandProgress;
            _canvasGroup = canvasGroup;
            _handVisual = handVisual;
            _instructionText = instructionText;
            _tapToContinueText = tapToContinueText;
            _focusHighlight = focusHighlight;
            _progressFocusTarget = progressFocusTarget;
            _freezePulseFocusTarget = freezePulseFocusTarget;
            _instantBarrierFocusTarget = instantBarrierFocusTarget;
            _gravityWellFocusTarget = gravityWellFocusTarget;
            _speedHudFocusTarget = speedHudFocusTarget;
            _livesHudFocusTarget = livesHudFocusTarget;
            _travelDistance = travelDistance;
            _cycleSeconds = cycleSeconds;
            _fadeSeconds = fadeSeconds;
            _requiredOriginTolerance = requiredOriginTolerance;
            _handRestPosition = _handVisual != null
                ? _handVisual.anchoredPosition
                : Vector2.zero;
            _observedSession = null;
            _activeDefinition = null;
            _stepIndex = 0;
            _hasPendingIntent = false;
            SetStage(GuidedTrainingStage.Inactive);
            SetGroupImmediate(false);
            _focusHighlight?.Hide();

            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
            }
        }

        public void RefreshNow(float unscaledDeltaTime)
        {
            ValidateNonNegativeFinite(
                unscaledDeltaTime,
                nameof(unscaledDeltaTime));
            Subscribe();

            ThreatMotionSession session = _controller?.Session;
            if (!ReferenceEquals(_observedSession, session))
            {
                ObserveSession(session);
            }

            if (_activeDefinition == null || session == null)
            {
                ReleaseTrainingControl();
                SetGroupImmediate(false);
                _focusHighlight?.Hide();
                return;
            }

            ResolvePendingIntent();
            bool externallyBlocked = _controller.BarrierInputBlocked;
            GuidedTrainingStep step = CurrentStep;
            if (_stage == GuidedTrainingStage.WaitingForIntro
                && !externallyBlocked
                && !IsIntroPlaying()
                && session.LevelStatus == CaptureLevelStatus.Playing)
            {
                BeginCurrentStep();
            }
            else if (_stage == GuidedTrainingStage.SuccessFeedback
                && !externallyBlocked)
            {
                AdvanceSuccess(unscaledDeltaTime);
            }
            else if (_stage == GuidedTrainingStage.Prompting
                && !externallyBlocked
                && step != null
                && step.StepKind == GuidedTrainingStepKind.Observe)
            {
                AdvancePassive(unscaledDeltaTime);
            }

            bool shouldShow = !externallyBlocked
                && (_stage == GuidedTrainingStage.Prompting
                    || _stage == GuidedTrainingStage.ResolvingAction
                    || _stage == GuidedTrainingStage.SuccessFeedback);
            UpdateGroupAlpha(shouldShow, unscaledDeltaTime);
            if (shouldShow)
            {
                AnimateHand(unscaledDeltaTime);
            }
            else
            {
                ResetHandVisual();
            }
        }

        public void SkipForTesting()
        {
            _observedSession = _controller?.Session;
            _activeDefinition = FindDefinition(
                _controller?.CurrentLevelId);
            CompleteTraining();
        }

        private void ObserveSession(ThreatMotionSession session)
        {
            ReleaseTrainingControl();
            _focusHighlight?.Hide();
            _observedSession = session;
            _activeDefinition = session != null
                ? FindDefinition(_controller.CurrentLevelId)
                : null;
            _stepIndex = 0;
            _hasPendingIntent = false;
            _successElapsed = 0f;
            _passiveElapsed = 0f;
            SetStage(_activeDefinition != null
                ? GuidedTrainingStage.WaitingForIntro
                : GuidedTrainingStage.Inactive);
            SetGroupImmediate(false);
        }

        private void BeginCurrentStep()
        {
            GuidedTrainingStep step = CurrentStep;
            if (step == null)
            {
                CompleteTraining();
                return;
            }

            _hasPendingIntent = false;
            _successElapsed = 0f;
            _passiveElapsed = 0f;
            _gesture?.SetRequiredOrientation(
                RequiredOrientationFor(step.Action));
            _gesture?.SetRequiredOrigin(
                step.StepKind == GuidedTrainingStepKind.Action
                    ? step.FixedOrigin
                    : null,
                _requiredOriginTolerance);
            _gesture?.SetPointTargeting(
                step.StepKind == GuidedTrainingStepKind.Info);
            _gesture?.SetInputSuppressed(
                step.StepKind == GuidedTrainingStepKind.Observe);
            SetTrainingHeld(step.Freeze);
            ShowFocus(step.PromptFocus);
            SetStage(GuidedTrainingStage.Prompting);

            if (step.StepKind == GuidedTrainingStepKind.Action
                && step.RequiresLevelCompletion
                && _observedSession != null
                && _observedSession.LevelStatus
                    == CaptureLevelStatus.Completed)
            {
                // The level was already finished by an earlier cut; there is
                // nothing left for this step to wait for.
                BeginSuccessFeedback();
            }
        }

        private void BeginResolvingAction()
        {
            _gesture?.SetRequiredOrientation(BarrierOrientation.None);
            _gesture?.SetRequiredOrigin(null);
            SetTrainingHeld(false);
            SetStage(GuidedTrainingStage.ResolvingAction);
        }

        private void BeginSuccessFeedback()
        {
            GuidedTrainingStep step = CurrentStep;
            if (step == null)
            {
                CompleteTraining();
                return;
            }

            SetTrainingHeld(true);
            _gesture?.SetRequiredOrientation(BarrierOrientation.None);
            _gesture?.SetRequiredOrigin(null);
            // The simulation is frozen for this celebratory beat and
            // nothing here is listening for a committed intent, so any
            // stray touch must be ignored outright -- otherwise a swipe
            // during this window can start a real barrier in the frozen
            // session that then never grows and blocks every later
            // attempt (see ADR entry for the guided-training stuck-barrier
            // fix).
            _gesture?.SetInputSuppressed(true);
            _successElapsed = 0f;
            ShowFocus(step.SuccessFocus);
            SetStage(GuidedTrainingStage.SuccessFeedback);
        }

        private void AdvanceSuccess(float unscaledDeltaTime)
        {
            GuidedTrainingStep step = CurrentStep;
            if (step == null)
            {
                CompleteTraining();
                return;
            }

            _successElapsed += unscaledDeltaTime;
            if (_successElapsed < step.SuccessSeconds
                || !CompletionGateSatisfied(step.CompletionGate))
            {
                return;
            }

            AdvanceToNextStepOrComplete();
        }

        private void AdvancePassive(float unscaledDeltaTime)
        {
            GuidedTrainingStep step = CurrentStep;
            if (step == null)
            {
                CompleteTraining();
                return;
            }

            _passiveElapsed += unscaledDeltaTime;
            if (_passiveElapsed < step.DurationSeconds)
            {
                return;
            }

            AdvanceToNextStepOrComplete();
        }

        private void AdvanceToNextStepOrComplete()
        {
            _stepIndex++;
            if (_stepIndex >= _activeDefinition.Steps.Count)
            {
                CompleteTraining();
            }
            else
            {
                BeginCurrentStep();
            }
        }

        private void CompleteTraining()
        {
            ReleaseTrainingControl();
            _focusHighlight?.Hide();
            SetStage(GuidedTrainingStage.Complete);
            SetGroupImmediate(false);
        }

        private void Subscribe()
        {
            if (!_subscribed)
            {
                if (_gesture != null)
                {
                    _gesture.IntentCommitted += OnIntentCommitted;
                    _gesture.PointCommitted += OnPointCommitted;
                }

                if (_controller != null)
                {
                    _controller.FeedbackEventRaised += OnFeedbackEvent;
                }

                _subscribed = _gesture != null || _controller != null;
            }

            if (!_localizationSubscribed && _localization != null)
            {
                _localization.LanguageChanged += OnLanguageChanged;
                _localizationSubscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribed)
            {
                if (_gesture != null)
                {
                    _gesture.IntentCommitted -= OnIntentCommitted;
                    _gesture.PointCommitted -= OnPointCommitted;
                }

                if (_controller != null)
                {
                    _controller.FeedbackEventRaised -= OnFeedbackEvent;
                }
            }

            if (_localizationSubscribed && _localization != null)
            {
                _localization.LanguageChanged -= OnLanguageChanged;
            }

            _subscribed = false;
            _localizationSubscribed = false;
        }

        private void OnIntentCommitted(BarrierIntent intent)
        {
            GuidedTrainingStep step = CurrentStep;
            if (_stage != GuidedTrainingStage.Prompting
                || step == null
                || !IsBarrierAction(step.Action))
            {
                return;
            }

            _pendingIntent = intent;
            _hasPendingIntent = true;
        }

        private void OnPointCommitted(LogicalPoint point)
        {
            _ = point;
            GuidedTrainingStep step = CurrentStep;
            if (_stage == GuidedTrainingStage.Prompting
                && step != null
                && step.StepKind == GuidedTrainingStepKind.Info)
            {
                AdvanceToNextStepOrComplete();
            }
        }

        private void ResolvePendingIntent()
        {
            if (!_hasPendingIntent)
            {
                return;
            }

            _hasPendingIntent = false;
            GuidedTrainingStep step = CurrentStep;
            bool accepted = step != null
                && IntentMatches(step.Action, _pendingIntent)
                && _controller.LastBarrierStartResult.Accepted;
            if (accepted)
            {
                BeginResolvingAction();
            }
            else if (_stage == GuidedTrainingStage.Prompting)
            {
                BeginCurrentStep();
            }
        }

        private void OnFeedbackEvent(FeedbackEvent feedbackEvent)
        {
            GuidedTrainingStep step = CurrentStep;
            if (step == null || step.StepKind != GuidedTrainingStepKind.Action)
            {
                return;
            }

            if (step.RequiresLevelCompletion)
            {
                if (feedbackEvent.Kind == FeedbackEventKind.LevelCompleted)
                {
                    // Finish immediately and let the game's own completion
                    // UI take over; a training success beat would only
                    // compete with it.
                    CompleteTraining();
                }
                else if (feedbackEvent.Kind == FeedbackEventKind.BarrierLocked
                    && _stage == GuidedTrainingStage.ResolvingAction)
                {
                    // Locked, but the level isn't finished yet: let the
                    // player keep cutting freely without re-freezing.
                    SetStage(GuidedTrainingStage.Prompting);
                }

                return;
            }

            if (_stage == GuidedTrainingStage.ResolvingAction)
            {
                if (feedbackEvent.Kind == FeedbackEventKind.BarrierLocked)
                {
                    BeginSuccessFeedback();
                }
                else if (feedbackEvent.Kind
                    == FeedbackEventKind.BarrierBroken)
                {
                    BeginCurrentStep();
                }

                return;
            }

            if (_stage == GuidedTrainingStage.Prompting
                && FeedbackMatches(step.Action, feedbackEvent.Kind))
            {
                BeginSuccessFeedback();
            }
        }

        private void SetStage(GuidedTrainingStage stage)
        {
            _stage = stage;
            _animationElapsed = 0f;
            RefreshInstruction();
            ResetHandVisual();
        }

        private void RefreshInstruction()
        {
            if (_instructionText == null)
            {
                return;
            }

            GuidedTrainingStep step = CurrentStep;
            if (step == null)
            {
                _instructionText.text = string.Empty;
                if (_tapToContinueText != null)
                {
                    _tapToContinueText.gameObject.SetActive(false);
                }

                return;
            }

            bool turkish = _localization != null
                && _localization.CurrentLanguage
                    == SupportedLanguage.Turkish;
            string copy;
            switch (_stage)
            {
                case GuidedTrainingStage.Prompting:
                    copy = step.Prompt(turkish);
                    break;
                case GuidedTrainingStage.ResolvingAction:
                    copy = step.Resolving(turkish);
                    break;
                case GuidedTrainingStage.SuccessFeedback:
                    copy = step.Success(turkish);
                    break;
                default:
                    copy = string.Empty;
                    break;
            }

            _instructionText.text = !turkish && _localization != null
                ? _localization.Localize(copy)
                : copy;

            bool showTapPrompt = _stage == GuidedTrainingStage.Prompting
                && step.StepKind == GuidedTrainingStepKind.Info;
            if (_tapToContinueText != null)
            {
                _tapToContinueText.gameObject.SetActive(showTapPrompt);
                if (showTapPrompt)
                {
                    _tapToContinueText.text = turkish
                        ? TapToContinueTurkish
                        : TapToContinueEnglish;
                }
            }
        }

        private void OnLanguageChanged(SupportedLanguage language)
        {
            _ = language;
            RefreshInstruction();
        }

        private void AnimateHand(float unscaledDeltaTime)
        {
            if (_handVisual == null)
            {
                return;
            }

            GuidedTrainingStep step = CurrentStep;
            bool showHand = _stage == GuidedTrainingStage.Prompting
                && step != null
                && step.HandMotion != GuidedTrainingHandMotion.None;
            Image image = _handVisual.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = showHand;
            }

            if (!showHand)
            {
                _handVisual.anchoredPosition = _handRestPosition;
                _handVisual.localScale = Vector3.one;
                return;
            }

            Vector2 restPosition = ResolveHandRestPosition(step);
            _animationElapsed += unscaledDeltaTime;
            float phase = (_animationElapsed / _cycleSeconds)
                * Mathf.PI * 2f;
            float travel = Mathf.Sin(phase) * _travelDistance;
            Vector2 offset = Vector2.zero;
            Vector3 scale = Vector3.one;
            switch (step.HandMotion)
            {
                case GuidedTrainingHandMotion.Horizontal:
                    offset.x = travel;
                    break;
                case GuidedTrainingHandMotion.Vertical:
                    offset.y = travel;
                    break;
                case GuidedTrainingHandMotion.Pulse:
                    scale *= 1f + 0.08f
                        * (0.5f + 0.5f * Mathf.Sin(phase));
                    break;
            }

            _handVisual.anchoredPosition = restPosition + offset;
            _handVisual.localScale = scale;
        }

        private Vector2 ResolveHandRestPosition(GuidedTrainingStep step)
        {
            if (!step.FixedOrigin.HasValue || _controller == null)
            {
                return _handRestPosition;
            }

            return LogicalToAnchored(
                step.FixedOrigin.Value,
                _controller.BoardBounds);
        }

        private Vector2 LogicalToAnchored(LogicalPoint point, LogicalRect board)
        {
            Rect rect = ((RectTransform)transform).rect;
            return new Vector2(
                ((point.X - board.MinX) / board.Width - 0.5f) * rect.width,
                ((point.Y - board.MinY) / board.Height - 0.5f) * rect.height);
        }

        private void ResetHandVisual()
        {
            if (_handVisual == null)
            {
                return;
            }

            _handVisual.anchoredPosition = _handRestPosition;
            _handVisual.localScale = Vector3.one;
            Image image = _handVisual.GetComponent<Image>();
            if (image != null)
            {
                GuidedTrainingStep step = CurrentStep;
                image.enabled = _stage == GuidedTrainingStage.Prompting
                    && step != null
                    && step.HandMotion != GuidedTrainingHandMotion.None;
            }
        }

        private void UpdateGroupAlpha(bool visible, float deltaTime)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            float target = visible ? 1f : 0f;
            _canvasGroup.alpha = _fadeSeconds <= 0f
                ? target
                : Mathf.MoveTowards(
                    _canvasGroup.alpha,
                    target,
                    deltaTime / _fadeSeconds);
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void SetGroupImmediate(bool visible)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void SetTrainingHeld(bool held)
        {
            if (Application.isPlaying && _controller != null)
            {
                _controller.SetSimulationHold(
                    SimulationHoldReason.GuidedTraining,
                    held);
            }
        }

        private void ReleaseTrainingControl()
        {
            SetTrainingHeld(false);
            _gesture?.SetRequiredOrientation(BarrierOrientation.None);
            _gesture?.SetRequiredOrigin(null);
            _gesture?.SetPointTargeting(false);
            _gesture?.SetInputSuppressed(false);
        }

        private bool IsIntroPlaying() =>
            _preLevelIntro != null && _preLevelIntro.IsPlaying;

        private bool CompletionGateSatisfied(
            GuidedTrainingCompletionGate gate)
        {
            switch (gate)
            {
                case GuidedTrainingCompletionGate.ProgressSettled:
                    return _sandProgress != null
                        && _sandProgress.IsSettledAtLatestLogicalValue;
                default:
                    return true;
            }
        }

        private void ShowFocus(GuidedTrainingFocusTarget target)
        {
            RectTransform focus = ResolveFocus(target);
            if (focus != null)
            {
                _focusHighlight?.Show(focus);
            }
            else
            {
                _focusHighlight?.Hide();
            }
        }

        private RectTransform ResolveFocus(
            GuidedTrainingFocusTarget focus)
        {
            switch (focus)
            {
                case GuidedTrainingFocusTarget.Progress:
                    return _progressFocusTarget;
                case GuidedTrainingFocusTarget.FreezePulse:
                    return _freezePulseFocusTarget;
                case GuidedTrainingFocusTarget.InstantBarrier:
                    return _instantBarrierFocusTarget;
                case GuidedTrainingFocusTarget.GravityWell:
                    return _gravityWellFocusTarget;
                case GuidedTrainingFocusTarget.BarrierSpeed:
                    return _speedHudFocusTarget;
                case GuidedTrainingFocusTarget.Lives:
                    return _livesHudFocusTarget;
                default:
                    return null;
            }
        }

        private GuidedTrainingDefinition FindDefinition(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || _definitions == null)
            {
                return null;
            }

            for (int index = 0; index < _definitions.Length; index++)
            {
                GuidedTrainingDefinition definition = _definitions[index];
                if (definition != null
                    && string.Equals(
                        definition.StableLevelId,
                        levelId,
                        StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        private static BarrierOrientation RequiredOrientationFor(
            GuidedTrainingActionKind action)
        {
            switch (action)
            {
                case GuidedTrainingActionKind.HorizontalBarrier:
                    return BarrierOrientation.Horizontal;
                case GuidedTrainingActionKind.VerticalBarrier:
                    return BarrierOrientation.Vertical;
                default:
                    return BarrierOrientation.None;
            }
        }

        private static bool IsBarrierAction(
            GuidedTrainingActionKind action) =>
            action == GuidedTrainingActionKind.HorizontalBarrier
            || action == GuidedTrainingActionKind.VerticalBarrier
            || action == GuidedTrainingActionKind.FreeBarrier;

        private static bool IntentMatches(
            GuidedTrainingActionKind action,
            BarrierIntent intent)
        {
            BarrierOrientation required = RequiredOrientationFor(action);
            return required == BarrierOrientation.None
                || required == intent.Orientation;
        }

        private static bool FeedbackMatches(
            GuidedTrainingActionKind action,
            FeedbackEventKind feedback)
        {
            switch (action)
            {
                case GuidedTrainingActionKind.FreezePulse:
                    return feedback
                        == FeedbackEventKind.PowerFreezePulseActivated;
                case GuidedTrainingActionKind.InstantBarrier:
                    return feedback
                        == FeedbackEventKind.PowerInstantBarrierArmed;
                case GuidedTrainingActionKind.GravityWell:
                    return feedback
                        == FeedbackEventKind.PowerGravityWellActivated;
                default:
                    return false;
            }
        }

        private static void ValidateDefinitions(
            GuidedTrainingDefinition[] definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            for (int index = 0; index < definitions.Length; index++)
            {
                GuidedTrainingDefinition definition = definitions[index]
                    ?? throw new ArgumentException(
                        "Training definitions cannot contain null entries.",
                        nameof(definitions));
                definition.Validate();
                for (int other = index + 1;
                    other < definitions.Length;
                    other++)
                {
                    if (definitions[other] != null
                        && string.Equals(
                            definition.StableLevelId,
                            definitions[other].StableLevelId,
                            StringComparison.Ordinal))
                    {
                        throw new ArgumentException(
                            "Training level IDs must be unique.",
                            nameof(definitions));
                    }
                }
            }
        }

        private static void ValidatePositiveFinite(float value, string name)
        {
            if (float.IsNaN(value)
                || float.IsInfinity(value)
                || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static void ValidateNonNegativeFinite(
            float value,
            string name)
        {
            if (float.IsNaN(value)
                || float.IsInfinity(value)
                || value < 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }
}
