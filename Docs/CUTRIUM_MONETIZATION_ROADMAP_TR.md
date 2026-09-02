# Cutrium — Para Kazanma ve Ekonomi Uygulama Yol Haritası

> English version: [CUTRIUM_MONETIZATION_ROADMAP.md](CUTRIUM_MONETIZATION_ROADMAP.md)
>
> Bu belge Claude Code / CLI için bir uygulama yol haritasıdır.
> Tüm görevleri aynı anda uygulamayın.
> Geliştirici her seferinde bir görevi açıkça talep edecektir; örneğin:
> **"Task 01'i uygula"**.

## Bu Belge Nasıl Kullanılmalı

1. Bağımlılıkların ve gelecekteki sistemlerin anlaşılması için kodu değiştirmeden önce belgenin tamamını okuyun.
2. Belirli bir görevin uygulanması istendiğinde yalnızca **o görevi** ve onun için kesinlikle gerekli olan minimum altyapıyı uygulayın.
3. Daha sonraki görevleri kendiliğinizden uygulamayın.
4. Uygulamaya başlamadan önce mevcut proje mimarisini, kuralları, kayıt sistemini, UI sistemini, yapılandırma/veri modelini ve ilgili oynanış kodunu inceleyin. Uygun yerlerde mevcut sistemleri yeniden kullanın; gereksiz yere paralel bir mimari oluşturmayın.
5. Seçilen görev açıkça değiştirmediği sürece mevcut oynanış davranışını koruyun.
6. Bu belgede gösterilen değerler başlangıç dengeleme değerleridir. Makul olduğu yerlerde sabit kodlanmış değerler yerine yapılandırılabilir/veri odaklı değerleri tercih edin.
7. Geliştirici, bir görev talep edilmeden önce gerekli görsel/ses assetlerini ekleyecektir. İlgili görevin `ASSETS` bölümüne girilmiş yolları kullanın. Bir asset alanı boşsa yol uydurmayın veya ilgisiz bir asseti sessizce kullanmayın. Yalnızca açıkça uygun olduğunda mevcut bir proje assetini yeniden kullanın; aksi takdirde eksik asseti bildirin.
8. Bir görevi uygularken ilgisiz ekranları veya sistemleri yeniden tasarlamayın.
9. Uygulamayı üretime hazır tutun: ilgili olduğunda kalıcılık, hata durumları, yinelenen ödüller, tekrarlanan buton dokunuşları, reklam hataları, satın alma hataları, sahne yeniden yüklemeleri ve benzeri uç durumlar ele alınmalıdır.
10. Bir görevi tamamladıktan sonra şunları raporlayın: değiştirilen dosyalar, eklenen/değiştirilen sistemler, eklenen yapılandırma değerleri, kullanılan asset yolları, kalıcılık değişiklikleri ve hâlâ gerekli olan manuel Editor/mağaza konsolu kurulumu.
11. Ses asseti girdilerinde bir `ElevenLabs SFX Prompt` bulunabilir. Bu prompt yalnızca geliştirici için üretim notudur ve bir asset yolu değildir. Geliştirici sesi ürettikten sonra `[ASSET YOLU]` alanını gerçek proje yoluyla değiştirebilir ve promptu silebilir. Claude hiçbir zaman ses üretmeye çalışmamalı, prompt metnini runtime asseti olarak değerlendirmemeli veya geçerli bir asset yolu varken prompt belgede kaldığı için uygulamayı engellememelidir.
12. SFX oynatma normalde düşük seviyeli ekonomi/kayıt değişimlerinden otomatik olarak değil, oyuncunun gördüğü oynanış/UI olayından tetiklenmelidir. Tek bir eylem birden fazla iç olay oluşturduğunda yinelenen veya üst üste binen seslerden kaçının.

---

# Mevcut Oyun Bağlamı

Cutrium, dikey ekranda oynanan mobil bir arcade-bulmaca oyunudur. Oyuncu, tahtayı bölmek ve boş bölgeleri güvenli şekilde ele geçirmek için yatay/dikey bariyerler çizer; hareketli tehditler tamamlanmamış bariyerleri yok edebilir. Gerekli ele geçirilmiş alan yüzdesine ulaşıldığında bölüm tamamlanır. Her bölüm kumun altında gerçek dünyadan bir simge yapı ortaya çıkarır.

Mevcut oynanış şunları içerir:

- Bölüm başına can / hata hakkı.
- İlgili bölümlerde sınırlı kesme sayısı.
- Freeze Pulse güçlendirmesi.
- Instant Barrier güçlendirmesi.
- Gravity Well güçlendirmesi.
- Ramak kala / kombo bağlantılı oynanış kavramları.
- Simge yapı temelli bölüm ilerlemesi.
- Henüz tamamlanmış bir ekonomi/kullanım senaryosu olmayan mevcut Shop bölümü.

Para kazanma yaklaşımı:

- Oyuncunun oynamaya devam etmesini engelleyen global bir enerji sistemi olmayacak.
- Ödeme yapmayan bir oyuncu oyunu oynayabilmeli ve tamamlayabilmelidir.
- Para kazanma öncelikle kolaylık, toparlanma, isteğe bağlı hızlandırma, kozmetik ve reklam kaldırma sağlamalıdır.
- Satın alınan güçlendirmeleri zorunlu kılan bölümler tasarlamaktan kaçının.
- Agresif kazanmak-için-öde veya bilinçli hayal kırıklığı yaratma mekaniklerinden kaçının.
- Ekonomiyi anlaşılır tutun. Tek bir yumuşak para birimiyle başlayın: **Coin**.

---

# AŞAMA 1 — TEMEL EKONOMİ

## GÖREV 01 — Temel Coin Sistemi

