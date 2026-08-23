# Audio Assets

## Amaç

Bu doküman, Cutrium'da ihtiyaç duyulan **her** ses dosyasını isimlendirmesi,
açıklamasıyla ve her biri için hazır bir **ElevenLabs prompt'uyla** listeler.
Kod tarafındaki bağlantı noktaları (Inspector alanları) zaten hazır; sen
sadece ElevenLabs'ta üretilen (veya kendi hazırladığın) ses dosyalarını bu
isimlerle kaydedip ilgili Inspector alanına sürükleyeceksin. Hiçbir alan
zorunlu değildir — boş bırakılan bir alan sessizce atlanır, oyun hata
vermez.

Özellikle **Bariyer / Kesim** ve **Alan Ele Geçirme & Ödül** bölümlerindeki
sesler oyunun çekirdek hissini oluşturuyor; en çok özeni onlara ayırmanı
öneririm.

## Bu Turda Değişen Davranışlar

- **`SFX_BarrierGrow` artık döngülü (loop) çalıyor.** Eskiden bariyer
  büyümeye başladığında bir kez çalıp bitiyordu; şimdi bariyer kilitlenene
  (`SFX_BarrierLock`) ya da kırılana (`SFX_BarrierBreak`) kadar **sürekli
  loop** ediyor. **Bu yüzden dosyanın kendisi sorunsuz döngülenebilir
  (seamless loop) olmalı** — başında/sonunda ani kesme/click olmayan bir
  devam sesi. Mevcut dosya loop için uygun değilse yeniden üretmen gerekir.
- **`SFX_PowerGravityWellActivate` de artık döngülü çalıyor.** Gravity Well
  gücü ne kadar sürerse (varsayılan birkaç saniye), ses de o kadar sürüyor;
  güç bitince otomatik duruyor. Bu dosya da **seamless loop** olmalı.
- **Bariyer/Gravity loop sesleri bir ara "Mute" kutusu işaretli kaldığı için
  hiç duyulmuyordu** — `Cutrium/Setup/Apply Audio Clips` artık bu iki loop
  kaynağının Mute'unu her çalıştırmada zorla kapatıyor, ayrıca çalma anında
  da (`PlayLoop`) tekrar zorluyor; Validate bundan sonra Mute açık kalırsa
  hata fırlatır.
- **`SFX_RegionCaptured` (Fill Clip / `SFX_SandFill`) her başarılı ele
  geçirmede sabit (pitch'lenmemiş) olarak çalmaya devam ediyor** — kesimin
  "gerçekleştiğini" onaylayan taban ses.
- **`SFX_ComboChanged` bu sabit sesin üzerine, ek bir katman olarak,
  komboda yükseldikçe tizleşerek çalıyor** (sadece pozitif kombo — sıfırlama
  anında bir şey çalmıyor, bkz. aşağıdaki `SFX_ComboChanged` bölümü). Pitch
  adımı artık `+0.15`/basamak, tavan `2.5×` — eskiden `+0.06`/`1.6×` idi ve
  fark neredeyse duyulmuyordu.
- **Kombo artık zaman aşımına uğrayabiliyor.** Bir bariyer açık değilken
  belirli bir süre (varsayılan 3 saniye, `FeedbackTuning.asset` üzerinden
  ayarlanabilir) yeni bir başarılı kesim gelmezse kombo sessizce sıfırlanır
  (herhangi bir ses çalmaz — bariyer kırılmasıyla aynı "sessiz sıfırlama"
  mantığı). Bariyer aktifken (çizim sürerken) bu sayaç durur, sadece
  boştayken işler.
- **Instant Barrier'da çifte ses kalktı.** Instant Barrier gücüyle açılan
  bir kesimde artık sadece `SFX_PowerInstantBarrierConsume` çalıyor;
  genel `SFX_BarrierStart` o kesim için atlanıyor (aynı anda ikisi
  çalmıyor). `SFX_BarrierGrow` loop'u ise instant kesimlerde de olduğu gibi
  çalışmaya devam ediyor (çok hızlı büyüdüğü için zaten kısa sürüyor).
- **YENİ: Seviye başlangıcındaki "LEVEL N / TARGET %X / N CUT / tanıtım
  kartı" yazı sekansına 5 yeni ses eklendi** (bkz. aşağıdaki
  `8. Seviye Başlangıcı` bölümü) — `PreLevelIntroPresenter` artık her metin
  belirdiğinde ve Target/Cut sayıları HUD'a "uçarak indiğinde" ayrı bir
  `AudioSource` üzerinden ses çalıyor. Bu 5 alan da opsiyonel/boş
  bırakılabilir.
