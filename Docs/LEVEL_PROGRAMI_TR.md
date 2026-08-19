# Cutrium 60 Level Türkçe Bölüm Programı

Bu belge, her levelde oyuncuya ne öğretildiğini ve bir önceki levele göre neyin
değiştiğini hızlıca görebilmek için hazırlanmış Türkçe test rehberidir.
Teknik kararların ve ilerleme kayıtlarının ana kaynağı
`.agent/plans/008-sixty-level-chapter-progression.md` dosyasıdır.

## Durum

- Chapter 1 — Temeller, Level 1–12: Uygulandı.
- Chapter 2 — Hareket ve Gravity, Level 13–24: Uygulandı; test ve dengeleme aşamasında.
- Chapter 3 — Yükselen Baskı, Level 25–36: Planlandı, henüz uygulanmadı.
- Chapter 4 — Birleşik Tehditler, Level 37–48: Planlandı, henüz uygulanmadı.
- Chapter 5 — Taş Bahçesi ve Ustalık, Level 49–60: Kaya prototipinin onayına bağlı.

## Kısaltmalar

- `N`: Normal top.
- `C`: Comet; Normal davranışlı, küçük ve hızlı top.
- `G`: Heavy/Giant; Normal davranışlı, büyük ve yavaş top.
- `H`: Hunter; bariyer başlangıcına sınırlı açıyla tepki veren top.
- `P`: Pulse; yavaş ve hızlı fazlar arasında geçiş yapan top.
- `HP`: Hunter ve Pulse özelliklerini birlikte taşıyan planlanmış birleşik top.
- `GW`: Gravity Well.
- `F`: Freeze Pulse.
- `I`: Instant Barrier.
- “Bariyer 3,10” gibi değerler bariyerin saniyedeki mantıksal büyüme hızıdır.
  Değer yükseldikçe bariyer daha hızlı tamamlanır ve genel olarak daha güvenli olur.
- “4 kırılma” oyuncunun level boyunca kaybedebileceği bariyer/can miktarıdır.

## Chapter 1 — Temeller

Bu chapter temel kesme mantığını, top gruplamayı, Hunter/Pulse okumayı ve
Freeze/Instant kullanımını öğretir.

| Lv | Bir önceki levele göre değişiklik ve amaç | Toplar | Hedef / bariyer | Sınır ve skill | Beklenen süre / zorluk |
| ---: | --- | --- | --- | --- | --- |
| 1 | Başlangıç eğitimi. Topsuz taraftaki alanın ele geçirildiğini öğretir. | 1 N | %75 / 3,40 | 5 kırılma | 15 sn / 1 |
| 2 | Bariyer belirgin biçimde yavaşlar ve hedef %3 artar. Top uzaklaşırken kesme zamanlaması öğretilir. | 1 N | %78 / 2,15 | 5 kırılma | 24 sn / 2 |
| 3 | İkinci Normal top eklenir. İki topu aynı bölgede tutma kararı başlar. | 2 N | %80 / 2,80 | 4 kırılma | 28 sn / 2 |
| 4 | Tek topa dönülür fakat hedef %84 olur. Küçük güvenli kesikler yerine büyük kesikler ve 10 kesim bütçesi denenir. | 1 N | %84 / 3,20 | 10 kesim, 4 kırılma | 30 sn / 2 |
| 5 | İlk Hunter tanıtılır. Oyuncu Hunter’ın kesime verdiği sınırlı tepkiyi yemleyip ardından keser. | 1 H | %80 / 2,60 | 4 kırılma | 30 sn / 2 |
| 6 | Hunter yerine ilk Pulse gelir. Kesimlerin Pulse’ın yavaş fazında başlatılması beklenir. | 1 P | %80 / 2,55 | 4 kırılma | 32 sn / 2 |
| 7 | Pulse daha tehlikeli hâle gelir, bariyer ciddi biçimde yavaşlar ve ilk Freeze verilir. Güvenli bir zaman penceresi oluşturma levelidir. | 1 P | %82 / 1,85 | 10 kesim, 3 kırılma, 1 F | 36 sn / 3 |
| 8 | İki Normal topa geçilir ve ilk Instant verilir. Uzun, açıkta kalan bir kesim için Instant saklanmalıdır. | 2 N | %84 / 1,75 | 9 kesim, 3 kırılma, 1 I | 38 sn / 3 |
| 9 | Hunter ile Normal aynı boarda gelir. Yalnızca Hunter’ın kesime tepki verdiği ayırt edilmelidir. | H + N | %84 / 2,45 | 3 kırılma | 38 sn / 3 |
| 10 | Hunter yerine Pulse gelir. Pulse fazı ile iki topun aynı tarafta bulunması aynı anda okunur. | P + N | %84 / 2,35 | 3 kırılma | 40 sn / 4 |
| 11 | Hunter ve Pulse birlikte kullanılır. Daha düşük bariyer hızı ve 8 kesim sınırında Freeze ile Instant arasında seçim yapılır. | H + P | %87 / 1,85 | 8 kesim, 2 kırılma, 1 F, 1 I | 43 sn / 4 |
| 12 | Üçüncü bir Normal eklenir ve hedef %90’a çıkar. Chapter 1’de öğrenilen gruplama, faz okuma, Hunter yemleme ve skill kullanımı birlikte sınanır. | H + P + N | %90 / 2,00 | 10 kesim, 2 kırılma, 1 F, 1 I | 45 sn / 5 |