**Görev Kimliği:** `01_CORE_COIN_SYSTEM`

### Amaç

Daha sonraki tüm ekonomi ve para kazanma özelliklerinin kullanacağı temel yumuşak para birimi sistemini oluşturmak.

### Gereksinimler

- `Coin` / `Coins` adlı tek bir yumuşak para birimi ekleyin.
- Kalıcı bir oyuncu Coin bakiyesi tutun.
- Uygulanabildiği yerde Coin bakiyesini projenin mevcut kayıt/bulut kayıt mimarisiyle bütünleştirin.
- Coin sorgulama, ekleme, harcama ve doğrulama için merkezi bir API/servis sağlayın.
- Gelecekte analiz/hata ayıklama sırasında ödül ve harcamaların ayırt edilebilmesi için Coin değişimleri, uygulanabilir olduğunda neden/kaynak kimliği desteklemelidir.
- Negatif bakiyeyi önleyin.
- Oyuncunun yeterli Coin'i olmadığında harcama güvenli şekilde başarısız olmalıdır.
- Bakiye değiştiğinde UI bunu gözlemleyebilmeli/yenileyebilmelidir.
- Sonraki görevleri uygulamadan sistemi gelecekteki kaynaklar ve harcama alanları için hazırlayın.
- Gem, Ticket, Energy veya başka bir para birimi eklemeyin.

### Başlangıç Yapılandırması

```text
Currency: Coins
StartingBalance: [0]
```

### Assetler

Uygulama talep edilmeden önce bu alanları doldurun.

```text
Coin İkonu:
[CoinStackL1.png]

Küçük/HUD Coin İkonu:
[CoinStackL1.png]

Coin Kazanma SFX:
[SFX_CoinEarn]
Kullanım: Yeniden kullanılabilir pozitif Coin geri bildirim sesi. Oyuncunun gördüğü bir UI akışı Coin kazancını gerçekten sunduğunda/bakiyeye eklediğinde çalın (örneğin ödül alma veya görünür bakiye artışı). Sonraki ödül akışları birden fazla değişim ya da kendilerine özel daha zengin SFX oluşturabileceğinden her düşük seviyeli `AddCoins`/kayıt değişiminde otomatik çalmayın.

Coin Harcama SFX:
[SFX_CoinSpend]
Kullanım: Yeniden kullanılabilir başarılı harcama geri bildirimi. Yalnızca Coin işlemi başarılı olduktan ve harcama oyuncu için görünür/anlamlı olduğunda çalın. Yetersiz bakiye, iptal edilmiş eylem, doğrulama hatası veya sessiz arka plan düzeltmesinde çalmayın.
```

### Ses Entegrasyonu Notu

- Görev 01, uygunsa bu ortak Coin SFX'lerini projenin normal ses/UI mimarisi üzerinden erişilebilir kılmalı; ancak bunları her düşük seviyeli para birimi değişiminin içinde otomatik olarak **çalmamalıdır**.
- `Coin Kazanma SFX`, daha özel bir ödül sesi onun yerini almadığında sonraki oyuncuya görünür kazanma akışları (Görev 02 bölüm ödülü, Görev 03 bonusları ve diğer açık Coin kazanımları) için tasarlanmıştır.
- `Coin Harcama SFX`, daha özel bir satın alma/canlandırma sesi onun yerini almadığında sonraki oyuncuya görünür başarılı harcama akışları (Görev 04 ve sonrası satın alma/toparlanma işlemleri) için tasarlanmıştır.
- Sonraki bir görevin kendine ait SFX'i varsa (örneğin `Ödül Alma SFX`, `Güçlendirme Satın Alma SFX` veya `Canlandırma SFX`), mevcut ses tasarımı açıkça katmanlı geri bildirim istemediği sürece bu özel sesi tercih edin ve genel Coin SFX'ini üzerine bindirmeyin.

### Kalıcılık

- Coin bakiyesi uygulama yeniden başlatıldığında korunmalıdır.
- Mimari aksini gerektirmedikçe rakip bir kayıt dosyası oluşturmak yerine mevcut oyuncu verisi/kayıt modeliyle çalışmalıdır.
- Bu alan eklenmeden önce oluşturulmuş kayıtlar yüklenirken mevcut oyuncular güvenli bir varsayılan bakiye almalıdır.

### Uç Durumlar

- Aynı frame/event içinde birden fazla ödül çağrısı.
- Mevcut bakiyenin tam olarak harcanması.
- Mevcut bakiyeden fazlasını harcama girişimi.
- Coin alanı bulunmayan eski kayıt verisinin yüklenmesi.
- Kayıt/yükleme hatası davranışı mevcut proje kurallarını izlemelidir.

### Kabul Kriterleri

- Coin bakiyesi hedeflenen oyun mimarisi üzerinden global olarak okunabilir.
- Coin doğru şekilde eklenebilir ve harcanabilir.
- Yetersiz bakiye işlemleri reddedilir.
- Bakiye hiçbir zaman negatif olmaz.
- Bakiye yeniden başlatma/yeniden yükleme sonrasında korunur.
- Mevcut kayıt verisi uyumlu kalır.
- UI dinleyicileri bakiye değişikliklerine tepki verebilir.
- Daha sonraki hiçbir para kazanma görevi uygulanmaz.

### Yapılmaması Gerekenler

- Henüz bölüm ödüllerini uygulamayın.
- Ödüllü reklamları uygulamayın.
- IAP uygulamayın.
- Güçlendirme satın almayı uygulamayın.
- İkinci bir para birimi eklemeyin.

---

## GÖREV 02 — Bölüm Coin Ödülü

**Görev Kimliği:** `02_LEVEL_COIN_REWARD`  
**Bağımlılık:** Görev 01

