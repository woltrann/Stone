# Taş kesme entegrasyonu — 26 Ekim 2026

Sistem mevcut TidyUpFramework üzerine uygulanmış, Unity 6000.6.5f1 içindeki TidyUp_Warehouse sahnesinde çalıştırılmıştır. Canlı Editor işlemleri Unity Pipeline/CLI üzerinden yapılmıştır. Yeni pickup, inventory veya placement sistemi eklenmemiştir.

## 1. İncelenen sistemler

- TidyInteractor: nişan alma, engel kontrolü, ForcePickUp/ForceDrop, elde taşıma, önizleme ve mevcut Interact girdisiyle yerleştirme.
- TidyObject / TidyObjectDefinition: durum geçişleri, tanımlar, Collider/Rigidbody önbelleği, hareket/fizik profili, tekil kayıt kimlikleri.
- TidySnapSlot / TidyPlacementTarget: kapasite, kabul değerlendirmesi, animasyonlu yerleştirme, kaldırma izni, hedef kaydı ve Release.
- TidyInputReader / TidyUpInputActions: mevcut Interact, Drop, Throw, Inspect ve kontrol kilitleri. F zaten Throw için kullanılıyordu.
- TidyObjectRegistry / TidyTargetRegistry: mevcut nesne ve hedef kayıtları.
- TidyLevelSnapshot, TidyLevelSnapshotter ve TidySaveService: framework nesne durumunu saklıyordu; çalışma anında oluşan taş yarımlarını yeniden oluşturma ve cevher sonucu saklama desteği yoktu.
- Beş mevcut taş/cevher scripti, HamTas profili, dört OreDefinition, sol/sag prefabları ve sahne bileşenleri.

## 2. Değiştirilen scriptler

| Script | Değişiklik / neden |
| --- | --- |
| StoneCutProfile.cs | İki aşamalı olasılık korundu. Tam %0/%100 sınırları düzeltildi; toplam, aralık, NaN/Infinity, eksik referans ve tekrarlı cevher doğrulamaları eklendi. Prefab/cevher önkoşulları kesimden önce denetleniyor. Inspector liste ekleme/silme işlevi korunuyor. |
| OreDefinition.cs | Görsel prefab için eksik referans, TidyObject ve fizik bileşeni kontrolü. Cevher ayrı alınabilir nesne değil, parçanın görselidir. |
| RawStone.cs | Örneğe özel tek seferlik sonuç, işlenmiş durum ve mevcut kayıt sistemine cevher/yarım oluşturma verisi. Aynı taş yeniden zar atmıyor. |
| CutStoneHalf.cs | Cevher prefabının yazılmış ölçeği korunuyor; kaynak taş kimliği ve profil bilgisi taşınıyor. Tekrarlı Initialize eski görseli kaldırıyor. |
| StoneSawMachine.cs | T tuşu; framework nişan/mesafe/boş el/kontrol kilidi koşulları; tek kesim, süre, güvenli iptal; iki parça hazır olduktan sonra tüketme; doğru boyutlandırma ve yüzeye yerleştirme; durum yazısı ve VFX/ses UnityEvent bağlantıları. |
| TidySnapSlot.cs | Yalnızca sealed kaldırıldı; mevcut yerleştirme davranışı korunarak özel taş slotuna kalıtım açıldı. |
| TidyLevelSnapshot.cs / TidyLevelSnapshotter.cs | İsteğe bağlı CustomState verisi ve nesne kimlikleri eşleştirilmeden önce katılımcının çalışma anı nesnelerini yeniden oluşturması. Eski kayıtlarda boş CustomState destekleniyor. |
| TidyOutlineHighlighter.cs | Testte bulunan boş runtime malzeme dizisinin IndexOutOfRangeException üretmesi düzeltildi; gerektiğinde malzemeler yeniden hazırlanıyor. |

Ham taş başarılı kesimden sonra görünmez ve etkileşilemez hale gelir. Framework kaydının kaynak nesneyi tanıyabilmesi için tüketilmiş, pasif bir kayıt nesnesi olarak tutulur. Sağ/sol parçalar ayrı TidyObject olarak kullanılabilir. Başarısız veya iptal edilen kesimde ham taş korunur.

## 3. Yeni scriptler

- StoneSawSlot.cs: TidySnapSlot türevi. Yalnız RawStone taşıyan işlenmemiş taşları kabul eder. Kesim sırasında kaldırmayı engeller; mevcut önizleme, animasyon ve kayıt altyapısını kullanır.
- ITidyObjectSnapshotParticipant.cs: framework ile oyun mekaniğini birbirine bağımlı hale getirmeyen küçük kayıt katılımcısı arayüzü.
- Assets/STONE/Tests~/ProbabilityChecks.cs, PlayChecks.cs, OreVisualChecks.cs: Unity Pipeline script run ile çalıştırılan doğrulama araçlarıdır; Assets dışında olduklarından oyun build'ine eklenmezler.

