Taş teslimatı performans düzeltmesi — 29 Ekim 2026

Neden

Mevcut ekonomi kaydı 5.664.022 bayt, 68 teslimat taşı ve kesilmiş mesh verileri içeriyordu. Her taşın rezervasyonu/doğrulaması sırasında bütün kayıt JSON üzerinden kopyalanıyor, SHA-256 hesaplanıyor ve atomik dosya yazımı oyun iş parçacığında yapılıyordu. Periyodik checkpoint ayrıca değişmeyen taşları ve kesilmiş meshleri tekrar topluyordu. Mevcut ham taş 1.309 vertex ve BoxCollider kullanıyor; ölçülen ana darboğaz bu kayıt akışıydı.

Ölçümler

- Değişiklik öncesi aynı gerçek kaydın bağımsız bellek kopyasında tam commit: 324–363 ms. Bu süre disk yazımı hariçtir. Kopyalama 45–61 ms; teslimat listesi kopyalama 53–99 ms; atomik disk yazımı ayrıca 14–21 ms.
- Tipli C# kopyalama sonrası aynı veri: tam kopya 0,05–4,57 ms; teslimat listesi 0,036–0,185 ms. Sonuçlar 4 örnektir; mikro ölçüm tek başına FPS karşılaştırması değildir.
- Son kodla Play Mode: gerçek kaydın ayrı dosyasında 68 geçmiş taş + yeni 30 taş. 30/30 teslim, ikinci ödeme yok. Döküm boyunca 535 gözlenen kare: medyan 12,78 ms; p95 33,74 ms; en yüksek 114,47 ms; 20 kare 50 ms üzerinde. Ana iş parçacığında dosya yazımı 0; arka planda 71 yazım. Son arka plan yazımı 96,84 ms.
- Önceki kodun Play Mode kare süresi kaydı alınmadı; önce/sonra FPS artış yüzdesi iddia edilmiyor. Editör ölçümünde kalan tekil sıçramalar var; bunların tümü için neden doğrulanmış değil. Standalone build/GPU profili ve uzun süreli yüzlerce taş testi yapılmadı.

Üretim kodu değişiklikleri

- Assets/PC/Scripts/Economy/EconomyService.cs: JSON roundtrip kopyalama yerine tipli kopya; başarılı yazım sonrası ortak yayınlama. Ödeme/yeni oyun işlemi mevcut bekleyen yazımı tamamlayarak veri sırasını korur.
- Assets/PC/Scripts/Orders/StoneOrder.cs: alan kopyası için Clone. (Dosyanın gerçek konumu Orders klasörüdür.)
- Assets/PC/Scripts/Delivery/StoneDeliveryItem.cs: mutable snapshot/liste alanlarını ayrı kopyalama; immutable büyük JSON metinlerini paylaşma; JSON üretmeden dünya durumu karşılaştırma.
- Assets/PC/Scripts/Delivery/EconomyDeliveryPersistence.cs: mevcut senkron API korunarak teslimat için async kayıt API'leri; yalnız değişiklik varsa kopyalama/yazım.
- Yeni Assets/PC/Scripts/Economy/EconomyAsyncSave.cs: tek sıralı arka plan yazıcısı; ayrılmış DTO verisinde JSON, hash, dosya yazımı. Unity nesnelerine erişim ve olaylar ana iş parçacığında. Başarı onayı olmadan yeni durum yayınlanmaz. Hata, çıkış ve reset akışı desteklenir.
- Yeni Assets/PC/Scripts/Economy/EconomySaveHash.cs: Windows CNG standart SHA-256; eski hash ile birebir uyumlu. Diğer platformlarda mevcut managed sağlayıcı kullanılır. Kayıt biçimi/sürümü değiştirilmedi.
- Assets/PC/Scripts/Economy/StoneEconomyBootstrap.cs: sonuçları ana iş parçacığında işleme; kapanışta bekleyen yazımı tamamlama.
- Assets/PC/Scripts/Delivery/StoneDeliveryManager.cs: ana iş parçacığını bekletmeden kalıcı rezervasyon/doğrulama; yalnız hareket eden/değişen kaynakların snapshot'ı; uykuya geçen fiziğin son pozunu bir kez daha yakalama. Kesilmiş kaynağın değişmeyen mesh verisini tekrar üretmeme; yarımların güncel pozları yine kaydedilir. Arıza sonrası yeni denemede eski Faulted durumunu temizleme.
- Assets/PC/Scripts/Delivery/DeliveredStoneInstance.cs: hangi taşın değiştiğini sahibine iletme.
- Assets/TidyUpFramework/Runtime/Save/TidyLevelSnapshotter.cs: isteğe bağlı ID filtresi ve önceden yakalanmış custom state; mevcut çağrıların varsayılan davranışı korunur.

