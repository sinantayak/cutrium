using System;
using Cutrium.Presentation.Landmark;
using UnityEditor;
using UnityEngine;

namespace Cutrium.Editor.Setup
{
    /// Editor content source copied from repository-root landmarks.md. The
    /// gameplay catalog never references this type or its generated assets.
    public static class FirstTwelveLandmarkContent
    {
        public const string ContentFolder =
            "Assets/Cutrium/Content/Landmarks";
        public const string FirstTwelveFolder = ContentFolder + "/First12";

        public static readonly Entry[] Entries =
        {
            new Entry(
                "galata-kulesi", "Galata Kulesi", "İstanbul",
                "Galata Kulesi, İstanbul’un en tanınan simgelerinden biri ve şehrin yüzyıllara yayılan silüetinin önemli parçalarındandır. Günümüzdeki kulenin temelleri, 14. yüzyılda Galata’daki Ceneviz kolonisi döneminde atılmış ve yapı zaman içinde farklı amaçlarla kullanılmıştır. Yaklaşık 67 metre yüksekliğindeki kule, bulunduğu konum sayesinde İstanbul Boğazı, Haliç ve tarihî yarımadaya hâkim geniş bir manzara sunar. Galata Kulesi bugün İstanbul’un Bizans, Ceneviz ve Osmanlı dönemlerini bir arada hatırlatan en güçlü tarihî yapılardan biridir.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Istanbul-Galata_Kulesi.png",
                ContentFolder + "/GalataKulesi.asset"),
            new Entry(
                "misis-antik-kenti", "Misis Antik Kenti", "Adana",
                "Misis, Çukurova’nın doğusunda binlerce yıllık yerleşim izleri taşıyan ve tarihî ticaret yolları üzerinde gelişen önemli bir antik merkezdir. Roma döneminden kalma köprüsü ve mozaikleri, kentin farklı çağlarda taşıdığı önemi gösterir. Bölgedeki kazılar, Misis’in Anadolu ile Mezopotamya arasındaki geçiş yollarında uzun süre yaşayan bir kent olduğunu ortaya koyuyor.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Adana-Misis_Antik_Kenti.png",
                FirstTwelveFolder + "/L02_MisisAntikKenti.asset"),
            new Entry(
                "yilankale", "Yılankale", "Adana",
                "Ceyhan Ovası’na hâkim sarp bir kayalık üzerine kurulan Yılankale, Orta Çağ Çukurova’sının en etkileyici savunma yapılarından biridir. Kale, özellikle Kilikya Ermeni Krallığı dönemine ait mimarisi ve vadiden görülen güçlü silüetiyle dikkat çeker. Türk halk anlatılarında Şahmeran efsanesiyle de ilişkilendirilmesi, yapıya tarih kadar güçlü bir mitolojik kimlik kazandırır.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Adana-Yılankale.png",
                FirstTwelveFolder + "/L03_Yilankale.asset"),
            new Entry(
                "aspendos-antik-tiyatrosu", "Aspendos Antik Tiyatrosu", "Antalya",
                "MS 2. yüzyılda inşa edilen Aspendos Tiyatrosu, Roma dünyasından günümüze ulaşan en iyi korunmuş tiyatrolardan biri kabul edilir. Yaklaşık 15 bin kişilik kapasitesi, anıtsal sahne binası ve güçlü akustiği Roma mühendisliğinin ulaştığı seviyeyi gösterir. Yapı bugün bile konser ve gösterilere ev sahipliği yapabilecek kadar etkileyici bir bütünlüğe sahiptir.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Antalya-Aspendos_Antik_Tiyatrosu.png",
                FirstTwelveFolder + "/L04_AspendosAntikTiyatrosu.asset"),
            new Entry(
                "myra-antik-kenti", "Myra Antik Kenti", "Antalya",
                "Myra, Likya Birliği’nin en güçlü kentlerinden biriydi ve özellikle kayalara oyulmuş görkemli mezarlarıyla tanınır. Roma döneminden kalan büyük tiyatro, kentin antik çağdaki zenginliğini bugün de hissettirir. Yakındaki Demre, Aziz Nikolaos geleneğiyle de dünya çapında bilinen önemli bir tarih ve inanç merkezidir.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Antalya-Myra_Antik_Kenti.png",
                FirstTwelveFolder + "/L05_MyraAntikKenti.asset"),
            new Entry(
                "patara-antik-kenti", "Patara Antik Kenti", "Antalya",
                "Patara, Likya’nın en önemli liman ve yönetim merkezlerinden biriydi. Kentteki meclis yapısı, Likya Birliği’nin gelişmiş temsil sisteminin en güçlü mimari izlerinden biridir. Antik kalıntıların hemen yanında uzanan geniş Patara sahili, burayı tarih ile doğal peyzajın aynı karede buluştuğu özel yerlerden biri yapar.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Antalya-Patara_Antik_Kenti.png",
                FirstTwelveFolder + "/L06_PataraAntikKenti.asset"),
            new Entry(
                "xanthos-antik-kenti", "Xanthos Antik Kenti", "Antalya",
                "Xanthos, antik Likya’nın başkentlerinden ve en güçlü siyasi merkezlerinden biriydi. Kaya mezarları, anıtları ve özgün Likya mezar mimarisi, bölgenin kendine özgü kültürünü yansıtır. Xanthos, kutsal alan Letoon ile birlikte UNESCO Dünya Mirası Listesi’nde yer alır.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Antalya-Xanthos_Antik_Kenti.png",
                FirstTwelveFolder + "/L07_XanthosAntikKenti.asset"),
            new Entry(
                "sagalassos-antik-kenti", "Sagalassos Antik Kenti", "Burdur",
                "Batı Toroslar’ın yükseklerinde kurulan Sagalassos, Pisidia bölgesinin en zengin antik kentlerinden biriydi. Restore edilen Antoninler Çeşmesi’nden bugün bile su akması, kenti benzersiz bir deneyime dönüştürür. Tiyatro, agoralar ve anıtsal yapılar, özellikle Roma dönemindeki refahını güçlü biçimde ortaya koyar.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Burdur-Sagalassos_Antik_Kenti.png",
                FirstTwelveFolder + "/L08_SagalassosAntikKenti.asset"),
            new Entry(
                "oludeniz", "Ölüdeniz", "Fethiye",
                "Ölüdeniz, turkuaz lagünü ve Kumburnu’nun oluşturduğu benzersiz kıyı şekliyle Türkiye’nin en tanınan doğal manzaralarından biridir. Sakin lagün ile açık denizin yan yana oluşturduğu renk geçişi, bölgenin ikonik görüntüsünü yaratır. Babadağ’dan yapılan yamaç paraşütleri de bu manzarayı dünyaca ünlü hâle getirmiştir.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Mugla-Oludeniz.png",
                FirstTwelveFolder + "/L09_Oludeniz.asset"),
            new Entry(
                "truva-antik-kenti", "Truva Antik Kenti", "Çanakkale",
                "Truva, yaklaşık 4 bin yıllık yerleşim katmanlarıyla Anadolu ve Akdeniz uygarlıkları arasındaki erken temasları gösteren en önemli arkeolojik alanlardan biridir. Homeros’un İlyada destanındaki Troya Savaşı anlatısı, kenti dünya kültürünün en güçlü efsanelerinden biri hâline getirmiştir. UNESCO Dünya Mirası olan alandaki üst üste kurulmuş kent katmanları, binlerce yıllık değişimi aynı yerde görmeyi mümkün kılar.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Canakkale-Truva_Antik_Kenti.png",
                FirstTwelveFolder + "/L10_TruvaAntikKenti.asset"),
            new Entry(
                "zeugma-antik-kenti", "Zeugma Antik Kenti", "Gaziantep",
                "Fırat kıyısındaki Zeugma, Helenistik ve Roma dönemlerinde doğu ile batı arasındaki ticaret yollarını kontrol eden zengin bir kentti. Kent özellikle villalarını süsleyen olağanüstü taban mozaikleriyle tanınır. Zeugma’dan çıkarılan eserlerin büyük bölümü, bugün dünyanın en önemli mozaik koleksiyonlarından birini barındıran Zeugma Mozaik Müzesi’nde sergilenir.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Gaziantep-Zeugma_Antik_Kenti.png",
                FirstTwelveFolder + "/L11_ZeugmaAntikKenti.asset"),
            new Entry(
                "topkapi-sarayi", "Topkapı Sarayı", "İstanbul",
                "Fatih Sultan Mehmet döneminde yapımına başlanan Topkapı Sarayı, yaklaşık dört yüzyıl boyunca Osmanlı yönetiminin ve saray yaşamının merkezi oldu. Boğaz ve Haliç’e hâkim konumu, saraya İstanbul’un en güçlü panoramalarından birini kazandırır. Avluları, Harem’i, köşkleri ve imparatorluk koleksiyonları Osmanlı saray dünyasının farklı katmanlarını bir arada gösterir.",
                "Assets/Cutrium/Content/Landmarks/Artwork/Istanbul-Topkapi_Sarayi.png",
                FirstTwelveFolder + "/L12_TopkapiSarayi.asset"),
        };

