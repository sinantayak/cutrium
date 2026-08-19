using System;
using System.Collections.Generic;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Threats;
using Cutrium.Presentation.Theme;
using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Threats
{
    [DisallowMultipleComponent]
    public sealed class ThreatPresenter : MonoBehaviour
    {
        private const float HunterReactionEmphasisSeconds = 0.34f;
        private const string HunterTrailShaderResource =
            "Presentation/ThreatTrailDetailTint";

        [SerializeField]
        private FirstPlayableController _controller;

        [SerializeField]
        private RectTransform _boardFrame;

        [SerializeField]
        private RectTransform _visual;

        [SerializeField]
        private Image _image;

        [SerializeField]
        private Sprite _optionalSprite;

        [SerializeField]
        private float _visualLogicalDiameter = 0.9f;

        private readonly Dictionary<ThreatId, ThreatView> _activeViews =
            new Dictionary<ThreatId, ThreatView>();
        private readonly List<ThreatId> _staleIds = new List<ThreatId>();
        private readonly List<ThreatView> _availableViews =
            new List<ThreatView>();
        private ThreatView _primaryView;
        private ThreatVisualStyle _themeStyle;
        private bool _hasThemeStyle;
        private bool _feedbackSubscribed;
        private float _hunterReactionEmphasisUntil;
        private Material _hunterTrailMaterial;
        private bool _visible = true;

        public FirstPlayableController Controller => _controller;

        public RectTransform BoardFrame => _boardFrame;

        public RectTransform Visual => _visual;

        public Image Image => _image;

        public Sprite OptionalSprite => _optionalSprite;

        public float VisualLogicalDiameter => _visualLogicalDiameter;

        public ThreatId PresentedThreatId => _controller.Session.Threat.Id;

        public int ActiveViewCount => _activeViews.Count;

        public IReadOnlyCollection<ThreatId> PresentedThreatIds =>
            _activeViews.Keys;

        public ThreatVisualStyle ThemeStyle => _themeStyle;

        public bool HasThemeStyle => _hasThemeStyle;

        public bool Visible => _visible;

        // Lets the pre-level intro cinematic (PreLevelIntroPresenter) hide
        // just the threats -- not the board/sand/landmark art around them --
        // until its staged text resolves and the level actually starts.
        public void SetVisible(bool visible)
        {
            _visible = visible;
            foreach (ThreatView view in _activeViews.Values)
            {
                ApplyVisibility(view);
            }
        }

        private void ApplyVisibility(ThreatView view)
        {
            view.RectTransform.gameObject.SetActive(_visible);
            if (view.TrailImage != null)
            {
                view.TrailImage.gameObject.SetActive(_visible);
            }
        }

        public void Configure(
            FirstPlayableController controller,
            RectTransform boardFrame,
            RectTransform visual,
            Image image,
            Sprite optionalSprite,
            float visualLogicalDiameter)
        {
            _controller = controller;
            _boardFrame = boardFrame;
            _visual = visual;
            _image = image;
            _optionalSprite = optionalSprite;
            _activeViews.Clear();
            _availableViews.Clear();
            _primaryView = null;
            SetVisualLogicalDiameter(visualLogicalDiameter);
            ApplyOptionalSprite(_image);
            SubscribeFeedback();
        }

        public void ApplyTheme(ThreatVisualStyle style)
        {
            if (!ThemeDefinition.IsValidScale(style.Scale))
            {
                throw new ArgumentOutOfRangeException(nameof(style));
            }

            _themeStyle = style;
            _hasThemeStyle = true;
            EnsurePrimaryView();
            foreach (KeyValuePair<ThreatId, ThreatView> pair in _activeViews)
            {
                ApplyStyle(
                    pair.Value,
                    _controller.Session.BehaviorFor(pair.Key).Kind);
            }
        }

        public void SetVisualLogicalDiameter(float visualLogicalDiameter)
        {
            if (!IsFinite(visualLogicalDiameter)
                || visualLogicalDiameter <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(visualLogicalDiameter),
                    visualLogicalDiameter,
                    "Threat visual diameter must be finite and positive.");
            }

            _visualLogicalDiameter = visualLogicalDiameter;
        }

        public bool TryGetVisual(ThreatId id, out RectTransform visual)
        {
            if (_activeViews.TryGetValue(id, out ThreatView view))
            {
                visual = view.RectTransform;
                return true;
            }

            visual = null;
            return false;
        }

        public void RefreshNow()
        {
            if (_controller == null
                || _controller.Session == null
                || _boardFrame == null
                || _visual == null
                || _image == null)
            {
                return;
            }

            SynchronizeViews();
            LogicalRect board = _controller.BoardBounds;
            Rect frameRect = _boardFrame.rect;
            float logicalScale = Math.Min(
                frameRect.width / board.Width,
                frameRect.height / board.Height);
            for (int index = 0;
                 index < _controller.Session.Threats.Count;
                 index++)
            {
                ThreatState threat = _controller.Session.Threats[index];
                ThreatView view = _activeViews[threat.Id];
                PositionView(view, threat, board, frameRect, logicalScale);
            }
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private void OnEnable()
        {
            SubscribeFeedback();
        }

        private void OnDisable()
        {
            UnsubscribeFeedback();
        }

        private void OnDestroy()
        {
            if (_hunterTrailMaterial != null)
            {
                Destroy(_hunterTrailMaterial);
            }
        }

        private void SynchronizeViews()
        {
            EnsurePrimaryView();
            _staleIds.Clear();
            foreach (KeyValuePair<ThreatId, ThreatView> pair in _activeViews)
            {
                if (!SessionContains(pair.Key))
                {
                    _staleIds.Add(pair.Key);
                }
            }

            for (int index = 0; index < _staleIds.Count; index++)
            {
                ThreatId id = _staleIds[index];
                ThreatView view = _activeViews[id];
                _activeViews.Remove(id);
                view.RectTransform.gameObject.SetActive(false);
                if (view.TrailImage != null)
                {
                    view.TrailImage.gameObject.SetActive(false);
                }
                if (view != _primaryView)
                {
                    _availableViews.Add(view);
                }
            }

            for (int index = 0;
                 index < _controller.Session.Threats.Count;
                 index++)
            {
                ThreatState threat = _controller.Session.Threats[index];
                ThreatId id = threat.Id;
                if (_activeViews.ContainsKey(id))
                {
                    continue;
                }

                ThreatView view = id.Value == 1
                    ? _primaryView
                    : GetOrCreateAdditionalView();
                view.RectTransform.name = id.Value == 1
                    ? "ThreatVisual"
                    : $"ThreatVisual_{id.Value}";
                view.RectTransform.gameObject.SetActive(_visible);
                ApplyStyle(
                    view,
                    _controller.Session.BehaviorFor(id).Kind);
                _activeViews.Add(id, view);
            }
        }

        private void EnsurePrimaryView()
        {
            if (_primaryView == null)
            {
                _primaryView = new ThreatView(_visual, _image);
            }
        }

        private ThreatView GetOrCreateAdditionalView()
        {
            if (_availableViews.Count > 0)
            {
                int last = _availableViews.Count - 1;
                ThreatView reused = _availableViews[last];
                _availableViews.RemoveAt(last);
                return reused;
            }

            RectTransform clone = Instantiate(_visual, _visual.parent, false);
            Image image = clone.GetComponent<Image>();
            if (image == null)
            {
                throw new InvalidOperationException(
                    "A cloned threat visual requires an Image component.");
            }

            return new ThreatView(clone, image);
        }

        private bool SessionContains(ThreatId id)
        {
            for (int index = 0;
                 index < _controller.Session.Threats.Count;
                 index++)
            {
                if (_controller.Session.Threats[index].Id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private void PositionView(
            ThreatView view,
            ThreatState threat,
            LogicalRect board,
            Rect frameRect,
            float logicalScale)
        {
            float normalizedX =
                (threat.Position.X - board.MinX) / board.Width;
            float normalizedY =
                (threat.Position.Y - board.MinY) / board.Height;
            RectTransform rect = view.RectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(
                (normalizedX - 0.5f) * frameRect.width,
                (normalizedY - 0.5f) * frameRect.height);
            float diameter = _visualLogicalDiameter * logicalScale;
            Vector2 visualScale = _hasThemeStyle
                ? _themeStyle.Scale
                : Vector2.one;
            Vector2 logicalOffset = _hasThemeStyle
                ? _themeStyle.Offset
                : Vector2.zero;
            rect.anchoredPosition += logicalOffset * logicalScale;
            rect.sizeDelta = new Vector2(
                diameter * visualScale.x,
                diameter * visualScale.y);
            ThreatBehaviorConfiguration behavior =
                _controller.Session.BehaviorFor(threat.Id);
            ApplyStyle(view, behavior.Kind);
            float pulseMultiplier =
                _controller.Session.PulsePhaseMultiplierFor(threat.Id);
            bool hunterEmphasized =
                behavior.Kind == ThreatBehaviorKind.Hunter
                && Time.unscaledTime < _hunterReactionEmphasisUntil
                && WasLastReactingHunter(threat.Id);
            ThreatTrailTreatment treatment = ThreatTrailTreatment.Resolve(
                behavior.Kind,
                pulseMultiplier,
                hunterEmphasized);
            PositionDecorations(view, threat, diameter, treatment);
            Color baseTrailColor = _hasThemeStyle
                ? _themeStyle.TrailColorFor(behavior.Kind)
                : view.TrailImage.color;
            view.TrailImage.color = new Color(
                baseTrailColor.r,
                baseTrailColor.g,
                baseTrailColor.b,
                Mathf.Clamp01(baseTrailColor.a * treatment.Intensity));
        }

        private void ApplyStyle(
            ThreatView view,
            ThreatBehaviorKind behaviorKind)
        {
            EnsureDecorations(view);
            view.TrailImage.gameObject.SetActive(_visible);
            view.Image.sprite = _hasThemeStyle
                ? _themeStyle.SpriteFor(behaviorKind) ?? _optionalSprite
                : _optionalSprite;
            if (_hasThemeStyle)
            {
                view.Image.color = _themeStyle.Color;
                view.ShadowImage.sprite = _themeStyle.ShadowSprite;
                view.ShadowImage.color = _themeStyle.ShadowColor;
                view.TrailImage.sprite = _themeStyle.TrailSprite;
                view.TrailImage.color =
                    _themeStyle.TrailColorFor(behaviorKind);
            }
            view.TrailImage.material = behaviorKind
                == ThreatBehaviorKind.Hunter
                    ? ResolveHunterTrailMaterial()
                    : null;

            view.Image.preserveAspect = false;
            // The authored meteor trail includes transparent padding around
            // a tapered silhouette. Preserve its sprite aspect so the form
            // is never stretched as the threat changes screen size.
            view.TrailImage.preserveAspect = true;
            view.Image.raycastTarget = false;
            view.ShadowImage.raycastTarget = false;
            view.TrailImage.raycastTarget = false;
        }

        private static void EnsureDecorations(ThreatView view)
        {
            if (view.ShadowImage == null)
            {
                view.ShadowImage = GetOrCreateDecoration(
                    view.RectTransform,
                    "ThreatShadow");
            }

            if (view.TrailImage == null)
            {
                string trailName = view.RectTransform.name == "ThreatVisual"
                    ? "ThreatTrail"
                    : view.RectTransform.name.Replace(
                        "ThreatVisual",
                        "ThreatTrail");
                Transform legacyChild = view.RectTransform.Find("ThreatTrail");
                if (legacyChild != null)
                {
                    legacyChild.name = trailName;
                    legacyChild.SetParent(
                        view.RectTransform.parent,
                        false);
                    view.TrailImage = legacyChild.GetComponent<Image>()
                        ?? legacyChild.gameObject.AddComponent<Image>();
                }
                else
                {
                    view.TrailImage = GetOrCreateDecoration(
                        (RectTransform)view.RectTransform.parent,
                        trailName);
                }
            }

            // Trail is a sibling immediately behind the threat, not a child
            // Graphic rendered over it. The authored wide end may therefore
            // reach the threat center while the ball cleanly occludes it.
            if (view.TrailImage.transform.GetSiblingIndex()
                >= view.RectTransform.GetSiblingIndex())
            {
                view.TrailImage.transform.SetSiblingIndex(
                    view.RectTransform.GetSiblingIndex());
            }
            view.ShadowImage.transform.SetSiblingIndex(1);
        }

        private Material ResolveHunterTrailMaterial()
        {
            if (_hunterTrailMaterial != null)
            {
                return _hunterTrailMaterial;
            }

            Shader shader = Resources.Load<Shader>(HunterTrailShaderResource);
            if (shader == null)
            {
                return null;
            }

            _hunterTrailMaterial = new Material(shader)
            {
                name = "Hunter Trail Detail Tint (Runtime)",
                hideFlags = HideFlags.HideAndDontSave,
            };
            return _hunterTrailMaterial;
        }

        private static Image GetOrCreateDecoration(
            RectTransform parent,
            string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<Image>()
                    ?? existing.gameObject.AddComponent<Image>();
            }

            var gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return gameObject.GetComponent<Image>();
        }

        private static void PositionDecorations(
            ThreatView view,
            ThreatState threat,
            float diameter,
            ThreatTrailTreatment treatment)
        {
            RectTransform shadow =
                (RectTransform)view.ShadowImage.transform;
            shadow.anchoredPosition = new Vector2(
                diameter * 0.08f,
                -diameter * 0.1f);
            shadow.sizeDelta = new Vector2(diameter * 1.15f, diameter * 1.15f);

            Vector2 direction = new Vector2(
                threat.Velocity.X,
                threat.Velocity.Y).normalized;
            RectTransform trail =
                (RectTransform)view.TrailImage.transform;
            trail.anchoredPosition =
                view.RectTransform.anchoredPosition
                - (direction * diameter * treatment.CenterOffsetInDiameters);
            trail.localRotation = Quaternion.Euler(
                0f,
                0f,
                (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) + 180f);
            // A square, aspect-preserved box retains the authored 256x256
            // canvas. Its visible meteor silhouette already supplies the
            // desired long/thin shape through transparent padding.
            trail.sizeDelta = Vector2.one
                * diameter
                * treatment.ScaleInDiameters;
        }

        private void SubscribeFeedback()
        {
            if (_feedbackSubscribed || _controller == null)
            {
                return;
            }

            _controller.FeedbackEventRaised += OnFeedbackEventRaised;
            _feedbackSubscribed = true;
        }

        private bool WasLastReactingHunter(ThreatId id)
        {
            IReadOnlyList<ThreatId> reacted =
                _controller.Session.LastHunterReactionThreatIds;
            for (int index = 0; index < reacted.Count; index++)
            {
                if (reacted[index] == id)
                {
                    return true;
                }
            }

            return false;
        }

        private void UnsubscribeFeedback()
        {
            if (!_feedbackSubscribed || _controller == null)
            {
                return;
            }

            _controller.FeedbackEventRaised -= OnFeedbackEventRaised;
            _feedbackSubscribed = false;
        }

        private void OnFeedbackEventRaised(
            Cutrium.Gameplay.Feedback.FeedbackEvent feedbackEvent)
        {
            if (feedbackEvent.Kind
                == Cutrium.Gameplay.Feedback.FeedbackEventKind.HunterReacted)
            {
                _hunterReactionEmphasisUntil =
                    Time.unscaledTime + HunterReactionEmphasisSeconds;
            }
        }

        private void ApplyOptionalSprite(Image image)
        {
            if (image != null && _optionalSprite != null)
            {
                image.sprite = _optionalSprite;
            }
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        private sealed class ThreatView
        {
            public ThreatView(RectTransform rectTransform, Image image)
            {
                RectTransform = rectTransform;
                Image = image;
            }

            public RectTransform RectTransform { get; }

            public Image Image { get; }

            public Image ShadowImage { get; set; }

            public Image TrailImage { get; set; }
        }
    }

    /// Presentation-only geometry/intensity treatment for the shared trail.
    /// Uniform scaling preserves the authored meteor silhouette while the
    /// theme independently tints it for each threat behavior.
    public readonly struct ThreatTrailTreatment
    {
        private ThreatTrailTreatment(
            float scaleInDiameters,
            float centerOffsetInDiameters,
            float intensity)
        {
            ScaleInDiameters = scaleInDiameters;
            CenterOffsetInDiameters = centerOffsetInDiameters;
            Intensity = intensity;
        }

        public float ScaleInDiameters { get; }
        public float CenterOffsetInDiameters { get; }
        public float Intensity { get; }

        public static ThreatTrailTreatment Resolve(
            ThreatBehaviorKind behavior,
            float pulsePhaseMultiplier,
            bool hunterReactionEmphasized)
        {
            switch (behavior)
            {
                case ThreatBehaviorKind.Hunter:
                    return hunterReactionEmphasized
                        ? new ThreatTrailTreatment(2.25f, 1.08f, 1.55f)
                        : new ThreatTrailTreatment(1.78f, 0.88f, 1.15f);
                case ThreatBehaviorKind.Pulse:
                    return pulsePhaseMultiplier > 1f
                        ? new ThreatTrailTreatment(2.05f, 0.98f, 1.35f)
                        : new ThreatTrailTreatment(1.08f, 0.58f, 0.68f);
                default:
                    return new ThreatTrailTreatment(1.42f, 0.72f, 0.9f);
            }
        }
    }
}
