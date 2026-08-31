# Cutrium — Game Overview

## Bu Doküman Ne İçin

Bu, oyunun **oyun-içi kodundan bağımsız**, dışa dönük genel bir tanıtım
dokümanı. Şunlar için kullan:

- Web sitesi metni
- Google Play Store / App Store mağaza açıklaması
- Basın kiti / sosyal medya bio'su
- Yapay zekaya (görsel üretim) wallpaper/anahtar görsel/ikon ürettirirken
  referans/prompt kaynağı

Diğer `Docs/*.md` dosyaları geliştirme ekibi için yazıldı (mimari,
kısıtlar, kararlar); bu dosya ise **oyunu hiç bilmeyen birine** (bir
pazarlama metni okuyucusuna ya da bir görsel üretim modeline) doğru,
güncel ve kullanılabilir bir tablo sunmak için var. İçerik gerçek kod ve
içerikten alındı, uydurma/abartı yok — mağaza politikalarına aykırı
olmaması için henüz oyunda olmayan şeyleri "var" gibi yazmadım (ör. henüz
sadece 2 bölüm var, 5 bölüm planlanıyor — aşağıda bu ayrım net).

---

## Kimlik

| | |
|---|---|
| **İsim** | Cutrium *(çalışma adı: Containment — değişebilir)* |
| **Tür** | Tek parmakla oynanan mobil arcade-bulmaca |
| **Platform** | Android & iOS, sadece dikey (portrait) |
| **Diller** | İngilizce, Türkçe |
| **Oturum süresi** | Kısa — metroda, molada, uyumadan önce birkaç dakikada birkaç seviye |

## Tek Cümlelik Pitch

> Parmağınla çizdiğin bariyerlerle tahtayı böl, boş alanları güvenle ele
> geçir, hareketli tehditleri köşeye sıkıştır — her başarılı kesim, gerçek
> bir dünya harikasını kaplayan kumun altından yavaşça ortaya çıkmasını
> sağlıyor.

## Kısa Açıklama (mağaza için, ~1-2 cümle)

**TR:** Tek dokunuşla bariyer çiz, alanları ele geçir, dünyanın dört bir
yanından ünlü yapıları kumun altından ortaya çıkar. Sakin ama gergin,
kısa ama tatmin edici.

**EN:** Draw barriers with a single touch, capture the board, and reveal
famous landmarks from around the world hidden beneath the sand. Calm yet
tense, short yet deeply satisfying.

## Uzun Açıklama (mağaza / web sitesi taslağı)

Cutrium'da amaç basit: tahtanın üzerinde hareket eden tehditlerden kaçarak
parmağınla bariyerler çiz, tahtayı böl ve boş bölgeleri ele geçir. Her
bariyer tamamlanana kadar kırılgan — bir tehdit ona değerse kesim boşa
gider. Ama tamamlandığı an, o bölge kumun altından temizlenir ve altında
**gerçek bir dünya yapısı** belirmeye başlar.

Her seviye, dünyanın farklı bir yerinden gerçek bir yapıyı (tapınak, kule,
köprü, saray...) konu alıyor — Kamboçya'daki Angkor Wat'tan Paris'teki
Eyfel Kulesi'ne, İstanbul'daki Ayasofya'dan Tokyo yakınlarındaki Fuji
Dağı'na kadar. Seviyeyi bitirdiğinde sadece bir "level complete" ekranı
görmüyorsun — o yapı hakkında kısa, gerçek bir bilgi kartı açılıyor.

Yol boyunca özel güçler kazanıyorsun: tehditleri yavaşlatan **Freeze
Pulse**, bir kesimi anında tamamlayan **Instant Barrier**, tehditleri
kendine çeken **Gravity Well**. Art arda başarılı kesimler kombo
oluşturuyor; bir tehdide kıl payı yaklaşıp kaçırmadan tamamlanan
kesimler "near miss" ödülü veriyor.

## Temel Oynanış Döngüsü

1. Tahtada bir veya daha fazla tehdit hareket ediyor.
2. Oyuncu boş bir bölgeden bir nokta seçip yatay ya da dikey bir bariyer
   başlatıyor.
3. Bariyer iki yöne doğru büyüyor; tamamlanana kadar bir tehdit ona
   değerse bariyer kırılıyor (ceza hafif — tüm seviye baştan başlamıyor).
4. Bariyer tamamlanınca bölge ikiye ayrılıyor; içinde tehdit **olmayan**
   yarı ele geçiriliyor (kum çekiliyor, altındaki gerçek yapı fotoğrafı
   ortaya çıkıyor).
5. Hedef yüzdeye (genelde %75-85) ulaşınca seviye tamamlanıyor, o
   seviyenin yapısı hakkında kısa bir bilgi kartı açılıyor.

## Güçler

- **Freeze Pulse** — Tehditleri kısa süreliğine ciddi şekilde yavaşlatır/
  durdurur; nefes alacak bir an yaratır.
- **Instant Barrier** — Bir sonraki bariyer büyümeden anında tamamlanır;
  riskli bir kesimi güvenle bitirmek için.
- **Gravity Well** — Belirli bir yarıçaptaki tehditleri kendine doğru
  çekip yönlendirir, süresi boyunca tehlikeyi kontrol altına alır.

Tüm güçler seviyeye göre şarj sınırlı ve içerik olarak tanımlı (yani her
seviyede olmayabilir, ilerledikçe açılır).

## Dünya Turu — İçerik Kapsamı

