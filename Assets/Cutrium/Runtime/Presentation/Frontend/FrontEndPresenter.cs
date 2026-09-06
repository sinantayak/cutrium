using System;
using System.Collections.Generic;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.Frontend
{
    public enum FrontEndTab
    {
        Shop,
        Home,
        Challenge,
    }

    [DisallowMultipleComponent]
    public sealed class FrontEndPresenter : MonoBehaviour
    {
        private static readonly Vector2 RefinedPlayButtonSize =
            new Vector2(420f, 172f);
        private static readonly Vector2 RefinedNodeNumberSize =
            new Vector2(112f, 112f);
        private const float ChallengeNodeSpacing = 226f;
        private const float ChallengeNodeBottomPadding = 118f;
        private const float ChallengeNodeTopPadding = 360f;
        private const float ChallengePlayBottomGap = 58f;
        private const float ChallengeMapPlayGap = 56f;
        private const float SelectedTabTopExtension = 30f;
        private const float NavigationCornerRadius = 34f;
        private const float SelectedTabCornerRadius = 24f;

        [Header("Flow")]
        [SerializeField] private FirstPlayableController _controller;
        [SerializeField] private PreLevelIntroPresenter _preLevelIntro;
        [SerializeField] private CanvasGroup _frontEndCanvasGroup;
        [SerializeField] private ScreenTransitionPresenter _screenTransition;

        [Header("Pages")]
        [SerializeField] private CanvasGroup _shopPage;
        [SerializeField] private CanvasGroup _homePage;
        [SerializeField] private CanvasGroup _challengePage;

        [Header("Artwork")]
        [SerializeField] private Image _backgroundArtwork;
        [SerializeField] private Image _homeLogo;

        [Header("Bottom Navigation")]
        [SerializeField] private Button _shopTabButton;
        [SerializeField] private Button _homeTabButton;
        [SerializeField] private Button _challengeTabButton;
        [SerializeField] private Image[] _tabPlates = Array.Empty<Image>();
        [SerializeField] private Image[] _tabIcons = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] _tabLabels = Array.Empty<TMP_Text>();

        [Header("Play")]
        [SerializeField] private Button _homePlayButton;
        [SerializeField] private Button _challengePlayButton;
        [SerializeField] private TMP_Text _challengePlayLabel;

        [Header("Challenge Map")]
        [SerializeField] private ScrollRect _challengeScrollRect;
        [SerializeField]
        private FrontEndLevelNodeView[] _levelNodes =
            Array.Empty<FrontEndLevelNodeView>();
        [SerializeField] private Image[] _pathConnectors = Array.Empty<Image>();

        [Header("Palette")]
        [SerializeField] private Color _activeTabColor =
            new Color(1f, 0.9f, 0.72f, 1f);
        [SerializeField] private Color _inactiveTabColor =
            new Color(0.52f, 0.31f, 0.2f, 1f);
        [SerializeField] private Color _activeTabPlateColor =
            new Color(0.35f, 0.13f, 0.04f, 0.94f);
        [SerializeField] private Color _inactiveTabPlateColor =
            new Color(0.18f, 0.07f, 0.025f, 0f);
        [SerializeField] private Color _navigationBackgroundColor =
            new Color(0.15f, 0.055f, 0.02f, 1f);
        [SerializeField] private Color _upcomingNodeColor =
            new Color(0.47f, 0.29f, 0.2f, 1f);
        [SerializeField] private Color _traversedNodeColor =
            new Color(0.86f, 0.46f, 0.16f, 1f);
        [SerializeField] private Color _selectedNodeColor = Color.white;
        [SerializeField] private Color _nodeNumberColor =
            new Color(1f, 0.89f, 0.7f, 1f);
        [SerializeField] private Color _upcomingPathColor =
            new Color(0.35f, 0.2f, 0.13f, 0.72f);
        [SerializeField] private Color _traversedPathColor =
            new Color(0.93f, 0.42f, 0.08f, 0.95f);

        private bool _subscribed;
        private bool _frontEndVisible;
        private int _selectedLevelNumber = 1;
        private readonly Vector3[] _navigationWorldCorners = new Vector3[4];

        public FirstPlayableController Controller => _controller;
        public CanvasGroup FrontEndCanvasGroup => _frontEndCanvasGroup;
        public ScreenTransitionPresenter ScreenTransition =>
            _screenTransition;
        public CanvasGroup ShopPage => _shopPage;
        public CanvasGroup HomePage => _homePage;
        public CanvasGroup ChallengePage => _challengePage;
        public Image BackgroundArtwork => _backgroundArtwork;
        public Image HomeLogo => _homeLogo;
        public Button HomePlayButton => _homePlayButton;
        public Button ChallengePlayButton => _challengePlayButton;
        public ScrollRect ChallengeScrollRect => _challengeScrollRect;

        public IReadOnlyList<FrontEndLevelNodeView> LevelNodes => _levelNodes;
        public IReadOnlyList<Image> PathConnectors => _pathConnectors;

        public FrontEndTab ActiveTab { get; private set; } = FrontEndTab.Home;
        public int SelectedLevelNumber => _selectedLevelNumber;
        public bool FrontEndVisible => _frontEndVisible;

        public void ConfigureForSetup(
            FirstPlayableController controller,
            PreLevelIntroPresenter preLevelIntro,
            CanvasGroup frontEndCanvasGroup,
            CanvasGroup shopPage,
            CanvasGroup homePage,
            CanvasGroup challengePage,
            Image backgroundArtwork,
            Image homeLogo,
            Button shopTabButton,
            Button homeTabButton,
            Button challengeTabButton,
            Image[] tabPlates,
            Image[] tabIcons,
            TMP_Text[] tabLabels,
            Button homePlayButton,
            Button challengePlayButton,
            TMP_Text challengePlayLabel,
            ScrollRect challengeScrollRect,
            FrontEndLevelNodeView[] levelNodes,
            Image[] pathConnectors,
            ScreenTransitionPresenter screenTransition = null)
        {
            Unsubscribe();
            _controller = controller;
            _preLevelIntro = preLevelIntro;
            _frontEndCanvasGroup = frontEndCanvasGroup;
            _shopPage = shopPage;
            _homePage = homePage;
            _challengePage = challengePage;
            _backgroundArtwork = backgroundArtwork;
            _homeLogo = homeLogo;
            _shopTabButton = shopTabButton;
            _homeTabButton = homeTabButton;
            _challengeTabButton = challengeTabButton;
            _tabPlates = tabPlates ?? Array.Empty<Image>();
            _tabIcons = tabIcons ?? Array.Empty<Image>();
            _tabLabels = tabLabels ?? Array.Empty<TMP_Text>();
            _homePlayButton = homePlayButton;
            _challengePlayButton = challengePlayButton;
            _challengePlayLabel = challengePlayLabel;
            _challengeScrollRect = challengeScrollRect;
            _levelNodes = levelNodes
                ?? Array.Empty<FrontEndLevelNodeView>();
            _pathConnectors = pathConnectors ?? Array.Empty<Image>();
            _screenTransition = screenTransition;
            _selectedLevelNumber = GetCurrentLevelNumber();
            if (isActiveAndEnabled && Application.isPlaying)
            {
                Subscribe();
            }

            RefreshNow();
        }

        public void ConfigureScreenTransitionForSetup(
            ScreenTransitionPresenter screenTransition)
        {
            _screenTransition = screenTransition
                ?? throw new ArgumentNullException(nameof(screenTransition));
        }

        public void Open(FrontEndTab tab = FrontEndTab.Home)
        {
            if (_controller == null)
            {
                return;
            }

            _frontEndVisible = true;
            _controller.SetSimulationHold(
                SimulationHoldReason.FrontEnd,
                true);
            _selectedLevelNumber = GetCurrentLevelNumber();
            ApplyStaticVisualRefinements();
            SetGroup(_frontEndCanvasGroup, true, true);
            ShowTab(tab);
        }

        public void ShowTab(FrontEndTab tab)
        {
            ActiveTab = tab;
            SetGroup(_shopPage, tab == FrontEndTab.Shop, tab == FrontEndTab.Shop);
            SetGroup(_homePage, tab == FrontEndTab.Home, tab == FrontEndTab.Home);
            SetGroup(
                _challengePage,
                tab == FrontEndTab.Challenge,
                tab == FrontEndTab.Challenge);
            ApplyTabVisuals();
            RefreshLevelMap();
            if (tab == FrontEndTab.Challenge)
            {
                ScrollToSelectedLevel();
            }
        }

        public void SelectLevel(int oneBasedLevelNumber)
        {
            if (oneBasedLevelNumber <= 0
                || oneBasedLevelNumber > _levelNodes.Length
                || IsLocked(oneBasedLevelNumber))
            {
                return;
            }

            _selectedLevelNumber = oneBasedLevelNumber;
            RefreshLevelMap();
        }

        private bool IsLocked(int oneBasedLevelNumber) =>
            _controller != null
            && oneBasedLevelNumber - 1 > _controller.HighestUnlockedLevelIndex;

        public void PlayCurrentLevel()
        {
            int levelNumber = GetCurrentLevelNumber();
            RunScreenTransition(() => StartGameplayAt(levelNumber));
        }

        public void PlaySelectedLevel()
        {
            int levelNumber = _selectedLevelNumber;
            RunScreenTransition(() => StartGameplayAt(levelNumber));
        }

        // Existing gameplay-focused Play Mode tests load the production scene
        // directly and intentionally bypass opening presentation. Keeping the
        // escape hatch here avoids test-only scene variants while preserving
        // the real player startup path.
        public void SkipForTesting()
        {
            _frontEndVisible = false;
            SetGroup(_frontEndCanvasGroup, false, false);
            _controller?.SetSimulationHold(
                SimulationHoldReason.FrontEnd,
                false);
        }

        public void RefreshNow()
        {
            ApplyStaticVisualRefinements();
            SetGroup(
                _frontEndCanvasGroup,
                _frontEndVisible,
                _frontEndVisible);
            ShowTab(ActiveTab);
        }

        private void Awake()
        {
            _selectedLevelNumber = GetCurrentLevelNumber();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                Subscribe();
                Open(FrontEndTab.Home);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (Application.isPlaying)
            {
                _controller?.SetSimulationHold(
                    SimulationHoldReason.FrontEnd,
                    false);
            }
        }

        private void LateUpdate()
        {
            if (!_frontEndVisible)
            {
                return;
            }

            UpdateNavigationUnderlayGeometry();
            if (ActiveTab == FrontEndTab.Challenge
                && _challengePage != null)
            {
                PositionChallengePlayAboveNavigation(
                    (RectTransform)_challengePage.transform);
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _shopTabButton?.onClick.AddListener(OnShopClicked);
            _homeTabButton?.onClick.AddListener(OnHomeClicked);
            _challengeTabButton?.onClick.AddListener(OnChallengeClicked);
            _homePlayButton?.onClick.AddListener(PlayCurrentLevel);
            _challengePlayButton?.onClick.AddListener(PlaySelectedLevel);
            if (_controller != null)
            {
                _controller.LevelMapProgressChanged +=
                    OnLevelMapProgressChanged;
            }
            foreach (FrontEndLevelNodeView node in _levelNodes)
            {
                if (node != null)
                {
                    node.Clicked += OnLevelNodeClicked;
                }
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _shopTabButton?.onClick.RemoveListener(OnShopClicked);
            _homeTabButton?.onClick.RemoveListener(OnHomeClicked);
            _challengeTabButton?.onClick.RemoveListener(OnChallengeClicked);
            _homePlayButton?.onClick.RemoveListener(PlayCurrentLevel);
            _challengePlayButton?.onClick.RemoveListener(PlaySelectedLevel);
            if (_controller != null)
            {
                _controller.LevelMapProgressChanged -=
                    OnLevelMapProgressChanged;
            }
            foreach (FrontEndLevelNodeView node in _levelNodes)
            {
                if (node != null)
                {
                    node.Clicked -= OnLevelNodeClicked;
                }
            }

            _subscribed = false;
        }

        private void OnShopClicked()
        {
            _controller?.NotifyUiFeedback();
            TransitionToTab(FrontEndTab.Shop);
        }

        private void OnHomeClicked()
        {
            _controller?.NotifyUiFeedback();
            TransitionToTab(FrontEndTab.Home);
        }

        private void OnChallengeClicked()
        {
            _controller?.NotifyUiFeedback();
            TransitionToTab(FrontEndTab.Challenge);
        }

        private void OnLevelNodeClicked(FrontEndLevelNodeView node)
        {
            _controller?.NotifyUiFeedback();
            SelectLevel(node.LevelNumber);
        }

        private void OnLevelMapProgressChanged()
        {
            RefreshLevelMap();
        }

        private void TransitionToTab(FrontEndTab tab)
        {
            if (ActiveTab == tab)
            {
                return;
            }

            RunScreenTransition(() => ShowTab(tab));
        }

        private void RunScreenTransition(Action midpointAction)
        {
            if (_screenTransition != null)
            {
                _screenTransition.TryTransition(midpointAction);
                return;
            }

            midpointAction();
        }

        private void StartGameplayAt(int oneBasedLevelNumber)
        {
            if (_controller == null
                || !_controller.TryStartLevel(oneBasedLevelNumber))
            {
                return;
            }

            _selectedLevelNumber = oneBasedLevelNumber;
            _frontEndVisible = false;
            SetGroup(_frontEndCanvasGroup, false, false);
            _controller.SetSimulationHold(
                SimulationHoldReason.FrontEnd,
                false);
            _preLevelIntro?.RefreshNow(0f);
        }

        private int GetCurrentLevelNumber()
        {
            if (_controller == null || _controller.LevelCount <= 0)
            {
                return 1;
            }

            return Mathf.Clamp(
                _controller.CurrentLevelIndex + 1,
                1,
                _controller.LevelCount);
        }

        private void ApplyTabVisuals()
        {
            for (int index = 0; index < 3; index++)
            {
                bool active = index == (int)ActiveTab;
                if (index < _tabPlates.Length && _tabPlates[index] != null)
                {
                    Image plate = _tabPlates[index];
                    plate.sprite = null;
                    plate.preserveAspect = false;
                    plate.color = Color.clear;
                    RectTransform plateRect =
                        (RectTransform)plate.transform;
                    plateRect.offsetMin = active
                        ? new Vector2(6f, -4f)
                        : new Vector2(14f, 4f);
                    plateRect.offsetMax = active
                        ? new Vector2(-6f, SelectedTabTopExtension)
                        : new Vector2(-14f, -4f);
                    FrontEndRoundedRectangleGraphic roundedFill =
                        ResolveOrCreateRoundedFill(
                            plateRect,
                            "RoundedFill");
                    if (roundedFill != null)
                    {
                        roundedFill.gameObject.SetActive(true);
                        roundedFill.ConfigureForSetup(
                            active
                                ? _activeTabPlateColor
                                : _inactiveTabPlateColor,
                            SelectedTabCornerRadius,
                            true);
                    }
                }

                Color foreground = active
                    ? _activeTabColor
                    : _inactiveTabColor;
                if (index < _tabIcons.Length && _tabIcons[index] != null)
                {
                    _tabIcons[index].color = foreground;
                    ((RectTransform)_tabIcons[index].transform)
                        .anchoredPosition = new Vector2(
                            0f,
                            active ? 34f : 28f);
                }

                if (index < _tabLabels.Length && _tabLabels[index] != null)
                {
                    _tabLabels[index].color = foreground;
                    ((RectTransform)_tabLabels[index].transform)
                        .anchoredPosition = new Vector2(
                            0f,
                            active ? 12f : 8f);
                }
            }
        }

        private void RefreshLevelMap()
        {
            int highestUnlocked = _controller != null
                ? _controller.HighestUnlockedLevelIndex
                : 0;
            for (int index = 0; index < _levelNodes.Length; index++)
            {
                FrontEndLevelNodeView node = _levelNodes[index];
                if (node == null)
                {
                    continue;
                }

                FrontEndLevelNodeState state;
                if (index > highestUnlocked)
                {
                    // Locked: never let a stale/attempted selection show a
                    // level beyond reached progress as available.
                    state = FrontEndLevelNodeState.Locked;
                }
                else if (node.LevelNumber == _selectedLevelNumber)
                {
                    state = FrontEndLevelNodeState.Selected;
                }
                else if (index < highestUnlocked)
                {
                    // Fully unlocked past this point, so it has already
                    // been completed -- independent of where the resume
                    // position (CurrentLevelIndex) currently sits, which
                    // moves backward whenever an earlier level is replayed.
                    state = FrontEndLevelNodeState.Traversed;
                }
                else
                {
                    state = FrontEndLevelNodeState.Upcoming;
                }

                node.ApplyState(
                    state,
                    _upcomingNodeColor,
                    _traversedNodeColor,
                    _selectedNodeColor,
                    _nodeNumberColor);
                node.ApplyBestStarRating(
                    _controller != null
                        ? _controller.GetBestStarRatingForLevel(
                            node.LevelNumber)
                        : 0);
            }

            for (int index = 0; index < _pathConnectors.Length; index++)
            {
                if (_pathConnectors[index] != null)
                {
                    _pathConnectors[index].color = index < highestUnlocked
                        ? _traversedPathColor
                        : _upcomingPathColor;
                }
            }

            if (_challengePlayLabel != null)
            {
                _challengePlayLabel.text =
                    $"PLAY LEVEL {_selectedLevelNumber}";
            }
        }

        private void ScrollToSelectedLevel()
        {
            if (_challengeScrollRect == null || _levelNodes.Length <= 1)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            _challengeScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
                (_selectedLevelNumber - 1f) / (_levelNodes.Length - 1f));
        }

        private void ApplyStaticVisualRefinements()
        {
            ResolveArtworkReferences();
            ApplyArtworkRefinements();
            ApplyPlayButtonRefinement(_homePlayButton);
            ApplyPlayButtonRefinement(_challengePlayButton, 46f, 28f);
            ApplyBottomNavigationRefinements();
            ApplyFullBleedChallengeLayout();
            EnsureAttentionAnimations();

            foreach (FrontEndLevelNodeView node in _levelNodes)
            {
                if (node == null || node.NumberLabel == null)
                {
                    continue;
                }

                RectTransform numberRect =
                    (RectTransform)node.NumberLabel.transform;
                numberRect.anchorMin = new Vector2(0.5f, 0.5f);
                numberRect.anchorMax = new Vector2(0.5f, 0.5f);
                numberRect.pivot = new Vector2(0.5f, 0.5f);
                numberRect.anchoredPosition = Vector2.zero;
                numberRect.sizeDelta = RefinedNodeNumberSize;
                node.NumberLabel.alignment = TextAlignmentOptions.Center;
                node.NumberLabel.enableAutoSizing = true;
                node.NumberLabel.fontSize = 58f;
                node.NumberLabel.fontSizeMin = 36f;
                node.NumberLabel.fontSizeMax = 58f;
            }
        }

        private void ResolveArtworkReferences()
        {
            if (_frontEndCanvasGroup == null)
            {
                return;
            }

            Transform root = _frontEndCanvasGroup.transform;
            if (_backgroundArtwork == null)
            {
                _backgroundArtwork = root.Find("BackgroundArtwork")
                    ?.GetComponent<Image>();
            }

            if (_homeLogo == null && _homePage != null)
            {
                _homeLogo = _homePage.transform.Find("CutriumLogo")
                    ?.GetComponent<Image>();
            }

        }

        private void ApplyArtworkRefinements()
        {
            if (_backgroundArtwork != null)
            {
                _backgroundArtwork.preserveAspect = false;
                _backgroundArtwork.raycastTarget = false;
                _backgroundArtwork.color = Color.white;
                _backgroundArtwork.transform.SetAsFirstSibling();
            }

            if (_homeLogo != null)
            {
                _homeLogo.preserveAspect = true;
                _homeLogo.raycastTarget = false;
                RectTransform logoRect =
                    (RectTransform)_homeLogo.transform;
                logoRect.anchorMin = new Vector2(0.5f, 0.64f);
                logoRect.anchorMax = new Vector2(0.5f, 0.64f);
                logoRect.pivot = new Vector2(0.5f, 0.5f);
                logoRect.anchoredPosition = new Vector2(0f, 30f);
                logoRect.sizeDelta = new Vector2(610f, 590f);
            }

            if (_homePlayButton != null)
            {
                RectTransform playRect =
                    (RectTransform)_homePlayButton.transform;
                playRect.anchorMin = new Vector2(0.5f, 0.31f);
                playRect.anchorMax = new Vector2(0.5f, 0.31f);
                playRect.pivot = new Vector2(0.5f, 0.5f);
                playRect.anchoredPosition = Vector2.zero;
            }
        }

        private void ApplyFullBleedChallengeLayout()
        {
            if (_frontEndCanvasGroup == null
                || _challengePage == null
                || _challengeScrollRect == null)
            {
                return;
            }

            RectTransform frontEndRoot =
                (RectTransform)_frontEndCanvasGroup.transform;
            RectTransform challengeRect =
                (RectTransform)_challengePage.transform;
            if (challengeRect.parent != frontEndRoot)
            {
                challengeRect.SetParent(frontEndRoot, false);
            }

            Stretch(challengeRect);
            Transform background = frontEndRoot.Find("BackgroundArtwork");
            challengeRect.SetSiblingIndex(background != null ? 1 : 0);

            Transform header = challengeRect.Find("ChallengeHeader");
            if (header != null)
            {
                header.gameObject.SetActive(false);
            }

            RectTransform scrollRect =
                (RectTransform)_challengeScrollRect.transform;
            Stretch(scrollRect);
            Image mapSurface = _challengeScrollRect.GetComponent<Image>();
            if (mapSurface != null)
            {
                mapSurface.sprite = null;
                mapSurface.color = Color.clear;
            }

            if (_challengeScrollRect.viewport != null)
            {
                RectMask2D mask = _challengeScrollRect.viewport
                    .GetComponent<RectMask2D>();
                if (mask != null)
                {
                    mask.padding = Vector4.zero;
                }
            }

            ApplyChallengePathLayout();
            PositionChallengePlayAboveNavigation(challengeRect);
        }

        private void ApplyChallengePathLayout()
        {
            RectTransform content = _challengeScrollRect.content;
            if (content == null)
            {
                return;
            }

            content.sizeDelta = new Vector2(
                content.sizeDelta.x,
                ChallengeNodeBottomPadding
                    + Mathf.Max(0, _levelNodes.Length - 1)
                        * ChallengeNodeSpacing
                    + ChallengeNodeTopPadding);

            for (int index = 0; index < _levelNodes.Length; index++)
            {
                if (_levelNodes[index] == null)
                {
                    continue;
                }

                RectTransform nodeRect =
                    (RectTransform)_levelNodes[index].transform;
                Vector2 position = nodeRect.anchoredPosition;
                position.y = ChallengeNodeBottomPadding
                    + index * ChallengeNodeSpacing;
                nodeRect.anchoredPosition = position;
            }

            int connectorCount = Mathf.Min(
                _pathConnectors.Length,
                Mathf.Max(0, _levelNodes.Length - 1));
            for (int index = 0; index < connectorCount; index++)
            {
                Image connector = _pathConnectors[index];
                if (connector == null
                    || _levelNodes[index] == null
                    || _levelNodes[index + 1] == null)
                {
                    continue;
                }

                Vector2 start = ((RectTransform)_levelNodes[index].transform)
                    .anchoredPosition;
                Vector2 end = ((RectTransform)_levelNodes[index + 1].transform)
                    .anchoredPosition;
                Vector2 delta = end - start;
                RectTransform connectorRect =
                    (RectTransform)connector.transform;
                connectorRect.anchoredPosition = (start + end) * 0.5f;
                connectorRect.sizeDelta = new Vector2(
                    Mathf.Max(24f, delta.magnitude - 116f),
                    14f);
                connectorRect.localEulerAngles = new Vector3(
                    0f,
                    0f,
                    Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        private void PositionChallengePlayAboveNavigation(
            RectTransform challengeRect)
        {
            if (_challengePlayButton == null)
            {
                return;
            }

            RectTransform playRect =
                (RectTransform)_challengePlayButton.transform;
            playRect.anchorMin = new Vector2(0.5f, 0f);
            playRect.anchorMax = new Vector2(0.5f, 0f);
            playRect.pivot = new Vector2(0.5f, 0f);

            RectTransform navigation = _challengeTabButton != null
                ? _challengeTabButton.transform.parent as RectTransform
                : null;
            float bottom = 194f;
            if (navigation != null)
            {
                navigation.GetWorldCorners(_navigationWorldCorners);
                Vector3 localTop = challengeRect.InverseTransformPoint(
                    _navigationWorldCorners[1]);
                bottom = localTop.y
                    - challengeRect.rect.yMin
                    + ChallengePlayBottomGap;
            }

            playRect.anchoredPosition = new Vector2(0f, bottom);
            PositionChallengeMapAbovePlay(playRect);
        }

        private void PositionChallengeMapAbovePlay(RectTransform playRect)
        {
            RectTransform mapRect =
                (RectTransform)_challengeScrollRect.transform;
            float mapBottom = playRect.anchoredPosition.y
                + playRect.rect.yMax
                + ChallengeMapPlayGap;
            mapRect.offsetMin = new Vector2(0f, mapBottom);
            mapRect.offsetMax = Vector2.zero;
        }

        private void ApplyBottomNavigationRefinements()
        {
            RectTransform navigation = GetNavigationRect();
            if (navigation == null || _frontEndCanvasGroup == null)
            {
                return;
            }

            Image navigationImage = navigation.GetComponent<Image>();
            if (navigationImage != null)
            {
                navigationImage.color = Color.clear;
                navigationImage.raycastTarget = true;
            }

            RectTransform frontEndRoot =
                (RectTransform)_frontEndCanvasGroup.transform;
            RectTransform underlay = ResolveOrCreateUiChild(
                frontEndRoot,
                "BottomNavigationUnderlay");
            if (underlay == null)
            {
                return;
            }

            Transform safeArea = navigation.parent;
            safeArea.SetAsLastSibling();
            underlay.SetSiblingIndex(Mathf.Max(
                0,
                safeArea.GetSiblingIndex() - 1));
            FrontEndRoundedRectangleGraphic rounded =
                ResolveOrAddComponent<FrontEndRoundedRectangleGraphic>(
                    underlay.gameObject);
            rounded?.ConfigureForSetup(
                _navigationBackgroundColor,
                NavigationCornerRadius,
                false);
            UpdateNavigationUnderlayGeometry();
        }

        private void UpdateNavigationUnderlayGeometry()
        {
            if (_frontEndCanvasGroup == null)
            {
                return;
            }

            RectTransform navigation = GetNavigationRect();
            RectTransform frontEndRoot =
                (RectTransform)_frontEndCanvasGroup.transform;
            RectTransform underlay = frontEndRoot
                .Find("BottomNavigationUnderlay") as RectTransform;
            if (navigation == null || underlay == null)
            {
                return;
            }

            navigation.GetWorldCorners(_navigationWorldCorners);
            Vector3 localTop = frontEndRoot.InverseTransformPoint(
                _navigationWorldCorners[1]);
            float height = Mathf.Max(
                navigation.rect.height,
                localTop.y - frontEndRoot.rect.yMin);
            underlay.anchorMin = Vector2.zero;
            underlay.anchorMax = new Vector2(1f, 0f);
            underlay.pivot = new Vector2(0.5f, 0f);
            underlay.anchoredPosition = Vector2.zero;
            underlay.sizeDelta = new Vector2(0f, height);
        }

        private void EnsureAttentionAnimations()
        {
            ConfigurePlayLabelPulse(_homePlayButton, 0f);
            ConfigurePlayLabelPulse(_challengePlayButton, 0.5f);

            for (int index = 0; index < _levelNodes.Length; index++)
            {
                Image glow = _levelNodes[index]?.SelectionGlow;
                if (glow == null)
                {
                    continue;
                }

                Color glowColor = glow.color;
                glowColor.a = 0.78f;
                glow.color = glowColor;
                FrontEndPulseAnimator glowPulse =
                    glow.GetComponent<FrontEndPulseAnimator>();
                if (glowPulse != null)
                {
                    glowPulse.enabled = false;
                }

                CanvasGroup glowGroup = glow.GetComponent<CanvasGroup>();
                if (glowGroup != null)
                {
                    glowGroup.alpha = 1f;
                }

                glow.transform.localScale = Vector3.one;
            }
        }

        private static void ConfigurePlayLabelPulse(
            Button button,
            float phaseOffset)
        {
            if (button == null)
            {
                return;
            }

            FrontEndPulseAnimator buttonPulse =
                button.GetComponent<FrontEndPulseAnimator>();
            if (buttonPulse != null)
            {
                buttonPulse.enabled = false;
            }

            RectTransform label = button.transform.Find("Label")
                as RectTransform;
            ConfigurePulse(
                label,
                null,
                0.78f,
                0.045f,
                1f,
                1f,
                phaseOffset);
        }

        private static void ConfigurePulse(
            RectTransform target,
            CanvasGroup canvasGroup,
            float cyclesPerSecond,
            float scaleAmplitude,
            float minimumAlpha,
            float maximumAlpha,
            float phaseOffset)
        {
            if (target == null)
            {
                return;
            }

            FrontEndPulseAnimator pulse =
                ResolveOrAddComponent<FrontEndPulseAnimator>(
                    target.gameObject);
            pulse?.ConfigureForSetup(
                target,
                canvasGroup,
                cyclesPerSecond,
                scaleAmplitude,
                minimumAlpha,
                maximumAlpha,
                phaseOffset);
        }

        private RectTransform GetNavigationRect()
        {
            return _homeTabButton != null
                ? _homeTabButton.transform.parent as RectTransform
                : _challengeTabButton != null
                    ? _challengeTabButton.transform.parent as RectTransform
                    : null;
        }

        private static FrontEndRoundedRectangleGraphic
            ResolveOrCreateRoundedFill(
                RectTransform parent,
                string name)
        {
            RectTransform fill = ResolveOrCreateUiChild(parent, name);
            if (fill == null)
            {
                return null;
            }

            Stretch(fill);
            fill.SetAsFirstSibling();
            return ResolveOrAddComponent<FrontEndRoundedRectangleGraphic>(
                fill.gameObject);
        }

        private static RectTransform ResolveOrCreateUiChild(
            Transform parent,
            string name)
        {
            Transform existing = parent.Find(name);
            if (existing is RectTransform existingRect)
            {
                return existingRect;
            }

            if (!Application.isPlaying)
            {
                return null;
            }

            var gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static T ResolveOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null && Application.isPlaying)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyPlayButtonRefinement(
            Button button,
            float maximumFontSize = 56f,
            float minimumFontSize = 34f)
        {
            if (button == null)
            {
                return;
            }

            RectTransform buttonRect = (RectTransform)button.transform;
            buttonRect.sizeDelta = RefinedPlayButtonSize;
            TMP_Text label = buttonRect.Find("Label")?.GetComponent<TMP_Text>();
            if (label == null)
            {
                return;
            }

            RectTransform labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(30f, 24f);
            labelRect.offsetMax = new Vector2(-30f, -24f);
            label.enableAutoSizing = true;
            label.fontSize = maximumFontSize;
            label.fontSizeMin = minimumFontSize;
            label.fontSizeMax = maximumFontSize;
            label.alignment = TextAlignmentOptions.Center;
        }

        private static void SetGroup(
            CanvasGroup group,
            bool visible,
            bool interactive)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible && interactive;
            group.blocksRaycasts = visible && interactive;
        }
    }
}
