using UnityEngine;

namespace Cutrium.Unity.Layout
{
    [DisallowMultipleComponent]
    public sealed class BoardCameraFitter : MonoBehaviour
    {
        private readonly Vector3[] _viewportCorners = new Vector3[4];

        [SerializeField]
        private Camera _boardCamera;

        [SerializeField]
        private Canvas _canvas;

        [SerializeField]
        private RectTransform _boardStage;

        [SerializeField]
        private RectTransform _boardViewport;

        [SerializeField]
        private RectTransform _boardFrame;

        [SerializeField]
        [Range(0f, 1f)]
        private float _verticalAlignment = 0.5f;

        private Rect _lastViewportScreenRect;
        private Vector2 _lastScreenSize;
        private bool _hasAppliedLayout;

        public Camera BoardCamera => _boardCamera;

        public Canvas Canvas => _canvas;

        /// The stable, VerticalLayoutGroup-controlled slot that always
        /// receives the full available board area for the current screen,
        /// regardless of its own aspect ratio. Used only to read "how much
        /// space is available" -- never resized to the fitted rect itself,
        /// so it remains a correct reference across resolution/orientation
        /// changes.
        public RectTransform BoardStage => _boardStage;

        /// The visual board shell: resized every Apply() to exactly the
        /// 10:16 aspect-fitted rect within BoardStage. BoardFrame (and
        /// therefore everything anchored to it -- board surface, landmark
        /// artwork, veil layer, barriers, threats) is a plain full-stretch
        /// child of this, so it always shares the exact same final rect.
        public RectTransform BoardViewport => _boardViewport;

        public RectTransform BoardFrame => _boardFrame;

        public float VerticalAlignment => _verticalAlignment;

        public Vector2 LogicalBoardSize => BoardViewportLayout.LogicalSize;

        public Rect ViewportScreenRect { get; private set; }

        public Rect BoardScreenRect { get; private set; }

        public int AppliedLayoutCount { get; private set; }

        public void Configure(
            Camera boardCamera,
            Canvas canvas,
            RectTransform boardStage,
            RectTransform boardViewport,
            RectTransform boardFrame)
        {
            _boardCamera = boardCamera;
            _canvas = canvas;
            _boardStage = boardStage;
            _boardViewport = boardViewport;
            _boardFrame = boardFrame;
            _hasAppliedLayout = false;
        }

        public void ConfigureVerticalAlignmentForSetup(float verticalAlignment)
        {
            if (float.IsNaN(verticalAlignment)
                || float.IsInfinity(verticalAlignment)
                || verticalAlignment < 0f
                || verticalAlignment > 1f)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(verticalAlignment));
            }

            _verticalAlignment = verticalAlignment;
            _hasAppliedLayout = false;
        }

        public bool Apply(Rect viewportScreenRect, Vector2 screenSize)
        {
            if (_boardCamera == null
                || _boardStage == null
                || _boardViewport == null
                || screenSize.x <= 0f
                || screenSize.y <= 0f
                || viewportScreenRect.width <= 0f
                || viewportScreenRect.height <= 0f)
            {
                return false;
            }

            if (_hasAppliedLayout
                && _lastViewportScreenRect == viewportScreenRect
                && _lastScreenSize == screenSize)
            {
                return false;
            }

            Rect boardScreenRect =
                BoardViewportLayout.CalculateAspectFitRect(
                    viewportScreenRect,
                    _verticalAlignment);
            Rect localBoardRect =
                BoardViewportLayout.CalculateAspectFitRect(
                    _boardStage.rect,
                    _verticalAlignment);

            _boardCamera.rect = new Rect(
                viewportScreenRect.x / screenSize.x,
                viewportScreenRect.y / screenSize.y,
                viewportScreenRect.width / screenSize.x,
                viewportScreenRect.height / screenSize.y);
            _boardCamera.orthographic = true;
            _boardCamera.orthographicSize =
                BoardViewportLayout.CalculateOrthographicSize(viewportScreenRect);
            _boardCamera.transform.position = new Vector3(
                BoardViewportLayout.LogicalWidth * 0.5f,
                BoardViewportLayout.LogicalHeight * 0.5f,
                -10f);

            // BoardViewport becomes exactly the fitted rect within
            // BoardStage -- there is no larger container left to
            // letterbox inside. BoardFrame stays a plain full-stretch
            // child of BoardViewport (configured once at scene-setup
            // time), so it automatically shares this same final rect
            // without needing its own per-frame sizing here anymore.
            _boardViewport.anchorMin = new Vector2(0.5f, 0.5f);
            _boardViewport.anchorMax = new Vector2(0.5f, 0.5f);
            _boardViewport.pivot = new Vector2(0.5f, 0.5f);
            _boardViewport.anchoredPosition =
                localBoardRect.center - _boardStage.rect.center;
            _boardViewport.sizeDelta = localBoardRect.size;

            ViewportScreenRect = viewportScreenRect;
            BoardScreenRect = boardScreenRect;
            _lastViewportScreenRect = viewportScreenRect;
            _lastScreenSize = screenSize;
            _hasAppliedLayout = true;
            AppliedLayoutCount++;
            return true;
        }

        public bool RefreshNow()
        {
            if (_boardStage == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return false;
            }

            _boardStage.GetWorldCorners(_viewportCorners);
            Camera canvasCamera =
                _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? _canvas.worldCamera
                    : null;
            Vector2 minimum =
                RectTransformUtility.WorldToScreenPoint(canvasCamera, _viewportCorners[0]);
            Vector2 maximum =
                RectTransformUtility.WorldToScreenPoint(canvasCamera, _viewportCorners[2]);
            Rect viewportScreenRect = Rect.MinMaxRect(
                minimum.x,
                minimum.y,
                maximum.x,
                maximum.y);

            return Apply(
                viewportScreenRect,
                new Vector2(Screen.width, Screen.height));
        }

        private void OnEnable()
        {
            _hasAppliedLayout = false;
        }

        private void LateUpdate()
        {
            RefreshNow();
        }
    }
}
