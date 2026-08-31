using System;
using UnityEngine;

namespace Cutrium.Presentation.HUD
{
    /// <summary>
    /// Pulses an explicitly supplied UI target's own scale up and down in
    /// place, rather than drawing a separate frame around it — so the
    /// highlighted HUD element (a heart row, a speed readout, a progress
    /// bar) stays fully visible and readable while it's being pointed at.
    /// Owns only unscaled presentation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrainingFocusHighlightPresenter : MonoBehaviour
    {
        [SerializeField] [Min(0.01f)] private float _cycleSeconds = 0.9f;
        [SerializeField] [Range(0f, 1f)] private float _scalePulse = 0.14f;

        private RectTransform _target;
        private Vector3 _targetHomeScale = Vector3.one;
        private float _elapsed;

        public RectTransform Target => _target;
        public bool IsVisible => _target != null;

        private void OnDisable()
        {
            Hide();
        }

        private void LateUpdate()
        {
            AdvancePresentation(Time.unscaledDeltaTime);
        }

        public void ConfigureForSetup(
            float cycleSeconds = 0.9f,
            float scalePulse = 0.14f)
        {
            ValidatePositive(cycleSeconds, nameof(cycleSeconds));
            ValidateUnit(scalePulse, nameof(scalePulse));

            _cycleSeconds = cycleSeconds;
            _scalePulse = scalePulse;
            Hide();
        }

        public void Show(RectTransform target)
        {
            if (target == null)
            {
                Hide();
                return;
            }

            if (!ReferenceEquals(_target, target))
            {
                RestoreTargetScale();
                _target = target;
                _targetHomeScale = target.localScale;
                _elapsed = 0f;
            }

            ApplyPulse(0f);
        }

        public void Hide()
        {
            RestoreTargetScale();
            _target = null;
            _elapsed = 0f;
        }

        public void AdvancePresentation(float unscaledDeltaTime)
        {
            ValidateNonNegative(
                unscaledDeltaTime,
                nameof(unscaledDeltaTime));
            if (_target == null)
            {
                return;
            }

            _elapsed += unscaledDeltaTime;
            float phase = (_elapsed / _cycleSeconds) * Mathf.PI * 2f;
            ApplyPulse(0.5f + 0.5f * Mathf.Sin(phase));
        }

        private void ApplyPulse(float pulse)
        {
            if (_target == null)
            {
                return;
            }

            float scale = 1f + (_scalePulse * pulse);
            _target.localScale = _targetHomeScale * scale;
        }

        private void RestoreTargetScale()
        {
            if (_target != null)
            {
                _target.localScale = _targetHomeScale;
            }
        }

        private static void ValidatePositive(float value, string name)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static void ValidateNonNegative(float value, string name)
        {
            if (!IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static void ValidateUnit(float value, string name)
        {
            if (!IsFinite(value) || value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