- **Düzeltildi: ana menü açıkken arka planda ses geliyordu.**
  `FirstPlayableController.Awake()` oyun tahtasını menünün arkasında hazır
  tutmak için seviyeyi baştan yüklüyor; `PreLevelIntroPresenter` bunu
  "yeni seviye başladı" sanıp menü hâlâ görünürken sekansı (ve artık sesini)
  hemen başlatıyordu. Artık ana menü açıkken (`SimulationHoldReason.FrontEnd`
  tutulduğu sürece) bu sekans hiç başlamıyor/ilerlemiyor; oyuncu gerçekten
  Play'e bastığında doğru anda baştan başlıyor.
- **Düzeltildi: iniş sesi ile bir sonraki kartın sesi üst üste binip
  birbirini maskeliyordu** (özellikle Info/landmark kartında fark
  edilmiyordu) — bkz. yukarıdaki `SFX_IntroFlightLand` notu.
- **YENİ: Seviye tamamlama popup'ının fotoğraf girişine ses eklendi**
  (`SFX_LandmarkPhotoReveal`, bkz. aşağıdaki `9. Seviye Tamamlama Popup'ı`
  bölümü) — `LandmarkRevealPresenter` popup açılmaya başladığı anda,
  kendi ayrı `AudioSource`'u üzerinden çalıyor.

## Nasıl Çalışıyor (kod tarafı)

- Neredeyse tüm oyun-içi sesler tek bir olay akışından besleniyor:
  `Cutrium.Gameplay.Feedback.FeedbackEventKind` → `FeedbackAudioPresenter`
  (`Assets/Cutrium/Runtime/Presentation/Feedback/FeedbackAudioPresenter.cs`).
  Bu bileşen sahnede **`VerticalSliceRoot/FeedbackServices`** objesinin
  üzerinde duruyor. Aşağıdaki listede geçen "Inspector alanı" isimleri bu
  bileşenin üzerindeki `AudioClip` alanlarının Unity Inspector'da göründüğü
  isimlerdir.
- Arka plan müziği ayrı bir mekanizma: **`VerticalSliceRoot/FeedbackServices/
  MusicSource`** objesindeki `AudioSource` bileşeninin kendi `AudioClip`
  alanı. Loop ve Play On Awake zaten açık geliyor; sadece klibi sürüklemen
  yeterli.
- Seviye başlangıcı yazı sekansı da ayrı bir mekanizma: **`PreLevelIntroPresenter`**
  (`Assets/Cutrium/Runtime/Presentation/HUD/PreLevelIntroPresenter.cs`),
  sahnede kendi **`PreLevelIntroAudioSource`** objesi üzerinden çalıyor —
  `FeedbackAudioPresenter`'dan bağımsız, ayrı bir `AudioClip` alanları
  seti var.
- Seviye tamamlama popup'ı da ayrı bir mekanizma: **`LandmarkRevealPresenter`**
  (`Assets/Cutrium/Runtime/Presentation/Landmark/LandmarkRevealPresenter.cs`),
  sahnede kendi **`LandmarkRevealAudioSource`** objesi üzerinden çalıyor.
- Ayarlar panelindeki Ses/Müzik/Titreşim aç-kapa düğmeleri bu dört kaynağı
  da (FeedbackAudioPresenter, MusicSource, PreLevelIntroAudioSource,
  LandmarkRevealAudioSource) otomatik olarak susturup açıyor — ekstra bir
  şey yapmana gerek yok.
- `Cutrium/Setup/Apply Settings Panel` menü komutu, `MusicSource` objesini ve
  tüm referansları idempotent şekilde (tekrar tekrar çalıştırılabilir, obje
  çoğaltmaz) oluşturup doğruluyor. Ses dosyalarını ekleyip Inspector'dan
  sürüklemen için bu komutu tekrar çalıştırman gerekmiyor — doğrudan sahnede
  ilgili objeyi seçip alana sürükleyebilirsin.

## ElevenLabs Kullanımı Hakkında Notlar

- Kısa efektler (SFX) için **ElevenLabs Sound Effects** aracını kullan
  (elevenlabs.io → Sound Effects). Prompt'u İngilizce yazmak en tutarlı
  sonucu veriyor, bu yüzden aşağıdaki prompt'lar İngilizce hazırlandı —
  doğrudan kopyala-yapıştır kullanabilirsin.
