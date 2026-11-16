# Hafta 2 Ödevi — Akıllı Kule (RAPOR)

**Ders:** Dijital Oyunlarda Yapay Zekâ · Hafta 2 "YZ'nin Fiziği"
**Proje:** `yapay-zeka/hafta02-akilli-kule` (.NET 10 konsol uygulaması, `dotnet run`)

## 1. Ödevin kapsamı

| İstenen | Durum | Kod |
|---|---|---|
| Lab 1: `Truncate`, `Distance`, `DistanceSq`; `(6, 8).Truncate(5) = (3, 4)` | Yapıldı, PASS | `Vector2D.cs` |
| Lab 2: `Agent` (Position, Velocity, Heading, Mass, MaxSpeed, MaxForce, `Update(kuvvet, dt)`), Euler hata tablosu, MaxSpeed = 3 deneyi | Yapıldı, PASS | `Agent.cs`, `Lab.Step2Agent` |
| Lab 3: `FuturePosition`, `TimeToCoverDistance` (ulaşamaz = −1), top tablosu | Yapıldı, PASS | `Kinematics.cs`, `Lab.Step3Ball` |
| Lab 4: `InterceptTime(P, V, s)`, örnek A ve B yaklaşık + kesin | Yapıldı, PASS | `Interception.cs`, `Lab.Step4Intercept` |
| Ödev: `bool NisanAl(kule, hedef, hedefHiz, mermiSurati, out nisan, out sure)` | Yapıldı | `SmartTurret.NisanAl` |
| Ödev: `bool AtisAcisi(v, x, y, out alcak, out yuksek)` | Yapıldı, slayt 16–17 değerleriyle PASS | `SmartTurret.AtisAcisi` |
| En az 6 senaryo, yaklaşık ve kesin nişan yan yana | 6 zorunlu + 3 ek senaryo | `Homework.ScenarioTable` |
| 3–5 cümlelik yorum | Bölüm 3 | bu dosya |
| Ek (3B): `Vector3D` ile fırlatma hızı vektörü | Yapıldı, slayt 18 notundaki cevapla PASS | `Vector3D.cs`, `SmartTurret.LaunchVelocity` |
| Alıştırmalar (slayt 23) cevap anahtarıyla doğrulama | 7/7 PASS | `Exercises.cs` |

Program çalıştırıldığında **58 kontrolün tamamı PASS** veriyor (çıktının sonundaki `Özet: 58 PASS, 0 FAIL`).

### Kısa yöntem notları

- **Agent.Update** slayt 9'daki yarı örtük Euler'dir: önce kuvvet `Truncate(MaxForce)` ile kırpılır (ders notundaki hatırlatma), sonra `a = F / m`, `v += a·Δt`, `v = Truncate(v, MaxSpeed)`, `p += v·Δt`; hız sıfır değilse `Heading = birim(v)`.
- **FuturePosition** sürtünmeli topta yalnızca top durana kadar geçerlidir; durduktan sonra durma noktasını döndürür (slayt 12 notu). Örnek: `FuturePosition(10, −2, 7) = 25 m`.
- **InterceptTime** `(V·V − s²)t² + 2(P·V)t + P·P = 0` denkleminin en küçük pozitif kökünü döndürür; kök yoksa −1. Hedef mermiyle tam aynı hızdaysa (`V·V = s²`) denklem birinci dereceye iner; bu da ayrıca ele alındı (senaryo 9).
- **NisanAl** `InterceptTime` −1 döndürürse `false` verir (`nisan` = hedefin şimdiki yeri, `sure` = −1). Böylece YZ "vuramıyorum" bilgisini kullanabilir.
- **AtisAcisi** açıları derece olarak döndürür, `g = 9.81`. Kökün içi < 0 ise `false` (menzil dışı), = 0 ise iki açı eşit (45°, en uzak atış). Kayan nokta yuvarlaması yüzünden tam sınırdaki hedefin reddedilmemesi için çok küçük bir tolerans var.
- **Iska** ölçüsü: mermi nişan noktasına vardığı anda (`|nişan| / s` saniye sonra) hedefin o noktaya uzaklığı. Slayt 15'teki "0.36 m ıska" değeri bu tanımla birebir çıkıyor.

## 2. Senaryo sonuçları (özet)

