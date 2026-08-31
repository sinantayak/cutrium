using System;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using UnityEngine;

namespace Cutrium.Unity.Input
{
    [DisallowMultipleComponent]
    public sealed class BarrierGestureAdapter : MonoBehaviour
    {
        [SerializeField]
        private PointerInputAdapter _pointerInput;

        [SerializeField]
        private float _selectionDeadZone = 0.35f;

        [SerializeField]
        private float _orientationHysteresis = 0.1f;

        private bool _subscribed;

        public event Action<BarrierIntent> IntentCommitted;

        public event Action<LogicalPoint> PointCommitted;

        public event Action<LogicalPoint> InteractionStarted;

        public event Action<BarrierOrientation> OrientationChanged;

        public PointerInputAdapter PointerInput => _pointerInput;
        public float SelectionDeadZone => _selectionDeadZone;
        public float OrientationHysteresis => _orientationHysteresis;
        public bool IsTracking { get; private set; }
        public LogicalPoint Origin { get; private set; }
        public LogicalPoint CurrentPoint { get; private set; }
        public BarrierOrientation SelectedOrientation { get; private set; }
        public BarrierOrientation RequiredOrientation { get; private set; }
        public LogicalPoint? RequiredOrigin { get; private set; }
        public float RequiredOriginTolerance { get; private set; } = 1.5f;
        public bool InputSuppressed { get; private set; }
        public int CommittedIntentCount { get; private set; }
        public int CancelledInteractionCount { get; private set; }
        public bool IsPointTargeting { get; private set; }

        public void SetPointTargeting(bool enabled)
        {
            if (IsPointTargeting == enabled)
            {
                return;
            }

            IsPointTargeting = enabled;
            ResetTracking();
        }

        public void SetRequiredOrientation(BarrierOrientation orientation)
        {
            if (orientation != BarrierOrientation.None
                && orientation != BarrierOrientation.Horizontal
                && orientation != BarrierOrientation.Vertical)
            {
                throw new ArgumentOutOfRangeException(nameof(orientation));
            }

            RequiredOrientation = orientation;
        }

        /// <summary>
        /// When set, an interaction may only START within
        /// <paramref name="tolerance"/> logical units of
        /// <paramref name="origin"/> (a start elsewhere is silently
        /// ignored), and a committed intent's origin is snapped to exactly
        /// that point regardless of where inside the tolerance the player
        /// actually touched. Used to make a taught cut land on the same
        /// fixed spot for every player.
        /// </summary>
        public void SetRequiredOrigin(LogicalPoint? origin, float tolerance = 1.5f)
        {
            if (origin.HasValue
                && (!IsFinite(tolerance) || tolerance < 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(tolerance));
            }

            RequiredOrigin = origin;
            RequiredOriginTolerance = tolerance;
        }

        /// <summary>
        /// When true, every sample is ignored and no interaction can start
        /// or continue — used for a brief "look, don't touch" beat where
        /// the board must not react to input at all.
        /// </summary>
        public void SetInputSuppressed(bool suppressed)
        {
            InputSuppressed = suppressed;
            if (suppressed)
            {
                ResetTracking();
            }
        }

        public void Configure(
            PointerInputAdapter pointerInput,
            float selectionDeadZone,
            float orientationHysteresis)
        {
            if (!IsFinite(selectionDeadZone) || selectionDeadZone <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(selectionDeadZone));
            }

