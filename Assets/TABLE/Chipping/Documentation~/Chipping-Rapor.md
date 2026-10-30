# Cevher yontma sistemi — 29 Ekim 2026

Tarihsel rapor: Bu metin önceki cevher materyaline dönüşme sürümünü anlatır. Güncel kesit materyali davranışı ve TABLE dosya düzeni için TABLE-Duzeltme-Rapor.md dosyasını kullanın.

Sistem mevcut TidyUp_Warehouse sahnesine entegre edildi ve Play Mode'da çalıştırıldı. Aktif Unity Editor, bu oturumda mevcut Unity Pipeline/CLI bağlantısıyla kontrol edildi. Yeni paket, model veya asset indirilmedi.

## Kullanım

1. Testereyle kesilmiş bir parçayı mevcut sistemle al, Table üzerindeki slotu hedefle ve sol tıkla yerleştir.
2. Elin boşken masayı/yerleştirilmiş taşı hedefleyip E'ye bas.
3. Panodaki çekice tıkla. Çekiç mouse'u takip eder.
4. Yalnız masadaki işlenebilir taşa sol tıklayarak vur. Varsayılan tamamlanma sayısı 6.
5. İlk ESC çekici panoya döndürür; ikinci ESC kamerayı ve karakter kontrollerini geri verir. Çekicin yuvasına tıklayarak da çekici bırakabilirsin.

Tamamlanan parça aynı mesh, TidyObject ve instance ID ile kalır; dış yüzeyleri kendi cevher materyaline dönüşür. Tekrar vurulamaz; mevcut pickup/drop sistemiyle taşınabilir. Satış, ekonomi geliştirmesi, parçacık, ses veya kırılma geometrisi eklenmedi.

## 1. İncelenen mevcut mimari

TidyInteractor, TidyObject/Definition, TidySnapSlot, TidyPlacementTarget, TidyInputReader, kontrol kilitleri, cursor/pause/screen stack, PC kamera modu, testere ve runtime mesh üretimi, RawStone/CutStoneHalf, cevher profilleri, framework snapshot ve teslimat kayıtları incelendi. Yerleştirme mevcut TidySnapSlot üzerinden yürür; framework'ün önizlemesi, hareketi ve sol mouse Interact binding'i kullanılır. Yeni pickup/inventory sistemi oluşturulmadı.

## 2. Yeni üretim scriptleri

Üçü de Assets/STONE/Chipping/Scripts altında:

- StoneWorkSlot.cs: yalnız CutStoneHalf kabul eder; kapasite 1; kesit yüzünü yukarı hizalar. Çalışma dışında mevcut sistemle çıkarılabilir; çalışma sırasında kaldırma kapalıdır.
- StoneChippingTable.cs: hedefleme/E, çalışma kamerası, mevcut sistemlerin geçici askıya alınması, çekiç alma/iade/takip/vuruş, gerçek raycast doğrulaması, ilerleme UI ve temiz çıkış.
- CutStoneHalf.Chipping.cs: mevcut CutStoneHalf sınıfının partial uzantısıdır. Ayrı bir taş/veri bileşeni değildir. Taşa özel vuruş sayısı, ilerleme, tamamlanma, materyal ve snapshot işlemleri burada bulunur.

## 3. Değiştirilen mevcut scriptler

- CutStoneHalf.cs: partial tanımı ve yeni parça Initialize aşamasında yontma verisinin hazırlanması. Eski mesh/cevher görseli üretimi korunur.
- StoneCutProfile.cs: Chipping Required Hits alanı; varsayılan 6, en az 1. Yeni taş örneği bu değeri alır.
- OreDefinition.cs: Processed Ore Material alanı. Mevcut olasılık, fiyat ve görsel prefab verileri korunur.
- TidyLevelSnapshotter.cs: kaynak katılımcı yeni runtime parçalar oluşturduğunda, bu yeni nesnelerin kendi CustomState verisi ikinci aşamada yüklenir. Böylece kaynak geometrisiyle birlikte parçaların bağımsız yontma ilerlemesi de geri gelir.
- TidyLevelManager.cs: MarkCustomStateDirty ile yerleştirme durumu değişmese de yontma vuruşu framework otomatik kaydına bildirilir.

PC üretim kodları, taşıma sistemi, testere üretim kodu ve cevher olasılık seçimi yeniden yazılmadı. STONE/README.md kullanım notuyla güncellendi.

## 4. Table hierarchy ve sahne bağlantıları

```text
Table [mevcut model, collider, StoneChippingTable]
  StonePlacementSlot [StoneWorkSlot, trigger BoxCollider]
  StoneWorkPoint [yerleştirilen taşın parent'ı]
  TableCameraPoint
  HammerHolder [çekiç alma/iade trigger'ı]
    WarehouseHammer [mevcut model]
  ChippingHUD [uGUI Canvas]
    ProgressPanel
      ProgressText
      ProgressFill
```