Kule (0, 0), mermi sürati 20 m/s. Değerler aşağıdaki program çıktısından alındı.

| # | Senaryo | Hedef / V | Yaklaşık t → nişan (ıska) | Kesin t → nişan (ıska) | Balistik (AtisAcisi) |
|---|---|---|---|---|---|
| 1 | Yandan geçen | (15, 0) / (0, 5) | 0.600 s → (15, 3) (0.82 m) | 0.775 s → (15, 3.873) (0) | alçak 11.16° / yüksek 78.84° |
| 2 | Uzaklaşan | (12, 9) / (4, 3) | 0.600 s → (14.4, 10.8) (1.50 m) | 1.000 s → (16, 12) (0) | 14.69° / 75.31° |
| 3 | Yaklaşan | (30, 0) / (−6, 0) | 1.154 s → (23.077, 0) (0) | 1.154 s → (23.077, 0) (0) | 17.23° / 72.77° |
| 4 | Duran | (10, 10) / (0, 0) | 0.707 s → (10, 10) (0) | 0.707 s → (10, 10) (0) | 10.15° / 79.85° |
| 5 | Mermiden hızlı kaçan | (10, 0) / (25, 0) | 0.222 s → (15.556, 0) (13.89 m) | **false** (yetişemez) | ateş etme |
| 6 | Menzil dışı | (45, 0) / (0, 3) | 1.957 s → (45, 5.87) (0.94 m) | 2.276 s → (45, 6.827) (0) | **false** (45.51 m > 40.77 m) |
| 7 | (ek) Hızlı yandan geçen | (20, 0) / (0, 15) | 0.571 s → (20, 8.571) (7.75 m) | 1.512 s → (20, 22.678) (0) | 23.93° / 66.07° |
| 8 | (ek) Mermiden hızlı ama yaklaşan | (30, 10) / (−25, 0) | 0.703 s → (12.432, 10) (2.38 m) | 0.751 s → (11.216, 10) (0) | 10.81° / 79.19° |
| 9 | (ek) Mermi hızında yaklaşan | (10, 0) / (−20, 0) | 0.250 s → (5, 0) (0) | 0.250 s → (5, 0) (0) | 3.52° / 86.48° |

Senaryo 8, "hedef mermiden hızlıysa her zaman yetişilemez" yanılgısını test ediyor: hedef kuleye doğru geldiği için ikinci dereceden denklemin iki pozitif kökü var ve kesin yöntem yine çözüm buluyor.

## 3. Yorum: yaklaşık yöntem nerede yetersiz?

Yaklaşık yöntem `t ≈ |P| / (s + |V|)` hedef hızının yalnızca büyüklüğünü kullanır, yönünü hiç hesaba katmaz; bu yüzden sadece hedef durduğunda ya da tam kuleye doğru geldiğinde (senaryo 3, 4 ve 9) kesin sonucu verir. Hedef yandan geçtiğinde süreyi kısa tahmin eder ve mermi hedefin arkasına düşer: yavaş geçen hedefte 0.82 m, hızlı geçen hedefte 7.75 m ıska. Uzaklaşan hedefte ise payda `s + |V|` olduğu hâlde hedef aslında `s − |V|` bağıl hızla kaçtığından hata büyür (gerçek süre 1.0 s iken 0.6 s tahmin edilir, 1.50 m ıska). En ciddi eksiği, mermiden hızlı kaçan hedefte bile bir nişan noktası üretmesidir (13.89 m ıska): "vuramıyorum" bilgisini veremez, oysa kesin yöntem diskriminant negatif olduğunda `false` döndürür ve YZ yaklaşmaya ya da hedef değiştirmeye karar verebilir. Yaklaşık yöntem her karede yeniden hesaplanan takip (Pursuit) davranışında hatasını kendiliğinden düzelttiği için yeterlidir; ama tek atışlık bir mermide düzeltme şansı olmadığından kule için kesin çözüm gerekir.

**Ek gözlem (balistik):** Kesin kestirim de merminin düz çizgide `s` hızıyla gittiğini varsayar. Kavisli atışta yatay hız `v·cos θ` olduğundan uçuş biraz daha uzun sürer; senaryo 1'de bu fark 0.075 m ıskaya yol açıyor. Slayt 18'deki gibi gerçek uçuş süresiyle kestirimi 2–3 kez tekrarlayınca ıska 0.0050 m → 0.0003 m → 0'a iniyor (çıktıdaki "EK — yinelemeli düzeltme" tablosu).

