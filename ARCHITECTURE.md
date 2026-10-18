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

## 3. Güncel Kontrol Şeması

| Eylem | PC Kontrolü | Mobil Uyarlaması (Hedef) |
|---|---|---|
| **Kamera Gezinme (Pan)** | Orta Fare Tuşu (Tekerlek basılı) | Tek/İki Parmakla Kaydırma |
| **Kamera Zoom** | Fare Tekerleği | İki Parmakla Kıstırma (Pinch-to-zoom) |
| **Bileşen Taşıma** | Sol Tık + Sürükle | Tek Parmakla Sürükleme |
| **Kablo Bağlama** | Pin 1 Sol Tık -> Pin 2 Sol Tık | Pin 1 Dokun -> Pin 2 Dokun |
| **Switch Aç/Kapa** | Sol Tık (Sürüklemeden bırak) | Dokunup bırakma |
| **Çoklu Seçim (Box Select)**| TOOLS menüsünden `⬚ SELECT` -> Sol tık sürükle | `⬚ SELECT` aracı ile seçim kutusu |
| **Grup Sürükleme** | Seçili gruptan birini tut ve sürükle | Seçili kapıyı tutup sürükleme |
| **Onaylı Silme** | Sağ Tık (Tekli veya seçili grup) | Uzun Basma (Long Press) |
| **İptal / Bırakma** | ESC veya Boş Alana Tıklama | Boş Ekrana Dokunma |

---

---

## 4. Son Mimari İyileştirmeler
- **Sürükle-Bırak (Drag & Drop) Spawn:** Spawn menüsündeki kare butonlardan bileşenler oyun alanına sürüklenip bırakılarak doğrudan istenen ızgara hücresine spawn edilir.
- **Kare Kart Menüsü (Grid Cards):** Sol spawn menüsü 2 sütunlu, ortasında sembol/ikon, altında ismi olan kare kartlar şeklinde düzenlendi.
- **Evrensel Şematik Standart Sembolleri (IEEE Std 91 / ANSI / IEC):**
  - **Lamba / Gösterge (Lamp):** Daire içinde tam 'X' (`Circle with X`). Sinyal 1 olduğunda yeşil dolgu, parlayan beyaz 'X' ve ışıma halkası.
  - **Anahtar (SPST Switch):** Bıçak kontaklı terminal anahtar. Açıkken kol yukarı kalkık, kapalıyken iki terminali birleştiren düz hat.
  - **Mantık Kapıları:** AND, NAND, OR, NOR, XOR, XNOR ve NOT için tam standart ANSI sembolleri (D-şekli, hilal sırt, üçgen ve çıkış baloncukları).
- **Ortogonal (Grid/Manhattan) Kablolama:** Kablolar 90 derecelik dik açılı bükümlerle (S/Z rotalama) ve paralel kanal offsetleri ile estetik biçimde akar.
- **Kablo Kutu Seçimi (Box Select):** Çoklu seçim kutusu kabloları da algılar, seçilen kablolar altın sarısı parlar ve grup silme işlemine dahil edilir.
- **İzole Kablo Ağları (Multi-Net):** Her kablo kendi bağımsız `CircuitNet` nesnesine bağlanır, sinyaller birbirine karışmaz.

---

## 5. Bir Sonraki Aşama İçin Yapılacaklar
- [ ] Tersiyer kapılar (Tam Toplayıcı / Full Adder, Flip-Flop, Osilatör/Clock)
- [ ] Mobil platform için uzun basma (Long-press) ve dokunmatik jest optimizasyonu
- [ ] Devre Kaydetme / Yükleme (JSON Save/Load)
- [ ] Entegre Devre Paketleme (Black Box / IC Çip oluşturma)