### Amaç

Bir bölüm başarıyla tamamlandığında Coin vermek.

### Gereksinimler

- Başarılı bölüm tamamlamaya temel Coin ödülü ekleyin.
- İlk varsayılan değer: tamamlanan bölüm başına `100 Coin`.
- Ödül miktarı, proje mimarisiyle uyumlu biçimde yapılandırılabilir/veri odaklı olmalıdır.
- Aynı tamamlama olayının temel ödülü yanlışlıkla birden fazla kez vermesini önleyin.
- Kazanılan Coin'leri mevcut bölüm tamamlama akışında gösterin.
- Coin'leri Görev 01'in para birimi API'si üzerinden ekleyin.

### Assetler

```text
Ödül Coin İkonu:
[CoinStackL1.png]

Ödül Container / Arka Planı:
Level complete overlayinde gösterebiliriz. (son başarılı kesim yapılınca complete captured cuts sayısı yazan overlay burayı toparlayıp burada gösterebiliriz. Şu kadar coin kazandık diye.)
game ekranında çarkın tam tersine yani çark en sağda en sola bir coin ikonu koyup onun yanına da bakiyemizi yazalım. complete overlayde de coinler buraya uçsun sonra level complete ekranına geçsin uçma tamamlanınca.

Ödül Alma SFX:
[SFX_CoinEarn.wav]
Kullanım: Bölüm tamamlama Coin ödülü başarıyla alındığında/bakiyeye eklendiğinde ve görsel olarak onaylandığında bir kez çalın. Tamamlama UI'ı daha önce bitmiş bir ödül alma işleminden sonra yeniden açılır veya geri yüklenirse tekrar çalmayın.
```

### Kabul Kriterleri

- Bir bölümü tamamlamak, yapılandırılan miktarı o tamamlama için tam olarak bir kez verir.
- UI kazanılan miktarı gösterir.
- Bakiye doğru şekilde güncellenir ve kalıcı olur.

---

## GÖREV 03 — Performans Coin Ödülleri

**Görev Kimliği:** `03_PERFORMANCE_REWARDS`  
**Bağımlılık:** Görevler 01–02

### Amaç

Başarılı oynanışı bonus Coin ile ödüllendirmek.

### İlk Bonus Adayları

```text
Ramak Kala:            +10 Coin
Mükemmel Kesim:        +20 Coin
Can Kaybetmeden:       +30 Coin
Güçlendirme Kullanmadan:+30 Coin
```

### Gereksinimler

- Yalnızca mevcut olan veya güncel oynanıştan güvenilir şekilde türetilebilen performans sinyallerini kullanın.
- Desteklenmeyen istatistikleri uydurmayın.
- Bonus tanımları ve miktarları yapılandırılabilir olmalıdır.
- Bölüm tamamlandığında açık bir ödül dökümü gösterin.
- Aynı tamamlama için yinelenen bonus ödemelerini önleyin.

### Assetler

```text
Ramak Kala Bonus İkonu:
[ASSET YOLU]

Mükemmel Kesim Bonus İkonu:
[ASSET YOLU]

Can Kaybetmeden İkonu:
[ASSET YOLU]

Güçlendirme Kullanmadan İkonu:
[ASSET YOLU]

Bonus SFX:
[ASSET YOLU]
Kullanım: Sonuç ekranında bir performans bonusu satırı/rozeti gösterildiğinde veya onaylandığında çalın. Birkaç bonus birlikte görünüyorsa aynı sesin birden fazla örneğini aynı anda bindirmek yerine sıraya koyun veya çalma sıklığını sınırlayın.
```

---

# AŞAMA 2 — EKONOMİ HARCAMA ALANLARI

## GÖREV 04 — Güçlendirme Envanteri ve Coin Ekonomisi

**Görev Kimliği:** `04_POWERUP_INVENTORY_ECONOMY`  
**Bağımlılık:** Görev 01

### Amaç

Mevcut güçlendirmelere kalıcı miktarlar vermek ve Coin ile edinilmelerini sağlamak.

### Güçlendirmeler

```text
Freeze Pulse       — başlangıç fiyatı: 200 Coin
Instant Barrier    — başlangıç fiyatı: 250 Coin
Gravity Well       — başlangıç fiyatı: 250 Coin
```

### Gereksinimler

- Desteklenen her güçlendirme için envanter miktarı tutun.
- Miktarları mevcut oyuncu verisi mimarisini kullanarak kalıcılaştırın.
- Fiyatlar yapılandırılabilir olmalıdır.
- Satın alma yalnızca geçerli bir işlemden sonra Coin tüketmelidir.
- Oynanışta kullanım envanter miktarını doğru şekilde azaltmalıdır.
- Güçlendirmeleri bölümleri tamamlamak için zorunlu hâle getirmeyin.

### Assetler

```text
Freeze Pulse İkonu:
[ASSET YOLU]

Instant Barrier İkonu:
[ASSET YOLU]

Gravity Well İkonu:
[ASSET YOLU]

Güçlendirme Satın Alma SFX:
[ASSET YOLU]
Kullanım: Güçlendirme satın alımı başarılı olduktan ve hem Coin bakiyesi hem envanter miktarı güncellendikten sonra çalın. Yetersiz Coin veya başarısız işlem durumunda asla çalmayın.

Güçlendirme Boş Durumu:
[ASSET YOLU]
```

---

## GÖREV 05 — Coin ile Ekstra Can

**Görev Kimliği:** `05_COIN_EXTRA_LIFE`  
**Bağımlılık:** Görev 01

### Amaç

Bölümün oyun bitti koşuluna ulaşan oyuncunun bir ek can/devam fırsatı için Coin harcamasını sağlamak.

