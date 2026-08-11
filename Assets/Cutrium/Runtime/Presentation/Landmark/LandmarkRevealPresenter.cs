using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Board;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.Serialization;
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

    /// Renders the landmark's sharp artwork behind an opaque sand
    /// composite over every active (uncaptured) room, and animates a short
    /// top-to-bottom recede -- shrinking the composite's own rect and UV
    /// mapping together, no shader -- the instant that room is captured,
    /// as if the sand had drained downward and out. Each newly captured
    /// room also launches a short-lived burst of pooled sand-grain
    /// particles that visually fly from that room's screen position to the
    /// start of the target-progress bar. The leading grain releases the
    /// presentation-only progress interpolation on arrival; gameplay capture
    /// and completion remain immediate and authoritative.
    [DisallowMultipleComponent]
    public sealed class LandmarkRevealPresenter : MonoBehaviour
    {
        private const float ContentSlideOffset = 18f;
        private const float TintAlpha = 0.08f;
        private const int MinGrainsPerCapture = 28;
        private const int MaxGrainsPerCapture = 72;
        private const int MaxCreatedGrainViews = 144;
        private const float MinGrainStreamSeconds = 0.18f;
        private const float MaxGrainStreamSeconds = 0.95f;
        private const float GrainMinDuration = 0.42f;
        private const float GrainMaxDuration = 0.76f;
        private const float GrainMinArcHeight = 24f;
        private const float GrainMaxArcHeight = 64f;
        private const float GrainMinSize = 5.5f;
        private const float GrainMaxSize = 10.5f;
        private const float MinGrainJitter = 10f;
        private const float GrainSpreadFraction = 0.8f;
        private const float GrainEndJitter = 18f;
        private const float GrainMaxLateralDrift = 22f;
        private const float GrainMaxRotationSpeed = 190f;
        private const float ReferenceLinearFraction = 0.5f;

        private static readonly Color TintColor = new Color(0.32f, 0.22f, 0.1f, TintAlpha);
        private static readonly Color EdgeHighlightColor =
            new Color(0.95f, 0.86f, 0.62f, 0.6f);
        private static readonly Color GrainDarkColor =
            new Color(0.72f, 0.52f, 0.28f, 0.96f);
        private static readonly Color GrainLightColor =
            new Color(0.96f, 0.79f, 0.46f, 1f);

        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private RectTransform _boardFrame;
        [SerializeField] private Image _artworkImage;
        [SerializeField] private RectTransform _veilRoot;
        [SerializeField] private Texture2D _sandTexture;
        [SerializeField] private RectTransform _grainFlightRoot;
        [FormerlySerializedAs("_bowlFillTarget")]
        [SerializeField] private RectTransform _progressFillStartTarget;
        [SerializeField] private SandProgressPresenter _sandProgressPresenter;
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

        private readonly Dictionary<RoomId, ActiveSandEntry> _activeViews =
            new Dictionary<RoomId, ActiveSandEntry>();
        private readonly List<RoomId> _staleActiveIds = new List<RoomId>();
        private readonly HashSet<RoomId> _seenActiveIds = new HashSet<RoomId>();
        private readonly List<WipeEntry> _wipeEntries = new List<WipeEntry>();
        private readonly List<SandCompositeView> _pool = new List<SandCompositeView>();
        private readonly List<LogicalRect> _obscuredRoomBounds = new List<LogicalRect>();
        private readonly List<LogicalRect> _wipingRoomBounds = new List<LogicalRect>();

        private readonly List<GrainView> _grainPool = new List<GrainView>();
        private readonly List<GrainFlight> _activeGrainFlights = new List<GrainFlight>();
        private readonly List<GrainFlight> _grainFlightPool = new List<GrainFlight>();

        private int _createdGrainViewCount;

        private ThreatMotionSession _lastSeenSession;
        private int _capturedRoomsSeen;
        private bool _wasCompleted;
        private float _completionRevealStartTime;

        public FirstPlayableController Controller => _controller;
        public RectTransform BoardFrame => _boardFrame;
        public Image ArtworkImage => _artworkImage;
        public RectTransform VeilRoot => _veilRoot;
        public Texture2D SandTexture => _sandTexture;
        public RectTransform GrainFlightRoot => _grainFlightRoot;
        public RectTransform SandDestination => _progressFillStartTarget;
        public RectTransform BowlFillTarget => _progressFillStartTarget;
        public SandProgressPresenter SandProgressPresenter =>
            _sandProgressPresenter;
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

        /// True once every in-flight sand recede has finished (or none has
        /// ever started). Active (still sand-covered) rooms never affect
        /// this -- it only tracks rooms mid-transition.
        public bool AllVeilsFullyRevealed => _wipeEntries.Count == 0;

        /// Number of recedes currently animating from sand-covered to
        /// clear.
        public int InFlightWipeCount => _wipeEntries.Count;

        /// Logical bounds of every room currently showing full sand
        /// coverage (not yet captured).
        public IReadOnlyList<LogicalRect> ObscuredRoomBounds => _obscuredRoomBounds;

        /// Logical bounds of every room currently mid-recede (captured,
        /// but its reveal animation has not finished).
        public IReadOnlyList<LogicalRect> WipingRoomBounds => _wipingRoomBounds;

        /// Number of pooled sand-grain particles currently flying toward
        /// the progress bar. Purely cosmetic -- never drives gameplay or
        /// the authoritative captured-area value.
        public int ActiveGrainCount => _activeGrainFlights.Count;
        public int CreatedGrainViewCount => _createdGrainViewCount;
        public int MaximumGrainViewCount => MaxCreatedGrainViews;
        public int MinimumGrainsPerCapture => MinGrainsPerCapture;
        public Vector3 LastGrainArrivalTargetWorldPosition { get; private set; }

        public void Configure(
            FirstPlayableController controller,
            RectTransform boardFrame,
            Image artworkImage,
            RectTransform veilRoot,
            Texture2D sandTexture,
            RectTransform grainFlightRoot,
            RectTransform progressFillStartTarget,
            SandProgressPresenter sandProgressPresenter,
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

            ReturnAllViewsToPool();
            _controller = controller;
            _boardFrame = boardFrame;
            _artworkImage = artworkImage;
            _veilRoot = veilRoot;
            _sandTexture = sandTexture;
            _grainFlightRoot = grainFlightRoot;
            _progressFillStartTarget = progressFillStartTarget;
            _sandProgressPresenter = sandProgressPresenter;
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
            _lastSeenSession = null;
            _capturedRoomsSeen = 0;
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
            bool justCompleted = completed && !_wasCompleted;
            RenderSand(completed, justCompleted);
            AdvanceGrainFlights();
            RefreshCompletionText();
            if (justCompleted)
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
            // The board layer stretches to fill the 10x16 frame it reveals
            // (no native aspect ratio to preserve there), but the
            // completion-screen hero photo sits in its own fixed frame and
            // must never be stretched away from its native aspect ratio.
            ApplyImage(_artworkImage, preserveAspect: false);
            ApplyImage(_heroArtworkImage, preserveAspect: true);
        }

        private void ApplyImage(Image image, bool preserveAspect)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = CurrentLandmark != null ? CurrentLandmark.Artwork : null;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
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

        // ------------------------------------------------------------
        // Sand: active coverage + top-to-bottom recede reveal
        // ------------------------------------------------------------

        private void RenderSand(bool completed, bool justCompleted)
        {
            if (_veilRoot == null)
            {
                return;
            }

            ThreatMotionSession session = _controller.Session;
            if (!ReferenceEquals(session, _lastSeenSession))
            {
                // A new session (Retry/Next) reuses low RoomId values, so
                // any bookkeeping keyed by RoomId or by a captured-rooms
                // index from the previous session is stale -- start clean.
                ResetSandState();
                _lastSeenSession = session;
            }

            // The true current active rooms, regardless of completion --
            // needed below so a room captured on the very same tick the
            // level completes (extremely common: the capturing cut is
            // usually what reaches the target) still gets its own precise
            // recede rectangle instead of a stale, larger cached one.
            IReadOnlyList<RoomState> realActiveRooms = session.Board.ActiveRooms;
            IReadOnlyList<RoomState> sandActiveRooms = completed
                ? Array.Empty<RoomState>()
                : realActiveRooms;

            _seenActiveIds.Clear();
            for (int index = 0; index < sandActiveRooms.Count; index++)
            {
                RoomState room = sandActiveRooms[index];
                _seenActiveIds.Add(room.Id);
                if (!_activeViews.TryGetValue(room.Id, out ActiveSandEntry entry))
                {
                    entry = new ActiveSandEntry { View = RentFromPool() };
                    _activeViews.Add(room.Id, entry);
                }

                entry.Bounds = room.Bounds;
                ApplyFullCoverage(entry.View, room.Bounds);
            }

            // A sand view leaves _activeViews the instant its room stops
            // being covered, whether that room was captured (handled
            // below, via the exact captured rectangle) or the level
            // completed (handled below too, via the room's own current
            // bounds) -- never by reusing this entry's possibly-stale
            // cached bounds.
            _staleActiveIds.Clear();
            foreach (KeyValuePair<RoomId, ActiveSandEntry> pair in _activeViews)
            {
                if (!_seenActiveIds.Contains(pair.Key))
                {
                    _staleActiveIds.Add(pair.Key);
                }
            }

            for (int index = 0; index < _staleActiveIds.Count; index++)
            {
                RoomId id = _staleActiveIds[index];
                ReturnToPool(_activeViews[id].View);
                _activeViews.Remove(id);
            }

            // Always reconcile against CapturedRooms (an append-only list
            // within a session), regardless of completion, so a room
            // captured on the completing tick still gets its own precise
            // recede rectangle rather than being swallowed by the
            // whole-board fallback below.
            IReadOnlyList<RoomState> captured = session.Board.CapturedRooms;
            for (int index = _capturedRoomsSeen; index < captured.Count; index++)
            {
                LogicalRect capturedBounds = captured[index].Bounds;
                StartWipe(RentFromPool(), capturedBounds);
                SpawnGrainBurst(
                    capturedBounds,
                    advancesProgress: true,
                    capturedFractionAtRelease: session.CapturedFraction);
            }

            _capturedRoomsSeen = captured.Count;

            if (justCompleted)
            {
                // Completion forces a full reveal (ADR-021) even for area
                // that was never individually captured. Each such room's
                // own current bounds are used -- never a stale parent rect
                // from before a same-tick split.
                for (int index = 0; index < realActiveRooms.Count; index++)
                {
                    LogicalRect bounds = realActiveRooms[index].Bounds;
                    StartWipe(RentFromPool(), bounds);
                    SpawnGrainBurst(
                        bounds,
                        advancesProgress: false,
                        capturedFractionAtRelease: session.CapturedFraction);
                }
            }

            for (int index = _wipeEntries.Count - 1; index >= 0; index--)
            {
                WipeEntry entry = _wipeEntries[index];
                float elapsed = Time.unscaledTime - entry.StartTime;
                float progress = _revealFadeSeconds <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / _revealFadeSeconds);
                ApplyWipeProgress(entry.View, entry.Bounds, progress);
                if (progress >= 1f)
                {
                    ReturnToPool(entry.View);
                    _wipeEntries.RemoveAt(index);
                }
            }

            _obscuredRoomBounds.Clear();
            foreach (KeyValuePair<RoomId, ActiveSandEntry> pair in _activeViews)
            {
                _obscuredRoomBounds.Add(pair.Value.Bounds);
            }

            _wipingRoomBounds.Clear();
            for (int index = 0; index < _wipeEntries.Count; index++)
            {
                _wipingRoomBounds.Add(_wipeEntries[index].Bounds);
            }

            VisibleVeilCount = sandActiveRooms.Count;
        }

        private void ResetSandState()
        {
            foreach (KeyValuePair<RoomId, ActiveSandEntry> pair in _activeViews)
            {
                ReturnToPool(pair.Value.View);
            }

            _activeViews.Clear();
            for (int index = 0; index < _wipeEntries.Count; index++)
            {
                ReturnToPool(_wipeEntries[index].View);
            }

            _wipeEntries.Clear();
            _capturedRoomsSeen = 0;

            for (int index = 0; index < _activeGrainFlights.Count; index++)
            {
                ReturnGrainFlightToPool(_activeGrainFlights[index]);
            }

            _activeGrainFlights.Clear();
        }

        private void ReturnAllViewsToPool()
        {
            ResetSandState();
        }

        private void StartWipe(SandCompositeView view, LogicalRect bounds)
        {
            ApplyWipeProgress(view, bounds, 0f);
            _wipeEntries.Add(new WipeEntry
            {
                View = view,
                Bounds = bounds,
                StartTime = Time.unscaledTime,
            });
        }

        private void ApplyFullCoverage(SandCompositeView view, LogicalRect bounds) =>
            ApplyWipeProgress(view, bounds, 0f);

        // Shrinks the composite's own rect and its sand RawImage's uvRect
        // together, from the top down, keeping the bottom edge (and
        // bottom V coordinate) anchored -- so the remaining sand strip
        // stays correctly scaled (never squished) as its surface recedes
        // downward and out, as if draining. No shader, no per-frame
        // screen blur, just RectTransform + UV math on already-pooled
        // objects.
        private void ApplyWipeProgress(
            SandCompositeView view,
            LogicalRect bounds,
            float progress)
        {
            Vector2 min = LogicalToAnchored(bounds.Min);
            Vector2 max = LogicalToAnchored(bounds.Max);
            float fullWidth = max.x - min.x;
            float fullHeight = max.y - min.y;
            float centerX = (min.x + max.x) * 0.5f;

            Rect fullUv = LogicalToBoardUv(bounds);

            float visibleFraction = Mathf.Clamp01(1f - progress);
            float visibleHeight = fullHeight * visibleFraction;
            float bottomY = min.y;
            float newCenterY = bottomY + (visibleHeight * 0.5f);

            view.Container.anchorMin = new Vector2(0.5f, 0.5f);
            view.Container.anchorMax = new Vector2(0.5f, 0.5f);
            view.Container.pivot = new Vector2(0.5f, 0.5f);
            view.Container.anchoredPosition = new Vector2(centerX, newCenterY);
            view.Container.sizeDelta = new Vector2(fullWidth, visibleHeight);

            var visibleUv = new Rect(
                fullUv.x,
                fullUv.y,
                fullUv.width,
                fullUv.height * visibleFraction);
            view.SandCrop.uvRect = visibleUv;
            view.SandCrop.texture = _sandTexture;
            view.SandCrop.enabled = _sandTexture != null;

            bool midWipe = progress > 0f && progress < 1f;
            view.EdgeHighlightObject.SetActive(midWipe && visibleHeight > 0.5f);
        }

        private SandCompositeView RentFromPool()
        {
            int last = _pool.Count - 1;
            if (last >= 0)
            {
                SandCompositeView pooled = _pool[last];
                _pool.RemoveAt(last);
                pooled.Container.gameObject.SetActive(true);
                return pooled;
            }

            return CreateCompositeView();
        }

        private void ReturnToPool(SandCompositeView view)
        {
            view.Container.gameObject.SetActive(false);
            _pool.Add(view);
        }

        private SandCompositeView CreateCompositeView()
        {
            var containerObject = new GameObject(
                "Sand",
                typeof(RectTransform));
            var container = (RectTransform)containerObject.transform;
            container.SetParent(_veilRoot, false);

            RawImage sandCrop = CreateRawImage(container, "SandSurface");

            var tintObject = new GameObject(
                "Tint",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var tintRect = (RectTransform)tintObject.transform;
            tintRect.SetParent(container, false);
            StretchToParent(tintRect);
            Image tint = tintObject.GetComponent<Image>();
            tint.color = TintColor;
            tint.raycastTarget = false;

            // The receding edge highlight sits at the TOP of the shrinking
            // sand rect (the boundary between remaining sand and revealed
            // area), not the side -- matching the top-to-bottom recede.
            var highlightObject = new GameObject(
                "SandEdgeHighlight",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var highlightRect = (RectTransform)highlightObject.transform;
            highlightRect.SetParent(container, false);
            highlightRect.anchorMin = new Vector2(0f, 1f);
            highlightRect.anchorMax = new Vector2(1f, 1f);
            highlightRect.pivot = new Vector2(0.5f, 1f);
            highlightRect.sizeDelta = new Vector2(0f, 3f);
            highlightRect.anchoredPosition = Vector2.zero;
            Image highlightImage = highlightObject.GetComponent<Image>();
            highlightImage.color = EdgeHighlightColor;
            highlightImage.raycastTarget = false;
            highlightObject.SetActive(false);

            return new SandCompositeView
            {
                Container = container,
                SandCrop = sandCrop,
                Tint = tint,
                EdgeHighlightObject = highlightObject,
            };
        }

        private static RawImage CreateRawImage(RectTransform parent, string name)
        {
            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            StretchToParent(rect);
            RawImage image = gameObject.GetComponent<RawImage>();
            image.raycastTarget = false;
            return image;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Vector2 LogicalToAnchored(LogicalPoint point)
        {
            LogicalRect board = _controller.BoardBounds;
            Rect rect = _boardFrame.rect;
            return new Vector2(
                ((point.X - board.MinX) / board.Width - 0.5f) * rect.width,
                ((point.Y - board.MinY) / board.Height - 0.5f) * rect.height);
        }

        private Rect LogicalToBoardUv(LogicalRect bounds)
        {
            LogicalRect board = _controller.BoardBounds;
            float uMin = (bounds.MinX - board.MinX) / board.Width;
            float uMax = (bounds.MaxX - board.MinX) / board.Width;
            float vMin = (bounds.MinY - board.MinY) / board.Height;
            float vMax = (bounds.MaxY - board.MinY) / board.Height;
            return new Rect(uMin, vMin, uMax - uMin, vMax - vMin);
        }

        // ------------------------------------------------------------
        // Sand grain flight: pooled cosmetic stream from a captured room to
        // the live progress-fill start. A leading grain may notify the HUD
        // when it arrives, but never writes gameplay state.
        // ------------------------------------------------------------

        private void SpawnGrainBurst(
            LogicalRect capturedBounds,
            bool advancesProgress,
            float capturedFractionAtRelease)
        {
            if (_grainFlightRoot == null
                || _progressFillStartTarget == null)
            {
                return;
            }

            LogicalRect board = _controller.BoardBounds;
            float areaFraction = board.Width > 0f && board.Height > 0f
                ? Mathf.Clamp01(
                    (capturedBounds.Width * capturedBounds.Height)
                    / (board.Width * board.Height))
                : 0f;
            // sqrt() converts an area ratio back to a linear-size ratio, so a
            // capture with 4x the area reads as roughly 2x "bigger" here,
            // matching how the eye judges cut size rather than raw area.
            float sizeFactor = Mathf.Clamp01(
                Mathf.Sqrt(areaFraction) / ReferenceLinearFraction);

            int grainCount = Mathf.RoundToInt(
                Mathf.Lerp(MinGrainsPerCapture, MaxGrainsPerCapture, sizeFactor));
            float streamWindow = Mathf.Lerp(
                MinGrainStreamSeconds, MaxGrainStreamSeconds, sizeFactor);

            Vector2 centerAnchored = LogicalToAnchored(capturedBounds.Center);
            Vector3 centerWorld = _boardFrame.TransformPoint(
                new Vector3(centerAnchored.x, centerAnchored.y, 0f));
            Vector2 centerLocal = _grainFlightRoot.InverseTransformPoint(centerWorld);

            Vector2 minAnchored = LogicalToAnchored(
                new LogicalPoint(capturedBounds.MinX, capturedBounds.MinY));
            Vector2 maxAnchored = LogicalToAnchored(
                new LogicalPoint(capturedBounds.MaxX, capturedBounds.MaxY));
            Vector3 minWorld = _boardFrame.TransformPoint(
                new Vector3(minAnchored.x, minAnchored.y, 0f));
            Vector3 maxWorld = _boardFrame.TransformPoint(
                new Vector3(maxAnchored.x, maxAnchored.y, 0f));
            Vector2 minLocal = _grainFlightRoot.InverseTransformPoint(minWorld);
            Vector2 maxLocal = _grainFlightRoot.InverseTransformPoint(maxWorld);
            float jitterX = Mathf.Max(
                MinGrainJitter,
                Mathf.Abs(maxLocal.x - minLocal.x) * 0.5f * GrainSpreadFraction);
            float jitterY = Mathf.Max(
                MinGrainJitter,
                Mathf.Abs(maxLocal.y - minLocal.y) * 0.5f * GrainSpreadFraction);

            float now = Time.unscaledTime;
            int spawnedCount = 0;
            for (int index = 0; index < grainCount; index++)
            {
                GrainView view = RentGrainFromPool();
                if (view == null)
                {
                    break;
                }

                Vector2 jitteredStart = centerLocal + new Vector2(
                    UnityEngine.Random.Range(-jitterX, jitterX),
                    UnityEngine.Random.Range(-jitterY, jitterY));
                view.Rect.anchoredPosition = jitteredStart;
                view.Rect.localScale = Vector3.one;
                view.Rect.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    UnityEngine.Random.Range(0f, 360f));
                float grainSize = UnityEngine.Random.Range(
                    GrainMinSize,
                    GrainMaxSize);
                view.Rect.sizeDelta = new Vector2(grainSize, grainSize);
                view.Image.color = Color.Lerp(
                    GrainDarkColor,
                    GrainLightColor,
                    UnityEngine.Random.value);

                // Spread departures across the stream window instead of
                // firing every grain on the same frame, so a large capture
                // reads as a sustained pour rather than one simultaneous
                // puff.
                float spawnDelay = grainCount > 1
                    ? (index / (float)(grainCount - 1)) * streamWindow
                    : 0f;
                spawnDelay += UnityEngine.Random.Range(0f, streamWindow * 0.15f + 0.001f);
                bool isProgressLead = advancesProgress && spawnedCount == 0;
                if (isProgressLead)
                {
                    // One leading grain provides a stable arrival beat; the
                    // rest retain timing variation and form the fuller stream
                    // around the progress animation.
                    spawnDelay = 0f;
                }

                GrainFlight flight = RentGrainFlight();
                flight.View = view;
                flight.Start = jitteredStart;
                flight.EndOffset = new Vector2(
                    UnityEngine.Random.Range(-GrainEndJitter, GrainEndJitter),
                    0f);
                flight.StartTime = now + spawnDelay;
                flight.Duration = isProgressLead
                    ? GrainMinDuration
                    : UnityEngine.Random.Range(
                        GrainMinDuration,
                        GrainMaxDuration);
                flight.ArcHeight = UnityEngine.Random.Range(
                    GrainMinArcHeight,
                    GrainMaxArcHeight);
                flight.LateralDrift = UnityEngine.Random.Range(
                    -GrainMaxLateralDrift,
                    GrainMaxLateralDrift);
                flight.RotationSpeed = UnityEngine.Random.Range(
                    -GrainMaxRotationSpeed,
                    GrainMaxRotationSpeed);
                flight.HasStarted = false;
                flight.NotifiesProgressOnArrival = isProgressLead;
                flight.ProgressTargetOnArrival = capturedFractionAtRelease;
                _activeGrainFlights.Add(flight);
                spawnedCount++;
            }
        }

        private void AdvanceGrainFlights()
        {
            float now = Time.unscaledTime;
            for (int index = _activeGrainFlights.Count - 1; index >= 0; index--)
            {
                GrainFlight flight = _activeGrainFlights[index];

                if (!flight.HasStarted)
                {
                    if (now < flight.StartTime)
                    {
                        continue;
                    }

                    flight.HasStarted = true;
                    flight.View.Rect.gameObject.SetActive(true);
                }

                float elapsed = now - flight.StartTime;
                float progress = flight.Duration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / flight.Duration);

                Vector3 targetWorld =
                    _progressFillStartTarget.TransformPoint(Vector3.zero);
                Vector2 liveEnd =
                    (Vector2)_grainFlightRoot.InverseTransformPoint(targetWorld)
                    + flight.EndOffset;
                Vector2 linear = Vector2.Lerp(flight.Start, liveEnd, progress);
                float arc = Mathf.Sin(progress * Mathf.PI) * flight.ArcHeight;
                float lateral = Mathf.Sin(progress * Mathf.PI * 2f)
                    * flight.LateralDrift;
                flight.View.Rect.anchoredPosition =
                    linear + new Vector2(lateral, arc);
                flight.View.Rect.localScale =
                    Vector3.one * Mathf.Lerp(1f, 0.6f, progress);
                flight.View.Rect.Rotate(
                    0f,
                    0f,
                    flight.RotationSpeed * Time.unscaledDeltaTime);

                if (progress >= 1f)
                {
                    LastGrainArrivalTargetWorldPosition = targetWorld;
                    if (flight.NotifiesProgressOnArrival
                        && _sandProgressPresenter != null)
                    {
                        _sandProgressPresenter.NotifySandArrived(
                            flight.ProgressTargetOnArrival);
                    }

                    ReturnGrainFlightToPool(flight);
                    _activeGrainFlights.RemoveAt(index);
                }
            }
        }

        private GrainView RentGrainFromPool()
        {
            int last = _grainPool.Count - 1;
            if (last >= 0)
            {
                GrainView pooled = _grainPool[last];
                _grainPool.RemoveAt(last);
                return pooled;
            }

            if (_createdGrainViewCount >= MaxCreatedGrainViews)
            {
                return null;
            }

            return CreateGrainView();
        }

        private void ReturnGrainToPool(GrainView view)
        {
            view.Rect.gameObject.SetActive(false);
            _grainPool.Add(view);
        }

        private GrainView CreateGrainView()
        {
            var gameObject = new GameObject(
                "SandGrain",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(_grainFlightRoot, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(GrainMaxSize, GrainMaxSize);
            Image image = gameObject.GetComponent<Image>();
            image.color = GrainLightColor;
            image.raycastTarget = false;
            gameObject.SetActive(false);
            _createdGrainViewCount++;
            return new GrainView { Rect = rect, Image = image };
        }

        private GrainFlight RentGrainFlight()
        {
            int last = _grainFlightPool.Count - 1;
            if (last < 0)
            {
                return new GrainFlight();
            }

            GrainFlight flight = _grainFlightPool[last];
            _grainFlightPool.RemoveAt(last);
            return flight;
        }

        private void ReturnGrainFlightToPool(GrainFlight flight)
        {
            ReturnGrainToPool(flight.View);
            flight.View = null;
            flight.HasStarted = false;
            flight.NotifiesProgressOnArrival = false;
            flight.ProgressTargetOnArrival = 0f;
            _grainFlightPool.Add(flight);
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        private sealed class SandCompositeView
        {
            public RectTransform Container;
            public RawImage SandCrop;
            public Image Tint;
            public GameObject EdgeHighlightObject;
        }

        private sealed class ActiveSandEntry
        {
            public LogicalRect Bounds;
            public SandCompositeView View;
        }

        private sealed class WipeEntry
        {
            public LogicalRect Bounds;
            public SandCompositeView View;
            public float StartTime;
        }

        private sealed class GrainView
        {
            public RectTransform Rect;
            public Image Image;
        }

        private sealed class GrainFlight
        {
            public GrainView View;
            public Vector2 Start;
            public Vector2 EndOffset;
            public float StartTime;
            public float Duration;
            public float ArcHeight;
            public float LateralDrift;
            public float RotationSpeed;
            public bool HasStarted;
            public bool NotifiesProgressOnArrival;
            public float ProgressTargetOnArrival;
        }
    }
}