- Her prompt'un yanında önerilen **Süre** (Duration) değeri var; ElevenLabs
  arayüzünde bu değeri saniye olarak elle girebilirsin. Otomatik bırakmak da
  sorun değil, sadece sonuç biraz daha uzun/kısa çıkabilir.
- Sonuç istediğin gibi çıkmazsa "Prompt Influence" değerini yükseltmek
  prompt'a daha sıkı bağlı kalmasını sağlar; daha "yaratıcı"/varyasyonlu
  sonuç istersen düşür.
- Arka plan müziği (`SFX_Music`) için ElevenLabs'ın **Music** aracı (varsa
  hesabında) daha uygun — kısa döngü değil, tam bir müzik parçası üretir.
  Yalnızca Sound Effects aracına erişimin varsa, aşağıdaki prompt'la ~20-30
  saniyelik bir döngü segmenti üretip Unity'de `Loop` açık şekilde
  kullanabilirsin.
- Oyunun genel tonu: sıcak, sandy/toprak tonları, dünya çapında ünlü
  landmark'ları keşfeden rahat ama tatmin edici bir mobil bulmaca oyunu.
  Sesler bu sıcak/keyifli tona uymalı — çok agresif, karanlık ya da gerçekçi
  silah/patlama tarzı efektlerden kaçın.

## Öneri: Dosya Yerleşimi ve Format

- Kaynak dosyaları `Assets/Cutrium/Content/Audio/` altında toplaman öneri
  (klasör yoksa oluşturabilirsin; kod bu yola bağımlı değil, sadece
  organizasyon için).
- Kısa vuruşlu efektler (SFX): mono, `Decompress On Load`, gerekirse
  `PCM`/`ADPCM`.
- Döngülü müzik: stereo, `Compressed In Memory`, `Vorbis`, sorunsuz loop için
  dosyanın başında/sonunda sessizlik olmamalı.
- Hedef platform mobil olduğundan dosya boyutlarını makul tutmak (~kısa
  efektler için birkaç yüz KB, müzik için birkaç MB) yeterli.

---

## 1. Müzik

### SFX_Music — Loop — `FeedbackServices/MusicSource` → `AudioSource` → **Audio Clip**

Oyun boyunca (menüde ve seviye içinde) çalan genel arka plan müziği. Şu an
tek ve sürekli bir parça olarak tasarlandı; menü/oyun için ayrı parçalar
istersen ayrıca söyle, o zaman ek bir geçiş mekanizması kurarız.

> **ElevenLabs Prompt:** Warm, cozy, adventurous background music loop for a
> mobile puzzle game about exploring famous world landmarks. Light
> orchestral instruments blended with soft electronic textures, gentle
> plucked strings, subtle world-travel percussion, curious and playful mood,
> mid tempo, seamless loop, no vocals, no harsh drums, calm but engaging
> throughout.
>
> **Süre:** 20–30 sn (loop segmenti) veya Music aracıyla tam parça.

## 2. Genel Arayüz (UI)

### SFX_Button — One-shot — `FeedbackAudioPresenter` → **Ui Clip**

Uygulamadaki **her** buton için ortak tıklama sesi: alt menü sekmeleri (Shop/
Home/Challenge), seviye haritasındaki node'lar, Ayarlar dişlisi, Ayarlar
panelindeki Ses/Müzik/Titreşim/Dil/Home/Exit düğmeleri, Retry düğmesi, Play
düğmeleri. Bunların hepsi zaten kodda bu tek olaya bağlı; ayrı ayrı
bağlamana gerek yok.

> **ElevenLabs Prompt:** Short, crisp UI click sound for a mobile game
> button. Soft pop layered with a light digital tick, clean and satisfying,
> minimal, high frequency, no reverb, very short and punchy.
>
> **Süre:** 0.2–0.3 sn.

## 3. Bariyer / Kesim (Oyunun Kalbi)

Oyuncunun parmağıyla/mouse'uyla çizdiği kesim çizgisiyle ilgili sesler.
Bunlar en sık duyulacak sesler olduğu için en fazla özenin burada olmasını
öneririm.

### SFX_BarrierStart — One-shot — `FeedbackAudioPresenter` → **Start Clip**

Oyuncu parmağını bırakıp yeni bir bariyer (kesim çizgisi) çizmeye başladığı
an çalar. **İstisna:** kesim, hazırlanmış bir Instant Barrier gücüyle
başladıysa bu ses **çalmıyor** — o an zaten `SFX_PowerInstantBarrierConsume`
çaldığı için ikisi üst üste binmesin diye bilerek atlanıyor.

