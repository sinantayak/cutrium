using System;
using Cutrium.Presentation.Landmark;
using UnityEditor;
using UnityEngine;

namespace Cutrium.Editor.Setup
{
    /// Chapter 1 Earth content copied from repository-root
    /// earth-landmarks.md. The gameplay catalog never references this type or
    /// its generated assets.
    public static class FirstTwelveLandmarkContent
    {
        public const string ContentFolder =
            "Assets/Cutrium/Content/Landmarks";
        public const string EarthArtworkFolder =
            "Assets/Cutrium/Content/Earth Landmarks";
        public const string FirstTwelveFolder =
            ContentFolder + "/Earth/Chapter01";

        public static readonly Entry[] Entries =
        {
            new Entry(
                "angkor-wat", "Angkor Wat", "Siem Reap / KAMBOÇYA",
                "12. yüzyılda dev bir Hindu tapınağı olarak kurulan Angkor Wat, zamanla Budist bir kutsal alana dönüştü. Beş kuleli silüeti ve bir kilometreyi aşan kabartmalarıyla dünyanın en büyük dinî anıt komplekslerinden biridir. Batıya dönük sıra dışı planı ve taşlara işlenen savaş, saray yaşamı ve destan sahneleri, Khmer İmparatorluğu’nun gücünü ayrıntılarıyla anlatır.",
                EarthArtworkFolder + "/Angkor Wat — Kamboçya  Siem Reap.png",
                FirstTwelveFolder + "/L01_AngkorWat.asset"),
            new Entry(
                "aspendos-tiyatrosu", "Aspendos Antik Tiyatrosu",
                "Antalya / TÜRKİYE",
                "MS 2. yüzyılda inşa edilen Aspendos, Roma dünyasından günümüze ulaşan en iyi korunmuş tiyatrolardan biridir. Anıtsal sahne binası ve güçlü akustiği, Roma mühendisliğinin etkileyici bir göstergesidir. Yaklaşık 15 bin kişilik seyirci düzeni sesi uzak sıralara taşırken, yapının bugün de gösteriler için kullanılabilmesi olağanüstü bütünlüğünü kanıtlar.",
                EarthArtworkFolder + "/Aspendos Antik Tiyatrosu - Antalya.png",
                FirstTwelveFolder + "/L02_AspendosTiyatrosu.asset"),
            new Entry(
                "aziz-vasil-katedrali", "Aziz Vasil Katedrali",
                "Moskova / RUSYA",
                "16. yüzyılda Kızıl Meydan’da inşa edilen katedral, birbirinden farklı renk ve desenlere sahip soğan kubbeleriyle tanınır. Yapı aslında tek bir salon değil, birbirine bağlanan dokuz şapelden oluşur. Korkunç İvan’ın Kazan zaferini anmak için yaptırdığı yapının bugün ikonik olan canlı renkleri, ilk inşasından sonraki yüzyıllarda geliştirilmiştir.",
                EarthArtworkFolder + "/Aziz Basil Katedrali — Rusya  Moskova.png",
                FirstTwelveFolder + "/L03_AzizVasilKatedrali.asset"),
            new Entry(
                "big-ben-westminster", "Big Ben ve Westminster Sarayı",
                "Londra / BİRLEŞİK KRALLIK",
                "“Big Ben” aslında saat kulesinin değil, Elizabeth Kulesi içindeki Büyük Çan’ın takma adıdır. Yanındaki Westminster Sarayı, Birleşik Krallık Parlamentosuna ev sahipliği yapar. Eski sarayın büyük bölümünü yok eden 1834 yangınından sonra Gotik Canlanma üslubunda yeniden kurulan kompleksin ünlü saati 1859’dan beri Londra’nın ritmini belirler.",
                EarthArtworkFolder + "/Big Ben ve Westminster Sarayı — Birleşik Krallık  Londra.png",
                FirstTwelveFolder + "/L04_BigBenWestminster.asset"),
            new Entry(
                "borobudur-tapinagi", "Borobudur Tapınağı",
                "Cava / ENDONEZYA",
                "9. yüzyılda inşa edilen Borobudur, basamaklı terasları dev bir Budist mandalası gibi yükselen anıtsal bir tapınaktır. Yüzlerce Buda heykeli ve anlatı kabartması, ziyaretçiyi sembolik bir aydınlanma yolculuğuna çıkarır. Zirveye yakın üç dairesel terasta, içlerinde Buda heykelleri bulunan 72 delikli stupa ana kubbeyi çevreler.",
                EarthArtworkFolder + "/Borobudur Tapınağı — Endonezya  Cava.png",
                FirstTwelveFolder + "/L05_BorobudurTapinagi.asset"),
            new Entry(
                "brandenburg-kapisi", "Brandenburg Kapısı",
                "Berlin / ALMANYA",
                "18. yüzyılın sonunda yapılan Brandenburg Kapısı, Berlin’in en güçlü simgelerinden biridir. Soğuk Savaş yıllarında bölünmenin sınırında kaldı; 1989’dan sonra Almanya’nın yeniden birleşmesini temsil etmeye başladı. Kapının tepesindeki dört atlı Quadriga heykeli, zafer tanrıçasını şehre doğru ilerlerken gösterir.",
                EarthArtworkFolder + "/Brandenburg Kapısı — Almanya  Berlin.png",
                FirstTwelveFolder + "/L06_BrandenburgKapisi.asset"),
            new Entry(
                "burj-al-arab", "Burç el-Arab",
                "Dubai / BİRLEŞİK ARAP EMİRLİKLERİ",
                "Yelken biçimli Burj Al Arab, Dubai kıyısındaki yapay bir ada üzerinde yükselir. Cesur silüeti ve gösterişli iç mekânları, yapıyı modern Dubai’nin en tanınan simgelerinden biri yapmıştır. Yaklaşık 180 metre yüksekliğindeki dev atriyumu ve denizin üzerinde uzanan helikopter pisti, otelin mimarisine beklenmedik bir ölçek duygusu katar.",
                EarthArtworkFolder + "/Burç el-Arap (Burj Al Arab) — BAE  Dubai.png",
                FirstTwelveFolder + "/L07_BurjAlArab.asset"),
            new Entry(
                "burj-khalifa", "Burj Khalifa",
                "Dubai / BİRLEŞİK ARAP EMİRLİKLERİ",
                "828 metre yüksekliğindeki Burj Khalifa, dünyanın en yüksek binasıdır. Çöl çiçeğinden esinlenen üç kollu planı, dev yapının rüzgâra karşı dengeli kalmasına yardımcı olur. Otel, konut, ofis ve seyir teraslarını aynı çatı altında bir araya getiren 160’tan fazla katıyla adeta dikey bir şehir gibi çalışır.",
                EarthArtworkFolder + "/Burj Khalifa — BAE  Dubai.png",
                FirstTwelveFolder + "/L08_BurjKhalifa.asset"),
            new Entry(
                "buyuk-kanyon", "Büyük Kanyon",
                "Arizona / ABD",
                "Colorado Nehri’nin milyonlarca yılda aşındırdığı Büyük Kanyon, Dünya’nın jeolojik geçmişini renkli kaya katmanları hâlinde sergiler. En eski katmanlarından bazıları yaklaşık iki milyar yıllıktır. Yaklaşık 446 kilometre boyunca uzanan kanyon, bazı noktalarda 1,6 kilometreden daha derine inerek boyutlarını tek bakışta kavramayı neredeyse imkânsızlaştırır.",
                EarthArtworkFolder + "/Büyük Kanyon (Grand Canyon) — ABD  Arizona.png",
                FirstTwelveFolder + "/L09_BuyukKanyon.asset"),
            new Entry(
                "chichen-itza", "Chichén Itzá",
                "Yucatán / MEKSİKA",
                "Chichén Itzá, Maya dünyasının en önemli kentlerinden biriydi. Kukulkán Piramidi’nin basamakları ve gölgeleri, özellikle ekinoks günlerinde yılanı andıran etkileyici bir ışık oyunu oluşturur. Dev Top Oyunu Sahası ve kurban törenleriyle ilişkilendirilen Kutsal Kuyu, kentin spor, inanç ve gökyüzü gözlemlerini aynı merkezde buluşturduğunu gösterir.",
                EarthArtworkFolder + "/Chichén Itzá — Meksika  Yucatán.png",
                FirstTwelveFolder + "/L10_ChichenItza.asset"),
            new Entry(
                "kurtarici-isa-heykeli", "Kurtarıcı İsa Heykeli",
                "Rio de Janeiro / BREZİLYA",
                "Corcovado Dağı’nın zirvesinde kollarını şehre açan Art Deco heykel, Rio de Janeiro’nun simgesidir. 30 metre yüksekliğindeki figür, dağ ve deniz manzarasıyla birlikte dünyanın en tanınan silüetlerinden birini oluşturur. 1931’de tamamlanan heykelin betonarme gövdesi, ışığı yumuşak biçimde yansıtan milyonlarca küçük sabuntaşı karoyla kaplıdır.",
                EarthArtworkFolder + "/Christ the Redeemer — Brezilya  Riode Janeiro.png",
                FirstTwelveFolder + "/L11_KurtariciIsaHeykeli.asset"),
            new Entry(
                "cn-kulesi", "CN Kulesi", "Toronto / KANADA",
                "553,3 metrelik CN Kulesi, bir dönem dünyanın en yüksek bağımsız yapısıydı. Cam zeminli seyir alanları ve dışarıda yapılan EdgeWalk deneyimi, Toronto manzarasına güçlü bir yükseklik hissi katar. 1976’da açılan kule aslında iletişim sinyallerini şehrin hızla yükselen gökdelenlerinin üzerinden iletmek için tasarlanmıştı.",
                EarthArtworkFolder + "/CN Kulesi (CN Tower) — Kanada  Toronto.png",
                FirstTwelveFolder + "/L12_CNKulesi.asset"),
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

                EarthLandmarkLocalizationContent.EnglishEntry english =
                    EarthLandmarkLocalizationContent.GetEnglish(entry.Id);
                definition.ConfigureLocalizedForSetup(
                    entry.Id,
                    english.Title,
                    english.Description,
                    english.Sector,
                    entry.Title,
                    entry.Description,
                    entry.Sector,
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
                string sector,
                string description,
                string artworkPath,
                string definitionPath)
            {
                Id = id;
                Title = title;
                Sector = sector;
                Description = description;
                ArtworkPath = artworkPath;
                DefinitionPath = definitionPath;
            }

            public string Id { get; }
            public string Title { get; }
            public string Sector { get; }
            public string Description { get; }
            public string ArtworkPath { get; }
            public string DefinitionPath { get; }
        }
    }
}