### Başlangıç Yapılandırması

```text
ExtraLifeCost: 150 Coin
MaxCoinLifeRevivesPerLevel: 1
```

### Gereksinimler

- Toparlanma seçeneğini yalnızca uygun başarısızlık noktasında sunun.
- Harcama merkezi para birimi sistemini kullanmalıdır.
- Mevcut bölüm durumuna mevcut oynanış mimarisine uygun şekilde devam edin.
- Sınır yapılandırılabilir olmalıdır.
- Sonsuz Coin tabanlı canlandırma döngüsü oluşturmayın.

### Assetler

```text
Can / Kalp İkonu:
[ASSET YOLU]

Canlandırma Popup Arka Planı:
[ASSET YOLU]

Canlandırma Butonu:
[ASSET YOLU]

Canlandırma SFX:
[ASSET YOLU]
Kullanım: Ekstra can satın alımı başarılı olduktan ve oyuncu gerçekten geri getirildiğinde/devam ettirildiğinde çalın. Canlandırma teklifi açıldığında veya ödeme başarısız olduğunda çalmayın.
```

---

## GÖREV 06 — Coin ile Ekstra Kesim

**Görev Kimliği:** `06_COIN_EXTRA_CUT`  
**Bağımlılık:** Görev 01

### Amaç

Ele geçirme hedefine ulaşılmadan önce kesme hakkı bittiğinde oyuncunun Coin ile bir ek kesim satın almasını sağlamak.

### Başlangıç Yapılandırması

```text
ExtraCutCost: 120 Coin
MaxCoinExtraCutsPerLevel: 1
```

### Assetler

```text
Kesim İkonu:
[ASSET YOLU]

Ekstra Kesim Popup:
[ASSET YOLU]

Ekstra Kesim Butonu:
[ASSET YOLU]

Ekstra Kesim SFX:
[ASSET YOLU]
Kullanım: Ekstra kesim satın alımı başarılı olduktan ve ek kesim gerçekten verildikten sonra çalın. Teklif yalnızca gösterildiğinde çalmayın.
```

---

# AŞAMA 3 — ÖDÜLLÜ REKLAMLAR

## GÖREV 07 — Ödüllü Reklam: Ekstra Can

**Görev Kimliği:** `07_REWARDED_AD_EXTRA_LIFE`  
**Bağımlılık:** Görev 05 + seçilen reklam SDK'sı/entegrasyonu

### Amaç

Coin tabanlı canlandırmaya isteğe bağlı ödüllü reklam alternatifi eklemek.

### Kurallar

- Oyuncu reklamı izlemeyi açıkça seçer.
- Ödül yalnızca ödüllü reklamın tamamlandığı doğrulandıktan sonra verilir.
- Başarısız/iptal edilmiş/kullanılamayan reklamlar can vermemelidir.
- İlk maksimum: bölüm başına 1 ödüllü reklam canlandırması.
- Coin ve reklam seçenekleri aynı toparlanma UI'ında birlikte bulunabilir.

### Assetler

```text
Reklam İzle İkonu:
[ASSET YOLU]

Ödüllü Can Butonu:
[ASSET YOLU]

Yükleme Göstergesi:
[ASSET YOLU]
```

### Harici Kurulum

```text
Reklam Sağlayıcısı / SDK:
[AD]

Ödüllü Reklam Birim Kimliği — Android:
[DEĞER]

Ödüllü Reklam Birim Kimliği — iOS:
[DEĞER]
```

---

## GÖREV 08 — Ödüllü Reklam: Ekstra Kesim

**Görev Kimliği:** `08_REWARDED_AD_EXTRA_CUT`  
**Bağımlılık:** Görev 06 + ödüllü reklam altyapısı

### Amaç

İsteğe bağlı ödüllü reklam karşılığında bir ek kesim sunmak.

### Kurallar

Görev 07 ile aynı güvenilirlik ve ödül doğrulama kuralları geçerlidir.

### Assetler

```text
Reklam İzle İkonu:
[ASSET YOLU]

Ödüllü Kesim Butonu:
[ASSET YOLU]

Ekstra Kesim İkonu:
[ASSET YOLU]
```

---

## GÖREV 09 — Ödüllü Reklam: Bölüm Coin'lerini İkiye Katlama

**Görev Kimliği:** `09_REWARDED_DOUBLE_COINS`  
**Bağımlılık:** Görev 02 + ödüllü reklam altyapısı

### Amaç

Başarılı bölüm tamamlamadan sonra oyuncunun uygun Coin ödülünü çarpmak için isteğe bağlı olarak ödüllü reklam izlemesini sağlamak.

### Başlangıç Yapılandırması

```text
RewardedCoinMultiplier: 2x
```

### Gereksinimler

- Temel ödül reklam izlenmeden alınabilmelidir.
- Ek ödül yalnızca reklam başarıyla tamamlandıktan sonra verilmelidir.
- Bir tamamlamanın ödülü yalnızca bir kez ikiye katlanabilir.
- Yeniden bağlanma/yeniden yükleme/tekrarlanan dokunuşların ödüllü kısmı çoğaltamayacağı bir mantık kurun.

### Assetler

```text
2X Ödül İkonu:
[ASSET YOLU]

Reklam İzle İkonu:
[ASSET YOLU]

Coin Ödül Animasyonu:
[ASSET YOLU]

Ödül SFX:
[ASSET YOLU]
Kullanım: Ödüllü reklamın tamamlandığı doğrulandıktan ve ek 2x Coin kısmı başarıyla bakiyeye eklendikten sonra çalın. Normal temel ödül alma sesinden daha zengin hissettirmeli ancak kısa kalmalıdır.
```

