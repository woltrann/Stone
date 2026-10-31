# Yerel göçük ve tamamlanınca küçülme

Çekiç temas noktası çevresinde taşın görsel mesh vertexleri içeri itilir. Etki yarıçapın kenarına doğru yumuşar; UV ve materyal sınırlarında aynı konumdaki vertexler eşit hareket ettirildiğinden açıklık oluşturulmaz. Tekrarlanan vuruşlar birikir; aşırı göçme sınırlıdır ve taş masanın kesit düzleminin altına geçirilmez.

Son vuruşta önceki göçükler korunur, bütün materyaller mevcut kesit materyaline döner ve taş kesit merkezi etrafında 0.92 katsayısıyla küçülür (her doğrultuda %8). Root Transform ölçeği değiştirilmez. Kaynak mesh korunur; kayıt, kaynak geometri + yerel temas noktaları + göçük/küçülme parametrelerini tutar. Tekrar yüklemek küçülmeyi katlamaz. Eski v3 iz kayıtları desteklenir.

StoneCutProfile Inspector ayarları:
- Chipping Dent Depth: 0.018 varsayılan; kaynak mesh köşegenine göre vuruş başına derinlik.
- Chipping Final Size: 0.92 varsayılan; 0.90 = doğrultu başına %10 küçülme.

Değişen dosyalar:
- STONE/Scripts/Cutting/StoneCutProfile.cs: ortak ayarlar.
- TABLE/Chipping/Scripts/CutStoneHalf.Chipping.cs: taş başına parametre ve v4 kayıt.
- TABLE/Chipping/Scripts/CutStoneHalf.ChipPatches.cs: deterministik deformasyon, final küçülme, proxy collider küçültme ve reset/temizlik.
- TABLE/Chipping/Scripts/StoneChippingTable.cs: yeni görsel mesh üzerinden hedefleme; aynı mesh nesnesinin vertexleri değiştiğinde picking cache yenilenir.

Fizik: mevcut compound/convex yaklaşım korunur. Yerel göçükler görsel mesh üzerinde hassastır; mevcut fizik proxyleri yerel çukurları birebir modellemez. Son küçülmede BoxCollider merkez/boyutları ve convex MeshCollider kopyası aynı 0.92 oranıyla küçülür. Dinamik concave collider veya her kare mesh cooking eklenmedi. Göçük sonrası çekiç hedeflemesi gerçek güncel yüzeyi kullanır.

Testler:
- Derleme 0 hata/uyarı.
- DentChecks: 27 kontrol; ölçülebilir yerel göçük (~8.3 mm test taşı), uzak vertexlerin korunması, seam bütünlüğü, kaynak geometri/kayıt değişmezliği, yeni nesnede kayıt rekonstrüksiyonu, %8 küçülme, proxy boyutları, Rigidbody durumunun korunması, masa düzlemi, tekrarlı yüklemede küçülmenin katlanmaması ve v3 uyumu.
- DentHammerRegression: 41 kontrol; kullanıcı taşının test kopyasında 18 gerçek fare tıklaması, 319/319 görünür hedef noktası, metal baş teması ve sap merkezli salınım, altı ardışık vuruşta yeni şekil ve son materyal. Dent-hit-1/3/6.png görüntüleri incelendi.
- DentConvexChecks: 5 kontrol; yalnızca geçici test küpünde kaba mesh göçük sonrası ve tamamlandığında kapalı kalıyor, convex proxy %8 küçülüyor, yükleme/reset doğru. İlk çalıştırmanın test materyali eksik referansı düzeltildikten sonra yeniden geçmiştir. Kalıcı model/asset oluşturulmadı.
- ChippingPlayChecks: mevcut mağaza/teslimat, testere, pickup/drop, placement, masa/kamera kontrolleri, ESC, UI engelleme, boş taş ve kayıt akışı testleri geçti; başarısız kontrol yok.

Testler ayrı dosyada ekonomi ve bellek içi framework kayıt deposuyla çalıştı. Play Mode kapatıldı, başlangıç ayarları geri getirildi; gerçek ekonomi, level ve progress kayıtlarının metinleri aynı doğrulandı. Önceden kirli sahne zorla kaydedilmedi. Yeni paket, shader veya materyal eklenmedi. Her kare deformasyon yapılmaz.
