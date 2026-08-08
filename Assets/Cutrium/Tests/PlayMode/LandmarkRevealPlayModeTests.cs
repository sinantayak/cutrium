using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cutrium.Gameplay.Barriers;
using Cutrium.Gameplay.Geometry;
using Cutrium.Gameplay.Session;
using Cutrium.Presentation.Barriers;
using Cutrium.Presentation.Landmark;
using Cutrium.Unity.Bootstrap;
using Cutrium.Unity.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cutrium.PlayModeTests
{
    public sealed class LandmarkRevealPlayModeTests
    {
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";

        private GameObject _root;
        private LandmarkRevealPresenter _landmarkPresenter;
        private BarrierPresenter _barrierPresenter;
        private EventSystem _eventSystem;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(
                ScenePath,
                LoadSceneMode.Single);
            yield return null;
            _root = SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(candidate => candidate.name == "VerticalSliceRoot");
            _landmarkPresenter = _root
                .GetComponentInChildren<LandmarkRevealPresenter>(true);
            _barrierPresenter = _root
                .GetComponentInChildren<BarrierPresenter>(true);
            _eventSystem = _root.GetComponentInChildren<SceneCompositionRoot>(true)
                .EventSystem;
            Canvas.ForceUpdateCanvases();
        }

        [Test]
        public void Scene_HasOneLandmarkRevealPresenterWithThreeLandmarksAndTunedBarrier()
        {
            Assert.That(
                _root.GetComponentsInChildren<LandmarkRevealPresenter>(true),
                Has.Length.EqualTo(1));
            Assert.That(_landmarkPresenter.ArtworkImage, Is.Not.Null);
            Assert.That(_landmarkPresenter.VeilRoot, Is.Not.Null);
            Assert.That(_landmarkPresenter.CompletionArtworkImage, Is.Not.Null);
            Assert.That(_landmarkPresenter.CompletionTitleText, Is.Not.Null);
            Assert.That(
                _landmarkPresenter.CompletionDescriptionText,
                Is.Not.Null);
            Assert.That(_landmarkPresenter.CompletionSectorText, Is.Not.Null);
            Assert.That(_landmarkPresenter.Landmarks.Count, Is.EqualTo(3));
            Assert.That(
                _landmarkPresenter.Landmarks.Select(l => l.LandmarkId),
                Is.EqualTo(new[]
                {
                    "galata-kulesi",
                    "coastal-lagoon",
                    "desert-dunes",
                }));
            foreach (LandmarkDefinition landmark in _landmarkPresenter.Landmarks)
            {
                Assert.That(landmark.Artwork, Is.Not.Null);
            }

            Assert.That(_barrierPresenter.VisualLogicalThickness,
                Is.EqualTo(0.13f).Within(0.0001f));
        }

        [Test]
        public void PowerButtons_AreHiddenFromTheDefaultGameplayHud()
        {
            // The default level catalog grants zero Freeze Pulse/Instant
            // Barrier charges, which left these buttons permanently
            // non-interactable (visible but dead) in real play; see
            // Milestone6ThreatsAndPowersPlayModeTests for the matching
            // reference-still-valid check.
            Transform powerControls = _root.transform.Find(
                "Canvas/SafeAreaRoot/PowerControls");
            Assert.That(powerControls, Is.Not.Null);
            Assert.That(powerControls.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void QuickRetryButton_ExistsAndIsInteractable()
        {
            Transform safeArea = _root.transform.Find("Canvas/SafeAreaRoot");
            Transform bottomHud = safeArea.Find("BottomHUD");
            Transform retryTransform = bottomHud.Find("QuickRetryButton");
            Assert.That(retryTransform, Is.Not.Null);

            Button retryButton = retryTransform.GetComponent<Button>();
            Assert.That(retryButton, Is.Not.Null);
            Assert.That(retryButton.interactable, Is.True);

            // Guard against the button being stretched to fill the whole
            // row (the bug this test originally caught): it must keep a
            // small, deliberate footprint.
            var retryRect = (RectTransform)retryTransform;
            Assert.That(retryRect.rect.width, Is.LessThanOrEqualTo(160f));

            GameObject[] hits = RaycastAtCenter(retryRect);
            Assert.That(hits, Is.Not.Empty);
            Assert.That(hits[0], Is.SameAs(retryButton.gameObject));
        }

        [UnityTest]
        public IEnumerator QuickRetryButton_RealMouseClickTriggersRetry()
        {
            InputSettings.BackgroundBehavior originalBackground =
                InputSystem.settings.backgroundBehavior;
            InputSettings.EditorInputBehaviorInPlayMode originalEditorBehavior =
                InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior =
                InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode
                    .AllDeviceInputAlwaysGoesToGameView;
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.EnableDevice(mouse);
            try
            {
                FirstPlayableController controller = _root
                    .GetComponentInChildren<FirstPlayableController>(true);
                Transform safeArea = _root.transform.Find("Canvas/SafeAreaRoot");
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    (RectTransform)safeArea);
                Transform retryTransform = safeArea.Find(
                    "BottomHUD/QuickRetryButton");
                Vector2 center = GetScreenCenter((RectTransform)retryTransform);
                int before = controller.RetryCount;

                // A real press+release through the Input System, exactly as
                // a player would tap it -- not Button.onClick.Invoke() --
                // is the only way to prove the click genuinely reaches the
                // button through the live EventSystem/InputSystemUIInputModule
                // pipeline.
                InputSystem.QueueDeltaStateEvent(mouse.position, center);
                yield return null;
                InputSystem.QueueDeltaStateEvent(mouse.press, true);
                yield return null;
                InputSystem.QueueDeltaStateEvent(mouse.press, false);
                yield return null;
                yield return null;

                Assert.That(controller.RetryCount, Is.EqualTo(before + 1));
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                InputSystem.settings.backgroundBehavior = originalBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode =
                    originalEditorBehavior;
            }
        }

        private static Vector2 GetScreenCenter(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return RectTransformUtility.WorldToScreenPoint(
                null,
                (corners[0] + corners[2]) * 0.5f);
        }

        private GameObject[] RaycastAtCenter(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 center = RectTransformUtility.WorldToScreenPoint(
                null,
                (corners[0] + corners[2]) * 0.5f);
            var eventData = new PointerEventData(_eventSystem)
            {
                position = center,
            };
            var results = new List<RaycastResult>();
            _eventSystem.RaycastAll(eventData, results);
            return results.Select(result => result.gameObject).ToArray();
        }

        [Test]
        public void ArtworkSelection_CyclesWithLevelIndex()
        {
            var rig = new IsolatedRig(2);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();
                Sprite firstArtwork = rig.Presenter.ArtworkImage.sprite;
                Assert.That(firstArtwork,
                    Is.SameAs(rig.Landmarks[0].Artwork));

                Assert.That(rig.CompleteAndAdvance(), Is.True);
                rig.Presenter.RefreshNow();

                Assert.That(rig.Presenter.ArtworkImage.sprite,
                    Is.SameAs(rig.Landmarks[1].Artwork));
                Assert.That(rig.Presenter.ArtworkImage.sprite,
                    Is.Not.SameAs(firstArtwork));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator VeilCoversOnlyActiveRoomsAndFadesOutOnCapture()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                rig.Presenter.RefreshNow();
                Assert.That(rig.Presenter.VisibleVeilCount, Is.EqualTo(1));

                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                // The completed level forces a full reveal regardless of any
                // remaining active area.
                Assert.That(rig.Presenter.VisibleVeilCount, Is.EqualTo(0));

                float waited = 0f;
                while (!rig.Presenter.AllVeilsFullyRevealed && waited < 2f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                Assert.That(rig.Presenter.AllVeilsFullyRevealed, Is.True);
            }
            finally
            {
                rig.Dispose();
            }
        }

        [Test]
        public void CompletionRevealsFullArtworkAndPopulatesCard()
        {
            var rig = new IsolatedRig(1);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                Assert.That(rig.Presenter.CompletionArtworkImage.sprite,
                    Is.SameAs(rig.Landmarks[0].Artwork));
                Assert.That(rig.Presenter.CompletionTitleText.text,
                    Is.EqualTo(rig.Landmarks[0].DisplayTitle));
                Assert.That(rig.Presenter.CompletionDescriptionText.text,
                    Is.EqualTo(rig.Landmarks[0].ShortDescription));
                Assert.That(rig.Presenter.CompletionSectorText.text,
                    Is.EqualTo(rig.Landmarks[0].Sector));
            }
            finally
            {
                rig.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator CompletionSequenceStagesScrimThenContentThenButtons()
        {
            var timing = new LandmarkCompletionTiming(
                scrimFadeSeconds: 0.05f,
                contentDelaySeconds: 0.05f,
                contentFadeSeconds: 0.05f,
                buttonsDelaySeconds: 0.2f,
                buttonsFadeSeconds: 0.05f);
            var rig = new IsolatedRig(1, timing);
            try
            {
                rig.Controller.AdvanceSimulation(0f);
                Assert.That(rig.CompleteWithoutAdvancing(), Is.True);
                rig.Presenter.RefreshNow();

                // Immediately after completion nothing has faded in yet.
                Assert.That(rig.Presenter.ScrimCanvasGroup.alpha, Is.Zero);
                Assert.That(rig.Presenter.ContentCanvasGroup.alpha, Is.Zero);
                Assert.That(rig.Presenter.NextCanvasGroup.alpha, Is.Zero);

                float waited = 0f;
                while (rig.Presenter.ScrimCanvasGroup.alpha < 1f && waited < 2f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                // The scrim finishes fading in well before content/buttons
                // are given a chance to start.
                Assert.That(rig.Presenter.ScrimCanvasGroup.alpha, Is.EqualTo(1f));
                Assert.That(rig.Presenter.NextCanvasGroup.alpha, Is.Zero);
                Assert.That(rig.Presenter.NextCanvasGroup.interactable, Is.False);

                waited = 0f;
                while (rig.Presenter.NextCanvasGroup.alpha < 1f && waited < 2f)
                {
                    yield return null;
                    rig.Presenter.RefreshNow();
                    waited += Time.unscaledDeltaTime;
                }

                Assert.That(rig.Presenter.ContentCanvasGroup.alpha, Is.EqualTo(1f));
                Assert.That(rig.Presenter.RetryCanvasGroup.alpha, Is.EqualTo(1f));
                Assert.That(rig.Presenter.NextCanvasGroup.alpha, Is.EqualTo(1f));
                Assert.That(rig.Presenter.NextCanvasGroup.interactable, Is.True);
                Assert.That(rig.Presenter.NextCanvasGroup.blocksRaycasts, Is.True);
            }
            finally
            {
                rig.Dispose();
            }
        }

        private sealed class IsolatedRig
        {
            private readonly List<Texture2D> _textures = new List<Texture2D>();
            private readonly List<Sprite> _sprites = new List<Sprite>();
            private readonly GameObject _simulationObject;

            public IsolatedRig(int levelCount, LandmarkCompletionTiming? timing = null)
            {
                _simulationObject = new GameObject("LandmarkRevealTestRig");
                _simulationObject.SetActive(false);
                Controller =
                    _simulationObject.AddComponent<FirstPlayableController>();

                var levels = new CoreFunLevelDefinition[levelCount];
                for (int index = 0; index < levelCount; index++)
                {
                    levels[index] = new CoreFunLevelDefinition(
                        $"tiny-{index}",
                        index + 1,
                        new Vector2(5f, 8f),
                        new Vector2(1f, 0f),
                        1f,
                        0.35f,
                        0.05f,
                        8f,
                        0.08f,
                        3f,
                        8,
                        16,
                        8,
                        "Landmark reveal test level.",
                        10f);
                }

                Controller.ConfigureLevelsForSetup(levels);

                Landmarks = new LandmarkDefinition[2];
                for (int index = 0; index < Landmarks.Length; index++)
                {
                    var texture = new Texture2D(2, 2);
                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, 2f, 2f),
                        new Vector2(0.5f, 0.5f));
                    _textures.Add(texture);
                    _sprites.Add(sprite);
                    LandmarkDefinition landmark =
                        ScriptableObject.CreateInstance<LandmarkDefinition>();
                    landmark.ConfigureForSetup(
                        $"landmark-{index}",
                        $"Landmark {index}",
                        $"Description {index}",
                        $"Sector {index}",
                        sprite);
                    Landmarks[index] = landmark;
                }

                var frameObject = new GameObject(
                    "Frame",
                    typeof(RectTransform));
                var frame = (RectTransform)frameObject.transform;
                frame.SetParent(_simulationObject.transform, false);
                frame.sizeDelta = new Vector2(625f, 1000f);

                var artworkObject = new GameObject(
                    "Artwork",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                artworkObject.transform.SetParent(frame, false);
                Image artworkImage = artworkObject.GetComponent<Image>();

                var veilRootObject = new GameObject(
                    "VeilRoot",
                    typeof(RectTransform));
                veilRootObject.transform.SetParent(frame, false);
                var veilRoot = (RectTransform)veilRootObject.transform;

                Text titleText = CreateText(_simulationObject.transform);
                Text descriptionText = CreateText(_simulationObject.transform);
                Text sectorText = CreateText(_simulationObject.transform);
                var completionArtworkObject = new GameObject(
                    "CompletionArtwork",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                completionArtworkObject.transform.SetParent(
                    _simulationObject.transform,
                    false);
                Image completionArtworkImage =
                    completionArtworkObject.GetComponent<Image>();

                CanvasGroup scrimGroup = CreateGroup(_simulationObject.transform);
                CanvasGroup contentGroup = CreateGroup(_simulationObject.transform);
                CanvasGroup statsGroup = CreateGroup(_simulationObject.transform);
                CanvasGroup retryGroup = CreateGroup(_simulationObject.transform);
                CanvasGroup nextGroup = CreateGroup(_simulationObject.transform);

                Presenter = _simulationObject
                    .AddComponent<LandmarkRevealPresenter>();
                Presenter.Configure(
                    Controller,
                    frame,
                    artworkImage,
                    veilRoot,
                    null,
                    new Color(0.09f, 0.11f, 0.15f, 0.94f),
                    0.2f,
                    completionArtworkImage,
                    scrimGroup,
                    contentGroup,
                    statsGroup,
                    retryGroup,
                    nextGroup,
                    titleText,
                    sectorText,
                    descriptionText,
                    timing ?? LandmarkCompletionTiming.Default,
                    Landmarks);

                _simulationObject.SetActive(true);
            }

            public FirstPlayableController Controller { get; }
            public LandmarkRevealPresenter Presenter { get; }
            public LandmarkDefinition[] Landmarks { get; }

            public bool CompleteWithoutAdvancing()
            {
                BarrierStartResult start = Controller.SubmitBarrierIntent(
                    new BarrierIntent(
                        new LogicalPoint(0.6f, 8f),
                        BarrierOrientation.Vertical));
                if (!start.Accepted)
                {
                    return false;
                }

                for (int tick = 0;
                     tick < 600
                     && Controller.Session.LevelStatus
                         != CaptureLevelStatus.Completed;
                     tick++)
                {
                    Controller.AdvanceSimulation(
                        FirstPlayableController.SimulationStep);
                }

                return Controller.Session.LevelStatus
                    == CaptureLevelStatus.Completed;
            }

            public bool CompleteAndAdvance()
            {
                return CompleteWithoutAdvancing()
                    && Controller.TryAdvanceToNextLevel();
            }

            public void Dispose()
            {
                Object.DestroyImmediate(_simulationObject);
                foreach (LandmarkDefinition landmark in Landmarks)
                {
                    Object.DestroyImmediate(landmark);
                }

                foreach (Sprite sprite in _sprites)
                {
                    Object.DestroyImmediate(sprite);
                }

                foreach (Texture2D texture in _textures)
                {
                    Object.DestroyImmediate(texture);
                }
            }

            private static Text CreateText(Transform parent)
            {
                var textObject = new GameObject(
                    "Text",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                textObject.transform.SetParent(parent, false);
                return textObject.GetComponent<Text>();
            }

            private static CanvasGroup CreateGroup(Transform parent)
            {
                var groupObject = new GameObject(
                    "Group",
                    typeof(RectTransform),
                    typeof(CanvasGroup));
                groupObject.transform.SetParent(parent, false);
                return groupObject.GetComponent<CanvasGroup>();
            }
        }
    }
}