---

# AŞAMA 4 — SHOP

## GÖREV 10 — Shop V1: Güçlendirmeler

**Görev Kimliği:** `10_SHOP_POWERUPS`  
**Bağımlılık:** Görevler 01 ve 04

### Amaç

Mevcut Shop bölümünü Coin tabanlı, işlevsel bir güçlendirme mağazasına dönüştürmek.

### Gereksinimler

- Freeze Pulse, Instant Barrier ve Gravity Well'i gösterin.
- Fiyatı ve sahip olunan miktarı gösterin.
- Coin kullanarak satın alınmasını sağlayın.
- Yetersiz bakiyeyi açık şekilde ele alın.
- İlgisiz UI'ı değiştirmek yerine, uygulanabildiği yerde mevcut Shop navigasyonunu/yerleşimini kullanın.

### Assetler

```text
Shop Arka Planı:
[ASSET YOLU]

Shop Ürün Kartı:
[ASSET YOLU]

Freeze İkonu:
[ASSET YOLU]

Instant Barrier İkonu:
[ASSET YOLU]

Gravity Well İkonu:
[ASSET YOLU]

Coin İkonu:
[ASSET YOLU]

Satın Al Butonu:
[ASSET YOLU]
```

---

# KİLOMETRE TAŞI A — DUR VE TEST ET

Görevler 01–10 tamamlandıktan sonra özellik genişletmeyi durdurun ve ekonomi döngüsünü değerlendirin:

`Oyna → Coin Kazan → Coin Harca → Toparlan / Güçlendirme Satın Al → Tamamla → Ödül → Shop → Oyna`

Devam etmeden önce en az şunları değerlendirin:

- Bölüm başına kazanılan ortalama Coin.
- Bölüm/oturum başına harcanan ortalama Coin.
- Oyuncu bakiyesinin ilerleyişi.
- Güçlendirme kullanım oranı.
- Canlandırma kullanım oranı.
- Ödüllü reklam tercih oranı.
- Oyuncuların kalıcı olarak Coin sıkıntısına düşüp düşmediği.
- Oyuncular çok fazla biriktirdiği için Coin'in anlamsızlaşıp anlamsızlaşmadığı.
- Bölüm zorluğunun toparlanma satmak için manipüle ediliyormuş gibi hissettirip hissettirmediği.

Playtest verisi mevcutsa sonraki sistemleri varsayımlara göre dengelemeyin.

---

# AŞAMA 5 — TEKRAR OYNANABİLİRLİK

## GÖREV 11 — Bölüm Yıldız Derecelendirmesi

**Görev Kimliği:** `11_LEVEL_STAR_RATING`

### Amaç

Tekrar oynama motivasyonu oluşturmak için bölümlere kalıcı 1–3 yıldız performans derecelendirmesi eklemek.

### Aday Koşullar

```text
Yıldız 1: Bölümü Tamamla
Yıldız 2: Can Kaybetme
Yıldız 3: Yapılandırılmış Kesim Eşiğinin Altında Tamamla
```

Nihai koşullar gerçek bölüm verilerine ve mekaniklere uygun olmalıdır.

### Gereksinimler

- Bölüm başına en iyi sonucu kalıcılaştırın.
- Tekrar oynamak daha önce kazanılmış yıldızları azaltamaz.
- Yıldızları ilgili bölüm/sonuç UI'ında gösterin.

### Assetler

```text
Boş Yıldız:
[ASSET YOLU]

Dolu Yıldız:
[ASSET YOLU]

Yıldız Açılma Animasyonu:
[ASSET YOLU]

Yıldız SFX:
[ASSET YOLU]
Kullanım: Yeni kazanılan bir yıldız görünür şekilde dolduğunda/açıldığında çalın. Birden fazla yıldız sırayla açılıyorsa tüm örnekleri aynı anda bindirmek yerine aynı SFX'in animasyonla doğal şekilde adım adım çalmasına izin verin.
```

---

## GÖREV 12 — Yıldız Ödül Ekonomisi

**Görev Kimliği:** `12_STAR_REWARD_ECONOMY`  
**Bağımlılık:** Görevler 01, 11

### Amaç

Tekrarlanabilir ödül açıkları oluşturmadan yıldız performansını Coin ödüllerine bağlamak.

### Aday Model

```text
1 Yıldız: normal ödül
2 Yıldız: +25 Coin
3 Yıldız: +50 Coin
```

Açıkça farklı tasarlanmadığı sürece sınırsız farm yapılmasına izin vermek yerine yeni kazanılan/en iyi performansı ödüllendirmeyi tercih edin.

### Assetler

```text
Yıldız Ödül İkonu:
[ASSET YOLU]

Bonus Coin Animasyonu:
[ASSET YOLU]
```

---

# AŞAMA 6 — OYUNCUYU TUTMA

## GÖREV 13 — Günlük Ödüller

**Görev Kimliği:** `13_DAILY_REWARDS`

### İlk Ödül Tablosu

```text
Gün 1: 100 Coin
Gün 2: 1x Freeze Pulse
Gün 3: 200 Coin
Gün 4: 1x Instant Barrier
Gün 5: 300 Coin
Gün 6: 1x Gravity Well
Gün 7: Özel Ödül [TANIMLA]
```

### Gereksinimler

- Ödül tablosu yapılandırılabilir olmalıdır.
- Projede mevcut olan uygun bir yetkili/zaman stratejisini kullanarak saat/yeniden yükleme kaynaklı yinelenen talepleri önleyin.
- Henüz belirtilmediyse uygulamadan önce serinin sıfırlanma/devam davranışını tanımlayın.

### Assetler