            if (!IsFinite(orientationHysteresis)
                || orientationHysteresis < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orientationHysteresis));
            }

            if (_subscribed)
            {
                Unsubscribe();
            }

            _pointerInput = pointerInput;
            _selectionDeadZone = selectionDeadZone;
            _orientationHysteresis = orientationHysteresis;
            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
            }
        }

        public void ProcessSample(PointerSample sample)
        {
            switch (sample.Phase)
            {
                case PointerSamplePhase.Started:
                    Begin(sample);
                    break;
                case PointerSamplePhase.Moved:
                    UpdateSelection(sample);
                    break;
                case PointerSamplePhase.Released:
                    Release(sample);
                    break;
                case PointerSamplePhase.Cancelled:
                    Cancel();
                    break;
            }
        }

        public void ResetForRetry()
        {
            IsPointTargeting = false;
            RequiredOrientation = BarrierOrientation.None;
            RequiredOrigin = null;
            InputSuppressed = false;
            ResetTracking();
            CommittedIntentCount = 0;
            CancelledInteractionCount = 0;
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
            ResetTracking();
        }

        private void Begin(PointerSample sample)
        {
            ResetTracking();
            if (InputSuppressed || !sample.IsAcceptedBoardStart)
            {
                return;
            }

            if (!IsPointTargeting
                && RequiredOrigin.HasValue
                && (sample.LogicalPoint - RequiredOrigin.Value).Length
                    > RequiredOriginTolerance)
            {
                // Outside the taught spot: ignore the touch entirely rather
                // than starting (and then cancelling) an interaction.
                return;
            }

            IsTracking = true;
            Origin = sample.LogicalPoint;
            CurrentPoint = sample.LogicalPoint;
            InteractionStarted?.Invoke(Origin);
        }

        private void UpdateSelection(PointerSample sample)
        {
            if (!IsTracking || !sample.IsInsideBoard)
            {
                return;
            }

            CurrentPoint = sample.LogicalPoint;
            if (IsPointTargeting)
            {
                return;
            }

            float horizontal = Math.Abs(CurrentPoint.X - Origin.X);
            float vertical = Math.Abs(CurrentPoint.Y - Origin.Y);
            if (SelectedOrientation == BarrierOrientation.None)
            {
                if (Math.Max(horizontal, vertical) < _selectionDeadZone)
                {
                    return;
                }

                SetSelectedOrientation(horizontal >= vertical
                    ? BarrierOrientation.Horizontal
                    : BarrierOrientation.Vertical);
                return;
            }

            if (SelectedOrientation == BarrierOrientation.Horizontal
                && vertical > horizontal + _orientationHysteresis
                && vertical >= _selectionDeadZone)
            {
                SetSelectedOrientation(BarrierOrientation.Vertical);
            }
            else if (SelectedOrientation == BarrierOrientation.Vertical
                && horizontal > vertical + _orientationHysteresis
                && horizontal >= _selectionDeadZone)
            {
                SetSelectedOrientation(BarrierOrientation.Horizontal);
            }
        }

        private void SetSelectedOrientation(
            BarrierOrientation orientation)
        {
            if (SelectedOrientation == orientation)
            {
                return;
            }

            SelectedOrientation = orientation;
            OrientationChanged?.Invoke(orientation);
        }

        private void Release(PointerSample sample)
        {
            if (!IsTracking)
            {
                return;
            }

            UpdateSelection(sample);
            if (IsPointTargeting)
            {
                if (!sample.IsInsideBoard)
                {
                    CancelledInteractionCount++;
                    ResetTracking();
                    return;
                }

                LogicalPoint point = sample.LogicalPoint;
                ResetTracking();
                PointCommitted?.Invoke(point);
                return;
            }

            if (SelectedOrientation == BarrierOrientation.None)
            {
                CancelledInteractionCount++;
                ResetTracking();
                return;
            }

            if (RequiredOrientation != BarrierOrientation.None
                && RequiredOrientation != SelectedOrientation)
            {
                CancelledInteractionCount++;
                ResetTracking();
                return;
            }

            LogicalPoint intentOrigin = RequiredOrigin ?? Origin;
            var intent = new BarrierIntent(intentOrigin, SelectedOrientation);
            CommittedIntentCount++;
            ResetTracking();
            IntentCommitted?.Invoke(intent);
        }

        private void Cancel()
        {
            if (IsTracking)
            {
                CancelledInteractionCount++;
            }

            ResetTracking();
        }

        private void Subscribe()
        {
            if (_subscribed || _pointerInput == null)
            {
                return;
            }

            _pointerInput.Sampled += ProcessSample;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_pointerInput != null)
            {
                _pointerInput.Sampled -= ProcessSample;
            }

            _subscribed = false;
        }

        private void ResetTracking()
        {
            IsTracking = false;
            Origin = default;
            CurrentPoint = default;
            SelectedOrientation = BarrierOrientation.None;
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
