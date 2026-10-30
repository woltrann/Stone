Kamyonla taş teslimatı — 29 Ekim 2026

Son güncelleme: taşlar artık Point merkezli yatay dairenin içinden rastgele seçilen konumlardan düşer. Küçük Spawn Jitter alanı, Inspector'da Spawn Radius ayarına dönüştürüldü; sahnede yarıçap 0,75 metre (çap 1,5 metre). Scene görünümünde Stone Delivery seçiliyken alan yeşil çemberle gösterilir. Point yüksekliği ve konumu korunur. Önceki 0,015 metre sapma açıklamaları ilk teslimin ayarlarını anlatır.

1. Tamamlanan akış

Başarılı ve kalıcı biçimde kaydedilmiş sipariş, mevcut EconomyService.OrderPlaced olayından teslimat kuyruğuna girer. Satın alma butonuna ikinci ödeme/spawn işlemi bağlanmadı. Yeni siparişlerin yanında kayıtlı Pending/Processing siparişler de tanınır. Aynı sipariş kuyruğa bir kere alınır; tek Car sırayla çalışır.

Varsayılan akış: Car dünya (50,1,15) noktasından (-7,1,15) noktasına 3 saniyelik yumuşak hareket yapar; rotasyonu değişmez. 2 saniye bekledikten sonra gerçek Boru/Point üzerinden sipariş adedi kadar taş, en az 0,15 saniye aralıkla oluşturulur. Bütün oluşturma kayıtları doğrulanınca mevcut sipariş Delivered olur; Siparişlerim ekranında Teslim edildi görünür. Car varsayılan olarak 3 saniyede başlangıca döner. Dönüş kapatılırsa sonraki siparişten önce yine yumuşak dönüş yapılır; görünür teleport yoktur.

2. Dosyalar ve mevcut sistemdeki değişiklikler

Yeni üretim dosyaları Assets/PC/Scripts/Delivery altında:

- StoneDeliveryManager.cs: merkezi sipariş olayına bağlanma, tek kuyruk, Car hareketi, bekleme, prefab çözümleme, sıralı döküm, hata/tekrar deneme ve dünya kaydı.
- DeliveredStoneInstance.cs: sipariş/ordinal kimliği; yalnızca ilk dökümde geçici yerçekimi kontrolü ve başlangıç çakışmalarının güvenli ayrılması.
- StoneDeliveryItem.cs: siparişe bağlı taş oluşturma rezervasyonu, doğrulama ve dünya snapshot verisi.
- EconomyDeliveryPersistence.cs: mevcut EconomyService'in partial uzantısı; Processing/sahne sahipliği, taş rezervasyonu/doğrulaması, dünya checkpoint'i ve yükleme doğrulaması.

Mevcut EconomyService.cs içine isteğe bağlı teslimat kayıtları eklendi; ödeme hesapları ve TryPlaceOrder akışı yeniden yazılmadı. Delivered geçişi artık bütün taş kayıtlarının doğrulanmasını gerektirir. Başlangıç/yeni oyun ve sipariş/bakiye kayıtları aynı atomik belgede kalır. Yeni oyun olayı teslimat kuyruğu ve o oyuna ait runtime taşları da temizler. Yalnız dünya checkpoint'lerinde Siparişlerim listesi gereksiz yere yenilenmez.

StoneOrder.cs, mevcut enum değiştirilmeden DeliveryScene bilgisiyle genişletildi. TidyLevelSnapshotter.cs içine generic Restoring/Restored olayları ve domain reset temizliği eklendi. Bu küçük bağlantı, runtime taşları framework ID eşleştirmesinden önce yeniden kurmayı sağlar; framework doğrudan PC/teslimat sınıflarına bağımlı yapılmadı. Mevcut PC, kamera, cursor, katalog, TidyObject, RawStone ve testere üretim scriptleri değiştirilmedi.

Mevcut mağaza test yardımcıları yeni Delivered önkoşuluna uygun hale getirildi; bunların tüm eski test turları bu görevde yeniden çalıştırılmış gibi raporlanmıyor. Yeni test/kurulum yardımcıları Tests~/ altında, gerçek çıktılar Documentation~/ altında tutuluyor. Üretim dosyaları mevcut PC klasöründe. Yeni kamyon modeli/prefabı veya ScriptableObject asset kopyası oluşturulmadı.

3. Sahne ve Inspector

TidyUp_Warehouse sahnesine Stone Delivery kökü ve StoneDeliveryManager eklendi. Inspector'da mevcut Car, Boru/Point, Stone Economy, StoneMarketCatalog ve TidyLevelManager referansları bağlıdır. Car'ın mevcut placeholder modeli kullanıldı. Boru/Point dünya konumu yaklaşık (-6,92;4,34;9,87), rotasyonu ve koordinatları değiştirilmedi. Taşlar mevcut sahnede boru altındaki Hazmat Cage/zemin collider'larına çarpar.