## Chapter 2 — Hareket ve Gravity

Bu chapterın çalışan değerleri doğrudan mevcut implementasyondan alınmıştır.
Comet ve Heavy yeni davranış türleri değildir; ikisi de Normal top görselini ve
hareket kurallarını kullanır. Farkları hızları ve mantıksal yarıçaplarıdır.

| Lv | Bir önceki levele göre değişiklik ve amaç | Toplar | Hedef / bariyer | Sınır ve skill | Beklenen süre / zorluk |
| ---: | --- | --- | --- | --- | --- |
| 13 | Chapter 1 finalinden sonra rahatlama leveli. Tek sakin Normal, %76 hedef ve hızlı bariyerle temel ritme dönülür. | 1 N | %76 / 3,10 | 5 kırılma | 22 sn / 2 |
| 14 | İkinci Normal eklenir. Toplar paralel hareket ettiği için ortak hareket yönünden yararlanarak büyük kesim yapılır. | 2 N | %78 / 2,90 | 5 kırılma | 27 sn / 2 |
| 15 | Paralel düzen çapraz yörüngeye dönüşür, hedef %80 olur ve bariyer yavaşlar. Topların kesişmesini beklemek gerekir. | 2 N | %80 / 2,65 | 4 kırılma | 30 sn / 3 |
| 16 | İlk Comet tanıtılır. Tek topa dönülür; top küçülür ve hızlanır, buna karşılık bariyer tekrar hızlanır ve hedef %78’e iner. | 1 C | %78 / 3,00 | 4 kırılma | 29 sn / 3 |
| 17 | Comet yerine ilk Heavy gelir. Heavy daha büyük ve yavaştır; oyuncu düşük hızdan yararlanırken geniş çarpışma alanına mesafe bırakır. | 1 G | %80 / 2,80 | 4 kırılma | 30 sn / 3 |
| 18 | Heavy yerine Comet + Normal ikilisi gelir. Davranışlar aynı, hızlar farklıdır; hızlı topun tekrar uygun tarafa gelmesi beklenir. | C + N | %82 / 2,70 | 4 kırılma | 34 sn / 3 |
| 19 | Comet, Heavy ile değiştirilir. Hız farkına ek olarak Heavy’nin büyük yarıçapı hesaba katılarak iki top birlikte tutulur. | G + N | %82 / 2,60 | 4 kırılma | 35 sn / 3 |
| 20 | İki standart Normal topa dönülür ve Gravity Well ilk kez verilir. Skill seçildikten sonra boarda dokunulur; 4,5 birimlik alandaki yakın toplar aynı aktif oda içinde noktaya doğru yönelir. İlk dizilimde boardun ortasına yerleştirilen kuyu iki topa da ulaşır. | 2 N | %80 / 2,80 | 4 kırılma, 1 GW | 34 sn / 3 |
| 21 | Gravity korunur fakat toplardan biri Heavy olur. Farklı yarıçap ve hızdaki iki top Gravity ile aynı bölgede toplanmalıdır. | G + N | %83 / 2,55 | 4 kırılma, 1 GW | 38 sn / 4 |
| 22 | Gravity kaldırılır; Pulse + Comet ikilisi ve Freeze gelir. İki topun hızlandığı tehlikeli pencere Freeze ile kontrol edilir. | P + C | %84 / 2,55 | 3 kırılma, 1 F | 40 sn / 4 |
| 23 | Top sayısı ilk kez dörde çıkar. Dört top da yavaştır, hedef %82’ye düşer, bariyer hızlanır ve Gravity destek verir; amaç paniklemeden yararlı bir grubu toplamaktır. | 4 yavaş N | %82 / 2,90 | 4 kırılma, 1 GW | 42 sn / 4 |
| 24 | Dört yavaş Normal yerine Hunter + Comet + Heavy gelir. Hedef %86, kesim sınırı 10 olur; önce Gravity ile şekillendirip finalde Instant kullanmak beklenir. | H + C + G | %86 / 2,55 | 10 kesim, 3 kırılma, 1 GW, 1 I | 45 sn / 5 |

