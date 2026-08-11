using System;
using UnityEngine;

namespace Cutrium.Presentation.Theme
{
    [CreateAssetMenu(
        fileName = "ThemeDefinition",
        menuName = "Cutrium/Theme Definition")]
    public sealed class ThemeDefinition : ScriptableObject
    {
        [SerializeField] private string _stableId = "fallback";

        [Header("Environment")]
        [SerializeField] private Sprite _backgroundSprite;
        [SerializeField] private Color _backgroundColor =
            new Color(0.035f, 0.075f, 0.095f, 1f);
        [SerializeField] private Sprite _boardSprite;
        [SerializeField] private Color _boardColor =
            new Color(0.055f, 0.16f, 0.18f, 1f);
        [SerializeField] private Sprite _frameSprite;
        [SerializeField] private Color _frameColor =
            new Color(0.25f, 0.92f, 0.79f, 1f);

        [Header("Normal Threat")]
        [SerializeField] private Sprite _threatSprite;
        [SerializeField] private Color _threatColor =
            new Color(1f, 0.32f, 0.38f, 1f);
        [SerializeField] private Vector2 _threatScale = Vector2.one;
        [SerializeField] private Vector2 _threatOffset = Vector2.zero;
        [SerializeField] private Sprite _threatShadowSprite;
        [SerializeField] private Color _threatShadowColor =
            new Color(0f, 0f, 0f, 0.3f);
        [SerializeField] private Sprite _threatTrailSprite;
        [SerializeField] private Color _threatTrailColor =
            new Color(1f, 0.28f, 0.36f, 0.32f);

        [Header("Barrier")]
        [SerializeField] private Sprite _barrierBodySprite;
        [SerializeField] private Sprite _barrierCapSprite;
        [SerializeField] private Sprite _barrierPreviewSprite;
        [SerializeField] private Color _barrierPreviewColor =
            new Color(0.35f, 0.9f, 1f, 0.45f);
        [SerializeField] private Color _barrierGrowingColor =
            new Color(0.35f, 0.95f, 1f, 1f);
        [SerializeField] private Color _barrierLockedColor =
            new Color(0.42f, 1f, 0.64f, 1f);
        [SerializeField] private Color _barrierBreakColor =
            new Color(1f, 0.3f, 0.36f, 0.9f);

        [Header("Capture")]
        [SerializeField] private Sprite _captureSprite;
        [SerializeField] private Material _captureMaterial;
        [SerializeField] private Color _captureColor =
            new Color(0.2f, 0.72f, 0.68f, 0.72f);

        [Header("HUD")]
        [SerializeField] private Color _hudBackgroundColor =
            new Color(0.035f, 0.09f, 0.11f, 0.96f);
        [SerializeField] private Color _hudAccentColor =
            new Color(0.25f, 0.92f, 0.79f, 1f);
        [SerializeField] private Color _hudTextColor = Color.white;

        [Header("Sand & Bowl (Landmark Reveal)")]
        [SerializeField] private Sprite _sandTexture;
        [SerializeField] private Sprite _bowlOutlineSprite;
        [SerializeField] private Sprite _bowlInteriorMaskSprite;

        public string StableId => _stableId;
        public Sprite BackgroundSprite => _backgroundSprite;
        public Color BackgroundColor => _backgroundColor;
        public Sprite BoardSprite => _boardSprite;
        public Color BoardColor => _boardColor;
        public Sprite FrameSprite => _frameSprite;
        public Color FrameColor => _frameColor;
        public Sprite ThreatSprite => _threatSprite;
        public Color ThreatColor => _threatColor;
        public Vector2 ThreatScale => _threatScale;
        public Vector2 ThreatOffset => _threatOffset;
        public Sprite ThreatShadowSprite => _threatShadowSprite;
        public Color ThreatShadowColor => _threatShadowColor;
        public Sprite ThreatTrailSprite => _threatTrailSprite;
        public Color ThreatTrailColor => _threatTrailColor;
        public Sprite BarrierBodySprite => _barrierBodySprite;
        public Sprite BarrierCapSprite => _barrierCapSprite;
        public Sprite BarrierPreviewSprite => _barrierPreviewSprite;
        public Color BarrierPreviewColor => _barrierPreviewColor;
        public Color BarrierGrowingColor => _barrierGrowingColor;
        public Color BarrierLockedColor => _barrierLockedColor;
        public Color BarrierBreakColor => _barrierBreakColor;
        public Sprite CaptureSprite => _captureSprite;
        public Material CaptureMaterial => _captureMaterial;
        public Color CaptureColor => _captureColor;
        public Color HudBackgroundColor => _hudBackgroundColor;
        public Color HudAccentColor => _hudAccentColor;
        public Color HudTextColor => _hudTextColor;

