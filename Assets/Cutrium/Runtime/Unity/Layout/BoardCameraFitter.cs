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
        private RectTransform _boardViewport;

        [SerializeField]
        private RectTransform _boardFrame;

        private Rect _lastViewportScreenRect;
        private Vector2 _lastScreenSize;
        private bool _hasAppliedLayout;

        public Camera BoardCamera => _boardCamera;

        public Canvas Canvas => _canvas;

        public RectTransform BoardViewport => _boardViewport;

        public RectTransform BoardFrame => _boardFrame;

        public Vector2 LogicalBoardSize => BoardViewportLayout.LogicalSize;

        public Rect ViewportScreenRect { get; private set; }

        public Rect BoardScreenRect { get; private set; }

        public int AppliedLayoutCount { get; private set; }

        public void Configure(
            Camera boardCamera,
            Canvas canvas,
            RectTransform boardViewport,
            RectTransform boardFrame)
        {
            _boardCamera = boardCamera;
            _canvas = canvas;
            _boardViewport = boardViewport;
            _boardFrame = boardFrame;
            _hasAppliedLayout = false;
        }

        public bool Apply(Rect viewportScreenRect, Vector2 screenSize)
        {
            if (_boardCamera == null
                || _boardViewport == null
                || _boardFrame == null
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
                BoardViewportLayout.CalculateAspectFitRect(viewportScreenRect);
            Rect localBoardRect =
                BoardViewportLayout.CalculateAspectFitRect(_boardViewport.rect);

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

            _boardFrame.anchorMin = new Vector2(0.5f, 0.5f);
            _boardFrame.anchorMax = new Vector2(0.5f, 0.5f);
            _boardFrame.pivot = new Vector2(0.5f, 0.5f);
            _boardFrame.anchoredPosition =
                localBoardRect.center - _boardViewport.rect.center;
            _boardFrame.sizeDelta = localBoardRect.size;

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
            if (_boardViewport == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return false;
            }

            _boardViewport.GetWorldCorners(_viewportCorners);
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
