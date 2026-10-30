STONE MARKET — taş siparişi ve merkezi para sistemi
29 Ekim 2026

Sonraki teslimat geliştirmesi: siparişler artık OrderPlaced olayından kamyon kuyruğuna bağlanıyor; Delivered geçişi tüm taş kayıtlarının doğrulanmasını gerektiriyor. Ekonomi dosyası teslimat ilerlemesi ve runtime dünya kayıtlarıyla genişletildi. Bu belgedeki teslimatın henüz eklenmediği açıklamaları ilk aşamanın tarihçesidir; güncel teslimat davranışı ve testleri StoneDelivery-Rapor.md içindedir.

1. Sonuç ve kapsam

Mevcut PC'nin World Space Canvas'ı genişletildi. Taş Mağazası, Siparişlerim ve Çıkış sekmeleri; bakiye, dinamik ürün kartları, adet alanı, toplam ve bildirimler çalışıyor. Başlangıç kataloğunda gerçek sahne taşından hazırlanmış Ham Taş bulunuyor: birim fiyatı 100 TL. Başlangıç bakiyesi 10.000 TL, adet üst sınırı 999. Bu değerler Inspector'dan değiştirilebilir.

Sipariş kabul edildiğinde para ve sipariş birlikte kalıcı kayda işleniyor. Bu görevde teslimat, fiziksel taş oluşturma, satış, stok veya cevher değerleme eklenmedi. Ürün prefabının hazırlanması bir Editor asset oluşturma işlemidir; sipariş verirken sahneye taş doğmaz.

2. Yeni scriptler

Tüm yeni üretim dosyaları Assets/PC/Scripts altında:

- Shop/StoneMarketCatalog.cs: dinamik StoneProduct listesi, benzersiz ID ve prefab/Definition/RawStone/fizik/kesme profili doğrulaması; adet üst sınırı.
- Economy/EconomySettings.cs: yalnızca yeni oyun için başlangıç bakiyesi.
- Economy/EconomyService.cs: bağımsız merkezi bakiye API'si, güvenli tam sayı hesapları, sipariş ödeme işlemi, tekrar istek koruması, olaylar ve durum güncellemesi.
- Economy/EconomyFileStore.cs: para ve siparişleri tek dosyada atomik olarak saklayan, hata bildirebilen depolama adaptörü ve IEconomyStore arayüzü.
- Economy/StoneEconomyBootstrap.cs: servisi TidyServices'e kaydeder, sahne geçişlerinde korur ve framework'ün açık ResetProgress işlemini yeni oyun bakiyesine bağlar.
- Orders/StoneOrder.cs: kayıt edilebilir sipariş verisi, genişletilebilir durum enum'u ve gelecekteki taş örnekleri için belirlenebilir benzersiz ID üretimi.
- UI/StoneProductCard.cs: prefab tabanlı ürün kartı; Sprite oranı ve eksik görsel placeholder'ı.
- UI/StoneMarketView.cs: sekmeler, dinamik kartlar/sipariş satırları, TMP adet düzenleme, toplam, ödeme ve monitör içi bildirimler.

Mevcut ComputerDesktopView, InteractiveComputer, kamera/cursor/outline kodu ve TidyUpFramework kaynak scriptleri bu aşamada değiştirilmedi. ComputerDesktop prefabı genişletildi. Eski dikey buton layout'u kaldırılıp mevcut üç buton yatay sekme olarak kullanıldı; sahnedeki bu üç RectTransform'un eski boyut override'ları yeni yerleşime uyarlandı. Test yardımcıları ve kayıt denetimleri Tests~/ altında; Unity bunları oyun scripti olarak derlemez.

3. Yeni assetler ve sahne bağlantıları

- ScriptableObjects/StoneMarketCatalog.asset
- ScriptableObjects/EconomySettings.asset
- Prefabs/Products/RawStone.prefab
- Prefabs/StoneProductCard.prefab
- Prefabs/StoneOrderRow.prefab

Prefabs/ComputerDesktop.prefab mevcut arayüz korunarak mağaza ve sipariş sayfalarıyla genişletildi. Eski PC Türkçe fontuna gereken para/liste noktalama glifleri eklendi. Yeni EventSystem veya Overlay mağaza Canvas'ı oluşturulmadı. PC/Screen/ComputerDesktop altında World Space Canvas ve mevcut Input System UI modülü kullanılıyor. Monitor 2 outline hedefi korunuyor.