        /// Optional artist-provided sand surface texture for the sand/
        /// bowl landmark reveal (ADR-026). Null means "use the generated
        /// default" -- assigning this later replaces the procedural sand
        /// without any reveal-system code change.
        public Sprite SandTexture => _sandTexture;

        /// Optional artist-provided decorative bowl silhouette (ADR-026).
        /// Null means "use the generated default".
        public Sprite BowlOutlineSprite => _bowlOutlineSprite;

        /// Optional artist-provided bowl-interior alpha mask -- drives the
        /// UI Mask that clips the rising sand-fill level to the bowl's
        /// shape (ADR-026). Null means "use the generated default".
        public Sprite BowlInteriorMaskSprite => _bowlInteriorMaskSprite;

        public void ConfigureSandBowlForSetup(
            Sprite sandTexture,
            Sprite bowlOutlineSprite,
            Sprite bowlInteriorMaskSprite)
        {
            _sandTexture = sandTexture;
            _bowlOutlineSprite = bowlOutlineSprite;
            _bowlInteriorMaskSprite = bowlInteriorMaskSprite;
        }

        public void ConfigureForSetup(
            string stableId,
            Sprite backgroundSprite,
            Color backgroundColor,
            Sprite boardSprite,
            Color boardColor,
            Sprite frameSprite,
            Color frameColor,
            Sprite threatSprite,
            Color threatColor,
            Vector2 threatScale,
            Vector2 threatOffset,
            Sprite threatShadowSprite,
            Color threatShadowColor,
            Sprite threatTrailSprite,
            Color threatTrailColor,
            Sprite barrierBodySprite,
            Sprite barrierCapSprite,
            Sprite barrierPreviewSprite,
            Color barrierPreviewColor,
            Color barrierGrowingColor,
            Color barrierLockedColor,
            Color barrierBreakColor,
            Sprite captureSprite,
            Material captureMaterial,
            Color captureColor,
            Color hudBackgroundColor,
            Color hudAccentColor,
            Color hudTextColor)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                throw new ArgumentException(
                    "A theme needs a stable ID.",
                    nameof(stableId));
            }