        public static LandmarkDefinition[] CreateOrUpdateAssets()
        {
            EnsureFolder(FirstTwelveFolder);
            var definitions = new LandmarkDefinition[Entries.Length];
            for (int index = 0; index < Entries.Length; index++)
            {
                Entry entry = Entries[index];
                Sprite artwork = AssetDatabase.LoadAssetAtPath<Sprite>(
                    entry.ArtworkPath);
                if (artwork == null)
                {
                    throw new InvalidOperationException(
                        $"Missing landmark artwork: {entry.ArtworkPath}");
                }

                LandmarkDefinition definition =
                    AssetDatabase.LoadAssetAtPath<LandmarkDefinition>(
                        entry.DefinitionPath);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<LandmarkDefinition>();
                    AssetDatabase.CreateAsset(definition, entry.DefinitionPath);
                }

                definition.ConfigureForSetup(
                    entry.Id,
                    entry.Title,
                    entry.Description,
                    $"{entry.City} / TÜRKİYE",
                    artwork);
                EditorUtility.SetDirty(definition);
                definitions[index] = definition;
            }

            return definitions;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)
                ?.Replace('\\', '/');
            if (!string.IsNullOrWhiteSpace(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(
                parent,
                System.IO.Path.GetFileName(path));
        }

        public readonly struct Entry
        {
            public Entry(
                string id,
                string title,
                string city,
                string description,
                string artworkPath,
                string definitionPath)
            {
                Id = id;
                Title = title;
                City = city;
                Description = description;
                ArtworkPath = artworkPath;
                DefinitionPath = definitionPath;
            }

            public string Id { get; }
            public string Title { get; }
            public string City { get; }
            public string Description { get; }
            public string ArtworkPath { get; }
            public string DefinitionPath { get; }
        }
    }
}
