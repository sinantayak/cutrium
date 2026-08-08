using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Landmark
{
    public readonly struct LandmarkCompletionTiming
    {
        public LandmarkCompletionTiming(
            float scrimFadeSeconds,
            float contentDelaySeconds,
            float contentFadeSeconds,
            float buttonsDelaySeconds,
            float buttonsFadeSeconds)
        {
            ScrimFadeSeconds = scrimFadeSeconds;
            ContentDelaySeconds = contentDelaySeconds;
            ContentFadeSeconds = contentFadeSeconds;
            ButtonsDelaySeconds = buttonsDelaySeconds;
            ButtonsFadeSeconds = buttonsFadeSeconds;
        }

        public float ScrimFadeSeconds { get; }
        public float ContentDelaySeconds { get; }
        public float ContentFadeSeconds { get; }
        public float ButtonsDelaySeconds { get; }
        public float ButtonsFadeSeconds { get; }

        public static LandmarkCompletionTiming Default { get; } =
            new LandmarkCompletionTiming(0.3f, 0.15f, 0.35f, 0.5f, 0.25f);
    }

    [DisallowMultipleComponent]
    public sealed class LandmarkRevealPresenter : MonoBehaviour
    {
        private const float ContentSlideOffset = 18f;

        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private RectTransform _boardFrame;
        [SerializeField] private Image _artworkImage;
        [SerializeField] private RectTransform _veilRoot;
        [SerializeField] private Sprite _veilSprite;
        [SerializeField] private Color _veilColor = new Color(0.09f, 0.11f, 0.15f, 0.94f);
        [SerializeField] [Min(0f)] private float _revealFadeSeconds = 0.35f;

        [SerializeField] private Image _heroArtworkImage;
        [SerializeField] private CanvasGroup _scrimCanvasGroup;
        [SerializeField] private CanvasGroup _contentCanvasGroup;
        [SerializeField] private CanvasGroup _statsCanvasGroup;
        [SerializeField] private CanvasGroup _retryCanvasGroup;
        [SerializeField] private CanvasGroup _nextCanvasGroup;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _sectorText;
        [SerializeField] private LandmarkCompletionTiming _timing =
            LandmarkCompletionTiming.Default;
        [SerializeField] private LandmarkDefinition[] _landmarks = Array.Empty<LandmarkDefinition>();

        private readonly Dictionary<RoomId, VeilView> _veilViews =
            new Dictionary<RoomId, VeilView>();
        private readonly List<VeilView> _availableViews = new List<VeilView>();
        private readonly List<RoomId> _staleIds = new List<RoomId>();
        private bool _wasCompleted;
        private float _completionRevealStartTime;

        public FirstPlayableController Controller => _controller;
        public RectTransform BoardFrame => _boardFrame;
        public Image ArtworkImage => _artworkImage;
        public RectTransform VeilRoot => _veilRoot;
        public float RevealFadeSeconds => _revealFadeSeconds;
        public Image CompletionArtworkImage => _heroArtworkImage;
        public CanvasGroup ScrimCanvasGroup => _scrimCanvasGroup;
        public CanvasGroup ContentCanvasGroup => _contentCanvasGroup;
        public CanvasGroup StatsCanvasGroup => _statsCanvasGroup;
        public CanvasGroup RetryCanvasGroup => _retryCanvasGroup;
        public CanvasGroup NextCanvasGroup => _nextCanvasGroup;
        public Text CompletionTitleText => _titleText;
        public Text CompletionDescriptionText => _descriptionText;
        public Text CompletionSectorText => _sectorText;
        public LandmarkCompletionTiming Timing => _timing;
        public IReadOnlyList<LandmarkDefinition> Landmarks => _landmarks;
        public LandmarkDefinition CurrentLandmark { get; private set; }
        public int VisibleVeilCount { get; private set; }
        public float CompletionSequenceElapsedSeconds =>
            _wasCompleted ? Time.unscaledTime - _completionRevealStartTime : 0f;

        public bool AllVeilsFullyRevealed
        {
            get
            {
                foreach (KeyValuePair<RoomId, VeilView> pair in _veilViews)
                {
                    if (pair.Value.CanvasGroup.alpha > 0f)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void Configure(
            FirstPlayableController controller,
            RectTransform boardFrame,
            Image artworkImage,
            RectTransform veilRoot,
            Sprite veilSprite,
            Color veilColor,
            float revealFadeSeconds,
            Image heroArtworkImage,
            CanvasGroup scrimCanvasGroup,
            CanvasGroup contentCanvasGroup,
            CanvasGroup statsCanvasGroup,
            CanvasGroup retryCanvasGroup,
            CanvasGroup nextCanvasGroup,
            Text titleText,
            Text sectorText,
            Text descriptionText,
            LandmarkCompletionTiming timing,
            IReadOnlyList<LandmarkDefinition> landmarks)
        {
            if (landmarks == null || landmarks.Count == 0)
            {
                throw new ArgumentException(
                    "Landmark reveal needs at least one landmark.",
                    nameof(landmarks));
            }

            if (!IsFinite(revealFadeSeconds) || revealFadeSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(revealFadeSeconds));
            }

            _controller = controller;
            _boardFrame = boardFrame;
            _artworkImage = artworkImage;
            _veilRoot = veilRoot;
            _veilSprite = veilSprite;
            _veilColor = veilColor;
            _revealFadeSeconds = revealFadeSeconds;
            _heroArtworkImage = heroArtworkImage;
            _scrimCanvasGroup = scrimCanvasGroup;
            _contentCanvasGroup = contentCanvasGroup;
            _statsCanvasGroup = statsCanvasGroup;
            _retryCanvasGroup = retryCanvasGroup;
            _nextCanvasGroup = nextCanvasGroup;
            _titleText = titleText;
            _sectorText = sectorText;
            _descriptionText = descriptionText;
            _timing = timing;
            var copy = new LandmarkDefinition[landmarks.Count];
            for (int index = 0; index < landmarks.Count; index++)
            {
                copy[index] = landmarks[index]
                    ?? throw new ArgumentException(
                        "Landmark entries cannot be null.",
                        nameof(landmarks));
            }

            _landmarks = copy;
            CurrentLandmark = null;
            _wasCompleted = false;
        }

        public void RefreshNow()
        {
            if (_controller == null
                || _controller.Session == null
                || _boardFrame == null
                || _landmarks == null
                || _landmarks.Length == 0)
            {
                return;
            }

            LandmarkDefinition landmark = SelectCurrentLandmark();
            if (!ReferenceEquals(landmark, CurrentLandmark))
            {
                CurrentLandmark = landmark;
                ApplyArtwork();
            }

            bool completed = _controller.Session.LevelStatus
                == CaptureLevelStatus.Completed;
            RenderVeils(completed
                ? Array.Empty<RoomState>()
                : _controller.Session.Board.ActiveRooms);
            RefreshCompletionText();
            if (completed && !_wasCompleted)
            {
                _completionRevealStartTime = Time.unscaledTime;
            }

            _wasCompleted = completed;
            UpdateCompletionSequence(completed);
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private LandmarkDefinition SelectCurrentLandmark()
        {
            int index = _controller.CurrentLevelIndex % _landmarks.Length;
            if (index < 0)
            {
                index += _landmarks.Length;
            }

            return _landmarks[index];
        }

        private void ApplyArtwork()
        {
            ApplyImage(_artworkImage);
            ApplyImage(_heroArtworkImage);
        }

        private void ApplyImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = CurrentLandmark != null ? CurrentLandmark.Artwork : null;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private void RefreshCompletionText()
        {
            if (CurrentLandmark == null)
            {
                return;
            }

            if (_titleText != null)
            {
                _titleText.text = CurrentLandmark.DisplayTitle;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = CurrentLandmark.ShortDescription;
            }

            if (_sectorText != null)
            {
                _sectorText.text = CurrentLandmark.Sector;
            }
        }

        private void UpdateCompletionSequence(bool completed)
        {
            if (!completed)
            {
                SetGroup(_scrimCanvasGroup, 0f, false);
                SetGroup(_contentCanvasGroup, 0f, false);
                SetGroup(_statsCanvasGroup, 0f, false);
                SetGroup(_retryCanvasGroup, 0f, false);
                SetGroup(_nextCanvasGroup, 0f, false);
                ResetContentOffset();
                return;
            }

            float elapsed = CompletionSequenceElapsedSeconds;
            float scrimProgress = Progress(
                elapsed,
                0f,
                _timing.ScrimFadeSeconds);
            SetGroup(_scrimCanvasGroup, scrimProgress, false);

            float contentProgress = Progress(
                elapsed,
                _timing.ContentDelaySeconds,
                _timing.ContentFadeSeconds);
            SetGroup(_contentCanvasGroup, contentProgress, false);
            SetGroup(_statsCanvasGroup, contentProgress, false);
            ApplyContentOffset(contentProgress);

            float buttonsProgress = Progress(
                elapsed,
                _timing.ButtonsDelaySeconds,
                _timing.ButtonsFadeSeconds);
            bool buttonsReady = buttonsProgress >= 1f;
            SetGroup(_retryCanvasGroup, buttonsProgress, buttonsReady);
            SetGroup(_nextCanvasGroup, buttonsProgress, buttonsReady);
        }

        private static float Progress(
            float elapsed,
            float delaySeconds,
            float fadeSeconds)
        {
            float local = elapsed - delaySeconds;
            if (local <= 0f)
            {
                return 0f;
            }

            return fadeSeconds <= 0f ? 1f : Mathf.Clamp01(local / fadeSeconds);
        }

        private static void SetGroup(
            CanvasGroup group,
            float alpha,
            bool interactable)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = alpha;
            group.interactable = interactable;
            group.blocksRaycasts = interactable;
        }

        private void ApplyContentOffset(float progress)
        {
            if (_contentCanvasGroup == null)
            {
                return;
            }

            var rect = (RectTransform)_contentCanvasGroup.transform;
            float offset = Mathf.Lerp(ContentSlideOffset, 0f, progress);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -offset);
        }

        private void ResetContentOffset()
        {
            if (_contentCanvasGroup == null)
            {
                return;
            }

            var rect = (RectTransform)_contentCanvasGroup.transform;
            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                -ContentSlideOffset);
        }

        private void RenderVeils(IReadOnlyList<RoomState> activeRooms)
        {
            if (_veilRoot == null)
            {
                return;
            }

            for (int index = 0; index < activeRooms.Count; index++)
            {
                RoomState room = activeRooms[index];
                VeilView view = GetOrCreateVeilView(room.Id);
                view.CanvasGroup.alpha = 1f;
                view.FadingOut = false;
                RenderRectangle(view.RectTransform, room.Bounds);
            }

            _staleIds.Clear();
            foreach (KeyValuePair<RoomId, VeilView> pair in _veilViews)
            {
                if (!ContainsRoom(activeRooms, pair.Key))
                {
                    _staleIds.Add(pair.Key);
                }
            }

            for (int index = 0; index < _staleIds.Count; index++)
            {
                RoomId id = _staleIds[index];
                VeilView view = _veilViews[id];
                if (!view.FadingOut)
                {
                    view.FadingOut = true;
                    view.FadeStartAlpha = view.CanvasGroup.alpha;
                    view.FadeStartTime = Time.unscaledTime;
                }

                float elapsed = Time.unscaledTime - view.FadeStartTime;
                float progress = _revealFadeSeconds <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / _revealFadeSeconds);
                view.CanvasGroup.alpha = Mathf.Lerp(
                    view.FadeStartAlpha,
                    0f,
                    progress);
                if (progress >= 1f)
                {
                    view.RectTransform.gameObject.SetActive(false);
                    _veilViews.Remove(id);
                    _availableViews.Add(view);
                }
            }

            VisibleVeilCount = activeRooms.Count;
        }

        private static bool ContainsRoom(
            IReadOnlyList<RoomState> rooms,
            RoomId id)
        {
            for (int index = 0; index < rooms.Count; index++)
            {
                if (rooms[index].Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private VeilView GetOrCreateVeilView(RoomId id)
        {
            if (_veilViews.TryGetValue(id, out VeilView existing))
            {
                return existing;
            }

            VeilView view;
            if (_availableViews.Count > 0)
            {
                int last = _availableViews.Count - 1;
                view = _availableViews[last];
                _availableViews.RemoveAt(last);
                view.RectTransform.gameObject.SetActive(true);
            }
            else
            {
                var gameObject = new GameObject(
                    "Veil",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(CanvasGroup));
                var rect = (RectTransform)gameObject.transform;
                rect.SetParent(_veilRoot, false);
                Image image = gameObject.GetComponent<Image>();
                image.raycastTarget = false;
                CanvasGroup group = gameObject.GetComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;
                view = new VeilView(rect, image, group);
            }

            view.Image.sprite = _veilSprite;
            view.Image.color = _veilColor;
            view.Image.type = Image.Type.Simple;
            view.Image.preserveAspect = false;
            view.FadingOut = false;
            _veilViews.Add(id, view);
            return view;
        }

        private void RenderRectangle(RectTransform visual, LogicalRect bounds)
        {
            Vector2 min = LogicalToAnchored(bounds.Min);
            Vector2 max = LogicalToAnchored(bounds.Max);
            visual.anchorMin = new Vector2(0.5f, 0.5f);
            visual.anchorMax = new Vector2(0.5f, 0.5f);
            visual.pivot = new Vector2(0.5f, 0.5f);
            visual.anchoredPosition = (min + max) * 0.5f;
            visual.localRotation = Quaternion.identity;
            visual.sizeDelta = new Vector2(max.x - min.x, max.y - min.y);
        }

        private Vector2 LogicalToAnchored(LogicalPoint point)
        {
            LogicalRect board = _controller.BoardBounds;
            Rect rect = _boardFrame.rect;
            return new Vector2(
                ((point.X - board.MinX) / board.Width - 0.5f) * rect.width,
                ((point.Y - board.MinY) / board.Height - 0.5f) * rect.height);
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        private sealed class VeilView
        {
            public VeilView(
                RectTransform rectTransform,
                Image image,
                CanvasGroup canvasGroup)
            {
                RectTransform = rectTransform;
                Image = image;
                CanvasGroup = canvasGroup;
            }

            public RectTransform RectTransform { get; }
            public Image Image { get; }
            public CanvasGroup CanvasGroup { get; }
            public bool FadingOut { get; set; }
            public float FadeStartAlpha { get; set; }
            public float FadeStartTime { get; set; }
        }
    }
}