            ValidateScale(threatScale);
            ValidateVector(threatOffset, nameof(threatOffset));
            _stableId = stableId;
            _backgroundSprite = backgroundSprite;
            _backgroundColor = backgroundColor;
            _boardSprite = boardSprite;
            _boardColor = boardColor;
            _frameSprite = frameSprite;
            _frameColor = frameColor;
            _threatSprite = threatSprite;
            _threatColor = threatColor;
            _threatScale = threatScale;
            _threatOffset = threatOffset;
            _threatShadowSprite = threatShadowSprite;
            _threatShadowColor = threatShadowColor;
            _threatTrailSprite = threatTrailSprite;
            _threatTrailColor = threatTrailColor;
            _barrierBodySprite = barrierBodySprite;
            _barrierCapSprite = barrierCapSprite;
            _barrierPreviewSprite = barrierPreviewSprite;
            _barrierPreviewColor = barrierPreviewColor;
            _barrierGrowingColor = barrierGrowingColor;
            _barrierLockedColor = barrierLockedColor;
            _barrierBreakColor = barrierBreakColor;
            _captureSprite = captureSprite;
            _captureMaterial = captureMaterial;
            _captureColor = captureColor;
            _hudBackgroundColor = hudBackgroundColor;
            _hudAccentColor = hudAccentColor;
            _hudTextColor = hudTextColor;
        }

        internal static bool IsValidScale(Vector2 scale) =>
            IsFinite(scale.x) && IsFinite(scale.y)
            && scale.x > 0f && scale.y > 0f;

        private static void ValidateScale(Vector2 scale)
        {
            if (!IsValidScale(scale))
            {
                throw new ArgumentOutOfRangeException(nameof(scale));
            }
        }

        private static void ValidateVector(Vector2 value, string name)
        {
            if (!IsFinite(value.x) || !IsFinite(value.y))
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public readonly struct ThreatVisualStyle
    {
        public ThreatVisualStyle(
            Sprite sprite,
            Color color,
            Vector2 scale,
            Vector2 offset,
            Sprite shadowSprite,
            Color shadowColor,
            Sprite trailSprite,
            Color trailColor)
        {
            Sprite = sprite;
            Color = color;
            Scale = scale;
            Offset = offset;
            ShadowSprite = shadowSprite;
            ShadowColor = shadowColor;
            TrailSprite = trailSprite;
            TrailColor = trailColor;
        }

        public Sprite Sprite { get; }
        public Color Color { get; }
        public Vector2 Scale { get; }
        public Vector2 Offset { get; }
        public Sprite ShadowSprite { get; }
        public Color ShadowColor { get; }
        public Sprite TrailSprite { get; }
        public Color TrailColor { get; }
    }

    public readonly struct BarrierVisualStyle
    {
        public BarrierVisualStyle(
            Sprite bodySprite,
            Sprite capSprite,
            Sprite previewSprite,
            Color previewColor,
            Color growingColor,
            Color lockedColor,
            Color breakColor)
        {
            BodySprite = bodySprite;
            CapSprite = capSprite;
            PreviewSprite = previewSprite;
            PreviewColor = previewColor;
            GrowingColor = growingColor;
            LockedColor = lockedColor;
            BreakColor = breakColor;
        }

        public Sprite BodySprite { get; }
        public Sprite CapSprite { get; }
        public Sprite PreviewSprite { get; }
        public Color PreviewColor { get; }
        public Color GrowingColor { get; }
        public Color LockedColor { get; }
        public Color BreakColor { get; }
    }

    public readonly struct CaptureVisualStyle
    {
        public CaptureVisualStyle(
            Sprite sprite,
            Material material,
            Color color,
            Sprite completedBarrierSprite,
            Color completedBarrierColor)
        {
            Sprite = sprite;
            Material = material;
            Color = color;
            CompletedBarrierSprite = completedBarrierSprite;
            CompletedBarrierColor = completedBarrierColor;
        }

        public Sprite Sprite { get; }
        public Material Material { get; }
        public Color Color { get; }
        public Sprite CompletedBarrierSprite { get; }
        public Color CompletedBarrierColor { get; }
    }

    public readonly struct ResolvedTheme
    {
        public ResolvedTheme(
            string stableId,
            Sprite backgroundSprite,
            Color backgroundColor,
            Sprite boardSprite,
            Color boardColor,
            Sprite frameSprite,
            Color frameColor,
            ThreatVisualStyle threat,
            BarrierVisualStyle barrier,
            CaptureVisualStyle capture,
            Color hudBackgroundColor,
            Color hudAccentColor,
            Color hudTextColor)
        {
            StableId = stableId;
            BackgroundSprite = backgroundSprite;
            BackgroundColor = backgroundColor;
            BoardSprite = boardSprite;
            BoardColor = boardColor;
            FrameSprite = frameSprite;
            FrameColor = frameColor;
            Threat = threat;
            Barrier = barrier;
            Capture = capture;
            HudBackgroundColor = hudBackgroundColor;
            HudAccentColor = hudAccentColor;
            HudTextColor = hudTextColor;
        }

        public string StableId { get; }
        public Sprite BackgroundSprite { get; }
        public Color BackgroundColor { get; }
        public Sprite BoardSprite { get; }
        public Color BoardColor { get; }
        public Sprite FrameSprite { get; }
        public Color FrameColor { get; }
        public ThreatVisualStyle Threat { get; }
        public BarrierVisualStyle Barrier { get; }
        public CaptureVisualStyle Capture { get; }
        public Color HudBackgroundColor { get; }
        public Color HudAccentColor { get; }
        public Color HudTextColor { get; }
    }

    public readonly struct SandBowlVisualStyle
    {
        public SandBowlVisualStyle(
            Sprite sandTexture,
            Sprite bowlOutlineSprite,
            Sprite bowlInteriorMaskSprite)
        {
            SandTexture = sandTexture;
            BowlOutlineSprite = bowlOutlineSprite;
            BowlInteriorMaskSprite = bowlInteriorMaskSprite;
        }

        public Sprite SandTexture { get; }
        public Sprite BowlOutlineSprite { get; }
        public Sprite BowlInteriorMaskSprite { get; }
    }

    public static class ThemeResolver
    {
        public static SandBowlVisualStyle ResolveSandBowl(
            ThemeDefinition selected,
            ThemeDefinition fallback) =>
            new SandBowlVisualStyle(
                ResolveSprite(selected?.SandTexture, fallback?.SandTexture),
                ResolveSprite(
                    selected?.BowlOutlineSprite,
                    fallback?.BowlOutlineSprite),
                ResolveSprite(
                    selected?.BowlInteriorMaskSprite,
                    fallback?.BowlInteriorMaskSprite));

        public static ResolvedTheme Resolve(
            ThemeDefinition selected,
            ThemeDefinition fallback)
        {
            ThemeDefinition values = selected ?? fallback;
            string stableId = values != null ? values.StableId : "component-flat";
            Color background = values != null
                ? values.BackgroundColor
                : new Color(0.09f, 0.05f, 0.035f, 1f);
            Color board = values != null
                ? values.BoardColor
                : new Color(0.055f, 0.16f, 0.18f, 1f);
            Color frame = values != null
                ? values.FrameColor
                : new Color(0.25f, 0.92f, 0.79f, 1f);
            Vector2 scale = selected != null
                && ThemeDefinition.IsValidScale(selected.ThreatScale)
                    ? selected.ThreatScale
                    : fallback != null
                        && ThemeDefinition.IsValidScale(fallback.ThreatScale)
                            ? fallback.ThreatScale
                            : Vector2.one;
            Vector2 offset = values != null
                ? values.ThreatOffset
                : Vector2.zero;
            var threat = new ThreatVisualStyle(
                ResolveSprite(
                    selected?.ThreatSprite,
                    fallback?.ThreatSprite),
                values != null ? values.ThreatColor :
                    new Color(1f, 0.32f, 0.38f, 1f),
                scale,
                offset,
                ResolveSprite(
                    selected?.ThreatShadowSprite,
                    fallback?.ThreatShadowSprite),
                values != null ? values.ThreatShadowColor :
                    new Color(0f, 0f, 0f, 0.3f),
                ResolveSprite(
                    selected?.ThreatTrailSprite,
                    fallback?.ThreatTrailSprite),
                values != null ? values.ThreatTrailColor :
                    new Color(1f, 0.28f, 0.36f, 0.32f));
            var barrier = new BarrierVisualStyle(
                ResolveSprite(
                    selected?.BarrierBodySprite,
                    fallback?.BarrierBodySprite),
                ResolveSprite(
                    selected?.BarrierCapSprite,
                    fallback?.BarrierCapSprite),
                ResolveSprite(
                    selected?.BarrierPreviewSprite,
                    fallback?.BarrierPreviewSprite),
                values != null ? values.BarrierPreviewColor :
                    new Color(0.35f, 0.9f, 1f, 0.45f),
                values != null ? values.BarrierGrowingColor :
                    new Color(0.35f, 0.95f, 1f, 1f),
                values != null ? values.BarrierLockedColor :
                    new Color(0.42f, 1f, 0.64f, 1f),
                values != null ? values.BarrierBreakColor :
                    new Color(1f, 0.3f, 0.36f, 0.9f));
            var capture = new CaptureVisualStyle(
                ResolveSprite(
                    selected?.CaptureSprite,
                    fallback?.CaptureSprite),
                selected?.CaptureMaterial ?? fallback?.CaptureMaterial,
                values != null ? values.CaptureColor :
                    new Color(0.2f, 0.72f, 0.68f, 0.72f),
                barrier.BodySprite,
                barrier.LockedColor);
            return new ResolvedTheme(
                stableId,
                ResolveSprite(
                    selected?.BackgroundSprite,
                    fallback?.BackgroundSprite),
                background,
                ResolveSprite(selected?.BoardSprite, fallback?.BoardSprite),
                board,
                ResolveSprite(selected?.FrameSprite, fallback?.FrameSprite),
                frame,
                threat,
                barrier,
                capture,
                values != null ? values.HudBackgroundColor :
                    new Color(0.035f, 0.09f, 0.11f, 0.96f),
                values != null ? values.HudAccentColor : frame,
                values != null ? values.HudTextColor : Color.white);
        }

        private static Sprite ResolveSprite(Sprite selected, Sprite fallback) =>
            selected != null ? selected : fallback;
    }
}
