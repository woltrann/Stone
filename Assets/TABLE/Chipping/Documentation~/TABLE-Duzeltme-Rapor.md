# TABLE yontma düzeltmesi ve klasör düzeni — 29 Ekim 2026

İstenen düzeltme uygulandı. Cevherli/cevher bulunmayan bütün gerçek runtime kesim parçaları, geçerli kesit materyalleri varsa aynı yontma işlemini kullanır. Son vuruşta yalnız taş geometrisinin materyal yuvaları o parçanın kendi kesit materyaline döner. Geometri değiştirilmez. Cevher türü ve olasılık verisi korunur; yontma iznini veya uygulanacak materyali belirlemez.

## 1. Değiştirilen scriptler

- **CutStoneHalf.Chipping.cs:** cevher koşulları kaldırıldı. Taşa özel RequiredHits/CurrentHits korunur. Tamamlanma yalnız açıkça tanımlanan taş geometri renderer'larını değiştirir. Yardımcı meshler ve ayrı cevher görseli boyanmaz veya gizlenmez. Kayıt v2 kesit submesh indeksini ve materyalin kalıcı kimliğini saklar; v1 vuruş kayıtları da yüklenir.
- **StoneChippingTable.cs:** yalnız durum metni düzeltildi. Cevhersiz taş uyarısı yerine normal yontma ilerlemesi; gerçek kesit bilgisi eksikse açıklayıcı yapılandırma mesajı gösterilir. Kamera, cursor, çekiç takip/animasyon, raycast, giriş/çıkış ve yerleştirme akışları yeniden yazılmadı.
- **CutStoneHalf.cs:** CutSurfaceSubmesh artık son elemandan hesaplanmıyor; kesim sonucu tarafından açıkça atanıyor. Parça CutSurfaceMaterial ve kalıcı materyal kimliğini tutuyor.
- **StoneRuntimeMeshSlicer.cs:** mevcut algoritmadan gelen kapak indeksini sonuç verisine ekler. Mesh kesme algoritması, olasılıklar ve geometri üretimi değiştirilmedi.
- **StoneSawMachine.cs / StoneCutPieceFactory.cs:** bu indeksi yeni parçanın oluşturulmasına ileten küçük parametre bağlantıları.
- **StoneMeshGeometry.cs / RawStone.cs:** snapshot'a kapak indeksi/materyal kimliği ekleme ve parçayı aynı materyalle geri kurma. Kayıt yükleme sırasında mevcut testere ayarı başka bir materyal olsa bile kaydedilmiş kimlik kullanılır.
- **OreDefinition.cs:** artık kullanılmayan Processed Ore Material alanı Inspector'da gizlendi. Alan, mevcut serialized değeri, public uyumluluk getter'ı ve asset verisi silinmedi. Yontma kodu bu değeri okumuyor.
- Taşınan test yardımcıları yeni klasör ve mantığa uyarlandı. Eski kurulum yardımcısının cevher materyali atayan adımı kaldırıldı. STONE/README.md güncel TABLE konumuna yönlendirildi.

## 2. Gerçek kesit materyali nasıl bulunuyor?

Mevcut RockMeshSlicer kodu `input.subMeshCount + 1` submesh üretir ve kesit üçgenlerini `capSubmesh = input.subMeshCount` indeksine yazar. StoneRuntimeMeshSlicer kaynak materyal listesini aynı sırada oluşturur ve testerenin seçilmiş kesit materyalini bu yeni yuvaya ekler. Testereye ayrı kesit materyali atanmamışsa slicer'ın mevcut kaynak materyal fallback davranışı korunur.

Bu eşleme sonuçtan `CutSurfaceSubmesh` olarak Factory'ye ve CutStoneHalf'a taşınır. Parça oluşturulurken **doğrulanmış indeksteki** MeshRenderer.sharedMaterials referansı yakalanır. Sonraki yontma işlemi materyal dizisinin son elemanını okumaz ve bütün sahne için güncel testere materyalini kullanmaz. Testte cap indeksi 0, son materyal yuvası farklı olacak şekilde ayrıca doğrulandı.

Son vuruş renderer'ın kendi sharedMaterials dizisindeki referansları değiştirir; Material assetinin shader/renk/doku değerleri düzenlenmez. Mevcut mesh referansı, vertex/üçgen verisi ve collider'lar korunur. Runtime kesim zaten kaynak geometrisini tek taş renderer'ında birleştirir; ek taş geometri renderer'ları gerekiyorsa CutStoneHalf üzerindeki Stone Geometry Renderers listesi kullanılabilir. Cevher görselleri, başka CutStoneHalf'a ait renderer'lar ve listeye dahil olmayan yardımcı objeler işlenmez.