> **ElevenLabs Prompt:** Short energetic whoosh sound for starting a fast
> line-drawing action in a mobile puzzle game. Light synth swipe with a
> subtle spark at the tail, quick attack, bright and clean, no low-end
> rumble, no metallic clank.
>
> **Süre:** 0.3–0.5 sn.

### SFX_BarrierGrow — **Loop** — `FeedbackAudioPresenter` → **Grow Clip**

Bariyer büyümeye başladığında çalmaya başlar ve **bariyer kilitlenene
(`SFX_BarrierLock`) ya da kırılana (`SFX_BarrierBreak`) kadar sürekli
loop eder** — ayrı bir `AudioSource` (`FeedbackServices/
BarrierGrowLoopSource`) üzerinden çalınıyor, diğer seslerle karışmaz.
**Bu dosyanın baştan sona sorunsuz döngülenebilir (seamless loop) olması
gerekiyor** — kısa bir "başlangıç" vuruşu değil, devam eden bir doku/hum
sesi olmalı.

> **ElevenLabs Prompt:** Seamless looping energetic hum representing a
> barrier line continuously extending outward in a mobile puzzle game.
> Smooth sustained synth tone with light tension, no strong transient at
> the start or end so it can loop cleanly, clean and airy, no metallic or
> mechanical texture.
>
> **Süre:** 0.6–1.5 sn (loop segmenti; ne kadar sorunsuz döngülenirse o
> kadar iyi).

### SFX_BarrierLock — One-shot — `FeedbackAudioPresenter` → **Lock Clip**

Bariyer karşı kenara ulaşıp kilitlendiğinde, oda ikiye bölündüğünde çalar.
Kesimin "başarıyla tamamlandı" anı.

> **ElevenLabs Prompt:** Satisfying mechanical lock sound for a mobile
> puzzle game. Light metallic snap combined with a soft resonant thud,
> confirms a line sealing shut, crisp, punchy, and short, slightly warm
> tone rather than cold/industrial.
>
> **Süre:** 0.3–0.4 sn.

### SFX_BarrierBreak — One-shot — `FeedbackAudioPresenter` → **Break Clip**

Tamamlanmamış bir bariyer bir tehditle çarpışıp kırıldığında çalar
(başarısız kesim / hata anı). Bir kombo sürerken bariyer kırılırsa kombo da
sıfırlanır — bu durumda **ayrıca bir "kombo bozuldu" sesi çalmıyoruz**,
tek başına bu ses yeterli kabul ediliyor (bkz. `SFX_ComboChanged`).

> **ElevenLabs Prompt:** Short glassy crack and shatter sound representing a
> failed line breaking apart in a mobile puzzle game. Brittle snap with a
> quick descending pitch, mildly negative but not harsh or violent, light
> and game-appropriate, not a realistic glass smash.
>
> **Süre:** 0.4–0.6 sn.

## 4. Alan Ele Geçirme & Ödül

### SFX_RegionCaptured — One-shot — `FeedbackAudioPresenter` → **Fill Clip**

Her başarılı ele geçirmede **sabit** (pitch değişmeden) çalar — kesimin
gerçekleştiğini onaylayan taban ses. Kombo hissi bu sesin üstüne, ayrı bir
katman olarak, `SFX_ComboChanged` ile veriliyor (bkz. aşağıda) — ikisi
birlikte, aynı anda çalar.

> **ElevenLabs Prompt:** Warm, satisfying fill/reveal sound representing an
> area being claimed in a mobile puzzle game. Soft rising chime layered
> with a gentle whoosh, magical and rewarding, light sparkle tail, warm
> tone rather than icy or metallic.
>
> **Süre:** 0.5–0.8 sn.

### SFX_SandFill — One-shot — `FeedbackAudioPresenter` → **Sand Fill Clip**

`SFX_RegionCaptured` ile **aynı anda**, ona ek olarak çalar — ele geçirilen
alanın kum ile "dolma" hissi veren ayrı bir doku sesi. `SFX_RegionCaptured`
kısa/net bir ödül vuruşu (chime) iken, bu ses daha çok bir "dökülme/dolma"
akışı (pour/fill texture) olmalı; ikisi üst üste bindiğinde bütünsel bir
"alan dolduruldu" hissi vermeli. Bağlandı:
`Assets/Cutrium/Content/Sounds/SFX_SandFill.wav`.