## 4. Sahne, prefab ve asset değişiklikleri

- Stein (1)–Stein (4): RawStone ve HamTas referansı eklendi. Mevcut pickup/fizik bileşenleri korundu. Dört kaynak taşın kayıt kimlikleri benzersiz yapıldı.
- Stein: zaten CutStoneHalf içeriyordu. Bu mevcut kesilmiş örnek korundu, ham taşa dönüştürülmedi; ayrı kimlik ve kesilmiş taş tanımı verildi.
- Mevcut Cube / CuttingSlot: StoneSawMachine, StoneSawSlot ve yerleştirmeyi algılayan trigger eklendi. Slot masa yüzeyinin üzerine taşındı. Hedef organizasyon ilerlemesine katılmaz.
- Cube altına LeftOutputPoint ve RightOutputPoint eklendi ve bağlandı. Çıktılar masa yüzeyinde, birbirine girmeyen konumlarda oluşuyor.
- sol.prefab / sag.prefab kullanıldı. OreSocket referansları tamamlandı; sağ prefabda eksik soket oluşturuldu. Soketler mevcut modellerin üst yüzeyine yerleştirildi.
- Yeni Stone_CutPiece.asset, mevcut TidyObjectDefinition temel alınarak oluşturuldu; HamTas ve iki yarım prefaba bağlandı. Ham taş tanımı silinmedi.
- kom/elm/yak/zum görsel prefablarındaki SphereCollider kaldırıldı. Framework MarkLoose, çocuk collider'larını yeniden etkinleştirdiği için yalnız devre dışı bırakmak yeterli değildi.
- TidyHUD Canvas'ına TextMeshPro StoneSawStatus eklendi. İşlem tuşu ve kesim ilerlemesi yalnız uygun mesafede testereye bakarken görünür. Türkçe metin için mevcut dinamik LiberationSans fallback asseti Unity tarafından güncellendi; yeni font indirilmedi.
- HamTas olasılıkları %5 ve koşullu %50/%25/%20/%5 olarak korundu. Cevher assetleri silinmedi.

## 5. Kullanım ve Inspector ayarları

Mevcut sahne için zorunlu bir bağlantı eksikliği kalmadı.

1. Mevcut kontrollerle ham taşı al; Cube üzerindeki kesim noktasına bakıp mevcut Interact girdisiyle yerleştir (E veya sol tık).
2. Elin boşken kesim masasına bak ve T'ye bas. Kesim 2 saniye sürer.
3. İki parçayı mevcut alma/bırakma kontrolleriyle kullan.

Olasılık: HamTas → Contains Ore Percent (0–100). Ore Chances içinden satır ekle/sil; her satıra OreDefinition ve koşullu Chance Percent ata. Cevher ihtimali sıfırdan büyükken toplam 100 olmalıdır; hatalar Console'a yazılır. %0 için boş liste geçerlidir.

Yeni taş türü: ayrı StoneCutProfile oluştur/çoğalt, kendi olasılıklarını ve sol/sağ prefablarını ayarla; ilgili RawStone.Cut Profile alanına ata. Yarım prefabların kökünde TidyObject, CutStoneHalf, Rigidbody ve Collider olmalı; RawStone bulunmamalı. OreSocket kesit yüzeyinde olmalı. Cevher görsel prefabında TidyObject, Collider veya Rigidbody bulunmamalı.

Kesim süresi ve mesafe Cube → StoneSawMachine üzerinden ayarlanabilir. T tuşu mevcut framework bağlarıyla çakışmaz; farklı bir tuş seçerken framework girdilerini dikkate al.

Gerçek gönye testere/bıçak modeli sahnede bulunmadı. Mevcut Cube korundu; rastgele model eklenmedi. Model sağlandığında StoneSawMachine.Saw Blade alanına hareket edecek bıçağın Transform'unu ata; Blade Local Cut Offset ve Blade Spin Degrees Per Second değerlerini modele göre ayarla. Kesim başlangıcı, tamamlanması ve iptali için UnityEvent alanları ses/efekt eklemeye hazırdır.

## 6. Çalıştırılan testler

Son derleme: 0 hata, 0 uyarı. Sahne taraması: missing script yok; zorunlu altı testere referansı dolu; dört RawStone profili geçerli. Sahne kaydedilmiş ve Play Mode kapatılmıştır. Ham taşlarda testten kalmış zar sonucu veya çalışma anı yarımları bulunmadığı ayrıca kontrol edilmiştir.

200.000 örnek, sabit tohumlu gerçek Unity Random testi:

| Sonuç | Sayı |
| --- | ---: |
| Boş | 189.991 (%94,9955) |
| Kömür | 4.912 |
| Yakut | 2.523 |
| Zümrüt | 2.049 |
| Elmas | 525 |

