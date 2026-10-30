1000 taşta küçük takılma incelemesi — 29 Ekim 2026

Mevcut kullanıcı Play Mode oturumu durdurulmadan incelendi. Üretim scriptleri, fizik ayarları ve oyuncu kayıtları değiştirilmedi. Yalnız Tests~/ altında ölçüm yardımcıları ve Documentation~/ altında sonuç dosyaları eklendi.

Sahnede 999 teslimat kaynak nesnesi var; bunlardan 998'i aktif ham taş, kayıtlı bir kaynak iki kesilmiş parçaya sahip. TidyObject toplamı 1020. Aktif ham taşların 934'ü uyanık Rigidbody, 64'ü uyuyor. Uyanıkların 291'i örnek anında 0,05 m/s altında ve düşük açısal hızdaydı. Kalan yığında da küçük fizik hareketleri devam ediyor; en yüksek örnek hız yaklaşık 0,252 m/s.

İlk örnekleme Unity arka planda, Run In Background kapalı olduğundan 0 kare verdi; performans kanıtı olarak kullanılmadı. Kullanıcı Game görünümünü öne alınca ikinci pencerede 2625 kare örneği toplandı. Odak geçişinde Time.unscaledDeltaTime yaklaşık 9,3 saniyelik arka plan boşluğunu taşıdığı için bu değer oyun takılması olarak yorumlanmadı. Main Thread sayacı Editor işlerini de içerebilir; standalone/GPU ölçümü değildir.

Geçerli native sayaç sonuçları:

| Sayaç | Medyan | p95 | Maksimum |
| --- | --- | --- | --- |
| Main Thread | 6,03 ms | 10,21 ms | 213,37 ms |
| Physics.Simulate | 0 ms | 1,79 ms | 5,83 ms |
| Physics.UpdateRigidbodies | 0 ms | 0,29 ms | 0,83 ms |
| FixedBehaviourUpdate | 0 ms | 0,42 ms | 0,92 ms |
| BehaviourUpdate | 0,145 ms | 0,199 ms | 76,17 ms |
| LateBehaviourUpdate | 0,024 ms | 0,033 ms | 68,19 ms |
| GC.Collect | 0 ms | 0 ms | 9,60 ms |
| CPU Render Thread | 1,57 ms | 2,06 ms | 3,51 ms |

Draw Calls Count medyanı 3509. GC Allocated In Frame medyanı 7783 bayt; en yüksek değer 49.778.083 bayt. Bu sayaç arka plan kayıt işlemleri dahil uygulama tahsislerinden etkilenebilir; tüm tahsisin ana iş parçacığında olduğu söylenmiyor.

Kayıt maliyetlerinin ayrı ölçümü (dosya yazmadan):

- Teslimat listesi kopyalama: 0,55 ms; tam DTO kopyalama: 0,48 ms.
- 999 kaynağın, kesilmiş mesh verisi önbelleği kullanılarak snapshot'ı: 13,45 ms.
- Aynı checkpoint'in karşılaştırma/validasyon adımı: 13,69 ms. Bu ölçüm gerçek servise dokunmayan bağımsız bellek servisi üzerinde yapıldı.
- Yaklaşık 5.051.681 karakterlik ekonomi JSON/hash üretimi: 77,08 ms. Normal teslimat/checkpoint akışında bu iş arka planda çalışır; bu tanı çağrısında tek sefer ayrıca ölçülmüştür. Ana oyun iş parçacığında her saniye 77 ms çalıştığı iddia edilmiyor.
- Framework tam sahne snapshot'ı: 38,11 ms; JSON üretimi ayrıca 22,20 ms (4.348.014 karakter). Bunlar dosya I/O hariçtir.

En güçlü takılma kaynağı: periyodik checkpoint ve tam sahne kayıtlarının taş sayısı ile büyüyen CPU/bellek işi. StoneDeliveryManager.Moving, uyanık Rigidbody'yi değişen nesne saydığı için 934 gövdenin küçük hareketleri saniyelik dünya yakalamasına katılıyor. CaptureCheckpoint ana iş parçacığında; yaklaşık 27 ms yakalama/karşılaştırma işi tek karede bütçeyi aşabiliyor. BuildCheckpoint her güncelleme için listedeki kaydı tekrar FindIndex ile arıyor; yüzlerce güncellemede arama maliyeti karesel büyüyor. Büyük belgenin arka planda tekrar üretilmesi de tahsis/GC baskısı yaratıyor. Framework SaveProgress ise tam snapshot/JSON/dosya işini ana iş parçacığında yapmaya devam ediyor.

Fizik mevcut sahnede sürekli yük oluşturuyor fakat ölçülen kısa sıçramaların tamamını açıklamıyor. Çizim sayısı da yüksek; render thread CPU ölçümü bu örnekte baskın değil. GPU süresi ölçülmedi. En yüksek 213 ms Main Thread örneğinin tek başına hangi işe ait olduğu doğrulanmadı; bütün sıçramaları kayıt sistemine kesin bağlayan ayrı marker zaman çizelgesi alınmadı.

Düzeltme öncelikleri: kayıt eşleştirmelerinde ID→indeks sözlüğü; ham taşın değişmeyen cevher/custom state bilgisini her checkpoint'te JSON üretmeden önbellekleme; dünya snapshot yakalamasını sınırlı kare bütçesiyle toplama; framework tam kayıt işini mevcut güvenli arka plan yazıcı yaklaşımıyla düzenleme. Sonra dinlenmiş yığının uyku davranışı ayrı fizik testiyle değerlendirilmeli. Salt IsSleeping yerine büyük toleransla değişiklikleri yok saymak veya taşları kalıcı kinematik yapmak mevcut fizik/kayıt davranışını değiştirebileceğinden bu incelemede uygulanmadı.

Ham çıktılar: Thousand-stone-profile.json ve Thousand-stone-checkpoint-probe.json. Yardımcı scriptler Unity'nin yok saydığı Assets/PC/Tests~/ klasöründe. Üretim düzeltmesi veya standalone karşılaştırma testi bu tanı aşamasında yapılmadı.