> **ElevenLabs Prompt:** Short sand or fine-grain pouring/filling sound for
> a mobile puzzle game, representing an area filling up with warm sand.
> Soft granular trickling texture with a gentle rising swell, cozy and
> tactile, no harsh noise, blends well underneath a bright reward chime.
>
> **Süre:** 0.6–1.0 sn.

### SFX_LargeCapture — One-shot — `FeedbackAudioPresenter` → **Large Capture Clip**

Tek seferde büyük bir alan ele geçirildiğinde, `SFX_ComboChanged`'a
**ek olarak** çalar — ekstra ödül hissi vermeli.

> **ElevenLabs Prompt:** Bigger, triumphant version of a reward chime for
> claiming a large area in a mobile puzzle game. Layered bell tones rising
> together, richer harmonic sparkle bloom, celebratory but still short and
> punchy, warm and bright.
>
> **Süre:** 0.6–1.0 sn.

### SFX_NearMiss — One-shot — `FeedbackAudioPresenter` → **Near Miss Clip**

Bir tehdit, oyuncunun bariyerine çok yaklaşıp kıl payı ıskaladığında çalar.
Gerilim + rahatlama hissi veren bir ses iyi olur.

> **ElevenLabs Prompt:** Short tense whoosh followed by a quick relief
> accent, representing a narrow escape in a mobile puzzle game. Fast
> air-swipe transitioning into a light positive ding, combining adrenaline
> and relief in one brief sound.
>
> **Süre:** 0.4–0.6 sn.

### SFX_ComboChanged — One-shot — `FeedbackAudioPresenter` → **Combo Clip**

**Sadece** art arda başarılı kilitlemelerle kombo sayacı arttığında çalar.
Kod artık her yeni kombo basamağında bu klibi biraz daha **tiz** (yüksek
perdeli) çalıyor — kombo 1 iken normal perde, sonraki her basamakta perde
kademeli olarak artıyor (üst sınırla sınırlı), böylece art arda gelen
başarılı kesimler kulakta yükselen bir "seri" hissi veriyor. Bariyer bir
tehditle çarpışıp kombo bozulduğunda **bu ses hiç çalmıyor** — o an zaten
`SFX_BarrierBreak` çalıyor, üzerine ayrı bir "kombo bozuldu" sesi
eklemiyoruz (ayrım kod tarafında yapıldı, sen sadece tek, yükselen bir
kombo sesi hazırlaman yeterli). Ayrıca kombo, bariyer açık değilken belirli
bir süre (varsayılan 3 sn) yeni kilitleme gelmezse **sessizce** de
sıfırlanabilir — o durumda da bu ses çalmaz.

> **ElevenLabs Prompt:** Short ascending musical chime/arpeggio
> representing a combo counter increasing in a mobile puzzle game. Bright
> bell-like tones stepping upward quickly, playful and rewarding, very
> short, no harsh transients. Should sound good pitched up slightly for
> higher combo streaks, so keep it a clean, simple tone rather than a
> complex chord.
>
> **Süre:** 0.3–0.5 sn.

### SFX_LevelComplete — One-shot — `FeedbackAudioPresenter` → **Complete Clip**

Seviye hedefi tamamlandığında çalar; hemen ardından landmark tanıtım kartı
(o yerin adı/açıklaması) açılır. Zafer/başarı hissi taşımalı.

> **ElevenLabs Prompt:** Short triumphant victory fanfare for completing a
> level in a warm, friendly mobile puzzle game. Rising melodic phrase with
> a light percussive hit at the end, celebratory and satisfying, no vocals,
> world-travel/adventure flavor rather than epic/orchestral bombast.
>
> **Süre:** 1.5–2 sn.

## 5. Tehditler (Threats)

### SFX_HunterReact — One-shot — `FeedbackAudioPresenter` → **Hunter React Clip**

**Hunter** tipi bir tehdit, oyuncunun yeni çizdiği bariyere tepki verip
yönünü değiştirdiğinde çalar — bir "fark edildin!" anı. Normal/pulse
tehditlerde bu ses hiç çalmaz, sadece hunter davranışına özel.

> **ElevenLabs Prompt:** Short alert stinger representing an enemy noticing
> the player in a mobile puzzle game. Quick tense synth stab with a subtle
> whoosh, slightly ominous but playful rather than scary, brief "spotted
> you" feel.
>
> **Süre:** 0.3–0.5 sn.

## 6. Başarısızlık / Oyun Sonu

### SFX_OutOfCuts — One-shot — `FeedbackAudioPresenter` → **Out Of Cuts Clip**

Seviyede izin verilen kesim (cut) hakkı bitip seviye başarısız olduğunda
çalar.