## 4. Ek görev (3B) doğrulaması

Namlu (0, 1, 0), hedef (12, 1, 16), v = 20 m/s. Slayt 18 notundaki cevap: yatay mesafe 20 m, yaw ≈ 36.9°, alçak açı ≈ 14.7°, fırlatma hızı ≈ (11.6, 5.1, 15.5).
Programın sonucu: yaw 36.87°, alçak açı 14.69°, hız (11.608, 5.071, 15.477), |hız| = 20. Mermi bu hızla analitik olarak uçurulduğunda t = 1.034 s'de tam (12, 1, 16) noktasına iniyor; Δt = 0.001 s ile Euler simülasyonunda hedefe en yakın mesafe 0.008 m.

## 5. Notlar

- Alıştırma 7'de cevap anahtarı açıları 19.4° / 70.6° olarak veriyor; program 19.45° / 70.55° buluyor (anahtardaki değerler yuvarlanmış; kökün içi 236647 ve kök 486.5 birebir aynı).
- Slayt 15'te örnek A için kesin süre "0.680 s" yazıyor; program 0.680 s ve (10, 2.041) buluyor.

## 6. Program çıktısı (`dotnet run`, değiştirilmeden kopyalandı)

```
Dijital Oyunlarda Yapay Zekâ - Hafta 2: Akıllı kule
============================================================

LAB 1 - Vector2D eklentileri: Truncate, Distance, DistanceSq
------------------------------------------------------------
                                                    hesaplanan                        beklenen                          sonuç
(6, 8).Truncate(5)                                  (3, 4)                            (3, 4)                            PASS
(3, 4).Truncate(10) (sınırın altında)               (3, 4)                            (3, 4) değişmez                   PASS
(1, 1).Distance((4, 5))                             5                                 5                                 PASS
(1, 1).DistanceSq((4, 5))                           25                                25                                PASS

LAB 2a - Euler hata tablosu: m = 2 kg, F = (10, 0) N, durgun başlangıç, 1 s
---------------------------------------------------------------------------
a = F / m = 5 m/s², gerçek konum = ½·a·t² = 2.5 m

Δt (s)  adım  Euler x (m)  gerçek (m)  hata (m)  hata / Δt  slayt   sonuç
0.5     2     3.750        2.500       1.250     2.500      3.75    PASS
0.1     10    2.750        2.500       0.250     2.500      2.75    PASS
0.02    50    2.550        2.500       0.050     2.500      2.55    PASS
Hata / Δt sabit (2.5): Euler birinci dereceden; Δt beşte birine inince hata da beşte birine iner.

LAB 2b - MaxSpeed = 3 sınırı: m = 2 kg, F = (10, 0) N, Δt = 0.1 s
-----------------------------------------------------------------
t (s)  Velocity    |v| (m/s)  Position      Heading
0.1    (0.5, 0)    0.50       (0.05, 0)     (1, 0)
0.2    (1, 0)      1.00       (0.15, 0)     (1, 0)
0.3    (1.5, 0)    1.50       (0.3, 0)      (1, 0)
0.4    (2, 0)      2.00       (0.5, 0)      (1, 0)
0.5    (2.5, 0)    2.50       (0.75, 0)     (1, 0)
0.6    (3, 0)      3.00       (1.05, 0)     (1, 0)
0.7    (3, 0)      3.00       (1.35, 0)     (1, 0)
0.8    (3, 0)      3.00       (1.65, 0)     (1, 0)
0.9    (3, 0)      3.00       (1.95, 0)     (1, 0)
1      (3, 0)      3.00       (2.25, 0)     (1, 0)
1.1    (3, 0)      3.00       (2.55, 0)     (1, 0)
1.2    (3, 0)      3.00       (2.85, 0)     (1, 0)
                                                    hesaplanan                        beklenen                          sonuç
1.2 s sonra |v|                                     3                                 3 (MaxSpeed'de sabit)             PASS
MaxForce = 4: F = (10, 0) kırpılır, 1 s sonra v     (2, 0)                            (2, 0) (a = 4 / 2 = 2)            PASS
Duran ajan: Heading korunur (sıfıra bölme yok)      (0, 1)                            (0, 1)                            PASS

LAB 3 - Top tablosu (Simple Soccer): u = 10 m/s, a = -2 m/s²
------------------------------------------------------------
Soru                                Fonksiyon                           Sonuç     Slayt         
Ne zaman durur?                     TimeToStop(10, -2)                  5         5 s           PASS
Duruncaya kadar ne kadar gider?     StoppingDistance(10, -2)            25        25 m          PASS
3 s sonra nerede?                   FuturePosition(10, -2, 3)           21        21 m          PASS
16 m'yi kaç saniyede gider?         TimeToCoverDistance(10, -2, 16)     2         2 s           PASS
30 m'yi kaç saniyede gider?         TimeToCoverDistance(10, -2, 30)     -1        Ulaşamaz (-1) PASS
(ek) 7 s sonra nerede? (durmuş)     FuturePosition(10, -2, 7)           25        25 m (not)    PASS
(ek) 25 m'yi kaç saniyede gider?    TimeToCoverDistance(10, -2, 25)     5         5 s (sınır)   PASS

LAB 4 - InterceptTime: örnek A ve B, kule (0, 0), mermi s = 15 m/s
------------------------------------------------------------------

Örnek A: hedef P = (10, 0), V = (0, 3), s = 15   denklem: -216·t² + 0·t + 100 = 0
  Yöntem    t (s)    nişan            ıska (m)  slayt                             sonuç
  yaklaşık  0.556    (10, 1.667)      0.36      t = 0.556 → (10, 1.667), 0.36 m   PASS
  kesin     0.680    (10, 2.041)      0.00      t = 0.68 → (10, 2.04)             PASS

Örnek B: hedef P = (10, 0), V = (5, 0), s = 15   denklem: -200·t² + 100·t + 100 = 0
  Yöntem    t (s)    nişan            ıska (m)  slayt                             sonuç
  yaklaşık  0.500    (12.5, 0)        1.67      t = 0.5 → (12.5, 0)               PASS
  kesin     1.000    (15, 0)          0.00      t = 1 → (15, 0)                   PASS

ÖDEV - AtisAcisi(v, x, y): slayt 16-17 tablosuyla karşılaştırma (g = 9.81)
--------------------------------------------------------------------------
v    x (m)    y (m)  kökün içi   dönüş  alçak    yüksek   uçuş (s)       slayt                               sonuç
20   30       0      73388       True   23.69°   66.31°   1.64 / 3.73    73388; 23.7° / 66.3°; 1.64 / 3.73 s PASS
20   30       5      34148       True   36.18°   63.29°   1.86 / 3.34    34148; 36.2° / 63.3°; 1.86 / 3.34 s PASS
20   45       0      -34878      False  -        -        -              < 0; menzil dışı                    PASS
20   40.77    0      0           True   45.00°   45.00°   2.88 / 2.88    = 0; tek açı (45°, en uzak atış)    PASS
25   40       0      236647      True   19.45°   70.55°   1.70 / 4.81    236647; 19.4° / 70.6° (alıştırma 7) PASS

ÖDEV - Senaryolar: kule (0, 0), mermi sürati 20 m/s, yaklaşık ve kesin nişan yan yana
-------------------------------------------------------------------------------------
ıska = mermi nişan noktasına vardığı anda hedefin o noktaya uzaklığı (0 = isabet)

#  Senaryo                       hedef     V         |V|   | yakl. t yaklaşık nişan   ıska   | NisanAl kesin t kesin nişan      ıska  
----------------------------------------------------------------------------------------------------------------------------------------
1  Yandan geçen                  (15, 0)   (0, 5)    5     | 0.600   (15, 3)          0.82   | True    0.775   (15, 3.873)      0.00  
2  Uzaklaşan                     (12, 9)   (4, 3)    5     | 0.600   (14.4, 10.8)     1.50   | True    1.000   (16, 12)         0.00  
3  Yaklaşan                      (30, 0)   (-6, 0)   6     | 1.154   (23.077, 0)      0.00   | True    1.154   (23.077, 0)      0.00  
4  Duran                         (10, 10)  (0, 0)    0     | 0.707   (10, 10)         0.00   | True    0.707   (10, 10)         0.00  
5  Mermiden hızlı kaçan          (10, 0)   (25, 0)   25    | 0.222   (15.556, 0)      13.89  | False   -       yetişemez        -     
6  Menzil dışı                   (45, 0)   (0, 3)    3     | 1.957   (45, 5.87)       0.94   | True    2.276   (45, 6.827)      0.00  
7  (ek) Hızlı yandan geçen       (20, 0)   (0, 15)   15    | 0.571   (20, 8.571)      7.75   | True    1.512   (20, 22.678)     0.00  
8  (ek) Mermiden hızlı yaklaşan  (30, 10)  (-25, 0)  25    | 0.703   (12.432, 10)     2.38   | True    0.751   (11.216, 10)     0.00  
9  (ek) Mermi hızında yaklaşan   (10, 0)   (-20, 0)  20    | 0.250   (5, 0)           0.00   | True    0.250   (5, 0)           0.00  

                                                    hesaplanan                        beklenen                          sonuç
1 Yandan geçen: kesin nişan isabet                  true, ıska 0.000000000            true, ıska 0                      PASS
2 Uzaklaşan: kesin nişan isabet                     true, ıska 0.000000000            true, ıska 0                      PASS
3 Yaklaşan: kesin nişan isabet                      true, ıska 0.000000000            true, ıska 0                      PASS
4 Duran: kesin nişan isabet                         true, ıska 0.000000000            true, ıska 0                      PASS
5 Mermiden hızlı kaçan: NisanAl                     False, süre -1                    false, -1 (diskriminant < 0)      PASS
6 Menzil dışı: kesin nişan isabet                   true, ıska 0.000000000            true, ıska 0                      PASS
7 (ek) Hızlı yandan geçen: kesin nişan isabet       true, ıska 0.000000000            true, ıska 0                      PASS
8 (ek) Mermiden hızlı yaklaşan: kesin nişan isabet  true, ıska 0.000000000            true, ıska 0                      PASS
9 (ek) Mermi hızında yaklaşan: kesin nişan isabet   true, ıska 0.000000000            true, ıska 0                      PASS
4 Duran: yaklaşık = kesin = hedef                   (10, 10) / (10, 10)               (10, 10) / (10, 10)               PASS

ÖDEV - Kesin nişan noktasına balistik atış açıları (AtisAcisi, v = 20 m/s, y = 0, en uzak atış v²/g = 40.77 m)
--------------------------------------------------------------------------------------------------------------
#  Senaryo                       kesin nişan      x (m)    AtisAcisi  alçak    yüksek   uçuş alçak/yüksek (s)   karar
1  Yandan geçen                  (15, 3.873)      15.49    True       11.16°   78.84°   0.79 / 4.00             alçak açıyla ateş
2  Uzaklaşan                     (16, 12)         20.00    True       14.69°   75.31°   1.03 / 3.94             alçak açıyla ateş
3  Yaklaşan                      (23.077, 0)      23.08    True       17.23°   72.77°   1.21 / 3.89             alçak açıyla ateş
4  Duran                         (10, 10)         14.14    True       10.15°   79.85°   0.72 / 4.01             alçak açıyla ateş
5  Mermiden hızlı kaçan          yetişemez        -        -          -        -        -                       ateş etme, hedef değiştir
6  Menzil dışı                   (45, 6.827)      45.51    False      -        -        -                       menzil dışı: yaklaş
7  (ek) Hızlı yandan geçen       (20, 22.678)     30.24    True       23.93°   66.07°   1.65 / 3.73             alçak açıyla ateş
8  (ek) Mermiden hızlı yaklaşan  (11.216, 10)     15.03    True       10.81°   79.19°   0.76 / 4.01             alçak açıyla ateş
9  (ek) Mermi hızında yaklaşan   (5, 0)           5.00     True       3.52°    86.48°   0.25 / 4.07             alçak açıyla ateş

                                                    hesaplanan                        beklenen                          sonuç
6 Menzil dışı: AtisAcisi                            False                             false (kökün içi < 0)             PASS

EK - Hareketli hedef + balistik atış: uçuş süresiyle yinelemeli düzeltme (senaryo 1, Yandan geçen)
--------------------------------------------------------------------------------------------------
Düz çizgi kestirimi t'yi mermi hızıyla hesaplar; balistik merminin yatay hızı v·cos θ olduğu için gerçek uçuş daha uzundur.
tur  kullanılan t  nişan            alçak    gerçek uçuş  ıska (hedef - nişan, uçuş bitince)
0    0.775         (15, 3.873)      11.16°   0.790        0.0747 m
1    0.790         (15, 3.948)      11.18°   0.791        0.0050 m
2    0.791         (15, 3.953)      11.18°   0.791        0.0003 m
3    0.791         (15, 3.953)      11.18°   0.791        0.0000 m
4    0.791         (15, 3.953)      11.18°   0.791        0.0000 m
5    0.791         (15, 3.953)      11.18°   0.791        0.0000 m
                                                    hesaplanan                        beklenen                          sonuç
5 tur sonra ıska                                    0.000000 m                        ≈ 0 (yakınsar)                    PASS

EK GÖREV (3B) - Vector3D ile fırlatma hızı: namlu (0, 1, 0), hedef (12, 1, 16), v = 20 m/s
------------------------------------------------------------------------------------------
d = hedef - namlu = (12, 0, 16), yatay mesafe x = 20 m, y = 0 m
birim(yatay) = (0.6, 0, 0.8)
                                                    hesaplanan                        beklenen                          sonuç
Yaw = atan2(d.x, d.z)                               36.87°                            ≈ 36.9°                           PASS
Pitch (alçak açı)                                   14.69°                            ≈ 14.7°                           PASS
Fırlatma hızı (alçak)                               (11.608, 5.071, 15.477)           ≈ (11.6, 5.1, 15.5)               PASS
|fırlatma hızı|                                     20                                20                                PASS
Uçuş sonu konum (t = 1.034 s)                       (12, 1, 16)                       (12, 1, 16) = hedef               PASS
Euler simülasyonu (Δt = 0.001): hedefe en yakın     0.008 m                           < 0.05 m                          PASS
(ek) Yüksek açı 75.31°, hız                         (3.042, 19.347, 4.057)            hedefe ulaşmalı                   PASS
(ek) Hedef (40, 1, 30), 50 m: menzil dışı           False                             false                             PASS

ALIŞTIRMALAR (slayt 23) - ders notundaki cevap anahtarıyla doğrulama
--------------------------------------------------------------------
                                                  hesaplanan                              beklenen                                sonuç
1) 144 FPS: Δt ve 6 birim/s ile bir kare          Δt = 0.00694 s, adım = 0.0417           Δt ≈ 0.00694 s, adım ≈ 0.0417           PASS
2) m = 3, F = (12, -6): ivme                      (4, -2) (Agent: Δv/1 s = (4, -2))       (4, -2) m/s²                            PASS
3) (9, 12), |v| = 15, MaxSpeed = 10               (6, 8)                                  (6, 8)                                  PASS
4) Euler, Δt = 0.25: 1. adım                      v = 1, p = 0.25                         v = 1, p = 0.25                         PASS
   2. adım                                        v = 2, p = 0.75                         v = 2, p = 0.75                         PASS
   0.5 s sonra konum / gerçek / hata              0.75 / 0.5 / 0.25 m                     0.75 / 0.5 / 0.25 m                     PASS
5) Top u = 12, a = -3: durma / yol / 20 m         4 s / 24 m / 2.37 s (v = 4.9)           4 s / 24 m / ≈ 2.37 s (v ≈ 4.90)        PASS
6) Yaklaşık nişan                                 t = 0.5 → (12, 2)                       t = 0.5 → (12, 2)                       PASS
   Kesin nişan                                    t = 0.612 → (12, 2.449)                 t ≈ 0.612 → (12, 2.45)                  PASS
7) v = 25, x = 40: kökün içi / kök                236647 / 486.5                          ≈ 236647 / ≈ 486.5                      PASS
   iki açı                                        19.45° / 70.55°                         ≈ 19.4° / ≈ 70.6°                       PASS
   keskin nişancı: uçuş süreleri                  alçak 1.70 s < yüksek 4.81 s            alçak açı (kısa uçuş)                   PASS

Özet: 58 PASS, 0 FAIL
```