```text
Günlük Ödül Arka Planı:
[ASSET YOLU]

Aktif Gün:
[ASSET YOLU]

Alınmış Gün:
[ASSET YOLU]

Hediye Kutusu:
[ASSET YOLU]

Takvim İkonu:
[ASSET YOLU]

Alma SFX:
[ASSET YOLU]
Kullanım: Günlük ödül alma işlemi başarıyla doğrulanıp ödül verildiğinde bir kez çalın. Daha önce alınmış günlerde veya başarısız/yinelenen alma girişimlerinde çalmayın.
```

---

## GÖREV 14 — Günlük Görev / Günlük Keşif

**Görev Kimliği:** `14_DAILY_CHALLENGE`

### Amaç

Mümkün olduğunda mevcut oynanış/simge yapı altyapısını kullanarak dönüşümlü günlük görev sağlamak.

### Örnek

```text
GÜNLÜK KEŞİF
1 Can
8 Kesim
Güçlendirme Yok
Ödül: 500 Coin
```

### Gereksinimler

- Kaçınılabildiği yerlerde ikinci bir oyun modu mimarisi oluşturmak yerine mevcut oynanış sistemlerini yeniden kullanın.
- Görev kuralları ve ödül veri odaklı olmalıdır.
- Günlük tamamlama ödülü tekrar tekrar alınamamalıdır.

### Assetler

```text
Günlük Görev İkonu:
[ASSET YOLU]

Günlük Görev Kartı:
[ASSET YOLU]

Zamanlayıcı İkonu:
[ASSET YOLU]

Görev Tamamlandı Rozeti:
[ASSET YOLU]
```

---

# AŞAMA 7 — GEÇİŞ REKLAMLARI

## GÖREV 15 — Geçiş Reklamları

**Görev Kimliği:** `15_INTERSTITIAL_ADS`  
**Bağımlılık:** seçilen reklam SDK'sı/entegrasyonu

### Amaç

Aktif oynanışı kesintiye uğratmadan kontrollü geçiş reklamı para kazanması eklemek.

### Başlangıç Yapılandırması

```text
MinimumLevelsBetweenAds: 3
MinimumSecondsBetweenAds: 180
```

### Kurallar

- Aktif oynanış/kesim sırasında asla göstermeyin.
- Yalnızca bölüm tamamlandıktan sonra sonraki bölüme geçiş gibi doğal geçişlerde gösterin.
- Sıklık kontrolleri yapılandırılabilir olmalıdır.
- Görev 16 mevcut olduğunda Remove Ads sahipliğine uyun.
- Kaçınılabiliyorsa ödüllü reklamdan hemen sonra geçiş reklamı göstermeyin; makul bastırma/bekleme süresi davranışı uygulayın.

### Assetler

```text
Mevcut UI bir reklam geçiş/yükleme durumu gerektirmediği sürece özel asset gerekmez.
```

### Harici Kurulum

```text
Reklam Sağlayıcısı / SDK:
[AD]

Geçiş Reklamı Birim Kimliği — Android:
[DEĞER]

Geçiş Reklamı Birim Kimliği — iOS:
[DEĞER]
```

---

# AŞAMA 8 — UYGULAMA İÇİ SATIN ALMALAR

## GÖREV 16 — IAP: Reklamları Kaldır

**Görev Kimliği:** `16_IAP_REMOVE_ADS`

### Amaç

Tüketilmeyen bir Remove Ads satın alımı satmak.

### Gereksinimler

- Zorunlu/geçiş reklamlarını kaldırır.
- Ürün tasarımı değişmediği sürece isteğe bağlı ödüllü reklamlar kullanılabilir kalır.
- Hak sahipliğini uygun şekilde kalıcılaştırın.
- Platformun gerektirdiği/izin verdiği yerde satın alma geri yüklemeyi destekleyin.
- Mevcut IAP mimarisi hak geri yükleme/doğrulama destekliyorsa yalnızca yerel bir boolean'a güvenmeyin.

### Assetler

```text
Remove Ads İkonu:
[ASSET YOLU]

Remove Ads Shop Kartı:
[ASSET YOLU]

Satın Alma Başarılı İkonu / Animasyonu:
[ASSET YOLU]
```

### Mağaza Yapılandırması

```text
Android Ürün Kimliği:
[DEĞER]

iOS Ürün Kimliği:
[DEĞER]
```

---

## GÖREV 17 — IAP: Coin Paketleri

**Görev Kimliği:** `17_IAP_COIN_PACKS`

### Amaç

Gerçek para karşılığında tüketilebilir Coin paketleri satmak.

### Paket Yapılandırması

```text
Paket 1 Coin: [DEĞER]
Android Ürün Kimliği: [DEĞER]
iOS Ürün Kimliği: [DEĞER]

Paket 2 Coin: [DEĞER]
Android Ürün Kimliği: [DEĞER]
iOS Ürün Kimliği: [DEĞER]

Paket 3 Coin: [DEĞER]
Android Ürün Kimliği: [DEĞER]
iOS Ürün Kimliği: [DEĞER]

Paket 4 Coin: [DEĞER]
Android Ürün Kimliği: [DEĞER]
iOS Ürün Kimliği: [DEĞER]
```

### Gereksinimler

- Coin yalnızca başarılı satın alma doğrulandıktan sonra verilmelidir.
- Bekleyen/iptal edilen/başarısız işlemleri ele alın.
- Yinelenen teslimatı önleyin.
- Desteklendiğinde sabit kodlanmış görüntü fiyatları yerine platform/mağaza fiyatlarını kullanın.

### Assetler

