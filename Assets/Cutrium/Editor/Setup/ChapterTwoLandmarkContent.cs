using System;
using Cutrium.Presentation.Landmark;
using UnityEditor;
using UnityEngine;

namespace Cutrium.Editor.Setup
{
    public static class ChapterTwoLandmarkContent
    {
        public const string ChapterTwoFolder =
            FirstTwelveLandmarkContent.ContentFolder + "/Earth/Chapter02";

        public static readonly FirstTwelveLandmarkContent.Entry[] Entries =
        {
            Entry(
                "cin-seddi", "Çin Seddi", "ÇİN",
                "Çin Seddi tek ve kesintisiz bir duvar değil, farklı hanedanların yüzyıllar boyunca kurduğu geniş bir savunma ağıdır. Duvarlar, geçitler ve gözetleme kuleleri binlerce kilometrelik bir sistem oluşturur. Bölgeye göre sıkıştırılmış toprak, taş ya da tuğla kullanılan kulelerde duman, ateş ve bayrak sinyalleriyle haberler hızla aktarılırdı.",
                "ÇinSeddi — Çin.png", "L13_CinSeddi.asset"),
            Entry(
                "dubrovnik-surlari", "Dubrovnik Surları",
                "Dubrovnik / HIRVATİSTAN",
                "Dubrovnik’in yaklaşık iki kilometre uzunluğundaki taş surları, Adriyatik kıyısındaki tarihî kenti bütünüyle çevreler. Kulelerden görülen turuncu çatılar ve mavi deniz, şehrin en unutulmaz manzarasını oluşturur. 13. ve 17. yüzyıllar arasında sürekli güçlendirilen bu savunma kuşağı, zengin Ragusa Cumhuriyeti’nin bağımsızlığını yüzyıllarca korumasına yardım etti.",
                "Dubrovnik Surları — Hırvatistan.png",
                "L14_DubrovnikSurlari.asset"),
            Entry(
                "efes-antik-kenti", "Efes Antik Kenti",
                "İzmir / TÜRKİYE",
                "Efes, Roma döneminin en büyük liman ve ticaret kentlerinden biriydi. Celsus Kütüphanesi, mermer caddeleri ve Büyük Tiyatrosu kentin görkemini bugün hâlâ güçlü biçimde hissettirir. Yakındaki Artemis Tapınağı Antik Dünyanın Yedi Harikası arasında sayılırken, teras evlerdeki mozaik ve freskler zengin kentlilerin günlük yaşamına yakından bakmayı sağlar.",
                "Efes Antik Kenti - Izmir.png", "L15_Efes.asset"),
            Entry(
                "el-hamra-sarayi", "El Hamra Sarayı",
                "Granada / İSPANYA",
                "El Hamra, Nasrî hükümdarlarının saray ve kale kompleksidir. İnce geometrik süslemeler, yazılar, avlular ve akan suyun birlikte kullanımı yapıya sakin ama büyüleyici bir atmosfer kazandırır. Adı Arapçada “Kızıl Kale” anlamına gelir; gün batımında sıcak bir renge bürünen dış duvarları bu ismi hemen anlaşılır kılar.",
                "El Hamra Sarayı — İspanya  Granada.png",
                "L16_ElHamra.asset"),
            Entry(
                "eyfel-kulesi", "Eyfel Kulesi", "Paris / FRANSA",
                "1889 Dünya Fuarı için inşa edilen Eyfel Kulesi başlangıçta geçici bir yapı olarak tasarlanmıştı. Bir zamanlar eleştirilen demir kafes, bugün Paris’in en güçlü simgesidir. Yaklaşık 300 metrelik kule, radyo deneyleri ve haberleşme antenleri sayesinde kullanım değerini kanıtlayarak planlanan sökülüşten kurtuldu.",
                "France-Paris-Eiffel-Tower.png", "L17_EyfelKulesi.asset"),
            Entry(
                "fuji-dagi", "Fuji Dağı", "JAPONYA",
                "3.776 metre yüksekliğindeki Fuji, Japonya’nın en yüksek dağı ve hâlâ aktif kabul edilen bir stratovolkandır. Neredeyse kusursuz konisi yüzyıllardır sanatçılara, hacılara ve gezginlere ilham verir. Son büyük patlaması 1707’de gerçekleşen kutsal dağ, Hokusai’nin ünlü baskıları sayesinde Japonya dışındaki sanat dünyasında da kalıcı bir simgeye dönüştü.",
                "Fuji Dağı — Japonya.png", "L18_FujiDagi.asset"),
            Entry(
                "fushimi-inari-taisha", "Fushimi Inari Taisha",
                "Kyoto / JAPONYA",
                "Bereket ve pirinçle ilişkilendirilen İnari’ye adanan bu Şinto tapınağı, dağa doğru uzanan binlerce parlak kırmızı torii kapısıyla ünlüdür. Kapılar, ormanın içinde adeta sonsuz bir tünel oluşturur. Her kapı bir kişi ya da işletmenin bağışını temsil eder; yol boyunca görülen tilki heykelleri ise İnari’nin habercileri kabul edilir.",
                "Fushimi Inari Taisha — Japonya  Kyoto.png",
                "L19_FushimiInari.asset"),
            Entry(
                "galata-kulesi", "Galata Kulesi", "İstanbul / TÜRKİYE",
                "Günümüzdeki biçiminin kökleri 14. yüzyıldaki Ceneviz yerleşimine uzanan Galata Kulesi, Haliç’in üzerinde yükselir. Tepesinden Boğaz ve tarihî yarımadanın geniş panoraması görülür. Yüzyıllar boyunca savunma kulesi, hapishane ve yangın gözetleme noktası gibi farklı görevler üstlenen yapı, deprem ve yangınlardan sonra birçok kez onarıldı.",
                "Galata Kulesi - Istanbul.png", "L20_GalataKulesi.asset"),
            Entry(
                "golden-gate-koprusu", "Golden Gate Köprüsü",
                "San Francisco / ABD",
                "1937’de açılan Golden Gate, sisli boğazı geçen dev bir asma köprüdür. “International Orange” rengi, köprünün hem sis içinde seçilmesini hem de çevredeki tepelerle uyum kurmasını sağlar. 1.280 metrelik ana açıklığı tamamlandığında dünya rekoruydu; Art Deco ayrıntılı kuleleri suyun yaklaşık 227 metre üzerine yükselir.",
                "Golden Gate Köprüsü — ABD  San Francisco.png",
                "L21_GoldenGate.asset"),
            Entry(
                "gobeklitepe", "Göbeklitepe", "Şanlıurfa / TÜRKİYE",
                "Göbeklitepe’nin hayvan kabartmalı T biçimli taş sütunları yaklaşık MÖ 9600’e kadar uzanır. Bu anıtsal alan, çanak çömlekten ve yerleşik tarım toplumlarından bile daha eski olmasıyla insanlık tarihine bakışı değiştirdi. Bazıları beş metreyi aşan sütunların bulunduğu dairesel yapılarda belirgin ev izlerine rastlanmaması, alanın büyük buluşmalar ve ritüeller için kullanılmış olabileceğini düşündürür.",
                "Göbeklitepe - Şanlıurfa.png", "L22_Gobeklitepe.asset"),
            Entry(
                "kapalicarsi", "Kapalıçarşı", "İstanbul / TÜRKİYE",
                "Kökleri 15. yüzyıla uzanan Kapalıçarşı, kubbeli geçitleri ve binlerce dükkânıyla yaşayan bir ticaret labirentidir. Mücevherden halıya uzanan zanaat geleneği, İstanbul’un yüzyıllık alışveriş kültürünü taşır. Hanlar ve bedestenlerle örülü 60’tan fazla sokağı, farklı mesleklerin belirli bölümlerde toplandığı tarihî şehir düzenini hâlâ hissettirir.",
                "Grand Bazaar - Istanbul.png", "L23_Kapalicarsi.asset"),
            Entry(
                "ayasofya", "Ayasofya", "İstanbul / TÜRKİYE",
                "537’de tamamlanan Ayasofya’nın dev kubbesi, mimarlık tarihinde bir dönüm noktasıydı. Yüzyıllar boyunca kilise, cami ve müze olarak kullanılan yapı, Bizans ve Osmanlı mirasını aynı mekânda buluşturur. Kubbeyi kare ana mekâna bağlayan pandantifler büyük bir mühendislik yeniliğiydi; mozaikler ile dev hat levhaları da farklı dönemleri yan yana görünür kılar.",
                "Hagia Sophia - Istanbul.png", "L24_Ayasofya.asset"),
        };

        public static LandmarkDefinition[] CreateOrUpdateAssets()
        {
            EnsureFolder(ChapterTwoFolder);
            var definitions = new LandmarkDefinition[Entries.Length];
            for (int index = 0; index < Entries.Length; index++)
            {
                FirstTwelveLandmarkContent.Entry entry = Entries[index];
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
                    definition =
                        ScriptableObject.CreateInstance<LandmarkDefinition>();
                    AssetDatabase.CreateAsset(definition, entry.DefinitionPath);
                }

                definition.ConfigureForSetup(
                    entry.Id,
                    entry.Title,
                    entry.Description,
                    entry.Sector,
                    artwork);
                EditorUtility.SetDirty(definition);
                definitions[index] = definition;
            }

            return definitions;
        }

        private static FirstTwelveLandmarkContent.Entry Entry(
            string id,
            string title,
            string sector,
            string description,
            string artworkName,
            string assetName) =>
            new FirstTwelveLandmarkContent.Entry(
                id,
                title,
                sector,
                description,
                FirstTwelveLandmarkContent.EarthArtworkFolder
                    + "/" + artworkName,
                ChapterTwoFolder + "/" + assetName);

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
    }
}