## 3. Kalıcı materyal referansları

Yeni **StoneCutMaterialReferences.cs** ve **Editor/StoneCutMaterialReferencesEditor.cs**, yalnız kullanılan kesit materyallerini GUID + local file ID ile referanslar. Verisi `Resources/TableCutMaterials.asset` içindedir. Bu, iki taş farklı materyallerle kesildiğinde veya testere ayarı daha sonra değiştiğinde yanlış materyalin yüklenmesini önler; materyaller kopyalanmaz.

Editor yardımcısı açık testerelerin Inspector değişikliklerinde ve Play Mode girişinde referansları toplar. Testerenin mevcut kaynak materyal fallback'i kullanılacaksa yüklenmiş ham taşların materyalleri de kayda alınır. Tüm projenin 311 materyalini Resources içine yükleyen bir liste oluşturulmadı; yalnız ilgili materyal referansları tutulur. Son kayıtta 3 referans vardır.

Eski meshVersion=1 kayıtları için o sürümün doğrulanmış cap=input.subMeshCount sözleşmesi ve mevcut materyal adlarıyla uyumluluk yolu korunur. Eski v1 yontma ilerlemesi, cevher adı filtresi uygulanmadan yüklenir. Gerçek materyal güvenilir biçimde bulunamıyorsa cevher materyali veya rastgele başka materyal tahmin edilmez: işlem engellenir ve mesaj verilir. Silinmiş/ulaşılamayan materyal assetinin kendisini otomatik yeniden oluşturmaz.

## 4. Cevher kontrolü nereden kaldırıldı?

CanChip artık ContainsOre/ProcessedOreMaterial kontrol etmez. Table HUD'u cevher yok diye işlemi durdurmaz veya cevher materyali istemez; başlık genel “Taş — Yontma İşlemi”dir. Tamamlanma cevher materyaline dönüşmez. Kayıt yüklemesinde vuruş ilerlemesi cevher ismi eşleşmesine bağlı değildir. ContainedOre/ContainsOre ve mevcut cevher görseli bilgi olarak korunur; olasılık veya seçim algoritmasına dokunulmadı.

## 5. TABLE'a taşınan dosyalar

Mevcut `Assets/TABLE` başlangıçta boştu. `AssetDatabase.MoveAsset` ile bütün `Assets/STONE/Chipping` klasörü **Assets/TABLE/Chipping** konumuna taşındı; `.meta` dosyaları korundu. Buna önceki üç üretim scripti (StoneChippingTable, StoneWorkSlot, CutStoneHalf.Chipping), önceki beş test yardımcısı ve rapor/sonuç/görsel dosyaları dahildir.

```text
Assets/TABLE/
  Chipping/
    Scripts/
      StoneChippingTable.cs
      StoneWorkSlot.cs
      CutStoneHalf.Chipping.cs
      StoneCutMaterialReferences.cs
    Editor/
      StoneCutMaterialReferencesEditor.cs
    Resources/
      TableCutMaterials.asset
    Tests~/
      ChippingSetup.cs
      ChippingPlayChecks.cs
      ChippingDataChecks.cs
      ChippingRestartVerify.cs
      ChippingCompatibilityChecks.cs
      SurfaceMaterialPlayChecks.cs
      TABLEFolderAudit.cs
    Documentation~/
      TABLE-Duzeltme-Rapor.md
      TABLE-folder-audit.json
      TABLE-final-save-audit.json
      test sonuçları ve görseller
```

Tests~/Documentation~ Unity'nin üretim derlemesi dışında tutulan yardımcı/çıktı klasörleridir. Eski Chipping-Rapor.md tarihsel rapor olarak saklandı; artık geçerli davranış bu rapordadır. Kullanıldığı belirsiz eski dosyalar silinmedi. Yeni bir TABLE kökü, boş Materials/Prefabs/UI klasörleri veya gereksiz model/prefab kopyası oluşturulmadı.

Ortak TidyUpFramework dosyaları, PC dosyaları, testere/slicing dosyaları, ana CutStoneHalf dosyası, OreDefinition/StoneCutProfile ve ortak materyal/prefablar yerlerinde kaldı. Masa/çekiç modeli ve HUD mevcut sahneye gömülüydü; bunları sırf klasör düzeni için yeni prefablara dönüştürmedim.

## 6. Inspector ve sahne denetimi