```text
Küçük Coin Paketi:
[ASSET YOLU]

Orta Coin Paketi:
[ASSET YOLU]

Büyük Coin Paketi:
[ASSET YOLU]

Çok Büyük Coin Paketi:
[ASSET YOLU]

Satın Alma SFX:
[ASSET YOLU]
Kullanım: Yalnızca mağaza başarılı IAP işlemini onayladıktan ve Coin teslimatı tamamlandıktan sonra çalın. Bekleyen, iptal edilmiş, başarısız veya yinelenen işlemlerde asla çalmayın.
```

---

## GÖREV 18 — IAP: Başlangıç / Kâşif Paketi

**Görev Kimliği:** `18_IAP_STARTER_PACK`

### Amaç

Oyuncunun ilk satın alımını teşvik etmeyi amaçlayan, tek seferlik değer paketi oluşturmak.

### Aday İçerikler

```text
1000 Coin
3x Freeze Pulse
3x Instant Barrier
3x Gravity Well
1x Özel Kozmetik [TANIMLA]
```

### Gereksinimler

- Paket içerikleri yapılandırılabilir olmalıdır.
- Tek seferlik uygunluk/sahiplik zorunlu kılınmalıdır.
- Teslimat, proje/mağaza mimarisinin izin verdiği ölçüde atomik/idempotent olmalıdır.

### Assetler

```text
Kâşif Paketi Görseli:
[ASSET YOLU]

Kâşif Paketi Shop Kartı:
[ASSET YOLU]

Özel Kozmetik:
[ASSET YOLU]

Paket İkonları:
[ASSET YOLU]
```

### Mağaza Yapılandırması

```text
Android Ürün Kimliği:
[DEĞER]

iOS Ürün Kimliği:
[DEĞER]
```

---

# AŞAMA 9 — KOZMETİKLER

## GÖREV 19 — Bariyer Kozmetikleri

**Görev Kimliği:** `19_BARRIER_COSMETICS`

### Amaç

Oynanış avantajı sağlamayan kozmetik bariyer stilleri eklemek.

### Aday Stiller

```text
Klasik
Altın
Neon
Elektrik
Gökkuşağı
Kozmik
```

### Gereksinimler

- Sahip olunan/kullanılan durum kalıcı olmalıdır.
- Yalnızca kozmetik: çarpışma, zamanlama, boyutlar, tamamlanma hızı ve oynanış mantığı değişmeden kalmalıdır.
- Açılma kaynağı ileride Coin, başarımlar, koleksiyon ödülleri veya premium ürünleri destekleyebilir.

### Assetler

```text
Klasik Bariyer:
[ASSET YOLU]

Altın Bariyer:
[ASSET YOLU]

Neon Bariyer:
[ASSET YOLU]

Elektrik Bariyer:
[ASSET YOLU]

Gökkuşağı Bariyer:
[ASSET YOLU]

Kozmik Bariyer:
[ASSET YOLU]

Bariyer Shop İkonları:
[ASSET YOLU]
```

---

## GÖREV 20 — Kum / Tahta Kozmetikleri

**Görev Kimliği:** `20_BOARD_COSMETICS`

### Amaç

Oynanış etkileri olmadan kum/tahta ortamının kozmetik olarak özelleştirilmesini sağlamak.

### Aday Temalar

```text
Klasik Kum
Sahra
Volkanik
Arktik
Sakura
Kozmik
Kristal
```

### Gereksinimler

- Yalnızca kozmetik olmalıdır.
- Sahip olunan/kullanılan durum kalıcı olmalıdır.
- Simge yapı okunabilirliğini ve oynanış netliğini koruyun.

### Assetler

```text
Klasik Kum:
[ASSET YOLU]

Sahra:
[ASSET YOLU]

Volkanik:
[ASSET YOLU]

Arktik:
[ASSET YOLU]

Sakura:
[ASSET YOLU]

Kozmik:
[ASSET YOLU]

Kristal:
[ASSET YOLU]

Shop Önizleme Arka Planı:
[ASSET YOLU]
```

---

# AŞAMA 10 — KOLEKSİYON METASI

## GÖREV 21 — Simge Yapı Koleksiyonu

**Görev Kimliği:** `21_LANDMARK_COLLECTION`

### Amaç

Keşfedilen simge yapılar ve bunların tamamlanma/yıldız durumu için koleksiyon görünümü oluşturmak.

### Gereksinimler

- Doğruluk kaynağı olarak mevcut simge yapı ilerleme verisini kullanın.
- Keşfedilmiş ve kilitli/keşfedilmemiş girdileri gösterin.
- Görev 11 mevcut olduğunda yıldız/en iyi performans bilgisini gösterin.
- Mevcut içerik verisiyle uyumlu olduğunda bölüm/bölge gruplamasını destekleyin.

### Assetler

```text
Koleksiyon Arka Planı:
[ASSET YOLU]

Kilitli Simge Yapı Kartı:
[ASSET YOLU]

Açılmış Simge Yapı Kartı:
[ASSET YOLU]

Bölge / Bölüm İkonları:
[ASSET YOLU]

Kilit İkonu:
[ASSET YOLU]

Koleksiyon Tamamlandı Rozeti:
[ASSET YOLU]
```

---

## GÖREV 22 — Koleksiyon Ödülleri

**Görev Kimliği:** `22_COLLECTION_REWARDS`  
**Bağımlılık:** Görev 21 ve ilgili ödül sistemleri

### Amaç

Anlamlı koleksiyon kilometre taşlarını ödüllendirmek.

### Aday Ödüller

```text
5 Simge Yapı Keşfet → 500 Coin
Bölümü Tamamla → Altın Bariyer
Bölümdeki Tüm Seviyeleri 3 Yıldızla Tamamla → Özel Kum Teması
```

### Gereksinimler

