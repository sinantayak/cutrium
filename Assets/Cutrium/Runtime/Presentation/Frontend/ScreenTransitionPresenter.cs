using System;
using UnityEngine;

namespace Cutrium.Presentation.Frontend
{
    /// Covers same-scene presentation changes with one full-screen fade.
    /// The supplied action executes exactly once while the overlay is fully
    /// opaque, keeping the old and new screen from flashing in the same frame.
    [DisallowMultipleComponent]
    public sealed class ScreenTransitionPresenter : MonoBehaviour
    {
        private enum TransitionPhase
        {
            Idle,
            Covering,
            Holding,
            Revealing,
        }

        [SerializeField] private CanvasGroup _overlayCanvasGroup;
        [SerializeField] [Min(0f)] private float _coverSeconds = 0.18f;
        [SerializeField] [Min(0f)] private float _coveredHoldSeconds = 0.04f;
        [SerializeField] [Min(0f)] private float _revealSeconds = 0.24f;

        private TransitionPhase _phase;
        private float _phaseElapsed;
        private Action _midpointAction;

        public CanvasGroup OverlayCanvasGroup => _overlayCanvasGroup;
        public float CoverSeconds => _coverSeconds;
        public float CoveredHoldSeconds => _coveredHoldSeconds;
        public float RevealSeconds => _revealSeconds;
        public bool IsTransitioning => _phase != TransitionPhase.Idle;

        public void ConfigureForSetup(
            CanvasGroup overlayCanvasGroup,
            float coverSeconds = 0.18f,
            float coveredHoldSeconds = 0.04f,
            float revealSeconds = 0.24f)
        {
            ValidateDuration(coverSeconds, nameof(coverSeconds));
            ValidateDuration(coveredHoldSeconds, nameof(coveredHoldSeconds));
            ValidateDuration(revealSeconds, nameof(revealSeconds));
            _overlayCanvasGroup = overlayCanvasGroup
                ?? throw new ArgumentNullException(
                    nameof(overlayCanvasGroup));
            _coverSeconds = coverSeconds;
            _coveredHoldSeconds = coveredHoldSeconds;
            _revealSeconds = revealSeconds;
            CancelTransition();
        }

        /// Returns false when another transition already owns the overlay.
        /// Callers should treat that as consumed input, not execute the action
        /// immediately and bypass the active transition.
        public bool TryTransition(Action midpointAction)
        {
            if (midpointAction == null)
            {
                throw new ArgumentNullException(nameof(midpointAction));
            }

            if (IsTransitioning)
            {
                return false;
            }

            if (_overlayCanvasGroup == null || !isActiveAndEnabled)
            {
                midpointAction();
                return true;
            }

            _midpointAction = midpointAction;
            _phase = TransitionPhase.Covering;
            _phaseElapsed = 0f;
            SetOverlay(0f, true);
            Advance(0f);
            return true;
        }

        /// Deterministic unscaled-time step shared by Update and Play Mode
        /// tests. A large step may cross several phases without skipping the
        /// covered midpoint action.
        public void Advance(float elapsedSeconds)
        {
            ValidateDuration(elapsedSeconds, nameof(elapsedSeconds));
            float remaining = elapsedSeconds;
            int guard = 0;
            while (IsTransitioning && guard++ < 8)
            {
                float duration = CurrentPhaseDuration();
                if (duration <= 0f)
                {
                    CompleteCurrentPhase();
                    continue;
                }

                float available = Mathf.Max(0f, duration - _phaseElapsed);
                float consumed = Mathf.Min(remaining, available);
                _phaseElapsed += consumed;
                remaining -= consumed;
                RefreshOverlayForCurrentPhase(duration);
                if (_phaseElapsed + 0.00001f < duration)
                {
                    break;
                }

                CompleteCurrentPhase();
                if (remaining <= 0f)
                {
                    // Continue only through zero-duration phases.
                    if (!IsTransitioning || CurrentPhaseDuration() > 0f)
                    {
                        break;
                    }
                }
            }
        }

        public void CancelTransition()
        {
            _midpointAction = null;
            _phase = TransitionPhase.Idle;
            _phaseElapsed = 0f;
            SetOverlay(0f, false);
        }

        private void Update()
        {
            Advance(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            CancelTransition();
        }

        private float CurrentPhaseDuration() => _phase switch
        {
            TransitionPhase.Covering => _coverSeconds,
            TransitionPhase.Holding => _coveredHoldSeconds,
            TransitionPhase.Revealing => _revealSeconds,
            _ => 0f,
        };

        private void RefreshOverlayForCurrentPhase(float duration)
        {
            float progress = Mathf.Clamp01(_phaseElapsed / duration);
            switch (_phase)
            {
                case TransitionPhase.Covering:
                    SetOverlay(Mathf.SmoothStep(0f, 1f, progress), true);
                    break;
                case TransitionPhase.Holding:
                    SetOverlay(1f, true);
                    break;
                case TransitionPhase.Revealing:
                    SetOverlay(
                        1f - Mathf.SmoothStep(0f, 1f, progress),
                        true);
                    break;
            }
        }

        private void CompleteCurrentPhase()
        {
            _phaseElapsed = 0f;
            switch (_phase)
            {
                case TransitionPhase.Covering:
                    SetOverlay(1f, true);
                    Action midpoint = _midpointAction;
                    _midpointAction = null;
                    try
                    {
                        midpoint?.Invoke();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, this);
                    }

                    if (this != null && _phase != TransitionPhase.Idle)
                    {
                        _phase = TransitionPhase.Holding;
                    }
                    break;
                case TransitionPhase.Holding:
                    _phase = TransitionPhase.Revealing;
                    SetOverlay(1f, true);
                    break;
                case TransitionPhase.Revealing:
                    _phase = TransitionPhase.Idle;
                    SetOverlay(0f, false);
                    break;
            }
        }

        private void SetOverlay(float alpha, bool blocksInput)
        {
            if (_overlayCanvasGroup == null)
            {
                return;
            }

            _overlayCanvasGroup.alpha = Mathf.Clamp01(alpha);
            _overlayCanvasGroup.interactable = false;
            _overlayCanvasGroup.blocksRaycasts = blocksInput;
        }

        private static void ValidateDuration(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }
}