Sahneye bağımsız Stone Economy kökü eklendi; ayar asseti bağlı. Para servisi PC arayüzünün açılmasına bağlı değildir ve TidyServices üzerinden diğer oyun sistemlerine açıktır. İlk sahne yüklemesinde kurulur, sonraki sahne geçişlerinde aynı servis korunur.

4. Inspector'dan ürün ekleme/kaldırma

Assets/PC/ScriptableObjects/StoneMarketCatalog.asset dosyasını açın. Products listesine satır ekleyin. Yeni satırda:

- ID: benzersiz, kalıcı bir değer yazın. Örneğin ham-tas-ikinci. Kaydedilmiş siparişler varsa ilgili ID'yi değiştirmeyin.
- Display Name ve Description: görünen ad ve açıklama.
- Image: isteğe bağlı Sprite. Yoksa kartta TAŞ placeholder'ı görünür. Texture kullanacaksanız önce import türünü Sprite yapın.
- Unit Price: negatif olmayan tam sayı TL fiyatı.
- Prefab: kökünde TidyObject, RawStone, Rigidbody ve uygun collider bulunan gerçek taş prefabı.
- Definition: prefab kökündeki TidyObject.Definition ile aynı asset.
- Active: mağazada gösterilip sipariş kabul edilmesini belirler.

Kartlar kod değişikliği olmadan katalogdan oluşturulur. Yeni ürün veya fiyat/görsel değişikliği mağaza tekrar açıldığında yenilenir. 100 ürün iki sütunlu ScrollRect içinde test edildi. Aynı ID, eksik referans veya Definition uyumsuzluğu doğrulanır; geçersiz ürün satın alınamaz, Inspector değişikliğinde anlaşılır Console uyarısı verilir. Negatif Inspector fiyatı 0'a düzeltilir; işlem katmanı da negatif değeri ayrıca reddeder.

Bir ürünü mağazadan kaldırmak için Active'i kapatmak yeterlidir. Geçmiş siparişlere ait ürünleri listede pasif tutmak, gelecekte teslimatın aynı ID'den prefabı çözebilmesini sağlar. Liste satırı tamamen silinirse geçmiş siparişin adı/adedi/fiyatı korunur; fakat gelecekte teslimat için o ID'yi yeniden katalogda tanımlamak gerekir.

5. Başlangıç bakiyesi ve genel para API'si

Assets/PC/ScriptableObjects/EconomySettings.asset içindeki Starting Balance değiştirilebilir. Bu değer yalnızca henüz ekonomi kaydı yokken veya framework'te açık Yeni Oyun/ResetProgress yapıldığında kullanılır. Mevcut kayıt yüklenirken Inspector'daki yeni başlangıç değeri uygulanmaz; normal autosave, PC'ye giriş veya Play Mode yeniden başlatma bakiyeyi sıfırlamaz.

Diğer sistemler TidyServices.TryGet<EconomyService>(out var economy) ile servisi alabilir. Balance, CanAfford, TryAddMoney, TrySpend ve BalanceChanged kullanılabilir. Para long tam sayı TL olarak tutulur; kuruş/ondalıklı para eklenmedi. Negatif bakiye, negatif harcama ve toplama/çarpma taşması engellenir. Gelir/gider başarılı biçimde kaydedilmeden bellek bakiyesi değiştirilmez.

6. Sipariş işlemi ve sonraki teslimat bağlantısı

Ürün seçilince adet 1 ve yeni bir istek kimliği hazırlanır. Adet yalnızca ASCII rakamlarından oluşan, 1–999 aralığında bir tam sayı olabilir. Boş, sıfır, negatif, ondalıklı, bilimsel yazımlı, boşluklu veya taşan girişler reddedilir. Adet değişince toplam anında güncellenir. Geçersiz girişte butona basılırsa hata bildirimi gösterilir; sipariş oluşmaz.

Oluştur sırasında katalog ve ürün tekrar doğrulanır, seçildiğinden beri fiyatın değişmediği kontrol edilir, toplam checked long hesabıyla bulunur ve bakiye kontrol edilir. Yeni bakiye ve Pending sipariş aday bir belgede birlikte kaydedilir. Kayıt başarısızsa ikisi de uygulanmaz. Aynı istek kimliği tekrar gelse, kayıt yüklemesi sonrasında bile ikinci ödeme veya sipariş oluşmaz. UI ayrıca başarılı taslağın butonunu kapatır; yeni sipariş için ürünü yeniden seçebilirsiniz.

