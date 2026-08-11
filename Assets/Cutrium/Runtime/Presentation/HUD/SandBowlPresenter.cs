using Cutrium.Gameplay.Session;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.HUD
{
    /// Drives the BottomHUD sand bowl: a rising sand-fill level (clipped
    /// to the bowl's silhouette by a UI Mask elsewhere in the hierarchy)
    /// that always mirrors `Session.CapturedFraction` exactly, and a
    /// target-percentage readout. Deliberately independent of
    /// `LandmarkRevealPresenter`'s cosmetic sand-grain flight animation
    /// (see ADR-026) -- the fill level here is the single source of truth
    /// for "how full is the bowl", never derived from arrived particles,
    /// so it can never lag or desync from real captured progress.
    [DisallowMultipleComponent]
    public sealed class SandBowlPresenter : MonoBehaviour
    {
        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private RectTransform _sandFillRect;
        [SerializeField] private Text _targetText;
        [SerializeField] private RectTransform _fillTargetRect;

        public FirstPlayableController Controller => _controller;
        public RectTransform SandFillRect => _sandFillRect;
        public Text TargetText => _targetText;

        /// The point sand-grain particles from `LandmarkRevealPresenter`
        /// visually fly toward. Purely a positional reference -- reading
        /// it never affects the fill level above.
        public RectTransform FillTargetRect => _fillTargetRect;

        public float CurrentFillFraction =>
            _sandFillRect != null ? _sandFillRect.anchorMax.y : 0f;

        public void Configure(
            FirstPlayableController controller,
            RectTransform sandFillRect,
            Text targetText,
            RectTransform fillTargetRect)
        {
            _controller = controller;
            _sandFillRect = sandFillRect;
            _targetText = targetText;
            _fillTargetRect = fillTargetRect;
            RefreshNow();
        }

        public void RefreshNow()
        {
            if (_controller == null || _controller.Session == null)
            {
                return;
            }

            float captured = _controller.Session.CapturedFraction;
            float target = _controller.Session.TargetCapturedFraction;

            if (_sandFillRect != null)
            {
                Vector2 anchorMax = _sandFillRect.anchorMax;
                anchorMax.y = Mathf.Clamp01(captured);
                _sandFillRect.anchorMax = anchorMax;
            }

            if (_targetText != null)
            {
                _targetText.text = $"Target {RoundedPercent(target)}%";
            }
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private static int RoundedPercent(float fraction) =>
            Mathf.FloorToInt((fraction * 100f) + 0.5f);
    }
}