- Açıkça farklı yapılandırılmadığı sürece kilometre taşı ödülleri yalnızca bir kez alınabilir.
- Alınmış durumu kalıcılaştırın.
- Ödül tanımları yapılandırılabilir/veri odaklı olmalıdır.

### Assetler

```text
Koleksiyon Ödül Sandığı:
[ASSET YOLU]

Bölüm Tamamlandı Rozeti:
[ASSET YOLU]

Özel Ödül Assetleri:
[ASSET YOLU]
```

---

# ANA UYGULAMA SIRASI

```text
AŞAMA 1 — TEMEL EKONOMİ
[ ] 01_CORE_COIN_SYSTEM
[ ] 02_LEVEL_COIN_REWARD
[ ] 03_PERFORMANCE_REWARDS

AŞAMA 2 — EKONOMİ HARCAMA ALANLARI
[ ] 04_POWERUP_INVENTORY_ECONOMY
[ ] 05_COIN_EXTRA_LIFE
[ ] 06_COIN_EXTRA_CUT

AŞAMA 3 — ÖDÜLLÜ REKLAMLAR
[ ] 07_REWARDED_AD_EXTRA_LIFE
[ ] 08_REWARDED_AD_EXTRA_CUT
[ ] 09_REWARDED_DOUBLE_COINS

AŞAMA 4 — SHOP
[ ] 10_SHOP_POWERUPS

=== KİLOMETRE TAŞI A: DUR VE EKONOMİYİ PLAYTEST ET ===

AŞAMA 5 — TEKRAR OYNANABİLİRLİK
[ ] 11_LEVEL_STAR_RATING
[ ] 12_STAR_REWARD_ECONOMY

AŞAMA 6 — OYUNCUYU TUTMA
[ ] 13_DAILY_REWARDS
[ ] 14_DAILY_CHALLENGE

AŞAMA 7 — REKLAMLAR
[ ] 15_INTERSTITIAL_ADS

AŞAMA 8 — IAP
[ ] 16_IAP_REMOVE_ADS
[ ] 17_IAP_COIN_PACKS
[ ] 18_IAP_STARTER_PACK

AŞAMA 9 — KOZMETİKLER
[ ] 19_BARRIER_COSMETICS
[ ] 20_BOARD_COSMETICS

AŞAMA 10 — META / KOLEKSİYON
[ ] 21_LANDMARK_COLLECTION
[ ] 22_COLLECTION_REWARDS
```

---

# Claude Code Uygulama Sözleşmesi

Geliştirici **"Task XX'i uygula"** dediğinde aşağıdaki süreci izleyin:

### 1. Kapsam

Bu belgede `GÖREV XX` (`TASK XX`) bölümünü bulun. Talep edilen kapsam o görevdir. Mimari farkındalık için bağımlılıklarını ve ilgili gelecek görevleri okuyun, ancak gelecek görevleri uygulamayın.

### 2. Önce İncele

Düzenleme yapmadan önce:

- proje yapısını inceleyin;
- ilgili veri/oynanış/UI'ın sahibi olan mevcut sistemleri belirleyin;
- mevcut kayıt/bulut kayıt kurallarını inceleyin;
- ilgili yapılandırma/veri tanımlarını inceleyin;
- yeniden kullanılabilir mevcut bileşenleri inceleyin;
- seçilen göreve girilmiş asset yollarını inceleyin.

### 3. Mevcut Mimariye Göre Planla

Körü körüne yeni manager/servis/singleton oluşturmayın. Projenin mevcut kalıplarını tercih edin. Yol haritasındaki terminoloji gerçek sınıf/dosya adlarından farklıysa yol haritasındaki adları zorlamak yerine projeye uyarlayın.

### 4. Assetler

Yalnızca seçilen görevin sağlanan asset yollarını ve açıkça uygun olan mevcut ortak assetleri kullanın.

Gerekli bir asset yolu hâlâ `[ASSET YOLU]` ise ve uygulama gerçekten bu asseti gerektiriyorsa ilgili kısmı durdurun ve tam olarak hangi assetin eksik olduğunu bildirin. Bir dosya yolu uydurmayın.

### 5. Uygulama Sınırları

- Seçilen görevi tamamen uygulayın.
- Gerçekten gerekliyse minimum, bağımlılık güvenli refactor yapın.
- "Hazır buradayken" sonraki yol haritası özelliklerini uygulamayın.
- Para kazanmayı artırmak için oynanış zorluğunu değiştirmeyin.
- Açıkça istenmediği sürece mevcut işlevleri kaldırmayın.

### 6. Doğrulama

Mümkün olduğunda şunları doğrulayın:

- derleme/build doğruluğu;
- kalıcılık davranışı;
- yinelenen olay koruması;
- yetersiz kaynak davranışı;
- sahne/yeniden yükleme davranışı;
- UI durum güncellemeleri;
- ilgili olduğunda platforma özel hata yolları.

### 7. Tamamlama Raporu

Sonunda aşağıdakileri içeren kısa bir uygulama raporu verin:

```text
TAMAMLANAN GÖREV:
[Görev Kimliği]

OLUŞTURULAN DOSYALAR:
- ...

DEĞİŞTİRİLEN DOSYALAR:
- ...

YAPILANDIRMA / DENGE DEĞERLERİ:
- ...

KULLANILAN ASSETLER:
- ...

KAYIT / KALICILIK DEĞİŞİKLİKLERİ:
- ...

GEREKLİ MANUEL KURULUM:
- ...

YAPILAN TESTLER / DOĞRULAMALAR:
- ...

UYGULANMAYANLAR (gelecek yol haritası):
- ...
```

Uygulama engellendiyse görev tamamlanmış gibi davranmayın. Engeli ve gereken tam bilgi/asset/yapılandırmayı belirtin.