OrderPlaced olayı yalnızca başarılı kalıcı işlemden sonra yayınlanır. Siparişte OrderId, RequestId, ProductId, ProductName anlık kopyası, Quantity, UnitPrice, TotalPaid, CreatedAtUtc ve Status bulunur. Kaynak prefab veya ScriptableObject nesnesi JSON'a yazılmaz. Gelecekte katalog.TryFind(order.ProductId, ..., requireActive:false) ile gerçek prefab ve Definition alınabilir. order.ItemInstanceId(i), her taş için kararlı ID sağlar; gelecekte oluşturulan TidyObject'a AssignInstanceId ile atanabilir. Bu görevde oluşturma yapılmadı.

TrySetOrderStatus, Pending → Processing → Delivered ve uygun Cancelled geçişleri için hazırdır. Tamamlanan sipariş tekrar Pending olamaz. Cancelled durum değişikliği kendiliğinden para iadesi yapmaz; iptal/iade oynanışı bu görevin kapsamında eklenmedi. Sipariş anındaki fiyat, sonraki katalog fiyat değişikliklerinden etkilenmez.

7. Kayıt yaklaşımı

Mevcut TidySaveService ve ITidySaveStorage incelendi. Framework depolama Write metodu void döndürüyor ve dosya yazma hatasını içeride yakalayabiliyor; bu arayüz ödeme için güvenilir commit sonucu vermiyor. Ortak framework'ü değiştirmek yerine aynı persistentDataPath/SaveFolderName altında küçük bir ekonomi dosyası kullanıldı:

C:/Users/pc/AppData/LocalLow/DefaultCompany/tas/TidyUpSaves/stone-market.json

Para ve siparişlerin tamamı tek sürümlü JSON belge içindedir. Önce aynı klasörde geçici dosya diske flush edilir, sonra atomik File.Replace ile asıl dosya değiştirilir. İlk kayıtta File.Move kullanılır. Önceki tam belge .bak olarak korunur. SHA-256 payload bütünlük kontrolü vardır; bu bir çevrimiçi anti-cheat mekanizması değildir. Bozuk veya bilinmeyen sürümlü mevcut kayıt sessizce 10.000 TL'ye sıfırlanmaz; para işlemleri kapalı kalır ve açıklayıcı hata bildirilir. Yalnız yedek kalmışsa da otomatik yeni bakiye oluşturulmaz.

Kaynak ScriptableObject assetlerine runtime sırasında yazılmaz. Açık framework ResetProgress olayı ekonomi başlangıcını sıfırlar; sıradan ProgressChanged/autosave bildirimleri bakiyeyi sıfırlamaz. Bu bağlantı gerçek dosyaları silmeyen bellek depolu framework örneğiyle ayrıca test edildi.

8. Gerçek testler

- MarketCoreChecks: Edit Mode'da CLI üzerinden 47 başarılı otomatik kontrol. Geçersiz adetler, long taşması, 15×100 ödeme, yetersiz bakiye, tekrarlı istek ve kayıt sonrası tekrarı, yanlış ürün/Definition, negatif para, tarihi fiyat, durum geçişleri, bozuk kayıt, başarısız ilk kayıt, başarısız ödeme/gelir kaydı, gerçek atomik dosya değişimi ve açık yeni oyun sınandı.
- MarketPlayChecks: 55 başarılı kontrol, 0 başarısızlık; son turda 1493 oyun karesi ilerledi. E ve fare tıklamaları geçici Input System aygıtlarından üretim koduna gönderildi. Ürün seçimi, TMP odaklanması, anlık toplam, geçersiz adet bildirimleri, başarı, tekrar tıklama, yetersiz bakiye, sipariş satırı, kayıt yükleme, 100 ürün, kaydırma, iki/tek ürün listeleri, Sprite değişimi/oranı, fiyat geçmişi, ESC/Çıkış/tekrar giriş ve taş alma/bırakma doğrulandı.
- MarketRegressionChecks: 9 başarılı kontrol. Mevcut testere %0 ve %100 cevher profiliyle zamanlı kesim yaptı; her durumda iki gerçek runtime mesh yarısı ve doğru cevher metadatası oluştu. Test sonunda dünya snapshot'ı ve geçici ayarlar geri alındı.
- MarketBridgeChecks: 4 başarılı kontrol. Normal framework kaydı bakiyeyi sıfırlamadı; ResetProgress yeni başlangıcı uyguladı; sonraki harcama ve normal kayıt yeni bakiyeyi korudu.
- Ayrı Play Mode kapatma/açma turu: 8.500 TL, bir Pending sipariş ve hazır servis tekrar yüklendi; ikinci ödeme yapılmadı.
- StoneMarket-shop.png ve StoneMarket-orders.png, 1800×1100 kamera görüntüleri olarak alındı ve incelendi.
- Son Console denetiminde yeni runtime hata/uyarı yok; canlı hata ve uyarı sayıları 0. Derleme hatası yok.