> **ElevenLabs Prompt:** Soft descending failure tone for running out of
> moves in a mobile puzzle game. Gentle low buzz with a falling pitch,
> disappointed but friendly, not harsh or alarming, short game-over cue.
>
> **Süre:** 0.6–0.8 sn.

### SFX_OutOfLives — One-shot — `FeedbackAudioPresenter` → **Out Of Lives Clip**

Seviyede izin verilen bariyer kırılma (can) hakkı bitip seviye başarısız
olduğunda çalar.

> **ElevenLabs Prompt:** Soft descending failure tone for running out of
> lives in a mobile puzzle game. Low muted thud with a falling pitch,
> slightly heavier and more final than a simple miss sound, still friendly
> and short.
>
> **Süre:** 0.6–0.8 sn.

İkisi de Retry ekranının açılma anıyla örtüşür; istersen ikisi için aynı
klibi de kullanabilirsin.

## 7. Güçler (Powers)

### SFX_PowerFreezeActivate — One-shot — `FeedbackAudioPresenter` → **Power Freeze Activate Clip**

Freeze Pulse gücü aktive edilip tehditler yavaşladığında/durduğunda çalar.

> **ElevenLabs Prompt:** Icy magical activation sound for a freeze/slow-time
> power-up in a mobile puzzle game. Crystalline chime with a cold whoosh
> and shimmering sparkle tail, bright and crisp, short and clean.
>
> **Süre:** 0.6–0.8 sn.

### SFX_PowerInstantBarrierArm — One-shot — `FeedbackAudioPresenter` → **Power Instant Barrier Arm Clip**

Instant Barrier gücü "hazır" durumuna geçtiğinde çalar (henüz bir kesimde
kullanılmadan, sadece hazırlanma anı).

> **ElevenLabs Prompt:** Quick charging power-up sound representing a
> special ability becoming ready in a mobile puzzle game. Rising synth
> pulse ending in a soft electronic beep, building anticipation, short and
> light.
>
> **Süre:** 0.3–0.5 sn.

### SFX_PowerInstantBarrierConsume — One-shot — `FeedbackAudioPresenter` → **Power Instant Barrier Consume Clip**

Hazırlanmış Instant Barrier, oyuncu yeni bir kesime başladığı anda harcanıp
o kesime anlık hız uygulandığında çalar. Bu kesimde genel `SFX_BarrierStart`
**çalmıyor** — o an "bariyer açıldı" hissini tek başına bu ses veriyor,
çiftlenmiyor.

> **ElevenLabs Prompt:** Fast energetic burst sound representing a charged
> power being unleashed instantly in a mobile puzzle game. Quick synth zap
> with a sharp transient and a light impact, powerful, snappy, and short.
>
> **Süre:** 0.3–0.5 sn.

### SFX_PowerGravityWellActivate — **Loop** — `FeedbackAudioPresenter` → **Power Gravity Well Activate Clip**

Gravity Well gücü bir tehdidi hedefleyip aktive edildiğinde çalmaya başlar
ve **güç etkisi bitene kadar sürekli loop eder** (ayrı bir `AudioSource`
üzerinden, `FeedbackServices/GravityWellLoopSource`), sonra otomatik durur.
**Bu dosya da seamless loop olmalı** — kısa bir "aktivasyon" vuruşu değil,
gücün süresi boyunca devam eden bir doku sesi.

> **ElevenLabs Prompt:** Seamless looping deep magical pull sound
> representing an active gravity vortex in a mobile puzzle game. Low
> sustained whoosh with an inward-sucking swirl, no strong transient at the
> start or end so it can loop cleanly, mysterious and powerful but not
> scary.
>
> **Süre:** 0.6–1.5 sn (loop segmenti).

### SFX_PowerUnavailable — One-shot — `FeedbackAudioPresenter` → **Power Unavailable Clip**

Oyuncu şarjı olmayan veya koşulları sağlamayan bir gücü kullanmaya
çalıştığında çalar — kısa, "olmadı" hissi veren bir red/hata sesi olmalı.

> **ElevenLabs Prompt:** Short muted denial sound representing an action
> that cannot be used right now in a mobile puzzle game. Soft low-pitched
> double blip, neutral and friendly rather than harsh, very short "not yet"
> feel.
>
> **Süre:** 0.2–0.4 sn.

## 8. Seviye Başlangıcı (Pre-Level Intro)

