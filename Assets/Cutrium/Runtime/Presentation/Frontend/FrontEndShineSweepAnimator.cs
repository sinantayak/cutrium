using UnityEngine;

namespace Cutrium.Presentation.Frontend
{
    /// Drives a FrontEndShineSweepGraphic through a looping sweep-then-pause
    /// cycle: the band crosses the icon once, then stays hidden for a
    /// pause before the next pass, so a row of icons don't all glint at
    /// once with the same phase offset.
    [DisallowMultipleComponent]
    public sealed class FrontEndShineSweepAnimator : MonoBehaviour
    {
        [SerializeField] private FrontEndShineSweepGraphic _graphic;
        [SerializeField, Min(0.1f)] private float _sweepSeconds = 1.1f;
        [SerializeField, Min(0f)] private float _pauseSeconds = 1.9f;
        [SerializeField, Range(0f, 1f)] private float _phaseOffset;

        private float _startedAt;

        public void ConfigureForSetup(
            FrontEndShineSweepGraphic graphic,
            float sweepSeconds,
            float pauseSeconds,
            float phaseOffset = 0f)
        {
            _graphic = graphic;
            _sweepSeconds = Mathf.Max(0.1f, sweepSeconds);
            _pauseSeconds = Mathf.Max(0f, pauseSeconds);
            _phaseOffset = Mathf.Repeat(phaseOffset, 1f);
        }

        private void OnEnable()
        {
            _startedAt = Time.unscaledTime;
            Apply(0f);
        }

        private void Update()
        {
            Apply(Time.unscaledTime - _startedAt);
        }

        private void OnDisable() => _graphic?.SetProgress(0f);

        private void Apply(float elapsedSinceEnable)
        {
            float cycle = _sweepSeconds + _pauseSeconds;
            if (_graphic == null || cycle <= 0f)
            {
                return;
            }

            float cursor = Mathf.Repeat(
                elapsedSinceEnable + _phaseOffset * cycle,
                cycle);
            float progress = cursor <= _sweepSeconds
                ? EaseInOut(cursor / _sweepSeconds)
                : 0f;
            _graphic.SetProgress(progress);
        }

        private static float EaseInOut(float value) =>
            value * value * (3f - 2f * value);
    }
}
