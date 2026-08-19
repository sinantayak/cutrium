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

        public PointerInputAdapter PointerInput => _pointerInput;
        public float SelectionDeadZone => _selectionDeadZone;
        public float OrientationHysteresis => _orientationHysteresis;
        public bool IsTracking { get; private set; }
        public LogicalPoint Origin { get; private set; }
        public LogicalPoint CurrentPoint { get; private set; }
        public BarrierOrientation SelectedOrientation { get; private set; }
        public int CommittedIntentCount { get; private set; }
        public int CancelledInteractionCount { get; private set; }
        public bool IsPointTargeting { get; private set; }

        public void SetPointTargeting(bool enabled)
        {
            IsPointTargeting = enabled;
            ResetTracking();
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
            if (!sample.IsAcceptedBoardStart)
            {
                return;
            }

            IsTracking = true;
            Origin = sample.LogicalPoint;
            CurrentPoint = sample.LogicalPoint;
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

                SelectedOrientation = horizontal >= vertical
                    ? BarrierOrientation.Horizontal
                    : BarrierOrientation.Vertical;
                return;
            }

            if (SelectedOrientation == BarrierOrientation.Horizontal
                && vertical > horizontal + _orientationHysteresis
                && vertical >= _selectionDeadZone)
            {
                SelectedOrientation = BarrierOrientation.Vertical;
            }
            else if (SelectedOrientation == BarrierOrientation.Vertical
                && horizontal > vertical + _orientationHysteresis
                && horizontal >= _selectionDeadZone)
            {
                SelectedOrientation = BarrierOrientation.Horizontal;
            }
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

            var intent = new BarrierIntent(Origin, SelectedOrientation);
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