Mevcut WarehousePegboard kullanılır. Modelin yaklaşık 29 katlık ölçeği nedeniyle eklenen dünya hedeflerinin ölçeği telafi edildi. Slot dünya konumu yaklaşık (-1,13; 0,90; 11,12), kamera (-1,13; 2,10; 9,55), çekiç yuvası (-2,10; 1,85; 11,78). Kamera masa ile panoyu birlikte görür; Transform Inspector'da düzenlenebilir. Mevcut Table modelinin konumu/rotasyonu değiştirilmedi.

## 5. Çekiç bağlantısı

Hazır WarehouseHammer modeli panodaki HammerHolder altına taşındı. Ayrı veya rastgele model üretilmedi. Mevcut hammer TidyObject bileşeni korunarak devre dışı bırakıldı; alet yeni çalışma moduna ayrıldı. Framework TidyObject'i disabled olsa bile Awake'te kaydettiği için masa Start aşamasında bu özel aleti world registry'den ayırır. Böylece eski seviye kaydı çekici panodan başka yere taşımaz. Gövdesi kinematik; modelin fizik collider'ları kapalı; tıklama/iade yuvasının collider'ından yapılır. Taş pickup sistemi aynı kalır.

Masa Inspector'ındaki Hammer Visual alanına HammerHolder altındaki başka bir sahne modelinin Transform'u atanabilir. Prefab kullanmak için önce onu Holder altında sahneye yerleştirip bu Transform'u ata. Mevcut modele ilişkin zorunlu eksik referans yok.

## 6. Kamera, cursor ve giriş sahipliği

Geçiş konum/rotasyon interpolasyonuyla yapılır; süre varsayılan 0,55 saniye. PC'nin kullandığı TidyControlLock kanalları kullanılır. Karakter, interactor, kamera efektleri, normal oyun HUD Canvas'ı ve pause menülerinin önceki enabled durumları saklanır. Screen stack back girişi geçici kapatılır. Çalışırken cursor görünür ve serbesttir; kamera TableCameraPoint'te tutulur.

PC aktifken masa giriş yapamaz; masa aktifken kontrol kilidi PC girişini engeller. ESC'nin aynı karede pause menüsüne sızmasını önlemek için kamera dönüşü bittiğinde ESC'nin bırakılması beklenir. Çıkışta önceki davranışlar ve cursor durumu geri yüklenir. Normal HUD yalnız çalışma süresince gizlenir; bilgisayar arayüzü değiştirilmedi.

## 7. Vuruş doğrulaması ve animasyon

Her yeni sol tıklamada kameradan ray gönderilir. UI üzerindeki pointer, devam eden animasyon veya yanlış çalışma modu işlemi engeller. Mevcut Tool Wall ve yerleştirme trigger'ları atlanır; katı collider'lar arkadaki taşı hedeflemeyi engeller. HammerHolder kendi özel trigger'ıyla tanınır. Taş için layer yeterli sayılmaz: collider'ın GetComponentInParent<CutStoneHalf>() sonucu doğrudan ActiveStone ile aynı olmalıdır.

Vuruş, gerçek RaycastHit.point'e doğru kısa dönme/iniş/dönüş hareketidir. İlerleme darbe anında bir kez artar. Tuşu basılı tutmak tekrar vurmaz. ESC darbe öncesi animasyonu keserse henüz uygulanmamış vuruş sayılmaz. Varsayılan vuruş süresi 0,45 saniyedir. Panoya tıklama animasyon sırasında yeni eylem başlatmaz; ESC çekici anında iade edebilir.

## 8. Taşa özel veri ve görünüm

CutStoneHalf üzerinde RequiredHits, CurrentHits, ChippingProgress, IsChippingCompleted ve CanChip okunabilir. Her yarının kendi sayacı vardır. Ortak profil yalnız yeni taş için gerekli vuruş sayısını sağlar. Taşı kaldırmak, başka taş koymak veya kameradan çıkmak sayacı sıfırlamaz.

Tamamlanmada taş renderer'larının bütün materyal slotlarına ContainedOre.ProcessedOreMaterial uygulanır; ayrı kesit cevheri child'ı gizlenir. Genel Stone_CutSurface materyali kullanılmaz. Cevhersiz veya Processed Ore Material atanmamış taşlara vurulamaz; HUD nedenini bildirir. Completed parça tekrar vurulamaz.

Dört mevcut OreDefinition assetine kendi görsel prefabının mevcut cevher materyali bağlandı: Kömür→New Material, Yakut→New Material 1, Zümrüt→New Material 2, Elmas→New Material 3. Yeni materyal yaratılmadı; mevcut alanlar silinmedi.

## 9. Kayıt ve geri yükleme

Her yarının CustomState verisi sürüm, cevher asset adı, RequiredHits, CurrentHits ve completed durumunu taşır; benzersiz kimlik mevcut TidyObject snapshot Id alanındadır. Geometri kaydı mevcut görünmez RawStone kaynağı üzerinden devam eder. Yeni nesne oluşturulduktan sonra yarının kendi CustomState'i yüklenir ve cevher görünümü yeniden uygulanır.

Satın alınmış taşlarda mevcut teslimat checkpoint'i parçaların CustomState'ini kaydeder. Geçerli darbe, ilgili kaynak için teslimat kaydını dirty işaretler. Framework sahne kayıtlarında da özel durum değişikliği bildirilir. Önceki sürümdeki CustomState'i boş yarımlar 0 vuruşla açılır. Profilin gelecekteki vuruş sayısı değişse bile kayıtlı örneğin RequiredHits değeri korunur.