Korunan mekanikler

Taş, rezervasyon diske ulaşmadan etkinleşmez; bütün taşlar doğrulanmadan Delivered olmaz. Ödeme yeniden alınmaz. Tek kamyon/kuyruk; 3 saniye geliş, 2 saniye bekleme, en az 0,15 saniye döküm aralığı, 3 saniye dönüş ve Point merkezli 0,75 m rastgele disk korunur. Mevcut RockMeshVariator, pickup, fizik, yerleştirme, testere, mesh kesme ve cevher sistemi değiştirilmedi. Çok yavaş depolamada gerçek döküm aralığı kayıt onayını beklediği için uzayabilir.

Sahne/prefab/Inspector

Model, materyal, collider, prefab, katalog ve ScriptableObject verisi değiştirilmedi. Yeni Inspector ataması gerekmez. Test için kullanılan kayıt yolu ve saveProgress değişiklikleri geri alındı; sahne mevcut gerçek ayarlarıyla kaydedildi. Point ve kamyon rotasyonu aynı; eksik script ve geçici test taşı yok. Git ve paket işlemi yapılmadı.

Çalıştırılan testler

- DeliveryCoreChecks: 21 başarılı kontrol. Rezervasyon, ordinal sırası, hatalı yazım, idempotency, Delivered önkoşulları ve eski API.
- DeliveryPlayChecks: 37 başarılı kontrol, başarısız kontrol yok. Gerçek oyuncu döngüsü; 1/10/2 adet ve kuyruk; süreler; diskten döküm; fizik; varyasyonlu mesh/materyal; mevcut pickup; testereye yerleştirme; iki runtime mesh yarısı; kesilmiş kaynağı yeniden kurma; ikinci ödeme/çoğalma olmaması.
- DeliveryAsyncChecks: yeni 14 başarılı kontrol. İş parçacığı ayrımı, durable acknowledgement, veri kopyasının izolasyonu, hata sonrası yazıcıyı bırakma, ödeme ile sıralama, eski formatla yükleme, değişmeyen durumda sıfır yazım, yeni oyunun eski yazımla ezilmemesi ve boş/ASCII/Türkçe/uzun metin SHA-256 uyumu.
- DeliveryFailureChecks: 13 başarılı kontrol. Doğrulama yazımı hatası, mevcut taşı çoğaltmadan yeniden deneme, döküm sırasında Point kaybı, katalog referansı hatası ve kamyon Rigidbody politikası.
- DeliveryRestartPrepare + DeliveryRestartVerify: tam Play Mode kapat/aç sonrasında 6 başarılı kontrol. Teslim edilen 1 taş korunur; yarım 3 taşlık sipariş eksiksiz tamamlanır; benzersiz ID, aynı bakiye, kayıtlı rezervasyonun çoğalmadan doğrulanması.
- DeliveryCleanup: Siparişlerim ekranında Teslim edildi; test taşları ve siparişleri temizlendi.
- DeliveryLivePerformance: yukarıdaki 30 taşlık büyük kayıt testi.
- Unity derlemesi başarılı. Son Console ground truth: compilationFailed=false, 0 mevcut error, 0 mevcut warning. Yakalanmış eski log geçmişinde bu değişiklik öncesi hatalar bulunuyor; bunlar yeni hata gibi sayılmadı.
- DeliveryFinalize: gerçek ekonomi, framework level ve progress kayıtlarının önce/sonra hash'leri aynı. Sonuç Delivery-final-audit.json içinde. Gerçek kayıt sıfırlanmadı veya test siparişiyle değiştirilmedi.

Testlerde bulunan ve düzeltilen durumlar

Async akışta arıza sonrası eski Faulted durumu yeni başarılı yazımı da hatalı kabul ediyordu; yeni denemenin durum başlangıcı düzeltildi ve 13 arıza kontrolü tekrar geçti. Eski Play Mode testi mesh referansının aynı kalmasını bekliyordu; mevcut prefabın RockMeshVariator bileşeni runtime mesh oluşturduğu için test bu mevcut davranışı doğrulayacak şekilde düzeltildi. Üretim varyasyon davranışı kaldırılmadı. Derleme sonrası kısa Pipeline yeniden yüklenmesinde bir Play Mode komutu bağlantı bulamadı; editörün hazır durumu doğrulandıktan sonra oturum açıldı, testler tamamlandı.

Kaynak ölçümler ve bütün test sonuçları bu klasördeki Delivery-*.json dosyalarındadır. Yardımcı test scriptleri Assets/PC/Tests~/ altında tutulur; Unity bunları üretim derlemesine dahil etmez.
