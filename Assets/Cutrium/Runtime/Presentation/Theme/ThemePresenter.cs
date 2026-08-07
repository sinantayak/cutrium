using System;
using Cutrium.Presentation.Barriers;
using Cutrium.Presentation.Capture;
using Cutrium.Presentation.Feedback;
using Cutrium.Presentation.Threats;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Theme
{
    [DisallowMultipleComponent]
    public sealed class ThemePresenter : MonoBehaviour
    {
        [SerializeField] private ThemeDefinition _selectedTheme;
        [SerializeField] private ThemeDefinition _fallbackTheme;
        [SerializeField] private Image _background;
        [SerializeField] private Image _boardSurface;
        [SerializeField] private Image _boardFrame;
        [SerializeField] private Image[] _hudBackgrounds = Array.Empty<Image>();
        [SerializeField] private Graphic[] _hudAccents = Array.Empty<Graphic>();
        [SerializeField] private Text[] _hudTexts = Array.Empty<Text>();
        [SerializeField] private ThreatPresenter _threatPresenter;
        [SerializeField] private BarrierPresenter _barrierPresenter;
        [SerializeField] private CaptureBoardPresenter _capturePresenter;
        [SerializeField] private FeedbackPresenter _feedbackPresenter;

        public ThemeDefinition SelectedTheme => _selectedTheme;
        public ThemeDefinition FallbackTheme => _fallbackTheme;
        public Image Background => _background;
        public Image BoardSurface => _boardSurface;
        public Image BoardFrame => _boardFrame;
        public ThreatPresenter ThreatPresenter => _threatPresenter;
        public BarrierPresenter BarrierPresenter => _barrierPresenter;
        public CaptureBoardPresenter CapturePresenter => _capturePresenter;
        public FeedbackPresenter FeedbackPresenter => _feedbackPresenter;
        public ResolvedTheme Current { get; private set; }
        public int ApplyCount { get; private set; }

        public void Configure(
            ThemeDefinition selectedTheme,
            ThemeDefinition fallbackTheme,
            Image background,
            Image boardSurface,
            Image boardFrame,
            Image[] hudBackgrounds,
            Graphic[] hudAccents,
            Text[] hudTexts,
            ThreatPresenter threatPresenter,
            BarrierPresenter barrierPresenter,
            CaptureBoardPresenter capturePresenter,
            FeedbackPresenter feedbackPresenter)
        {
            _selectedTheme = selectedTheme;
            _fallbackTheme = fallbackTheme;
            _background = background;
            _boardSurface = boardSurface;
            _boardFrame = boardFrame;
            _hudBackgrounds = hudBackgrounds ?? Array.Empty<Image>();
            _hudAccents = hudAccents ?? Array.Empty<Graphic>();
            _hudTexts = hudTexts ?? Array.Empty<Text>();
            _threatPresenter = threatPresenter;
            _barrierPresenter = barrierPresenter;
            _capturePresenter = capturePresenter;
            _feedbackPresenter = feedbackPresenter;
            ApplyNow();
        }

        public void SetSelectedTheme(ThemeDefinition theme)
        {
            _selectedTheme = theme;
            ApplyNow();
        }

        public void ApplyNow()
        {
            Current = ThemeResolver.Resolve(
                _selectedTheme,
                _fallbackTheme);
            ApplyImage(
                _background,
                Current.BackgroundSprite,
                Current.BackgroundColor);
            ApplyImage(
                _boardSurface,
                Current.BoardSprite,
                Current.BoardColor);
            ApplyImage(
                _boardFrame,
                Current.FrameSprite,
                Current.FrameColor);
            for (int index = 0; index < _hudBackgrounds.Length; index++)
            {
                if (_hudBackgrounds[index] != null)
                {
                    _hudBackgrounds[index].color = Current.HudBackgroundColor;
                }
            }

            for (int index = 0; index < _hudAccents.Length; index++)
            {
                if (_hudAccents[index] != null)
                {
                    _hudAccents[index].color = Current.HudAccentColor;
                }
            }

            for (int index = 0; index < _hudTexts.Length; index++)
            {
                if (_hudTexts[index] != null)
                {
                    _hudTexts[index].color = Current.HudTextColor;
                }
            }

            _threatPresenter?.ApplyTheme(Current.Threat);
            _barrierPresenter?.ApplyTheme(Current.Barrier);
            _capturePresenter?.ApplyTheme(Current.Capture);
            _feedbackPresenter?.SetBaseFrameColor(Current.FrameColor);
            ApplyCount++;
        }

        private void OnEnable()
        {
            if (_background != null)
            {
                ApplyNow();
            }
        }

        private static void ApplyImage(
            Image image,
            Sprite sprite,
            Color color)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }
    }
}