## 10. Inspector ayarları

- StoneCutProfile / Chipping Required Hits: taş türünün yeni parçaları için sayı (varsayılan 6).
- OreDefinition / Processed Ore Material: tamamlanma materyali; dört mevcut cevher bağlı.
- Table / StoneChippingTable: slot, kamera noktası, Hammer Holder/Visual/Return Collider, mevcut kamera/oyuncu/interactor ve efektler, HUD/panel/metin/bar.
- Transition Duration, Hit Duration, Interaction Distance, Ray Distance, Hammer Depth, Hammer Offset, Held Hammer Euler/Scale, Strike Euler/Lift.
- TableCameraPoint Transform'u: masayı/panoyu gösterecek dünya pozu.

Kurulum için kullanıcının tamamlaması gereken zorunlu model/prefab/materyal bağlantısı kalmadı. Kendi çekiç görselini ve kamera/vuruş pozunu daha sonra değiştirebilirsin.

## 11. Gerçek testler ve sınırlamalar

Toplam 100 başarılı kontrol:

| Test | Sayı | Kapsam |
| --- | ---: | --- |
| ChippingPlayChecks | 50 | Gerçek teslimat ve runtime testere kesimi; raw reddi; tek slot; framework önizleme adayı ve mevcut Interact eylemiyle yerleştirme; gerçek Input System E/ESC ve mouse olayları; kontrol/cursor/kamera kilitleri; çekiç alma/takip/iade; yanlış taş/masa/boşluk/UI engeli; basılı tutmada tek darbe; bağımsız 3/6 ve 1/6; autosave dirty; kayıt yeniden kurma; 6. darbede materyal; completed reddi; boş taş bilgilendirmesi. |
| ChippingDataChecks | 15 | Dört cevherin kendi materyali ve görsel child gizlenmesi; profil değiştikten sonra kayıtlı 8 vuruş gereksiniminin korunması; eksik materyal ve boş cevher engeli. |
| ChippingRestartVerify | 7 | Play Mode tamamen kapatılıp açıldı; diskten iki runtime yarı yeniden kuruldu; biri 6/6 ve cevher materyaliyle, diğeri bağımsız 1/6; kimlikler, cevher ve sipariş durumu korundu. |
| ChippingCompatibilityChecks | 7 | Mevcut PC giriş/kamera/desktop/çıkış ve normal pause/resume. |
| DeliveryCoreChecks | 21 | Mevcut ekonomi/teslimat API'leri, rezervasyon, sıra, hata, idempotency, Delivered önkoşulları, yükleme ve yeni oyun. |

Hazırlık turundaki tekrarlar ek başarı sayısı olarak sayılmadı. Tam fiziksel yerleştirme tıklaması yerine framework'ün aynı Interact eylemi sanal tetiklendi; sol mouse binding'i ayrıca doğrulandı. Yontma sol mouse basılı tutma ve E/ESC olayları test cihazlarıyla gerçek Input System üzerinden yürüdü. Son ekran görüntüsü Chipping-completed.png içinde görsel olarak incelendi.

Bulunan/düzeltilen hatalar: modelden miras alınan collider ölçeği; Tool Wall trigger engeli; yeni üretilen yarıların custom state'inin yüklenmemesi; çalışma görünümünde normal HUD çakışması; tekrar kurulumda çekiç boyutunun küçülmesi; disabled TidyObject'in yine Awake'te registry'ye girmesi; scene taşlarının özel ilerleme değişikliğinin autosave'i işaretlememesi. Test yardımcı klavyesinin odak politikasıyla devre dışı kalması yalnız test cihazını etkinleştirerek çözüldü; üretim input politikası değiştirilmedi.

Derleme başarılı. Son Console ground truth: compilationFailed=false; 0 error ve 0 warning. Testler sırasında mevcut framework fontunun döndürme oklarına ilişkin uyarıları görüldü; yeni sistem metinleriyle ilgili değildi ve ortak font/prompts kapsam dışı değiştirilmedi.

Gerçek ekonomi, level ve progress kayıtlarının önce/sonra hash'leri aynı; Chipping-final-audit.json içinde. Test yolu ve saveProgress geri alındı, test taşları silindi, kendi Play Mode oturumum kapatıldı, sahne kaydedildi. Git işlemi yapılmadı.

Standalone build, gamepad/touch çalışma girişi, farklı ekran oranlarında geniş görsel tarama, 1000 taşta yeni bir performans karşılaştırması ve işletim sistemi uygulamasını tamamen kapat/aç testi yapılmadı. Disk geri yükleme Play Mode domain yeniden başlatmasıyla doğrulandı. Bu aşamada parçalanma, gelişmiş el animasyonu, efekt ve ses yoktur.

Yardımcı testler Assets/STONE/Chipping/Tests~/ altında ve Unity üretim derlemesinin dışındadır. Test JSON'ları bu raporun yanındadır. PC ve ortak framework klasörleri taşınmadı.
