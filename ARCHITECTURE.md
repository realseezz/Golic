# Golic - 2D Logic Circuit Simulator

## 1. Proje Özeti & Vizyon
Golic, Godot 4 (.NET / C#) tabanlı, yüksek performanslı ve profesyonel seviyeye (IC entegrasyonu, Bus veri yolları, CPU tasarımı) ölçeklenebilir bir 2D mantık devresi simülasyon sandbox oyunudur.

Mevcut prototip PC ortamında geliştirilmekte olup, kontroller mobil (Android/iOS dokunmatik) mimariye doğrudan uyarlanabilecek biçimde tasarlanmıştır.

---

## 2. Mimari Yapı (Model-View Separation)

```text
Golic/
├── Core/                      # Godot'tan bağımsız Saf C# Simülasyon Motoru
│   ├── LogicState.cs          # Low (0), High (1), Floating
│   ├── Pin.cs                 # Input & Output bağlantı uçları
│   ├── CircuitComponent.cs    # Kapılar için temel soyut sınıf
│   ├── CircuitNet.cs          # Elektriksel hatlar (Kablo ağları)
│   ├── CircuitSimulator.cs    # Tick tabanlı BFS/Adım simülatörü
│   └── Gates/
│       └── BasicGates.cs      # AND, OR, NOT, XOR, NAND, NOR, SWITCH, LED
│
└── Presentation/              # Godot 4 Görsel ve Kullanıcı Etkileşim Katmanı
    ├── GridDrawer.cs          # Sonsuz ızgara (Grid) çizimi
    ├── CameraController.cs    # Pan & Zoom kamerası
    ├── GateView.cs            # Grid-snap kapı çizimleri, pinler, LED ve Switch
    ├── PinView.cs             # Dokunulabilir pin noktaları ve seçim halkaları
    ├── WireView.cs            # Dinamik kablo çizimi, gerilim rengi (Yeşil/Mavi)
    ├── WiringController.cs    # Tap-to-Connect kablolama mantığı
    ├── ComponentPalette.cs    # People Playground tarzı katlanabilir katalog ve araçlar
    └── CircuitManager.cs      # Tüm girdi hiyerarşisi, çoklu seçim ve simülasyon orkestrası
```

---

## 3. Kontrol Şeması & Mobil UX Mimarisi (Benchmark Analizi)

> **Mobil Sandbox Oyunları Benchmarkı (Mindustry, Bad Piggies, Circuit Jam, Wire-Head):**
> - **Parmak Kapama Problemi (Fat-Finger):** Mobilde 16-20px pinler parmak altında kalır. Çözüm: Dokunma anında pini algılayan sanal yarıçap (Touch Hitbox) 36px'e genişletilir ve parmağın hemen üzerinde minik bir büyüteç lensi (Magnifier/Offset indicator) belirir.
> - **Kamera vs Etkileşim:** Boş alanda tek parmak = Pan. Çift parmak = Pinch-to-Zoom. Obje üzerinde tek parmak = Sürükleme.
> - **Bağlam Menüsü (Radial/Context Menu):** Bir objeye dokunulduğunda hemen yanında 3 hızlı hap buton belirir: 🗑️ Sil, 🔄 Döndür (Rotate), 📋 Kopyala.

| Eylem | PC Kontrolü | Mobil Uyarlaması (Hedeflenen UX) |
|---|---|---|
| **Kamera Gezinme (Pan)** | Orta Fare Tuşu (Tekerlek basılı) | Boş alanda Tek Parmakla Kaydırma |
| **Kamera Zoom** | Fare Tekerleği | İki Parmakla Kıstırma (Pinch-to-zoom) |
| **Bileşen Taşıma** | Sol Tık + Sürükle | Obje üzerine dokunup sürükleme |
| **Kablo Bağlama** | Pin 1 Sol Tık -> Pin 2 Sol Tık | Pin 1 Dokun (Seçilir) -> Pin 2 Dokun |
| **Switch Aç/Kapa** | Sol Tık (Sürüklemeden bırak) | Hızlı dokunup bırakma (Tap) |
| **Bileşen Ekleme** | Menüden Sol Tık Sürükle-Bırak | Menü kartından parmakla sahneye sürükle-bırak |
| **Çoklu Seçim (Box Select)**| TOOLS menüsünden `⬚ SELECT` | TOOLS'tan `⬚ SELECT` seçiliyken parmakla kutu çizme |
| **Onaylı Silme** | Sağ Tık (Tekli / Grup) | Uzun Basma (Long-Press ~350ms) veya Obje Bağlam Menüsü |
| **İptal / Bırakma** | ESC veya Boş Alana Tıklama | Boş Ekrana Dokunma |

---

## 4. Son Mimari İyileştirmeler
- **Sürükle-Bırak (Drag & Drop) Spawn:** Spawn menüsündeki kare butonlardan bileşenler oyun alanına sürüklenip bırakılarak doğrudan istenen ızgara hücresine spawn edilir.
- **Dikdörtgen Kart Menüsü (1:1 Orantılı Çizim):** Sol spawn menüsü 2 sütunlu, üstte 1:1 kare şematik ikon, altta ismi olan kartlar şeklinde düzenlendi.
- **Evrensel Şematik Standart Sembolleri (IEEE Std 91 / ANSI / IEC):**
  - **Lamba / Gösterge (Lamp):** Daire içinde tam 'X' (`Circle with X`). Sinyal 1 olduğunda yeşil dolgu, parlayan beyaz 'X' ve ışıma halkası.
  - **Anahtar (SPST Switch):** Bıçak kontaklı terminal anahtar. Açıkken kol yukarı kalkık, kapalıyken iki terminali birleştiren düz hat.
  - **Mantık Kapıları:** AND, NAND, OR, NOR, XOR, XNOR ve NOT için tam standart ANSI sembolleri (D-şekli, hilal sırt, üçgen ve çıkış baloncukları).
- **Ortogonal (Grid/Manhattan) Kablolama:** Kablolar 90 derecelik dik açılı bükümlerle (S/Z rotalama) ve paralel kanal offsetleri ile estetik biçimde akar.
- **Kablo Kutu Seçimi (Box Select):** Çoklu seçim kutusu kabloları da algılar, seçilen kablolar altın sarısı parlar ve grup silme işlemine dahil edilir.
- **İzole Kablo Ağları (Multi-Net):** Her kablo kendi bağımsız `CircuitNet` nesnesine bağlanır, sinyaller birbirine karışmaz.

---

## 5. Sıralı Yol Haritası (Roadmap)

### Aşama 1: Dinamik & Zamana Bağlı Bileşenler (Öncelikli)
1. **Clock (Osilatör):** Belirli frekansta (0.5s, 1s) otomatik `0 -> 1 -> 0` vuruşu üreten kaynak. Devreye kalp atışı kazandırır.
2. **Push Button (Anlık Buton):** Sadece basılı tutulduğunda 1 veren, bırakınca 0 olan yaylı buton.
3. **D Flip-Flop (Bellek/Hafıza):** Clock darbesiyle gelen biti saklayan 1-bit hafıza hücresi (Sayaç ve RAM temeli).

### Aşama 2: Sandbox & Üretkenlik Araçları (Tools Menüsü)
1. **Rotate (Döndürme - R Tuşu):** Seçili kapıyı 90 derece döndürerek pin yönlerini (kuzey/güney/doğu/batı) ayarlama.
2. **Klonlama (Clone / Ctrl+C, Ctrl+V):** Seçili grubu çoğaltma.
3. **Save / Load (JSON Kayıt Sistemi):** Yapılan devreyi isim verip dosyaya kaydetme ve geri yükleme.

### Aşama 3: Zengin Çıktı & Göstergeler (Geri Bildirim)
1. **7-Segment Display:** 0-9 arası sayıları yakan 7 girişli dijital gösterge.
2. **Buzzer (Sesli Uyarıcı):** Girişine 1 geldiğinde 'bip' tonu çıkaran piezo ses bileşeni.
3. **Full Adder (Tam Toplayıcı) & Entegre Paketleme (IC / Black Box):** Kendi yaptığın devreyi tek bir çip haline getirme.
