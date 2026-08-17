using System;
using System.IO;
using System.Linq;
using System.Text;
using Cutrium.Presentation.HUD;
using Cutrium.Unity.Bootstrap;
using Cutrium.Unity.Input;
using Cutrium.Unity.Layout;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cutrium.Editor.Setup
{
    public static class Milestone1BSceneSetup
    {
        public const string InputAssetPath =
            "Assets/Cutrium/Input/CutriumInput.inputactions";
        public const string VerticalSliceScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";
        public const string SampleScenePath =
            "Assets/Scenes/SampleScene.unity";

        private static readonly Color BackgroundColor =
            new Color(0.025f, 0.055f, 0.075f, 1f);
        private static readonly Color HudColor =
            new Color(0.08f, 0.15f, 0.19f, 0.98f);
        private static readonly Color BoardColor =
            new Color(0.08f, 0.24f, 0.27f, 1f);
        private static readonly Color AccentColor =
            new Color(0.30f, 0.92f, 0.76f, 1f);
        private static readonly Color PrimaryTextColor =
            new Color(0.91f, 0.98f, 0.96f, 1f);
        private static readonly Color SecondaryTextColor =
            new Color(0.63f, 0.78f, 0.76f, 1f);

        [MenuItem("Cutrium/Setup/Milestone 1B Scene Shell")]
        public static void Apply()
        {
            VerifyBaseline();
            EnsureFolder("Assets/Cutrium/Input");
            EnsureFolder("Assets/Cutrium/Scenes");

            InputActionAsset inputAsset = EnsureInputActions();
            Scene scene = EnsureScene(inputAsset);
            ValidateScene(scene, inputAsset);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Milestone 1B scene shell verified. Dedicated input actions, " +
                "VerticalSlice scene, serialized references, and Build Settings are valid.");
        }

        private static void VerifyBaseline()
        {
            if (!string.Equals(
                    Application.unityVersion,
                    "6000.3.21f1",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Milestone 1B requires Unity 6000.3.21f1, but " +
                    $"'{Application.unityVersion}' is running.");
            }

            VerifyPackageVersion("Packages/com.unity.inputsystem", "1.20.0");
            VerifyPackageVersion(
                "Packages/com.unity.render-pipelines.universal",
                "17.3.0");
        }

        private static void VerifyPackageVersion(string assetPath, string expectedVersion)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
            if (packageInfo == null
                || !string.Equals(
                    packageInfo.version,
                    expectedVersion,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Expected package at '{assetPath}' to resolve to " +
                    $"'{expectedVersion}', but found '{packageInfo?.version ?? "missing"}'.");
            }
        }

        private static InputActionAsset EnsureInputActions()
        {
            InputActionAsset existing =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (existing != null)
            {
                ValidateInputActions(existing);
                return existing;
            }

            if (File.Exists(GetPhysicalPath(InputAssetPath)))
            {
                AssetDatabase.ImportAsset(
                    InputAssetPath,
                    ImportAssetOptions.ForceSynchronousImport
                    | ImportAssetOptions.ForceUpdate);
                existing =
                    AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
                if (existing == null)
                {
                    throw new InvalidOperationException(
                        $"Unity could not import '{InputAssetPath}'.");
                }

                ValidateInputActions(existing);
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "CutriumInput";

            InputActionMap gameplay = asset.AddActionMap("Gameplay");
            gameplay
                .AddAction(
                    "Point",
                    InputActionType.PassThrough,
                    expectedControlLayout: "Vector2")
                .AddBinding("<Pointer>/position");
            gameplay
                .AddAction(
                    "Press",
                    InputActionType.Button,
                    expectedControlLayout: "Button")
                .AddBinding("<Pointer>/press");
            InputAction gameplayCancel = gameplay.AddAction(
                "Cancel",
                InputActionType.Button,
                expectedControlLayout: "Button");
            gameplayCancel.AddBinding("<Keyboard>/escape");
            gameplayCancel.AddBinding("<Mouse>/rightButton");

            InputActionMap ui = asset.AddActionMap("UI");
            ui.AddAction(
                    "Point",
                    InputActionType.PassThrough,
                    expectedControlLayout: "Vector2")
                .AddBinding("<Pointer>/position");
            ui.AddAction(
                    "LeftClick",
                    InputActionType.PassThrough,
                    expectedControlLayout: "Button")
                .AddBinding("<Pointer>/press");
            ui.AddAction(
                    "RightClick",
                    InputActionType.PassThrough,
                    expectedControlLayout: "Button")
                .AddBinding("<Mouse>/rightButton");
            ui.AddAction(
                    "MiddleClick",
                    InputActionType.PassThrough,
                    expectedControlLayout: "Button")
                .AddBinding("<Mouse>/middleButton");
            ui.AddAction(
                    "ScrollWheel",
                    InputActionType.PassThrough,
                    expectedControlLayout: "Vector2")
                .AddBinding("<Mouse>/scroll");

            InputAction navigate = ui.AddAction(
                "Navigate",
                InputActionType.PassThrough,
                expectedControlLayout: "Vector2");
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            navigate.AddBinding("<Gamepad>/leftStick");
            navigate.AddBinding("<Gamepad>/dpad");

            InputAction submit = ui.AddAction(
                "Submit",
                InputActionType.Button,
                expectedControlLayout: "Button");
            submit.AddBinding("<Keyboard>/enter");
            submit.AddBinding("<Gamepad>/buttonSouth");

            InputAction uiCancel = ui.AddAction(
                "Cancel",
                InputActionType.Button,
                expectedControlLayout: "Button");
            uiCancel.AddBinding("<Keyboard>/escape");
            uiCancel.AddBinding("<Gamepad>/buttonEast");

            ui.AddAction(
                    "TrackedDevicePosition",
                    InputActionType.PassThrough,
                    expectedControlLayout: "Vector3")
                .AddBinding("<TrackedDevice>/devicePosition");
            ui.AddAction(
                    "TrackedDeviceOrientation",
                    InputActionType.PassThrough,
                    expectedControlLayout: "Quaternion")
                .AddBinding("<TrackedDevice>/deviceRotation");

            string json = asset.ToJson();
            File.WriteAllText(
                GetPhysicalPath(InputAssetPath),
                json,
                new UTF8Encoding(false));
            UnityEngine.Object.DestroyImmediate(asset);

            AssetDatabase.ImportAsset(
                InputAssetPath,
                ImportAssetOptions.ForceSynchronousImport
                | ImportAssetOptions.ForceUpdate);

            InputActionAsset imported =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (imported == null)
            {
                throw new InvalidOperationException(
                    $"Unity could not import the generated '{InputAssetPath}'.");
            }

            ValidateInputActions(imported);
            return imported;
        }

        private static void ValidateInputActions(InputActionAsset asset)
        {
            RequireAction(
                asset,
                "Gameplay",
                "Point",
                InputActionType.PassThrough,
                "<Pointer>/position");
            RequireAction(
                asset,
                "Gameplay",
                "Press",
                InputActionType.Button,
                "<Pointer>/press");
            RequireAction(
                asset,
                "Gameplay",
                "Cancel",
                InputActionType.Button,
                "<Keyboard>/escape",
                "<Mouse>/rightButton");

            RequireAction(
                asset,
                "UI",
                "Point",
                InputActionType.PassThrough,
                "<Pointer>/position");
            RequireAction(
                asset,
                "UI",
                "LeftClick",
                InputActionType.PassThrough,
                "<Pointer>/press");
            RequireAction(asset, "UI", "Navigate", InputActionType.PassThrough);
            RequireAction(asset, "UI", "Submit", InputActionType.Button);
            RequireAction(asset, "UI", "Cancel", InputActionType.Button);
            RequireAction(asset, "UI", "RightClick", InputActionType.PassThrough);
            RequireAction(asset, "UI", "MiddleClick", InputActionType.PassThrough);
            RequireAction(asset, "UI", "ScrollWheel", InputActionType.PassThrough);
            RequireAction(
                asset,
                "UI",
                "TrackedDevicePosition",
                InputActionType.PassThrough);
            RequireAction(
                asset,
                "UI",
                "TrackedDeviceOrientation",
                InputActionType.PassThrough);
        }

        private static void RequireAction(
            InputActionAsset asset,
            string mapName,
            string actionName,
            InputActionType actionType,
            params string[] requiredBindings)
        {
            InputActionMap map = asset.FindActionMap(mapName);
            InputAction action = map?.FindAction(actionName);
            if (action == null || action.type != actionType)
            {
                throw new InvalidOperationException(
                    $"'{InputAssetPath}' is missing required {mapName}/{actionName} " +
                    $"with type {actionType}.");
            }

            string[] bindingPaths =
                action.bindings.Select(binding => binding.path).ToArray();
            foreach (string requiredBinding in requiredBindings)
            {
                if (!bindingPaths.Contains(requiredBinding))
                {
                    throw new InvalidOperationException(
                        $"{mapName}/{actionName} is missing binding " +
                        $"'{requiredBinding}'.");
                }
            }
        }

        private static Scene EnsureScene(InputActionAsset inputAsset)
        {
            SceneAsset existing =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(VerticalSliceScenePath);
            if (existing != null)
            {
                Scene existingScene = EditorSceneManager.OpenScene(
                    VerticalSliceScenePath,
                    OpenSceneMode.Single);
                bool needsSave = RepairExistingScene(existingScene, inputAsset);
                needsSave |= RepairBoardHierarchy(existingScene);
                if (needsSave)
                {
                    if (!EditorSceneManager.SaveScene(
                            existingScene,
                            VerticalSliceScenePath))
                    {
                        throw new InvalidOperationException(
                            $"Could not update '{VerticalSliceScenePath}'.");
                    }
                }

                ValidateScene(existingScene, inputAsset);
                return existingScene;
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            BuildSceneHierarchy(inputAsset);

            if (!EditorSceneManager.SaveScene(scene, VerticalSliceScenePath))
            {
                throw new InvalidOperationException(
                    $"Could not save '{VerticalSliceScenePath}'.");
            }

            AssetDatabase.ImportAsset(
                VerticalSliceScenePath,
                ImportAssetOptions.ForceSynchronousImport);
            return scene;
        }

        private static void BuildSceneHierarchy(InputActionAsset inputAsset)
        {
            var root = new GameObject("VerticalSliceRoot");
            root.SetActive(false);

            Camera boardCamera = CreateCamera(root.transform);
            CreateGlobalLight(root.transform);

            var compositionObject = new GameObject("SceneCompositionRoot");
            compositionObject.transform.SetParent(root.transform, false);
            SceneCompositionRoot compositionRoot =
                compositionObject.AddComponent<SceneCompositionRoot>();
            BoardCameraFitter boardCameraFitter =
                compositionObject.AddComponent<BoardCameraFitter>();
            ScreenToLogicalBoardMapper boardMapper =
                compositionObject.AddComponent<ScreenToLogicalBoardMapper>();
            EventSystemPointerUiBlocker uiBlocker =
                compositionObject.AddComponent<EventSystemPointerUiBlocker>();
            PointerInputAdapter pointerInput =
                compositionObject.AddComponent<PointerInputAdapter>();

            RectTransform canvasRect = CreateUiObject("Canvas", root.transform);
            Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            CanvasScaler canvasScaler =
                canvasRect.gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasScaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();

            RectTransform background =
                CreateUiObject("PresentationBackground", canvasRect);
            Stretch(background);
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = BackgroundColor;
            backgroundImage.raycastTarget = false;

            RectTransform safeAreaRoot =
                CreateUiObject("SafeAreaRoot", canvasRect);
            Stretch(safeAreaRoot);
            SafeAreaFitter safeAreaFitter =
                safeAreaRoot.gameObject.AddComponent<SafeAreaFitter>();
            safeAreaFitter.Configure(safeAreaRoot);
            VerticalLayoutGroup safeAreaLayout =
                safeAreaRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            safeAreaLayout.padding = new RectOffset(32, 32, 28, 28);
            safeAreaLayout.spacing = 22f;
            safeAreaLayout.childAlignment = TextAnchor.UpperCenter;
            safeAreaLayout.childControlWidth = true;
            safeAreaLayout.childControlHeight = true;
            safeAreaLayout.childForceExpandWidth = true;
            safeAreaLayout.childForceExpandHeight = false;

            CreateTopHud(safeAreaRoot);
            RectTransform boardStage = CreateBoardStage(
                safeAreaRoot,
                out RectTransform boardViewport,
                out RectTransform boardFrame);
            RectTransform bottomHud = CreateBottomHud(
                safeAreaRoot,
                out Text pointerStatus,
                out Text mappingStatus);

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(root.transform, false);
            EventSystem eventSystem =
                eventSystemObject.AddComponent<EventSystem>();
            InputSystemUIInputModule uiInputModule =
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
            ConfigureUiInputModule(uiInputModule, inputAsset);

            uiBlocker.Configure(eventSystem);
            boardCameraFitter.Configure(
                boardCamera,
                canvas,
                boardStage,
                boardViewport,
                boardFrame);
            boardMapper.Configure(boardCameraFitter);
            pointerInput.Configure(
                GetActionReference("Gameplay", "Point"),
                GetActionReference("Gameplay", "Press"),
                GetActionReference("Gameplay", "Cancel"),
                uiBlocker,
                boardMapper);
            compositionRoot.Configure(
                boardCamera,
                canvas,
                safeAreaFitter,
                boardCameraFitter,
                boardMapper,
                eventSystem,
                uiInputModule,
                uiBlocker,
                pointerInput);

            DebugPointerStatusView debugView =
                bottomHud.gameObject.AddComponent<DebugPointerStatusView>();
            debugView.Configure(pointerInput, pointerStatus, mappingStatus);

            root.SetActive(true);
            safeAreaFitter.Apply(
                new Rect(0f, 0f, 1080f, 1920f),
                new Vector2(1080f, 1920f));
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(safeAreaRoot);
            boardCameraFitter.RefreshNow();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static Camera CreateCamera(Transform parent)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(5f, 8f, -10f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            return camera;
        }

        private static void CreateGlobalLight(Transform parent)
        {
            var lightObject = new GameObject("Global Light 2D");
            lightObject.transform.SetParent(parent, false);
            Light2D light = lightObject.AddComponent<Light2D>();
            var serializedLight = new SerializedObject(light);
            serializedLight.FindProperty("m_LightType").intValue =
                (int)Light2D.LightType.Global;
            serializedLight.ApplyModifiedPropertiesWithoutUndo();
            light.intensity = 1f;
        }

        private static RectTransform CreateTopHud(RectTransform parent)
        {
            RectTransform topHud = CreateUiObject("TopHUD", parent);
            Image panel = topHud.gameObject.AddComponent<Image>();
            panel.color = HudColor;
            panel.raycastTarget = true;
            LayoutElement layout = topHud.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 132f;
            layout.minHeight = 116f;
            HorizontalLayoutGroup row =
                topHud.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(28, 24, 18, 18);
            row.spacing = 18f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;

            Text title = CreateText(
                "Title",
                topHud,
                "CUTRIUM  •  RESPONSIVE SHELL",
                34,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                PrimaryTextColor);
            LayoutElement titleLayout =
                title.gameObject.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;

            RectTransform buttonRect = CreateUiObject("HudBlockerButton", topHud);
            Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = AccentColor;
            buttonImage.raycastTarget = true;
            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            LayoutElement buttonLayout =
                buttonRect.gameObject.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 230f;
            buttonLayout.minWidth = 190f;
            buttonLayout.preferredHeight = 82f;
            CreateText(
                "Label",
                buttonRect,
                "HUD BLOCKER",
                25,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                BackgroundColor);

            return topHud;
        }

        private static RectTransform CreateBoardStage(
            RectTransform parent,
            out RectTransform boardViewport,
            out RectTransform boardFrame)
        {
            // BoardStage is the stable, VerticalLayoutGroup-controlled slot
            // that always receives the full available board area.
            // BoardViewport (inside it) is resized every frame by
            // BoardCameraFitter to exactly the 10:16 aspect-fitted rect, so
            // it never has a leftover letterbox margin of its own; BoardFrame
            // is a plain full-stretch child of BoardViewport.
            RectTransform boardStage = CreateUiObject("BoardStage", parent);
            LayoutElement stageLayout =
                boardStage.gameObject.AddComponent<LayoutElement>();
            stageLayout.minHeight = 240f;
            stageLayout.flexibleHeight = 1f;
            stageLayout.flexibleWidth = 1f;

            boardViewport = CreateUiObject("BoardViewport", boardStage);
            LayoutElement viewportLayout =
                boardViewport.gameObject.AddComponent<LayoutElement>();
            viewportLayout.ignoreLayout = true;
            Image viewportImage = boardViewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);
            viewportImage.raycastTarget = false;

            boardFrame = CreateUiObject("BoardFrame", boardViewport);
            boardFrame.anchorMin = Vector2.zero;
            boardFrame.anchorMax = Vector2.one;
            boardFrame.pivot = new Vector2(0.5f, 0.5f);
            boardFrame.offsetMin = Vector2.zero;
            boardFrame.offsetMax = Vector2.zero;
            Image boardImage = boardFrame.gameObject.AddComponent<Image>();
            boardImage.color = BoardColor;
            boardImage.raycastTarget = false;
            Outline outline = boardFrame.gameObject.AddComponent<Outline>();
            outline.effectColor = AccentColor;
            outline.effectDistance = new Vector2(4f, -4f);
            outline.useGraphicAlpha = false;

            Text boardLabel = CreateText(
                "BoardLabel",
                boardFrame,
                "10 × 16 LOGICAL BOARD\nSCENE SHELL • NO GAMEPLAY",
                30,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                PrimaryTextColor);
            Stretch(boardLabel.rectTransform, 36f);
            return boardStage;
        }

        private static RectTransform CreateBottomHud(
            RectTransform parent,
            out Text pointerStatus,
            out Text mappingStatus)
        {
            RectTransform bottomHud = CreateUiObject("BottomHUD", parent);
            Image panel = bottomHud.gameObject.AddComponent<Image>();
            panel.color = HudColor;
            panel.raycastTarget = true;
            LayoutElement layout =
                bottomHud.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 176f;
            layout.minHeight = 152f;
            VerticalLayoutGroup column =
                bottomHud.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(28, 28, 16, 16);
            column.spacing = 6f;
            column.childAlignment = TextAnchor.MiddleLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            pointerStatus = CreateText(
                "PointerStatus",
                bottomHud,
                "Pointer: waiting",
                24,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                PrimaryTextColor);
            pointerStatus.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;

            mappingStatus = CreateText(
                "MappingStatus",
                bottomHud,
                "Board: move or press to inspect 10 × 16 mapping",
                22,
                FontStyle.Normal,
                TextAnchor.MiddleLeft,
                SecondaryTextColor);
            mappingStatus.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;

            return bottomHud;
        }

        private static Text CreateText(
            string name,
            RectTransform parent,
            string content,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color)
        {
            RectTransform rectTransform = CreateUiObject(name, parent);
            Stretch(rectTransform);
            Text text = rectTransform.gameObject.AddComponent<Text>();
            text.font =
                LandmarkRevealPresentationSetup.LoadLegacyUiFontForSetup();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static RectTransform CreateUiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static void Stretch(RectTransform rectTransform, float inset = 0f)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(inset, inset);
            rectTransform.offsetMax = new Vector2(-inset, -inset);
        }

        private static void ConfigureUiInputModule(
            InputSystemUIInputModule module,
            InputActionAsset inputAsset)
        {
            module.UnassignActions();
            module.actionsAsset = inputAsset;
            module.point = GetActionReference("UI", "Point");
            module.leftClick = GetActionReference("UI", "LeftClick");
            module.rightClick = GetActionReference("UI", "RightClick");
            module.middleClick = GetActionReference("UI", "MiddleClick");
            module.scrollWheel = GetActionReference("UI", "ScrollWheel");
            module.move = GetActionReference("UI", "Navigate");
            module.submit = GetActionReference("UI", "Submit");
            module.cancel = GetActionReference("UI", "Cancel");
            module.trackedDevicePosition =
                GetActionReference("UI", "TrackedDevicePosition");
            module.trackedDeviceOrientation =
                GetActionReference("UI", "TrackedDeviceOrientation");

            var serializedModule = new SerializedObject(module);
            serializedModule.FindProperty("m_ActionsAsset").objectReferenceValue =
                inputAsset;
            serializedModule.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(module);
        }

        private static bool RepairExistingScene(
            Scene scene,
            InputActionAsset inputAsset)
        {
            InputSystemUIInputModule module = scene
                .GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<InputSystemUIInputModule>(true))
                .SingleOrDefault();
            if (module == null)
            {
                return false;
            }

            bool needsRepair =
                module.actionsAsset != inputAsset
                || module.point == null
                || module.leftClick == null
                || module.move == null
                || module.submit == null
                || module.cancel == null;
            if (!needsRepair)
            {
                return false;
            }

            ConfigureUiInputModule(module, inputAsset);
            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        /// Migrates the old flat "BoardViewport (large, flexible) containing
        /// a smaller aspect-fitted BoardFrame" shell into "BoardStage (the
        /// stable, flexible layout slot) containing BoardViewport (resized
        /// every frame to exactly the fitted rect) containing a plain
        /// full-stretch BoardFrame" -- so BoardViewport itself has no
        /// leftover letterbox area once BoardCameraFitter starts running.
        /// A no-op once the scene already has this structure.
        private static bool RepairBoardHierarchy(Scene scene)
        {
            GameObject root = scene.GetRootGameObjects()
                .SingleOrDefault(candidate => candidate.name == "VerticalSliceRoot");
            Transform safeArea = root == null
                ? null
                : root.transform.Find("Canvas/SafeAreaRoot");
            Transform existingBoardViewport = safeArea == null
                ? null
                : safeArea.Find("BoardViewport");
            if (existingBoardViewport == null)
            {
                return false;
            }

            int boardIndex = existingBoardViewport.GetSiblingIndex();
            var boardStageObject = new GameObject("BoardStage", typeof(RectTransform));
            var boardStage = (RectTransform)boardStageObject.transform;
            boardStage.SetParent(safeArea, false);
            boardStage.SetSiblingIndex(boardIndex);

            LayoutElement oldBoardLayout =
                existingBoardViewport.GetComponent<LayoutElement>();
            LayoutElement stageLayout =
                boardStageObject.AddComponent<LayoutElement>();
            stageLayout.minHeight =
                oldBoardLayout != null ? oldBoardLayout.minHeight : 240f;
            stageLayout.flexibleHeight = 1f;
            stageLayout.flexibleWidth = 1f;

            existingBoardViewport.SetParent(boardStage, false);
            var boardViewportRect = (RectTransform)existingBoardViewport;
            LayoutElement boardViewportLayout =
                existingBoardViewport.GetComponent<LayoutElement>();
            if (boardViewportLayout == null)
            {
                boardViewportLayout =
                    existingBoardViewport.gameObject.AddComponent<LayoutElement>();
            }

            boardViewportLayout.ignoreLayout = true;
            boardViewportRect.anchorMin = new Vector2(0.5f, 0.5f);
            boardViewportRect.anchorMax = new Vector2(0.5f, 0.5f);
            boardViewportRect.pivot = new Vector2(0.5f, 0.5f);
            // A temporary fallback size/position, immediately overwritten by
            // BoardCameraFitter's own next Apply() -- just avoids a
            // zero-size flash between this repair and that first refresh.
            boardViewportRect.sizeDelta = boardStage.rect.size;
            boardViewportRect.anchoredPosition = Vector2.zero;

            Transform boardFrame = existingBoardViewport.Find("BoardFrame");
            if (boardFrame != null)
            {
                var boardFrameRect = (RectTransform)boardFrame;
                boardFrameRect.anchorMin = Vector2.zero;
                boardFrameRect.anchorMax = Vector2.one;
                boardFrameRect.pivot = new Vector2(0.5f, 0.5f);
                boardFrameRect.offsetMin = Vector2.zero;
                boardFrameRect.offsetMax = Vector2.zero;
            }

            BoardCameraFitter fitter = root.GetComponentInChildren<BoardCameraFitter>(true);
            if (fitter != null)
            {
                fitter.Configure(
                    fitter.BoardCamera,
                    fitter.Canvas,
                    boardStage,
                    boardViewportRect,
                    fitter.BoardFrame);
                EditorUtility.SetDirty(fitter);
            }

            EditorUtility.SetDirty(boardStageObject);
            EditorUtility.SetDirty(existingBoardViewport.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        private static InputActionReference GetActionReference(
            string mapName,
            string actionName)
        {
            InputActionReference reference = AssetDatabase
                .LoadAllAssetsAtPath(InputAssetPath)
                .OfType<InputActionReference>()
                .FirstOrDefault(candidate =>
                    candidate.action != null
                    && candidate.action.actionMap != null
                    && candidate.action.actionMap.name == mapName
                    && candidate.action.name == actionName);

            if (reference == null)
            {
                throw new InvalidOperationException(
                    $"Could not load InputActionReference for " +
                    $"'{mapName}/{actionName}'.");
            }

            return reference;
        }

        private static void ValidateScene(Scene scene, InputActionAsset inputAsset)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    $"'{VerticalSliceScenePath}' is not a valid loaded scene.");
            }

            GameObject[] roots = scene.GetRootGameObjects();
            GameObject root = roots.SingleOrDefault(
                candidate => candidate.name == "VerticalSliceRoot");
            if (root == null || roots.Length != 1)
            {
                throw new InvalidOperationException(
                    "VerticalSlice must contain exactly one VerticalSliceRoot.");
            }

            Transform cameraTransform = RequireChild(root.transform, "Main Camera");
            Transform lightTransform = RequireChild(root.transform, "Global Light 2D");
            Transform compositionTransform =
                RequireChild(root.transform, "SceneCompositionRoot");
            Transform canvasTransform = RequireChild(root.transform, "Canvas");
            Transform eventSystemTransform =
                RequireChild(root.transform, "EventSystem");
            Transform safeAreaTransform =
                RequireChild(canvasTransform, "SafeAreaRoot");
            RequireChild(safeAreaTransform, "TopHUD");
            Transform boardStageTransform =
                RequireChild(safeAreaTransform, "BoardStage");
            Transform boardViewportTransform =
                RequireChild(boardStageTransform, "BoardViewport");
            RequireChild(boardViewportTransform, "BoardFrame");
            RequireChild(safeAreaTransform, "BottomHUD");

            if (cameraTransform.GetComponent<Camera>() == null
                || lightTransform.GetComponent<Light2D>() == null)
            {
                throw new InvalidOperationException(
                    "VerticalSlice camera or Global Light 2D is missing.");
            }

            SceneCompositionRoot compositionRoot =
                compositionTransform.GetComponent<SceneCompositionRoot>();
            if (compositionRoot == null
                || compositionRoot.BoardCamera == null
                || compositionRoot.Canvas == null
                || compositionRoot.SafeAreaFitter == null
                || compositionRoot.BoardCameraFitter == null
                || compositionRoot.BoardMapper == null
                || compositionRoot.EventSystem == null
                || compositionRoot.UiInputModule == null
                || compositionRoot.UiBlocker == null
                || compositionRoot.PointerInput == null
                || compositionRoot.BoardCameraFitter.BoardStage == null
                || compositionRoot.BoardCameraFitter.BoardViewport == null
                || compositionRoot.BoardCameraFitter.BoardFrame == null)
            {
                throw new InvalidOperationException(
                    "SceneCompositionRoot has missing serialized references.");
            }

            InputSystemUIInputModule module =
                eventSystemTransform.GetComponent<InputSystemUIInputModule>();
            if (module == null
                || module.actionsAsset != inputAsset
                || module.point == null
                || module.leftClick == null
                || module.move == null
                || module.submit == null
                || module.cancel == null)
            {
                throw new InvalidOperationException(
                    "InputSystemUIInputModule is not configured with Cutrium UI actions.");
            }

            PointerInputAdapter pointerInput = compositionRoot.PointerInput;
            if (pointerInput.PointAction == null
                || pointerInput.PressAction == null
                || pointerInput.CancelAction == null
                || pointerInput.UiBlockerComponent == null
                || pointerInput.BoardMapper == null)
            {
                throw new InvalidOperationException(
                    "PointerInputAdapter has missing serialized references.");
            }

            if (compositionRoot.BoardCameraFitter.LogicalBoardSize
                != new Vector2(10f, 16f))
            {
                throw new InvalidOperationException(
                    "The VerticalSlice logical board is not fixed at 10 by 16.");
            }

            if (safeAreaTransform.GetComponent<VerticalLayoutGroup>() == null
                || canvasTransform.GetComponent<CanvasScaler>() == null
                || canvasTransform.GetComponent<GraphicRaycaster>() == null)
            {
                throw new InvalidOperationException(
                    "VerticalSlice responsive Canvas layout is incomplete.");
            }
        }

        private static Transform RequireChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                throw new InvalidOperationException(
                    $"'{parent.name}' is missing required child '{childName}'.");
            }

            return child;
        }

        private static void ConfigureBuildSettings()
        {
            var desired = new[]
            {
                new EditorBuildSettingsScene(VerticalSliceScenePath, true),
                new EditorBuildSettingsScene(SampleScenePath, false)
            };

            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            bool alreadyConfigured =
                current.Length == desired.Length
                && current
                    .Zip(
                        desired,
                        (left, right) =>
                            left.path == right.path
                            && left.enabled == right.enabled)
                    .All(matches => matches);

            if (!alreadyConfigured)
            {
                EditorBuildSettings.scenes = desired;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            {
                throw new InvalidOperationException(
                    $"Cannot create asset folder '{path}'.");
            }

            EnsureFolder(parent);
            string guid = AssetDatabase.CreateFolder(parent, folderName);
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException(
                    $"Unity could not create asset folder '{path}'.");
            }
        }

        private static string GetPhysicalPath(string assetPath)
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException(
                    "Could not resolve the Unity project root.");
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }
    }
}