TABLE-folder-audit.json: Table ve slot script GUID'leri öncekiyle aynı; taşıma öncesi/sonrası serialized Table/slot ayarları aynı; 12 temel Inspector referansı bağlı; eksik script ve eksik referans 0. Required Hits, vuruş sayısı verisi, Hammer/Holder, kamera, slot, vuruş/kamera süreleri ve diğer offset/rotasyon ayarları korundu. Source fallback materyal referansları ayrıca Editor denetiminde doğrulandı.

Kullanıcının yapması gereken zorunlu yeni ayar yok. Normalde testeredeki Cut Surface Material seçimini yapman yeterlidir; yeni kesilen her parça kendi o andaki referansını alır. Mevcut taşların materyali sonradan testere ayarı değişince topluca değişmez.

## 7. Gerçek test sonuçları

**212 başarılı kontrol**, hazırlık tekrarları sayılmadan:

| Test | Kontrol | Sonuç |
| --- | ---: | --- |
| ChippingPlayChecks | 50 | Gerçek cevherli taş kesimi, mevcut preview/yerleştirme eylemi, gerçek Input System E/ESC/sol mouse, hold tek vuruş, UI/yanlış hedef engeli, kamera/cursor/çekiç, 3/6 bağımsız ilerleme, kesit materyaliyle tamamlanma, boş parçanın da işlenmesi. |
| ChippingDataChecks | 30 | Dört cevher türü aynı kuralla işler; explicit cap slot=0 ve farklı son materyal; son darbeden önce dış yüzey aynı; çoklu taş renderer'ı; cevher görseli/yardımcı mesh değişmez; geometri ve shared asset baytları aynı; eksik gerçek materyal reddi; v1 ilerleme uyumu. |
| SurfaceMaterialPlayChecks.Main | 13 | İki gerçek cevher bulunmayan kaynak, iki farklı kesit materyali, dört gerçek runtime yarı; masada bağımsız 3/6; yalnız A'yı tamamlama; B'nin materyali değişmez; mesh aynı; mevcut checkpoint. |
| SurfaceMaterialPlayChecks.VerifyRestart | 10 | Play Mode tamamen kapatılıp açıldı: A 6/6, B 3/6; materyal asset kimlikleri farklı ve doğru; B mevcut testere materyalini ödünç almaz; kalan üç vuruşla kendi materyaline dönüşür; tekrar ödeme/teslimat yok. |
| ChippingCompatibilityChecks | 7 | Mevcut bilgisayar giriş/kamera/desktop/çıkış ve normal pause/resume. |
| DeliveryCoreChecks | 21 | Mevcut rezervasyon, teyit, sıra, idempotency, ücret, hata/yükleme ve yeni oyun API'leri. |
| RuntimeSliceGeometryChecks | 81 | 17 başarılı kesim; gerçek topology, kapak normal/UV/submesh eşlemesi, hacim, konkav geometri, dönüşüm/shear/mirror, çoklu kaynak mesh, açık/uygunsuz meshleri güvenli reddetme. Native tool sonucu: ortalama 27,39 ms, maksimum 37,62 ms; FPS/performance karşılaştırması değildir. |

Yerleştirme mevcut Interact eylemi sanal tetiklenerek sınandı; çalışma E/ESC/mouse olayları test cihazlarıyla gerçek Input System'den gönderildi. Standalone build, gamepad/touch ve OS uygulamasını tamamen kapat/aç testi yapılmadı. Kalıcı veri geri yüklemesi tam Play Mode domain yeniden başlatmasıyla doğrulandı. Runtime üretilmiş, projede kalıcı Material asseti olmayan materyallerin uygulamalar arası geri üretilmesi geliştirilmedi.

## 8. Güvenlik ve Console

Derleme başarılı. Son Console'da bu mekanizmaya ait derleme/runtime hatası yok. Mevcut tek error, Unity AI Assistant'ın Logs/assistant-perf.jsonl dosyasını “Sharing violation” nedeniyle açamaması; native Editor log girdisiyle doğrulandı. Aynı eklentinin NoSubscription bilgisi de mevcut. Bu task dışında kalan eklenti/paket kodu değiştirilmedi; Console temizlenerek hata gizlenmedi.

Gerçek ekonomi, level ve progress kayıtlarının önce/sonra hash'leri aynı. Test kaydı ve test ayarları kaldırıldı, test taşları temizlendi, kendi Play Mode oturumum kapatıldı, sahne gerçek ayarlarıyla kaydedildi. Bkz. TABLE-final-save-audit.json. Git/paket indirme işlemi yapılmadı.