Vizyon **60 seviye / 5 bölüm** (her bölümde 12 gerçek dünya yapısı) —
ama **şu an sadece ilk 2 bölüm (24 seviye/24 yapı) yayında**, kalan 3
bölüm planlı ama henüz üretilmedi. Mağaza metninde "60 harika" gibi bir
iddia kullanma, şu anki gerçek sayı **24**.

**Bölüm 1 (12):** Angkor Wat (Kamboçya) · Aspendos Antik Tiyatrosu
(Antalya, Türkiye) · Aziz Basil Katedrali (Moskova) · Big Ben & Westminster
Sarayı (Londra) · Borobudur Tapınağı (Endonezya) · Brandenburg Kapısı
(Berlin) · Burj Al Arab (Dubai) · Burj Khalifa (Dubai) · Grand Canyon
(Arizona) · Chichén Itzá (Meksika) · Kurtarıcı İsa Heykeli (Rio de
Janeiro) · CN Kulesi (Toronto).

**Bölüm 2 (12):** Çin Seddi · Dubrovnik Surları (Hırvatistan) · Efes
(İzmir, Türkiye) · Elhamra Sarayı (Granada, İspanya) · Eyfel Kulesi
(Paris) · Fuji Dağı (Japonya) · Fushimi Inari Tapınağı (Kyoto) · Galata
Kulesi (İstanbul) · Golden Gate Köprüsü (San Francisco) · Göbeklitepe
(Şanlıurfa, Türkiye) · Kapalıçarşı (İstanbul) · Ayasofya (İstanbul).

Her yapı için hem İngilizce hem Türkçe, 2-3 cümlelik, tarihî/mimari bir
ton taşıyan gerçek bir açıklama metni zaten oyunda mevcut (müze levhası
gibi — dönem + çarpıcı bir detay).

## Görsel Yön (görsel üretim / wallpaper için referans)

Oyunun ana görsel fikri: **sıcak, kumlu bir "keşif kutusu."** Her seviye
bir kum kabının içinde saklı bir dünya harikası; oyuncu kumu "çekerek"
altındaki gerçek fotoğrafı ortaya çıkarıyor. Renk paleti sıcak/toprak
tonları (kum sarısı, amber, hafif turuncu-kahve) — agresif, karanlık ya da
gerçekçi silah/patlama estetiğinden tamamen uzak. Tehditler düz renkli,
sade şekiller (Normal / Hunter (kırmızıya kayan) / Pulse varyantları) —
odak her zaman ortaya çıkan gerçek yapıda.

**Ruh hâli anahtar kelimeleri:** sıcak, meraklı, keşif, sakin ama hafif
gergin, tatmin edici, "before/after" dönüşüm anı, dünya turu, müze
levhası ciddiyeti + oyuncak/mobil oyun sıcaklığı.

### Hazır Yapay Zeka Görsel Prompt'ları

> **Anahtar görsel / wallpaper:** A warm, cozy mobile puzzle game key art.
> A shallow bowl of golden desert sand with a glowing world landmark
> (e.g. a temple or tower) half-revealed beneath it, as if the sand is
> being gently swept away. Soft warm lighting, amber and honey tones,
> a single glowing barrier line cutting across the sand, minimal and
> inviting, no violence or danger, family-friendly mobile game
> aesthetic, high detail, promotional key art composition, portrait
> orientation.

> **Uygulama ikonu konsepti:** A minimal mobile game app icon: a
> simple bowl shape filled with warm golden sand, a single glowing
> amber line cutting diagonally across it, a faint hint of a landmark
> silhouette peeking through the sand at the bottom, flat and clean,
> centered composition, warm color palette, no text, rounded square
> safe area.

> **Sosyal medya / mağaza afişi:** A collage-style promotional banner
> for a "world landmarks" mobile puzzle game: several famous world
> landmarks (a temple, a tower, a cathedral, a desert canyon) softly
> emerging from golden sand in a row, warm cinematic lighting, cohesive
> amber/sand color grading, playful but polished mobile-game marketing
> style, landscape orientation, space reserved for a logo/title at the
> top.

## Ses/Müzik Yönü

Sıcak, rahat ama tatmin edici bir mobil bulmaca tonu — hafif orkestral +
elektronik doku, dünya keşfi hissi veren perküsyon, meraklı ve oyuncu bir
ruh hâli; agresif/karanlık değil. (Tam ses listesi ve prompt'lar için bkz.
`Docs/AUDIO_ASSETS.md`.)

## Hedef Kitle

Kısa oturumlarda (metro, mola, yatmadan önce) rahatlatıcı ama zihinsel
olarak tatmin edici bir bulmaca arayan, gündelik/casual mobil oyuncular.
Yaş kısıtlaması yok, şiddet/gerçekçi tehlike içermiyor.

## Kullanılabilir Asset Referansları

Mağaza listeleme / web sitesi için gerçek landmark görsellerini
kullanabilirsin:

- `Assets/Cutrium/Content/Earth Landmarks/` — 60 landmark fotoğrafının
  tamamı burada (sadece ilk 24'ü şu an oyunda aktif, geri kalanı gelecek
  bölümler için hazır bekliyor).
- `Docs/ASSET_PROVENANCE.md` — hangi görsellerin sana ait/üretilmiş,
  hangilerinin placeholder olduğunun kaynak/lisans notları — mağazaya
  görsel yüklemeden önce burayı kontrol et.

## Önemli Not

Bu doküman **pazarlama/tanıtım amaçlı** — teknik doğruluk için değil.
Oyun içeriği değiştikçe (yeni bölüm eklenince, güç değişince) burayı
güncel tutmayı unutma; aksi hâlde mağaza açıklaması gerçek oyunla
uyuşmaz hâle gelir.