Boşluk ve dört cevherin frekansları beklenen dağılımdan 6 standart sapma sınırı içinde kaldı. Ayrıca %0/boş liste, %100/tek cevher, %100/çok cevher, boş listeyle pozitif ihtimal, toplam 99/101, negatif/NaN/Infinity, pozitif ağırlıkla eksik cevher, tekrarlı cevher ve geçersiz ilk aşama ihtimali testleri geçti. Rastgele üretecin önceki durumu test sonunda geri yüklendi.

31 Play Mode kontrolü geçti. Bunlar mevcut pickup, elde takip ve drop; mevcut Interact girdisiyle önizleme/yerleştirme; süreli kesim; ikinci kesim/ikinci taş/geri alma kilitleri; ham taşın erken tüketilmemesi; iki boş yarım; iki cevherli yarım; parçaların mevcut etkileşim desteği; yeniden kesimi reddetme; testere devre dışı bırakılınca güvenli iptal; aynı cevherle yeniden başlatma; Input System üzerinden T tuşu; kaynak/profil bilgisi ve JSON kayıt geri yüklemesidir.

Kayıt testi mevcut dinamik yarımları kaldırıp kayıt JSON'undan yeniden oluşturdu; cevher aynı kaldı, kopya parça oluşmadı ve tüketilmiş kaynak durumu korundu. Eski, kesim öncesi snapshot'a dönüşte dinamik yarımların temizlenmesi de doğrulandı.

Dört cevherin her biri ayrı gerçek kesimden geçirildi. İki çıktı, doğru Mesh/prefab, soket konumu, collider'sız cevher görseli ve çıktıların çakışmaması doğrulandı. Slot 45 derece döndürülerek taş boyutlarının dünya eksenindeki bounds nedeniyle büyümemesi de test edildi. Kamera render'ı görsel olarak incelendi.

Kanıtlar: PlayChecks-result.json, OreVisualChecks-result.json ve ore-preview.png bu raporun yanındadır.

## 7. Bulunan hatalar ve çözümler

Eksik RawStone/testere bileşenleri ve profil/soket bağlantıları tamamlandı. Tekrarlanan taş kimlikleri ayrıldı. F/Throw çakışması T ile giderildi. Kesim sırasında geri alma kapatıldı. İki sonuç hazır olmadan ham taş tüketilmiyor; hata halinde oluşturulan kısmi sonuçlar temizleniyor. Önceden yazılmış prefab ölçeğinin ham taş ölçeğiyle tekrar çarpılarak küçülmesi ve döndürülmüş bounds kaynaklı boyut şişmesi giderildi. Cevher görselinin ölçeği korunuyor. Cevher collider'larının yeniden etkinleşmesi kaynak prefab düzeyinde düzeltildi. Dinamik parçaların kayıtta kaybolması için snapshot katılımı eklendi. Boş outline malzeme dizisi kaynaklı framework exception'ı düzeltildi.

İlk sentetik klavye denemeleri gerçek klavyenin Editor odak/arka plan sıfırlamasından etkilendi. Nihai test geçici Input System klavyesiyle başarıyla çalıştı; test cihazı kaldırıldı, değiştirilen input ve arka plan ayarları geri yüklendi.

## 8. Sınırlar ve test edilmeyenler

- Gerçek testere/bıçak modeli bulunmadığından bıçak animasyonunun görsel testi yapılamadı. Mantıksal kesim ve ilerleme geri bildirimi çalışıyor.
- Mevcut sol/sag prefabları aynı taş meshinin yassılaştırılmış sürümleridir; düz geometrik kesit yüzeyleri yoktur. Bunlar korundu. Cevher mevcut üst yüzeyde görünür. Gerçek düz kesit estetiği için hazırlanmış yarım modelleri bu iki prefabda değiştirip OreSocket konumlarını ayarlamalısın. Gerçek zamanlı mesh kesme eklenmedi.
- Kayıt testi framework snapshot ve JSON CaptureWorld/RestoreWorld üzerinden yapıldı. Oyunu tamamen kapatıp yeniden açma, menüden PlayerPrefs kaydet/yükle ve platform build testi yapılmadı.
- Fiziksel klavyeyle insan oynanışı yerine framework API'leri, mevcut sanal Interact girdisi ve Input System test klavyesi kullanıldı.
- Son çalışma kontrolünde yeni runtime hata yoktu. Framework'ün mevcut Prompt yazısındaki ↺/↻ karakterleri için LiberationSans glyph uyarıları kaldı; taş kesme hatası değiller. Önceden oluşmuş ve düzeltilmiş outline exception'ı Console geçmişinden silinmedi.
- Cevher kaydı profil içindeki OreDefinition asset adını kullanır. Kayıt uyumluluğu gerekiyorsa mevcut cevher assetlerini yeniden adlandırmadan/kalıcı olarak kaldırmadan önce kayıt göçü planlanmalıdır.
- Satış, vitrin, zımparalama, envanter ve ekonomi eklenmedi. Yeni paket/asset indirilmedi. Git commit/pull/push/reset/checkout yapılmadı.