Her gerçekten yeni bir seviye başladığında (retry'lerde **çalmaz** — bkz.
`Bilerek Ses Eklenmeyen Durum`), oyun tahtası görünürken ortada sırayla
büyük yazılar beliriyor: **LEVEL N → TARGET %X → (varsa) N CUT → (varsa)
mekanik/landmark tanıtım kartı**. Bu 5 ses, `PreLevelIntroPresenter`
(`Assets/Cutrium/Runtime/Presentation/HUD/PreLevelIntroPresenter.cs`)
üzerinden, sahnedeki **`PreLevelIntroAudioSource`** adlı ayrı bir
`AudioSource` ile çalınıyor — mevcut oyun-içi seslerle karışmaz. Hepsi
**opsiyonel**: dosya eklenmemiş bir alan sessizce atlanır. Ayarlar
panelindeki Ses aç/kapa düğmesi bu kaynağı da otomatik susturup açıyor.

### SFX_LevelReveal — One-shot — `PreLevelIntroPresenter` → **Level Reveal Clip**

"LEVEL N" yazısı ekranın ortasında belirdiği anda çalar — sekansın ilk
adımı, her yeni seviyede bir kez.

> **ElevenLabs Prompt:** Short warm chime announcing a new level starting in
> a friendly mobile puzzle game. Gentle rising bell tone with a soft sparkle,
> welcoming and light, no drums, very short and clean.
>
> **Süre:** 0.3–0.5 sn.

### SFX_TargetReveal — One-shot — `PreLevelIntroPresenter` → **Target Reveal Clip**

"TARGET %X" yazısı belirdiği anda çalar (LEVEL yazısı kaybolduktan hemen
sonra).

> **ElevenLabs Prompt:** Short bright informative chime revealing a goal
> percentage in a mobile puzzle game. Soft ascending two-note tone, clear
> and crisp, slightly more energetic than a plain UI click, very short.
>
> **Süre:** 0.2–0.4 sn.

### SFX_CutReveal — One-shot — `PreLevelIntroPresenter` → **Cut Reveal Clip**

Seviyenin kesim sınırı varsa (`N CUT`/`N CUTS`) bu yazı belirdiği anda
çalar. Kesim sınırı olmayan seviyelerde bu aşama hiç yaşanmaz, dolayısıyla
ses de çalmaz.

> **ElevenLabs Prompt:** Short focused alert-like chime revealing a limited
> resource count in a mobile puzzle game. Slightly more tense/urgent than
> the target reveal, but still friendly and short, quick double-tone,
> no harsh or negative tone.
>
> **Süre:** 0.2–0.4 sn.

### SFX_InfoReveal — One-shot — `PreLevelIntroPresenter` → **Info Reveal Clip**

Seviyeye özel bir tanıtım kartı varsa (yeni bir mekanik — Hunter/Pulse/
Instant/Gravity Well — ya da bir landmark tanıtımı) o kart belirdiği anda
çalar. İçeriksiz seviyelerde bu aşama atlanır.

> **ElevenLabs Prompt:** Short curious, inviting chime revealing new
> information or a new mechanic in a mobile puzzle game. Warm ascending
> tone with a light magical sparkle, inviting curiosity, friendly and
> short, not as punchy as a reward sound.
>
> **Süre:** 0.3–0.5 sn.

### SFX_IntroFlightLand — One-shot — `PreLevelIntroPresenter` → **Flight Land Clip**

TARGET ve CUT yazıları, kartları kaybolmadan hemen önce ekranın üstündeki
gerçek HUD konumlarına (ilerleme çubuğu / kesim sayacı) doğru "uçarak"
küçülüyor. **Sadece** bu uçuşun ardından **hiçbir yeni kart gelmiyorsa**
(yani sekans bu inişle bitiyorsa) bu ses çalar — inişin hemen ardından bir
kart daha geliyorsa (`N CUT` ya da tanıtım kartı) o kartın **kendi**
`SFX_CutReveal`/`SFX_InfoReveal` sesi zaten "bir şey geldi" hissini
veriyor; ikisini aynı anda çalmak üst üste binip birbirini maskeliyordu, bu
yüzden bilerek ayrıştırıldı.

> **ElevenLabs Prompt:** Short satisfying soft landing "pop" sound
> representing a UI element flying into its final place in a mobile puzzle
> game. Quick whoosh ending in a light snappy tap, tiny and precise, no
> low-end thump, very short.
>
> **Süre:** 0.15–0.3 sn.

## 9. Seviye Tamamlama Popup'ı (Landmark Kartı)