Zorunlu eksik bağlantı bırakılmadı. Stone Delivery nesnesinde şunlar düzenlenebilir:

- Car ve Spawn Point referansları.
- Start Position (50,1,15), Destination (-7,1,15).
- Travel Duration 3; Wait Duration 2.
- Spawn Interval 0,15; Spawn Jitter 0,015.
- Return After Delivery açık; Return Duration 3.
- Blocked Outlet Timeout 5; Retry Delay 5; Checkpoint Interval 1 saniye.

Mevcut katalogdan yeni ürün eklemek yeterlidir; teslimat prefabları koda sabitlenmez. Satıştan kaldırılan ama katalogda pasif duran ürünler, ödenmiş siparişler için çözümlenebilir. Katalog satırı/prefab/Definition tamamen silinmişse teslimat hata verip bekler; para tekrar alınmaz. Referansı düzeltince bekleyen sipariş otomatik tekrar denenir. Yükleme sırasında eksik kalan geçmiş runtime taşların kurulması için referanslar düzeltildikten sonra sahneyi yeniden yüklemek veya yöneticiyi kapatıp açmak gerekir.

4. Prefab ve fizik davranışı

Her taş order.ProductId üzerinden katalogdan çözülür ve o ürünün prefabından oluşturulur. Mesh, materyal, ölçek, TidyObjectDefinition ve RawStone.CutProfile referansları korunur. Mevcut ScriptableObject verileri değiştirilmedi veya kopyalanmadı. Rigidbody/collider'ı olmayan, yanlış Definition bağlı, geçersiz kesme profilli, Y hareketi kilitli veya dinamik harekete uygun olmayan non-convex MeshCollider taşıyan ürün açıklayıcı hata ile durur; bozuk taş sessizce sahneye bırakılmaz.

Mevcut ham taş prefabı Controlled modunda kinematikti. Prefabın fizik profilini değiştirmek yerine DeliveredStoneInstance yalnızca ilk dökümde Rigidbody'yi dinamik ve gravity açık yapar. Oyuncu taşı alınca bu kontrol bırakılır; mevcut pickup, kontrollü bırakma, yerleştirme ve testere davranışı devam eder.

Yeni taşlar küçük yatay sapmayla aynı çıkıştan görünür. İlk anda halen aşağı düşen komşu teslimat taşıyla çakışma varsa yalnız o collider çiftinin çarpışması ayrılana kadar geçici olarak yok sayılır; sonra geri açılır. Yalnızca yerçekimi kullanılır; dışarı fırlatma kuvveti eklenmez. Durgun taş yığını, oyuncu veya statik geometri çıkışı kapatmışsa yeni taş zorla üstüne yerleştirilmez; zaman aşımı ve güvenli tekrar deneme uygulanır. Bu durumda aralık 0,15 saniyeden uzayabilir.

Car'da Rigidbody yoksa Transform üzerinden, varsa hareket boyunca geçici kinematik sahiplikle FixedUpdate/MovePosition üzerinden ilerlenir. Son duruşta interpolasyon geçmişi eşitlenir ve kinematik hareketten kalan hız temizlenir; önceki Rigidbody ayarları geri gelir. Car'ın rotasyonu animasyon tarafından değiştirilmez.

5. Aynı taşın iki kere oluşturulmaması

Her taşın ID'si mevcut order.ItemInstanceId(index) metoduyla market_<siparişID>_<ordinal> biçiminde belirlenir. Prefab önce etkin olmayan bir hazırlık alanında üretilir, ID TidyObject.Awake/registry kaydından önce atanır. Daha sonra:

1) Taşa ait oluşturma rezervasyonu atomik ekonomi dosyasına kaydedilir.
2) Gerçek nesne etkinleştirilir ve mevcut framework registry'sine girer.
3) Oluşturma doğrulanır.
4) Tüm adetler doğrulanınca sipariş Delivered yapılır.

Kayıt hatası rezervasyondan önce olursa hazırlık nesnesi temizlenir. Etkinleştirmeden sonra doğrulama yazımı başarısızsa mevcut fiziksel taş korunur. Tekrar denemede aynı ID bulunup doğrulanır; ikinci taş üretilmez. Uygulama rezervasyon ile etkinleştirme arasında kapanmışsa, kayıtlı ordinal yüklemede tek nesne olarak kurulur. Kısmi siparişler yalnızca eksik ordinarilerle devam eder. Bu adımlar hiçbir zaman ödeme API'sini çağırmaz.