Fare ve E testleri gerçek oyun Input System hattından simüle edildi. TMP_InputField karakter işlemesi Unity'nin yerel GUI Event kuyruğunu kullandığı için testte adet yazımı kendi ProcessEvent klavye işleyicisiyle sınandı; sentetik InputSystem.QueueTextEvent bu kuyruğu doldurmadı. Fiziksel donanımla elle yazma oturumu ve bağımsız build yapılmış gibi raporlanmıyor. Tam otomatik Unity Test Runner/NUnit paketi kurulmadı; testler Unity Editor'da CLI script run ile çalıştırıldı.

9. Bulunan ve düzeltilen sorunlar

Adet hesabında C# kısa devre koşulundan doğan başlatılmamış count değişkeni derleme hatası düzeltildi. Eski dikey layout tarafından sürüklenmiş buton RectTransform değerleri yeni yatay düzende sıfır boyuta düşüyordu; eski layout kaldırılıp yalnızca ilgili üç butonun transform override'ları yeni prefab ölçülerine uyarlandı. Sağ panelde adet/toplam alanlarının çakışması giderildi; bildirim monitör içindeki alt şeride taşındı. Tekrarlı ödeme koruması hem UI taslağında hem kayıt içinde RequestId üzerinden kuruldu. Yazma hatasında ödeme/sipariş ayrışmasını önlemek için void framework Write yerine sonuç bildiren atomik birleşik kayıt kullanıldı.

Test sürücüsünde testerenin menzil/hedefleme koşulu ilk denemede sağlanmamıştı; oyuncu konumu ve bakışı düzeltilip iki gerçek kesim tekrar çalıştırıldı. TMP sentetik metin girişinin sınırlaması test yönteminde ayrıştırıldı; üretim projesine ikinci klavye sistemi eklenmedi.

10. Proje güvenliği ve kullanıcıda kalan ayarlar

Mevcut sahnenin kullanıcı değişiklikleri korunarak TidyUp_Warehouse kaydedildi. Gerçek seviye ve ilerleme kayıtları test öncesi yedeklendi; tüm Play testlerinde level save kapalı ve ekonomi kaydı Tests~/ altındaki ayrı sandbox dosyalarındaydı. Son denetimde seviye ve ilerleme içeriklerinin SHA-256 değerleri başlangıçla aynı. Kayıt ayarı geri yüklendi, ekonomi test yolu override'ı kaldırıldı, arka planda çalışma test ayarı eski durumuna getirildi. Eksik script sayısı 0, katalog doğrulama hatası 0. Ayrıntı Market-final-audit.json içinde.

Deneme siparişleri üretim ekonomi kaydına taşınmadı; ilk gerçek yeni oyunda ayar assetindeki başlangıç bakiyesi kullanılır. Geçerli bir gerçek ekonomi kaydı varsa o yüklenir. Test çıktıları Market-core-results.json, Market-play-results.json, Market-cut-regression-results.json, Market-new-game-results.json ve Market-restart-results.json içinde bulunur.

Tamamlamanız gereken zorunlu Inspector bağlantısı yok. Mevcut ürün görseli olmadığı için istenen placeholder kullanıldı; isterseniz Image alanına kendi Sprite'ınızı atayabilirsiniz. Yeni taş türleri/fiyatları ve başlangıç bakiyesi yukarıdaki assetlerden düzenlenir. Geçmiş sipariş ürününü katalogdan tamamen silmenin gelecekteki çözümleme etkisine dikkat edin. Teslimat, gerçek işletim sistemi kapanışı, disk dolması/izin kaybının işletim sistemi seviyesinde fiziksel simülasyonu ve bağımsız build bu aşamada test edilmedi; kayıt başarısızlığı kontrollü depolama adaptörüyle sınandı.

Git işlemi, paket indirme, mevcut veri asseti silme veya mevcut taş/testere scriptlerini değiştirme yapılmadı.
