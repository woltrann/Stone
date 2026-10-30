# STONE

Yontma sistemi: `Assets/TABLE/Chipping/Scripts`. Kullanım, Inspector bağlantıları ve test raporu: `Assets/TABLE/Chipping/Documentation~/TABLE-Duzeltme-Rapor.md`. Kesilmiş parçayı Table'a sol tıkla yerleştir; E ile çalış; panodaki çekice tıkla; taşa sol tıkla vur. İlk ESC çekici bırakır, ikinci ESC çalışma modundan çıkar.

Taş oluşturma, cevher ve kesim mekaniğinin projeye özel dosyaları bu klasörde toplanmıştır.

```text
STONE/
  Art/
    Models/             Stein.fbx; içindeki taş meshleri
    Materials/Rocks/    Taş materyalleri
    Materials/Ores/     Cevher materyalleri
    Textures/Rocks/     Taş dokuları, normal ve lighting map
    Icons/              Taş ikonu
  Data/
    CutProfiles/        HamTas, HamTas 1, HamTas 2, kaya
    Ores/               Kömür, yakut, zümrüt, elmas tanımları
    TidyDefinitions/    Ham/kesilmiş taş tanımları ve taş kategorisi
  Prefabs/
    CutStone/           sol ve sag
    Ores/               kom, yak, zum, elm
  Scripts/
    Cutting/            Taş/cevher/kesim scriptleri
    ProceduralRocks/    Prosedürel üretim, mesh kesici ve Editor araçları
    RockMeshVariator.cs
  FrameworkIntegration/
    ITidyObjectSnapshotParticipant.cs
    TidyUpFramework.Runtime.asmref
  Tests~/               Pipeline ile çalıştırılan doğrulama kodları
  Documentation~/      Önceki rapor, önizleme ve test kanıtları
```

27 Ekim 2026 düzenlemesi: 32 asset/klasör taşıması Unity AssetDatabase.MoveAsset ile yapıldı. .meta/GUID kimlikleri korundu. Sahnedeki 14 RawStone, dört StoneCutProfile ve dört OreDefinition korunuyor; kopuk referans veya missing script bulunmadı. HamTas üzerindeki mevcut %100 ayarı değiştirilmedi.

Tests~ ve Documentation~ sonundaki ~ Unity'nin bu klasörleri oyun asseti olarak derleyip import etmesini engeller. Dosyalar diskte bu STONE klasörünün içindedir. Test komutları Tests~/README.md içindedir.

Ortak bağımlılıklar:

- TidyUpFramework'ün mevcut pickup, placement, input, fizik, outline ve snapshot scriptleri ortak framework klasörlerinde kalır. Taşınan kayıt arayüzü .asmref sayesinde hâlâ TidyUpFramework.Runtime assembly'sine derlenir; döngüsel bağımlılık oluşmaz.
- URP, Input System, TextMeshPro ve ortak HUD/font dosyaları kendi yerindedir.
- Aktif oyun sahnesi Assets/Scenes/TidyUp_Warehouse.unity içinde kalır; tüm warehouse sahnesi taş mekaniğine özel olmadığı için taşınmadı.
- Prosedürel olarak üretilen çalışma anı meshleri dosya değildir; onları oluşturan scriptler burada toplanmıştır. FBX içindeki kalıcı meshler Art/Models altındadır.

Taşıma öncesinde sahnede kaydedilmemiş değişiklikler vardı. Bunlar açık Editor'de korundu; klasör düzenlemesi sırasında sahne zorla kaydedilmedi veya yeniden yüklenmedi. Bu, klasöre toplama aşamasındaki durumdur; sonraki runtime kesim entegrasyonu sonunda sahne kaydedildi.

Documentation~/Rapor.md önceki mekanik geliştirme raporudur; sahnenin o tarihteki durumunu anlatır. Güncel klasör düzeni bu README'dir. FolderAudit.json taşıma sonrası GUID/referans denetimini, FolderMove-ProbabilityChecks-result.json taşınmış testin sonucunu içerir.

## Güncel runtime kesme sistemi — 27 Ekim 2026

Kesim artık gerçek MeshFilter geometrisinden yapılır. Prefabs/CutStone altındaki sol/sag yalnız eski kayıt uyumluluğu içindir; yeni taş türleri için yarım prefab gerekmez.

Yeni servisler: StoneRuntimeMeshSlicer, StoneMeshGeometry ve StoneCutPieceFactory. CuttingPlane ve CutSurfaceMaterial sahnedeki Cube testeresine bağlandı. Inspector kullanımı, desteklenen mesh yapıları, gerçek test sonuçları ve kayıt kurtarma notu Documentation~/RuntimeMeshSlicing-Rapor.md içindedir.

Sahne kaydedilmiştir. Mevcut Stein (5) açık/non-manifold geometri nedeniyle güvenli biçimde reddedilir; yeni sistem onu kendiliğinden başka bir modelle değiştirmez.