6. Kalıcı dünya kaydı ve yükleme

Teslimat kayıtları mevcut stone-market.json atomik belgesinde bakiye ve siparişlerle birlikte tutulur. Eski version-1 ekonomi dosyaları teslimat alanı olmadan da okunur; gerçek eski kayıt değiştirilmeden ayrıca yüklendi ve doğrulandı. Kaynak asset referansları JSON'a yazılmaz; ürün ID'si katalogdan çözülür.

Her oluşturulan kaynak taş için poz/rotasyon/ölçek, TidyObjectSnapshot durumu ve RawStone custom state saklanır. Kesilen kaynak taşın custom state'i mevcut kesme sistemiyle gerçek iki mesh'in verisini taşır; parçaların poz, placement/carry ve diğer Tidy snapshot alanları da kaydedilir. Yüklemede önce kaynak prefab aynı ID ile kurulur, sonra mevcut RawStone/framework geri yüklemesi parçaları ve durumları kurar.

Runtime teslimat verisi, yalnız kendi taş ID'leri için yetkili kayıttır. Daha eski bir seviye snapshot'ının bu taşları diriltmesini veya teslimatı tekrarlamasını önlemek için yükleme öncesi ilgili girişler güncel teslimat snapshot'larıyla birleştirilir. Normal sahne nesnelerinin snapshot davranışı değiştirilmez. Bu nedenle eski bir seviye snapshot'ı, ekonomi sipariş geçmişini veya tamamlanan teslimatı geriye almaz.

Varsayılan 1 saniyelik checkpoint, ayrıca uygulama pause/quit ve yönetici kapanışında kayıt alınır. Taş sayısı/rezervasyonları taş başına kalıcılaştırılır. Ani güç kesilmesinde son checkpoint'ten sonraki yaklaşık bir saniyelik konum değişikliği geri alınabilir; oluşturma ordinali/ödemesi yeniden uygulanmaz. Yükleme sonrası kısmi teslimatın kamyon animasyonu sahnenin başlangıcından yeniden oynayabilir; taşların sayısal ilerlemesi korunur. Kamera/testere/satış için yeni bir kayıt sistemi yazılmadı.

7. Testler ve gerçek sonuçlar

- DeliveryCoreChecks: 21 başarılı otomatik Editor kontrolü. Processing/sahne sahipliği, ordinal sırası, rezervasyon, yazma hatası, tekrar doğrulama, bütün adetler tamamlanmadan Delivered reddi, dünya checkpoint'i, durum yüklemesi, ek ödeme/OrderPlaced olmaması ve yeni oyun temizliği.
- DeliveryPlayChecks: 37 başarılı canlı kontrol, 0 başarısızlık. 2746 oyun karesi ilerledi. Default 3 saniyelik yol gerçek ölçümde 3,008661 saniye; 2 saniyelik bekleme 2,007136 saniye çıktı. Bu farklar motorun kare zamanlamasıdır; Inspector süreleri tam 3 ve 2'dir. Oyun zamanı kullanılır; pause sırasında hareket ve bekleme ilerlemez.
- Gerçek 1 adet siparişte 1 taş; 10 adet ve farklı ürünün 2 adet siparişinde tam 10+2 taş oluştu. Kuyruk birbirine karışmadı; aralıkların tamamı en az 0,15 saniyeydi. İkinci ürün testi, geçici ve farklı ölçekli bir GameObject prefab şablonuyla yapıldı; kalıcı uydurma ürün asseti eklenmedi.
- Gerçek Rigidbody düşüşü ve mevcut yüzeye çarpma, ürün mesh/materyal/Definition/profil referansları, taş alma ve Controlled kontrole dönüş doğrulandı. Teslim edilen taş mevcut testerede iki gerçek runtime mesh'e kesildi; kaynak nesne kaldırılıp yeniden kurulduğunda aynı ID ve iki kesilmiş parça, tekrar ham taş teslim edilmeden geri geldi.
- DeliveryFailureChecks: 13 başarılı canlı kontrol. Etkinleştirme sonrası doğrulama yazma hatası, ilk taştan sonra kaybolan Point, eksik katalogla ilk doğrulama hatası ve düzeltme sonrası tekrar deneme; ayrıca geçici Rigidbody'li Car'ın MovePosition ve eski ayarlarına dönüşü sınandı. Hata senaryoları yalnız testte süreleri kısaltarak çalıştırıldı; varsayılanlar geri yüklendi.
- DeliveryRestartVerify: 6 başarılı kontrol. Gerçek yetersiz bakiye siparişi Car'ı oynatmadı. Tam Play Mode kapanışı/açılışında 1 teslim edilmiş taş ve 3 adetlik kısmi sipariş tam 1+3 taşla kuruldu. Etkinleşmeden önce kayıtlı ordinal de doğrulandı, ID'ler benzersiz kaldı ve bakiye 99.600 test TL olarak değişmedi.
- Siparişlerim ekranında Teslim edildi yazısı doğrulandı. DeliveryCleanup sonucunda geçici teslimat taşı 0, test siparişi 0. Delivery-legacy-load-results.json gerçek eski ekonomi dosyasının hazır, 9.000 TL ve 1 siparişle değişmeden okunabildiğini gösterir; hiçbir gerçek sipariş testte teslim edilmedi.
- Toplam 77 ana otomatik kontrol geçti. Ayrıca UI/temizlik, eski kayıt okuma ve son güvenlik denetimleri yapıldı. Test sonuçları Delivery-*-results.json dosyalarında; dünya görüntüsü StoneDelivery-world.png içinde.