### Chapter 2 test kontrol listesi

- Level 16’da Comet, Normalden belirgin biçimde hızlı ve daha küçük hissettirmeli.
- Level 17’de Heavy yavaş olmalı fakat yakın kesimlerde büyük yarıçapı fark edilmelidir.
- Level 20’de Gravity butonuna ikinci kez basmak hedef seçimini iptal etmelidir.
- Geçersiz, UI üzerindeki veya menzilinde hiç top bulunmayan Gravity dokunuşu hak tüketmemeli; hedef seçimi açık kalmalıdır.
- Tamamlanmış bir bariyerin diğer tarafındaki top Gravity’den etkilenmemelidir.
- Level 23 dört topa rağmen hızlı bariyer, düşük hedef ve Gravity sayesinde panik leveline dönüşmemelidir.
- Level 24 zorlayıcı olabilir ama 10 kesim, Gravity ve Instant ile adil kalmalıdır.

## Chapter 3 — Yükselen Baskı

Bu chapter henüz uygulanmadı. Süreli level ve zamanla yeni top eklenmesi ayrı
ayrı tanıtılır; aynı levelde süre, artan top ve sert kesim limiti birleştirilmez.

| Lv | Bir önceki levele göre planlanan değişiklik ve amaç | Toplar | Hedef / bariyer | Sınır ve skill | Beklenen süre / zorluk |
| ---: | --- | --- | --- | --- | --- |
| 25 | Chapter 2 finalinden sonra tek Normal ve hızlı bariyerle rahatlama sağlanır; ilk cömert 45 saniyelik süre tanıtılır. | 1 N | %78 / 3,00 | 45 sn sayaç, 5 kırılma | 30 sn / 2 |
| 26 | İkinci Normal eklenir, hedef %80 olur ve süre 42 saniyeye iner. Daha kararlı kesimler beklenir. | 2 N | %80 / 2,80 | 42 sn sayaç, 4 kırılma | 34 sn / 3 |
| 27 | İki Normal yerine tek Comet gelir; süre 40 saniye olur ve zaman daralmadan kullanmak için Instant verilir. | 1 C | %82 / 2,70 | 40 sn sayaç, 4 kırılma, 1 I | 34 sn / 3 |
| 28 | Sayaç kaldırılır ve rahatlama sağlanır. Heavy + Pulse, daha hızlı bariyer ve Freeze ile mekânsal okuma yapılır. | G + P | %80 / 2,90 | 4 kırılma, 1 F | 30 sn / 2 |
| 29 | İlk artan-top modu gelir. Tek Normalle başlanır; 12 saniyelik uyarılı aralıklarla sayı en fazla üçe çıkar. | Başlangıç 1 N, üst sınır 3 N | %80 / 3,00 | 12 sn artış, 5 kırılma | 32 sn / 2 |
| 30 | Artış aralığı 10 saniyeye düşer ve üst sınır dört olur. Oyuncu güvenli küçük kesimler yerine erken büyük kesim yapmalıdır. | Başlangıç 1 N, üst sınır 4 N | %82 / 2,90 | 10 sn artış, 4 kırılma | 36 sn / 3 |
| 31 | Başlangıç topu Pulse olur; sonradan Normaller gelir ve Freeze eklenir. Karışık artış dalgası kontrol edilir. | Başlangıç 1 P, N eklenir, üst sınır 3 | %83 / 2,65 | 12 sn artış, 4 kırılma, 1 F | 37 sn / 3 |
| 32 | Artan-top kuralı kaldırılır. İki yavaş Heavy ve hızlı bariyerle kısa bir dinlenme leveli sunulur. | 2 G | %78 / 3,10 | 5 kırılma | 25 sn / 2 |
| 33 | Tek Hunter’a dönülür ve 40 saniyelik sayaç eklenir. Hunter yemleme süre baskısı altında yapılır. | 1 H | %82 / 2,70 | 40 sn sayaç, 4 kırılma | 34 sn / 3 |
| 34 | Sayaç yerine tekrar artan Normaller gelir. Bir sonraki top gelmeden önce Gravity ile gruplama yapılır. | Başlangıç 1 N, üst sınır 3 N | %84 / 2,75 | 12 sn artış, 4 kırılma, 1 GW | 38 sn / 3 |
| 35 | Artış kaldırılır; iki Normal, %85 hedef ve 40 saniyelik sayaç gelir. Instant son hedef hamlesini güvenceye alır. | 2 N | %85 / 2,65 | 40 sn sayaç, 3 kırılma, 1 I | 38 sn / 4 |
| 36 | Sayaç yerine dört topa kadar çıkan karışık dalga gelir. Hunter, Normal ve Pulse; Freeze ve Gravity ile birlikte yönetilir. | Başlangıç 1 H; N, P, N eklenir | %84 / 2,75 | 10 sn artış, 4 kırılma, 1 F, 1 GW | 42 sn / 4 |

