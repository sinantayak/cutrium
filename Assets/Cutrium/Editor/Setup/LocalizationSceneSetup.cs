using System;
using System.Collections.Generic;
using Cutrium.Presentation.Localization;
using Cutrium.Presentation.Landmark;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cutrium.Editor.Setup
{
    public static class LocalizationSceneSetup
    {
        public const string TablePath =
            "Assets/Cutrium/Content/Localization/MainLocalizationTable.asset";
        private const string ScenePath =
            "Assets/Cutrium/Scenes/VerticalSlice.unity";
        private const string TmpFontPath =
            "Assets/Cutrium/Art/Fonts/LapsusPro-Bold SDF.asset";
        private const string SourceFontPath =
            "Assets/Cutrium/Art/Fonts/LapsusPro-Bold.otf";
        private const string RequiredTurkishGlyphs =
            "ÇçĞğİıÖöŞşÜü";

        [MenuItem("Cutrium/Setup/Apply EN-TR Localization")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Exit Play Mode before applying localization.");
            }

            Scene scene = OpenVerticalSliceScene();
            GameObject root = RequireRoot(scene, "VerticalSliceRoot");
            LocalizationSetupResult result = ApplyToScene(root);
            Validate(root, result);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "Unity could not save the localized scene.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"EN/TR localization ready with " +
                $"{result.Presenter.LabelCount} serialized UI labels.");
        }

        public static LocalizationSetupResult ApplyToScene(GameObject root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            MainEarthLandmarkContent.CreateOrUpdateAssets();
            EnsureTurkishFontGlyphs();
            LocalizationTable table = GetOrCreateTable();
            table.ConfigureForSetup(BuildEntries());
            EditorUtility.SetDirty(table);

            Transform servicesTransform = root.transform.Find(
                "LocalizationServices");
            GameObject services;
            if (servicesTransform != null)
            {
                services = servicesTransform.gameObject;
            }
            else
            {
                services = new GameObject("LocalizationServices");
                Undo.RegisterCreatedObjectUndo(
                    services,
                    "Create Localization Services");
                services.transform.SetParent(root.transform, false);
            }

            LocalizationService service = GetOrAddComponent<
                LocalizationService>(services);
            service.ConfigureForSetup(
                table,
                true,
                SupportedLanguage.English);

            LocalizationPresenter presenter = GetOrAddComponent<
                LocalizationPresenter>(services);
            LandmarkRevealPresenter landmarkPresenter = root
                .GetComponentInChildren<LandmarkRevealPresenter>(true);
            if (landmarkPresenter == null)
            {
                throw new InvalidOperationException(
                    "Localization setup requires the landmark reveal " +
                    "presenter.");
            }

            landmarkPresenter.ConfigureLocalizationForSetup(service);
            Text[] legacyLabels = root.GetComponentsInChildren<Text>(true);
            TMP_Text[] tmpLabels = root.GetComponentsInChildren<TMP_Text>(true);
            presenter.ConfigureForSetup(service, legacyLabels, tmpLabels);

            EditorUtility.SetDirty(service);
            EditorUtility.SetDirty(presenter);
            EditorUtility.SetDirty(landmarkPresenter);
            return new LocalizationSetupResult(service, presenter);
        }

        public static void Validate(
            GameObject root,
            LocalizationSetupResult result)
        {
            int expectedLabelCount =
                root.GetComponentsInChildren<Text>(true).Length
                + root.GetComponentsInChildren<TMP_Text>(true).Length;
            LandmarkRevealPresenter landmarkPresenter = root
                .GetComponentInChildren<LandmarkRevealPresenter>(true);
            if (result.Service == null
                || result.Service.Table == null
                || result.Service.Table.Entries.Count == 0
                || result.Service.CurrentLanguage
                    != SupportedLanguage.English
                || result.Presenter == null
                || result.Presenter.Service != result.Service
                || result.Presenter.LabelCount != expectedLabelCount
                || landmarkPresenter == null
                || landmarkPresenter.Localization != result.Service)
            {
                throw new InvalidOperationException(
                    "Localization service, table, or serialized label " +
                    "bindings are incomplete.");
            }
        }

        private static LocalizationTable GetOrCreateTable()
        {
            EnsureFolder("Assets/Cutrium/Content/Localization");
            LocalizationTable table =
                AssetDatabase.LoadAssetAtPath<LocalizationTable>(TablePath);
            if (table != null)
            {
                return table;
            }

            table = ScriptableObject.CreateInstance<LocalizationTable>();
            AssetDatabase.CreateAsset(table, TablePath);
            return table;
        }

        private static void EnsureTurkishFontGlyphs()
        {
            TMP_FontAsset fontAsset =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(
                SourceFontPath);
            if (fontAsset == null || sourceFont == null)
            {
                throw new InvalidOperationException(
                    "Localization needs the configured LapsusPro TMP and " +
                    "source font assets.");
            }

            var serializedFont = new SerializedObject(fontAsset);
            SerializedProperty sourceProperty = serializedFont.FindProperty(
                "m_SourceFontFile");
            SerializedProperty populationProperty =
                serializedFont.FindProperty("m_AtlasPopulationMode");
            SerializedProperty multiAtlasProperty =
                serializedFont.FindProperty(
                    "m_IsMultiAtlasTexturesEnabled");
            if (sourceProperty == null
                || populationProperty == null
                || multiAtlasProperty == null)
            {
                throw new InvalidOperationException(
                    "The installed TextMesh Pro font serialization layout " +
                    "does not expose the required localization settings.");
            }

            sourceProperty.objectReferenceValue = sourceFont;
            populationProperty.enumValueIndex =
                (int)AtlasPopulationMode.Dynamic;
            multiAtlasProperty.boolValue = true;
            serializedFont.ApplyModifiedPropertiesWithoutUndo();
            fontAsset.ReadFontAssetDefinition();
            if (!fontAsset.HasCharacters(RequiredTurkishGlyphs)
                && !fontAsset.TryAddCharacters(
                    RequiredTurkishGlyphs,
                    out string missingCharacters))
            {
                throw new InvalidOperationException(
                    "LapsusPro could not provide required Turkish glyphs: " +
                    missingCharacters);
            }

            EditorUtility.SetDirty(fontAsset);
        }

        private static LocalizationEntry[] BuildEntries()
        {
            var entries = new List<LocalizationEntry>
            {
                Entry("SHOP", "MAĞAZA"),
                Entry("HOME", "ANA SAYFA"),
                Entry("CHALLENGE", "BÖLÜMLER"),
                Entry("COMING SOON", "YAKINDA"),
                Entry("PLAY", "OYNA"),
                Entry("English", "Türkçe"),
                Entry("Home", "Ana Sayfa"),
                Entry("Exit", "Çıkış"),
                Entry("NEXT", "SONRAKİ"),
                Entry("RETRY", "TEKRAR DENE"),
                Entry("Retry", "Tekrar Dene"),
                Entry("RESTART SEQUENCE", "BAŞTAN BAŞLA"),
                Entry("Watch AD", "REKLAM İZLE"),
                Entry(
                    "Watch an AD\nto Continue!",
                    "Devam Etmek İçin\nReklam İzle!"),
                Entry("TARGET", "HEDEF"),
                Entry("LEVEL COMPLETE", "SEVİYE TAMAMLANDI"),
                Entry("Description", "Açıklama"),
                Entry("Sector", "Bölge"),
                Entry("Landmark", "Simgesel Yapı"),
                Entry("LOCKED", "SABİTLENDİ"),
                Entry("TRY AGAIN", "TEKRAR DENE"),
                Entry("BIG CUT", "BÜYÜK KESİM"),
                Entry("CLOSE!", "ÇOK YAKIN!"),
                Entry("ON", "AÇIK"),
                Entry("OFF", "KAPALI"),
                Entry("UI TEST", "ARAYÜZ TESTİ"),
                Entry("Pointer: waiting", "İşaretçi: bekliyor"),
                Entry(
                    "Board: move or press to inspect 10 × 16 mapping",
                    "Tahta: 10 × 16 eşlemesini incelemek için hareket " +
                    "ettir veya bas"),

                Entry("LEARN THE CUT", "KESİMİ ÖĞREN"),
                Entry("WATCH THE THREAT", "TEHDİDİ İZLE"),
                Entry("KEEP THEM TOGETHER", "ONLARI BİRLİKTE TUT"),
                Entry("CUT WITH CONFIDENCE", "KARARLI KES"),
                Entry("MEET THE HUNTER", "AVCIYLA TANIŞ"),
                Entry("READ THE PULSE", "NABZI OKU"),
                Entry(
                    "CREATE A FREEZE WINDOW",
                    "DONDURMA FIRSATI YARAT"),
                Entry("FINISH IT INSTANTLY", "ANINDA BİTİR"),
                Entry(
                    "TRACK TWO INTENTIONS",
                    "İKİ NİYETİ TAKİP ET"),
                Entry("FIND THE SHARED WINDOW", "ORTAK FIRSATI BUL"),
                Entry("CHOOSE THE RIGHT POWER", "DOĞRU GÜCÜ SEÇ"),
                Entry("MASTER THE BOARD", "TAHTADA USTALAŞ"),
                Entry("READ THE ROOM", "ALANI OKU"),
                Entry("MOVE WITH THEM", "ONLARLA HAREKET ET"),
                Entry("WAIT FOR THE CROSS", "KESİŞİMİ BEKLE"),
                Entry(
                    "MEET THE COMET",
                    "KUYRUKLU YILDIZLA TANIŞ"),
                Entry("GIVE IT SPACE", "ONA ALAN BIRAK"),
                Entry("READ TWO SPEEDS", "İKİ HIZI OKU"),
                Entry("BALANCE THE PAIR", "İKİLİYİ DENGELE"),
                Entry("BEND THEIR PATH", "YOLLARINI BÜK"),
                Entry("SHAPE THE GROUP", "GRUBU ŞEKİLLENDİR"),
                Entry("CONTROL THE BURST", "PATLAMAYI KONTROL ET"),
                Entry("GATHER THE CROWD", "KALABALIĞI TOPLA"),
                Entry("MASTER THE MOTION", "HAREKETTE USTALAŞ"),

                Entry("MAKE THEM COUNT", "İYİ DEĞERLENDİR"),
                Entry("HUNTER", "AVCI"),
                Entry(
                    "REACTS TO YOUR CUTS",
                    "KESİMLERİNE TEPKİ VERİR"),
                Entry("PULSE", "NABIZ"),
                Entry("WATCH ITS SPEED", "HIZINI İZLE"),
                Entry("FREEZE", "DONDUR"),
                Entry(
                    "CREATE A SAFE WINDOW",
                    "GÜVENLİ BİR FIRSAT YARAT"),
                Entry("INSTANT", "ANINDA"),
                Entry(
                    "SAVE IT FOR A RISKY CUT",
                    "RİSKLİ BİR KESİM İÇİN SAKLA"),
                Entry("COMET", "KUYRUKLU YILDIZ"),
                Entry("SMALL AND FAST", "KÜÇÜK VE HIZLI"),
                Entry("HEAVY", "AĞIR"),
                Entry("SLOW BUT LARGE", "YAVAŞ AMA BÜYÜK"),
                Entry("GRAVITY WELL", "ÇEKİM KUYUSU"),
                Entry(
                    "TAP A POINT TO PULL",
                    "ÇEKMEK İÇİN BİR NOKTAYA DOKUN"),
                Entry("CHAPTER MASTERY", "BÖLÜM USTALIĞI"),
                Entry("SHAPE THEN CUT", "ŞEKİLLENDİR, SONRA KES"),

                Entry("Coastal Lagoon", "Kıyı Lagünü"),
                Entry(
                    "Warm turquoise water meets soft white sand beneath " +
                    "an endless open sky.",
                    "Ilık turkuaz su, uçsuz bucaksız gökyüzünün altında " +
                    "yumuşak beyaz kumla buluşur."),
                Entry("Oceania", "Okyanusya"),
                Entry("Desert Dunes", "Çöl Kumulları"),
                Entry(
                    "Rolling amber dunes catch the last light of dusk " +
                    "across a silent horizon.",
                    "Dalgalanan kehribar kumullar sessiz ufukta günün " +
                    "son ışıklarını yakalar."),
                Entry("Middle East", "Orta Doğu"),
                Entry("Galata Tower", "Galata Kulesi"),
                Entry(
                    "Galata Tower is one of Istanbul's best-known symbols " +
                    "and a defining part of the city's centuries-old " +
                    "silhouette. The foundations of today's tower were " +
                    "laid during the Genoese colony period in Galata in " +
                    "the 14th century, and the structure served different " +
                    "purposes over time. Standing about 67 metres tall, its " +
                    "location offers sweeping views of the Bosphorus, the " +
                    "Golden Horn, and the historic peninsula. Today, " +
                    "Galata Tower remains one of Istanbul's strongest " +
                    "historical reminders of the Byzantine, Genoese, and " +
                    "Ottoman eras.",
                    "Galata Kulesi, İstanbul’un en tanınan simgelerinden " +
                    "biri ve şehrin yüzyıllara yayılan silüetinin önemli " +
                    "parçalarındandır. Günümüzdeki kulenin temelleri, 14. " +
                    "yüzyılda Galata’daki Ceneviz kolonisi döneminde atılmış " +
                    "ve yapı zaman içinde farklı amaçlarla kullanılmıştır. " +
                    "Yaklaşık 67 metre yüksekliğindeki kule, bulunduğu konum " +
                    "sayesinde İstanbul Boğazı, Haliç ve tarihî yarımadaya " +
                    "hâkim geniş bir manzara sunar. Galata Kulesi bugün " +
                    "İstanbul’un Bizans, Ceneviz ve Osmanlı dönemlerini bir " +
                    "arada hatırlatan en güçlü tarihî yapılardan biridir."),
                Entry("Istanbul / TURKEY", "İstanbul / TÜRKİYE"),
            };

            return entries.ToArray();
        }

        private static LocalizationEntry Entry(
            string english,
            string turkish) => new LocalizationEntry(english, turkish);

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = path.Substring(0, path.LastIndexOf('/'));
            string name = path.Substring(path.LastIndexOf('/') + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null
                ? component
                : Undo.AddComponent<T>(gameObject);
        }

        private static Scene OpenVerticalSliceScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path == ScenePath)
            {
                return scene;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                throw new OperationCanceledException(
                    "Localization setup cancelled before opening the scene.");
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            throw new InvalidOperationException(
                $"Scene does not contain required root '{name}'.");
        }

        public readonly struct LocalizationSetupResult
        {
            public LocalizationSetupResult(
                LocalizationService service,
                LocalizationPresenter presenter)
            {
                Service = service;
                Presenter = presenter;
            }

            public LocalizationService Service { get; }
            public LocalizationPresenter Presenter { get; }
        }
    }
}
