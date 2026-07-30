using UnityEngine;

namespace Cutrium.Unity.Layout
{
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _target;

        private Rect _lastSafeArea;
        private Vector2 _lastScreenSize;
        private bool _hasAppliedLayout;

        public RectTransform Target => _target;

        public int AppliedLayoutCount { get; private set; }

        public void Configure(RectTransform target)
        {
            _target = target;
            _hasAppliedLayout = false;
        }

        public bool Apply(Rect safeArea, Vector2 screenSize)
        {
            if (_target == null)
            {
                return false;
            }

            if (_hasAppliedLayout
                && _lastSafeArea == safeArea
                && _lastScreenSize == screenSize)
            {
                return false;
            }

            SafeAreaAnchors anchors = SafeAreaLayout.CalculateAnchors(safeArea, screenSize);
            _target.anchorMin = anchors.Minimum;
            _target.anchorMax = anchors.Maximum;
            _target.offsetMin = Vector2.zero;
            _target.offsetMax = Vector2.zero;

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;
            _hasAppliedLayout = true;
            AppliedLayoutCount++;
            return true;
        }

        private void OnEnable()
        {
            _hasAppliedLayout = false;
            ApplyCurrentSafeArea();
        }

        private void Update()
        {
            ApplyCurrentSafeArea();
        }

        private void ApplyCurrentSafeArea()
        {
            if (Screen.width > 0 && Screen.height > 0)
            {
                Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
            }
        }
    }
}