8. Bulunan ve düzeltilen hatalar

İlk doğrulama başarısızlığında coroutine handle ataması tamamlanmadan hata dönülmesi kuyruğu takılı bırakabilirdi; iş bir kare ertelenerek handle/tekrar deneme sahipliği düzeltildi ve gerçek eksik referans testiyle doğrulandı. Aynı karede tüm taşların oluşturulması yerine aralıklı stream ve başlangıç çarpışma ayrılması kullanıldı.

Rigidbody'li kamyon dönüşte fizik konumu (50,1,15) iken eski interpolasyon pozu gösteriliyor ve kalan kinematik hız dinamik moda geçince sürüklenmeye yol açıyordu. Park anında interpolasyon geçmişi eşitlenip hızlar sıfırlandı. Tekrar testinde Transform ve Rigidbody konumu tam (50,1,15), rotasyon hatası 0 ve önceki gövde ayarları geri gelmişti.

Mevcut framework yalnız sahnede var olan ID'leri geri yükleyebildiği için runtime teslimat taşları başlangıçta kurulmadan eski kayıt onları atlayacaktı. Küçük snapshot provider hook'u ve ekonomi dosyasındaki kaynak/parça checkpoint'leri bu eksikliği giderdi. Kesilmiş kaynak taşın ham olarak yeniden teslim edilmesi de bu yolla engellendi.

9. Güvenlik, temizlik ve sınırlar

Gerçek ekonomi, seviye ve ilerleme dosyaları test boyunca korunmuş ayrı sandbox ile çalışıldı. Son denetimde üç dosyanın da SHA-256 değerleri test öncesiyle aynı. Point poz/rotasyonu ve Car rotasyonu aynı; Inspector varsayılanları 50→-7 / 3 / 2 / 0,15 olarak doğrulandı. Kayıt ayarı ve ekonomi dosyası override'ı eski haline getirildi, sahne kaydedildi. Geçici Rigidbody, taşlar, ikinci ürün şablonu, kamera ve test siparişleri temizlendi. Yeni sandbox dosyaları/.bak/.tmp, yeniden açılış planı ve geçici başlangıç yedeği kaldırıldı; test kodu ve sonuç raporları bırakıldı. Kaynak ScriptableObject assetleri silinmedi/değiştirilmedi/kopyalanmadı; paket veya model indirilmedi, Git işlemi yapılmadı.

Teslimat scriptlerinde derleme hatası veya yeni runtime hatası yok. Beklenen hata enjeksiyonları açıklayıcı teslimat uyarıları verdi. Son Editor Console'unda ayrı bir Unity AI eklentisi hatası mevcut: AssistantPerfRecorder, Logs/assistant-perf.jsonl dosyasını sharing violation nedeniyle açamıyor; ayrıca abonelik yok bilgisi loglanıyor. Bu paket kaynaklı kayıt teslimat koduna ait değil; üçüncü parti paket veya kullanıcının log dosyası değiştirilmedi. Derleme sonrası kısa Hold on içe aktarma durumu tamamlandıktan sonra testlere devam edildi.

Bağımsız build, gerçek güç kesintisi, işletim sistemi seviyesinde disk dolması ve başka sahnelere geçişin tamamı bu görevde test edilmedi. Kayıt yazma hatası kontrollü storage adaptörüyle; yeniden yükleme gerçek Play Mode yeniden açılışı ve runtime dünya rekonstrüksiyonuyla sınandı. Görünürlük mevcut Car placeholder'ı ve kullanıcının verdiği rota/çevre geometrisine bağlıdır; rota için yol bulma veya engelleri kaldırma eklenmedi. Geçmiş teslimat ürünlerinin ID/prefab/Definition bağlantıları korunmalıdır; eksik veride kayıt silinmez ve bozuk taş üretilmez.