Seviye tamamlanınca önce tahta üzerinde kısa bir özet gösteriliyor
(`SFX_LevelComplete` bu anda çalıyor, bkz. yukarıdaki `6. Başarısızlık /
Oyun Sonu` bölümü), ardından ekranı tamamen kaplayan koyu popup açılıyor —
bu popup'ın **ilk** beliren öğesi landmark **fotoğrafı** (hero artwork), onu
başlık/açıklama metni ve en son Retry/Next butonları takip ediyor.

### SFX_LandmarkPhotoReveal — One-shot — `LandmarkRevealPresenter` → **Photo Reveal Clip**

Popup açılmaya başladığı **anda**, yani fotoğraf kararan arka planın
üzerinde belirmeye başlarken çalar — `SFX_LevelComplete`'ten sonra, kısa
tahta-özeti aşaması bitip asıl kart açılırken gelen ikinci, ayrı bir "kart
açıldı" vuruşu.

> **ElevenLabs Prompt:** Short elegant reveal sound for a photo/artwork
> appearing on screen in a warm mobile puzzle game. Soft magical shimmer
> with a gentle rising swell, a touch cinematic but still short, warm and
> inviting rather than epic or loud.
>
> **Süre:** 0.4–0.7 sn.

---

## Bilerek Ses Eklenmeyen Durum

- **Seviye yeniden başlatma (Retry/SessionReset)**: Retry düğmesine
  basıldığında zaten genel `SFX_Button` çalıyor; üzerine bir de "seviye
  sıfırlandı" sesi eklemek gereksiz üst üste binme yaratabileceği için bu
  olay bilerek sessiz bırakıldı. İstersen ayrı bir "level restart whoosh"
  sesi için ayrı bir alan da açabilirim.

## Özet Tablo (hızlı referans)

| İsim | Kategori | Süre |
|---|---|---|
| SFX_Music | Müzik (loop) | 20–30 sn / tam parça |
| SFX_Button | Genel UI | 0.2–0.3 sn |
| SFX_BarrierStart | Bariyer | 0.3–0.5 sn |
| SFX_BarrierGrow (loop) | Bariyer | 0.6–1.5 sn |
| SFX_BarrierLock | Bariyer | 0.3–0.4 sn |
| SFX_BarrierBreak | Bariyer | 0.4–0.6 sn |
| SFX_RegionCaptured | Ödül | 0.5–0.8 sn |
| SFX_SandFill | Ödül | 0.6–1.0 sn |
| SFX_LargeCapture | Ödül | 0.6–1.0 sn |
| SFX_NearMiss | Ödül | 0.4–0.6 sn |
| SFX_ComboChanged | Ödül | 0.3–0.5 sn |
| SFX_LevelComplete | Ödül | 1.5–2 sn |
| SFX_HunterReact | Tehdit | 0.3–0.5 sn |
| SFX_OutOfCuts | Oyun Sonu | 0.6–0.8 sn |
| SFX_OutOfLives | Oyun Sonu | 0.6–0.8 sn |
| SFX_PowerFreezeActivate | Güç | 0.6–0.8 sn |
| SFX_PowerInstantBarrierArm | Güç | 0.3–0.5 sn |
| SFX_PowerInstantBarrierConsume | Güç | 0.3–0.5 sn |
| SFX_PowerGravityWellActivate (loop) | Güç | 0.6–1.5 sn |
| SFX_PowerUnavailable | Güç | 0.2–0.4 sn |
| SFX_LevelReveal | Seviye Başlangıcı | 0.3–0.5 sn |
| SFX_TargetReveal | Seviye Başlangıcı | 0.2–0.4 sn |
| SFX_CutReveal | Seviye Başlangıcı | 0.2–0.4 sn |
| SFX_InfoReveal | Seviye Başlangıcı | 0.3–0.5 sn |
| SFX_IntroFlightLand | Seviye Başlangıcı | 0.15–0.3 sn |
| SFX_LandmarkPhotoReveal | Seviye Tamamlama | 0.4–0.7 sn |

Toplam **26** ses alanı (1 müzik + 25 efekt) kod tarafında hazır. 20'si
`Assets/Cutrium/Content/Sounds/` altındaki dosyalarla zaten bağlı; son 6'sı
(Seviye Başlangıcı grubu + `SFX_LandmarkPhotoReveal`) **yeni** ve henüz
opsiyonel/boş — aynı klasöre aynı isimlerle `.wav` eklediğinde
`Cutrium/Setup/Apply Audio Clips`'i tekrar çalıştırman yeterli, otomatik
bağlanır.
