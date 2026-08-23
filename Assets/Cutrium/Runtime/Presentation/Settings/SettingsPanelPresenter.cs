using System;
using System.Collections.Generic;
using Cutrium.Presentation.Feedback;
using Cutrium.Presentation.Frontend;
using Cutrium.Presentation.HUD;
using Cutrium.Presentation.Landmark;
using Cutrium.Presentation.Localization;
using Cutrium.Unity.Services;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Settings
{
    [DisallowMultipleComponent]
    public sealed class SettingsPanelPresenter : MonoBehaviour
    {
        private const string SoundPreferenceKey =
            "Cutrium.Settings.SoundEnabled";
        private const string MusicPreferenceKey =
            "Cutrium.Settings.MusicEnabled";
        private const string HapticPreferenceKey =
            "Cutrium.Settings.HapticEnabled";

        [Header("Flow")]
        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private FrontEndPresenter _frontEnd;
        [SerializeField] private FeedbackAudioPresenter _feedbackAudio;
        [SerializeField] private FeedbackHapticPresenter _feedbackHaptics;
        [SerializeField] private PreLevelIntroPresenter _preLevelIntro;
        [SerializeField] private LandmarkRevealPresenter _landmarkReveal;
        [SerializeField] private LocalizationService _localization;
        [SerializeField] private CanvasGroup _panelCanvasGroup;

        [Header("Actions")]
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _homeOpenButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _soundButton;
        [SerializeField] private Button _musicButton;
        [SerializeField] private Button _hapticButton;
        [SerializeField] private Button _languageButton;
        [SerializeField] private Button _homeButton;
        [SerializeField] private Button _exitButton;

        [Header("Toggle Presentation")]
        [SerializeField] private Image _soundBackground;
        [SerializeField] private Image _musicBackground;
        [SerializeField] private Image _hapticBackground;
        [SerializeField] private Image _soundIcon;
        [SerializeField] private Image _musicIcon;
        [SerializeField] private Image _hapticIcon;
        [SerializeField] private TMP_Text _soundStateLabel;
        [SerializeField] private TMP_Text _musicStateLabel;
        [SerializeField] private TMP_Text _hapticStateLabel;

        [Header("Optional Music")]
        [SerializeField] private AudioSource[] _musicSources =
            Array.Empty<AudioSource>();

        [Header("Preferences")]
        [SerializeField] private bool _persistPreferences = true;
        [SerializeField] private Color _enabledBackgroundColor = Color.white;
        [SerializeField] private Color _disabledBackgroundColor =
            new Color(0.48f, 0.28f, 0.18f, 1f);
        [SerializeField] private Color _enabledIconColor = Color.white;
        [SerializeField] private Color _disabledIconColor =
            new Color(0.42f, 0.32f, 0.26f, 0.55f);
        [SerializeField] private Color _enabledLabelColor =
            new Color(1f, 0.9f, 0.72f, 1f);
        [SerializeField] private Color _disabledLabelColor =
            new Color(0.45f, 0.29f, 0.22f, 1f);

        private bool _subscribed;
        private bool _preferencesLoaded;
        private bool _soundEnabled = true;
        private bool _musicEnabled = true;
        private bool _hapticEnabled = true;
        private readonly PlayerProgressStore _progressStore =
            new PlayerProgressStore();

        public FirstPlayableController Controller => _controller;
        public CanvasGroup PanelCanvasGroup => _panelCanvasGroup;
        public Button OpenButton => _openButton;
        public Button HomeOpenButton => _homeOpenButton;
        public Button CloseButton => _closeButton;
        public Button SoundButton => _soundButton;
        public Button MusicButton => _musicButton;
        public Button HapticButton => _hapticButton;
        public Button LanguageButton => _languageButton;
        public Button HomeButton => _homeButton;
        public Button ExitButton => _exitButton;
        public LocalizationService Localization => _localization;
        public bool IsOpen { get; private set; }
        public bool SoundEnabled => _soundEnabled;
        public bool MusicEnabled => _musicEnabled;
        public bool HapticEnabled => _hapticEnabled;
        public IReadOnlyList<AudioSource> MusicSources => _musicSources;
        public PreLevelIntroPresenter PreLevelIntro => _preLevelIntro;
        public LandmarkRevealPresenter LandmarkReveal => _landmarkReveal;

        public event Action ExitRequested;

        public void ConfigurePreLevelIntro(PreLevelIntroPresenter preLevelIntro)
        {
            _preLevelIntro = preLevelIntro;
            if (isActiveAndEnabled && Application.isPlaying)
            {
                ApplyPreferences();
            }
        }

        public void ConfigureLandmarkReveal(LandmarkRevealPresenter landmarkReveal)
        {
            _landmarkReveal = landmarkReveal;
            if (isActiveAndEnabled && Application.isPlaying)
            {
                ApplyPreferences();
            }
        }

        public void ConfigureForSetup(
            FirstPlayableController controller,
            FrontEndPresenter frontEnd,
            FeedbackAudioPresenter feedbackAudio,
            FeedbackHapticPresenter feedbackHaptics,
            CanvasGroup panelCanvasGroup,
            Button openButton,
            Button closeButton,
            Button soundButton,
            Button musicButton,
            Button hapticButton,
            Button languageButton,
            Button homeButton,
            Button exitButton,
            Image soundBackground,
            Image musicBackground,
            Image hapticBackground,
            Image soundIcon,
            Image musicIcon,
            Image hapticIcon,
            TMP_Text soundStateLabel,
            TMP_Text musicStateLabel,
            TMP_Text hapticStateLabel,
            AudioSource[] musicSources,
            bool persistPreferences = true,
            Button homeOpenButton = null,
            LocalizationService localization = null)
        {
            Unsubscribe();
            _controller = controller;
            _frontEnd = frontEnd;
            _feedbackAudio = feedbackAudio;
            _feedbackHaptics = feedbackHaptics;
            _localization = localization;
            _panelCanvasGroup = panelCanvasGroup;
            _openButton = openButton;
            _homeOpenButton = homeOpenButton;
            _closeButton = closeButton;
            _soundButton = soundButton;
            _musicButton = musicButton;
            _hapticButton = hapticButton;
            _languageButton = languageButton;
            _homeButton = homeButton;
            _exitButton = exitButton;
            _soundBackground = soundBackground;
            _musicBackground = musicBackground;
            _hapticBackground = hapticBackground;
            _soundIcon = soundIcon;
            _musicIcon = musicIcon;
            _hapticIcon = hapticIcon;
            _soundStateLabel = soundStateLabel;
            _musicStateLabel = musicStateLabel;
            _hapticStateLabel = hapticStateLabel;
            _musicSources = musicSources ?? Array.Empty<AudioSource>();
            _persistPreferences = persistPreferences;
            _preferencesLoaded = false;
            IsOpen = false;
            SetPanelVisible(false);
            RefreshToggleVisuals();

            if (isActiveAndEnabled && Application.isPlaying)
            {
                LoadPreferences();
                ApplyPreferences();
                Subscribe();
            }
        }

        public void Open()
        {
            LoadPreferences();
            ApplyPreferences();
            IsOpen = true;
            SetPanelVisible(true);
            _controller?.SetSimulationHold(
                SimulationHoldReason.Settings,
                true);
        }

        public void Close()
        {
            IsOpen = false;
            SetPanelVisible(false);
            _controller?.SetSimulationHold(
                SimulationHoldReason.Settings,
                false);
        }

        private void Awake()
        {
            LoadPreferences();
            ApplyPreferences();
            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            LoadPreferences();
            ApplyPreferences();
            SetPanelVisible(IsOpen);
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (Application.isPlaying)
            {
                Close();
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _openButton?.onClick.AddListener(OnOpenClicked);
            _homeOpenButton?.onClick.AddListener(OnOpenClicked);
            _closeButton?.onClick.AddListener(OnCloseClicked);
            _soundButton?.onClick.AddListener(OnSoundClicked);
            _musicButton?.onClick.AddListener(OnMusicClicked);
            _hapticButton?.onClick.AddListener(OnHapticClicked);
            _languageButton?.onClick.AddListener(OnLanguageClicked);
            _homeButton?.onClick.AddListener(OnHomeClicked);
            _exitButton?.onClick.AddListener(OnExitClicked);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _openButton?.onClick.RemoveListener(OnOpenClicked);
            _homeOpenButton?.onClick.RemoveListener(OnOpenClicked);
            _closeButton?.onClick.RemoveListener(OnCloseClicked);
            _soundButton?.onClick.RemoveListener(OnSoundClicked);
            _musicButton?.onClick.RemoveListener(OnMusicClicked);
            _hapticButton?.onClick.RemoveListener(OnHapticClicked);
            _languageButton?.onClick.RemoveListener(OnLanguageClicked);
            _homeButton?.onClick.RemoveListener(OnHomeClicked);
            _exitButton?.onClick.RemoveListener(OnExitClicked);
            _subscribed = false;
        }

        private void OnOpenClicked()
        {
            _controller?.NotifyUiFeedback();
            Open();
        }

        private void OnCloseClicked()
        {
            _controller?.NotifyUiFeedback();
            Close();
        }

        private void OnSoundClicked()
        {
            bool enabled = !_soundEnabled;
            if (!enabled)
            {
                _controller?.NotifyUiFeedback();
            }

            _soundEnabled = enabled;
            ApplyPreferences();
            SavePreferences();
            if (enabled)
            {
                _controller?.NotifyUiFeedback();
            }
        }

        private void OnMusicClicked()
        {
            _musicEnabled = !_musicEnabled;
            ApplyPreferences();
            SavePreferences();
            _controller?.NotifyUiFeedback();
        }

        private void OnHapticClicked()
        {
            bool enabled = !_hapticEnabled;
            if (!enabled)
            {
                _controller?.NotifyUiFeedback();
            }

            _hapticEnabled = enabled;
            ApplyPreferences();
            SavePreferences();
            if (enabled)
            {
                _controller?.NotifyUiFeedback();
            }
        }

        private void OnLanguageClicked()
        {
            _controller?.NotifyUiFeedback();
            _localization?.ToggleLanguage();
        }

        private void OnHomeClicked()
        {
            _controller?.NotifyUiFeedback();
            _frontEnd?.Open(FrontEndTab.Home);
            Close();
        }

        private void OnExitClicked()
        {
            _controller?.NotifyUiFeedback();
            SavePreferences();
            ExitRequested?.Invoke();
#if !UNITY_EDITOR
            Application.Quit();
#endif
        }

        private void LoadPreferences()
        {
            if (_preferencesLoaded)
            {
                return;
            }

            if (_persistPreferences)
            {
                _soundEnabled = PlayerPrefs.GetInt(
                    SoundPreferenceKey,
                    1) != 0;
                _musicEnabled = PlayerPrefs.GetInt(
                    MusicPreferenceKey,
                    1) != 0;
                _hapticEnabled = PlayerPrefs.GetInt(
                    HapticPreferenceKey,
                    1) != 0;
            }

            _preferencesLoaded = true;
        }

        private void SavePreferences()
        {
            if (!_persistPreferences)
            {
                return;
            }

            PlayerPrefs.SetInt(SoundPreferenceKey, _soundEnabled ? 1 : 0);
            PlayerPrefs.SetInt(MusicPreferenceKey, _musicEnabled ? 1 : 0);
            PlayerPrefs.SetInt(HapticPreferenceKey, _hapticEnabled ? 1 : 0);
            PlayerPrefs.Save();
            _progressStore.SaveSoundEnabled(_soundEnabled);
            _progressStore.SaveMusicEnabled(_musicEnabled);
            _progressStore.SaveHapticEnabled(_hapticEnabled);
        }

        private void ApplyPreferences()
        {
            _feedbackAudio?.SetEffectsEnabled(_soundEnabled);
            _preLevelIntro?.SetEffectsEnabled(_soundEnabled);
            _landmarkReveal?.SetEffectsEnabled(_soundEnabled);
            _feedbackHaptics?.SetHapticsEnabled(_hapticEnabled);
            foreach (AudioSource musicSource in _musicSources)
            {
                if (musicSource != null)
                {
                    musicSource.mute = !_musicEnabled;
                }
            }

            RefreshToggleVisuals();
        }

        private void RefreshToggleVisuals()
        {
            ApplyToggleVisual(
                _soundEnabled,
                _soundBackground,
                _soundIcon,
                _soundStateLabel);
            ApplyToggleVisual(
                _musicEnabled,
                _musicBackground,
                _musicIcon,
                _musicStateLabel);
            ApplyToggleVisual(
                _hapticEnabled,
                _hapticBackground,
                _hapticIcon,
                _hapticStateLabel);
        }

        private void ApplyToggleVisual(
            bool enabled,
            Image background,
            Image icon,
            TMP_Text stateLabel)
        {
            if (background != null)
            {
                background.color = enabled
                    ? _enabledBackgroundColor
                    : _disabledBackgroundColor;
            }

            if (icon != null)
            {
                icon.color = enabled
                    ? _enabledIconColor
                    : _disabledIconColor;
            }

            if (stateLabel != null)
            {
                stateLabel.text = enabled ? "ON" : "OFF";
                stateLabel.color = enabled
                    ? _enabledLabelColor
                    : _disabledLabelColor;
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (_panelCanvasGroup == null)
            {
                return;
            }

            _panelCanvasGroup.alpha = visible ? 1f : 0f;
            _panelCanvasGroup.interactable = visible;
            _panelCanvasGroup.blocksRaycasts = visible;
        }
    }
}