## Chapter 4 — Birleşik Tehditler

Bu chapter henüz uygulanmadı. `HP`, Hunter’ın kesime tepkisini ve Pulse’ın
fazlarını daha yumuşak ayarlarla birleştiren tek bir top olacaktır.

| Lv | Bir önceki levele göre planlanan değişiklik ve amaç | Toplar | Hedef / bariyer | Sınır ve skill | Beklenen süre / zorluk |
| ---: | --- | --- | --- | --- | --- |
| 37 | Chapter 3 finalinden sonra bütün süre/artış kuralları kaldırılır. Normal + Heavy ile sakin başlangıç yapılır. | N + G | %78 / 3,10 | 5 kırılma | 25 sn / 2 |
| 38 | İlk hafif Hunter+Pulse birleşik top tek başına tanıtılır. Hedef ve hızlı bariyer korunur. | 1 HP | %78 / 3,00 | 5 kırılma | 27 sn / 2 |
| 39 | Aynı tek birleşik top korunur; hedef %80 olur, bariyer yavaşlar ve birleşik topun yavaş fazından yararlanmak gerekir. | 1 HP | %80 / 2,85 | 4 kırılma | 30 sn / 3 |
| 40 | Bir Normal ve Gravity eklenir. Birleşik top tepki vermeden önce ikili toplanmalıdır. | HP + N | %82 / 2,75 | 4 kırılma, 1 GW | 34 sn / 3 |
| 41 | Normal yerine ayrı bir Pulse gelir ve Gravity yerine Freeze verilir. Tehlikeli ortak hızlı faz dondurulur. | HP + P | %83 / 2,70 | 4 kırılma, 1 F | 36 sn / 3 |
| 42 | Birleşik tehdit tamamen kaldırılır. Dört yavaş Normal, hızlı bariyer, düşük hedef ve Gravity ile rahatlama leveli oluşturulur. | 4 yavaş N | %78 / 3,10 | 5 kırılma, 1 GW | 30 sn / 2 |
| 43 | Dört Normal yerine HP + Comet gelir. Hızlı top ve birleşik tepkinin yarattığı açık kesimde Instant kullanılır. | HP + C | %84 / 2,70 | 3 kırılma, 1 I | 37 sn / 4 |
| 44 | Comet, Heavy ile değiştirilir ve Instant yerine Gravity verilir. Birleşik davranış ile büyük yarıçap birlikte yönetilir. | HP + G | %84 / 2,65 | 3 kırılma, 1 GW | 38 sn / 4 |
| 45 | Tek HP’ye dönülür fakat hedef %85 ve 9 kesim sınırı gelir. Verimli kesim planı ölçülür. | 1 HP | %85 / 2,65 | 9 kesim, 3 kırılma | 37 sn / 4 |
| 46 | Kesim sınırı kaldırılır, hedef %82’ye düşer ve 42 saniyelik sayaç ile Freeze eklenir. | 1 HP | %82 / 2,80 | 42 sn sayaç, 4 kırılma, 1 F | 35 sn / 3 |
| 47 | Sayaç kaldırılır; HP’nin yanına ayrı Hunter gelir. Hangisinin nasıl tepki verdiği Gravity ile alan yönetirken ayırt edilir. | HP + H | %84 / 2,75 | 3 kırılma, 1 GW | 39 sn / 4 |
| 48 | Ayrı Pulse da eklenir. Freeze, Instant ve Gravity arasından doğru araç doğru anda seçilir. | HP + H + P | %85 / 2,70 | 3 kırılma, 1 F, 1 I, 1 GW | 43 sn / 5 |

