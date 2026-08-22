using UnityEngine;

namespace Cutrium.Presentation.Frontend
{
    [DisallowMultipleComponent]
    public sealed class FrontEndPulseAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField, Min(0.05f)] private float _cyclesPerSecond = 0.8f;
        [SerializeField, Range(0f, 0.25f)]
        private float _scaleAmplitude = 0.04f;
        [SerializeField, Range(0f, 1f)] private float _minimumAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float _maximumAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float _phaseOffset;

        private Vector3 _baseScale = Vector3.one;
        private float _startedAt;
        private bool _hasBaseScale;

        public void ConfigureForSetup(
            RectTransform target,
            CanvasGroup canvasGroup,
            float cyclesPerSecond,
            float scaleAmplitude,
            float minimumAlpha,
            float maximumAlpha,
            float phaseOffset = 0f)
        {
            bool targetChanged = _target != target;
            _target = target;
            _canvasGroup = canvasGroup;
            _cyclesPerSecond = Mathf.Max(0.05f, cyclesPerSecond);
            _scaleAmplitude = Mathf.Clamp(scaleAmplitude, 0f, 0.25f);
            _minimumAlpha = Mathf.Clamp01(minimumAlpha);
            _maximumAlpha = Mathf.Clamp(
                maximumAlpha,
                _minimumAlpha,
                1f);
            _phaseOffset = Mathf.Repeat(phaseOffset, 1f);
            if (!_hasBaseScale || targetChanged)
            {
                CaptureBaseScale();
            }
        }

        private void OnEnable()
        {
            CaptureBaseScale();
            _startedAt = Time.unscaledTime;
            ApplyPulse(0f);
        }

        private void Update()
        {
            ApplyPulse(Time.unscaledTime - _startedAt);
        }

        private void OnDisable()
        {
            if (_target != null && _hasBaseScale)
            {
                _target.localScale = _baseScale;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        private void CaptureBaseScale()
        {
            if (_target == null)
            {
                _target = transform as RectTransform;
            }

            if (_target != null)
            {
                _baseScale = _target.localScale;
                _hasBaseScale = true;
            }
        }

        private void ApplyPulse(float elapsed)
        {
            float angle = (elapsed * _cyclesPerSecond + _phaseOffset)
                * Mathf.PI
                * 2f
                - Mathf.PI * 0.5f;
            float normalized = (Mathf.Sin(angle) + 1f) * 0.5f;
            float eased = normalized * normalized * (3f - 2f * normalized);

            if (_target != null && _hasBaseScale)
            {
                _target.localScale = _baseScale
                    * (1f + _scaleAmplitude * eased);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Lerp(
                    _minimumAlpha,
                    _maximumAlpha,
                    eased);
            }
        }
    }
}