## Chapter 5 — Taş Bahçesi ve Ustalık

Bu chapterın kaya mekaniği önce Level 49’da prototiplenecektir. Kaya, bariyerin
boardun karşı kenarına gitmeden kayada durabilmesini sağlayan sabit engeldir.
Prototip adil veya teknik olarak makul bulunmazsa Level 49–60 açık-board ustalık
chapterına çevrilecektir.

| Lv | Bir önceki levele göre planlanan değişiklik ve amaç | Toplar | Hedef / bariyer | Sınır ve skill | Beklenen süre / zorluk |
| ---: | --- | --- | --- | --- | --- |
| 49 | Chapter 4 finalinden sonra bütün birleşik baskı kaldırılır. Tek Normal ve tek basit kaya ile bariyerin kayada durması öğretilir. | 1 N | %75 / 3,10 | 1 kaya, 5 kırılma | 25 sn / 2 |
| 50 | İkinci Normal eklenir. Tek kayanın çevresini iki bağlantılı kesimle tamamlama rotası denenir. | 2 N | %78 / 2,90 | 1 kaya, 5 kırılma | 29 sn / 2 |
| 51 | Toplardan biri Heavy olur, hedef %80’e çıkar ve Gravity eklenir. Büyük top kaya çevresinde gruplanır. | G + N | %80 / 2,85 | 1 kaya, 4 kırılma, 1 GW | 32 sn / 3 |
| 52 | İkili yerine tek Pulse gelir ve kaya sayısı ikiye çıkar. Kaya uçları çevresinde Pulse zamanlaması Freeze ile yönetilir. | 1 P | %80 / 2,80 | 2 kaya, 4 kırılma, 1 F | 32 sn / 3 |
| 53 | Pulse yerine Hunter gelir. Hunter, planlanan kaya rotasından uzağa yemlenir. | 1 H | %82 / 2,75 | 2 kaya, 4 kırılma | 34 sn / 3 |
| 54 | Kayalar kaldırılır ve beş yavaş Normal gelir. Hızlı bariyer, %78 hedef ve Gravity sayesinde yüksek top sayılı rahatlama testi yapılır. | 5 yavaş N | %78 / 3,20 | 5 kırılma, 1 GW | 30 sn / 2 |
| 55 | Beş sabit top yerine tek Normalle başlanır; bir kaya ve üçe kadar artan top kuralı gelir. Yeni top gelmeden Gravity ile grup hazırlanır. | Başlangıç 1 N, üst sınır 3 N | %82 / 2,80 | 1 kaya, 12 sn artış, 4 kırılma, 1 GW | 37 sn / 4 |
| 56 | Artan toplar yerine tek Comet gelir. Kaya rotası 45 saniyelik sayaç altında Instant ile tamamlanır. | 1 C | %80 / 2,85 | 1 kaya, 45 sn sayaç, 4 kırılma, 1 I | 35 sn / 3 |
| 57 | Sayaç kaldırılır; iki Normal ve 2–3 kaya ile okunabilir çoklu bölme bulmacası gelir. | 2 N | %84 / 2,75 | 2–3 kaya, 4 kırılma | 39 sn / 4 |
| 58 | İki Normal yerine tek birleşik HP gelir, kaya sayısı bire düşer ve Gravity ile sabit geometri çevresinde kontrol edilir. | 1 HP | %83 / 2,80 | 1 kaya, 4 kırılma, 1 GW | 37 sn / 4 |
| 59 | HP yerine ayrı Hunter + Pulse gelir; iki kaya, Freeze ve Gravity ile Taş Bahçesi kimlikleri tekrar edilir. | H + P | %85 / 2,70 | 2 kaya, 3 kırılma, 1 F, 1 GW | 42 sn / 4 |
| 60 | Finale Normal ve Heavy eklenir; HP geri gelir. İki kaya ve üç skill ile cömert ama kapsamlı ustalık kutlaması yapılır. | HP + N + G | %86 / 2,75 | 2 kaya, 4 kırılma, 1 F, 1 I, 1 GW | 45 sn / 5 |

## Landmark Aralıkları

Landmark sırası gameplay ayarlarından bağımsızdır ve level numarasıyla eşleşir:

| Chapter | Leveller | Landmark aralığı |
| --- | --- | --- |
| 1 | 1–12 | Angkor Wat — CN Kulesi |
| 2 | 13–24 | Çin Seddi — Ayasofya |
| 3 | 25–36 | Ha Long Körfezi — Paskalya Adası Moai Heykelleri |
| 4 | 37–48 | Mont-Saint-Michel — Santorini |
| 5 | 49–60 | Sidney Opera Binası — Yerebatan Sarnıcı |

## Test ve Dengeleme Kuralı

- Normal bir level yaklaşık 20–45 saniye sürmelidir.
- Yeni bir mekanik önce tek başına veya düşük baskıyla tanıtılmalıdır.
- Dört ve beş toplu levellerde hedef, hız, bariyer veya skill desteği oyuncuyu
  bunaltmayacak şekilde telafi edilmelidir.
- Bir level gereğinden zor veya kolay bulunursa top sayısı, top hızı, hedef,
  bariyer hızı ve verilen skill birbirinden bağımsız ayarlanmalıdır.
- Bir chapter owner testinden geçmeden sonraki chapter uygulanmamalıdır.
